using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CS_CheckoutNPC : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private CS_CartCounter cartCounter;
    [SerializeField] private CS_CheckoutUIController checkoutUI;
    [SerializeField] private InputActionReference interactionAction;
    [SerializeField, Min(0.25f)] private float interactionRange = 3f;

    private bool playerInRange;
    private bool enabledInteractionAction;

    public bool IsPlayerInRange => playerInRange;

    private void Awake()
    {
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }

        if (playerTransform == null || cartCounter == null || checkoutUI == null)
        {
            Debug.LogError($"{nameof(CS_CheckoutNPC)} on '{name}' needs Player Transform, Cart Counter, and Checkout UI.", this);
        }

        if (checkoutUI != null)
        {
            checkoutUI.Configure(cartCounter);
        }
    }

    private void OnEnable()
    {
        BindInteractionAction();
    }

    private void OnDisable()
    {
        UnbindInteractionAction();
        playerInRange = false;
        if (checkoutUI != null)
        {
            checkoutUI.SetPlayerInRange(false);
        }
    }

    private void Update()
    {
        if (playerTransform == null || checkoutUI == null)
        {
            return;
        }

        float rangeSquared = interactionRange * interactionRange;
        bool isInRange = (playerTransform.position - transform.position).sqrMagnitude <= rangeSquared;
        if (isInRange == playerInRange)
        {
            return;
        }

        playerInRange = isInRange;
        checkoutUI.SetPlayerInRange(playerInRange);
    }

    public void Configure(Transform player, CS_CartCounter cart, CS_CheckoutUIController ui,
        InputActionReference interactAction, float range = 3f)
    {
        UnbindInteractionAction();
        playerTransform = player;
        cartCounter = cart;
        checkoutUI = ui;
        interactionAction = interactAction;
        interactionRange = Mathf.Max(0.25f, range);

        if (checkoutUI != null)
        {
            checkoutUI.Configure(cartCounter);
        }

        if (isActiveAndEnabled)
        {
            BindInteractionAction();
        }
    }

    public bool TryInteract()
    {
        if (!playerInRange || checkoutUI == null || !checkoutUI.IsPromptVisible)
        {
            return false;
        }

        checkoutUI.OpenReview();
        return true;
    }

    private void HandleInteractionPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void BindInteractionAction()
    {
        if (interactionAction == null || interactionAction.action == null)
        {
            return;
        }

        interactionAction.action.performed -= HandleInteractionPerformed;
        interactionAction.action.performed += HandleInteractionPerformed;
        if (!interactionAction.action.enabled)
        {
            interactionAction.action.Enable();
            enabledInteractionAction = true;
        }
    }

    private void UnbindInteractionAction()
    {
        if (interactionAction == null || interactionAction.action == null)
        {
            enabledInteractionAction = false;
            return;
        }

        interactionAction.action.performed -= HandleInteractionPerformed;
        if (enabledInteractionAction)
        {
            interactionAction.action.Disable();
        }

        enabledInteractionAction = false;
    }
}
