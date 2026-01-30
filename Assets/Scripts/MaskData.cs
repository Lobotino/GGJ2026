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
}
