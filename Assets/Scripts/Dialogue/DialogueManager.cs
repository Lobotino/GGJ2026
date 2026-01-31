using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References (auto-created if empty)")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] Text speakerNameText;
    [SerializeField] Text dialogueText;
    [SerializeField] GameObject continueIndicator;

    [Header("Settings")]
    [SerializeField] float typingSpeed = 0.01f;

    bool isActive;
    bool isTyping;
    bool skipRequested;
    bool uiReady;
    Action onCompleteCallback;
    PlayerMovement2D cachedPlayerMovement;

    public bool IsActive => isActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureUI();
    }

    void EnsureUI()
    {
        if (uiReady) return;

        if (dialoguePanel == null)
            BuildUI();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        uiReady = true;
    }

    void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("DialogueCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel — bottom ~28% of screen
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvasGo.transform, false);
        var panelImage = dialoguePanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);
        var panelRect = dialoguePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.28f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Speaker name — top-left of panel
        var nameGo = new GameObject("SpeakerName");
        nameGo.transform.SetParent(dialoguePanel.transform, false);
        speakerNameText = nameGo.AddComponent<Text>();
        speakerNameText.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        speakerNameText.fontSize = 28;
        speakerNameText.fontStyle = FontStyle.Bold;
        speakerNameText.color = Color.yellow;
        speakerNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        speakerNameText.verticalOverflow = VerticalWrapMode.Overflow;
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.72f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(24f, 0f);
        nameRect.offsetMax = new Vector2(0f, -8f);

        // Dialogue text — main area
        var textGo = new GameObject("DialogueText");
        textGo.transform.SetParent(dialoguePanel.transform, false);
        dialogueText = textGo.AddComponent<Text>();
        dialogueText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        dialogueText.fontSize = 24;
        dialogueText.color = Color.white;
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.72f);
        textRect.offsetMin = new Vector2(24f, 16f);
        textRect.offsetMax = new Vector2(-24f, -4f);

        // Continue indicator — bottom-right
        continueIndicator = new GameObject("ContinueIndicator");
        continueIndicator.transform.SetParent(dialoguePanel.transform, false);
        var indText = continueIndicator.AddComponent<Text>();
        indText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        indText.text = "\u25BC";
        indText.fontSize = 20;
        indText.color = Color.white;
        indText.alignment = TextAnchor.LowerRight;
        var indRect = continueIndicator.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.85f, 0f);
        indRect.anchorMax = new Vector2(1f, 0.25f);
        indRect.offsetMin = new Vector2(0f, 8f);
        indRect.offsetMax = new Vector2(-16f, 0f);

        Debug.Log("[Dialogue] UI built programmatically.");
    }

    public void StartDialogue(DialogueData data, PlayerMovement2D playerMovement, Action onComplete = null)
    {
        if (isActive || data == null || data.lines == null || data.lines.Length == 0)
            return;

        EnsureUI();

        if (dialogueText == null)
        {
            Debug.LogError("[Dialogue] dialogueText is null — assign UI references in inspector or let BuildUI create them.");
            return;
        }

        isActive = true;
        onCompleteCallback = onComplete;
        cachedPlayerMovement = playerMovement;

        if (cachedPlayerMovement != null)
        {
            cachedPlayerMovement.LockInputUntilStopped(() =>
            {
                if (dialoguePanel != null)
                    dialoguePanel.SetActive(true);
                StartCoroutine(RunDialogue(data));
            });
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);
            StartCoroutine(RunDialogue(data));
        }
    }

    IEnumerator RunDialogue(DialogueData data)
    {
        for (int i = 0; i < data.lines.Length; i++)
        {
            DialogueLine line = data.lines[i];

            if (speakerNameText != null)
                speakerNameText.text = line.speakerName;

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            yield return StartCoroutine(TypeText(line.text));

            if (continueIndicator != null)
                continueIndicator.SetActive(true);

            yield return StartCoroutine(WaitForAdvance());
        }

        EndDialogue();
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        skipRequested = false;
        dialogueText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (skipRequested)
            {
                dialogueText.text = text;
                break;
            }

            dialogueText.text += text[i];
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    IEnumerator WaitForAdvance()
    {
        yield return null;

        while (true)
        {
            if (GetAdvanceInput())
                yield break;

            yield return null;
        }
    }

    void Update()
    {
        if (!isActive) return;

        if (isTyping && GetAdvanceInput())
            skipRequested = true;
    }

    bool GetAdvanceInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame)
                return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#else
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            return true;
        if (Input.GetMouseButtonDown(0))
            return true;
#endif
        return false;
    }

    void EndDialogue()
    {
        isActive = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (cachedPlayerMovement != null)
            cachedPlayerMovement.UnlockInput();

        cachedPlayerMovement = null;

        Action callback = onCompleteCallback;
        onCompleteCallback = null;
        callback?.Invoke();
    }
}
