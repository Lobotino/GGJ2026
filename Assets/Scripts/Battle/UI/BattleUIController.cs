using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [SerializeField] FighterHUD playerHUD;
    [SerializeField] FighterHUD enemyHUD;
    [SerializeField] ActionMenuPanel actionMenu;
    [SerializeField] GameObject resultPanel;
    [SerializeField] Text resultText;

    FighterState playerState;
    FighterState enemyState;
    bool awaitingCommand;
    bool showMaskList;
    bool commandReady;
    PlayerCommand pendingCommand;
    string debugResult;
    Vector2 debugScroll;

    public void Initialize(FighterState player, FighterState enemy)
    {
        playerState = player;
        enemyState = enemy;
        if (resultPanel != null)
            resultPanel.SetActive(false);
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (playerHUD != null && playerState != null)
            playerHUD.Refresh(playerState, playerState.Profile != null ? playerState.Profile.displayName : "Игрок");
        if (enemyHUD != null && enemyState != null)
            enemyHUD.Refresh(enemyState, enemyState.Profile != null ? enemyState.Profile.displayName : "Враг");
    }

    public IEnumerator WaitForPlayerCommand(FighterState player, Action<PlayerCommand> onSelected)
    {
        if (actionMenu == null)
        {
            awaitingCommand = true;
            showMaskList = false;
            commandReady = false;
            pendingCommand = default;

            while (!commandReady)
                yield return null;

            awaitingCommand = false;
            onSelected?.Invoke(pendingCommand);
            yield break;
        }

        bool chosen = false;
        PlayerCommand command = default;

        void Choose(PlayerCommand result)
        {
            command = result;
            chosen = true;
        }

        Action showActions = null;
        showActions = () =>
        {
            actionMenu.ShowActions(player.CurrentMask != null ? player.CurrentMask.availableActions : null, player,
                action => Choose(new PlayerCommand { Type = PlayerCommandType.UseAction, Action = action }),
                () => Choose(new PlayerCommand { Type = PlayerCommandType.EndTurn }),
                () =>
                {
                    bool canChange = false;
                    foreach (var mask in player.AvailableMasks)
                    {
                        if (player.CanChangeMask(mask))
                        {
                            canChange = true;
                            break;
                        }
                    }
                    if (!canChange)
                    {
                        showActions();
                        return;
                    }
                    actionMenu.ShowMaskOptions(player.AvailableMasks, player,
                        mask => Choose(new PlayerCommand { Type = PlayerCommandType.ChangeMask, Mask = mask }),
                        showActions);
                });
        };

        showActions();

        while (!chosen)
            yield return null;

        actionMenu.Hide();
        onSelected?.Invoke(command);
    }

    public void ShowResult(string message)
    {
        if (resultText != null)
            resultText.text = message;
        if (resultPanel != null)
            resultPanel.SetActive(true);
        if (resultPanel == null)
            debugResult = message;
    }

    string FormatStatuses(FighterState fighter)
    {
        if (fighter.ActiveStatuses.Count == 0)
            return "Нет";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < fighter.ActiveStatuses.Count; i++)
        {
            var s = fighter.ActiveStatuses[i];
            if (i > 0) sb.Append(", ");
            sb.Append($"{s.Definition.statusType}({s.RemainingTurns})");
        }
        return sb.ToString();
    }

    void OnGUI()
    {
        if (playerState == null || enemyState == null)
            return;

        if (!awaitingCommand && !string.IsNullOrEmpty(debugResult))
        {
            Rect resultRect = new Rect(10f, 10f, 200f, 60f);
            GUI.Box(resultRect, debugResult);
            return;
        }

        if (!awaitingCommand)
            return;

        float width = 340f;
        float height = 400f;
        Rect panel = new Rect(10f, Screen.height - height - 10f, width, height);
        GUILayout.BeginArea(panel, GUI.skin.box);

        GUILayout.Label($"Игрок: Здоровье {playerState.CurrentHP}/{playerState.MaxHP}  Мана {playerState.CurrentMP}/{playerState.MaxMP}");
        GUILayout.Label($"ОД {playerState.CurrentAP}");
        GUILayout.Label($"Маска: {(playerState.CurrentMask != null ? playerState.CurrentMask.displayName : "Нет")}");
        GUILayout.Label($"Статусы: {FormatStatuses(playerState)}");
        GUILayout.Space(4f);
        GUILayout.Label($"Враг: Здоровье {enemyState.CurrentHP}/{enemyState.MaxHP}  Мана {enemyState.CurrentMP}/{enemyState.MaxMP}");
        GUILayout.Label($"Маска врага: {(enemyState.CurrentMask != null ? enemyState.CurrentMask.displayName : "Нет")}");
        GUILayout.Label($"Статусы: {FormatStatuses(enemyState)}");

        GUILayout.Space(8f);
        float listHeight = 160f;
        if (!showMaskList)
        {
            GUILayout.Label("Действия:");
            debugScroll = GUILayout.BeginScrollView(debugScroll, GUILayout.Height(listHeight));
            if (playerState.CurrentMask != null && playerState.CurrentMask.availableActions != null)
            {
                foreach (var action in playerState.CurrentMask.availableActions)
                {
                    if (action == null) continue;
                    GUI.enabled = playerState.CanUseAction(action);
                    string label = action.actionName;
                    string details = $"ОД {action.apCost}";
                    if (action.mpCost > 0) details += $" / МП {action.mpCost}";
                    if (action.basePower > 0 && !action.isHealing) details += $" / Урон {action.basePower}";
                    if (action.basePower > 0 && action.isHealing) details += $" / Лечение {action.basePower}";
                    if (action.statusToApply != null) details += $" / {action.statusToApply.statusType}";
                    if (GUILayout.Button($"{label} ({details})"))
                    {
                        pendingCommand = new PlayerCommand { Type = PlayerCommandType.UseAction, Action = action };
                        commandReady = true;
                    }
                    GUI.enabled = true;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            if (GUILayout.Button("Сменить маску"))
            {
                showMaskList = true;
                debugScroll = Vector2.zero;
            }
            if (GUILayout.Button("Конец хода"))
            {
                pendingCommand = new PlayerCommand { Type = PlayerCommandType.EndTurn };
                commandReady = true;
            }
        }
        else
        {
            GUILayout.Label("Выбор маски:");
            debugScroll = GUILayout.BeginScrollView(debugScroll, GUILayout.Height(listHeight));
            foreach (var mask in playerState.AvailableMasks)
            {
                if (mask == null) continue;
                GUI.enabled = playerState.CanChangeMask(mask);
                if (GUILayout.Button($"{mask.displayName} (ОД 2)"))
                {
                    pendingCommand = new PlayerCommand { Type = PlayerCommandType.ChangeMask, Mask = mask };
                    commandReady = true;
                }
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Назад"))
                showMaskList = false;
        }

        GUILayout.EndArea();
    }
}
