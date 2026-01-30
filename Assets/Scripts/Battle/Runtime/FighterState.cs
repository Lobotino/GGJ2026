using System.Collections.Generic;
using UnityEngine;

public class FighterState
{
    public FighterProfile Profile { get; }
    public StatBlock BaseStats { get; }

    public BattleMaskData CurrentMask { get; private set; }
    public IReadOnlyList<BattleMaskData> AvailableMasks => availableMasks;

    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public int CurrentAP { get; set; }

    public bool IsGuarding { get; private set; }
    public bool IsCountering { get; private set; }

    public int MaskCooldownTurns { get; private set; }

    public BattleContext Context { get; set; }
    public FighterState Opponent { get; set; }
    public bool IsPlayer { get; set; }

    readonly List<BattleMaskData> availableMasks = new List<BattleMaskData>();
    readonly List<ActiveStatus> activeStatuses = new List<ActiveStatus>();

    bool changedMaskThisTurn;
    bool changedMaskLastTurn;

    public FighterState(FighterProfile profile, BattleMaskData startingMask)
    {
        Profile = profile;
        BaseStats = profile != null ? profile.baseStats : new StatBlock();
        CurrentMask = startingMask != null ? startingMask : (profile != null ? profile.startingMask : null);

        if (profile != null && profile.availableMasks != null)
            availableMasks.AddRange(profile.availableMasks);
        else if (CurrentMask != null)
            availableMasks.Add(CurrentMask);

        CurrentHP = MaxHP;
        CurrentMP = MaxMP;
    }

    public bool IsAlive => CurrentHP > 0;

    public int MaxHP => Mathf.Max(1, Mathf.RoundToInt(BaseStats.HP * GetMaskMultiplier().HP * GetStatusMultiplier().HP));
    public int MaxMP => Mathf.Max(0, Mathf.RoundToInt(BaseStats.MP * GetMaskMultiplier().MP * GetStatusMultiplier().MP));
    public float EffectiveATK => BaseStats.ATK * GetMaskMultiplier().ATK * GetStatusMultiplier().ATK;
    public float EffectiveDEF => BaseStats.DEF * GetMaskMultiplier().DEF * GetStatusMultiplier().DEF;
    public float EffectiveMAG => BaseStats.MAG * GetMaskMultiplier().MAG * GetStatusMultiplier().MAG;
    public float EffectiveRES => BaseStats.RES * GetMaskMultiplier().RES * GetStatusMultiplier().RES;
    public float EffectiveSPD => BaseStats.SPD * GetMaskMultiplier().SPD * GetStatusMultiplier().SPD;

    public IReadOnlyList<ActiveStatus> ActiveStatuses => activeStatuses;

    public void ResetTurnFlags()
    {
        IsGuarding = false;
        IsCountering = false;
        changedMaskThisTurn = false;
        if (MaskCooldownTurns > 0)
            MaskCooldownTurns--;
    }

    public void EndTurn()
    {
        changedMaskLastTurn = changedMaskThisTurn;
        changedMaskThisTurn = false;
    }

    public void SetGuarding(bool value)
    {
        IsGuarding = value;
    }

    public void SetCountering(bool value)
    {
        IsCountering = value;
    }

    public bool HasStatus(StatusType statusType)
    {
        foreach (var status in activeStatuses)
        {
            if (status.Definition != null && status.Definition.statusType == statusType)
                return true;
        }
        return false;
    }

    public ActiveStatus GetStatus(StatusType statusType)
    {
        foreach (var status in activeStatuses)
        {
            if (status.Definition != null && status.Definition.statusType == statusType)
                return status;
        }
        return null;
    }

    public bool IsSilenced
    {
        get
        {
            foreach (var status in activeStatuses)
            {
                if (status.Definition != null && status.Definition.preventMagic)
                    return true;
            }
            return false;
        }
    }

    public bool DefenseDisabled
    {
        get
        {
            foreach (var status in activeStatuses)
            {
                if (status.Definition != null && status.Definition.preventDefense)
                    return true;
            }
            return false;
        }
    }

    public int GetApPenalty()
    {
        int penalty = 0;
        foreach (var status in activeStatuses)
        {
            if (status.Definition != null)
                penalty += status.Definition.apPenalty;
        }
        return penalty;
    }

    public int GetEffectiveAPCost(BattleActionData action, FighterState opponent, BattleContext context)
    {
        if (action == null) return 0;
        int cost = action.apCost;
        cost = PassiveHandler.OnCalculateActionCost(this, opponent, action, cost, context);
        return Mathf.Max(0, cost);
    }

    public bool CanUseAction(BattleActionData action)
    {
        if (action == null) return false;
        if (CurrentMask == null || CurrentMask.availableActions == null) return false;
        bool actionAllowed = false;
        foreach (var allowed in CurrentMask.availableActions)
        {
            if (allowed == action)
            {
                actionAllowed = true;
                break;
            }
        }
        if (!actionAllowed) return false;
        int effectiveCost = GetEffectiveAPCost(action, Opponent, Context);
        if (CurrentAP < effectiveCost) return false;
        if (CurrentMP < action.mpCost) return false;
        if (action.isMagical && IsSilenced) return false;
        if (action.category == ActionCategory.Defense && DefenseDisabled) return false;
        return true;
    }

