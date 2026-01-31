using UnityEngine;
using UnityEngine.Events;

public enum DialoguePostAction
{
    None,
    StartBattle,
    UnlockPassage,
    GiveItem,
    Custom
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] DialogueData dialogueData;
    [SerializeField] bool oneShot;

    [Header("Return Visit")]
    [Tooltip("If requiredFlag is set and the flag exists, this dialogue plays instead")]
    [SerializeField] string requiredFlag;
    [SerializeField] DialogueData returnDialogueData;
    [SerializeField] DialoguePostAction returnPostAction;

    [Header("Post Action")]
    [SerializeField] DialoguePostAction postAction;

    [Header("StartBattle Settings")]
    [SerializeField] BattleTransitionManager battleTransitionManager;
    [SerializeField] AIProfile aiProfile;

    [Header("UnlockPassage Settings")]
    [SerializeField] GameObject[] objectsToActivate;
    [SerializeField] GameObject[] objectsToDeactivate;

    [Header("Flags")]
    [Tooltip("Flag to set after this dialogue completes")]
    [SerializeField] string setFlagOnComplete;

    [Header("Custom Action")]
    [SerializeField] UnityEvent onDialogueComplete;

    bool hasTriggered;
    bool ready;

    void Start()
    {
        // Skip triggers that fire on the very first physics frame (player spawns inside collider)
        ready = false;
        Invoke(nameof(BecomeReady), 0.1f);
    }

    void BecomeReady() => ready = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ready) return;
        if (!other.CompareTag("Player")) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsActive) return;
        if (oneShot && hasTriggered) return;

        bool useReturn = !string.IsNullOrEmpty(requiredFlag) && DialogueFlags.HasFlag(requiredFlag);
        DialogueData data = useReturn ? returnDialogueData : dialogueData;
        DialoguePostAction action = useReturn ? returnPostAction : postAction;

        if (data == null || data.lines == null || data.lines.Length == 0) return;

        var playerMovement = other.GetComponent<PlayerMovement2D>();

        DialogueManager.Instance.StartDialogue(data, playerMovement, () =>
        {
            hasTriggered = true;
            ExecutePostAction(action, other);
        });
    }

    void ExecutePostAction(DialoguePostAction action, Collider2D player)
    {
        if (!string.IsNullOrEmpty(setFlagOnComplete))
            DialogueFlags.SetFlag(setFlagOnComplete);

        switch (action)
        {
            case DialoguePostAction.None:
                break;

            case DialoguePostAction.StartBattle:
                if (battleTransitionManager != null && !battleTransitionManager.InBattle)
                {
                    var playerMask = player.GetComponent<CharacterMask>();
                    var npcMask = GetComponent<CharacterMask>();
                    MaskType pMask = playerMask != null ? playerMask.CurrentMask : MaskType.None;
                    MaskType nMask = npcMask != null ? npcMask.CurrentMask : MaskType.None;
                    var playerMovement = player.GetComponent<PlayerMovement2D>();
                    battleTransitionManager.StartBattle(pMask, nMask, playerMovement, aiProfile);
                }
                break;

            case DialoguePostAction.UnlockPassage:
                if (objectsToActivate != null)
                    foreach (var obj in objectsToActivate)
                        if (obj != null) obj.SetActive(true);
                if (objectsToDeactivate != null)
                    foreach (var obj in objectsToDeactivate)
                        if (obj != null) obj.SetActive(false);
                break;

            case DialoguePostAction.GiveItem:
                onDialogueComplete?.Invoke();
                break;

            case DialoguePostAction.Custom:
                onDialogueComplete?.Invoke();
                break;
        }
    }
}
