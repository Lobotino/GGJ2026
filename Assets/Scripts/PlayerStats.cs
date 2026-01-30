using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    List<MaskType> collectedMasks = new List<MaskType>();

    public IReadOnlyList<MaskType> CollectedMasks => collectedMasks;

    public void AddMask(MaskType mask)
    {
        if (!collectedMasks.Contains(mask))
            collectedMasks.Add(mask);
    }

    public bool HasMask(MaskType mask)
    {
        return collectedMasks.Contains(mask);
    }

    public void RemoveMask(MaskType mask)
    {
        collectedMasks.Remove(mask);
    }
}
