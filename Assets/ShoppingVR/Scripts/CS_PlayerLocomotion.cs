using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CS_PlayerLocomotion : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference rightClickAction;
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField, Min(0.01f)] private float lookSensitivity = 0.16f;
    [SerializeField] private Transform viewTransform;

    private const float GroundedFallSpeed = -1f;
    private const float AirborneFallSpeed = -4f;
    private const float PitchLimit = 75f;

    private CharacterController characterController;
    private bool movementEnabled = true;
    private bool hasRequiredReferences;
    private bool enabledMoveAction;
    private bool enabledLookAction;
    private bool enabledRightClickAction;
    private float pitch;

    public bool MovementEnabled => movementEnabled;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (viewTransform == null && Camera.main != null)
        {
            viewTransform = Camera.main.transform;
        }

        hasRequiredReferences = moveAction != null && viewTransform != null;
        if (!hasRequiredReferences)
        {
            Debug.LogError($"{nameof(CS_PlayerLocomotion)} on '{name}' needs Move Action and View Transform. Movement is disabled.", this);
        }
    }

    private void OnEnable()
    {
        enabledMoveAction = EnableIfNeeded(moveAction);
        enabledLookAction = EnableIfNeeded(lookAction);
        enabledRightClickAction = EnableIfNeeded(rightClickAction);
    }

    private void OnDisable()
    {
        DisableIfEnabledHere(moveAction, enabledMoveAction);
        DisableIfEnabledHere(lookAction, enabledLookAction);
        DisableIfEnabledHere(rightClickAction, enabledRightClickAction);
        enabledMoveAction = false;
        enabledLookAction = false;
        enabledRightClickAction = false;
    }

    private void Update()
    {
        if (!hasRequiredReferences || !movementEnabled)
        {
            return;
        }

        UpdateLook();

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 forward = Vector3.ProjectOnPlane(viewTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(viewTransform.right, Vector3.up).normalized;
        Vector3 velocity = (forward * input.y + right * input.x) * moveSpeed;
        velocity.y = characterController.isGrounded ? GroundedFallSpeed : AirborneFallSpeed;

        characterController.Move(velocity * Time.deltaTime);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    // Optional right-mouse look; skipped when Look or Right Click is not assigned.
    private void UpdateLook()
    {
        if (lookAction == null || rightClickAction == null || !rightClickAction.action.IsPressed())
        {
            return;
        }

        Vector2 delta = lookAction.action.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(0f, delta.x, 0f, Space.World);
        pitch = Mathf.Clamp(pitch - delta.y, -PitchLimit, PitchLimit);
        viewTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