    public bool CanChangeMask(BattleMaskData newMask)
    {
        if (newMask == null) return false;
        if (CurrentMask == newMask) return false;
        if (!availableMasks.Contains(newMask)) return false;
        if (MaskCooldownTurns > 0) return false;
        if (CurrentAP < 2) return false;
        if (changedMaskThisTurn) return false;
        if (CurrentMask != null && CurrentMask.disallowConsecutiveChange && changedMaskLastTurn)
            return false;
        return true;
    }

    public void ChangeMask(BattleMaskData newMask)
    {
        CurrentMask = newMask;
        changedMaskThisTurn = true;
        MaskCooldownTurns = newMask != null ? newMask.changeCooldownTurns : 0;
        ClampResources();
    }

    public void ApplyStatus(StatusDefinition definition)
    {
        if (definition == null) return;
        foreach (var status in activeStatuses)
        {
            if (status.Definition == definition || status.Definition.statusType == definition.statusType)
            {
                status.Refresh(definition);
                ClampResources();
                return;
            }
        }
        activeStatuses.Add(new ActiveStatus(definition));
        ClampResources();
    }

    public void ApplyStatus(StatusDefinition definition, FighterState source, BattleContext context)
    {
        if (definition == null) return;
        StatusDefinition finalDef = definition;
        int durationMod = 0;

        PassiveHandler.OnStatusAboutToApply(this, source, ref finalDef, ref durationMod, context);

        foreach (var status in activeStatuses)
        {
            if (status.Definition == finalDef || status.Definition.statusType == finalDef.statusType)
            {
                status.Refresh(finalDef);
                if (durationMod != 0)
                    status.ReduceDuration(-durationMod);
                ClampResources();
                return;
            }
        }

        var newStatus = new ActiveStatus(finalDef);
        if (durationMod != 0)
            newStatus.ReduceDuration(-durationMod);
        activeStatuses.Add(newStatus);
        ClampResources();
    }

    public bool RemoveStatus(StatusType statusType)
    {
        for (int i = activeStatuses.Count - 1; i >= 0; i--)
        {
            if (activeStatuses[i].Definition != null && activeStatuses[i].Definition.statusType == statusType)
            {
                activeStatuses.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public void ReduceStatusDuration(StatusType statusType, int amount)
    {
        foreach (var status in activeStatuses)
        {
            if (status.Definition != null && status.Definition.statusType == statusType)
            {
                status.ReduceDuration(amount);
                break;
            }
        }
    }

    public void RemoveExpiredStatuses()
    {
        for (int i = activeStatuses.Count - 1; i >= 0; i--)
        {
            if (activeStatuses[i].IsExpired)
                activeStatuses.RemoveAt(i);
        }
    }

    public void TickStatusDurations()
    {
        foreach (var status in activeStatuses)
            status.TickDuration();
    }

    public void ApplyTickDamage(bool atTurnStart)
    {
        foreach (var status in activeStatuses)
        {
            var def = status.Definition;
            if (def == null) continue;
            if (atTurnStart && def.tickAtTurnStart && def.tickDamage > 0)
                TakeDamage(def.tickDamage);
            if (!atTurnStart && def.tickAtTurnEnd && def.tickDamage > 0)
                TakeDamage(def.tickDamage);
        }
    }

    public void SpendResources(BattleActionData action)
    {
        if (action == null) return;
        CurrentAP -= action.apCost;
        CurrentMP -= action.mpCost;
        ClampResources();
    }

    public void SpendResources(BattleActionData action, int effectiveAPCost)
    {
        if (action == null) return;
        CurrentAP -= effectiveAPCost;
        CurrentMP -= action.mpCost;
        ClampResources();
    }

    public void SpendForMaskChange(BattleMaskData mask)
    {
        CurrentAP -= 2;
        ClampResources();
    }

    public void ApplyInertia(BattleMaskData mask)
    {
        if (mask != null && mask.applyInertia)
            CurrentAP = Mathf.Max(0, CurrentAP - Mathf.Max(0, mask.inertiaApPenalty));
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, MaxHP);
    }

    public void RestoreMP(int amount)
    {
        CurrentMP = Mathf.Clamp(CurrentMP + amount, 0, MaxMP);
    }

    public void TakeDamage(int amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP - amount, 0, MaxHP);
    }

    void ClampResources()
    {
        CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
        CurrentMP = Mathf.Clamp(CurrentMP, 0, MaxMP);
    }

    StatMultiplier GetMaskMultiplier()
    {
        return CurrentMask != null ? CurrentMask.statMultipliers : StatMultiplier.One;
    }

    StatMultiplier GetStatusMultiplier()
    {
        StatMultiplier result = StatMultiplier.One;
        foreach (var status in activeStatuses)
        {
            if (status.Definition != null)
                result.Multiply(status.Definition.statMultipliers);
        }
        return result;
    }
}
