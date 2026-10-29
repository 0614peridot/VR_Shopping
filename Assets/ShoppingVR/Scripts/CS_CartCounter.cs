using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

public sealed class CS_CartCounter : MonoBehaviour
{
    public readonly struct CS_CartEntry
    {
        public CS_CartEntry(string productName, int priceWon)
        {
            ProductName = productName;
            PriceWon = priceWon;
        }

        public string ProductName { get; }
        public int PriceWon { get; }
    }

    private static readonly Color Ink = new Color(0.04f, 0.05f, 0.06f, 0.97f);
    private static readonly Color WarmWhite = new Color(0.92f, 0.89f, 0.83f, 1f);
    private static readonly Color Brass = new Color(0.83f, 0.67f, 0.39f, 1f);
    private static readonly Color Terracotta = new Color(0.67f, 0.30f, 0.20f, 1f);

    [SerializeField] private Text countText;
    [SerializeField] private GameObject badgeRoot;
    [SerializeField] private bool hideBadgeWhenEmpty;
    [Header("Cart Tab")]
    [SerializeField] private Button cartTabButton;
    [SerializeField] private GameObject cartPanel;
    [SerializeField] private RectTransform cartContent;
    [SerializeField] private Text emptyCartText;
    [SerializeField] private Text totalText;
    [SerializeField] private Button closeButton;

    private readonly List<CS_CartEntry> entries = new List<CS_CartEntry>();
    private ReadOnlyCollection<CS_CartEntry> readOnlyEntries;
    private Font uiFont;
    private int totalPriceWon;

    public int Count => entries.Count;
    public IReadOnlyList<CS_CartEntry> Entries => readOnlyEntries ??= entries.AsReadOnly();
    public int TotalPriceWon => totalPriceWon;
    public bool IsPanelOpen => cartPanel != null && cartPanel.activeSelf;
    public event Action Changed;

    private void Awake()
    {
        if (countText == null)
        {
            Debug.LogError($"{nameof(CS_CartCounter)} on '{name}' needs Count Text. The badge number will not be shown.", this);
        }

        uiFont = countText != null && countText.font != null
            ? countText.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureCartTabUI();
        SetPanelOpen(false);
        Refresh();
    }

