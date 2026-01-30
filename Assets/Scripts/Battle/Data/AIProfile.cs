using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AIProfile", menuName = "Game/Battle/AI Profile")]
public class AIProfile : ScriptableObject
{
    [Serializable]
    public struct AIRule
    {
        [Range(0f, 1f)] public float hpThreshold;
        public BattleMaskData switchToMask;
        public BattleActionData preferredAction;
    }

    public AIRule[] rules;
    public BattleActionData fallbackAction;
    [Min(0)] public int guardEveryNTurns = 0;
}
