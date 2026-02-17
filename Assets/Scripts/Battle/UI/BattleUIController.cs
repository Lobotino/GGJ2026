using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] FighterHUD playerHUD;
    [SerializeField] FighterHUD enemyHUD;

    [Header("Bars (Image.fillAmount 0-1)")]
    [SerializeField] Image playerHPBar;
    [SerializeField] Image enemyHPBar;
    [SerializeField] Image playerMPBar;

    [Header("Action Buttons (scene objects)")]
    [SerializeField] Button attackButton;
    [SerializeField] Button defendButton;
    [SerializeField] Button skill1Button;
    [SerializeField] Button skill2Button;
    [SerializeField] Button changeMaskButton;
    [SerializeField] Button endTurnButton;

    [Header("Action Points (3 icons)")]
    [SerializeField] GameObject apIcon1;
    [SerializeField] GameObject apIcon2;
    [SerializeField] GameObject apIcon3;

    [Header("Result")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] Text resultText;
    [SerializeField] bool showResultMessage;

    FighterState playerState;
    FighterState enemyState;
    BattleContext battleContext;
    bool commandReady;
    PlayerCommand pendingCommand;

    Text attackText;
    Text defendText;
    Text skill1Text;
    Text skill2Text;
    Text changeMaskText;

    string attackLabel;
    string defendLabel;
    string skill1Label;
    string skill2Label;
    string changeMaskLabel;

    void Awake()
    {
        attackText = attackButton != null ? attackButton.GetComponentInChildren<Text>(true) : null;
        defendText = defendButton != null ? defendButton.GetComponentInChildren<Text>(true) : null;
        skill1Text = skill1Button != null ? skill1Button.GetComponentInChildren<Text>(true) : null;
        skill2Text = skill2Button != null ? skill2Button.GetComponentInChildren<Text>(true) : null;
        changeMaskText = changeMaskButton != null ? changeMaskButton.GetComponentInChildren<Text>(true) : null;

        attackLabel = attackText != null ? attackText.text : null;
        defendLabel = defendText != null ? defendText.text : null;
        skill1Label = skill1Text != null ? skill1Text.text : null;
        skill2Label = skill2Text != null ? skill2Text.text : null;
        changeMaskLabel = changeMaskText != null ? changeMaskText.text : null;
    }

    public void Initialize(FighterState player, FighterState enemy, BattleContext context = null)
    {
        playerState = player;
        enemyState = enemy;
        battleContext = context;
        if (resultPanel != null)
            resultPanel.SetActive(false);
        HideAllButtons();
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (playerHUD != null && playerState != null)
            playerHUD.Refresh(playerState, playerState.Profile != null ? playerState.Profile.displayName : "Игрок");
        if (enemyHUD != null && enemyState != null)
            enemyHUD.Refresh(enemyState, enemyState.Profile != null ? enemyState.Profile.displayName : "Враг");

        if (playerState != null)
        {
            if (playerHPBar != null)
                playerHPBar.fillAmount = playerState.MaxHP > 0 ? (float)playerState.CurrentHP / playerState.MaxHP : 0f;
            if (playerMPBar != null)
                playerMPBar.fillAmount = playerState.MaxMP > 0 ? (float)playerState.CurrentMP / playerState.MaxMP : 0f;
        }
        if (enemyState != null && enemyHPBar != null)
            enemyHPBar.fillAmount = enemyState.MaxHP > 0 ? (float)enemyState.CurrentHP / enemyState.MaxHP : 0f;

        RefreshAPIcons();
    }

    void RefreshAPIcons()
    {
        int ap = playerState != null ? playerState.CurrentAP : 0;
        if (apIcon1 != null) apIcon1.SetActive(ap >= 1);
        if (apIcon2 != null) apIcon2.SetActive(ap >= 2);
        if (apIcon3 != null) apIcon3.SetActive(ap >= 3);
    }

    public IEnumerator WaitForPlayerCommand(FighterState player, Action<PlayerCommand> onSelected)
    {
        LogMaskChangeState(player);
        commandReady = false;
        pendingCommand = default;

        ShowActionButtons(player);

        while (!commandReady)
            yield return null;

        HideAllButtons();
        onSelected?.Invoke(pendingCommand);
    }

    public void ShowResult(string message)
    {
        if (!showResultMessage)
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);
            return;
        }
        if (resultText != null)
            resultText.text = message;
        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    void ShowActionButtons(FighterState player)
    {
        ClearAllListeners();
        RestoreOriginalLabels();

        var actions = player.CurrentMask != null ? player.CurrentMask.availableActions : null;

        BattleActionData attackAction = null;
        BattleActionData defendAction = null;
        var skillActions = new List<BattleActionData>();

        if (actions != null)
        {
            foreach (var action in actions)
            {
                if (action == null) continue;
                if (action.category == ActionCategory.Attack && attackAction == null)
                    attackAction = action;
                else if (action.category == ActionCategory.Defense && defendAction == null)
                    defendAction = action;
                else
                    skillActions.Add(action);
            }
        }

        ConfigureActionButton(attackButton, attackAction, player);
        ConfigureActionButton(defendButton, defendAction, player);
        ConfigureActionButton(skill1Button, skillActions.Count > 0 ? skillActions[0] : null, player);
        ConfigureActionButton(skill2Button, skillActions.Count > 1 ? skillActions[1] : null, player);

        // Change mask — show only when there are alternative masks
        if (changeMaskButton != null)
        {
            bool hasAlternatives = player.AvailableMasks != null && player.AvailableMasks.Count > 1;
            changeMaskButton.gameObject.SetActive(hasAlternatives);
            if (hasAlternatives)
            {
                bool canChangeAny = false;
                foreach (var mask in player.AvailableMasks)
                {
                    if (player.CanChangeMask(mask)) { canChangeAny = true; break; }
                }
                changeMaskButton.interactable = canChangeAny;
                changeMaskButton.onClick.AddListener(() =>
                {
                    LogMaskChangeState(player);
                    if (!canChangeAny) return;
                    ShowMaskSelection(player);
                });
            }
        }

        // End turn — always available
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
            endTurnButton.interactable = true;
            endTurnButton.onClick.AddListener(() =>
                SelectCommand(new PlayerCommand { Type = PlayerCommandType.EndTurn }));
        }
    }

    void ShowMaskSelection(FighterState player)
    {
        ClearAllListeners();

        var masks = player.AvailableMasks;
        Button[] slots = { attackButton, defendButton, skill1Button, skill2Button };
        Text[] slotTexts = { attackText, defendText, skill1Text, skill2Text };

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i < masks.Count)
            {
                var mask = masks[i];
                slots[i].gameObject.SetActive(true);
                slots[i].interactable = player.CanChangeMask(mask);
                if (slotTexts[i] != null)
                    slotTexts[i].text = !string.IsNullOrEmpty(mask.displayName) ? mask.displayName : mask.maskType.ToString();
                var captured = mask;
                slots[i].onClick.AddListener(() =>
                    SelectCommand(new PlayerCommand { Type = PlayerCommandType.ChangeMask, Mask = captured }));
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }

        // Mask button becomes "Back"
        if (changeMaskButton != null)
        {
            changeMaskButton.gameObject.SetActive(true);
            changeMaskButton.interactable = true;
            if (changeMaskText != null)
                changeMaskText.text = "Назад";
            changeMaskButton.onClick.AddListener(() => ShowActionButtons(player));
        }

        // End turn stays
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
            endTurnButton.interactable = true;
            endTurnButton.onClick.AddListener(() =>
                SelectCommand(new PlayerCommand { Type = PlayerCommandType.EndTurn }));
        }
    }

    void ConfigureActionButton(Button button, BattleActionData action, FighterState player)
    {
        if (button == null) return;
        if (action == null)
        {
            button.gameObject.SetActive(false);
            return;
        }
        button.gameObject.SetActive(true);
        button.interactable = player.CanUseAction(action);
        var captured = action;
        button.onClick.AddListener(() =>
            SelectCommand(new PlayerCommand { Type = PlayerCommandType.UseAction, Action = captured }));
    }

    void SelectCommand(PlayerCommand command)
    {
        RestoreOriginalLabels();
        pendingCommand = command;
        commandReady = true;
    }

    void RestoreOriginalLabels()
    {
        if (attackText != null && attackLabel != null) attackText.text = attackLabel;
        if (defendText != null && defendLabel != null) defendText.text = defendLabel;
        if (skill1Text != null && skill1Label != null) skill1Text.text = skill1Label;
        if (skill2Text != null && skill2Label != null) skill2Text.text = skill2Label;
        if (changeMaskText != null && changeMaskLabel != null) changeMaskText.text = changeMaskLabel;
    }

    void ClearAllListeners()
    {
        if (attackButton) attackButton.onClick.RemoveAllListeners();
        if (defendButton) defendButton.onClick.RemoveAllListeners();
        if (skill1Button) skill1Button.onClick.RemoveAllListeners();
        if (skill2Button) skill2Button.onClick.RemoveAllListeners();
        if (changeMaskButton) changeMaskButton.onClick.RemoveAllListeners();
        if (endTurnButton) endTurnButton.onClick.RemoveAllListeners();
    }

    void HideAllButtons()
    {
        ClearAllListeners();
        if (attackButton) attackButton.gameObject.SetActive(false);
        if (defendButton) defendButton.gameObject.SetActive(false);
        if (skill1Button) skill1Button.gameObject.SetActive(false);
        if (skill2Button) skill2Button.gameObject.SetActive(false);
        if (changeMaskButton) changeMaskButton.gameObject.SetActive(false);
        if (endTurnButton) endTurnButton.gameObject.SetActive(false);
    }

    void LogMaskChangeState(FighterState player)
    {
        if (player == null) return;
        var sb = new System.Text.StringBuilder();
        string current = player.CurrentMask != null ? player.CurrentMask.displayName : "null";
        sb.Append($"[BattleUI] ChangeMask check: AP={player.CurrentAP}, cooldown={player.MaskCooldownTurns}, current={current}");
        if (player.AvailableMasks != null)
        {
            foreach (var mask in player.AvailableMasks)
            {
                if (mask == null) continue;
                string name = !string.IsNullOrWhiteSpace(mask.displayName) ? mask.displayName : mask.maskType.ToString();
                sb.Append($", {name}:{player.CanChangeMask(mask)}");
            }
        }
        Debug.Log(sb.ToString());
    }
}
