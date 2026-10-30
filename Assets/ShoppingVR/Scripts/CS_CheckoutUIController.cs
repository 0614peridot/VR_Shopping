using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CS_CheckoutUIController : MonoBehaviour
{
    private static readonly Color BubbleColor = new Color(0.04f, 0.05f, 0.06f, 0.97f);
    private static readonly Vector2 CheckoutButtonPosition = new Vector2(166f, 42f);
    private static readonly Vector2 CheckoutButtonSize = new Vector2(290f, 72f);
    private static readonly Vector2 TotalPosition = new Vector2(166f, 126f);
    private static readonly Vector2 TotalSize = new Vector2(290f, 40f);
    private static readonly Vector2 ReviewViewportBottomLeft = new Vector2(32f, 178f);
    private static readonly Vector2 ReviewViewportTopRight = new Vector2(-32f, -108f);
    private static readonly Color RowColor = new Color(0.12f, 0.13f, 0.15f, 0.96f);
    private static readonly Color TextColor = new Color(0.92f, 0.89f, 0.83f, 1f);
    private static readonly Color PriceColor = new Color(0.83f, 0.67f, 0.39f, 1f);

    [SerializeField] private CS_CartCounter cartCounter;
    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptText;
    [SerializeField] private Button promptButton;
    [Header("Review")]
    [SerializeField] private GameObject reviewRoot;
    [SerializeField] private RectTransform itemContent;
    [SerializeField] private Text emptyCartText;
    [SerializeField] private Text totalText;
    [SerializeField] private Button shopMoreButton;
    [SerializeField] private Text shopMoreText;
    [SerializeField] private Button checkoutButton;
    [SerializeField] private Text checkoutText;
    [Header("Confirmation")]
    [SerializeField] private GameObject thankYouRoot;
    [SerializeField] private Text thankYouText;
    [SerializeField, Min(0.1f)] private float thankYouDuration = 3f;
    [Header("Spatial Speech")]
    [SerializeField, Min(0.25f)] private float speechBubbleHeight = 2.15f;
    [SerializeField] private float speechBubbleSideOffset = 0.35f;
    [SerializeField, Min(0.0001f)] private float worldCanvasScale = 0.0015f;

    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private CS_CartCounter subscribedCart;
    private Coroutine thankYouRoutine;
    private Font uiFont;
    private bool playerInRange;
    private Transform speaker;
    private Camera viewCamera;
    private Canvas spatialCanvas;
    private RectTransform spatialCanvasRect;

    public bool IsReviewOpen => reviewRoot != null && reviewRoot.activeSelf;
    public bool IsThankYouVisible => thankYouRoot != null && thankYouRoot.activeSelf;
    public bool IsPromptVisible => promptRoot != null && promptRoot.activeSelf;

    private void Awake()
    {
        uiFont = ResolveFont();
        EnsureSpatialSpeechCanvas();
        ApplyReviewLayout();
        ApplyEnglishLabels();
        SetActive(promptRoot, false);
        SetActive(reviewRoot, false);
        SetActive(thankYouRoot, false);
    }

    private void LateUpdate()
    {
        UpdateSpatialCanvasTransform();
    }

    private void OnEnable()
    {
        AddButtonListeners();
        SubscribeToCart();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
        UnsubscribeFromCart();
        if (thankYouRoutine != null)
        {
            StopCoroutine(thankYouRoutine);
            thankYouRoutine = null;
        }

        SetActive(promptRoot, false);
        SetActive(reviewRoot, false);
        SetActive(thankYouRoot, false);
    }

    public void Configure(CS_CartCounter cart)
    {
        if (cartCounter == cart)
        {
            return;
        }

        UnsubscribeFromCart();
        cartCounter = cart;
        SubscribeToCart();
        RefreshReview();
    }

    public void BindSpeaker(Transform speakerTransform, Camera camera)
    {
        speaker = speakerTransform;
        viewCamera = camera != null ? camera : Camera.main;
        EnsureSpatialSpeechCanvas();
        UpdateSpatialCanvasTransform();
    }

    public void ConfigureUI(GameObject prompt, Text promptLabel, Button promptAction, GameObject review,
        RectTransform content, Text emptyLabel, Text totalLabel, Button shopMoreAction, Text shopMoreLabel,
        Button checkoutAction, Text checkoutLabel, GameObject confirmation, Text confirmationLabel)
    {
        RemoveButtonListeners();
        promptRoot = prompt;
        promptText = promptLabel;
        promptButton = promptAction;
        reviewRoot = review;
        itemContent = content;
        emptyCartText = emptyLabel;
        totalText = totalLabel;
        shopMoreButton = shopMoreAction;
        shopMoreText = shopMoreLabel;
        checkoutButton = checkoutAction;
        checkoutText = checkoutLabel;
        thankYouRoot = confirmation;
        thankYouText = confirmationLabel;
        uiFont = ResolveFont();
        EnsureSpatialSpeechCanvas();
        ApplyReviewLayout();
        ApplyEnglishLabels();
        SetActive(promptRoot, false);
        SetActive(reviewRoot, false);
        SetActive(thankYouRoot, false);
        if (isActiveAndEnabled)
        {
            AddButtonListeners();
        }

        RefreshReview();
    }

    private void EnsureSpatialSpeechCanvas()
    {
        if (spatialCanvas == null)
        {
            GameObject canvasObject = new GameObject("Checkout Clerk Spatial Speech", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster));
            spatialCanvasRect = canvasObject.GetComponent<RectTransform>();
            spatialCanvasRect.sizeDelta = new Vector2(700f, 360f);
            spatialCanvasRect.localScale = Vector3.one * worldCanvasScale;
            spatialCanvas = canvasObject.GetComponent<Canvas>();
            spatialCanvas.renderMode = RenderMode.WorldSpace;
            spatialCanvas.sortingOrder = 49;
        }

        spatialCanvas.worldCamera = viewCamera != null ? viewCamera : Camera.main;
        MoveSpeechElement(promptRoot, new Vector2(0f, 30f));
        MoveSpeechElement(thankYouRoot, Vector2.zero);
        EnsureSpeechTail(promptRoot);
        EnsureSpeechTail(thankYouRoot);
    }

    private void MoveSpeechElement(GameObject target, Vector2 position)
    {
        if (target == null || spatialCanvasRect == null)
        {
            return;
        }

        RectTransform rect = target.GetComponent<RectTransform>();
        if (target.transform.parent != spatialCanvasRect)
        {
            target.transform.SetParent(spatialCanvasRect, false);
        }

        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private void UpdateSpatialCanvasTransform()
    {
        if (spatialCanvasRect == null || speaker == null)
        {
            return;
        }

        Camera camera = viewCamera != null ? viewCamera : Camera.main;
        Vector3 side = camera != null
            ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized
            : Vector3.right;
        spatialCanvasRect.position = speaker.position + Vector3.up * speechBubbleHeight +
                                     side * speechBubbleSideOffset;
        spatialCanvasRect.localScale = Vector3.one * worldCanvasScale;

        if (camera != null)
        {
            spatialCanvas.worldCamera = camera;
            Vector3 awayFromCamera = spatialCanvasRect.position - camera.transform.position;
            if (awayFromCamera.sqrMagnitude > 0.0001f)
            {
                spatialCanvasRect.rotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
            }
        }
    }

    private static void EnsureSpeechTail(GameObject bubble)
    {
        if (bubble == null || bubble.transform.Find("Speech Tail") != null)
        {
            return;
        }

        GameObject tail = new GameObject("Speech Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tail.transform.SetParent(bubble.transform, false);
        RectTransform rect = tail.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -14f);
        rect.sizeDelta = new Vector2(28f, 28f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image image = tail.GetComponent<Image>();
        image.color = BubbleColor;
        image.raycastTarget = false;
    }

    private void ApplyReviewLayout()
    {
        RectTransform checkoutRect = checkoutButton == null ? null : checkoutButton.GetComponent<RectTransform>();
        if (checkoutRect != null)
        {
            checkoutRect.anchorMin = new Vector2(0.5f, 0f);
            checkoutRect.anchorMax = new Vector2(0.5f, 0f);
            checkoutRect.pivot = new Vector2(0.5f, 0f);
            checkoutRect.anchoredPosition = CheckoutButtonPosition;
            checkoutRect.sizeDelta = CheckoutButtonSize;
        }

        RectTransform totalRect = totalText == null ? null : totalText.rectTransform;
        if (totalRect != null)
        {
            totalRect.anchorMin = new Vector2(0.5f, 0f);
            totalRect.anchorMax = new Vector2(0.5f, 0f);
            totalRect.pivot = new Vector2(0.5f, 0f);
            totalRect.anchoredPosition = TotalPosition;
            totalRect.sizeDelta = TotalSize;
            totalText.alignment = TextAnchor.MiddleRight;
        }

        RectTransform viewportRect = itemContent == null || itemContent.parent == null
            ? null
            : itemContent.parent as RectTransform;
        if (viewportRect != null)
        {
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = ReviewViewportBottomLeft;
            viewportRect.offsetMax = ReviewViewportTopRight;
        }
    }

    public void SetPlayerInRange(bool inRange)
    {
        playerInRange = inRange;
        UpdatePromptVisibility();
    }

    public void OpenReview()
    {
        if (!playerInRange || cartCounter == null || IsReviewOpen)
        {
            return;
        }

        if (thankYouRoutine != null)
        {
            StopCoroutine(thankYouRoutine);
            thankYouRoutine = null;
        }

        SetActive(thankYouRoot, false);
        SetActive(promptRoot, false);
        SetActive(reviewRoot, true);
        RefreshReview();
    }

    public void ShopMore()
    {
        SetActive(reviewRoot, false);
        UpdatePromptVisibility();
    }

    public void CheckOut()
    {
        if (!IsReviewOpen || cartCounter == null)
        {
            return;
        }

        cartCounter.Clear();
        SetActive(reviewRoot, false);
        SetActive(promptRoot, false);
        SetActive(thankYouRoot, true);

        if (thankYouRoutine != null)
        {
            StopCoroutine(thankYouRoutine);
        }

        thankYouRoutine = StartCoroutine(HideThankYouAfterDelay());
    }

    private IEnumerator HideThankYouAfterDelay()
    {
        yield return new WaitForSeconds(thankYouDuration);
        thankYouRoutine = null;
        SetActive(thankYouRoot, false);
        UpdatePromptVisibility();
    }

    private void ApplyEnglishLabels()
    {
        SetText(promptText, "Check out?");
        SetText(emptyCartText, "Your cart is empty");
        SetText(shopMoreText, "SHOP MORE");
        SetText(checkoutText, "CHECK OUT");
        SetText(thankYouText, "Thanks for shopping!");
    }

    private void RefreshReview()
    {
        ClearGeneratedRows();
        if (cartCounter == null)
        {
            SetText(totalText, "TOTAL   $0.00");
            SetActive(emptyCartText == null ? null : emptyCartText.gameObject, true);
            return;
        }

        IReadOnlyList<CS_CartCounter.CS_CartEntry> entries = cartCounter.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            AddRow(entries[i], i);
        }

        SetActive(emptyCartText == null ? null : emptyCartText.gameObject, entries.Count == 0);
        SetText(totalText, $"TOTAL   {FormatPrice(cartCounter.TotalPriceWon)}");
    }

    private void AddRow(CS_CartCounter.CS_CartEntry entry, int index)
    {
        if (itemContent == null)
        {
            return;
        }

        GameObject row = new GameObject($"Checkout Item {index + 1}", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(itemContent, false);
        row.GetComponent<Image>().color = RowColor;
        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.preferredHeight = 64f;
        layout.flexibleHeight = 0f;
        generatedRows.Add(row);

        CreateText("Product Name", row.transform, entry.ProductName, TextAnchor.MiddleLeft,
            new Vector2(16f, 0f), new Vector2(-144f, 0f), Vector2.zero, Vector2.one, TextColor);
        CreateText("Price", row.transform, FormatPrice(entry.PriceWon), TextAnchor.MiddleRight,
            new Vector2(-16f, 0f), new Vector2(128f, 0f), new Vector2(1f, 0f), Vector2.one, PriceColor);
    }

    private void CreateText(string objectName, Transform parent, string value, TextAnchor alignment,
        Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = 20;
        text.color = color;
        text.alignment = alignment;
        text.text = value;
        text.raycastTarget = false;
    }

    private void ClearGeneratedRows()
    {
        for (int i = generatedRows.Count - 1; i >= 0; i--)
        {
            GameObject row = generatedRows[i];
            if (row == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                row.SetActive(false);
                Destroy(row);
            }
            else
            {
                DestroyImmediate(row);
            }
        }

        generatedRows.Clear();
    }

    private void UpdatePromptVisibility()
    {
        SetActive(promptRoot, playerInRange && !IsReviewOpen && !IsThankYouVisible);
    }

    private void HandleCartChanged()
    {
        if (IsReviewOpen)
        {
            RefreshReview();
        }
    }

    private void SubscribeToCart()
    {
        if (!isActiveAndEnabled || cartCounter == null || subscribedCart == cartCounter)
        {
            return;
        }

        subscribedCart = cartCounter;
        subscribedCart.Changed += HandleCartChanged;
    }

    private void UnsubscribeFromCart()
    {
        if (subscribedCart == null)
        {
            return;
        }

        subscribedCart.Changed -= HandleCartChanged;
        subscribedCart = null;
    }

    private void AddButtonListeners()
    {
        if (promptButton != null)
        {
            promptButton.onClick.AddListener(OpenReview);
        }

        if (shopMoreButton != null)
        {
            shopMoreButton.onClick.AddListener(ShopMore);
        }

        if (checkoutButton != null)
        {
            checkoutButton.onClick.AddListener(CheckOut);
        }
    }

    private void RemoveButtonListeners()
    {
        if (promptButton != null)
        {
            promptButton.onClick.RemoveListener(OpenReview);
        }

        if (shopMoreButton != null)
        {
            shopMoreButton.onClick.RemoveListener(ShopMore);
        }

        if (checkoutButton != null)
        {
            checkoutButton.onClick.RemoveListener(CheckOut);
        }
    }

    private Font ResolveFont()
    {
        if (promptText != null && promptText.font != null)
        {
            return promptText.font;
        }

        if (totalText != null && totalText.font != null)
        {
            return totalText.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static string FormatPrice(int priceWon)
    {
        return $"${priceWon / 1000m:N2}";
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
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