    private void OnEnable()
    {
        if (cartTabButton != null)
        {
            cartTabButton.onClick.AddListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnDisable()
    {
        if (cartTabButton != null)
        {
            cartTabButton.onClick.RemoveListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        SetPanelOpen(false);
    }

    public void AddProduct(CS_ProductItem product)
    {
        if (product == null)
        {
            return;
        }

        entries.Add(new CS_CartEntry(product.ProductName, product.PriceWon));
        totalPriceWon += product.PriceWon;
        AddRow(entries[entries.Count - 1]);
        Refresh();
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (entries.Count == 0)
        {
            Refresh();
            return;
        }

        entries.Clear();
        totalPriceWon = 0;
        ClearRows();
        Refresh();
        Changed?.Invoke();
    }

    public void TogglePanel()
    {
        SetPanelOpen(!IsPanelOpen);
    }

    public void ClosePanel()
    {
        SetPanelOpen(false);
    }

    private void SetPanelOpen(bool open)
    {
        if (cartPanel != null)
        {
            cartPanel.SetActive(open);
        }
    }

    private void Refresh()
    {
        if (countText != null)
        {
            countText.text = Count.ToString();
        }

        if (badgeRoot != null)
        {
            badgeRoot.SetActive(!hideBadgeWhenEmpty || Count > 0);
        }

        if (emptyCartText != null)
        {
            emptyCartText.gameObject.SetActive(Count == 0);
        }

        if (totalText != null)
        {
            totalText.text = $"TOTAL   {FormatPrice(totalPriceWon)}";
        }
    }

    private void EnsureCartTabUI()
    {
        if (cartTabButton == null)
        {
            cartTabButton = GetComponent<Button>();
            if (cartTabButton == null)
            {
                cartTabButton = gameObject.AddComponent<Button>();
            }

            cartTabButton.targetGraphic = GetComponent<Graphic>();
        }

        if (cartPanel != null && cartContent != null)
        {
            return;
        }

        Transform canvasTransform = GetComponentInParent<Canvas>()?.transform;
        if (canvasTransform == null)
        {
            Debug.LogError($"{nameof(CS_CartCounter)} on '{name}' must be inside a Canvas to create the cart tab.", this);
            return;
        }

        BuildCartPanel(canvasTransform);
    }

    private void BuildCartPanel(Transform canvasTransform)
    {
        cartPanel = CreateBox("Cart Panel", canvasTransform, Ink);
        RectTransform panelRect = cartPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = new Vector2(-34f, -112f);
        panelRect.sizeDelta = new Vector2(460f, 540f);

        CreateText("Title", cartPanel.transform, "YOUR CART", 30, FontStyle.Bold, WarmWhite,
            TextAnchor.MiddleLeft, new Vector2(24f, -18f), new Vector2(-96f, 62f), new Vector2(0f, 1f), Vector2.one);

        GameObject closeObject = CreateBox("Close", cartPanel.transform, new Color(0.12f, 0.13f, 0.15f, 1f));
        SetRect(closeObject.GetComponent<RectTransform>(), new Vector2(-18f, -18f), new Vector2(54f, 46f), Vector2.one, Vector2.one);
        closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeObject.GetComponent<Image>();
        CreateText("Label", closeObject.transform, "X", 22, FontStyle.Bold, WarmWhite,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

        GameObject divider = CreateBox("Header Divider", cartPanel.transform, Terracotta);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = Vector2.one;
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -80f);
        dividerRect.sizeDelta = new Vector2(-48f, 2f);
        divider.GetComponent<Image>().raycastTarget = false;

        GameObject viewport = CreateBox("Viewport", cartPanel.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(24f, 74f);
        viewportRect.offsetMax = new Vector2(-24f, -94f);
        viewport.AddComponent<RectMask2D>();

        GameObject contentObject = new GameObject("Cart Items", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        cartContent = contentObject.GetComponent<RectTransform>();
        cartContent.anchorMin = new Vector2(0f, 1f);
        cartContent.anchorMax = Vector2.one;
        cartContent.pivot = new Vector2(0.5f, 1f);
        cartContent.anchoredPosition = Vector2.zero;
        cartContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = cartContent;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        emptyCartText = CreateText("Empty State", viewport.transform, "YOUR CART IS EMPTY", 20, FontStyle.Normal,
            new Color(WarmWhite.r, WarmWhite.g, WarmWhite.b, 0.62f), TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        emptyCartText.raycastTarget = false;

        totalText = CreateText("Total", cartPanel.transform, "TOTAL   $0.00", 24, FontStyle.Bold, Brass,
            TextAnchor.MiddleRight, new Vector2(-24f, 14f), new Vector2(-48f, 52f), Vector2.zero, Vector2.one);
        totalText.raycastTarget = false;
    }

    private void AddRow(CS_CartEntry entry)
    {
        if (cartContent == null)
        {
            return;
        }

        GameObject row = CreateBox($"Cart Item {entries.Count}", cartContent, new Color(0.12f, 0.13f, 0.15f, 0.96f));
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 68f;
        rowLayout.flexibleHeight = 0f;
        CreateText("Product Name", row.transform, entry.ProductName, 21, FontStyle.Normal, WarmWhite,
            TextAnchor.MiddleLeft, new Vector2(16f, 0f), new Vector2(-148f, 0f), Vector2.zero, Vector2.one);
        CreateText("Price", row.transform, FormatPrice(entry.PriceWon), 20, FontStyle.Bold, Brass,
            TextAnchor.MiddleRight, new Vector2(-16f, 0f), new Vector2(132f, 0f), new Vector2(1f, 0f), Vector2.one);
    }

    private void ClearRows()
    {
        if (cartContent == null)
        {
            return;
        }

        for (int i = cartContent.childCount - 1; i >= 0; i--)
        {
            GameObject row = cartContent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(row);
            }
            else
            {
                DestroyImmediate(row);
            }
        }
    }

    private GameObject CreateBox(string objectName, Transform parent, Color color)
    {
        GameObject box = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(parent, false);
        Image image = box.GetComponent<Image>();
        image.color = color;
        return box;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, FontStyle fontStyle,
        Color color, TextAnchor alignment, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
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
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.text = value;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static string FormatPrice(int priceWon)
    {
        return $"${priceWon / 1000m:N2}";
    }
}
