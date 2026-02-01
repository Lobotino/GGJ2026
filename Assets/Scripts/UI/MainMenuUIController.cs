using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class MainMenuUIController : MonoBehaviour
{
    private const string SampleSceneName = "SampleScene";

    private static readonly Color BackgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
    private static readonly Color PanelColor = new Color(0.12f, 0.14f, 0.17f, 0.95f);

    private UIDocument document;
    private PanelSettings runtimePanel;

    private void OnEnable()
    {
        EnsureDocument();
        BuildUI();
    }

    private void OnDestroy()
    {
        if (runtimePanel != null)
        {
            Destroy(runtimePanel);
            runtimePanel = null;
        }
    }

    private void EnsureDocument()
    {
        if (document == null)
        {
            document = GetComponent<UIDocument>();
            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }
        }

        if (document.panelSettings == null)
        {
            runtimePanel = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanel.name = "MainMenuPanelSettings(Runtime)";
            runtimePanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanel.referenceResolution = new Vector2Int(1920, 1080);
            runtimePanel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimePanel.match = 0.5f;
            runtimePanel.sortingOrder = 0;
            runtimePanel.clearColor = true;
            runtimePanel.colorClearValue = BackgroundColor;
            document.panelSettings = runtimePanel;
        }
    }

    private void BuildUI()
    {
        var root = document.rootVisualElement;
        root.Clear();
        root.style.flexGrow = 1;
        root.style.flexDirection = FlexDirection.Column;
        root.style.alignItems = Align.Center;
        root.style.justifyContent = Justify.Center;
        root.style.backgroundColor = BackgroundColor;
        root.style.position = Position.Relative;

        AddBackdropBlobs(root);

        var panel = new VisualElement();
        panel.style.width = new Length(72, LengthUnit.Percent);
        panel.style.maxWidth = 420;
        panel.style.minWidth = 260;
        panel.style.paddingLeft = 28;
        panel.style.paddingRight = 28;
        panel.style.paddingTop = 26;
        panel.style.paddingBottom = 26;
        panel.style.backgroundColor = PanelColor;
        panel.style.borderTopLeftRadius = 22;
        panel.style.borderTopRightRadius = 22;
        panel.style.borderBottomLeftRadius = 22;
        panel.style.borderBottomRightRadius = 22;
        panel.style.borderBottomWidth = 3;
        panel.style.borderBottomColor = new Color(0.06f, 0.08f, 0.1f, 0.9f);
        panel.style.borderLeftWidth = 1;
        panel.style.borderRightWidth = 1;
        panel.style.borderTopWidth = 1;
        panel.style.borderLeftColor = new Color(0.2f, 0.22f, 0.26f, 0.8f);
        panel.style.borderRightColor = new Color(0.2f, 0.22f, 0.26f, 0.8f);
        panel.style.borderTopColor = new Color(0.22f, 0.24f, 0.28f, 0.8f);
        panel.style.flexDirection = FlexDirection.Column;
        panel.style.alignItems = Align.Stretch;

        var startButton = CreateMenuButton(
            "Старт",
            new Color(0.18f, 0.62f, 0.96f, 1f),
            new Color(0.14f, 0.54f, 0.88f, 1f),
            new Color(0.11f, 0.46f, 0.78f, 1f));
        startButton.clicked += OnStartClicked;

        var exitButton = CreateMenuButton(
            "Выход",
            new Color(0.96f, 0.32f, 0.38f, 1f),
            new Color(0.86f, 0.26f, 0.32f, 1f),
            new Color(0.74f, 0.22f, 0.27f, 1f));
        exitButton.clicked += OnExitClicked;

        panel.Add(startButton);
        panel.Add(exitButton);
        root.Add(panel);
    }

    private void AddBackdropBlobs(VisualElement root)
    {
        var blobLeft = new VisualElement();
        blobLeft.style.position = Position.Absolute;
        blobLeft.style.left = -180;
        blobLeft.style.top = -140;
        blobLeft.style.width = 520;
        blobLeft.style.height = 520;
        blobLeft.style.backgroundColor = new Color(0.2f, 0.45f, 0.8f, 0.18f);
        blobLeft.style.borderTopLeftRadius = 9999;
        blobLeft.style.borderTopRightRadius = 9999;
        blobLeft.style.borderBottomLeftRadius = 9999;
        blobLeft.style.borderBottomRightRadius = 9999;
        root.Add(blobLeft);

        var blobRight = new VisualElement();
        blobRight.style.position = Position.Absolute;
        blobRight.style.right = -160;
        blobRight.style.bottom = -180;
        blobRight.style.width = 460;
        blobRight.style.height = 460;
        blobRight.style.backgroundColor = new Color(0.92f, 0.35f, 0.4f, 0.15f);
        blobRight.style.borderTopLeftRadius = 9999;
        blobRight.style.borderTopRightRadius = 9999;
        blobRight.style.borderBottomLeftRadius = 9999;
        blobRight.style.borderBottomRightRadius = 9999;
        root.Add(blobRight);
    }

    private Button CreateMenuButton(string text, Color normal, Color hover, Color pressed)
    {
        var button = new Button { text = text };
        button.style.height = 56;
        button.style.marginTop = 8;
        button.style.marginBottom = 8;
        button.style.paddingLeft = 18;
        button.style.paddingRight = 18;
        var textColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        button.style.backgroundColor = normal;
        button.style.color = textColor;
        button.style.borderTopLeftRadius = 16;
        button.style.borderTopRightRadius = 16;
        button.style.borderBottomLeftRadius = 16;
        button.style.borderBottomRightRadius = 16;
        button.style.borderBottomWidth = 3;
        button.style.borderBottomColor = Darken(normal, 0.55f);
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopWidth = 1;
        button.style.borderLeftColor = Darken(normal, 0.7f);
        button.style.borderRightColor = Darken(normal, 0.7f);
        button.style.borderTopColor = Lighten(normal, 1.15f);
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.fontSize = 22;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.style.width = new Length(100, LengthUnit.Percent);
        button.style.justifyContent = Justify.Center;
        button.style.alignItems = Align.Center;

        ApplyButtonTextStyle(button, textColor);
        button.RegisterCallback<AttachToPanelEvent>(_ => ApplyButtonTextStyle(button, textColor));

        button.RegisterCallback<MouseEnterEvent>(_ => ApplyButtonState(button, hover, false));
        button.RegisterCallback<MouseLeaveEvent>(_ => ApplyButtonState(button, normal, false));
        button.RegisterCallback<PointerDownEvent>(_ => ApplyButtonState(button, pressed, true));
        button.RegisterCallback<PointerUpEvent>(evt =>
        {
            var isInside = button.worldBound.Contains(evt.position);
            ApplyButtonState(button, isInside ? hover : normal, false);
        });
        button.RegisterCallback<PointerLeaveEvent>(_ => ApplyButtonState(button, normal, false));

        return button;
    }

    private void ApplyButtonState(Button button, Color color, bool isPressed)
    {
        button.style.backgroundColor = color;
        button.style.borderBottomWidth = isPressed ? 1 : 3;
        button.style.borderBottomColor = Darken(color, 0.55f);
        button.style.borderLeftColor = Darken(color, 0.7f);
        button.style.borderRightColor = Darken(color, 0.7f);
        button.style.borderTopColor = Lighten(color, 1.15f);
    }

    private void ApplyButtonTextStyle(Button button, Color textColor)
    {
        var textElement = button.Q<TextElement>();
        if (textElement == null)
        {
            return;
        }

        textElement.style.color = textColor;
        textElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        textElement.style.fontSize = 22;
        textElement.style.unityTextAlign = TextAnchor.MiddleCenter;
        textElement.style.flexGrow = 1;
        textElement.style.marginLeft = 0;
        textElement.style.marginRight = 0;
        textElement.style.marginTop = 0;
        textElement.style.marginBottom = 0;
    }

    private static Color Darken(Color color, float multiplier)
    {
        return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, 1f);
    }

    private static Color Lighten(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            1f);
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene(SampleSceneName);
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
