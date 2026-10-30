using UnityEngine;
using UnityEngine.UI;

public sealed class CS_PersonalShopperUI : MonoBehaviour
{
    private static readonly Color ChoiceNormalColor = new Color(0.12f, 0.13f, 0.15f, 1f);
    private static readonly Color ChoiceHoverColor = new Color(0.67f, 0.30f, 0.20f, 1f);
    private static readonly Color ChoicePressedColor = new Color(0.48f, 0.20f, 0.13f, 1f);

    [Header("Conversation")]
    [SerializeField] private GameObject conversationPanel;
    [SerializeField] private Text promptText;
    [SerializeField] private Button primaryButton;
    [SerializeField] private Text primaryButtonText;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private Text secondaryButtonText;
    [SerializeField] private Button tertiaryButton;
    [SerializeField] private Text tertiaryButtonText;

    [Header("Speech Bubbles")]
    [SerializeField] private GameObject ambientBubble;
    [SerializeField] private Text ambientText;
    [SerializeField] private GameObject productCommentBubble;
    [SerializeField] private Text productCommentText;

    [SerializeField] private CS_PersonalShopperNPC shopper;

    private CS_ConversationKind conversationKind;

    private enum CS_ConversationKind
    {
        Initial,
        ActiveMode,
        GuideItem
    }

    public bool IsConversationOpen => conversationPanel != null && conversationPanel.activeSelf;

    public void Configure(CS_PersonalShopperNPC controller, GameObject conversation,
        Text prompt, Button primary, Text primaryLabel, Button secondary, Text secondaryLabel,
        GameObject ambient, Text ambientLabel, GameObject productComment, Text productCommentLabel)
    {
        shopper = controller;
        conversationPanel = conversation;
        promptText = prompt;
        primaryButton = primary;
        primaryButtonText = primaryLabel;
        secondaryButton = secondary;
        secondaryButtonText = secondaryLabel;
        ambientBubble = ambient;
        ambientText = ambientLabel;
        productCommentBubble = productComment;
        productCommentText = productCommentLabel;
    }

    public void Configure(CS_PersonalShopperNPC controller, GameObject conversation,
        Text prompt, Button primary, Text primaryLabel, Button secondary, Text secondaryLabel,
        Button tertiary, Text tertiaryLabel, GameObject ambient, Text ambientLabel,
        GameObject productComment, Text productCommentLabel)
    {
        Configure(controller, conversation, prompt, primary, primaryLabel, secondary, secondaryLabel,
            ambient, ambientLabel, productComment, productCommentLabel);
        tertiaryButton = tertiary;
        tertiaryButtonText = tertiaryLabel;
    }

    public void BindShopper(CS_PersonalShopperNPC controller)
    {
        shopper = controller;
    }

    private void Awake()
    {
        EnsureTertiaryButton();
        EnsurePromptInConversationPanel();
        ApplyConversationLayout(true);
        ConfigureChoiceButtons();
        HideAll();
    }

    private void OnEnable()
    {
        if (primaryButton != null)
        {
            primaryButton.onClick.AddListener(HandlePrimaryChoice);
        }

        if (secondaryButton != null)
        {
            secondaryButton.onClick.AddListener(HandleSecondaryChoice);
        }

        if (tertiaryButton != null)
        {
            tertiaryButton.onClick.AddListener(HandleTertiaryChoice);
        }
    }

    private void OnDisable()
    {
        if (primaryButton != null)
        {
            primaryButton.onClick.RemoveListener(HandlePrimaryChoice);
        }

        if (secondaryButton != null)
        {
            secondaryButton.onClick.RemoveListener(HandleSecondaryChoice);
        }

        if (tertiaryButton != null)
        {
            tertiaryButton.onClick.RemoveListener(HandleTertiaryChoice);
        }

        HideAll();
    }

    public void ShowInitialConversation()
    {
        conversationKind = CS_ConversationKind.Initial;
        SetConversation("Welcome back, my love.\nHow can I help you today?",
            "I'LL LOOK AROUND MYSELF", "GIVE ME ADVICE", "SHOW ME YOUR COLLECTIONS", true);
    }

    public void ShowFollowingConversation()
    {
        conversationKind = CS_ConversationKind.ActiveMode;
        SetConversation("Are you okay, darling?", "LEAVE ME ALONE", "NO, KEEP GOING", string.Empty, false);
    }

    public void ShowGuideItem(string message, bool hasNextItem)
    {
        conversationKind = CS_ConversationKind.GuideItem;
        SetConversation(message, hasNextItem ? "NEXT ITEM" : "FINISH", "QUIT", string.Empty, false);
    }

    public void HideConversation()
    {
        SetActive(conversationPanel, false);
    }

    public void ShowAmbient(string message)
    {
        EnsureCanvasActive();
        HideConversation();
        HideProductComment();
        if (ambientText != null)
        {
            ambientText.text = message;
        }

        SetActive(ambientBubble, true);
    }

