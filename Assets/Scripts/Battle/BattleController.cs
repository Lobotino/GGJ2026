using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleUIController uiController;
    [SerializeField] BattleArena battleArena;
    [SerializeField] MaskData maskData;
    [SerializeField] int baseApPerTurn = 3;
    [SerializeField] GameObject gameCamera;
    [SerializeField] GameObject battleCamera;

    FighterState playerState;
    FighterState enemyState;
    BattleContext battleContext;
    bool battleActive;

    public bool PlayerLost { get; private set; }

    public IEnumerator RunBattle(MaskType playerMaskType, MaskType enemyMaskType, AIProfile enemyAIProfile,
        MaskType playerCompanionMask = MaskType.None, MaskType enemyCompanionMask = MaskType.None,
        MaskType[] playerAvailableMasksOverride = null, FighterProfile enemyProfileOverride = null)
    {
        battleActive = true;
        PlayerLost = false;
        SetCameraBattle(true);
        var playerProfile = maskData != null ? maskData.GetFighterProfile(MaskType.Base) : null;
        var enemyProfile = enemyProfileOverride != null
            ? enemyProfileOverride
            : (maskData != null ? maskData.GetFighterProfile(enemyMaskType) : null);
        var playerMask = maskData != null ? maskData.GetBattleMask(playerMaskType) : null;
        var enemyMask = maskData != null ? maskData.GetBattleMask(enemyMaskType) : null;

        playerState = new FighterState(playerProfile, playerMask);
        enemyState = new FighterState(enemyProfile, enemyMask);

        if (playerAvailableMasksOverride != null && playerAvailableMasksOverride.Length > 0 && maskData != null)
        {
            var overrideMasks = new System.Collections.Generic.List<BattleMaskData>();
            foreach (var maskType in playerAvailableMasksOverride)
            {
                if (maskType == MaskType.None) continue;
                var mask = maskData.GetBattleMask(maskType);
                if (mask == null)
                    Debug.LogWarning($"[Battle] No BattleMaskData for maskType {maskType} in MaskData.");
                if (mask != null && !overrideMasks.Contains(mask))
                    overrideMasks.Add(mask);
            }
            if (overrideMasks.Count > 0)
                playerState.SetAvailableMasks(overrideMasks, includeCurrentMask: true);
        }

        battleContext = new BattleContext();

        playerState.Context = battleContext;
        playerState.Opponent = enemyState;
        playerState.IsPlayer = true;

        enemyState.Context = battleContext;
        enemyState.Opponent = playerState;
        enemyState.IsPlayer = false;

        // Create companions
        if (playerCompanionMask != MaskType.None && maskData != null)
        {
            var compMask = maskData.GetBattleMask(playerCompanionMask);
            if (compMask != null)
                battleContext.PlayerCompanion = new CompanionState(compMask, playerState);
        }
        if (enemyCompanionMask != MaskType.None && maskData != null)
        {
            var compMask = maskData.GetBattleMask(enemyCompanionMask);
            if (compMask != null)
                battleContext.EnemyCompanion = new CompanionState(compMask, enemyState);
        }

        if (uiController != null)
            uiController.Initialize(playerState, enemyState, battleContext);

        Debug.Log($"[Battle] Player available masks: {FormatMasks(playerState.AvailableMasks)}");

        bool playerTurn = playerState.EffectiveSPD >= enemyState.EffectiveSPD;

        while (playerState.IsAlive && enemyState.IsAlive)
        {
            battleContext.TurnNumber++;

            if (playerTurn)
                yield return RunTurn(playerState, enemyState, true, null);
            else
                yield return RunTurn(enemyState, playerState, false, enemyAIProfile);

            playerTurn = !playerTurn;
        }

        // Death animation
        FighterState dead = !playerState.IsAlive ? playerState : enemyState;
        yield return PlayAnimation(dead, "Die");

        PlayerLost = !playerState.IsAlive;

        if (uiController != null)
            uiController.ShowResult(playerState.IsAlive ? "Победа" : "Поражение");

        SetCameraBattle(false);
        battleActive = false;
    }

    void Update()
    {
        if (!battleActive || battleContext == null)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame)
        {
            battleContext.PlayerCheatNextAttack = true;
            Debug.Log("[Battle] Cheat enabled: next player attack deals 100 damage.");
        }

        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
        {
            battleContext.EnemyCheatNextAttack = true;
            Debug.Log("[Battle] Cheat enabled: next enemy attack deals 100 damage.");
        }
    }

    void SetCameraBattle(bool inBattle)
    {
        if (gameCamera != null)
            gameCamera.SetActive(!inBattle);
        if (battleCamera != null)
            battleCamera.SetActive(inBattle);
    }

    IEnumerator RunTurn(FighterState actor, FighterState target, bool isPlayer, AIProfile aiProfile)
    {
        if (actor == null || target == null) yield break;

        actor.ResetTurnFlags();
        battleContext.ResetTurnTracking();

        int apFromMask = actor.CurrentMask != null ? actor.CurrentMask.apBonus : 0;
        int apPenalty = actor.GetApPenalty();
        actor.CurrentAP = Mathf.Max(0, baseApPerTurn + apFromMask - apPenalty);

        actor.ApplyTickDamage(true);

        // Companion attack at turn start
        CompanionState companion = isPlayer ? battleContext.PlayerCompanion : battleContext.EnemyCompanion;
        yield return TryCompanionAttack(companion, target);

        if (uiController != null)
            uiController.RefreshAll();

        bool turnEnded = false;
        int safetyCounter = 0;

        while (!turnEnded && actor.CurrentAP > 0 && actor.IsAlive && target.IsAlive)
        {
            safetyCounter++;
            if (safetyCounter > 20)
                break;

            if (isPlayer)
            {
                PlayerCommand command = default;
                if (uiController != null)
                    yield return uiController.WaitForPlayerCommand(actor, result => command = result);
                else
                    turnEnded = true;

                if (turnEnded)
                {
                    // No UI available to drive player input.
                }
                else if (command.Type == PlayerCommandType.EndTurn)
                {
                    turnEnded = true;
                }
                else if (command.Type == PlayerCommandType.ChangeMask)
                {
                    if (actor.CanChangeMask(command.Mask))
                    {
                        yield return PlayAnimation(actor, "ChangeMask");
                        actor.SpendForMaskChange(command.Mask);
                        actor.ChangeMask(command.Mask);
                        actor.ApplyInertia(command.Mask);
                        if (battleArena != null)
                            battleArena.SwapFighterSprite(true, command.Mask);
                    }
                    else
                    {
                        turnEnded = true;
                    }
                }
                else if (command.Type == PlayerCommandType.UseAction && command.Action != null)
                {
                    if (actor.CanUseAction(command.Action))
                    {
                        FighterState actionTarget = command.Action.targetSelf ? actor : target;
                        yield return PlayActionAnimation(actor, command.Action);
                        int actorHpBefore = actor.CurrentHP;
                        int targetHpBefore = actionTarget.CurrentHP;
                        ActionExecutor.ExecuteAction(actor, actionTarget, command.Action, battleContext);
                        yield return PlayReturnAndHit(actor, actionTarget, actorHpBefore, targetHpBefore);
                        if (!target.IsAlive)
                            yield return PlayAnimation(target, "Die");
                    }
                }
            }
            else
            {
                AICommand aiCommand = AIDecider.Decide(actor, target, aiProfile);
                if (aiCommand.IsEndTurn)
                {
                    turnEnded = true;
                }
                else if (aiCommand.Mask != null)
                {
                    if (actor.CanChangeMask(aiCommand.Mask))
                    {
                        yield return PlayAnimation(actor, "ChangeMask");
                        actor.SpendForMaskChange(aiCommand.Mask);
                        actor.ChangeMask(aiCommand.Mask);
                        actor.ApplyInertia(aiCommand.Mask);
                        if (battleArena != null)
                            battleArena.SwapFighterSprite(false, aiCommand.Mask);
                    }
                    else
                    {
                        turnEnded = true;
                    }
                }
                else if (aiCommand.Action != null)
                {
                    if (actor.CanUseAction(aiCommand.Action))
                    {
                        FighterState actionTarget = aiCommand.Action.targetSelf ? actor : target;
                        yield return PlayActionAnimation(actor, aiCommand.Action);
                        int actorHpBefore = actor.CurrentHP;
                        int targetHpBefore = actionTarget.CurrentHP;
                        ActionExecutor.ExecuteAction(actor, actionTarget, aiCommand.Action, battleContext);
                        yield return PlayReturnAndHit(actor, actionTarget, actorHpBefore, targetHpBefore);
                        if (!target.IsAlive)
                            yield return PlayAnimation(target, "Die");
                    }
                    else
                    {
                        turnEnded = true;
                    }
                }
            }

            if (uiController != null)
                uiController.RefreshAll();

            if (actor.CurrentAP <= 0)
                turnEnded = true;
        }

        // End-of-turn hooks
        PassiveHandler.OnTurnEnd(actor, target, battleContext);

        actor.ApplyTickDamage(false);
        actor.TickStatusDurations();
        actor.RemoveExpiredStatuses();
        actor.EndTurn();

        if (uiController != null)
            uiController.RefreshAll();
    }

    static string FormatMasks(System.Collections.Generic.IReadOnlyList<BattleMaskData> masks)
    {
        if (masks == null || masks.Count == 0) return "none";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < masks.Count; i++)
        {
            var mask = masks[i];
            if (i > 0) sb.Append(", ");
            if (mask == null) sb.Append("null");
            else if (!string.IsNullOrWhiteSpace(mask.displayName)) sb.Append(mask.displayName);
            else sb.Append(mask.maskType);
        }
        return sb.ToString();
    }

    IEnumerator TryCompanionAttack(CompanionState companion, FighterState target)
    {
        if (companion == null || target == null || !target.IsAlive) yield break;

        companion.OnTurnStart();

        if (companion.ShouldAttackThisTurn())
        {
            // Play companion attack animation and apply damage
            BattleFighterAnimator compAnim = null;
            if (battleArena != null)
            {
                bool isPlayerCompanion = companion == battleContext.PlayerCompanion;
                compAnim = isPlayerCompanion
                    ? battleArena.PlayerCompanionAnimator
                    : battleArena.EnemyCompanionAnimator;
            }

            if (compAnim != null)
                yield return compAnim.PlayAndWait("Attack", lunge: true);

            int hpBefore = target.CurrentHP;
            ActionExecutor.ExecuteCompanionAction(companion, target, battleContext);
            companion.ResetAfterAttack();

            // Hit reaction + companion return in parallel
            if (compAnim != null)
            {
                bool needsHit = target.IsAlive && target.CurrentHP < hpBefore;
                Coroutine returnCo = StartCoroutine(compAnim.PlayReturnBack());

                if (needsHit)
                {
                    BattleFighterAnimator targetAnim = target.IsPlayer
                        ? battleArena.PlayerAnimator
                        : battleArena.EnemyAnimator;
                    if (targetAnim != null)
                        yield return targetAnim.PlayAndWait("Hit", shake: true);
                }

                if (returnCo != null)
                    yield return returnCo;
            }
        }
    }

    IEnumerator PlayAnimation(FighterState fighter, string trigger, bool lunge = false, bool shake = false)
    {
        if (battleArena == null || fighter == null) yield break;

        BattleFighterAnimator anim = fighter.IsPlayer
            ? battleArena.PlayerAnimator
            : battleArena.EnemyAnimator;

        if (anim != null)
            yield return anim.PlayAndWait(trigger, lunge, shake);
    }

    IEnumerator PlayActionAnimation(FighterState actor, BattleActionData action)
    {
        string trigger;
        bool isAttack = false;

        if (!string.IsNullOrEmpty(action.animationTrigger))
        {
            trigger = action.animationTrigger;
            isAttack = !action.grantsGuard && !action.grantsCounter && !action.isEnhancedCounter && !action.targetSelf;
        }
        else if (action.grantsGuard)
        {
            trigger = "Guard";
        }
        else if (action.grantsCounter || action.isEnhancedCounter)
        {
            trigger = "Counter";
        }
        else
        {
            trigger = "Attack";
            isAttack = true;
        }

        yield return PlayAnimation(actor, trigger, lunge: isAttack);
    }

    IEnumerator PlayReturnAndHit(FighterState actor, FighterState actionTarget, int actorHpBefore, int targetHpBefore)
    {
        if (battleArena == null) yield break;

        BattleFighterAnimator actorAnim = actor.IsPlayer
            ? battleArena.PlayerAnimator
            : battleArena.EnemyAnimator;

        bool needsReturn = actorAnim != null;
        bool needsTargetHit = actionTarget != null && actionTarget.CurrentHP < targetHpBefore;
        bool isTargetFinishing = needsTargetHit && !actionTarget.IsAlive;

        // Counter-attack may have damaged the actor
        bool needsActorHit = actor != null && actor.CurrentHP < actorHpBefore;
        bool isActorFinishing = needsActorHit && !actor.IsAlive;

        Coroutine returnCo = null;
        Coroutine targetHitCo = null;

        if (needsReturn)
            returnCo = StartCoroutine(actorAnim.PlayReturnBack());

        if (needsTargetHit)
        {
            BattleFighterAnimator targetAnim = actionTarget.IsPlayer
                ? battleArena.PlayerAnimator
                : battleArena.EnemyAnimator;

            if (targetAnim != null)
                targetHitCo = StartCoroutine(targetAnim.PlayAndWait("Hit", shake: true, finishing: isTargetFinishing));
        }

        if (returnCo != null)
            yield return returnCo;
        if (targetHitCo != null)
            yield return targetHitCo;

        // Play counter-attack hit on the actor (after return and target hit)
        if (needsActorHit && actorAnim != null)
            yield return actorAnim.PlayAndWait("Hit", shake: true, finishing: isActorFinishing);
    }

    IEnumerator PlayHitAnimation(FighterState target)
    {
        if (target == null || !target.IsAlive) yield break;
        yield return PlayAnimation(target, "Hit", shake: true);
    }
}
