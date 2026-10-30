using UnityEngine;

public sealed class CS_EntranceSequence : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private CS_PersonalShopperNPC personalShopper;
    [SerializeField, Min(0.5f)] private float openDistance = 4.5f;
    [SerializeField, Min(0.1f)] private float insideOffset = 0.65f;
    [SerializeField, Min(1f)] private float doorRotationDegreesPerSecond = 180f;

    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;
    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;
    private float entranceZ;
    private CS_EntrancePhase phase;

    private enum CS_EntrancePhase
    {
        Waiting,
        Opening,
        Closing,
        Complete
    }

    public void Configure(Transform playerTransform, Transform leftDoorTransform,
        Transform rightDoorTransform, CS_PersonalShopperNPC shopper)
    {
        player = playerTransform;
        leftDoor = leftDoorTransform;
        rightDoor = rightDoorTransform;
        personalShopper = shopper;
    }

    private void Awake()
    {
        ResolveMissingReferences();
        if (player == null || leftDoor == null || rightDoor == null || personalShopper == null)
        {
            Debug.LogError($"{nameof(CS_EntranceSequence)} on '{name}' needs Player, both doors, and Personal Shopper.", this);
            enabled = false;
            return;
        }

        leftOpenRotation = leftDoor.localRotation;
        rightOpenRotation = rightDoor.localRotation;
        leftClosedRotation = Quaternion.identity;
        rightClosedRotation = Quaternion.identity;
        entranceZ = (leftDoor.position.z + rightDoor.position.z) * 0.5f;
        leftDoor.localRotation = leftClosedRotation;
        rightDoor.localRotation = rightClosedRotation;
        phase = CS_EntrancePhase.Waiting;
    }

    private void ResolveMissingReferences()
    {
        player ??= GameObject.Find("Player")?.transform;
        leftDoor ??= GameObject.Find("Glass Door L")?.transform;
        rightDoor ??= GameObject.Find("Glass Door R")?.transform;
        personalShopper ??= FindAnyObjectByType<CS_PersonalShopperNPC>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        bool isInside = player.position.z >= entranceZ + insideOffset;
        if (isInside && phase != CS_EntrancePhase.Complete)
        {
            phase = CS_EntrancePhase.Closing;
        }
        else if (phase == CS_EntrancePhase.Waiting && Mathf.Abs(player.position.z - entranceZ) <= openDistance)
        {
            phase = CS_EntrancePhase.Opening;
        }

        if (phase == CS_EntrancePhase.Opening)
        {
            RotateDoors(leftOpenRotation, rightOpenRotation);
        }
        else if (phase == CS_EntrancePhase.Closing)
        {
            RotateDoors(leftClosedRotation, rightClosedRotation);
            if (DoorsMatch(leftClosedRotation, rightClosedRotation))
            {
                phase = CS_EntrancePhase.Complete;
                personalShopper.BeginEntranceGreeting();
            }
        }
    }

    private void RotateDoors(Quaternion leftTarget, Quaternion rightTarget)
    {
        float maxDegrees = doorRotationDegreesPerSecond * Time.deltaTime;
        leftDoor.localRotation = Quaternion.RotateTowards(leftDoor.localRotation, leftTarget, maxDegrees);
        rightDoor.localRotation = Quaternion.RotateTowards(rightDoor.localRotation, rightTarget, maxDegrees);
    }

    private bool DoorsMatch(Quaternion leftTarget, Quaternion rightTarget)
    {
        return Quaternion.Angle(leftDoor.localRotation, leftTarget) < 0.1f &&
               Quaternion.Angle(rightDoor.localRotation, rightTarget) < 0.1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapCurrentScene()
    {
        if (FindAnyObjectByType<CS_EntranceSequence>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject playerObject = GameObject.Find("Player");
        GameObject leftDoorObject = GameObject.Find("Glass Door L");
        GameObject rightDoorObject = GameObject.Find("Glass Door R");
        GameObject entranceObject = GameObject.Find("Entrance Header");
        CS_PersonalShopperNPC shopper = FindAnyObjectByType<CS_PersonalShopperNPC>(FindObjectsInactive.Include);
        if (playerObject == null || leftDoorObject == null || rightDoorObject == null ||
            entranceObject == null || shopper == null)
        {
            return;
        }

        CS_EntranceSequence sequence = entranceObject.AddComponent<CS_EntranceSequence>();
        sequence.Configure(playerObject.transform, leftDoorObject.transform, rightDoorObject.transform, shopper);
    }
}
