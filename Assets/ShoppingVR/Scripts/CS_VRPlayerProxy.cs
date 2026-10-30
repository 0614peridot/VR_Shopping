using UnityEngine;
using UnityEngine.XR;

// Lets one scene run on Quest and in the headset-free Editor. With an active XR device the Meta rig owns
// movement and tracking, and this Player object only mirrors the head's floor position and yaw so NPC,
// checkout and entrance logic that reads the Player transform keeps working unchanged.
// Without a device, the rig is disabled and the desktop camera, locomotion and focus controls stay active.
[DefaultExecutionOrder(-1000)]
public sealed class CS_VRPlayerProxy : MonoBehaviour
{
    private const string MainCameraTag = "MainCamera";
    private const string UntaggedTag = "Untagged";
    private const float MinimumForwardSqrMagnitude = 0.0001f;

    [SerializeField, Tooltip("OVRCameraRig root. Must not be a child of this Player, or following the head would feed back into the rig.")]
    private GameObject vrRig;
    [SerializeField, Tooltip("CenterEyeAnchor of the OVRCameraRig.")]
    private Transform vrHead;
    [SerializeField] private Camera desktopCamera;
    [SerializeField, Tooltip("Desktop camera, audio listener, locomotion and mouse focus components to disable in VR.")]
    private Behaviour[] desktopOnlyBehaviours;
    [SerializeField] private CharacterController desktopCharacterController;

    private bool isVRActive;

    public bool IsVRActive => isVRActive;

    public void Configure(GameObject rig, Transform head, Camera camera, Behaviour[] desktopBehaviours,
        CharacterController characterController)
    {
        vrRig = rig;
        vrHead = head;
        desktopCamera = camera;
        desktopOnlyBehaviours = desktopBehaviours;
        desktopCharacterController = characterController;
    }

    private void Awake()
    {
        // XR initializes before the first scene loads, so the device state is already known here.
        isVRActive = XRSettings.isDeviceActive;
        if (isVRActive && (vrRig == null || vrHead == null))
        {
            Debug.LogError($"{nameof(CS_VRPlayerProxy)} on '{name}' needs VR Rig and VR Head. Falling back to desktop controls.", this);
            isVRActive = false;
        }

        ApplyMode();
    }

    private void LateUpdate()
    {
        if (!isVRActive)
        {
            return;
        }

        Vector3 headPosition = vrHead.position;
        transform.position = new Vector3(headPosition.x, vrRig.transform.position.y, headPosition.z);

        Vector3 forward = Vector3.ProjectOnPlane(vrHead.forward, Vector3.up);
        if (forward.sqrMagnitude > MinimumForwardSqrMagnitude)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    private void ApplyMode()
    {
        if (vrRig != null)
        {
            vrRig.SetActive(isVRActive);
        }

        if (desktopOnlyBehaviours != null)
        {
            for (int i = 0; i < desktopOnlyBehaviours.Length; i++)
            {
                if (desktopOnlyBehaviours[i] != null)
                {
                    desktopOnlyBehaviours[i].enabled = !isVRActive;
                }
            }
        }

        if (desktopCharacterController != null)
        {
            desktopCharacterController.enabled = !isVRActive;
        }

        // Only one camera may carry the MainCamera tag, because UI and NPC scripts resolve Camera.main.
        if (desktopCamera != null)
        {
            desktopCamera.gameObject.tag = isVRActive ? UntaggedTag : MainCameraTag;
        }
    }
}