    public void HideAmbient()
    {
        SetActive(ambientBubble, false);
    }

    public void ShowProductComment(string message)
    {
        EnsureCanvasActive();
        HideConversation();
        HideAmbient();
        if (productCommentText != null)
        {
            productCommentText.text = message;
        }

        SetActive(productCommentBubble, true);
    }

    public void HideProductComment()
    {
        SetActive(productCommentBubble, false);
    }

    public void HideAll()
    {
        HideConversation();
        HideAmbient();
        HideProductComment();
    }

    private void SetConversation(string prompt, string primary, string secondary, string tertiary,
        bool showTertiary)
    {
        EnsureCanvasActive();
        EnsureTertiaryButton();
        EnsurePromptInConversationPanel();
        ApplyConversationLayout(showTertiary);
        ConfigureChoiceButtons();
        HideAmbient();
        HideProductComment();
        if (promptText != null)
        {
            promptText.text = prompt;
        }

        if (primaryButtonText != null)
        {
            primaryButtonText.text = primary;
        }

        if (secondaryButtonText != null)
        {
            secondaryButtonText.text = secondary;
        }

        if (tertiaryButtonText != null)
        {
            tertiaryButtonText.text = tertiary;
        }

        SetActive(tertiaryButton != null ? tertiaryButton.gameObject : null, showTertiary);

        SetActive(conversationPanel, true);
    }

    private void HandlePrimaryChoice()
    {
        if (shopper == null)
        {
            return;
        }

        if (conversationKind == CS_ConversationKind.GuideItem)
        {
            shopper.ContinueGuide();
        }
        else if (conversationKind == CS_ConversationKind.ActiveMode)
        {
            shopper.StopCurrentMode();
        }
        else
        {
            shopper.ChooseBrowseAlone();
        }
    }

    private void HandleSecondaryChoice()
    {
        if (shopper == null)
        {
            return;
        }

        if (conversationKind == CS_ConversationKind.GuideItem)
        {
            shopper.StopCurrentMode();
        }
        else if (conversationKind == CS_ConversationKind.ActiveMode)
        {
            shopper.ContinueCurrentMode();
        }
        else
        {
            shopper.ChooseAdviceMode();
        }
    }

    private void HandleTertiaryChoice()
    {
        if (shopper != null && conversationKind == CS_ConversationKind.Initial)
        {
            shopper.ChooseGuideMode();
        }
    }

    private void EnsureCanvasActive()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null && !canvas.gameObject.activeSelf)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    private void EnsurePromptInConversationPanel()
    {
        if (promptText == null || conversationPanel == null)
        {
            return;
        }

        if (promptText.transform.parent != conversationPanel.transform)
        {
            promptText.transform.SetParent(conversationPanel.transform, false);
        }

        RectTransform rect = promptText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(-64f, 126f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        promptText.alignment = TextAnchor.MiddleCenter;
    }

    private void EnsureTertiaryButton()
    {
        if (tertiaryButton != null || secondaryButton == null)
        {
            return;
        }

        GameObject buttonObject = Instantiate(secondaryButton.gameObject, secondaryButton.transform.parent);
        buttonObject.name = "Tertiary Choice";
        tertiaryButton = buttonObject.GetComponent<Button>();
        Transform label = buttonObject.transform.Find("Label");
        tertiaryButtonText = label != null ? label.GetComponent<Text>() : null;
    }

    private void ConfigureChoiceButtons()
    {
        ConfigureChoiceButton(primaryButton);
        ConfigureChoiceButton(secondaryButton);
        ConfigureChoiceButton(tertiaryButton);
    }

    private static void ConfigureChoiceButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (button.targetGraphic is Image image)
        {
            image.color = Color.white;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = ChoiceNormalColor;
        colors.highlightedColor = ChoiceHoverColor;
        colors.selectedColor = ChoiceHoverColor;
        colors.pressedColor = ChoicePressedColor;
        colors.disabledColor = new Color(ChoiceNormalColor.r, ChoiceNormalColor.g, ChoiceNormalColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    private void ApplyConversationLayout(bool showTertiary)
    {
        if (conversationPanel != null && conversationPanel.TryGetComponent(out RectTransform panelRect))
        {
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 28f);
            panelRect.sizeDelta = new Vector2(680f, 390f);
        }

        if (showTertiary)
        {
            SetButtonPosition(primaryButton, 192f, 64f);
            SetButtonPosition(secondaryButton, 120f, 64f);
            SetButtonPosition(tertiaryButton, 48f, 64f);
        }
        else
        {
            SetButtonPosition(primaryButton, 120f, 64f);
            SetButtonPosition(secondaryButton, 48f, 64f);
        }
    }

    private static void SetButtonPosition(Button button, float y, float height)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(580f, height);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
