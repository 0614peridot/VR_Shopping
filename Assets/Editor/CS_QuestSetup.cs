using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.OVR.Editor.QuickActions;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click Quest 3 setup. Relies on Meta's own Project Setup Tool and Interaction SDK Quick Actions
// instead of hand-written XR configuration, so the result matches what the SDK validates.
public static class CS_QuestSetup
{
    private const string PlayerName = "Player";
    private const string CenterEyePath = "TrackingSpace/CenterEyeAnchor";
    private const string OpenXRSettingsTypeName = "UnityEngine.XR.OpenXR.OpenXRSettings, Unity.XR.OpenXR";
    private static readonly string[] TeleportSurfaceNames = { "Floor", "Exterior Ground" };

    [MenuItem("ShoppingVR/Quest/1. Configure Project for Quest 3")]
    private static async void ConfigureProject()
    {
        if (System.Type.GetType(OpenXRSettingsTypeName) == null)
        {
            Debug.LogError("OpenXR Plugin (com.unity.xr.openxr) is not installed yet. Let Package Manager finish resolving, then run this again.");
            return;
        }

        OVRProjectConfig config = OVRProjectConfig.CachedProjectConfig;
        config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
        config.targetDeviceTypes = new List<OVRProjectConfig.DeviceType>
        {
            OVRProjectConfig.DeviceType.Quest3,
            OVRProjectConfig.DeviceType.Quest3S
        };
        OVRProjectConfig.CommitProjectConfig(config);

        // Meta's Fix All assigns the OpenXR loader, Meta XR feature group, controller profiles and the
        // Android player settings it requires. Standalone is included so Play Mode works over Quest Link.
        await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Standalone);
        await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Android);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Could not switch to Android. Install Android Build Support for this Unity version in Unity Hub.");
            return;
        }

        Debug.Log("Quest 3 project setup finished. Review anything left in Meta > Tools > Project Setup Tool (Android tab).");
    }

    [MenuItem("ShoppingVR/Quest/2. Add Quest Rig and Grab to Open Scene")]
    private static void SetUpScene()
    {
        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            Debug.LogError($"No '{PlayerName}' object in the open scene. Open ShoppingEnvironment first.");
            return;
        }

        OVRCameraRig cameraRig = SetUpRig(player.transform);
        if (cameraRig == null)
        {
            return;
        }

        if (!SetUpPlayerProxy(player, cameraRig))
        {
            return;
        }

        int productCount = SetUpProducts();
        int surfaceCount = SetUpTeleportSurfaces();

        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log($"Quest rig ready: {productCount} grabbable product(s), {surfaceCount} teleport surface(s). Save the scene to keep the changes.");
    }

    private static OVRCameraRig SetUpRig(Transform player)
    {
        OVRCameraRig cameraRig = Object.FindFirstObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
        if (cameraRig == null)
        {
            // Creates OVRCameraRig (floor-level tracking) plus hands, controllers and locomotion interactors.
            OVRQuickActionsAPI.AddOVRInteractionRig(generateAsEditableCopy: false);
            cameraRig = Object.FindFirstObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
        }

        if (cameraRig == null)
        {
            Debug.LogError("Interaction SDK could not create an OVRCameraRig.");
            return null;
        }

        // The rig must stay outside the Player, because the Player follows the head.
        Undo.SetTransformParent(cameraRig.transform, null, "Move Quest Rig");
        Undo.RecordObject(cameraRig.transform, "Place Quest Rig");
        cameraRig.transform.SetPositionAndRotation(player.position, Quaternion.Euler(0f, player.eulerAngles.y, 0f));
        return cameraRig;
    }

    private static bool SetUpPlayerProxy(GameObject player, OVRCameraRig cameraRig)
    {
        Transform head = cameraRig.transform.Find(CenterEyePath);
        Camera desktopCamera = player.GetComponentInChildren<Camera>(true);
        if (head == null || desktopCamera == null)
        {
            Debug.LogError($"Missing {CenterEyePath} on the rig or a desktop Camera under '{player.name}'.");
            return false;
        }

        List<Behaviour> desktopBehaviours = new List<Behaviour>();
        AddIfPresent(desktopBehaviours, desktopCamera);
        AddIfPresent(desktopBehaviours, desktopCamera.GetComponent<AudioListener>());
        AddIfPresent(desktopBehaviours, player.GetComponent<CS_PlayerLocomotion>());
        AddIfPresent(desktopBehaviours, player.GetComponent<CS_ProductFocusController>());

        CS_VRPlayerProxy proxy = player.GetComponent<CS_VRPlayerProxy>();
        if (proxy == null)
        {
            proxy = Undo.AddComponent<CS_VRPlayerProxy>(player);
        }

        Undo.RecordObject(proxy, "Configure VR Player Proxy");
        proxy.Configure(cameraRig.gameObject, head, desktopCamera, desktopBehaviours.ToArray(),
            player.GetComponent<CharacterController>());
        EditorUtility.SetDirty(proxy);
        return true;
    }

    private static int SetUpProducts()
    {
        CS_ProductItem[] products = Object.FindObjectsByType<CS_ProductItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CS_ProductItem product in products)
        {
            GameObject target = product.gameObject;
            if (target.GetComponent<Grabbable>() == null)
            {
                // Near grab for hands/controllers, plus pull-to-hand so shelf items are reachable without leaning.
                QuickActionsAPI.AddGrabInteraction(target);
                QuickActionsAPI.AddDistanceGrabInteraction(target, DistanceGrabMode.PullToHand);
            }

            CS_VRProductGrab grabBridge = target.GetComponent<CS_VRProductGrab>();
            if (grabBridge == null)
            {
                grabBridge = Undo.AddComponent<CS_VRProductGrab>(target);
            }

            grabBridge.Configure(target.GetComponent<Grabbable>());
            EditorUtility.SetDirty(grabBridge);
        }

        return products.Length;
    }

    private static int SetUpTeleportSurfaces()
    {
        int count = 0;
        foreach (string surfaceName in TeleportSurfaceNames)
        {
            GameObject surface = GameObject.Find(surfaceName);
            if (surface == null)
            {
                Debug.LogWarning($"Teleport surface '{surfaceName}' not found; skipped.");
                continue;
            }

            if (surface.GetComponentInChildren<TeleportInteractable>(true) == null)
            {
                // No snapping: the floor's own collider is the target, so any point on it is valid.
                QuickActionsAPI.AddTeleportInteraction(surface, TeleportSurfaceType.Hotspot, TeleportHotspotSnap.None);
            }

            count++;
        }

        return count;
    }

    private static void AddIfPresent(List<Behaviour> list, Behaviour behaviour)
    {
        if (behaviour != null)
        {
            list.Add(behaviour);
        }
    }
}
