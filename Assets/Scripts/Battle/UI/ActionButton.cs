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
            label.text = action != null ? action.actionName : "Action";

        if (costLabel != null)
        {
            if (action != null)
                costLabel.text = $"AP {action.apCost} / MP {action.mpCost} / W {action.willCost}";
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
            label.text = mask != null && !string.IsNullOrWhiteSpace(mask.displayName) ? mask.displayName : "Mask";

        if (costLabel != null)
        {
            if (mask != null)
                costLabel.text = $"AP 1 / W {mask.changeWillCost}";
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
