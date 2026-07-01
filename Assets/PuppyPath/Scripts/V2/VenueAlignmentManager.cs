using UnityEngine;

public class VenueAlignmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform xrCamera;
    [SerializeField] private Transform venueContentRoot;

    [Header("Manual Onsite Calibration")]
    [Tooltip("If enabled, calibration runs automatically on Start. Use this only for quick Quest tests.")]
    [SerializeField] private bool alignOnStart = true;
    [Tooltip("When standing at VenueOrigin, face the real-world direction that corresponds to map north / Unity +Z, then calibrate.")]
    [SerializeField] private bool alignYawToHeadForward = true;
    [Tooltip("Keep venue content on this world Y level after calibration.")]
    [SerializeField] private float venueGroundY = 0f;
    [Tooltip("Optional additional yaw adjustment in degrees after HMD-facing alignment.")]
    [SerializeField] private float additionalYawDegrees;

    [Header("Debug")]
    [SerializeField] private bool logCalibration = true;

    public Transform VenueContentRoot
    {
        get { return venueContentRoot; }
        set { venueContentRoot = value; }
    }

    private void Start()
    {
        if (alignOnStart)
            AlignVenueOriginToCurrentHeadPose();
    }

    [ContextMenu("Align Venue Origin To Current Head Pose")]
    public void AlignVenueOriginToCurrentHeadPose()
    {
        if (venueContentRoot == null || xrCamera == null)
        {
            Debug.LogWarning("VenueAlignmentManager: missing VenueContentRoot or XR Camera.");
            return;
        }

        Quaternion yawRotation = Quaternion.Euler(0f, additionalYawDegrees, 0f);
        if (alignYawToHeadForward)
            yawRotation = Quaternion.Euler(0f, GetFlatYawDegrees(xrCamera.forward) + additionalYawDegrees, 0f);

        venueContentRoot.rotation = yawRotation;

        Vector3 targetOriginWorld = xrCamera.position;
        targetOriginWorld.y = venueGroundY;

        Vector3 localOrigin = mapDefinition != null ? mapDefinition.originWorldPosition : Vector3.zero;
        venueContentRoot.position = targetOriginWorld - venueContentRoot.rotation * localOrigin;

        if (logCalibration)
        {
            Debug.Log(
                "VenueAlignmentManager: aligned venue origin to HMD. " +
                "Stand at Photo Wall upper-right origin and face map north / Unity +Z when calibrating.");
        }
    }

    [ContextMenu("Align Translation Only To Current Head Pose")]
    public void AlignTranslationOnlyToCurrentHeadPose()
    {
        if (venueContentRoot == null || xrCamera == null)
        {
            Debug.LogWarning("VenueAlignmentManager: missing VenueContentRoot or XR Camera.");
            return;
        }

        Vector3 targetOriginWorld = xrCamera.position;
        targetOriginWorld.y = venueGroundY;

        Vector3 localOrigin = mapDefinition != null ? mapDefinition.originWorldPosition : Vector3.zero;
        venueContentRoot.position = targetOriginWorld - venueContentRoot.rotation * localOrigin;
    }

    private static float GetFlatYawDegrees(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return 0f;

        forward.Normalize();
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
