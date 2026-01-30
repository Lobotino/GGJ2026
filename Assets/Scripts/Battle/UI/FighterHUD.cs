using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class FighterHUD : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text maskText;
    [SerializeField] Text willText;
    [SerializeField] Text apText;
    [SerializeField] Text hpText;
    [SerializeField] Text mpText;
    [SerializeField] Text statusText;
    [SerializeField] Slider hpSlider;
    [SerializeField] Slider mpSlider;

    public void Refresh(FighterState state, string displayName)
    {
        if (state == null) return;

        if (nameText != null)
            nameText.text = displayName;

        if (maskText != null)
            maskText.text = state.CurrentMask != null && !string.IsNullOrWhiteSpace(state.CurrentMask.displayName)
                ? state.CurrentMask.displayName
                : "Маска";

        if (willText != null)
            willText.text = $"Воля: {state.CurrentWill}/{state.MaxWill}";

        if (apText != null)
            apText.text = $"ОД: {state.CurrentAP}";

        if (hpText != null)
            hpText.text = $"Здоровье: {state.CurrentHP}/{state.MaxHP}";

        if (mpText != null)
            mpText.text = $"Мана: {state.CurrentMP}/{state.MaxMP}";

        if (hpSlider != null)
        {
            hpSlider.maxValue = state.MaxHP;
            hpSlider.value = state.CurrentHP;
        }

        if (mpSlider != null)
        {
            mpSlider.maxValue = state.MaxMP;
            mpSlider.value = state.CurrentMP;
        }

        if (statusText != null)
        {
            if (state.ActiveStatuses == null || state.ActiveStatuses.Count == 0)
            {
                statusText.text = "";
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var status in state.ActiveStatuses)
                {
                    if (status.Definition == null) continue;
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(status.Definition.statusType);
                }
                statusText.text = sb.ToString();
            }
        }
    }
}
