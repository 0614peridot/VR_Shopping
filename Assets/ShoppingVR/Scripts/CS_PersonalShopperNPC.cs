using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public sealed class CS_PersonalShopperNPC : MonoBehaviour
{
    private static readonly string[] AmbientMessages =
    {
        "Let me know if you need help!",
        "Welcome to the shop!"
    };

    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera userCamera;
    [SerializeField] private Collider[] interactionColliders;
    [SerializeField] private LayerMask interactionLayerMask = ~0;
    [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;

    [Header("Following")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2.4f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 8f;
    [SerializeField, Min(0.5f)] private float followDistance = 1.8f;
    [SerializeField, Range(0.55f, 0.9f)] private float followViewportX = 0.76f;
    [SerializeField, Min(0.5f)] private float minimumPlayerSeparation = 0.9f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.08f;

    [Header("Entrance Greeting")]
    [SerializeField, Min(0.5f)] private float greetingDistance = 2.2f;
    [SerializeField, Range(0.55f, 0.9f)] private float greetingViewportX = 0.68f;

    [Header("Guide Mode")]
    [SerializeField, Range(1, 10)] private int guideItemCount = 3;
    [SerializeField, Min(0.5f)] private float guideProductDistance = 1.4f;

    [Header("Speech")]
    [SerializeField, Min(1f)] private float ambientInterval = 30f;
    [SerializeField, Min(0.5f)] private float ambientDuration = 4f;
    [SerializeField] private CS_PersonalShopperUI shopperUI;
    [SerializeField] private CS_ProductFocusController productFocusController;

    private CS_PersonalShopperState state;
    private CS_PersonalShopperMode mode;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private CS_PlayerLocomotion playerLocomotion;
    private readonly List<CS_ProductItem> guideProducts = new List<CS_ProductItem>();
    private CS_ProductItem commentedProduct;
    private int guideProductIndex;
    private bool isStoppedAtGuideProduct;
    private bool entranceGreetingStarted;
    private bool entranceGreetingCompleted;
    private bool entranceConversationShown;
    private bool movementWasEnabledBeforeGreeting = true;
    private float ambientTimer;
    private float ambientVisibleTimer;
    private bool pressRequested;
    private bool clickPressed;
    private bool enabledPointAction;
    private bool enabledClickAction;
    private bool talkCursorVisible;
    private Texture2D talkCursorTexture;

    public CS_PersonalShopperState State => state;
    public CS_PersonalShopperMode Mode => mode;
    public bool IsConversationOpen => shopperUI != null && shopperUI.IsConversationOpen;
    public bool HasCompletedEntranceGreeting => entranceGreetingCompleted;
    public bool IsWaitingAtGuideItem => mode == CS_PersonalShopperMode.Guide && isStoppedAtGuideProduct;
    public Vector3 HomePosition => homePosition;
    public Camera UserCamera => userCamera;

    public void Configure(Transform playerTransform, Camera camera, CS_PersonalShopperUI ui,
        CS_ProductFocusController focusController, InputActionReference point, InputActionReference click,
        Collider[] colliders, LayerMask layerMask)
    {
        player = playerTransform;
        userCamera = camera;
        shopperUI = ui;
        productFocusController = focusController;
        pointAction = point;
        clickAction = click;
        interactionColliders = colliders;
        interactionLayerMask = layerMask;
        shopperUI?.BindShopper(this);
    }

    private void Awake()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        state = CS_PersonalShopperState.Waiting;
        mode = CS_PersonalShopperMode.None;
        ambientTimer = ambientInterval;

        if (interactionColliders == null || interactionColliders.Length == 0)
        {
            interactionColliders = GetComponentsInChildren<Collider>(true);
        }
    }

    private void Start()
    {
        if (player == null || userCamera == null || pointAction == null || clickAction == null || shopperUI == null)
        {
            Debug.LogError($"{nameof(CS_PersonalShopperNPC)} on '{name}' needs Player, Camera, Point/Click actions and Shopper UI.", this);
            enabled = false;
        }

        playerLocomotion = player != null ? player.GetComponent<CS_PlayerLocomotion>() : null;
        shopperUI?.BindShopper(this);
    }

    private void OnEnable()
    {
        enabledPointAction = EnableIfNeeded(pointAction);
        enabledClickAction = EnableIfNeeded(clickAction);
        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.performed += HandleClick;
            clickAction.action.canceled += HandleClickCanceled;
        }
    }

    private void OnDisable()
    {
        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.performed -= HandleClick;
            clickAction.action.canceled -= HandleClickCanceled;
        }

        DisableIfEnabledHere(pointAction, enabledPointAction);
        DisableIfEnabledHere(clickAction, enabledClickAction);
        enabledPointAction = false;
        enabledClickAction = false;
        pressRequested = false;
        clickPressed = false;
        ClearTalkCursor();

        if (shopperUI != null)
        {
            shopperUI.HideAll();
        }

        if (entranceGreetingStarted && !entranceGreetingCompleted && playerLocomotion != null)
        {
            playerLocomotion.SetMovementEnabled(movementWasEnabledBeforeGreeting);
        }
    }

    private void OnDestroy()
    {
        if (talkCursorTexture != null)
        {
            Destroy(talkCursorTexture);
        }
    }

    private void Update()
    {
        UpdateMovement();
        UpdateSpeech();
        UpdateProductComment();

        bool canTalk = CanTalkAtPointer();
        SetTalkCursor(canTalk);

        if (pressRequested)
        {
            pressRequested = false;
            if (canTalk)
            {
                OpenConversation();
            }
        }
    }

    public void OpenConversation()
    {
        if (shopperUI == null || player == null || !IsPlayerNear())
        {
            return;
        }

        shopperUI.HideProductComment();
        if (!entranceGreetingCompleted)
        {
            if (state == CS_PersonalShopperState.Approaching)
            {
                shopperUI.ShowInitialConversation();
                entranceConversationShown = true;
            }

            return;
        }

        if (mode != CS_PersonalShopperMode.None)
        {
            shopperUI.ShowFollowingConversation();
        }
        else
        {
            shopperUI.ShowInitialConversation();
        }
    }

    public void BeginEntranceGreeting()
    {
        if (entranceGreetingStarted || entranceGreetingCompleted || player == null || shopperUI == null)
        {
            return;
        }

        entranceGreetingStarted = true;
        entranceConversationShown = false;
        mode = CS_PersonalShopperMode.None;
        state = CS_PersonalShopperState.Approaching;
        shopperUI.HideAll();
        if (playerLocomotion != null)
        {
            movementWasEnabledBeforeGreeting = playerLocomotion.MovementEnabled;
            playerLocomotion.SetMovementEnabled(false);
        }
    }

    public void ChooseBrowseAlone()
    {
        CompleteEntranceGreetingIfNeeded();
        mode = CS_PersonalShopperMode.None;
        state = CS_PersonalShopperState.Returning;
        commentedProduct = null;
        shopperUI?.HideConversation();
        ShowTimedAmbient("Of course. Whenever you need help, I'll be right here.");
    }

    public void ChooseAdviceMode()
    {
        CompleteEntranceGreetingIfNeeded();
        mode = CS_PersonalShopperMode.Advice;
        state = CS_PersonalShopperState.Following;
        commentedProduct = null;
        shopperUI?.HideConversation();
        ShowTimedAmbient("By all means, ask me anything.");
    }

    public void ChooseGuideMode()
    {
        CompleteEntranceGreetingIfNeeded();
        mode = CS_PersonalShopperMode.Guide;
        commentedProduct = null;
        BuildGuideProductList();
        shopperUI?.HideConversation();

        if (guideProducts.Count == 0)
        {
            mode = CS_PersonalShopperMode.None;
            state = CS_PersonalShopperState.Returning;
            ShowTimedAmbient("I couldn't find a collection to show right now, darling.");
            return;
        }

        state = CS_PersonalShopperState.Guiding;
        ShowTimedAmbient("Of course. Follow me, darling.");
    }

    public void StopCurrentMode()
    {
        CompleteEntranceGreetingIfNeeded();
        mode = CS_PersonalShopperMode.None;
        state = CS_PersonalShopperState.Returning;
        commentedProduct = null;
        guideProducts.Clear();
        isStoppedAtGuideProduct = false;
        shopperUI?.HideConversation();
        shopperUI?.HideProductComment();
        ShowTimedAmbient("Of course, my love. I'll give you some space.");
    }

    public void ContinueCurrentMode()
    {
        shopperUI?.HideConversation();
    }

    public void ContinueGuide()
    {
        if (mode != CS_PersonalShopperMode.Guide || !isStoppedAtGuideProduct)
        {
            return;
        }

        shopperUI?.HideConversation();
        AdvanceGuide();
    }

    public void StartHelping()
    {
        ChooseAdviceMode();
    }

    public void KeepHelping()
    {
        ContinueCurrentMode();
    }

    public void DeclineHelp()
    {
        ChooseBrowseAlone();
    }

    public void StopHelping()
    {
        StopCurrentMode();
    }

    public static string BuildProductComment(CS_ProductItem product)
    {
        if (product == null)
        {
            return string.Empty;
        }

        string productName = product.ProductName ?? "this piece";
        string lowerName = productName.ToLowerInvariant();
        if (ContainsAny(lowerName, "dress", "gown"))
        {
            return $"{productName} has a lovely silhouette. It would make a strong centerpiece.";
        }

        if (ContainsAny(lowerName, "shirt", "top", "blouse", "jacket", "coat"))
        {
            return $"{productName} is versatile and easy to layer with the rest of your outfit.";
        }

        if (ContainsAny(lowerName, "pants", "trousers", "jeans", "skirt", "shorts"))
        {
            return $"{productName} gives you a clean base for a balanced outfit.";
        }

        if (ContainsAny(lowerName, "shoe", "boot", "sneaker", "heel"))
        {
            return $"{productName} could finish the look nicely while keeping it practical.";
        }

        if (ContainsAny(lowerName, "bag", "purse", "hat", "cap", "scarf"))
        {
            return $"{productName} is a great accent piece for adding personality.";
        }

        return $"{productName} is a flexible choice. Try pairing it with a contrasting color.";
    }

    public static Vector3 CalculateCameraRelativeFollowTarget(Vector3 playerPosition, Camera camera,
        float distance, float viewportX)
    {
        if (camera == null)
        {
            return playerPosition;
        }

        float clampedViewportX = Mathf.Clamp(viewportX, 0.55f, 0.9f);
        Ray viewportRay = camera.ViewportPointToRay(new Vector3(clampedViewportX, 0.5f, 0f));
        Vector3 direction = Vector3.ProjectOnPlane(viewportRay.direction, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        Vector3 target = playerPosition + direction.normalized * Mathf.Max(0.5f, distance);
        target.y = playerPosition.y;
        return target;
    }

    private void UpdateMovement()
    {
        if (player == null || shopperUI != null && shopperUI.IsConversationOpen)
        {
            return;
        }

        if (state == CS_PersonalShopperState.Approaching)
        {
            Vector3 target = CalculateCameraRelativeFollowTarget(player.position, userCamera,
                greetingDistance, greetingViewportX);
            target.y = transform.position.y;
            if (MoveToward(target, false, true) && !entranceConversationShown)
            {
                entranceConversationShown = true;
                shopperUI.ShowInitialConversation();
            }
        }
        else if (state == CS_PersonalShopperState.Following)
        {
            Vector3 target = CalculateCameraRelativeFollowTarget(player.position, userCamera,
                followDistance, followViewportX);
            target.y = transform.position.y;
            MoveToward(target, false, true);
        }
        else if (state == CS_PersonalShopperState.Guiding)
        {
            UpdateGuideMovement();
        }
        else if (state == CS_PersonalShopperState.Returning && MoveToward(homePosition, true, false))
        {
            transform.SetPositionAndRotation(homePosition, homeRotation);
            state = CS_PersonalShopperState.Waiting;
            ScheduleNextAmbient();
        }
    }

    private bool MoveToward(Vector3 target, bool restoreHomeFacing, bool facePlayer)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;
        Vector3 facingDirection = facePlayer && player != null ? player.position - transform.position : offset;
        facingDirection.y = 0f;
        if (offset.sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            RotateToward(facingDirection, restoreHomeFacing);
            return true;
        }

        Vector3 nextPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if (facePlayer && player != null)
        {
            Vector3 fromPlayer = nextPosition - player.position;
            fromPlayer.y = 0f;
            float requiredSeparation = Mathf.Max(0.5f, minimumPlayerSeparation);
            if (fromPlayer.sqrMagnitude < requiredSeparation * requiredSeparation)
            {
                Vector3 safeDirection = fromPlayer.sqrMagnitude > 0.0001f
                    ? fromPlayer.normalized
                    : Vector3.ProjectOnPlane(userCamera.transform.right, Vector3.up).normalized;
                nextPosition = player.position + safeDirection * requiredSeparation;
                nextPosition.y = transform.position.y;
            }
        }

        transform.position = nextPosition;
        RotateToward(facingDirection, restoreHomeFacing && offset.sqrMagnitude < 0.25f);
        return false;
    }

    private void RotateToward(Vector3 direction, bool restoreHomeFacing)
    {
        Quaternion targetRotation = homeRotation;
        if (!restoreHomeFacing && direction.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void UpdateSpeech()
    {
        if (shopperUI == null)
        {
            return;
        }

        if (ambientVisibleTimer > 0f)
        {
            ambientVisibleTimer -= Time.deltaTime;
            if (ambientVisibleTimer <= 0f)
            {
                shopperUI.HideAmbient();
            }
        }

        if (!entranceGreetingCompleted || state != CS_PersonalShopperState.Waiting || shopperUI.IsConversationOpen)
        {
            return;
        }

        ambientTimer -= Time.deltaTime;
        if (ambientTimer > 0f)
        {
            return;
        }

        shopperUI.ShowAmbient(AmbientMessages[UnityEngine.Random.Range(0, AmbientMessages.Length)]);
        ambientVisibleTimer = ambientDuration;
        ScheduleNextAmbient();
    }

    private void UpdateProductComment()
    {
        if (shopperUI == null || productFocusController == null || mode != CS_PersonalShopperMode.Advice)
        {
            if (commentedProduct != null)
            {
                commentedProduct = null;
                shopperUI?.HideProductComment();
            }

            return;
        }

        CS_ProductItem focusedProduct = productFocusController.Mode == CS_ShoppingMode.Focused
            ? productFocusController.CurrentProduct
            : null;
        if (focusedProduct == commentedProduct)
        {
            return;
        }

        commentedProduct = focusedProduct;
        if (focusedProduct == null)
        {
            shopperUI.HideProductComment();
        }
        else if (!shopperUI.IsConversationOpen)
        {
            shopperUI.ShowProductComment(BuildProductComment(focusedProduct));
        }
    }

    private bool CanTalkAtPointer()
    {
        if (player == null || userCamera == null || pointAction == null || pointAction.action == null ||
            shopperUI != null && shopperUI.IsConversationOpen || !IsPlayerNear() || IsPointerOverUI())
        {
            return false;
        }

        Vector2 pointerPosition = pointAction.action.ReadValue<Vector2>();
        Ray ray = userCamera.ScreenPointToRay(pointerPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance * 2f, interactionLayerMask,
                QueryTriggerInteraction.Collide))
        {
            return false;
        }

        return IsInteractionCollider(hit.collider);
    }

    private bool IsPlayerNear()
    {
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= interactionDistance * interactionDistance;
    }

    private bool IsInteractionCollider(Collider candidate)
    {
        if (candidate == null || interactionColliders == null)
        {
            return false;
        }

        for (int i = 0; i < interactionColliders.Length; i++)
        {
            if (interactionColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private void SetTalkCursor(bool visible)
    {
        if (!visible)
        {
            ClearTalkCursor();
            return;
        }

        if (talkCursorTexture == null)
        {
            talkCursorTexture = CreateTalkCursorTexture();
        }

        // Reapply while hovering because product hover feedback uses the same system cursor.
        Cursor.SetCursor(talkCursorTexture, new Vector2(5f, 5f), CursorMode.Auto);
        talkCursorVisible = true;
    }

    private void ClearTalkCursor()
    {
        if (!talkCursorVisible)
        {
            return;
        }

        talkCursorVisible = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private static Texture2D CreateTalkCursorTexture()
    {
        const int size = 32;
        Color orange = new Color(1f, 106f / 255f, 0f, 1f);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ShoppingVR Talk Cursor",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool bubble = x >= 3 && x <= 28 && y >= 6 && y <= 25;
                bool tail = y >= 2 && y < 7 && x >= 6 && x <= 12 - (y - 2);
                Color color = bubble || tail ? orange : Color.clear;
                bool dot = y >= 14 && y <= 17 && (x >= 8 && x <= 11 || x >= 15 && x <= 18 || x >= 22 && x <= 25);
                if (dot)
                {
                    color = Color.white;
                }

                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void HandleClick(InputAction.CallbackContext context)
    {
        bool pressed = context.ReadValueAsButton();
        if (pressed && !clickPressed)
        {
            pressRequested = true;
        }

        clickPressed = pressed;
    }

    private void HandleClickCanceled(InputAction.CallbackContext context)
    {
        clickPressed = false;
    }

    private void CompleteEntranceGreetingIfNeeded()
    {
        if (entranceGreetingCompleted)
        {
            return;
        }

        entranceGreetingCompleted = true;
        entranceConversationShown = false;
        if (playerLocomotion != null)
        {
            playerLocomotion.SetMovementEnabled(movementWasEnabledBeforeGreeting);
        }
    }

    private void BuildGuideProductList()
    {
        guideProducts.Clear();
        guideProductIndex = 0;
        isStoppedAtGuideProduct = false;

        CS_ProductItem[] products = FindObjectsByType<CS_ProductItem>(FindObjectsInactive.Exclude);
        int count = Mathf.Min(guideItemCount, products.Length);
        for (int i = 0; i < count; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(i, products.Length);
            (products[i], products[selectedIndex]) = (products[selectedIndex], products[i]);
            guideProducts.Add(products[i]);
        }
    }

    private void UpdateGuideMovement()
    {
        if (guideProductIndex >= guideProducts.Count)
        {
            FinishGuide();
            return;
        }

        CS_ProductItem product = guideProducts[guideProductIndex];
        if (product == null || !product.isActiveAndEnabled)
        {
            AdvanceGuide();
            return;
        }

        if (isStoppedAtGuideProduct)
        {
            RotateToward(player != null ? player.position - transform.position : Vector3.zero, false);
            return;
        }

        Vector3 productPosition = product.transform.position;
        Vector3 awayFromProduct = transform.position - productPosition;
        awayFromProduct.y = 0f;
        if (awayFromProduct.sqrMagnitude < 0.0001f)
        {
            awayFromProduct = Vector3.back;
        }

        Vector3 target = productPosition + awayFromProduct.normalized * guideProductDistance;
        target.y = transform.position.y;
        if (!MoveToward(target, false, false))
        {
            return;
        }

        isStoppedAtGuideProduct = true;
        shopperUI?.ShowGuideItem(BuildProductComment(product), guideProductIndex < guideProducts.Count - 1);
    }

    private void AdvanceGuide()
    {
        shopperUI?.HideProductComment();
        guideProductIndex++;
        isStoppedAtGuideProduct = false;
        if (guideProductIndex >= guideProducts.Count)
        {
            FinishGuide();
        }
    }

    private void FinishGuide()
    {
        guideProducts.Clear();
        mode = CS_PersonalShopperMode.None;
        state = CS_PersonalShopperState.Returning;
        isStoppedAtGuideProduct = false;
        shopperUI?.HideConversation();
        shopperUI?.HideProductComment();
        ShowTimedAmbient("Those are my three picks. Take your time, darling.");
    }

    private void ShowTimedAmbient(string message)
    {
        shopperUI?.ShowAmbient(message);
        ambientVisibleTimer = ambientDuration;
        ScheduleNextAmbient();
    }

    private void ScheduleNextAmbient()
    {
        ambientTimer = ambientInterval * UnityEngine.Random.Range(0.9f, 1.1f);
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (source.Contains(values[i], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool EnableIfNeeded(InputActionReference reference)
    {
        if (reference == null || reference.action == null || reference.action.enabled)
        {
            return false;
        }

        reference.action.Enable();
        return true;
    }

    private static void DisableIfEnabledHere(InputActionReference reference, bool enabledHere)
    {
        if (enabledHere && reference != null && reference.action != null)
        {
            reference.action.Disable();
        }
    }
}
