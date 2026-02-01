using UnityEngine;

public static class ActionExecutor
{
    public static void ExecuteAction(FighterState actor, FighterState target, BattleActionData action, BattleContext context)
    {
        if (actor == null || action == null) return;

        // 1. Guard / Counter / Enhanced Counter / Reactive Guard setup
        if (action.grantsGuard)
        {
            actor.SetGuarding(true);
            if (action.reactiveGuard)
            {
                // Reactive guard is tracked on the action itself;
                // damage reflection is handled below when actor takes counter damage
            }
        }
        if (action.grantsCounter)
            actor.SetCountering(true);
        if (action.isEnhancedCounter)
            actor.SetCountering(true);

        // 2. Spend resources with passive discounts
        int effectiveAPCost = actor.GetEffectiveAPCost(action, target, context);
        actor.SpendResources(action, effectiveAPCost);

        // 3. Increment action counter
        if (context != null)
            context.ActionsThisTurn++;

        // 4. Status removal (Сброс, Системная очистка)
        if (action.removeStatusTypes != null && action.removeStatusTypes.Length > 0)
        {
            FighterState removeTarget = action.targetSelf ? actor : target;
            if (removeTarget != null)
            {
                foreach (var statusType in action.removeStatusTypes)
                {
                    if (action.statusDurationReduction > 0)
                        removeTarget.ReduceStatusDuration(statusType, action.statusDurationReduction);
                    else
                        removeTarget.RemoveStatus(statusType);
                }
                removeTarget.RemoveExpiredStatuses();
            }
        }

        // 5. Healing path
        if (action.isHealing)
        {
            FighterState healTarget = action.targetSelf ? actor : target;
            if (healTarget == null) return;
            int healAmount = Mathf.Max(1, action.basePower);
            healTarget.Heal(healAmount);
            if (action.statusToApply != null)
            {
                if (context != null)
                    healTarget.ApplyStatus(action.statusToApply, actor, context);
                else
                    healTarget.ApplyStatus(action.statusToApply);
            }
            ApplySelfStatus(actor, action, context);
            ApplyConditionalAPGain(actor, target, action);
            return;
        }

        if (target == null) return;

        // 6. Damage calculation
        if (action.basePower > 0)
        {
            float atk = action.isMagical ? actor.EffectiveMAG : actor.EffectiveATK;
            float def = action.isMagical ? target.EffectiveRES : target.EffectiveDEF;
            float ratio = def <= 0f ? 1f : (atk / def);
            float damage = action.basePower * ratio;

            // Passive: modify damage dealt (Devil, Crystal, Bird)
            damage = PassiveHandler.OnModifyDamageDealt(actor, target, action, damage, context);

            // Conditional bonus on target status (e.g. bonus damage if target has specific status)
            if (action.conditionalBonusOnTargetStatus && target.HasStatus(action.requiredTargetStatus))
            {
                damage *= action.conditionalDamageMultiplier;
            }

            // Self debuff if condition fails (Резонансный луч)
            if (action.selfDebuffIfConditionFails && action.conditionalBonusOnTargetStatus)
            {
                if (!target.HasStatus(action.requiredTargetStatus) && action.selfDebuffOnFail != null)
                {
                    if (context != null)
                        actor.ApplyStatus(action.selfDebuffOnFail, actor, context);
                    else
                        actor.ApplyStatus(action.selfDebuffOnFail);
                }
            }

            // Last action bonus (Финальный номер: bonus if AP will be 0 after this action)
            if (action.lastActionBonus && actor.CurrentAP <= 0)
            {
                damage *= action.lastActionMultiplier;
            }

            // First turn bonus (Наскок)
            if (action.firstTurnBonus && context != null && context.TurnNumber <= 1)
            {
                damage *= action.firstTurnMultiplier;
            }

            // Guard multiplier
            if (target.IsGuarding)
            {
                if (action.overrideGuardMultiplier)
                    damage *= action.guardMultiplierOverride;
                else
                    damage *= 0.5f;
            }

            // Passive: modify damage received (Robot, Crystal self-vulnerability)
            damage = PassiveHandler.OnModifyDamageReceived(target, actor, action, damage, context);

            // Vulnerability bonus
            if (action.statusToApply != null && target.CurrentMask != null &&
                target.CurrentMask.vulnerability == action.statusToApply.statusType)
                damage *= 1.5f;

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            if (context != null && actor.IsPlayer && context.PlayerCheatNextAttack && target != actor)
            {
                finalDamage = 100;
                context.PlayerCheatNextAttack = false;
            }
            target.TakeDamage(finalDamage);

            // Passive: OnHPChanged (Cat last stand)
            PassiveHandler.OnHPChanged(target, context);

            // 8. Counter-attack
            if (target.IsCountering && action.category == ActionCategory.Attack)
            {
                float counterMult = 0.5f;
                // Check if the counter action on target was an enhanced counter
                // Enhanced counter uses the multiplier from the action that set up the counter
                // For simplicity, we check the target's current mask actions for enhanced counter
                if (HasEnhancedCounter(target))
                {
                    counterMult = GetEnhancedCounterMultiplier(target);
                }

                int counterDamage = Mathf.Max(1, Mathf.RoundToInt(target.EffectiveATK * counterMult));
                actor.TakeDamage(counterDamage);
                target.SetCountering(false);

                // Passive: Wrestler counter applies Weaken
                PassiveHandler.OnCounterTriggered(target, actor, context);

                // Passive: Cat HP check on counter damage
                PassiveHandler.OnHPChanged(actor, context);
            }

            // Reactive guard: if target is guarding and has a reactive guard action,
            // apply status to attacker on physical hit
            if (target.IsGuarding && !action.isMagical)
            {
                ApplyReactiveGuardStatus(target, actor, context);
            }
        }

        // 10. Apply statuses
        if (action.statusToApply != null)
        {
            if (context != null)
                target.ApplyStatus(action.statusToApply, actor, context);
            else
                target.ApplyStatus(action.statusToApply);
        }

        if (action.secondaryStatus != null)
        {
            if (context != null)
                target.ApplyStatus(action.secondaryStatus, actor, context);
            else
                target.ApplyStatus(action.secondaryStatus);
        }

        // Self status on use (e.g. Адский рывок → Expose on self)
        ApplySelfStatus(actor, action, context);

        // 11. Conditional AP gain (Зевок-отсос)
        ApplyConditionalAPGain(actor, target, action);
    }

