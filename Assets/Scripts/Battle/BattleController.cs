using System.Collections;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleUIController uiController;
    [SerializeField] BattleArena battleArena;
    [SerializeField] MaskData maskData;
    [SerializeField] int baseApPerTurn = 3;

    FighterState playerState;
    FighterState enemyState;
    BattleContext battleContext;

    public bool PlayerLost { get; private set; }

    public IEnumerator RunBattle(MaskType playerMaskType, MaskType enemyMaskType, AIProfile enemyAIProfile,
        MaskType playerCompanionMask = MaskType.None, MaskType enemyCompanionMask = MaskType.None)
    {
        PlayerLost = false;
        var playerProfile = maskData != null ? maskData.GetFighterProfile(playerMaskType) : null;
        var enemyProfile = maskData != null ? maskData.GetFighterProfile(enemyMaskType) : null;
        var playerMask = maskData != null ? maskData.GetBattleMask(playerMaskType) : null;
        var enemyMask = maskData != null ? maskData.GetBattleMask(enemyMaskType) : null;

        playerState = new FighterState(playerProfile, playerMask);
        enemyState = new FighterState(enemyProfile, enemyMask);

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

        PlayerLost = !playerState.IsAlive;

        if (uiController != null)
            uiController.ShowResult(playerState.IsAlive ? "Победа" : "Поражение");
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
        TryCompanionAttack(companion, target);

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
                        ActionExecutor.ExecuteAction(actor, actionTarget, command.Action, battleContext);
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
                        ActionExecutor.ExecuteAction(actor, actionTarget, aiCommand.Action, battleContext);
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

    void TryCompanionAttack(CompanionState companion, FighterState target)
    {
        if (companion == null || target == null || !target.IsAlive) return;

        companion.OnTurnStart();

        if (companion.ShouldAttackThisTurn())
        {
            ActionExecutor.ExecuteCompanionAction(companion, target, battleContext);
            companion.ResetAfterAttack();
        }
    }
}
