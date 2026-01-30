using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaskData", menuName = "Game/Mask Data")]
public class MaskData : ScriptableObject
{
    [Serializable]
    public struct MaskEntry
    {
        public MaskType type;
        public GameObject characterPrefab;
        public BattleMaskData battleMask;
        public FighterProfile fighterProfile;
    }

    [SerializeField] List<MaskEntry> entries;

    public GameObject GetPrefab(MaskType type)
    {
        if (entries == null) return null;
        foreach (var entry in entries)
        {
            if (entry.type == type)
                return entry.characterPrefab;
        }
        return null;
    }

    public BattleMaskData GetBattleMask(MaskType type)
    {
        if (entries == null) return null;
        foreach (var entry in entries)
        {
            if (entry.type == type)
                return entry.battleMask;
        }
        return null;
    }

    public FighterProfile GetFighterProfile(MaskType type)
    {
        if (entries == null) return null;
        foreach (var entry in entries)
        {
            if (entry.type == type)
                return entry.fighterProfile;
        }
        return null;
    }
}
