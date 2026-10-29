#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CS_ShoppingEnvironmentBuilder
{
    private const string ScenePath = "Assets/ShoppingVR/Scenes/ShoppingEnvironment.unity";
    private const string MaterialFolder = "Assets/ShoppingVR/Materials";
    private const string InputFolder = "Assets/ShoppingVR/Input";
    private const int ProductLayer = 8;
    private const float DefaultLookSensitivity = 0.16f;
    private static readonly Vector3 ExteriorStartPosition = new Vector3(0f, 0f, -13.5f);
    private static int generatedGarmentIndex;
    private const float CeilingLightIntensity = 26f;

    private static readonly Color Ink = new Color(0.08f, 0.10f, 0.13f);
    private static readonly Color WarmWhite = new Color(0.92f, 0.89f, 0.83f);
    private static readonly Color Brass = new Color(0.75f, 0.54f, 0.25f);
    private static readonly Color Sage = new Color(0.31f, 0.46f, 0.39f);
    private static readonly Color Terracotta = new Color(0.67f, 0.30f, 0.20f);

    static CS_ShoppingEnvironmentBuilder()
    {
        EditorApplication.delayCall += EnsureSceneExists;
        EditorApplication.delayCall += ApplyPendingCeilingUpgrade;
        EditorApplication.delayCall += ApplyPendingTextFreePresentation;
    }

    private static void ApplyPendingTextFreePresentation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ApplyPendingTextFreePresentation;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            return;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.scene == scene && canvases[i].gameObject.activeSelf)
            {
                RemoveAllWordingUI();
                return;
            }
        }

        TextMesh[] worldLabels = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < worldLabels.Length; i++)
        {
            if (worldLabels[i].gameObject.scene == scene && worldLabels[i].gameObject.activeSelf)
            {
                RemoveAllWordingUI();
                return;
            }
        }
    }

    private static void ApplyPendingCeilingUpgrade()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            return;
        }

        GameObject environment = GameObject.Find("Shopping Environment");
        if (environment == null)
        {
            return;
        }

        Transform firstLight = environment.transform.Find("Ceiling Light 1");
        Light light = firstLight != null ? firstLight.GetComponent<Light>() : null;
        if (environment.transform.Find("Ceiling") == null ||
            (light != null && !Mathf.Approximately(light.intensity, CeilingLightIntensity)))
        {
            ApplyCeilingAndLighting();
        }
    }

    [MenuItem("ShoppingVR/Rebuild Shopping Environment")]
    public static void RebuildFromMenu()
    {
        BuildScene(true);
    }

    [MenuItem("ShoppingVR/Apply Exterior and Product Placeholders")]
    public static void ApplyExteriorAndProductPlaceholders()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Please exit Play Mode before updating the ShoppingVR scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject environmentObject = GameObject.Find("Shopping Environment");
        GameObject player = GameObject.Find("Player");
        if (environmentObject == null || player == null)
        {
            throw new System.InvalidOperationException("ShoppingVR scene update failed: environment or player was not found.");
        }

        EnsureExterior(environmentObject.transform);
        player.transform.position = ExteriorStartPosition;

        int replacedProducts = 0;
        CS_ProductItem[] products = Object.FindObjectsByType<CS_ProductItem>(FindObjectsInactive.Include);
        for (int i = 0; i < products.Length; i++)
        {
            if (ReplaceClothingVisualWithPlaceholder(products[i]))
            {
                replacedProducts++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"ShoppingVR exterior added, player moved outside, and {replacedProducts} clothing products replaced with model placeholders.");
    }

    [MenuItem("ShoppingVR/Apply Ceiling and Lighting")]
    public static void ApplyCeilingAndLighting()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Please exit Play Mode before updating the ShoppingVR scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject environmentObject = GameObject.Find("Shopping Environment");
        if (environmentObject == null)
        {
            throw new System.InvalidOperationException("ShoppingVR ceiling update failed: environment was not found.");
        }

        Material ceiling = GetMaterial("M_Wall", WarmWhite, 0f, 0.65f);
        Material fixture = GetMaterial("M_Ink", Ink, 0.25f, 0.48f);
        Material diffuser = GetLightDiffuserMaterial();
        EnsureCeilingAndLighting(environmentObject.transform, ceiling, fixture, diffuser);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ShoppingVR ceiling and six performance-friendly recessed spotlights applied.");
    }

    [MenuItem("ShoppingVR/Remove All Wording UI")]
    public static void RemoveAllWordingUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Please exit Play Mode before removing the ShoppingVR wording UI.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        int hiddenCanvases = HideAllWordingUI(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"ShoppingVR wording UI removed: {hiddenCanvases} screen canvas(es) and all world labels hidden.");
    }

    [MenuItem("ShoppingVR/Validate Shopping Environment")]
    public static void ValidateGeneratedScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CS_ProductItem[] products = Object.FindObjectsByType<CS_ProductItem>(FindObjectsSortMode.None);
        CS_ProductFocusController focusController = Object.FindFirstObjectByType<CS_ProductFocusController>();
        CS_PlayerLocomotion locomotion = Object.FindFirstObjectByType<CS_PlayerLocomotion>();
        CS_FocusUIController focusUI = Object.FindFirstObjectByType<CS_FocusUIController>(FindObjectsInactive.Include);
        CS_CartCounter cart = Object.FindFirstObjectByType<CS_CartCounter>(FindObjectsInactive.Include);
        CS_CheckoutNPC checkoutNPC = Object.FindFirstObjectByType<CS_CheckoutNPC>(FindObjectsInactive.Include);
        CS_CheckoutUIController checkoutUI = Object.FindFirstObjectByType<CS_CheckoutUIController>(FindObjectsInactive.Include);
        CS_PersonalShopperNPC personalShopper = Object.FindFirstObjectByType<CS_PersonalShopperNPC>(FindObjectsInactive.Include);
        CS_PersonalShopperUI personalShopperUI = Object.FindFirstObjectByType<CS_PersonalShopperUI>(FindObjectsInactive.Include);
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (!scene.IsValid() || products.Length < 10 || focusController == null || locomotion == null ||
            focusUI == null || cart == null || checkoutNPC == null || checkoutUI == null ||
            personalShopper == null || personalShopperUI == null || eventSystem == null)
        {
            throw new System.InvalidOperationException("ShoppingVR scene validation failed: required environment or interaction objects are missing.");
        }

        int missingScripts = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            }
        }

        if (missingScripts != 0)
        {
            throw new System.InvalidOperationException($"ShoppingVR scene validation failed: {missingScripts} missing script reference(s).");
        }

        ValidateReferences(focusController, "userCamera", "focusAnchor", "pointAction", "clickAction", "cancelAction",
            "locomotion", "focusUI", "cartCounter");
        ValidateReferences(locomotion, "moveAction", "lookAction", "rightClickAction", "viewTransform");
        ValidateFloat(locomotion, "lookSensitivity", DefaultLookSensitivity);
        ValidateReferences(focusUI, "focusPanel", "productNameText", "priceText", "addToCartButton", "exitButton", "focusController");
        ValidateReferences(cart, "countText", "badgeRoot");
        ValidateReferences(checkoutNPC, "playerTransform", "cartCounter", "checkoutUI", "interactionAction");
        ValidateReferences(checkoutUI, "cartCounter", "promptRoot", "promptText", "promptButton", "reviewRoot",
            "itemContent", "emptyCartText", "totalText", "shopMoreButton", "shopMoreText", "checkoutButton",
            "checkoutText", "thankYouRoot", "thankYouText");
        ValidateReferences(personalShopper, "player", "userCamera", "pointAction", "clickAction", "shopperUI",
            "productFocusController");
        ValidateReferences(personalShopperUI, "conversationPanel", "promptText", "primaryButton", "primaryButtonText",
            "secondaryButton", "secondaryButtonText", "ambientBubble", "ambientText", "productCommentBubble",
            "productCommentText", "shopper");
        ValidateArrayHasReferences(personalShopper, "interactionColliders");

        Debug.Log($"ShoppingVR validation passed: {products.Length} selectable products, 2 interactive NPCs, " +
                  "UI/input references connected, 0 missing scripts.");
    }

    [MenuItem("ShoppingVR/Update Current Scene Components and English UI")]
    public static void UpdateCurrentSceneComponentsAndEnglishUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Please exit Play Mode before updating the ShoppingVR scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        EnsureProductLayer();
        int addedProducts = AddProductItemsToGarments();
        int removedDuplicates = RemoveDuplicateSceneControllers();
        RepairSceneControllerReferences();
        UpdateEnglishUI();
        UpdateFocusActionLayout();
        RepairStandaloneProductVisuals(GameObject.Find("Shopping Environment").transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"ShoppingVR scene updated: {addedProducts} garment ProductItems added, " +
                  $"{removedDuplicates} duplicate controllers removed, English UI applied.");
    }

    private static void EnsureSceneExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += EnsureSceneExists;
            return;
        }

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
        {
            BuildScene(false);
        }
    }

    private static void BuildScene(bool openWhenFinished)
    {
        EnsureFolder("Assets/ShoppingVR", "Scenes");
        EnsureFolder("Assets/ShoppingVR", "Materials");
        EnsureFolder("Assets/ShoppingVR", "Input");
        EnsureProductLayer();

        Scene previousScene = SceneManager.GetActiveScene();
        bool preservePreviousScene = previousScene.IsValid() && previousScene.isLoaded &&
                                     !string.IsNullOrEmpty(previousScene.path);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            preservePreviousScene ? NewSceneMode.Additive : NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        Material floorMat = GetMaterial("M_Floor", new Color(0.48f, 0.35f, 0.24f), 0f, 0.23f);
        Material wallMat = GetMaterial("M_Wall", WarmWhite, 0f, 0.65f);
        Material darkMat = GetMaterial("M_Ink", Ink, 0.25f, 0.48f);
        Material metalMat = GetMaterial("M_Brass", Brass, 0.75f, 0.4f);
        Material sageMat = GetMaterial("M_Sage", Sage, 0f, 0.5f);
        Material terracottaMat = GetMaterial("M_Terracotta", Terracotta, 0f, 0.55f);
        Material creamMat = GetMaterial("M_Cream", new Color(0.82f, 0.75f, 0.64f), 0f, 0.55f);
        Material glassMat = GetMaterial("M_Glass", new Color(0.25f, 0.48f, 0.55f, 0.25f), 0.35f, 0.75f);
        Material skinMat = GetMaterial("M_NPC_Skin", new Color(0.68f, 0.45f, 0.32f), 0f, 0.62f);
        Material hairMat = GetMaterial("M_NPC_Hair", new Color(0.055f, 0.045f, 0.04f), 0f, 0.38f);
        Material denimMat = GetMaterial("M_NPC_Denim", new Color(0.13f, 0.24f, 0.34f), 0f, 0.42f);

        GameObject environment = new GameObject("Shopping Environment");
        generatedGarmentIndex = 0;
        BuildShell(environment.transform, floorMat, wallMat, darkMat, glassMat);
        BuildBackWall(environment.transform, wallMat, darkMat, sageMat, metalMat);
        BuildLeftDisplays(environment.transform, darkMat, metalMat, sageMat, terracottaMat, creamMat);
        BuildCenterDisplays(environment.transform, darkMat, metalMat, sageMat, terracottaMat, creamMat);
        BuildRightWing(environment.transform, wallMat, darkMat, metalMat, sageMat, terracottaMat, creamMat, glassMat);
        BuildNPCVisuals(environment.transform, skinMat, hairMat, darkMat, creamMat, sageMat, terracottaMat, denimMat);
        BuildLighting(environment.transform, darkMat, GetLightDiffuserMaterial());

        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        InputActionReference move = GetActionReference(inputAsset, "Player/Move");
        InputActionReference look = GetActionReference(inputAsset, "Player/Look");
        InputActionReference point = GetActionReference(inputAsset, "UI/Point");
        InputActionReference click = GetActionReference(inputAsset, "UI/Click");
        InputActionReference rightClick = GetActionReference(inputAsset, "UI/RightClick");
        InputActionReference cancel = GetActionReference(inputAsset, "UI/Cancel");
        InputActionReference navigate = GetActionReference(inputAsset, "UI/Navigate");
        InputActionReference submit = GetActionReference(inputAsset, "UI/Submit");

        GameObject player = BuildPlayer(move, look, rightClick);
        Camera userCamera = player.GetComponentInChildren<Camera>();
        CS_PlayerLocomotion locomotion = player.GetComponent<CS_PlayerLocomotion>();
        Transform focusAnchor = userCamera.transform.Find("FocusAnchor");

        CS_CartCounter cartCounter;
        CS_FocusUIController focusUI;
        CS_CheckoutUIController checkoutUI;
        CS_PersonalShopperUI personalShopperUI;
        BuildUI(out cartCounter, out focusUI, out checkoutUI, out personalShopperUI);

        CS_ProductFocusController focusController = player.AddComponent<CS_ProductFocusController>();
        focusController.Configure(userCamera, focusAnchor, locomotion, focusUI, cartCounter, 1 << ProductLayer);
        SetObjectReference(focusController, "pointAction", point);
        SetObjectReference(focusController, "clickAction", click);
        SetObjectReference(focusController, "cancelAction", cancel);
        focusUI.Configure(focusController);

        Transform checkoutClerk = environment.transform.Find("NPCs/Checkout Clerk");
        CS_CheckoutNPC checkoutNPC = checkoutClerk.gameObject.AddComponent<CS_CheckoutNPC>();
        checkoutNPC.Configure(player.transform, cartCounter, checkoutUI, submit, 3f);

        Transform personalShopper = environment.transform.Find("NPCs/Personal Shopper");
        CapsuleCollider shopperCollider = personalShopper.GetComponent<CapsuleCollider>();
        CS_PersonalShopperNPC personalShopperNPC = personalShopper.gameObject.AddComponent<CS_PersonalShopperNPC>();
        personalShopperNPC.Configure(player.transform, userCamera, personalShopperUI, focusController,
            point, click, new Collider[] { shopperCollider }, ~0);
        SetObjectReference(personalShopperUI, "shopper", personalShopperNPC);

        BuildEventSystem(point, click, rightClick, cancel, navigate, submit);
        ApplySavedLayout(environment.transform);
        HideAllWordingUI(scene);
        ApplyInteriorAmbient();
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.68f, 0.66f, 0.62f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance = 65f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        if (!openWhenFinished && preservePreviousScene)
        {
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previousScene);
        }
        else
        {
            if (previousScene.IsValid() && previousScene != scene)
            {
                EditorSceneManager.CloseScene(previousScene, true);
            }
            SceneManager.SetActiveScene(scene);
        }

        Debug.Log("ShoppingVR: floor-plan environment generated at " + ScenePath);
    }

    private static void BuildShell(Transform parent, Material floor, Material wall, Material dark, Material glass)
    {
        CreateCube("Floor", new Vector3(0f, -0.15f, 0f), new Vector3(28f, 0.3f, 18f), floor, parent);
        CreateCube("Ceiling", new Vector3(0f, 5.1f, 0f), new Vector3(28f, 0.2f, 18f), wall, parent);
        BuildExterior(parent, floor, dark);
        CreateCube("Rear Wall", new Vector3(0f, 2.5f, 9f), new Vector3(28f, 5f, 0.3f), wall, parent);
        CreateCube("Left Wall", new Vector3(-14f, 2.5f, 0f), new Vector3(0.3f, 5f, 18f), wall, parent);
        CreateCube("Right Wall", new Vector3(14f, 2.5f, 0f), new Vector3(0.3f, 5f, 18f), wall, parent);
        CreateCube("Front Wall L", new Vector3(-8.2f, 2.5f, -9f), new Vector3(11.6f, 5f, 0.3f), wall, parent);
        CreateCube("Front Wall R", new Vector3(8.2f, 2.5f, -9f), new Vector3(11.6f, 5f, 0.3f), wall, parent);
        CreateCube("Entrance Header", new Vector3(0f, 4.35f, -9f), new Vector3(4.8f, 1.3f, 0.3f), dark, parent);

        GameObject doorL = CreateCube("Glass Door L", new Vector3(-1.15f, 1.65f, -8.92f), new Vector3(0.08f, 3.3f, 2.1f), glass, parent);
        doorL.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
        GameObject doorR = CreateCube("Glass Door R", new Vector3(1.15f, 1.65f, -8.92f), new Vector3(0.08f, 3.3f, 2.1f), glass, parent);
        doorR.transform.rotation = Quaternion.Euler(0f, -70f, 0f);
        Object.DestroyImmediate(doorL.GetComponent<Collider>());
        Object.DestroyImmediate(doorR.GetComponent<Collider>());

        CreateWorldLabel("SHOP / EDIT", new Vector3(0f, 4.35f, -8.78f), Quaternion.Euler(0f, 180f, 0f), 0.55f, WarmWhite, parent);
    }

    private static void BuildExterior(Transform parent, Material floor, Material dark)
    {
        CreateCube("Exterior Ground", new Vector3(0f, -0.15f, -15f), new Vector3(28f, 0.3f, 12f), floor, parent);
        CreateCube("Exterior Boundary Left", new Vector3(-13.85f, 0.2f, -15f), new Vector3(0.3f, 0.4f, 12f), dark, parent);
        CreateCube("Exterior Boundary Right", new Vector3(13.85f, 0.2f, -15f), new Vector3(0.3f, 0.4f, 12f), dark, parent);
        CreateCube("Exterior Boundary Front", new Vector3(0f, 0.2f, -20.85f), new Vector3(28f, 0.4f, 0.3f), dark, parent);
    }

    private static void EnsureExterior(Transform environment)
    {
        if (environment.Find("Exterior Ground") != null)
        {
            return;
        }

        Transform interiorFloor = environment.Find("Floor");
        Renderer floorRenderer = interiorFloor != null ? interiorFloor.GetComponent<Renderer>() : null;
        Material floor = floorRenderer != null ? floorRenderer.sharedMaterial :
            GetMaterial("M_Floor", new Color(0.48f, 0.35f, 0.24f), 0f, 0.23f);
        Material dark = GetMaterial("M_Ink", Ink, 0.25f, 0.48f);
        BuildExterior(environment, floor, dark);
    }

    private static void BuildBackWall(Transform parent, Material wall, Material dark, Material sage, Material metal)
    {
        CreateCabinet("Display Cabinet A", new Vector3(-11.8f, 1.15f, 8.45f), new Vector3(3.6f, 2.3f, 0.75f), dark, metal, parent);
        CreatePoster("NEW / FORM", new Vector3(-8.4f, 2.35f, 8.78f), 2.5f, 3.5f, Terracotta, parent);
        CreateCabinet("Display Cabinet B", new Vector3(-5.3f, 1.15f, 8.45f), new Vector3(3.7f, 2.3f, 0.75f), dark, metal, parent);
        CreateWorldLabel("SHOP / EDIT", new Vector3(-1.8f, 2.8f, 8.78f), Quaternion.Euler(0f, 180f, 0f), 0.6f, Ink, parent);

        CreateCube("Stockroom Left", new Vector3(2.2f, 2.5f, 7f), new Vector3(0.25f, 5f, 4f), wall, parent);
        CreateCube("Stockroom Right", new Vector3(5.2f, 2.5f, 7f), new Vector3(0.25f, 5f, 4f), wall, parent);
        CreateCube("Stockroom Front L", new Vector3(2.8f, 2.5f, 5f), new Vector3(1.2f, 5f, 0.25f), wall, parent);
        CreateCube("Stockroom Front R", new Vector3(4.7f, 2.5f, 5f), new Vector3(1f, 5f, 0.25f), wall, parent);
        CreateWorldLabel("STOCK", new Vector3(3.7f, 3.8f, 4.84f), Quaternion.identity, 0.38f, Ink, parent);

        CreateCube("Checkout Counter", new Vector3(0.3f, 0.65f, 6.4f), new Vector3(3f, 1.3f, 1.25f), sage, parent);
        CreateCube("Checkout Top", new Vector3(0.3f, 1.38f, 6.4f), new Vector3(3.25f, 0.16f, 1.45f), dark, parent);
        CreateCube("Register", new Vector3(0.65f, 1.7f, 6.45f), new Vector3(0.65f, 0.55f, 0.45f), metal, parent);
        CreateWorldLabel("CHECKOUT", new Vector3(0.3f, 1.02f, 5.73f), Quaternion.identity, 0.28f, WarmWhite, parent);
    }

    private static void BuildLeftDisplays(Transform parent, Material dark, Material metal, Material sage,
        Material terracotta, Material cream)
    {
        CreateWallRack("Left Wall Rack", new Vector3(-13.25f, 0f, 1.1f), 7, true, dark, metal, sage, terracotta, cream, parent);
        CreateRack("Rear Left Rack", new Vector3(-8.9f, 0f, 4.3f), 7, dark, metal, sage, terracotta, cream, parent);
        CreateRack("Front Left Rack", new Vector3(-8.9f, 0f, -3.2f), 7, dark, metal, cream, terracotta, sage, parent);
        CreateDisplayPlinth("Mode Display", new Vector3(-4.7f, 0f, 0.7f), cream, dark, parent);
        CreateShirtProduct("Ivory Knit", 89000, new Vector3(-4.7f, 1.45f, 0.7f), Quaternion.Euler(0f, 25f, 0f), cream, parent);
        CreateWorldLabel("MODE / 01", new Vector3(-4.7f, 0.55f, -0.04f), Quaternion.identity, 0.22f, Ink, parent);

        CreateCabinet("Front Cabinet A", new Vector3(-11.5f, 0.85f, -8.35f), new Vector3(3.1f, 1.7f, 1f), dark, metal, parent);
        CreateCabinet("Front Cabinet B", new Vector3(-7.8f, 0.85f, -8.35f), new Vector3(3.1f, 1.7f, 1f), dark, metal, parent);
        CreatePoster("FORM / 02", new Vector3(-4.7f, 2f, -8.78f), 2.4f, 3f, Sage, parent);
    }

    private static void BuildCenterDisplays(Transform parent, Material dark, Material metal, Material sage,
        Material terracotta, Material cream)
    {
        CreateRoundRack("Center Round Rack", new Vector3(1.1f, 0f, 0.7f), dark, metal, sage, terracotta, cream, parent);
        CreateRack("Right Center Rack", new Vector3(7.4f, 0f, -0.2f), 6, dark, metal, terracotta, cream, sage, parent);

        CreateShirtProduct("Sage Overshirt", 129000, new Vector3(-9.8f, 1.7f, 4.3f), Quaternion.identity, sage, parent);
        CreateShirtProduct("Clay Jacket", 159000, new Vector3(-8.2f, 1.7f, 4.3f), Quaternion.identity, terracotta, parent);
        CreateShirtProduct("Cream Blouse", 94000, new Vector3(-9.5f, 1.7f, -3.2f), Quaternion.identity, cream, parent);
        CreateShirtProduct("Charcoal Coat", 189000, new Vector3(-8f, 1.7f, -3.2f), Quaternion.identity, dark, parent);
        CreateBagProduct("Studio Tote", 118000, new Vector3(1.1f, 1.55f, 0.7f), terracotta, parent);
        CreateShirtProduct("Midnight Shirt", 112000, new Vector3(6.6f, 1.7f, -0.2f), Quaternion.identity, dark, parent);
        CreateShirtProduct("Sand Cardigan", 136000, new Vector3(8.1f, 1.7f, -0.2f), Quaternion.identity, cream, parent);
    }

    private static void BuildRightWing(Transform parent, Material wall, Material dark, Material metal,
        Material sage, Material terracotta, Material cream, Material glass)
    {
        // Fitting-room arc from the source plan, represented by faceted privacy panels.
        Vector3 arcCenter = new Vector3(10.5f, 0f, 6.8f);
        for (int i = 0; i < 5; i++)
        {
            float angle = Mathf.Lerp(205f, 335f, i / 4f);
            float radians = angle * Mathf.Deg2Rad;
            Vector3 position = arcCenter + new Vector3(Mathf.Cos(radians) * 3.1f, 2.5f, Mathf.Sin(radians) * 3.1f);
            GameObject panel = CreateCube("Fitting Panel " + (i + 1), position, new Vector3(2.1f, 5f, 0.18f), wall, parent);
            panel.transform.rotation = Quaternion.Euler(0f, -angle + 90f, 0f);
        }
        CreateWorldLabel("FITTING", new Vector3(10.5f, 3.7f, 8.78f), Quaternion.Euler(0f, 180f, 0f), 0.4f, Ink, parent);
        CreateWallRack("Right Wall Rack", new Vector3(13.2f, 0f, 0.6f), 6, true, dark, metal, terracotta, cream, sage, parent);

        CreateCylinder("Rest Rug", new Vector3(7.8f, 0.04f, 3.1f), new Vector3(2.4f, 0.04f, 2.4f), sage, parent);
        CreateCylinder("Rest Seat", new Vector3(7.8f, 0.38f, 3.1f), new Vector3(0.85f, 0.38f, 0.85f), cream, parent);
        CreateCylinder("Side Table", new Vector3(9.3f, 0.45f, 2.8f), new Vector3(0.45f, 0.45f, 0.45f), metal, parent);
        CreateWorldLabel("REST / AREA", new Vector3(7.8f, 0.12f, 1.75f), Quaternion.Euler(90f, 0f, 0f), 0.2f, WarmWhite, parent);

        CreateCube("Showcase Base", new Vector3(9.1f, 0.65f, -6.7f), new Vector3(8.8f, 1.3f, 3.4f), dark, parent);
        CreateCube("Showcase Glass", new Vector3(9.1f, 1.85f, -6.7f), new Vector3(8.3f, 1.1f, 2.9f), glass, parent);
        CreateDisplayPlinth("Showcase Plinth A", new Vector3(8.3f, 1.2f, -6.6f), sage, dark, parent);
        CreateDisplayPlinth("Showcase Plinth B", new Vector3(11f, 1.2f, -6.35f), terracotta, dark, parent);
        CreateBagProduct("Arc Mini Bag", 148000, new Vector3(8.3f, 2.35f, -6.6f), sage, parent);
        CreateBagProduct("Terracotta Case", 174000, new Vector3(11f, 2.35f, -6.35f), terracotta, parent);
        CreateWorldLabel("OBJECTS", new Vector3(5.15f, 0.72f, -8.43f), Quaternion.identity, 0.28f, WarmWhite, parent);
    }

    private static void BuildNPCVisuals(Transform parent, Material skin, Material hair, Material dark,
        Material cream, Material sage, Material terracotta, Material denim)
    {
        GameObject npcRoot = new GameObject("NPCs");
        npcRoot.transform.SetParent(parent, false);

        CreateHumanoid("Checkout Clerk", new Vector3(0.3f, 0f, 7.45f), Quaternion.Euler(0f, 180f, 0f),
            sage, dark, skin, hair, cream, cream, npcRoot.transform, true);
        CreateHumanoid("Personal Shopper", new Vector3(-11.85f, 0f, -5.65f), Quaternion.Euler(0f, 45f, 0f),
            terracotta, denim, skin, hair, dark, cream, npcRoot.transform, false);
    }

    private static GameObject CreateHumanoid(string name, Vector3 position, Quaternion rotation,
        Material top, Material trousers, Material skin, Material hair, Material shoes, Material accent,
        Transform parent, bool wearsApron)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.rotation = rotation;

        CapsuleCollider interactionCollider = root.AddComponent<CapsuleCollider>();
        interactionCollider.center = new Vector3(0f, 1.075f, 0f);
        interactionCollider.height = 2.15f;
        interactionCollider.radius = 0.4f;

        CreateNPCPart(PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.18f, 0f),
            new Vector3(0.42f, 0.43f, 0.27f), top, root.transform);
        CreateNPCPart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.82f, 0f),
            new Vector3(0.46f, 0.52f, 0.44f), skin, root.transform);
        CreateNPCPart(PrimitiveType.Sphere, "Hair", new Vector3(0f, 2.02f, 0.015f),
            new Vector3(0.49f, 0.28f, 0.47f), hair, root.transform);
        CreateNPCPart(PrimitiveType.Sphere, "Left Eye", new Vector3(-0.105f, 1.87f, 0.208f),
            new Vector3(0.052f, 0.052f, 0.035f), hair, root.transform);
        CreateNPCPart(PrimitiveType.Sphere, "Right Eye", new Vector3(0.105f, 1.87f, 0.208f),
            new Vector3(0.052f, 0.052f, 0.035f), hair, root.transform);

        GameObject leftArm = CreateNPCPart(PrimitiveType.Capsule, "Left Arm", new Vector3(-0.39f, 1.2f, 0f),
            new Vector3(0.13f, 0.37f, 0.13f), top, root.transform);
        leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        GameObject rightArm = CreateNPCPart(PrimitiveType.Capsule, "Right Arm", new Vector3(0.39f, 1.2f, 0f),
            new Vector3(0.13f, 0.37f, 0.13f), top, root.transform);
        rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        CreateNPCPart(PrimitiveType.Sphere, "Left Hand", new Vector3(-0.44f, 0.87f, 0f),
            new Vector3(0.2f, 0.2f, 0.2f), skin, root.transform);
        CreateNPCPart(PrimitiveType.Sphere, "Right Hand", new Vector3(0.44f, 0.87f, 0f),
            new Vector3(0.2f, 0.2f, 0.2f), skin, root.transform);

        CreateNPCPart(PrimitiveType.Capsule, "Left Leg", new Vector3(-0.17f, 0.5f, 0f),
            new Vector3(0.16f, 0.42f, 0.16f), trousers, root.transform);
        CreateNPCPart(PrimitiveType.Capsule, "Right Leg", new Vector3(0.17f, 0.5f, 0f),
            new Vector3(0.16f, 0.42f, 0.16f), trousers, root.transform);
        CreateNPCPart(PrimitiveType.Cube, "Left Shoe", new Vector3(-0.17f, 0.1f, 0.06f),
            new Vector3(0.28f, 0.16f, 0.48f), shoes, root.transform);
        CreateNPCPart(PrimitiveType.Cube, "Right Shoe", new Vector3(0.17f, 0.1f, 0.06f),
            new Vector3(0.28f, 0.16f, 0.48f), shoes, root.transform);

        if (wearsApron)
        {
            CreateNPCPart(PrimitiveType.Cube, "Apron", new Vector3(0f, 1.06f, 0.245f),
                new Vector3(0.55f, 0.7f, 0.035f), accent, root.transform);
        }
        else
        {
            CreateNPCPart(PrimitiveType.Cube, "Name Badge", new Vector3(0.19f, 1.35f, 0.255f),
                new Vector3(0.22f, 0.11f, 0.025f), accent, root.transform);
        }

        return root;
    }

    private static GameObject CreateNPCPart(PrimitiveType type, string name, Vector3 localPosition,
        Vector3 localScale, Material material, Transform parent)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static GameObject BuildPlayer(InputActionReference move, InputActionReference look, InputActionReference rightClick)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = ExteriorStartPosition;
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.75f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.88f, 0f);
        controller.stepOffset = 0.3f;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 65f;
        camera.nearClipPlane = 0.05f;
        cameraObject.AddComponent<AudioListener>();

        GameObject anchor = new GameObject("FocusAnchor");
        anchor.transform.SetParent(cameraObject.transform, false);
        anchor.transform.localPosition = new Vector3(0f, -0.05f, 2.25f);

        CS_PlayerLocomotion locomotion = player.AddComponent<CS_PlayerLocomotion>();
        SetObjectReference(locomotion, "moveAction", move);
        SetObjectReference(locomotion, "lookAction", look);
        SetObjectReference(locomotion, "rightClickAction", rightClick);
        SetObjectReference(locomotion, "viewTransform", cameraObject.transform);
        SetFloat(locomotion, "lookSensitivity", DefaultLookSensitivity);
        return player;
    }

    private static void BuildUI(out CS_CartCounter cartCounter, out CS_FocusUIController focusUI,
        out CS_CheckoutUIController checkoutUI, out CS_PersonalShopperUI personalShopperUI)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new GameObject("Shopping UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject guide = CreateUIBox("Controls", canvasObject.transform, new Color(0.04f, 0.05f, 0.06f, 0.78f),
            new Vector2(34f, -34f), new Vector2(420f, 82f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateUIText("Guide Text", guide.transform, "WASD  Move   |   Right-drag  Look around\nClick a product to inspect", font, 23,
            WarmWhite, TextAnchor.MiddleLeft, new Vector2(20f, 0f), new Vector2(-18f, 0f), Vector2.zero, Vector2.one);

        GameObject cart = CreateUIBox("Cart", canvasObject.transform, new Color(0.04f, 0.05f, 0.06f, 0.92f),
            new Vector2(-34f, -34f), new Vector2(190f, 64f), Vector2.one, Vector2.one);
        CreateUIText("Cart Label", cart.transform, "CART", font, 24, WarmWhite, TextAnchor.MiddleLeft,
            new Vector2(22f, 0f), new Vector2(-70f, 0f), Vector2.zero, Vector2.one);
        GameObject badge = CreateUIBox("Badge", cart.transform, Terracotta, new Vector2(-14f, 0f), new Vector2(48f, 48f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        Text countText = CreateUIText("Count", badge.transform, "0", font, 24, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        cartCounter = cart.AddComponent<CS_CartCounter>();
        SetObjectReference(cartCounter, "countText", countText);
        SetObjectReference(cartCounter, "badgeRoot", badge);

        GameObject controllerObject = new GameObject("Focus UI Controller");
        controllerObject.transform.SetParent(canvasObject.transform, false);
        focusUI = controllerObject.AddComponent<CS_FocusUIController>();

        GameObject focusPanel = new GameObject("Focus Panel", typeof(RectTransform));
        focusPanel.transform.SetParent(canvasObject.transform, false);
        Stretch(focusPanel.GetComponent<RectTransform>());

        GameObject dim = CreateUIBox("Backdrop", focusPanel.transform, new Color(0.02f, 0.025f, 0.03f, 0.32f),
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        Stretch(dim.GetComponent<RectTransform>());
        dim.GetComponent<Image>().raycastTarget = false;

        GameObject info = CreateUIBox("Product Info", focusPanel.transform, new Color(0.04f, 0.05f, 0.06f, 0.93f),
            new Vector2(0f, -38f), new Vector2(520f, 118f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Text nameText = CreateUIText("Product Name", info.transform, "PRODUCT", font, 34, WarmWhite, TextAnchor.MiddleCenter,
            new Vector2(0f, 18f), new Vector2(0f, -54f), Vector2.zero, Vector2.one);
        Text priceText = CreateUIText("Price", info.transform, "$0.00", font, 24, new Color(0.83f, 0.67f, 0.39f), TextAnchor.MiddleCenter,
            new Vector2(0f, -28f), new Vector2(0f, -56f), Vector2.zero, Vector2.one);

        Button exitButton = CreateUIButton("Exit", focusPanel.transform, "EXIT  ESC", font, new Vector2(-188f, 60f),
            new Vector2(250f, 72f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.12f, 0.13f, 0.15f, 0.95f));
        exitButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Button cartButton = CreateUIButton("Add To Cart", focusPanel.transform, "ADD TO CART", font, new Vector2(133f, 60f),
            new Vector2(360f, 72f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Terracotta);
        cartButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        ConfigureAddToCartContent(cartButton);
        CreateUIText("Rotate Hint", focusPanel.transform, "Click and drag the product to rotate", font, 22, WarmWhite,
            TextAnchor.MiddleCenter, new Vector2(0f, 134f), new Vector2(520f, 40f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        SetObjectReference(focusUI, "focusPanel", focusPanel);
        SetObjectReference(focusUI, "productNameText", nameText);
        SetObjectReference(focusUI, "priceText", priceText);
        SetObjectReference(focusUI, "addToCartButton", cartButton);
        SetObjectReference(focusUI, "exitButton", exitButton);
        focusPanel.SetActive(false);

        checkoutUI = BuildCheckoutUI(canvasObject.transform, font, cartCounter);
        personalShopperUI = BuildPersonalShopperUI(canvasObject.transform, font);
    }

    private static CS_CheckoutUIController BuildCheckoutUI(Transform canvas, Font font, CS_CartCounter cartCounter)
    {
        GameObject controllerObject = new GameObject("Checkout UI Controller");
        controllerObject.transform.SetParent(canvas, false);
        CS_CheckoutUIController controller = controllerObject.AddComponent<CS_CheckoutUIController>();

        Button promptButton = CreateUIButton("Checkout Prompt", canvas, "Check out?", font,
            new Vector2(0f, 172f), new Vector2(300f, 72f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), Terracotta);
        Text promptText = promptButton.transform.Find("Label").GetComponent<Text>();

        GameObject reviewRoot = CreateUIBox("Checkout Review", canvas, new Color(0.02f, 0.025f, 0.03f, 0.78f),
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        Stretch(reviewRoot.GetComponent<RectTransform>());
        GameObject reviewPanel = CreateUIBox("Review Panel", reviewRoot.transform, new Color(0.04f, 0.05f, 0.06f, 0.98f),
            Vector2.zero, new Vector2(700f, 690f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        CreateUIText("Title", reviewPanel.transform, "REVIEW YOUR CART", font, 34, WarmWhite,
            TextAnchor.MiddleCenter, new Vector2(0f, -38f), new Vector2(-48f, 64f),
            new Vector2(0f, 1f), Vector2.one);

        GameObject divider = CreateUIBox("Header Divider", reviewPanel.transform, Terracotta,
            new Vector2(0f, -88f), new Vector2(-48f, 2f), new Vector2(0f, 1f), Vector2.one);
        divider.GetComponent<Image>().raycastTarget = false;

        GameObject viewport = CreateUIBox("Viewport", reviewPanel.transform, Color.clear, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.one);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.offsetMin = new Vector2(32f, 178f);
        viewportRect.offsetMax = new Vector2(-32f, -108f);
        viewport.AddComponent<RectMask2D>();

        GameObject contentObject = new GameObject("Checkout Items", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        RectTransform itemContent = contentObject.GetComponent<RectTransform>();
        itemContent.anchorMin = new Vector2(0f, 1f);
        itemContent.anchorMax = Vector2.one;
        itemContent.pivot = new Vector2(0.5f, 1f);
        itemContent.anchoredPosition = Vector2.zero;
        itemContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = itemContent;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        Text emptyText = CreateUIText("Empty Cart", viewport.transform, "Your cart is empty", font, 22,
            new Color(WarmWhite.r, WarmWhite.g, WarmWhite.b, 0.68f), TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        Text totalText = CreateUIText("Checkout Total", reviewPanel.transform, "TOTAL   $0.00", font, 25,
            Brass, TextAnchor.MiddleRight, new Vector2(166f, 126f), new Vector2(290f, 40f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        Button shopMoreButton = CreateUIButton("Shop More", reviewPanel.transform, "SHOP MORE", font,
            new Vector2(-166f, 42f), new Vector2(290f, 72f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Color(0.12f, 0.13f, 0.15f, 1f));
        Button checkoutButton = CreateUIButton("Confirm Checkout", reviewPanel.transform, "CHECK OUT", font,
            new Vector2(166f, 42f), new Vector2(290f, 72f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), Terracotta);

        GameObject thankYouRoot = CreateUIBox("Checkout Thanks", canvas, new Color(0.04f, 0.05f, 0.06f, 0.97f),
            Vector2.zero, new Vector2(540f, 116f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Text thankYouText = CreateUIText("Message", thankYouRoot.transform, "Thanks for shopping!", font, 30,
            WarmWhite, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(-36f, -24f), Vector2.zero, Vector2.one);

        SetObjectReference(controller, "cartCounter", cartCounter);
        SetObjectReference(controller, "promptRoot", promptButton.gameObject);
        SetObjectReference(controller, "promptText", promptText);
        SetObjectReference(controller, "promptButton", promptButton);
        SetObjectReference(controller, "reviewRoot", reviewRoot);
        SetObjectReference(controller, "itemContent", itemContent);
        SetObjectReference(controller, "emptyCartText", emptyText);
        SetObjectReference(controller, "totalText", totalText);
        SetObjectReference(controller, "shopMoreButton", shopMoreButton);
        SetObjectReference(controller, "shopMoreText", shopMoreButton.transform.Find("Label").GetComponent<Text>());
        SetObjectReference(controller, "checkoutButton", checkoutButton);
        SetObjectReference(controller, "checkoutText", checkoutButton.transform.Find("Label").GetComponent<Text>());
        SetObjectReference(controller, "thankYouRoot", thankYouRoot);
        SetObjectReference(controller, "thankYouText", thankYouText);

        promptButton.gameObject.SetActive(false);
        reviewRoot.SetActive(false);
        thankYouRoot.SetActive(false);
        return controller;
    }

    private static CS_PersonalShopperUI BuildPersonalShopperUI(Transform canvas, Font font)
    {
        GameObject controllerObject = new GameObject("Personal Shopper UI Controller");
        controllerObject.transform.SetParent(canvas, false);
        CS_PersonalShopperUI controller = controllerObject.AddComponent<CS_PersonalShopperUI>();

        GameObject conversation = CreateUIBox("Shopper Conversation", canvas,
            new Color(0.04f, 0.05f, 0.06f, 0.98f), new Vector2(0f, -74f), new Vector2(680f, 300f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Text prompt = CreateUIText("Prompt", conversation.transform, "How can I help?", font, 32,
            WarmWhite, TextAnchor.MiddleCenter, new Vector2(0f, -48f), new Vector2(-48f, 72f),
            new Vector2(0f, 1f), Vector2.one);
        Button primaryButton = CreateUIButton("Primary Choice", conversation.transform,
            "Help me choose an outfit!", font, new Vector2(0f, 104f), new Vector2(580f, 70f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Terracotta);
        Text primaryLabel = primaryButton.transform.Find("Label").GetComponent<Text>();
        Button secondaryButton = CreateUIButton("Secondary Choice", conversation.transform, "Never mind", font,
            new Vector2(0f, 28f), new Vector2(580f, 58f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Color(0.12f, 0.13f, 0.15f, 1f));
        Text secondaryLabel = secondaryButton.transform.Find("Label").GetComponent<Text>();

        GameObject ambientBubble = CreateUIBox("Shopper Ambient Bubble", canvas,
            new Color(0.04f, 0.05f, 0.06f, 0.94f), new Vector2(34f, 128f), new Vector2(440f, 88f),
            new Vector2(0f, 0f), new Vector2(0f, 0f));
        Text ambientText = CreateUIText("Ambient Message", ambientBubble.transform,
            "Welcome to the shop!", font, 23, WarmWhite, TextAnchor.MiddleLeft,
            new Vector2(22f, 0f), new Vector2(-36f, -18f), Vector2.zero, Vector2.one);
        CreateSpeechTail(ambientBubble.transform, new Vector2(24f, -52f), darkFacingDown: true);

        GameObject commentBubble = CreateUIBox("Product Comment Bubble", canvas,
            new Color(0.04f, 0.05f, 0.06f, 0.96f), new Vector2(38f, 0f), new Vector2(430f, 190f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        Text commentText = CreateUIText("Product Comment", commentBubble.transform,
            "That piece is a flexible choice.", font, 24, WarmWhite, TextAnchor.MiddleLeft,
            new Vector2(24f, 0f), new Vector2(-46f, -34f), Vector2.zero, Vector2.one);
        CreateSpeechTail(commentBubble.transform, new Vector2(14f, 0f), darkFacingDown: false);

        controller.Configure(null, conversation, prompt, primaryButton, primaryLabel, secondaryButton,
            secondaryLabel, ambientBubble, ambientText, commentBubble, commentText);
        conversation.SetActive(false);
        ambientBubble.SetActive(false);
        commentBubble.SetActive(false);
        return controller;
    }

    private static void CreateSpeechTail(Transform parent, Vector2 position, bool darkFacingDown)
    {
        GameObject tail = CreateUIBox("Speech Tail", parent, new Color(0.04f, 0.05f, 0.06f, 0.96f),
            position, new Vector2(28f, 28f), darkFacingDown ? new Vector2(0f, 0f) : new Vector2(1f, 0.5f),
            darkFacingDown ? new Vector2(0f, 0f) : new Vector2(1f, 0.5f));
        RectTransform rect = tail.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tail.GetComponent<Image>().raycastTarget = false;
    }

    private static void BuildEventSystem(InputActionReference point, InputActionReference click,
        InputActionReference rightClick, InputActionReference cancel, InputActionReference move, InputActionReference submit)
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        InputSystemUIInputModule module = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        module.point = point;
        module.leftClick = click;
        module.rightClick = rightClick;
        module.cancel = cancel;
        module.move = move;
        module.submit = submit;
    }

    private static void BuildLighting(Transform parent, Material fixture, Material diffuser)
    {
        GameObject sunObject = new GameObject("Sun");
        sunObject.transform.SetParent(parent, false);
        sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.35f;
        sun.color = new Color(1f, 0.91f, 0.78f);
        sun.shadows = LightShadows.None;

        Vector3[] positions = GetCeilingLightPositions();
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject lightObject = new GameObject("Ceiling Light " + (i + 1));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = positions[i];
            lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            ConfigureCeilingLight(light);
            light.shadows = LightShadows.None;
            CreateCeilingFixture(i + 1, positions[i], fixture, diffuser, parent);
        }
    }

    private static int HideAllWordingUI(Scene scene)
    {
        int hiddenCanvases = 0;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.scene != scene)
            {
                continue;
            }

            canvases[i].gameObject.SetActive(false);
            hiddenCanvases++;
        }

        TextMesh[] worldLabels = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < worldLabels.Length; i++)
        {
            if (worldLabels[i].gameObject.scene == scene)
            {
                worldLabels[i].gameObject.SetActive(false);
            }
        }

        return hiddenCanvases;
    }

    private static void EnsureCeilingAndLighting(Transform environment, Material ceiling,
        Material fixture, Material diffuser)
    {
        if (environment.Find("Ceiling") == null)
        {
            CreateCube("Ceiling", new Vector3(0f, 5.1f, 0f), new Vector3(28f, 0.2f, 18f),
                ceiling, environment);
        }

        Transform sunTransform = environment.Find("Sun");
        Light sun = sunTransform != null ? sunTransform.GetComponent<Light>() : null;
        if (sun != null)
        {
            sun.intensity = 0.35f;
            sun.shadows = LightShadows.None;
        }

        Vector3[] positions = GetCeilingLightPositions();
        for (int i = 0; i < positions.Length; i++)
        {
            Transform lightTransform = environment.Find("Ceiling Light " + (i + 1));
            if (lightTransform != null)
            {
                lightTransform.position = positions[i];
                lightTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
                ConfigureCeilingLight(lightTransform.GetComponent<Light>());
            }

            Transform fixtureTransform = environment.Find("Ceiling Fixture " + (i + 1));
            if (fixtureTransform == null)
            {
                CreateCeilingFixture(i + 1, positions[i], fixture, diffuser, environment);
            }
            else
            {
                Renderer lens = fixtureTransform.Find("Diffuser")?.GetComponent<Renderer>();
                if (lens != null)
                {
                    lens.sharedMaterial = diffuser;
                }
            }
        }

        ApplyInteriorAmbient();
    }

    private static void ApplyInteriorAmbient()
    {
        // Darker ambient lets the warm spot pools read on the floor and racks.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.43f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.22f, 0.2f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.08f, 0.07f);
    }

    private static Vector3[] GetCeilingLightPositions()
    {
        return new[]
        {
            new Vector3(-8f, 4.6f, 3f), new Vector3(0f, 4.6f, 3f), new Vector3(8f, 4.6f, 3f),
            new Vector3(-8f, 4.6f, -4f), new Vector3(0f, 4.6f, -4f), new Vector3(8f, 4.6f, -4f)
        };
    }

    private static void ConfigureCeilingLight(Light light)
    {
        if (light == null)
        {
            return;
        }

        light.type = LightType.Spot;
        light.range = 9f;
        light.spotAngle = 95f;
        light.innerSpotAngle = 55f;
        light.intensity = CeilingLightIntensity;
        light.color = new Color(1f, 0.86f, 0.72f);
        light.shadows = LightShadows.None;
    }

    private static void CreateCeilingFixture(int index, Vector3 lightPosition, Material fixture,
        Material diffuser, Transform parent)
    {
        GameObject root = new GameObject("Ceiling Fixture " + index);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(lightPosition.x, 4.92f, lightPosition.z);

        GameObject trim = CreateCylinder("Trim", Vector3.zero, new Vector3(0.42f, 0.06f, 0.42f),
            fixture, root.transform, true);
        GameObject lens = CreateCylinder("Diffuser", new Vector3(0f, -0.08f, 0f),
            new Vector3(0.31f, 0.025f, 0.31f), diffuser, root.transform, true);
        Object.DestroyImmediate(trim.GetComponent<Collider>());
        Object.DestroyImmediate(lens.GetComponent<Collider>());
    }

    private static void ApplySavedLayout(Transform environment)
    {
        // Preserve the layout adjustments made in the Unity editor when the scene is rebuilt.
        RepairStandaloneProductVisuals(environment);

        SetCabinetParts(environment, "Display Cabinet A", new Vector3(0f, 0f, 3.04f), new Vector3(0f, 0.1f, 2.65f));
        SetCabinetParts(environment, "Display Cabinet B", new Vector3(-8.87f, 0f, 0.32f), new Vector3(-8.87f, 0.1f, 0.03f));
        SetCabinetParts(environment, "Front Cabinet A", new Vector3(0f, 0f, 3.28f), new Vector3(0f, 0.1f, 2.76f));
        SetCabinetParts(environment, "Front Cabinet B", new Vector3(-8.87f, 0f, 0.42f), new Vector3(-8.87f, 0.1f, -0.2f));

        Transform centerRack = environment.Find("Center Round Rack");
        Vector3 centerPartOffset = new Vector3(-0.99f, 0f, -0.771f);
        centerRack.Find("Round Base").localPosition += centerPartOffset;
        centerRack.Find("Center Post").localPosition += centerPartOffset;
        centerRack.Find("Top Ring").localPosition += centerPartOffset;
        Vector3[] roundGarmentOffsets =
        {
            new Vector3(-0.99f, 0f, -0.771f), new Vector3(-0.15f, 0f, -1.25f),
            new Vector3(0.77f, 0f, -0.99f), new Vector3(1.25f, 0f, -0.15f),
            new Vector3(0.99f, 0f, 0.77f), new Vector3(0.15f, 0f, 1.25f),
            new Vector3(-0.77f, 0f, 0.99f), new Vector3(-1.25f, 0f, 0.15f)
        };
        for (int i = 0; i < roundGarmentOffsets.Length; i++)
        {
            foreach (Transform part in centerRack.Find("Round Garment " + (i + 1)))
            {
                part.localPosition += roundGarmentOffsets[i];
            }
        }

        Transform leftWallRack = environment.Find("Left Wall Rack");
        leftWallRack.Find("Back").localPosition = new Vector3(0f, 1.45f, -0.04f);
        Transform leftRail = leftWallRack.Find("Rail");
        leftRail.localPosition = new Vector3(0f, 2.05f, 0.19f);
        leftRail.localRotation = new Quaternion(-0.7071068f, -0.7071068f, 0.00000035762787f, 0.00000035762787f);
        for (int i = 0; i < 7; i++)
        {
            foreach (Transform part in leftWallRack.Find("Wall Garment " + (i + 1)))
            {
                part.localPosition += new Vector3(0f, 0f, 0.38f);
            }
        }

        Transform rightRail = environment.Find("Right Wall Rack/Rail");
        rightRail.localPosition = new Vector3(0f, 2.05f, 0.24f);
        rightRail.localRotation = new Quaternion(-0.7071065f, -0.7071065f, 0.0006731151f, 0.0006731151f);

        Transform fittingLabel = environment.Find("FITTING Label");
        fittingLabel.position = new Vector3(10.5f, 3.7f, 3.44f);
        fittingLabel.rotation = Quaternion.identity;
        Transform newFormLabel = FindDescendantOrNull(environment, "NEW / FORM Label");
        if (newFormLabel != null)
        {
            newFormLabel.rotation = Quaternion.identity;
        }
        foreach (Transform child in environment)
        {
            if (child.name == "SHOP / EDIT Label" && child.position.z > 0f)
            {
                child.rotation = Quaternion.identity;
                break;
            }
        }
    }

    private static void RepairStandaloneProductVisuals(Transform environment)
    {
        // Standalone products use their root as the authored display pivot. Keep procedural
        // visual parts local to that root so the mesh stays on its plinth/rack in Explore mode.
        string[] shirtNames =
        {
            "Ivory Knit", "Sage Overshirt", "Clay Jacket", "Cream Blouse", "Charcoal Coat",
            "Midnight Shirt", "Sand Cardigan"
        };
        for (int i = 0; i < shirtNames.Length; i++)
        {
            Transform shirt = environment.Find(shirtNames[i]);
            if (shirt == null)
            {
                continue;
            }

            Transform placeholder = shirt.Find("Model Placeholder");
            if (placeholder != null)
            {
                placeholder.localPosition = Vector3.zero;
                continue;
            }

            SetLocalPosition(shirt, "Torso", Vector3.zero);
            SetLocalPosition(shirt, "Sleeve L", new Vector3(-0.62f, 0.27f, 0f));
            SetLocalPosition(shirt, "Sleeve R", new Vector3(0.62f, 0.27f, 0f));
            SetLocalPosition(shirt, "Collar", new Vector3(0f, 0.62f, 0f));
        }

        string[] bagNames = { "Studio Tote", "Arc Mini Bag", "Terracotta Case" };
        for (int i = 0; i < bagNames.Length; i++)
        {
            Transform bag = environment.Find(bagNames[i]);
            SetLocalPosition(bag, "Bag Body", Vector3.zero);
            SetLocalPosition(bag, "Handle L", new Vector3(-0.36f, 0.55f, 0f));
            SetLocalPosition(bag, "Handle R", new Vector3(0.36f, 0.55f, 0f));
        }
    }

    private static void SetLocalPosition(Transform parent, string childName, Vector3 localPosition)
    {
        if (parent == null)
        {
            throw new System.InvalidOperationException($"ShoppingVR layout repair failed: product '{childName}' parent was not found.");
        }

        Transform child = parent.Find(childName);
        if (child == null)
        {
            throw new System.InvalidOperationException($"ShoppingVR layout repair failed: '{parent.name}/{childName}' was not found.");
        }

        child.localPosition = localPosition;
    }

    private static void SetCabinetParts(Transform environment, string cabinetName, Vector3 bodyPosition,
        Vector3 shelfPosition)
    {
        Transform cabinet = environment.Find(cabinetName);
        cabinet.Find("Body").position = bodyPosition;
        cabinet.Find("Shelf").position = shelfPosition;
    }

    private static void CreateRack(string name, Vector3 origin, int itemCount, Material dark, Material metal,
        Material a, Material b, Material c, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = origin;
        CreateCube("Base", new Vector3(0f, 0.08f, 0f), new Vector3(5.7f, 0.16f, 1.25f), dark, root.transform, true);
        CreateCylinder("Post L", new Vector3(-2.65f, 1.2f, 0f), new Vector3(0.08f, 1.2f, 0.08f), metal, root.transform, true);
        CreateCylinder("Post R", new Vector3(2.65f, 1.2f, 0f), new Vector3(0.08f, 1.2f, 0.08f), metal, root.transform, true);
        CreateCylinder("Rail", new Vector3(0f, 2.2f, 0f), new Vector3(0.07f, 2.65f, 0.07f), metal, root.transform, true).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        for (int i = 0; i < itemCount; i++)
        {
            float x = Mathf.Lerp(-2.2f, 2.2f, itemCount == 1 ? 0.5f : i / (itemCount - 1f));
            Material material = i % 3 == 0 ? a : i % 3 == 1 ? b : c;
            CreateGarmentSilhouette("Garment " + (i + 1), new Vector3(x, 1.55f, 0f), material, root.transform);
        }
    }

    private static void CreateWallRack(string name, Vector3 origin, int itemCount, bool rotate,
        Material dark, Material metal, Material a, Material b, Material c, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = origin;
        if (rotate) root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        CreateCube("Back", new Vector3(0f, 1.45f, 0.45f), new Vector3(7.3f, 2.9f, 0.18f), dark, root.transform, true);
        CreateCylinder("Rail", new Vector3(0f, 2.05f, 0f), new Vector3(0.06f, 3.35f, 0.06f), metal, root.transform, true).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        for (int i = 0; i < itemCount; i++)
        {
            float x = Mathf.Lerp(-2.9f, 2.9f, i / (itemCount - 1f));
            CreateGarmentSilhouette("Wall Garment " + (i + 1), new Vector3(x, 1.35f, -0.04f),
                i % 3 == 0 ? a : i % 3 == 1 ? b : c, root.transform);
        }
    }

    private static void CreateRoundRack(string name, Vector3 origin, Material dark, Material metal,
        Material a, Material b, Material c, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = origin;
        CreateCylinder("Round Base", new Vector3(0f, 0.1f, 0f), new Vector3(1.8f, 0.1f, 1.8f), dark, root.transform, true);
        CreateCylinder("Center Post", new Vector3(0f, 1.15f, 0f), new Vector3(0.09f, 1.15f, 0.09f), metal, root.transform, true);
        CreateCylinder("Top Ring", new Vector3(0f, 2.15f, 0f), new Vector3(1.55f, 0.08f, 1.55f), metal, root.transform, true);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            float r = 1.35f;
            Vector3 p = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * r, 1.42f, Mathf.Cos(angle * Mathf.Deg2Rad) * r);
            GameObject garment = CreateGarmentSilhouette("Round Garment " + (i + 1), p, i % 3 == 0 ? a : i % 3 == 1 ? b : c, root.transform);
            garment.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    private static GameObject CreateGarmentSilhouette(string name, Vector3 localPosition, Material material, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        CreateCube("Model Placeholder", Vector3.zero, new Vector3(0.48f, 1.1f, 0.16f), material, root.transform, true);
        string displayName = parent.name + " " + name;
        int price = 59000 + (generatedGarmentIndex % 8) * 10000;
        generatedGarmentIndex++;
        FinalizeProduct(root, displayName, price);
        return root;
    }

    private static int AddProductItemsToGarments()
    {
        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<Transform> garments = new List<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            string objectName = allTransforms[i].name;
            if ((objectName.StartsWith("Garment ") || objectName.StartsWith("Wall Garment ") ||
                 objectName.StartsWith("Round Garment ")) && allTransforms[i].GetComponent<CS_ProductItem>() == null)
            {
                garments.Add(allTransforms[i]);
            }
        }

        garments.Sort((a, b) => string.CompareOrdinal(GetHierarchyPath(a), GetHierarchyPath(b)));
        for (int i = 0; i < garments.Count; i++)
        {
            GameObject garment = garments[i].gameObject;
            SetLayerRecursively(garment, ProductLayer);
            CS_ProductItem product = Undo.AddComponent<CS_ProductItem>(garment);
            string displayName = garments[i].parent.name + " " + garment.name;
            int price = 59000 + (i % 8) * 10000;
            product.Configure(displayName, price, garment.GetComponentsInChildren<Collider>(true),
                garment.GetComponentsInChildren<Renderer>(true));
            EditorUtility.SetDirty(product);
        }
        return garments.Count;
    }

    private static int RemoveDuplicateSceneControllers()
    {
        int removed = 0;
        removed += KeepOnlyExpectedComponent<CS_PlayerLocomotion>("Player");
        removed += KeepOnlyExpectedComponent<CS_ProductFocusController>("Player");
        removed += KeepOnlyExpectedComponent<CS_CartCounter>("Cart");
        removed += KeepOnlyExpectedComponent<CS_FocusUIController>("Focus UI Controller");
        return removed;
    }

    private static void RepairSceneControllerReferences()
    {
        GameObject player = GameObject.Find("Player");
        GameObject uiRoot = GameObject.Find("Shopping UI");
        if (player == null || uiRoot == null)
        {
            throw new System.InvalidOperationException("ShoppingVR scene repair failed: Player or Shopping UI was not found.");
        }

        Camera userCamera = player.GetComponentInChildren<Camera>(true);
        Transform focusAnchor = userCamera != null ? userCamera.transform.Find("FocusAnchor") : null;
        CS_PlayerLocomotion locomotion = player.GetComponent<CS_PlayerLocomotion>();
        CS_ProductFocusController focusController = player.GetComponent<CS_ProductFocusController>();

        Transform cartTransform = FindDescendant(uiRoot.transform, "Cart");
        Transform badgeTransform = FindDescendant(cartTransform, "Badge");
        Transform countTransform = FindDescendant(badgeTransform, "Count");
        CS_CartCounter cart = cartTransform.GetComponent<CS_CartCounter>();

        Transform uiControllerTransform = FindDescendant(uiRoot.transform, "Focus UI Controller");
        CS_FocusUIController focusUI = uiControllerTransform.GetComponent<CS_FocusUIController>();
        Transform focusPanel = FindDescendant(uiRoot.transform, "Focus Panel");
        Text nameText = FindDescendant(focusPanel, "Product Name").GetComponent<Text>();
        Text priceText = FindDescendant(focusPanel, "Price").GetComponent<Text>();
        Button addButton = FindDescendant(focusPanel, "Add To Cart").GetComponent<Button>();
        Button exitButton = FindDescendant(focusPanel, "Exit").GetComponent<Button>();

        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        InputActionReference move = GetActionReference(inputAsset, "Player/Move");
        InputActionReference look = GetActionReference(inputAsset, "Player/Look");
        InputActionReference rightClick = GetActionReference(inputAsset, "UI/RightClick");
        InputActionReference point = GetActionReference(inputAsset, "UI/Point");
        InputActionReference click = GetActionReference(inputAsset, "UI/Click");
        InputActionReference cancel = GetActionReference(inputAsset, "UI/Cancel");

        SetObjectReference(locomotion, "moveAction", move);
        SetObjectReference(locomotion, "lookAction", look);
        SetObjectReference(locomotion, "rightClickAction", rightClick);
        SetObjectReference(locomotion, "viewTransform", userCamera.transform);
        SetFloat(locomotion, "lookSensitivity", DefaultLookSensitivity);

        focusController.Configure(userCamera, focusAnchor, locomotion, focusUI, cart, 1 << ProductLayer);
        SetObjectReference(focusController, "pointAction", point);
        SetObjectReference(focusController, "clickAction", click);
        SetObjectReference(focusController, "cancelAction", cancel);

        SetObjectReference(cart, "countText", countTransform.GetComponent<Text>());
        SetObjectReference(cart, "badgeRoot", badgeTransform.gameObject);

        focusUI.Configure(focusController);
        SetObjectReference(focusUI, "focusPanel", focusPanel.gameObject);
        SetObjectReference(focusUI, "productNameText", nameText);
        SetObjectReference(focusUI, "priceText", priceText);
        SetObjectReference(focusUI, "addToCartButton", addButton);
        SetObjectReference(focusUI, "exitButton", exitButton);
        EditorUtility.SetDirty(locomotion);
        EditorUtility.SetDirty(focusController);
        EditorUtility.SetDirty(cart);
        EditorUtility.SetDirty(focusUI);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
        {
            throw new System.InvalidOperationException($"ShoppingVR scene repair failed while finding '{objectName}'.");
        }
        if (root.name == objectName)
        {
            return root;
        }
        foreach (Transform child in root)
        {
            Transform found = FindDescendantOrNull(child, objectName);
            if (found != null)
            {
                return found;
            }
        }
        throw new System.InvalidOperationException($"ShoppingVR scene repair failed: '{objectName}' was not found.");
    }

    private static Transform FindDescendantOrNull(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }
        foreach (Transform child in root)
        {
            Transform found = FindDescendantOrNull(child, objectName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static int KeepOnlyExpectedComponent<T>(string expectedObjectName) where T : Component
    {
        T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        T keep = null;
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.name == expectedObjectName)
            {
                keep = components[i];
                break;
            }
        }

        if (keep == null && components.Length > 0)
        {
            keep = components[0];
        }

        int removed = 0;
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != keep)
            {
                Undo.DestroyObjectImmediate(components[i]);
                removed++;
            }
        }
        return removed;
    }

    private static void UpdateEnglishUI()
    {
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            switch (texts[i].gameObject.name)
            {
                case "Guide Text":
                    texts[i].text = "WASD  Move   |   Right-drag  Look around\nClick a product to inspect";
                    break;
                case "Price":
                    texts[i].text = "$0.00";
                    break;
                case "Rotate Hint":
                    texts[i].text = "Click and drag the product to rotate";
                    break;
                case "Label" when texts[i].transform.parent != null && texts[i].transform.parent.name == "Exit":
                    texts[i].text = "EXIT  ESC";
                    break;
                case "Label" when texts[i].transform.parent != null && texts[i].transform.parent.name == "Add To Cart":
                    texts[i].text = "ADD TO CART";
                    break;
            }
            EditorUtility.SetDirty(texts[i]);
        }
    }

    private static void UpdateFocusActionLayout()
    {
        GameObject uiRoot = GameObject.Find("Shopping UI");
        if (uiRoot == null)
        {
            throw new System.InvalidOperationException("ShoppingVR scene update failed: Shopping UI was not found.");
        }
        Transform focusPanel = FindDescendant(uiRoot.transform, "Focus Panel");

        Button exitButton = FindDescendant(focusPanel, "Exit").GetComponent<Button>();
        RectTransform exitRect = exitButton.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0.5f, 0f);
        exitRect.anchorMax = new Vector2(0.5f, 0f);
        exitRect.pivot = new Vector2(0.5f, 0.5f);
        exitRect.anchoredPosition = new Vector2(-188f, 60f);
        exitRect.sizeDelta = new Vector2(250f, 72f);

        Button cartButton = FindDescendant(focusPanel, "Add To Cart").GetComponent<Button>();
        RectTransform cartRect = cartButton.GetComponent<RectTransform>();
        cartRect.anchorMin = new Vector2(0.5f, 0f);
        cartRect.anchorMax = new Vector2(0.5f, 0f);
        cartRect.pivot = new Vector2(0.5f, 0.5f);
        cartRect.anchoredPosition = new Vector2(133f, 60f);
        cartRect.sizeDelta = new Vector2(360f, 72f);

        ConfigureAddToCartContent(cartButton);
        EditorUtility.SetDirty(exitRect);
        EditorUtility.SetDirty(cartRect);
    }

    private static void ConfigureAddToCartContent(Button cartButton)
    {
        RectTransform labelRect = FindDescendant(cartButton.transform, "Label").GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(24f, 0f);
        labelRect.sizeDelta = new Vector2(-64f, 0f);

        if (FindDescendantOrNull(cartButton.transform, "Cart Icon") != null)
        {
            return;
        }

        GameObject icon = new GameObject("Cart Icon", typeof(RectTransform));
        icon.transform.SetParent(cartButton.transform, false);
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
        GameObject part = CreateUIBox(name, parent, Color.white, position, size,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform rect = part.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        part.GetComponent<Image>().raycastTarget = false;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static CS_ProductItem CreateShirtProduct(string displayName, int price, Vector3 position,
        Quaternion rotation, Material material, Transform parent)
    {
        GameObject root = new GameObject(displayName);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.rotation = rotation;
        CreateCube("Model Placeholder", Vector3.zero, new Vector3(0.9f, 1.25f, 0.28f), material, root.transform, true);
        return FinalizeProduct(root, displayName, price);
    }

    private static bool ReplaceClothingVisualWithPlaceholder(CS_ProductItem product)
    {
        if (product == null || product.transform.Find("Model Placeholder") != null)
        {
            return false;
        }

        Transform productTransform = product.transform;
        bool hasSleeve = productTransform.Find("Sleeve L") != null || productTransform.Find("Sleeve R") != null;
        bool hasTorso = productTransform.Find("Torso") != null;
        bool isRackGarment = productTransform.name.StartsWith("Garment ") ||
                             productTransform.name.StartsWith("Wall Garment ") ||
                             productTransform.name.StartsWith("Round Garment ");
        if (!hasSleeve && !hasTorso && !isRackGarment)
        {
            return false;
        }

        Renderer sourceRenderer = product.GetComponentInChildren<Renderer>(true);
        Material sourceMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
        Vector3 placeholderScale = isRackGarment
            ? new Vector3(0.48f, 1.1f, 0.16f)
            : new Vector3(0.9f, 1.25f, 0.28f);

        for (int childIndex = productTransform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Object.DestroyImmediate(productTransform.GetChild(childIndex).gameObject);
        }

        GameObject placeholder = CreateCube("Model Placeholder", Vector3.zero, placeholderScale,
            sourceMaterial, productTransform, true);
        SetLayerRecursively(placeholder, ProductLayer);
        product.Configure(product.ProductName, product.PriceWon,
            placeholder.GetComponentsInChildren<Collider>(true),
            placeholder.GetComponentsInChildren<Renderer>(true));
        EditorUtility.SetDirty(product);
        return true;
    }

    private static CS_ProductItem CreateBagProduct(string displayName, int price, Vector3 position,
        Material material, Transform parent)
    {
        GameObject root = new GameObject(displayName);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        CreateCube("Bag Body", Vector3.zero, new Vector3(1.1f, 0.78f, 0.42f), material, root.transform, true);
        CreateCylinder("Handle L", new Vector3(-0.36f, 0.55f, 0f), new Vector3(0.08f, 0.42f, 0.08f), material, root.transform, true).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        CreateCylinder("Handle R", new Vector3(0.36f, 0.55f, 0f), new Vector3(0.08f, 0.42f, 0.08f), material, root.transform, true).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        return FinalizeProduct(root, displayName, price);
    }

    private static CS_ProductItem FinalizeProduct(GameObject root, string displayName, int price)
    {
        SetLayerRecursively(root, ProductLayer);
        CS_ProductItem product = root.AddComponent<CS_ProductItem>();
        product.Configure(displayName, price, root.GetComponentsInChildren<Collider>(), root.GetComponentsInChildren<Renderer>());
        return product;
    }

    private static void CreateDisplayPlinth(string name, Vector3 position, Material material, Material trim, Transform parent)
    {
        CreateCylinder(name, position + new Vector3(0f, 0.5f, 0f), new Vector3(0.85f, 0.5f, 0.85f), material, parent);
        CreateCylinder(name + " Trim", position + new Vector3(0f, 1.02f, 0f), new Vector3(0.9f, 0.04f, 0.9f), trim, parent);
    }

    private static void CreateCabinet(string name, Vector3 position, Vector3 scale, Material body, Material trim, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        CreateCube("Body", Vector3.zero, scale, body, root.transform);
        CreateCube("Shelf", new Vector3(0f, 0.1f, -scale.z * 0.52f), new Vector3(scale.x * 0.9f, 0.1f, 0.25f), trim, root.transform);
    }

    private static void CreatePoster(string text, Vector3 position, float width, float height, Color color, Transform parent)
    {
        Material material = GetMaterial("M_Poster_" + text.Replace(" ", "_").Replace("/", "-"), color, 0f, 0.65f);
        CreateCube(text + " Poster", position, new Vector3(width, height, 0.08f), material, parent, true);
        CreateWorldLabel(text, position + new Vector3(0f, 0f, -0.06f), Quaternion.Euler(0f, 180f, 0f), 0.34f, WarmWhite, parent);
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material,
        Transform parent, bool local = false)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        if (local) gameObject.transform.localPosition = position;
        else gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
    }

    private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material,
        Transform parent, bool local = false)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        if (local) gameObject.transform.localPosition = position;
        else gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
    }

    private static void CreateWorldLabel(string text, Vector3 position, Quaternion rotation, float size, Color color, Transform parent)
    {
        GameObject label = new GameObject(text + " Label");
        label.transform.SetParent(parent, false);
        label.transform.position = position;
        label.transform.rotation = rotation;
        TextMesh mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 80;
        mesh.characterSize = size * 0.1f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
    }

    private static GameObject CreateUIBox(string name, Transform parent, Color color, Vector2 anchoredPosition,
        Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == Vector2.one ? Vector2.one : anchorMin;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        gameObject.GetComponent<Image>().color = color;
        return gameObject;
    }

    private static Text CreateUIText(string name, Transform parent, string value, Font font, int size, Color color,
        TextAnchor alignment, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        Text text = gameObject.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateUIButton(string name, Transform parent, string label, Font font,
        Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject box = CreateUIBox(name, parent, color, anchoredPosition, size, anchorMin, anchorMax);
        Button button = box.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        button.colors = colors;
        CreateUIText("Label", box.transform, label, font, 25, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Material GetMaterial(string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;
        if (isNew)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
        }
        material.color = color;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (color.a < 0.99f)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        if (isNew)
        {
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    private static Material GetLightDiffuserMaterial()
    {
        Color glow = new Color(1f, 0.9f, 0.76f);
        Material material = GetMaterial("M_LightDiffuser", glow, 0f, 0.4f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", glow * 2.2f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static InputActionReference GetActionReference(InputActionAsset asset, string actionPath)
    {
        if (asset == null)
        {
            Debug.LogError("ShoppingVR: Assets/InputSystem_Actions.inputactions was not found.");
            return null;
        }

        string safeName = actionPath.Replace('/', '_');
        string path = InputFolder + "/IA_" + safeName + ".asset";
        InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
        if (reference != null) return reference;

        InputAction action = asset.FindAction(actionPath, true);
        reference = InputActionReference.Create(action);
        reference.name = "IA_" + safeName;
        AssetDatabase.CreateAsset(reference, path);
        return reference;
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"ShoppingVR: serialized field '{propertyName}' was not found on {target.name}.");
            return;
        }
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"ShoppingVR: serialized field '{propertyName}' was not found on {target.name}.");
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateFloat(Object target, string propertyName, float expectedValue)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        if (property == null || !Mathf.Approximately(property.floatValue, expectedValue))
        {
            throw new System.InvalidOperationException(
                $"ShoppingVR scene validation failed: {target.name}.{propertyName} must be {expectedValue}.");
        }
    }

    private static void ValidateReferences(Object target, params string[] propertyNames)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        for (int i = 0; i < propertyNames.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyNames[i]);
            if (property == null || property.objectReferenceValue == null)
            {
                throw new System.InvalidOperationException(
                    $"ShoppingVR scene validation failed: {target.name}.{propertyNames[i]} is not connected.");
            }
        }
    }

    private static void ValidateArrayHasReferences(Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        if (property == null || !property.isArray || property.arraySize == 0)
        {
            throw new System.InvalidOperationException(
                $"ShoppingVR scene validation failed: {target.name}.{propertyName} is empty.");
        }

        for (int i = 0; i < property.arraySize; i++)
        {
            if (property.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                throw new System.InvalidOperationException(
                    $"ShoppingVR scene validation failed: {target.name}.{propertyName}[{i}] is not connected.");
            }
        }
    }

    private static void EnsureProductLayer()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        SerializedProperty layer = layers.GetArrayElementAtIndex(ProductLayer);
        if (string.IsNullOrEmpty(layer.stringValue))
        {
            layer.stringValue = "Product";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void AddToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = scenes.Count - 1; i >= 0; i--)
        {
            if (scenes[i].path == scenePath)
            {
                scenes.RemoveAt(i);
            }
        }
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
