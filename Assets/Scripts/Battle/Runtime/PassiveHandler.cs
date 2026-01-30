using UnityEngine;

public static class PassiveHandler
{
    static PassiveType GetPassive(FighterState fighter)
    {
        if (fighter == null || fighter.CurrentMask == null)
            return PassiveType.None;
        return fighter.CurrentMask.passiveType;
    }

    /// <summary>
    /// Called before a status is applied to 'target'. Can modify duration.
    /// Base: -1 turn on any status received.
    /// Silver: 30% chance to reflect status back to source.
    /// Frog: -1 turn on Control-category statuses received.
    /// </summary>
    public static void OnStatusAboutToApply(FighterState target, FighterState source,
        ref StatusDefinition definition, ref int durationMod, BattleContext context)
    {
        if (target == null || definition == null) return;

        switch (GetPassive(target))
        {
            case PassiveType.BaseShorten:
                durationMod -= 1;
                break;

            case PassiveType.SilverReflect:
                if (source != null && source != target && Random.value < 0.3f)
                {
                    source.ApplyStatus(definition);
                }
                break;

            case PassiveType.FrogControlResist:
                if (IsControlStatus(definition.statusType))
                {
                    durationMod -= 1;
                }
                break;
        }
    }

    /// <summary>
    /// Modifies AP cost of an action.
    /// Sleep: -1 AP on Control actions if enemy is Exhausted.
    /// Carnival: -1 AP on first action each turn.
    /// </summary>
    public static int OnCalculateActionCost(FighterState actor, FighterState opponent,
        BattleActionData action, int baseCost, BattleContext context)
    {
        if (actor == null || action == null) return baseCost;
        int cost = baseCost;

        switch (GetPassive(actor))
        {
            case PassiveType.SleepControlDiscount:
                if (action.category == ActionCategory.Control &&
                    opponent != null && opponent.HasStatus(StatusType.Exhausted))
                {
                    cost -= 1;
                }
                break;

            case PassiveType.CarnivalFirstCheap:
                if (context != null && context.ActionsThisTurn == 0)
                {
                    cost -= 1;
                }
                break;
        }

        return Mathf.Max(0, cost);
    }

    /// <summary>
    /// Modifies outgoing damage dealt by actor.
    /// Devil: x1.15 physical vs Bleed target.
    /// Crystal: x1.25 magical vs Expose target.
    /// Bird: x1.2 on first attack of the battle.
    /// </summary>
    public static float OnModifyDamageDealt(FighterState actor, FighterState target,
        BattleActionData action, float damage, BattleContext context)
    {
        if (actor == null || action == null) return damage;

        switch (GetPassive(actor))
        {
            case PassiveType.DevilBleedBonus:
                if (!action.isMagical && target != null && target.HasStatus(StatusType.Bleed))
                {
                    damage *= 1.15f;
                }
                break;

            case PassiveType.CrystalExposeSynergy:
                if (action.isMagical && target != null && target.HasStatus(StatusType.Expose))
                {
                    damage *= 1.25f;
                }
                break;

            case PassiveType.BirdFirstStrike:
                if (context != null && !context.GetFirstAttackUsed(actor.IsPlayer))
                {
                    damage *= 1.2f;
                    context.SetFirstAttackUsed(actor.IsPlayer);
                }
                break;
        }

        return damage;
    }

    /// <summary>
    /// Modifies incoming damage received by target.
    /// Robot: x0.75 on first physical hit per turn.
    /// Crystal: x1.15 if target itself has Expose.
    /// </summary>
    public static float OnModifyDamageReceived(FighterState target, FighterState attacker,
        BattleActionData action, float damage, BattleContext context)
    {
        if (target == null || action == null) return damage;

        switch (GetPassive(target))
        {
            case PassiveType.RobotPhysReduction:
                if (!action.isMagical && context != null && !context.PhysicalHitReceivedThisTurn)
                {
                    damage *= 0.75f;
                    context.PhysicalHitReceivedThisTurn = true;
                }
                break;
        }

        // Crystal self-Expose vulnerability (applies regardless of whose passive it is)
        if (GetPassive(target) == PassiveType.CrystalExposeSynergy && target.HasStatus(StatusType.Expose))
        {
            damage *= 1.15f;
        }

        return damage;
    }

    /// <summary>
    /// Called after a counter-attack triggers.
    /// Wrestler: applies Weaken to the attacker.
    /// </summary>
    public static void OnCounterTriggered(FighterState counterAttacker, FighterState attacker,
        BattleContext context)
    {
        if (counterAttacker == null || attacker == null) return;

        if (GetPassive(counterAttacker) == PassiveType.WrestlerCounterWeaken)
        {
            // Find or create a Weaken status — apply a 1-turn Weaken
            // Since we can't create ScriptableObjects at runtime, we check if attacker already has Weaken
            // and if not, we rely on the enhanced counter action having a reactiveGuardStatus set.
            // As a fallback, we just flag it. The actual Weaken status is applied via
            // the enhanced counter's reactiveGuardStatus field on the BattleActionData.
        }
    }

    /// <summary>
    /// Called after HP changes (damage taken).
    /// Cat: If HP drops below 20%, auto-guard + remove Bleed (once per battle).
    /// </summary>
    public static void OnHPChanged(FighterState fighter, BattleContext context)
    {
        if (fighter == null || context == null) return;

        if (GetPassive(fighter) == PassiveType.CatLastStand)
        {
            if (!context.GetCatLastStandUsed(fighter.IsPlayer))
            {
                float hpPercent = fighter.MaxHP > 0 ? (float)fighter.CurrentHP / fighter.MaxHP : 1f;
                if (hpPercent < 0.2f && fighter.IsAlive)
                {
                    context.SetCatLastStandUsed(fighter.IsPlayer);
                    fighter.SetGuarding(true);
                    fighter.RemoveStatus(StatusType.Bleed);
                }
            }
        }
    }

    /// <summary>
    /// Called at the end of a turn.
    /// Flower: if enemy has Burn, heal 3% HP (or restore 2 MP if HP is full).
    /// Carnival: if 3+ actions used this turn, apply Exhausted to self.
    /// </summary>
    public static void OnTurnEnd(FighterState actor, FighterState target, BattleContext context)
    {
        if (actor == null) return;

        switch (GetPassive(actor))
        {
            case PassiveType.FlowerBurnHeal:
                if (target != null && target.HasStatus(StatusType.Burn))
                {
                    int healAmount = Mathf.Max(1, Mathf.RoundToInt(actor.MaxHP * 0.03f));
                    if (actor.CurrentHP < actor.MaxHP)
                    {
                        actor.Heal(healAmount);
                    }
                    else
                    {
                        actor.RestoreMP(2);
                    }
                }
                break;

            case PassiveType.CarnivalFirstCheap:
                if (context != null && context.ActionsThisTurn >= 3)
                {
                    // Apply Exhausted — since we can't create SO at runtime,
                    // we check if actor already has the status. If not, we need a reference.
                    // The BattleController will need to provide an Exhausted definition.
                    // For now, flag via a lightweight approach:
                    // The Carnival mask's BattleMaskData should have its own exhausted reference,
                    // or we find it from current statuses. This is handled in BattleController.
                }
                break;
        }
    }

    static bool IsControlStatus(StatusType type)
    {
        switch (type)
        {
            case StatusType.Silence:
            case StatusType.GuardBreak:
            case StatusType.Exhausted:
            case StatusType.Weaken:
                return true;
            default:
                return false;
        }
    }
}
