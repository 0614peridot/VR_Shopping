using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class CS_ProductFocusController : MonoBehaviour
{
    [SerializeField] private Camera userCamera;
    [SerializeField] private Transform focusAnchor;
    [SerializeField] private LayerMask productLayerMask = ~0;
    [SerializeField, Min(0.5f)] private float maxSelectionDistance = 20f;
    [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.25f;
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference cancelAction;
    [SerializeField] private CS_PlayerLocomotion locomotion;
    [SerializeField] private CS_FocusUIController focusUI;
    [SerializeField] private CS_CartCounter cartCounter;

    private CS_ShoppingMode mode = CS_ShoppingMode.Explore;
    private CS_ProductItem currentProduct;
    private bool dragging;
    private bool pressRequested;
    private bool releaseRequested;
    private bool clickPressed;
    private Vector2 previousPointerPosition;
    private bool previousMovementState = true;
    private bool hasRequiredReferences;
    private bool enabledPointAction;
    private bool enabledClickAction;
    private bool enabledCancelAction;
    private Collider hoveredCollider;
    private CS_ProductItem hoveredProduct;
    private Texture2D selectableCursorTexture;
    private bool selectableCursorVisible;

    public CS_ShoppingMode Mode => mode;
    public CS_ProductItem CurrentProduct => currentProduct;

    public void Configure(Camera camera, Transform anchor, CS_PlayerLocomotion playerLocomotion,
        CS_FocusUIController ui, CS_CartCounter cart, LayerMask layerMask)
    {
        userCamera = camera;
        focusAnchor = anchor;
        locomotion = playerLocomotion;
        focusUI = ui;
        cartCounter = cart;
        productLayerMask = layerMask;
    }

    private void Awake()
    {
        hasRequiredReferences = userCamera != null && focusAnchor != null && pointAction != null &&
                                clickAction != null && cancelAction != null && locomotion != null &&
                                focusUI != null && cartCounter != null;
        if (!hasRequiredReferences)
        {
            Debug.LogError($"{nameof(CS_ProductFocusController)} on '{name}' is missing required references " +
                           "(Camera, Focus Anchor, Point/Click/Cancel actions, Locomotion, Focus UI, Cart Counter). Product focus is disabled.", this);
        }
    }

    private void OnEnable()
    {
        if (!hasRequiredReferences)
        {
            return;
        }

        enabledPointAction = EnableIfNeeded(pointAction);
        enabledClickAction = EnableIfNeeded(clickAction);
        enabledCancelAction = EnableIfNeeded(cancelAction);
        clickAction.action.performed += HandleClick;
        clickAction.action.canceled += HandleClickCanceled;
        cancelAction.action.performed += HandleCancel;
    }

    private void OnDisable()
    {
        if (hasRequiredReferences)
        {
            clickAction.action.performed -= HandleClick;
            clickAction.action.canceled -= HandleClickCanceled;
            cancelAction.action.performed -= HandleCancel;
            DisableIfEnabledHere(pointAction, enabledPointAction);
            DisableIfEnabledHere(clickAction, enabledClickAction);
            DisableIfEnabledHere(cancelAction, enabledCancelAction);
        }

        enabledPointAction = false;
        enabledClickAction = false;
        enabledCancelAction = false;
        pressRequested = false;
        releaseRequested = false;
        clickPressed = false;
        ClearHoverFeedback();
        ExitFocus();

        if (selectableCursorTexture != null)
        {
            Destroy(selectableCursorTexture);
            selectableCursorTexture = null;
        }
    }

    private void Update()
    {
        if (!hasRequiredReferences)
        {
            return;
        }

        if (mode == CS_ShoppingMode.Focused && (currentProduct == null || !currentProduct.isActiveAndEnabled))
        {
            ExitFocus();
        }

        Vector2 pointerPosition = pointAction.action.ReadValue<Vector2>();
        UpdateHoverFeedback(pointerPosition);

        if (releaseRequested)
        {
            releaseRequested = false;
            dragging = false;
        }

        if (pressRequested)
        {
            pressRequested = false;
            HandlePress(pointerPosition);
        }

        if (!dragging || currentProduct == null)
        {
            return;
        }

        Vector2 delta = pointerPosition - previousPointerPosition;
        previousPointerPosition = pointerPosition;
        currentProduct.FocusTransform.Rotate(Vector3.up, -delta.x * rotationSensitivity, Space.World);
        currentProduct.FocusTransform.Rotate(userCamera.transform.right, delta.y * rotationSensitivity, Space.World);
    }

    public void AddCurrentProductToCart()
    {
        if (mode == CS_ShoppingMode.Focused && currentProduct != null && cartCounter != null)
        {
            cartCounter.AddProduct(currentProduct);
        }
    }

    // Single exit path: Esc, Exit button, target change, disabled target and disabled controller.
    public void ExitFocus()
    {
        bool wasFocused = mode == CS_ShoppingMode.Focused || currentProduct != null;
        dragging = false;

        if (currentProduct != null)
        {
            currentProduct.RestoreDisplayState();
        }

        if (focusUI != null)
        {
            focusUI.Hide();
        }

        currentProduct = null;

        if (wasFocused && locomotion != null)
        {
            locomotion.SetMovementEnabled(previousMovementState);
        }

        mode = CS_ShoppingMode.Explore;
    }

    private void EnterFocus(CS_ProductItem product)
    {
        ExitFocus();
        ClearHoverFeedback();
        if (product == null)
        {
            return;
        }

        currentProduct = product;
        currentProduct.BeginFocus(focusAnchor);
        focusUI.Show(currentProduct);
        previousMovementState = locomotion.MovementEnabled;
        locomotion.SetMovementEnabled(false);
        mode = CS_ShoppingMode.Focused;
    }

    private void HandlePress(Vector2 pointerPosition)
    {
        if (IsPointerOverUI())
        {
            return;
        }

        CS_ProductItem product = RaycastProduct(pointerPosition);
        if (product == null)
        {
            return;
        }

        if (product != currentProduct)
        {
            EnterFocus(product);
            return;
        }

        dragging = clickPressed;
        previousPointerPosition = pointerPosition;
    }

    private CS_ProductItem RaycastProduct(Vector2 pointerPosition)
    {
        Ray ray = userCamera.ScreenPointToRay(pointerPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, productLayerMask,
                QueryTriggerInteraction.Collide))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<CS_ProductItem>();
    }

    private void UpdateHoverFeedback(Vector2 pointerPosition)
    {
        if (mode != CS_ShoppingMode.Explore || IsPointerOverUI())
        {
            ClearHoverFeedback();
            return;
        }

        Ray ray = userCamera.ScreenPointToRay(pointerPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, productLayerMask,
                QueryTriggerInteraction.Collide))
        {
            ClearHoverFeedback();
            return;
        }

        // Product lookup only runs when the hit collider changes, rather than every frame.
        if (hoveredCollider != hit.collider)
        {
            hoveredCollider = hit.collider;
            hoveredProduct = hoveredCollider.GetComponentInParent<CS_ProductItem>();
        }

        SetSelectableCursor(hoveredProduct != null && hoveredProduct.isActiveAndEnabled);
    }

    private void ClearHoverFeedback()
    {
        hoveredCollider = null;
        hoveredProduct = null;
        SetSelectableCursor(false);
    }

    private void SetSelectableCursor(bool visible)
    {
        if (selectableCursorVisible == visible)
        {
            return;
        }

        selectableCursorVisible = visible;
        if (!visible)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        if (selectableCursorTexture == null)
        {
            selectableCursorTexture = CreateSelectableCursorTexture();
        }

        Vector2 hotspot = new Vector2(1f, 1f);
        Cursor.SetCursor(selectableCursorTexture, hotspot, CursorMode.Auto);
    }

    private static Texture2D CreateSelectableCursorTexture()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ShoppingVR Selectable Cursor",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        bool[] arrowMask = new bool[size * size];
        Vector2[] arrowVertices =
        {
            new Vector2(1f, 1f),
            new Vector2(1f, 24f),
            new Vector2(7f, 18f),
            new Vector2(12f, 29f),
            new Vector2(17f, 27f),
            new Vector2(12f, 16f),
            new Vector2(24f, 16f)
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                arrowMask[y * size + x] = IsPointInsidePolygon(new Vector2(x + 0.5f, y + 0.5f), arrowVertices);
            }
        }

        Color orange = new Color(1f, 106f / 255f, 0f, 1f);
        Color outline = new Color(32f / 255f, 33f / 255f, 36f / 255f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                if (!arrowMask[index])
                {
                    pixels[(size - 1 - y) * size + x] = Color.clear;
                    continue;
                }

                bool isOutline = x == 0 || x == size - 1 || y == 0 || y == size - 1 ||
                                 !arrowMask[y * size + x - 1] || !arrowMask[y * size + x + 1] ||
                                 !arrowMask[(y - 1) * size + x] || !arrowMask[(y + 1) * size + x];
                pixels[(size - 1 - y) * size + x] = isOutline ? outline : orange;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static bool IsPointInsidePolygon(Vector2 point, Vector2[] vertices)
    {
        bool inside = false;
        for (int current = 0, previous = vertices.Length - 1; current < vertices.Length; previous = current++)
        {
            Vector2 currentVertex = vertices[current];
            Vector2 previousVertex = vertices[previous];
            bool crossesScanline = currentVertex.y > point.y != previousVertex.y > point.y;
            if (crossesScanline &&
                point.x < (previousVertex.x - currentVertex.x) * (point.y - currentVertex.y) /
                          (previousVertex.y - currentVertex.y) + currentVertex.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    // UI/Click is a PassThrough action, so presses and releases both arrive as performed.
    // EventSystem queries are unreliable inside input callbacks, so the work is deferred to Update.
    private void HandleClick(InputAction.CallbackContext context)
    {
        bool pressed = context.ReadValueAsButton();
        if (pressed && !clickPressed)
        {
            pressRequested = true;
        }
        else if (!pressed && clickPressed)
        {
            releaseRequested = true;
        }

        clickPressed = pressed;
    }

    private void HandleClickCanceled(InputAction.CallbackContext context)
    {
        clickPressed = false;
        releaseRequested = true;
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        if (mode == CS_ShoppingMode.Focused)
        {
            ExitFocus();
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool EnableIfNeeded(InputActionReference reference)
    {
        if (reference.action == null || reference.action.enabled)
        {
            return false;
        }

        reference.action.Enable();
        return true;
    }

    private static void DisableIfEnabledHere(InputActionReference reference, bool enabledHere)
    {
        if (enabledHere && reference.action != null)
        {
            reference.action.Disable();
        }
    }
}
