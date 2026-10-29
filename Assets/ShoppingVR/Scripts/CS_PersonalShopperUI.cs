using UnityEngine;
using UnityEngine.UI;

public sealed class CS_PersonalShopperUI : MonoBehaviour
{
    [Header("Conversation")]
    [SerializeField] private GameObject conversationPanel;
    [SerializeField] private Text promptText;
    [SerializeField] private Button primaryButton;
    [SerializeField] private Text primaryButtonText;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private Text secondaryButtonText;

    [Header("Speech Bubbles")]
    [SerializeField] private GameObject ambientBubble;
    [SerializeField] private Text ambientText;
    [SerializeField] private GameObject productCommentBubble;
    [SerializeField] private Text productCommentText;

    [SerializeField] private CS_PersonalShopperNPC shopper;

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

    public void BindShopper(CS_PersonalShopperNPC controller)
    {
        shopper = controller;
    }

    private void Awake()
    {
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

        HideAll();
    }

    public void ShowInitialConversation()
    {
        SetConversation("How can I help?", "Help me choose an outfit!", "Never mind");
    }

    public void ShowFollowingConversation()
    {
        SetConversation("Would you like me to keep helping?", "Keep helping me", "Stop following me");
    }

    public void HideConversation()
    {
        SetActive(conversationPanel, false);
    }

    public void ShowAmbient(string message)
    {
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

    private void SetConversation(string prompt, string primary, string secondary)
    {
        HideAmbient();
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

        SetActive(conversationPanel, true);
    }

    private void HandlePrimaryChoice()
    {
        if (shopper == null)
        {
            return;
        }

        if (shopper.State == CS_PersonalShopperState.Following)
        {
            shopper.KeepHelping();
        }
        else
        {
            shopper.StartHelping();
        }
    }

    private void HandleSecondaryChoice()
    {
        if (shopper == null)
        {
            return;
        }

        if (shopper.State == CS_PersonalShopperState.Following)
        {
            shopper.StopHelping();
        }
        else
        {
            shopper.DeclineHelp();
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
