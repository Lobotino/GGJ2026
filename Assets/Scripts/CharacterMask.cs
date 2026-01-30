using UnityEngine;

public class CharacterMask : MonoBehaviour
{
    [SerializeField] MaskType currentMask;

    public MaskType CurrentMask
    {
        get => currentMask;
        set => currentMask = value;
    }
}
