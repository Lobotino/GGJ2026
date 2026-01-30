using System;
using UnityEngine;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Text label;
    [SerializeField] Text costLabel;

    BattleActionData actionData;
    BattleMaskData maskData;

    public void SetupAction(BattleActionData action, bool interactable, Action<BattleActionData> onClick)
    {
        actionData = action;
        maskData = null;
        if (label != null)
            label.text = action != null ? action.actionName : "Действие";

        if (costLabel != null)
        {
            if (action != null)
                costLabel.text = $"ОД {action.apCost} / МП {action.mpCost}";
            else
                costLabel.text = string.Empty;
        }

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(actionData));
        }
    }

    public void SetupMask(BattleMaskData mask, bool interactable, Action<BattleMaskData> onClick)
    {
        maskData = mask;
        actionData = null;

        if (label != null)
            label.text = mask != null && !string.IsNullOrWhiteSpace(mask.displayName) ? mask.displayName : "Маска";

        if (costLabel != null)
        {
            if (mask != null)
                costLabel.text = "ОД 2";
            else
                costLabel.text = string.Empty;
        }

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(maskData));
        }
    }
}
