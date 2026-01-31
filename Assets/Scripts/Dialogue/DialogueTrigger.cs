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

[System.Serializable]
public class ConditionalDialogue
{
    [Tooltip("If this flag is set, use this dialogue")]
    public string requiredFlag;
    public DialogueData dialogueData;
    public DialoguePostAction postAction;
    [Tooltip("Flag to set after this dialogue completes")]
    public string setFlagOnComplete;
    [Tooltip("Disable this trigger after this dialogue completes")]
    public bool disableAfter;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Default Dialogue (no flags)")]
    [SerializeField] DialogueData dialogueData;
    [SerializeField] bool disableAfterDefault;

    [Header("Post Action (default)")]
    [SerializeField] DialoguePostAction postAction;

    [Header("Conditional Dialogues (checked top to bottom, first match wins)")]
    [SerializeField] ConditionalDialogue[] conditionalDialogues;

    [Header("StartBattle Settings")]
    [SerializeField] BattleTransitionManager battleTransitionManager;
    [Tooltip("If empty, will be taken from NPCPatrol on the same GameObject")]
    [SerializeField] AIProfile aiProfile;
    [SerializeField] GameObject playerBattlePrefab;
    [SerializeField] GameObject enemyBattlePrefab;
    [SerializeField] MaskType playerCompanionMask = MaskType.None;
    [SerializeField] MaskType enemyCompanionMask = MaskType.None;
    [SerializeField] GameObject playerCompanionPrefab;
    [SerializeField] GameObject enemyCompanionPrefab;

    [Header("UnlockPassage Settings")]
    [SerializeField] GameObject[] objectsToActivate;
    [SerializeField] GameObject[] objectsToDeactivate;

    [Header("Flags")]
    [Tooltip("Flag to set after this dialogue completes")]
    [SerializeField] string setFlagOnComplete;

    [Header("Custom Action")]
    [SerializeField] UnityEvent onDialogueComplete;

    bool disabled;
    bool ready;

    public bool HasDialogue => dialogueData != null && dialogueData.lines != null && dialogueData.lines.Length > 0;

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
        if (disabled) return;

        DialogueData data = dialogueData;
        DialoguePostAction action = postAction;
        string flagToSet = setFlagOnComplete;
        bool disableAfterThis = disableAfterDefault;

        if (conditionalDialogues != null)
        {
            for (int i = 0; i < conditionalDialogues.Length; i++)
            {
                var cd = conditionalDialogues[i];
                if (!string.IsNullOrEmpty(cd.requiredFlag) && DialogueFlags.HasFlag(cd.requiredFlag))
                {
                    data = cd.dialogueData;
                    action = cd.postAction;
                    flagToSet = cd.setFlagOnComplete;
                    disableAfterThis = cd.disableAfter;
                    break;
                }
            }
        }

        if (data == null || data.lines == null || data.lines.Length == 0) return;

        var playerMovement = other.GetComponent<PlayerMovement2D>();
        string capturedFlag = flagToSet;
        bool capturedDisable = disableAfterThis;

        DialogueManager.Instance.StartDialogue(data, playerMovement, () =>
        {
            if (capturedDisable)
                disabled = true;
            ExecutePostAction(action, capturedFlag, other);
        });
    }

    void ExecutePostAction(DialoguePostAction action, string flagToSet, Collider2D player)
    {
        if (!string.IsNullOrEmpty(flagToSet))
            DialogueFlags.SetFlag(flagToSet);

        switch (action)
        {
            case DialoguePostAction.None:
                break;

            case DialoguePostAction.StartBattle:
                BattleTransitionManager btm = battleTransitionManager;
                AIProfile profile = aiProfile;
                MaskType pComp = playerCompanionMask;
                MaskType eComp = enemyCompanionMask;
                GameObject pBattlePrefab = playerBattlePrefab;
                GameObject eBattlePrefab = enemyBattlePrefab;
                GameObject pCompPrefab = playerCompanionPrefab;
                GameObject eCompPrefab = enemyCompanionPrefab;
                var patrol = GetComponent<NPCPatrol>();
                if (patrol != null)
                {
                    if (btm == null) btm = patrol.BattleTransition;
                    if (profile == null) profile = patrol.AiProfile;
                    if (pComp == MaskType.None) pComp = patrol.PlayerCompanionMask;
                    if (eComp == MaskType.None) eComp = patrol.EnemyCompanionMask;
                    if (pBattlePrefab == null) pBattlePrefab = patrol.PlayerBattlePrefab;
                    if (eBattlePrefab == null) eBattlePrefab = patrol.EnemyBattlePrefab;
                    if (pCompPrefab == null) pCompPrefab = patrol.PlayerCompanionPrefab;
                    if (eCompPrefab == null) eCompPrefab = patrol.EnemyCompanionPrefab;
                }
                if (btm != null && !btm.InBattle)
                {
                    var playerMask = player.GetComponent<CharacterMask>();
                    var npcMask = GetComponent<CharacterMask>();
                    MaskType pMask = playerMask != null ? playerMask.CurrentMask : MaskType.None;
                    MaskType nMask = npcMask != null ? npcMask.CurrentMask : MaskType.None;
                    var playerMovement = player.GetComponent<PlayerMovement2D>();
                    btm.StartBattle(pMask, nMask, playerMovement, profile, pComp, eComp,
                        pBattlePrefab, eBattlePrefab, pCompPrefab, eCompPrefab);
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
