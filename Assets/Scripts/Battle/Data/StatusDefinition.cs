using UnityEngine;

[CreateAssetMenu(fileName = "StatusDefinition", menuName = "Game/Battle/Status")]
public class StatusDefinition : ScriptableObject
{
    public StatusType statusType;
    [TextArea] public string description;

    public int durationTurns = 1;
    public int tickDamage = 0;
    public bool tickAtTurnStart = false;
    public bool tickAtTurnEnd = false;

    public StatMultiplier statMultipliers = StatMultiplier.One;

    [Header("Restrictions")]
    public bool preventMagic = false;
    public bool preventDefense = false;
    public int apPenalty = 0;
}
