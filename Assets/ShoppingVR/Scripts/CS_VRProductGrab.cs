using Oculus.Interaction;
using UnityEngine;

// Bridges Interaction SDK grab events (hands and controllers alike) to product feedback.
// Releasing returns the product to its display pose, matching the desktop focus exit.
[RequireComponent(typeof(CS_ProductItem))]
public sealed class CS_VRProductGrab : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;

    private CS_ProductItem product;
    private bool hasRequiredReferences;
    private bool isHolding;

    public void Configure(Grabbable target)
    {
        grabbable = target;
    }

    private void Awake()
    {
        product = GetComponent<CS_ProductItem>();
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

        hasRequiredReferences = grabbable != null;
        if (!hasRequiredReferences)
        {
            Debug.LogError($"{nameof(CS_VRProductGrab)} on '{name}' needs a Grabbable. VR grabbing is disabled.", this);
        }
    }

    private void OnEnable()
    {
        if (hasRequiredReferences)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void OnDisable()
    {
        if (hasRequiredReferences)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        EndHold();
        product.SetHovered(false);
    }

    // PointableElement updates its point counts before raising the event, so the counts are the source of truth.
    // Several hands can hover or grab at once; the item stays held until the last grabbing point lets go.
    private void HandlePointerEvent(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Move)
        {
            return;
        }

        bool shouldHold = grabbable.SelectingPointsCount > 0;
        if (shouldHold && !isHolding && !product.IsFocused)
        {
            isHolding = true;
            product.BeginHold();
        }
        else if (!shouldHold)
        {
            EndHold();
        }

        product.SetHovered(!isHolding && grabbable.PointsCount > 0);
    }

    private void EndHold()
    {
        if (!isHolding)
        {
            return;
        }

        isHolding = false;
        product.RestoreDisplayState();
    }
}
