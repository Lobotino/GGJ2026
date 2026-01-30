using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenuPanel : MonoBehaviour
{
    [SerializeField] Transform buttonContainer;
    [SerializeField] ActionButton buttonPrefab;
    [SerializeField] Button endTurnButton;
    [SerializeField] Button changeMaskButton;
    [SerializeField] Button backButton;

    readonly List<ActionButton> spawnedButtons = new List<ActionButton>();

    public void ShowActions(IEnumerable<BattleActionData> actions, FighterState actor, Action<BattleActionData> onSelected,
        Action onEndTurn, Action onChangeMask)
    {
        ClearButtons();
        if (actions != null)
        {
            foreach (var action in actions)
            {
                var btn = CreateButton();
                if (btn != null)
                {
                    bool interactable = actor != null && actor.CanUseAction(action);
                    btn.SetupAction(action, interactable, onSelected);
                }
            }
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());
            endTurnButton.gameObject.SetActive(true);
        }

        if (changeMaskButton != null)
        {
            changeMaskButton.onClick.RemoveAllListeners();
            changeMaskButton.onClick.AddListener(() => onChangeMask?.Invoke());
            changeMaskButton.gameObject.SetActive(true);
        }

        if (backButton != null)
            backButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    public void ShowMaskOptions(IEnumerable<BattleMaskData> masks, FighterState actor, Action<BattleMaskData> onSelected,
        Action onBack)
    {
        ClearButtons();
        if (masks != null)
        {
            foreach (var mask in masks)
            {
                if (mask == null) continue;
                var btn = CreateButton();
                if (btn != null)
                {
                    bool interactable = actor != null && actor.CanChangeMask(mask);
                    btn.SetupMask(mask, interactable, onSelected);
                }
            }
        }

        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(false);
        if (changeMaskButton != null)
            changeMaskButton.gameObject.SetActive(false);

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => onBack?.Invoke());
            backButton.gameObject.SetActive(true);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();
    }

    ActionButton CreateButton()
    {
        if (buttonPrefab == null || buttonContainer == null) return null;
        var btn = Instantiate(buttonPrefab, buttonContainer);
        spawnedButtons.Add(btn);
        return btn;
    }
}
