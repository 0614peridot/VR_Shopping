using UnityEngine;

public sealed class CS_ProductItem : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private string productName = "Product";
    [SerializeField, Min(0)] private int priceWon;
    [SerializeField] private Collider[] selectionColliders;
    [SerializeField] private Renderer[] feedbackRenderers;
    [SerializeField] private Color selectedColor = new Color(1f, 0.72f, 0.25f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.88f, 0.62f, 1f);

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private bool[] originalTriggerStates;
    private Rigidbody cachedRigidbody;
    private bool originalKinematic;
    private bool originalUseGravity;
    private bool stateCaptured;
    private MaterialPropertyBlock propertyBlock;
    private Transform focusPivot;
    private bool hovered;

    public string ProductName => productName;
    public int PriceWon => priceWon;
    public bool IsFocused => stateCaptured;
    public Transform FocusTransform => focusPivot != null ? focusPivot : transform;

    public void Configure(string displayName, int won, Collider[] colliders, Renderer[] renderers)
    {
        productName = displayName;
        priceWon = Mathf.Max(0, won);
        selectionColliders = colliders;
        feedbackRenderers = renderers;
    }

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        // Hierarchy changes are not allowed during deactivation; the focus controller restores the transform.
        hovered = false;
        SetSelectedVisual(false);
    }

    private void OnDestroy()
    {
        DestroyFocusPivot();
    }

    public void BeginFocus(Transform focusAnchor)
    {
        if (focusAnchor == null || stateCaptured)
        {
            return;
        }

        CaptureDisplayState();
        CreateFocusPivot(focusAnchor, CalculateVisualCenter());
        SetCollidersAsTriggers();

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
        }

        SetSelectedVisual(true);
    }

    // VR hold: the Interaction SDK Grabbable moves the transform itself, so no focus pivot or reparenting.
    // Colliders become triggers so the held item cannot block or push the player's locomotion capsule.
    public void BeginHold()
    {
        if (stateCaptured)
        {
            return;
        }

        CaptureDisplayState();
        SetCollidersAsTriggers();
        hovered = false;
        SetSelectedVisual(true);
    }

    public void SetHovered(bool isHovered)
    {
        if (hovered == isHovered || stateCaptured)
        {
            return;
        }

        hovered = isHovered;
        SetFeedbackColor(isHovered, hoverColor);
    }

    public void RestoreDisplayState()
    {
        if (!stateCaptured)
        {
            return;
        }

        SetSelectedVisual(false);
        // Skipping a no-op SetParent lets a VR hold be restored from OnDisable, where hierarchy changes throw.
        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent, false);
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        transform.localScale = originalLocalScale;
        DestroyFocusPivot();

        if (selectionColliders != null && originalTriggerStates != null)
        {
            int count = Mathf.Min(selectionColliders.Length, originalTriggerStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (selectionColliders[i] != null)
                {
                    selectionColliders[i].isTrigger = originalTriggerStates[i];
                }
            }
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = originalKinematic;
            cachedRigidbody.useGravity = originalUseGravity;
            if (!originalKinematic)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }
        }

        stateCaptured = false;
    }

    private void SetCollidersAsTriggers()
    {
        if (selectionColliders == null)
        {
            return;
        }

        for (int i = 0; i < selectionColliders.Length; i++)
        {
            if (selectionColliders[i] != null)
            {
                selectionColliders[i].isTrigger = true;
            }
        }
    }

    private void CreateFocusPivot(Transform focusAnchor, Vector3 visualCenter)
    {
        GameObject pivotObject = new GameObject($"{name} Focus Pivot")
        {
            hideFlags = HideFlags.DontSave
        };
        focusPivot = pivotObject.transform;

        // Match the product's current orientation before parenting so its authored facing is
        // retained while the new pivot is moved from the visual center to the focus anchor.
        focusPivot.SetPositionAndRotation(visualCenter, transform.rotation);
        focusPivot.SetParent(focusAnchor, true);
        transform.SetParent(focusPivot, true);
        focusPivot.localPosition = Vector3.zero;
        focusPivot.localRotation = Quaternion.identity;
        focusPivot.localScale = Vector3.one;
    }

    private void DestroyFocusPivot()
    {
        if (focusPivot == null)
        {
            return;
        }

        GameObject pivotObject = focusPivot.gameObject;
        focusPivot = null;
        if (Application.isPlaying)
        {
            Destroy(pivotObject);
        }
        else
        {
            DestroyImmediate(pivotObject);
        }
    }

    private Vector3 CalculateVisualCenter()
    {
        Bounds combinedBounds = default;
        bool hasBounds = false;

        if (feedbackRenderers != null)
        {
            for (int i = 0; i < feedbackRenderers.Length; i++)
            {
                Renderer target = feedbackRenderers[i];
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                EncapsulateBounds(ref combinedBounds, ref hasBounds, target.bounds);
            }
        }

        if (!hasBounds && selectionColliders != null)
        {
            for (int i = 0; i < selectionColliders.Length; i++)
            {
                Collider target = selectionColliders[i];
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                EncapsulateBounds(ref combinedBounds, ref hasBounds, target.bounds);
            }
        }

        return hasBounds ? combinedBounds.center : transform.position;
    }

    private static void EncapsulateBounds(ref Bounds combinedBounds, ref bool hasBounds, Bounds bounds)
    {
        if (!hasBounds)
        {
            combinedBounds = bounds;
            hasBounds = true;
            return;
        }

        combinedBounds.Encapsulate(bounds);
    }

    private void CaptureDisplayState()
    {
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;

        if (cachedRigidbody != null)
        {
            originalKinematic = cachedRigidbody.isKinematic;
            originalUseGravity = cachedRigidbody.useGravity;
        }

        if (selectionColliders != null)
        {
            if (originalTriggerStates == null || originalTriggerStates.Length != selectionColliders.Length)
            {
                originalTriggerStates = new bool[selectionColliders.Length];
            }

            for (int i = 0; i < selectionColliders.Length; i++)
            {
                originalTriggerStates[i] = selectionColliders[i] != null && selectionColliders[i].isTrigger;
            }
        }

        stateCaptured = true;
    }

    private void SetSelectedVisual(bool selected)
    {
        SetFeedbackColor(selected, selectedColor);
    }

    private void SetFeedbackColor(bool enabled, Color color)
    {
        if (feedbackRenderers == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < feedbackRenderers.Length; i++)
        {
            Renderer target = feedbackRenderers[i];
            if (target == null)
            {
                continue;
            }

            if (enabled)
            {
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                target.SetPropertyBlock(propertyBlock);
            }
            else
            {
                target.SetPropertyBlock(null);
            }
        }
    }
}
