using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CS_ProductPivotTests
{
    [TestCase("Studio Tote", true)]
    [TestCase("Front Left Rack Garment 3", false)]
    public void SceneProduct_RotatesAroundItsVisualCenter(string productName, bool expectOffsetRoot)
    {
        EditorSceneManager.OpenScene("Assets/ShoppingVR/Scenes/ShoppingEnvironment.unity");
        CS_ProductItem[] products = Resources.FindObjectsOfTypeAll<CS_ProductItem>();
        CS_ProductItem product = null;
        for (int i = 0; i < products.Length; i++)
        {
            if (products[i].ProductName == productName && products[i].gameObject.scene.IsValid())
            {
                product = products[i];
                break;
            }
        }

        Assert.That(product, Is.Not.Null);
        Renderer[] renderers = product.GetComponentsInChildren<Renderer>();
        Bounds visualBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            visualBounds.Encapsulate(renderers[i].bounds);
        }

        float rootOffset = Vector3.Distance(product.transform.position, visualBounds.center);
        Assert.That(rootOffset, expectOffsetRoot ? Is.GreaterThan(0.5f) : Is.LessThan(0.5f));

        GameObject anchorObject = new GameObject("Test Focus Anchor");
        try
        {
            anchorObject.transform.position = new Vector3(0f, 1.5f, 4f);
            Transform originalParent = product.transform.parent;
            Vector3 originalLocalPosition = product.transform.localPosition;
            Quaternion originalLocalRotation = product.transform.localRotation;

            product.BeginFocus(anchorObject.transform);
            product.FocusTransform.Rotate(Vector3.up, 135f, Space.World);

            visualBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                visualBounds.Encapsulate(renderers[i].bounds);
            }

            Assert.That(Vector3.Distance(visualBounds.center, anchorObject.transform.position), Is.LessThan(0.0001f));

            product.RestoreDisplayState();

            Assert.That(product.transform.parent, Is.SameAs(originalParent));
            Assert.That(product.transform.localPosition, Is.EqualTo(originalLocalPosition));
            Assert.That(Quaternion.Angle(product.transform.localRotation, originalLocalRotation), Is.LessThan(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(anchorObject);
        }
    }

    [TestCase(0f)]
    [TestCase(8f)]
    public void FocusPivot_CentersVisualsAndKeepsTheirCenterStableWhileRotating(float childOffset)
    {
        GameObject originalParent = new GameObject("Original Parent");
        GameObject anchorObject = new GameObject("Focus Anchor");
        GameObject productObject = new GameObject("Product");
        GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        try
        {
            originalParent.transform.position = new Vector3(3f, 0f, -2f);
            productObject.transform.SetParent(originalParent.transform, false);
            productObject.transform.localPosition = new Vector3(1f, 2f, 3f);
            productObject.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            visualObject.transform.SetParent(productObject.transform, false);
            visualObject.transform.localPosition = new Vector3(childOffset, 0.5f, -childOffset * 0.25f);

            Renderer renderer = visualObject.GetComponent<Renderer>();
            Collider collider = visualObject.GetComponent<Collider>();
            CS_ProductItem product = productObject.AddComponent<CS_ProductItem>();
            product.Configure("Test Product", 100, new[] { collider }, new[] { renderer });

            Transform originalProductParent = productObject.transform.parent;
            Vector3 originalLocalPosition = productObject.transform.localPosition;
            Quaternion originalLocalRotation = productObject.transform.localRotation;
            anchorObject.transform.SetPositionAndRotation(new Vector3(-4f, 1.5f, 6f), Quaternion.Euler(5f, 40f, 0f));

            product.BeginFocus(anchorObject.transform);

            Assert.That(Vector3.Distance(renderer.bounds.center, anchorObject.transform.position), Is.LessThan(0.0001f));
            Assert.That(product.FocusTransform, Is.Not.SameAs(product.transform));

            product.FocusTransform.Rotate(Vector3.up, 90f, Space.World);

            Assert.That(Vector3.Distance(renderer.bounds.center, anchorObject.transform.position), Is.LessThan(0.0001f));

            product.RestoreDisplayState();

            Assert.That(productObject.transform.parent, Is.SameAs(originalProductParent));
            Assert.That(productObject.transform.localPosition, Is.EqualTo(originalLocalPosition));
            Assert.That(Quaternion.Angle(productObject.transform.localRotation, originalLocalRotation), Is.LessThan(0.0001f));
            Assert.That(product.FocusTransform, Is.SameAs(product.transform));
        }
        finally
        {
            Object.DestroyImmediate(originalParent);
            Object.DestroyImmediate(anchorObject);
        }
    }
}
