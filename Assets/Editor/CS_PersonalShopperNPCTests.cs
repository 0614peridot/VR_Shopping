using NUnit.Framework;
using UnityEngine;

public sealed class CS_PersonalShopperNPCTests
{
    private GameObject shopperObject;
    private GameObject productObject;
    private GameObject cameraObject;

    [TearDown]
    public void TearDown()
    {
        if (shopperObject != null)
        {
            Object.DestroyImmediate(shopperObject);
        }

        if (productObject != null)
        {
            Object.DestroyImmediate(productObject);
        }

        if (cameraObject != null)
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void HelpChoicesTransitionBetweenWaitingFollowingAndReturning()
    {
        shopperObject = new GameObject("Personal Shopper");
        CS_PersonalShopperNPC shopper = shopperObject.AddComponent<CS_PersonalShopperNPC>();

        Assert.AreEqual(CS_PersonalShopperState.Waiting, shopper.State);
        Assert.AreEqual(CS_PersonalShopperMode.None, shopper.Mode);

        shopper.StartHelping();
        Assert.AreEqual(CS_PersonalShopperState.Following, shopper.State);
        Assert.AreEqual(CS_PersonalShopperMode.Advice, shopper.Mode);

        shopper.KeepHelping();
        Assert.AreEqual(CS_PersonalShopperState.Following, shopper.State);

        shopper.StopHelping();
        Assert.AreEqual(CS_PersonalShopperState.Returning, shopper.State);
        Assert.AreEqual(CS_PersonalShopperMode.None, shopper.Mode);
    }

    [Test]
    public void BrowseChoiceCompletesGreetingAndReturnsShopperHome()
    {
        shopperObject = new GameObject("Personal Shopper");
        CS_PersonalShopperNPC shopper = shopperObject.AddComponent<CS_PersonalShopperNPC>();

        shopper.ChooseBrowseAlone();

        Assert.IsTrue(shopper.HasCompletedEntranceGreeting);
        Assert.AreEqual(CS_PersonalShopperMode.None, shopper.Mode);
        Assert.AreEqual(CS_PersonalShopperState.Returning, shopper.State);
    }

    [Test]
    public void GuideChoiceSelectsAvailableProductsAndStartsGuiding()
    {
        shopperObject = new GameObject("Personal Shopper");
        CS_PersonalShopperNPC shopper = shopperObject.AddComponent<CS_PersonalShopperNPC>();
        productObject = new GameObject("Guide Products");
        for (int i = 0; i < 3; i++)
        {
            GameObject item = new GameObject($"Guide Product {i + 1}");
            item.transform.SetParent(productObject.transform);
            CS_ProductItem product = item.AddComponent<CS_ProductItem>();
            product.Configure(item.name, 1000 + i, null, null);
        }

        shopper.ChooseGuideMode();

        Assert.IsTrue(shopper.HasCompletedEntranceGreeting);
        Assert.AreEqual(CS_PersonalShopperMode.Guide, shopper.Mode);
        Assert.AreEqual(CS_PersonalShopperState.Guiding, shopper.State);
    }

    [TestCase("Evening Dress", "silhouette")]
    [TestCase("Classic Shirt", "layer")]
    [TestCase("Blue Jeans", "clean base")]
    [TestCase("Leather Boots", "finish the look")]
    [TestCase("Canvas Bag", "accent piece")]
    [TestCase("Mystery Item", "contrasting color")]
    public void ProductCommentsMatchProductCategory(string productName, string expectedPhrase)
    {
        productObject = new GameObject(productName);
        CS_ProductItem product = productObject.AddComponent<CS_ProductItem>();
        product.Configure(productName, 1000, null, null);

        string comment = CS_PersonalShopperNPC.BuildProductComment(product);

        StringAssert.Contains(expectedPhrase, comment);
        StringAssert.StartsWith(productName, comment);
    }

    [Test]
    public void NullProductProducesNoComment()
    {
        Assert.AreEqual(string.Empty, CS_PersonalShopperNPC.BuildProductComment(null));
    }

    [Test]
    public void FollowTargetUsesCameraViewportAndStaysInFrontOfPlayer()
    {
        cameraObject = new GameObject("User Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 65f;
        camera.aspect = 16f / 9f;
        cameraObject.transform.SetPositionAndRotation(new Vector3(2f, 1.6f, -3f),
            Quaternion.Euler(0f, 35f, 0f));
        Vector3 playerPosition = new Vector3(2f, 0f, -3f);

        Vector3 target = CS_PersonalShopperNPC.CalculateCameraRelativeFollowTarget(
            playerPosition, camera, 1.8f, 0.76f);
        Vector3 cameraHeightTarget = new Vector3(target.x, cameraObject.transform.position.y, target.z);
        Vector3 viewportPoint = camera.WorldToViewportPoint(cameraHeightTarget);

        Assert.That(Vector3.Distance(playerPosition, target), Is.EqualTo(1.8f).Within(0.001f));
        Assert.That(viewportPoint.z, Is.GreaterThan(0f));
        Assert.That(viewportPoint.x, Is.EqualTo(0.76f).Within(0.001f));
        Assert.That(viewportPoint.x, Is.InRange(0.55f, 0.9f));
    }
}
