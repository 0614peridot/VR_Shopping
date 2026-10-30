#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CS_LuxuryAssortmentBuilder
{
    public const int ProductCount = 38;
    private const string ScenePath = "Assets/ShoppingVR/Scenes/ShoppingEnvironment.unity";
    private const string ModelFolder = "Assets/ShoppingVR/Models";
    private const string RootName = "Luxury Assortment — 38 Items";
    private const int ProductLayer = 8;

    private static readonly CS_ProductDefinition[] Bags =
    {
        new CS_ProductDefinition("bag-black.glb", "Bag Black", 245000, 0.42f),
        new CS_ProductDefinition("bag-purple.glb", "Bag Purple", 198000, 0.38f),
        new CS_ProductDefinition("bag-salmon.glb", "Bag Salmon", 285000, 0.40f)
    };

    private static readonly CS_ProductDefinition[] Shoes =
    {
        new CS_ProductDefinition("heel-less-boot.glb", "Heel Less Boot", 220000, 0.42f),
        new CS_ProductDefinition("heel-less-pump-black.glb", "Heel Less Pump Black", 165000, 0.24f),
        new CS_ProductDefinition("open-mule-heels.glb", "Open Mule Heels", 185000, 0.16f),
        new CS_ProductDefinition("tabi-ballet-beige.glb", "Tabi Ballet Beige", 175000, 0.12f),
        new CS_ProductDefinition("tabi-green.glb", "Tabi Green", 150000, 0.13f)
    };

    private static readonly CS_ProductDefinition[] Pants =
    {
        new CS_ProductDefinition("jeans-black.glb", "Jeans Black", 180000, 1.02f),
        new CS_ProductDefinition("jeans-checked.glb", "Jeans Checked", 195000, 1.02f)
    };

    private static readonly CS_ProductDefinition[] Outers =
    {
        new CS_ProductDefinition("jacket-patterned.glb", "Jacket Patterned", 290000, 0.74f),
        new CS_ProductDefinition("trench-coat-beige.glb", "Trench Coat Beige", 320000, 1.18f),
        new CS_ProductDefinition("trench-coat-black.glb", "Trench Coat Black", 390000, 1.18f)
    };

    private static readonly CS_ProductDefinition[] Dresses =
    {
        new CS_ProductDefinition("dress-black.glb", "Dress Black", 260000, 1.18f),
        new CS_ProductDefinition("dress-pink.glb", "Dress Pink", 295000, 1.15f)
    };

    private static readonly CS_ProductDefinition[] Tops =
    {
        new CS_ProductDefinition("flower-sweater.glb", "Flower Sweater", 175000, 0.68f),
        new CS_ProductDefinition("knit-top-black.glb", "Knit Top Black", 160000, 0.68f),
        new CS_ProductDefinition("knit-vest.glb", "Knit Vest", 145000, 0.62f)
    };

    private static readonly CS_ProductDefinition[] Skirts =
    {
        new CS_ProductDefinition("checked-knit-skirt.glb", "Checked Knit Skirt", 185000, 0.65f)
    };

    private readonly struct CS_ProductDefinition
    {
        public CS_ProductDefinition(string assetFileName, string name, int price, float targetHeight)
        {
            AssetFileName = assetFileName;
            Name = name;
            Price = price;
            TargetHeight = targetHeight;
        }

        public string AssetFileName { get; }
        public string Name { get; }
        public int Price { get; }
        public float TargetHeight { get; }
    }

    static CS_LuxuryAssortmentBuilder()
    {
        EditorApplication.delayCall += ApplyPendingLayout;
    }

    [MenuItem("ShoppingVR/Apply 38-Item Luxury Assortment")]
    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before applying the luxury assortment layout.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        ApplyToScene(scene);
    }

    public static void ReplaceGeneratedDisplays(Transform environment)
    {
        if (environment == null)
        {
            throw new System.ArgumentNullException(nameof(environment));
        }

        RemovePreviousAssortment(environment);
        BuildLuxuryAssortment(environment);
    }

    private static void ApplyPendingLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ApplyPendingLayout;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            return;
        }

        Transform environment = FindEnvironment(scene);
        if (environment == null || HasCompleteLayout(environment))
        {
            return;
        }

        ApplyToScene(scene);
    }

    private static void ApplyToScene(Scene scene)
    {
        Transform environment = FindEnvironment(scene);
        if (environment == null)
        {
            throw new System.InvalidOperationException("Shopping Environment was not found in the main scene.");
        }

        ReplaceGeneratedDisplays(environment);
        CS_ProductItem[] products = environment.GetComponentsInChildren<CS_ProductItem>(true);
        if (products.Length != ProductCount)
        {
            throw new System.InvalidOperationException(
                $"Luxury assortment build produced {products.Length} products instead of {ProductCount}.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ShoppingVR luxury assortment applied: 19 designs, 2 placements each, 38 selectable products.");
    }

    private static Transform FindEnvironment(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Shopping Environment")
            {
                return root.transform;
            }
        }

        return null;
    }

    private static bool HasCompleteLayout(Transform environment)
    {
        Transform assortment = environment.Find(RootName);
        return assortment != null && assortment.GetComponentsInChildren<CS_ProductItem>(true).Length == ProductCount;
    }

    private static void RemovePreviousAssortment(Transform environment)
    {
        CS_ProductItem[] products = environment.GetComponentsInChildren<CS_ProductItem>(true);
        for (int i = products.Length - 1; i >= 0; i--)
        {
            if (products[i] != null)
            {
                Object.DestroyImmediate(products[i].gameObject);
            }
        }

        string[] legacyFixtures =
        {
            RootName, "Left Wall Rack", "Rear Left Rack", "Front Left Rack", "Center Round Rack",
            "Right Center Rack", "Right Wall Rack", "Mode Display", "Mode Display Trim",
            "Front Cabinet A", "Front Cabinet B", "Showcase Base", "Showcase Glass",
            "Showcase Plinth A", "Showcase Plinth A Trim", "Showcase Plinth B",
            "Showcase Plinth B Trim", "MODE / 01 Label", "OBJECTS Label"
        };

        for (int i = 0; i < legacyFixtures.Length; i++)
        {
            Transform target = environment.Find(legacyFixtures[i]);
            if (target != null)
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }
    }

    private static void BuildLuxuryAssortment(Transform environment)
    {
        Material ink = LoadMaterial("M_Ink.mat");
        Material brass = LoadMaterial("M_Brass.mat");
        Material cream = LoadMaterial("M_Cream.mat");
        Material sage = LoadMaterial("M_Sage.mat");
        Material terracotta = LoadMaterial("M_Terracotta.mat");
        Material[] productPalette = { cream, sage, terracotta, ink };

        Transform root = NewRoot(RootName, environment);
        Transform fixtures = NewRoot("Architectural Fixtures", root);
        Transform bags = NewRoot("Bags — 3 Designs × 2", root);
        Transform shoes = NewRoot("Shoes — 5 Designs × 2", root);
        Transform pants = NewRoot("Pants — 2 Designs × 2", root);
        Transform outers = NewRoot("Outers — 3 Designs × 2", root);
        Transform dresses = NewRoot("Dresses — 2 Designs × 2", root);
        Transform tops = NewRoot("Tops — 3 Designs × 2", root);
        Transform skirts = NewRoot("Skirt — 1 Design × 2", root);

        BuildBagGallery(fixtures, bags, ink, brass, productPalette);
        BuildShoeWall(fixtures, shoes, ink, brass, productPalette);
        BuildOuterWall(fixtures, outers, ink, brass, productPalette);
        BuildRearGallery(fixtures, dresses, skirts, ink, brass, productPalette);
        BuildCenterRails(fixtures, tops, pants, ink, brass, productPalette);
    }

    private static void BuildBagGallery(Transform fixtures, Transform products, Material ink, Material brass,
        IReadOnlyList<Material> palette)
    {
        float[] xPositions = { 6.5f, 9f, 11.5f };
        float[] zPositions = { -5.8f, -3.7f };
        int[] designOrder = { 0, 2, 1, 2, 0, 1 };
        float[] yawOffsets = { -8f, 7f, -3f, 9f, 4f, -7f };
        int[] copyCounts = new int[Bags.Length];
        for (int slot = 0; slot < designOrder.Length; slot++)
        {
            int design = designOrder[slot];
            int copy = copyCounts[design]++;
            Vector3 floorPosition = new Vector3(xPositions[slot % xPositions.Length], 0f,
                zPositions[slot / xPositions.Length]);
            CreatePlinth($"Bag Plinth {slot + 1}", floorPosition, ink, brass, fixtures);
            float centerY = 0.95f + Bags[design].TargetHeight * 0.5f;
            CreateProduct(Bags[design], "Bag", copy, new Vector3(floorPosition.x, centerY, floorPosition.z),
                Quaternion.Euler(0f, 180f + yawOffsets[slot], 0f), palette[design % palette.Count], products);
        }
    }

    private static void BuildShoeWall(Transform fixtures, Transform products, Material ink, Material brass,
        IReadOnlyList<Material> palette)
    {
        CreateCube("Shoe Wall Backdrop", new Vector3(13.55f, 1.65f, 1.2f), new Vector3(0.18f, 3.3f, 11.8f),
            ink, fixtures);
        float[] zPositions = { -3f, -1f, 1f, 3f, 5f };
        float[] heights = { 0.85f, 1.95f };
        int[] designOrder = { 0, 3, 1, 4, 2, 2, 0, 4, 1, 3 };
        float[] yawOffsets = { 7f, -5f, 3f, -8f, 6f, -4f, 8f, 4f, -6f, 2f };
        int[] copyCounts = new int[Shoes.Length];
        for (int shelf = 0; shelf < heights.Length; shelf++)
        {
            CreateCube($"Shoe Shelf {shelf + 1}", new Vector3(13.05f, heights[shelf] - 0.32f, 1f),
                new Vector3(0.9f, 0.08f, 10.8f), brass, fixtures);
        }

        for (int slot = 0; slot < designOrder.Length; slot++)
        {
            int design = designOrder[slot];
            int copy = copyCounts[design]++;
            int shelf = slot / zPositions.Length;
            float shelfTop = heights[shelf] - 0.28f;
            float centerY = shelfTop + Shoes[design].TargetHeight * 0.5f + 0.01f;
            CreateProduct(Shoes[design], "Shoe", copy,
                new Vector3(12.85f, centerY, zPositions[slot % zPositions.Length]),
                Quaternion.Euler(0f, -90f + yawOffsets[slot], 0f),
                palette[(design + copy) % palette.Count], products);
        }
    }

    private static void BuildOuterWall(Transform fixtures, Transform products, Material ink, Material brass,
        IReadOnlyList<Material> palette)
    {
        CreateCube("Outer Wall Backdrop", new Vector3(-13.55f, 1.65f, 0f), new Vector3(0.18f, 3.3f, 12.8f),
            ink, fixtures);
        CreateCube("Outer Wall Rail", new Vector3(-13.02f, 2.22f, 0f), new Vector3(0.1f, 0.1f, 11.5f),
            brass, fixtures);
        float[] zPositions = { -5f, -3f, -1f, 1f, 3f, 5f };
        int[] designOrder = { 0, 2, 1, 0, 2, 1 };
        float[] yawOffsets = { -4f, 3f, -2f, 5f, -3f, 2f };
        int[] copyCounts = new int[Outers.Length];
        for (int index = 0; index < zPositions.Length; index++)
        {
            int design = designOrder[index];
            int copy = copyCounts[design]++;
            float centerY = 2.16f - Outers[design].TargetHeight * 0.5f;
            CreateProduct(Outers[design], "Outer", copy, new Vector3(-12.65f, centerY, zPositions[index]),
                Quaternion.Euler(0f, 90f + yawOffsets[index], 0f),
                palette[(design + 2) % palette.Count], products);
        }
    }

    private static void BuildRearGallery(Transform fixtures, Transform dresses, Transform skirts, Material ink,
        Material brass, IReadOnlyList<Material> palette)
    {
        CreateCube("Rear Apparel Backdrop", new Vector3(-7f, 1.7f, 8.55f), new Vector3(11.5f, 3.4f, 0.18f),
            ink, fixtures);
        CreateCube("Rear Apparel Rail", new Vector3(-7f, 2.28f, 8.05f), new Vector3(10.5f, 0.1f, 0.1f),
            brass, fixtures);

        float[] xPositions = { -11.5f, -9.7f, -7.9f, -6.1f, -4.1f, -2.3f };
        int[] designOrder = { 0, -1, 1, 0, -1, 1 };
        float[] yawOffsets = { -3f, 4f, -2f, 3f, -4f, 2f };
        int[] dressCopyCounts = new int[Dresses.Length];
        int skirtCopy = 0;
        for (int index = 0; index < xPositions.Length; index++)
        {
            int design = designOrder[index];
            if (design >= 0)
            {
                int copy = dressCopyCounts[design]++;
                float centerY = 2.22f - Dresses[design].TargetHeight * 0.5f;
                CreateProduct(Dresses[design], "Dress", copy, new Vector3(xPositions[index], centerY, 7.65f),
                    Quaternion.Euler(0f, 180f + yawOffsets[index], 0f),
                    palette[(design + 1) % palette.Count], dresses);
            }
            else
            {
                float centerY = 2.22f - Skirts[0].TargetHeight * 0.5f;
                CreateProduct(Skirts[0], "Skirt", skirtCopy++, new Vector3(xPositions[index], centerY, 7.65f),
                    Quaternion.Euler(0f, 180f + yawOffsets[index], 0f), palette[2], skirts);
            }
        }
    }

    private static void BuildCenterRails(Transform fixtures, Transform tops, Transform pants, Material ink,
        Material brass, IReadOnlyList<Material> palette)
    {
        CreateRail("Top Rail", new Vector3(-6.6f, 0f, -1.1f), 6.6f, ink, brass, fixtures);
        float[] topX = { -9.1f, -8.1f, -6.9f, -5.9f, -4.7f, -3.7f };
        int[] topDesignOrder = { 0, 2, 1, 0, 2, 1 };
        float[] topYawOffsets = { -4f, 3f, -2f, 4f, -3f, 2f };
        int[] topCopyCounts = new int[Tops.Length];
        for (int index = 0; index < topX.Length; index++)
        {
            int design = topDesignOrder[index];
            int copy = topCopyCounts[design]++;
            float centerY = 2.12f - Tops[design].TargetHeight * 0.5f;
            CreateProduct(Tops[design], "Top", copy, new Vector3(topX[index], centerY, -1.1f),
                Quaternion.Euler(0f, topYawOffsets[index], 0f), palette[design % palette.Count], tops);
        }

        CreateRail("Pant Rail", new Vector3(-5.5f, 0f, 3.25f), 5.1f, ink, brass, fixtures);
        float[] pantX = { -7.2f, -6.1f, -4.9f, -3.8f };
        int[] pantDesignOrder = { 0, 1, 0, 1 };
        float[] pantYawOffsets = { -3f, 2f, 3f, -2f };
        int[] pantCopyCounts = new int[Pants.Length];
        for (int index = 0; index < pantX.Length; index++)
        {
            int design = pantDesignOrder[index];
            int copy = pantCopyCounts[design]++;
            float centerY = 2.12f - Pants[design].TargetHeight * 0.5f;
            CreateProduct(Pants[design], "Pants", copy, new Vector3(pantX[index], centerY, 3.25f),
                Quaternion.Euler(0f, pantYawOffsets[index], 0f),
                palette[(design + 1) % palette.Count], pants);
        }
    }

    private static void CreateRail(string name, Vector3 position, float width, Material baseMaterial,
        Material railMaterial, Transform parent)
    {
        CreateCube(name + " Base", position + new Vector3(0f, 0.08f, 0f),
            new Vector3(width, 0.16f, 0.65f), baseMaterial, parent);
        CreateCube(name + " Bar", position + new Vector3(0f, 2.18f, 0f),
            new Vector3(width, 0.1f, 0.1f), railMaterial, parent);
        CreateCube(name + " Left Post", position + new Vector3(-width * 0.48f, 1.1f, 0f),
            new Vector3(0.1f, 2.2f, 0.1f), railMaterial, parent);
        CreateCube(name + " Right Post", position + new Vector3(width * 0.48f, 1.1f, 0f),
            new Vector3(0.1f, 2.2f, 0.1f), railMaterial, parent);
    }

    private static void CreatePlinth(string name, Vector3 position, Material body, Material trim, Transform parent)
    {
        CreateCylinder(name, position + Vector3.up * 0.45f, new Vector3(0.72f, 0.45f, 0.72f), body, parent);
        CreateCylinder(name + " Trim", position + Vector3.up * 0.92f, new Vector3(0.76f, 0.03f, 0.76f), trim, parent);
    }

    private static void CreateProduct(CS_ProductDefinition definition, string category, int copy,
        Vector3 position, Quaternion rotation, Material material, Transform parent)
    {
        GameObject root = new GameObject($"{definition.Name} — Display {copy + 1}");
        root.transform.SetParent(parent, false);
        string modelPath = $"{ModelFolder}/{definition.AssetFileName}";
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            AssetDatabase.ImportAsset(
                modelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        }

        if (modelAsset == null)
        {
            Object.DestroyImmediate(root);
            throw new System.InvalidOperationException($"Imported model '{modelPath}' was not found.");
        }

        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset, root.transform) as GameObject;
        if (modelInstance == null)
        {
            modelInstance = Object.Instantiate(modelAsset, root.transform);
        }

        modelInstance.name = "Model — " + definition.Name;
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localScale = Vector3.one;
        NormalizeModel(modelInstance.transform, definition.TargetHeight);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (!TryCalculateRendererBounds(renderers, out Bounds bounds))
        {
            Object.DestroyImmediate(root);
            throw new System.InvalidOperationException($"Imported model '{modelPath}' has no renderers.");
        }

        BoxCollider selectionCollider = root.AddComponent<BoxCollider>();
        selectionCollider.center = bounds.center;
        selectionCollider.size = bounds.size + Vector3.one * 0.08f;
        root.transform.SetPositionAndRotation(position, rotation);
        SetLayerRecursively(root, ProductLayer);
        CS_ProductItem product = root.AddComponent<CS_ProductItem>();
        product.Configure(definition.Name, definition.Price, new Collider[] { selectionCollider }, renderers);
    }

    private static void NormalizeModel(Transform model, float targetHeight)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (!TryCalculateRendererBounds(renderers, out Bounds bounds) || bounds.size.y <= 0.0001f)
        {
            return;
        }

        float uniformScale = targetHeight / bounds.size.y;
        model.localScale = Vector3.one * uniformScale;
        if (TryCalculateRendererBounds(renderers, out bounds))
        {
            model.position -= bounds.center;
        }
    }

    private static bool TryCalculateRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void CreateBagVisual(Transform root, Material material)
    {
        CreateCube("Bag Body", Vector3.zero, new Vector3(0.92f, 0.66f, 0.34f), material, root, true);
        CreateCube("Handle Left", new Vector3(-0.28f, 0.46f, 0f), new Vector3(0.08f, 0.42f, 0.08f), material, root, true);
        CreateCube("Handle Right", new Vector3(0.28f, 0.46f, 0f), new Vector3(0.08f, 0.42f, 0.08f), material, root, true);
        CreateCube("Handle Top", new Vector3(0f, 0.65f, 0f), new Vector3(0.62f, 0.08f, 0.08f), material, root, true);
    }

    private static void CreateShoeVisual(Transform root, Material material)
    {
        CreateCube("Sole", new Vector3(0f, -0.18f, 0.12f), new Vector3(0.88f, 0.12f, 0.34f), material, root, true);
        GameObject upper = CreateCube("Upper", new Vector3(-0.08f, 0.02f, 0f), new Vector3(0.58f, 0.34f, 0.32f), material, root, true);
        upper.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
    }

    private static void CreatePantsVisual(Transform root, Material material)
    {
        CreateCube("Waist", new Vector3(0f, 0.48f, 0f), new Vector3(0.78f, 0.2f, 0.24f), material, root, true);
        CreateCube("Left Leg", new Vector3(-0.21f, -0.18f, 0f), new Vector3(0.32f, 1.18f, 0.22f), material, root, true);
        CreateCube("Right Leg", new Vector3(0.21f, -0.18f, 0f), new Vector3(0.32f, 1.18f, 0.22f), material, root, true);
    }

    private static void CreateOuterVisual(Transform root, Material material)
    {
        CreateCube("Body", Vector3.zero, new Vector3(0.82f, 1.34f, 0.28f), material, root, true);
        CreateCube("Left Sleeve", new Vector3(-0.55f, 0f, 0f), new Vector3(0.25f, 1.15f, 0.24f), material, root, true);
        CreateCube("Right Sleeve", new Vector3(0.55f, 0f, 0f), new Vector3(0.25f, 1.15f, 0.24f), material, root, true);
    }

    private static void CreateDressVisual(Transform root, Material material)
    {
        CreateCube("Bodice", new Vector3(0f, 0.43f, 0f), new Vector3(0.68f, 0.66f, 0.24f), material, root, true);
        CreateCube("Skirt", new Vector3(0f, -0.47f, 0f), new Vector3(0.98f, 1.12f, 0.28f), material, root, true);
    }

    private static void CreateTopVisual(Transform root, Material material)
    {
        CreateCube("Body", Vector3.zero, new Vector3(0.72f, 0.9f, 0.24f), material, root, true);
        CreateCube("Left Sleeve", new Vector3(-0.5f, 0.18f, 0f), new Vector3(0.25f, 0.52f, 0.22f), material, root, true);
        CreateCube("Right Sleeve", new Vector3(0.5f, 0.18f, 0f), new Vector3(0.25f, 0.52f, 0.22f), material, root, true);
    }

    private static void CreateSkirtVisual(Transform root, Material material)
    {
        CreateCube("Waist", new Vector3(0f, 0.45f, 0f), new Vector3(0.72f, 0.18f, 0.24f), material, root, true);
        CreateCube("Skirt", new Vector3(0f, -0.1f, 0f), new Vector3(0.94f, 0.94f, 0.28f), material, root, true);
    }

    private static Transform NewRoot(string name, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        return root.transform;
    }

    private static Material LoadMaterial(string fileName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/ShoppingVR/Materials/{fileName}");
        if (material == null)
        {
            throw new System.InvalidOperationException($"Required luxury-layout material '{fileName}' was not found.");
        }

        return material;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material,
        Transform parent, bool local = false)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        if (local)
        {
            item.transform.localPosition = position;
        }
        else
        {
            item.transform.position = position;
        }

        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material,
        Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif
