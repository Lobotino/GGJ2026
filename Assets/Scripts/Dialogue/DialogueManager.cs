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
    [SerializeField] Image dialogueSideImage;

    [Header("Fonts")]
    [SerializeField] Font dialogueFont;
    [SerializeField] Font speakerNameFont;

    [Header("Settings")]
    [SerializeField] float typingSpeed = 0.01f;

    const float TextLeftPadding = 24f;
    const float TextRightPadding = 24f;
    const float TextBottomPadding = 16f;
    const float TextTopPadding = 4f;
    const float SideTextPadding = 520f;
    const float SideImagePadding = 24f;
    const float NameLeftPadding = 24f;
    const float NameRightPadding = 24f;
    const float NameTopPadding = 8f;

    bool isActive;
    bool isTyping;
    bool skipRequested;
    bool uiReady;
    Action onCompleteCallback;
    IEnumerator preEndSequence;
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
        speakerNameText.font = speakerNameFont != null ? speakerNameFont : Font.CreateDynamicFontFromOSFont("Arial", 28);
        speakerNameText.fontSize = 28;
        speakerNameText.fontStyle = FontStyle.Bold;
        speakerNameText.color = Color.yellow;
        speakerNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        speakerNameText.verticalOverflow = VerticalWrapMode.Overflow;
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.72f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(NameLeftPadding, 0f);
        nameRect.offsetMax = new Vector2(-NameRightPadding, -NameTopPadding);

        // Dialogue text — main area
        var textGo = new GameObject("DialogueText");
        textGo.transform.SetParent(dialoguePanel.transform, false);
        dialogueText = textGo.AddComponent<Text>();
        dialogueText.font = dialogueFont != null ? dialogueFont : Font.CreateDynamicFontFromOSFont("Arial", 24);
        dialogueText.fontSize = 24;
        dialogueText.color = Color.white;
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.72f);
        textRect.offsetMin = new Vector2(TextLeftPadding, TextBottomPadding);
        textRect.offsetMax = new Vector2(-TextRightPadding, -TextTopPadding);

        // Right-side image (optional per line)
        var imageGo = new GameObject("DialogueSideImage");
        imageGo.transform.SetParent(dialoguePanel.transform, false);
        dialogueSideImage = imageGo.AddComponent<Image>();
        dialogueSideImage.preserveAspect = true;
        dialogueSideImage.enabled = false;
        var imageRect = imageGo.GetComponent<RectTransform>();
        SetImageAnchorsRight(imageRect);

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

    public void StartDialogue(DialogueData data, PlayerMovement2D playerMovement, Action onComplete = null, IEnumerator preEndSequence = null)
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
        this.preEndSequence = preEndSequence;
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

            ApplySideImage(line.rightImage, line.imageOnLeft);

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            yield return StartCoroutine(TypeText(line.text));

            if (continueIndicator != null)
                continueIndicator.SetActive(true);

            yield return StartCoroutine(WaitForAdvance());
        }

        if (preEndSequence != null)
            yield return StartCoroutine(preEndSequence);

        EndDialogue();
    }

    void ApplySideImage(Sprite sprite, bool placeOnLeft)
    {
        if (dialogueSideImage == null)
            return;

        if (sprite == null)
        {
            dialogueSideImage.sprite = null;
            dialogueSideImage.enabled = false;
            ApplyTextPadding(TextLeftPadding, TextRightPadding);
            ApplyNamePadding(NameLeftPadding, NameRightPadding);
            return;
        }

        dialogueSideImage.sprite = sprite;
        dialogueSideImage.enabled = true;

        if (placeOnLeft)
        {
            SetImageAnchorsLeft(dialogueSideImage.rectTransform);
            ApplyTextPadding(SideTextPadding, TextRightPadding);
            ApplyNamePadding(SideTextPadding, NameRightPadding);
        }
        else
        {
            SetImageAnchorsRight(dialogueSideImage.rectTransform);
            ApplyTextPadding(TextLeftPadding, SideTextPadding);
            ApplyNamePadding(NameLeftPadding, SideTextPadding);
        }
    }

    void ApplyTextPadding(float left, float right)
    {
        if (dialogueText == null)
            return;

        var rect = dialogueText.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.offsetMin = new Vector2(left, TextBottomPadding);
        rect.offsetMax = new Vector2(-right, -TextTopPadding);
    }

    void ApplyNamePadding(float left, float right)
    {
        if (speakerNameText == null)
            return;

        var rect = speakerNameText.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = new Vector2(-right, -NameTopPadding);
    }

    void SetImageAnchorsRight(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.70f, 0.0f);
        rect.anchorMax = new Vector2(1.0f, 1.66f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(-SideImagePadding, 0f);
    }

    void SetImageAnchorsLeft(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.0f, 0.0f);
        rect.anchorMax = new Vector2(0.30f, 1.66f);
        rect.offsetMin = new Vector2(SideImagePadding, 0f);
        rect.offsetMax = Vector2.zero;
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
        preEndSequence = null;
        callback?.Invoke();
    }

    public void HideDialoguePanelImmediate()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
