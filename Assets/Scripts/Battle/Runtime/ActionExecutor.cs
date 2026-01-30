using UnityEngine;

public static class ActionExecutor
{
    public static void ExecuteAction(FighterState actor, FighterState target, BattleActionData action)
    {
        if (actor == null || action == null) return;

        if (action.grantsGuard)
            actor.SetGuarding(true);
        if (action.grantsCounter)
            actor.SetCountering(true);

        actor.SpendResources(action);

        if (action.isHealing)
        {
            if (target == null) return;
            int healAmount = Mathf.Max(1, action.basePower);
            target.Heal(healAmount);
            if (action.statusToApply != null)
                target.ApplyStatus(action.statusToApply);
            return;
        }

        if (target == null) return;

        if (action.basePower > 0)
        {
            float atk = action.isMagical ? actor.EffectiveMAG : actor.EffectiveATK;
            float def = action.isMagical ? target.EffectiveRES : target.EffectiveDEF;
            float ratio = def <= 0f ? 1f : (atk / def);
            float damage = action.basePower * ratio;

            if (target.IsGuarding)
                damage *= 0.5f;

            if (action.statusToApply != null && target.CurrentMask != null &&
                target.CurrentMask.vulnerability == action.statusToApply.statusType)
                damage *= 1.5f;

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            target.TakeDamage(finalDamage);

            if (target.IsCountering && action.category == ActionCategory.Attack)
            {
                int counterDamage = Mathf.Max(1, Mathf.RoundToInt(target.EffectiveATK * 0.5f));
                actor.TakeDamage(counterDamage);
                target.SetCountering(false);
            }
        }
        if (action.statusToApply != null)
            target.ApplyStatus(action.statusToApply);
    }
}
