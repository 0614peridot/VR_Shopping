using UnityEngine;
using UnityEngine.UI;

public sealed class CS_FocusUIController : MonoBehaviour
{
    [SerializeField] private GameObject focusPanel;
    [SerializeField] private Text productNameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button addToCartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private CS_ProductFocusController focusController;

    private void Awake()
    {
        if (focusPanel == null || addToCartButton == null || exitButton == null || focusController == null)
        {
            Debug.LogError($"{nameof(CS_FocusUIController)} on '{name}' is missing Focus Panel, buttons or Focus Controller.", this);
        }

        ConfigureActionLayout();
        Hide();
    }

    private void OnEnable()
    {
        if (addToCartButton != null)
        {
            addToCartButton.onClick.AddListener(HandleAddToCart);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(HandleExit);
        }
    }

    private void OnDisable()
    {
        if (addToCartButton != null)
        {
            addToCartButton.onClick.RemoveListener(HandleAddToCart);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(HandleExit);
        }
    }

    public void Configure(CS_ProductFocusController controller)
    {
        focusController = controller;
    }

    public void Show(CS_ProductItem product)
    {
        if (product == null)
        {
            Hide();
            return;
        }

        if (productNameText != null)
        {
            productNameText.text = product.ProductName;
        }

        if (priceText != null)
        {
            priceText.text = $"${product.PriceWon / 1000m:N2}";
        }

        if (focusPanel != null)
        {
            focusPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (focusPanel != null)
        {
            focusPanel.SetActive(false);
        }
    }

    private void HandleAddToCart()
    {
        if (focusController != null && focusController.CurrentProduct != null)
        {
            focusController.AddCurrentProductToCart();
        }
    }

    private void HandleExit()
    {
        if (focusController != null)
        {
            focusController.ExitFocus();
        }
    }

    private void ConfigureActionLayout()
    {
        if (exitButton != null)
        {
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.5f, 0f);
            exitRect.anchorMax = new Vector2(0.5f, 0f);
            exitRect.pivot = new Vector2(0.5f, 0.5f);
            exitRect.anchoredPosition = new Vector2(-188f, 60f);
            exitRect.sizeDelta = new Vector2(250f, 72f);
        }

        if (addToCartButton == null)
        {
            return;
        }

        RectTransform cartRect = addToCartButton.GetComponent<RectTransform>();
        cartRect.anchorMin = new Vector2(0.5f, 0f);
        cartRect.anchorMax = new Vector2(0.5f, 0f);
        cartRect.pivot = new Vector2(0.5f, 0.5f);
        cartRect.anchoredPosition = new Vector2(133f, 60f);
        cartRect.sizeDelta = new Vector2(360f, 72f);

        Transform label = addToCartButton.transform.Find("Label");
        if (label != null && label.TryGetComponent(out RectTransform labelRect))
        {
            labelRect.anchoredPosition = new Vector2(24f, 0f);
            labelRect.sizeDelta = new Vector2(-64f, 0f);
        }

        if (addToCartButton.transform.Find("Cart Icon") == null)
        {
            CreateCartIcon(addToCartButton.transform);
        }
    }

    private static void CreateCartIcon(Transform parent)
    {
        GameObject icon = new GameObject("Cart Icon", typeof(RectTransform));
        icon.transform.SetParent(parent, false);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-105f, 0f);
        iconRect.sizeDelta = new Vector2(36f, 32f);

        CreateCartIconPart("Handle", icon.transform, new Vector2(-14f, 10f), new Vector2(10f, 4f), 0f);
        CreateCartIconPart("Arm", icon.transform, new Vector2(-8.5f, 4.5f), new Vector2(4f, 15f), -22f);
        CreateCartIconPart("Basket Top", icon.transform, new Vector2(3f, 5f), new Vector2(24f, 4f), 0f);
        CreateCartIconPart("Basket Base", icon.transform, new Vector2(4f, -5f), new Vector2(21f, 4f), 0f);
        CreateCartIconPart("Basket Left", icon.transform, new Vector2(-7f, 0f), new Vector2(4f, 12f), -8f);
        CreateCartIconPart("Basket Right", icon.transform, new Vector2(15f, 0f), new Vector2(4f, 12f), 8f);
        CreateCartIconPart("Wheel Left", icon.transform, new Vector2(-2f, -12f), new Vector2(6f, 6f), 45f);
        CreateCartIconPart("Wheel Right", icon.transform, new Vector2(13f, -12f), new Vector2(6f, 6f), 45f);
    }

    private static void CreateCartIconPart(string name, Transform parent, Vector2 position, Vector2 size, float rotation)
    {
        GameObject part = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        part.transform.SetParent(parent, false);
        RectTransform rect = part.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        Image image = part.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }
}
