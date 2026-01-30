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

    public IEnumerator RunBattle(MaskType playerMaskType, MaskType enemyMaskType, AIProfile enemyAIProfile)
    {
        var playerProfile = maskData != null ? maskData.GetFighterProfile(playerMaskType) : null;
        var enemyProfile = maskData != null ? maskData.GetFighterProfile(enemyMaskType) : null;
        var playerMask = maskData != null ? maskData.GetBattleMask(playerMaskType) : null;
        var enemyMask = maskData != null ? maskData.GetBattleMask(enemyMaskType) : null;

        playerState = new FighterState(playerProfile, playerMask);
        enemyState = new FighterState(enemyProfile, enemyMask);

        if (uiController != null)
            uiController.Initialize(playerState, enemyState);

        bool playerTurn = playerState.EffectiveSPD >= enemyState.EffectiveSPD;

        while (playerState.IsAlive && enemyState.IsAlive)
        {
            if (playerTurn)
                yield return RunTurn(playerState, enemyState, true, null);
            else
                yield return RunTurn(enemyState, playerState, false, enemyAIProfile);

            playerTurn = !playerTurn;
        }

        if (uiController != null)
            uiController.ShowResult(playerState.IsAlive ? "Победа" : "Поражение");
    }

    IEnumerator RunTurn(FighterState actor, FighterState target, bool isPlayer, AIProfile aiProfile)
    {
        if (actor == null || target == null) yield break;

        actor.ResetTurnFlags();

        int apFromMask = actor.CurrentMask != null ? actor.CurrentMask.apBonus : 0;
        int apPenalty = actor.GetApPenalty();
        actor.CurrentAP = Mathf.Max(0, baseApPerTurn + apFromMask - apPenalty);

        actor.ApplyTickDamage(true);

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
                            battleArena.SwapFighterSprite(true, command.Mask.maskType);
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
                        ActionExecutor.ExecuteAction(actor, actionTarget, command.Action);
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
                            battleArena.SwapFighterSprite(false, aiCommand.Mask.maskType);
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
                        ActionExecutor.ExecuteAction(actor, actionTarget, aiCommand.Action);
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

        actor.ApplyTickDamage(false);
        actor.TickStatusDurations();
        actor.RemoveExpiredStatuses();
        actor.EndTurn();

        if (uiController != null)
            uiController.RefreshAll();
    }
}
