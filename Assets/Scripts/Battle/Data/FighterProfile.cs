using UnityEngine;

[CreateAssetMenu(fileName = "FighterProfile", menuName = "Game/Battle/Fighter Profile")]
public class FighterProfile : ScriptableObject
{
    public string displayName;
    public StatBlock baseStats;
    public BattleMaskData startingMask;
    public BattleMaskData[] availableMasks;
}