    // Overload for backwards compatibility
    public static void ExecuteAction(FighterState actor, FighterState target, BattleActionData action)
    {
        ExecuteAction(actor, target, action, null);
    }

    static void ApplySelfStatus(FighterState actor, BattleActionData action, BattleContext context)
    {
        if (action.selfStatusOnUse != null)
        {
            if (context != null)
                actor.ApplyStatus(action.selfStatusOnUse, actor, context);
            else
                actor.ApplyStatus(action.selfStatusOnUse);
        }
    }

    static void ApplyConditionalAPGain(FighterState actor, FighterState target, BattleActionData action)
    {
        if (action.conditionalAPGain && target != null)
        {
            if (action.conditionalBonusOnTargetStatus && target.HasStatus(action.requiredTargetStatus))
            {
                actor.CurrentAP += action.apGainAmount;
            }
            else if (!action.conditionalBonusOnTargetStatus)
            {
                actor.CurrentAP += action.apGainAmount;
            }
        }
    }

    static bool HasEnhancedCounter(FighterState fighter)
    {
        if (fighter.CurrentMask == null || fighter.CurrentMask.availableActions == null)
            return false;
        foreach (var a in fighter.CurrentMask.availableActions)
        {
            if (a != null && a.isEnhancedCounter)
                return true;
        }
        return false;
    }

    static float GetEnhancedCounterMultiplier(FighterState fighter)
    {
        if (fighter.CurrentMask == null || fighter.CurrentMask.availableActions == null)
            return 0.5f;
        foreach (var a in fighter.CurrentMask.availableActions)
        {
            if (a != null && a.isEnhancedCounter)
                return a.enhancedCounterMultiplier;
        }
        return 0.5f;
    }

    static void ApplyReactiveGuardStatus(FighterState guardian, FighterState attacker, BattleContext context)
    {
        if (guardian.CurrentMask == null || guardian.CurrentMask.availableActions == null)
            return;
        foreach (var a in guardian.CurrentMask.availableActions)
        {
            if (a != null && a.reactiveGuard && a.reactiveGuardStatus != null)
            {
                if (context != null)
                    attacker.ApplyStatus(a.reactiveGuardStatus, guardian, context);
                else
                    attacker.ApplyStatus(a.reactiveGuardStatus);
                break;
            }
        }
    }

    public static void ExecuteCompanionAction(CompanionState companion, FighterState target, BattleContext context)
    {
        if (companion == null || target == null || !target.IsAlive) return;

        BattleActionData action = companion.GetCompanionAction();

        if (action == null)
        {
            // Basic hit: flat 3 damage
            target.TakeDamage(3);
            return;
        }

        if (action.basePower > 0 && !action.isHealing)
        {
            float atk = action.isMagical ? companion.Owner.EffectiveMAG : companion.Owner.EffectiveATK;
            float def = action.isMagical ? target.EffectiveRES : target.EffectiveDEF;
            float ratio = def <= 0f ? 1f : (atk / def);
            float damage = action.basePower * ratio;

            // Companion modifier: ×0.5
            damage *= 0.5f;

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            target.TakeDamage(finalDamage);
        }

        // Apply primary status (full duration, simplified — no secondary/self statuses)
        if (action.statusToApply != null)
        {
            if (context != null)
                target.ApplyStatus(action.statusToApply, companion.Owner, context);
            else
                target.ApplyStatus(action.statusToApply);
        }
    }
}
