using UnityEngine;

public class CanvasFollowHead : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform headTransform;

    [Header("Position Settings")]
    [SerializeField] private float distance = 1.4f;
    [SerializeField] private float heightOffset = 0.05f;
    [SerializeField] private float sideOffset = 0.0f;

    [Header("Follow Settings")]
    [Tooltip("Normal position follow speed. Increase this if the canvas still feels too slow while walking.")]
    [SerializeField] private float positionSmoothSpeed = 18f;

    [Tooltip("Rotation follow speed when the canvas needs to rotate toward the user.")]
    [SerializeField] private float rotationSmoothSpeed = 10f;

    [Tooltip("If the canvas is farther than this from its target position, it snaps instead of slowly catching up.")]
    [SerializeField] private float snapDistance = 0.9f;

    [Tooltip("If the user gets closer than this to the canvas, the canvas immediately jumps back in front of the user.")]
    [SerializeField] private float minDistanceFromHead = 0.65f;

    [Header("Rotation Dead Zone")]
    [Tooltip("Rotation only updates after this angle. Position still follows every frame.")]
    [SerializeField] private float maxAngleBeforeRotate = 18f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasInitialized;

    private void Awake()
    {
        ResolveHeadTransform();
    }

    private void OnEnable()
    {
        ResolveHeadTransform();
        ForceSnapNow();
    }

    private void Start()
    {
        ResolveHeadTransform();
        ForceSnapNow();
    }

    private void LateUpdate()
    {
        if (headTransform == null)
        {
            ResolveHeadTransform();
            if (headTransform == null)
                return;
        }

        if (!hasInitialized)
            ForceSnapNow();

        GetFlatDirections(out Vector3 flatForward, out Vector3 flatRight);

        Vector3 desiredPosition =
            headTransform.position +
            flatForward * distance +
            flatRight * sideOffset +
            Vector3.up * heightOffset;

        Quaternion desiredRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        targetPosition = desiredPosition;

        float angle = Vector3.Angle(transform.forward, flatForward);
        if (angle > maxAngleBeforeRotate)
            targetRotation = desiredRotation;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        float distanceToHead = Vector3.Distance(transform.position, headTransform.position);

        Vector3 headToCanvas = transform.position - headTransform.position;
        float forwardDot = Vector3.Dot(flatForward, headToCanvas.normalized);
        bool canvasIsBehindOrTooClose = forwardDot < 0.25f || distanceToHead < minDistanceFromHead;

        if (distanceToTarget > snapDistance || canvasIsBehindOrTooClose)
        {
            transform.position = targetPosition;
            transform.rotation = desiredRotation;
            targetRotation = desiredRotation;
            return;
        }

        float t = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);

        float rt = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rt);
    }

    public void ForceSnapNow()
    {
        SnapToHeadView();
        hasInitialized = true;
    }

    public void SnapToHeadView()
    {
        if (headTransform == null)
        {
            ResolveHeadTransform();
            if (headTransform == null)
                return;
        }

        GetFlatDirections(out Vector3 flatForward, out Vector3 flatRight);

        targetPosition =
            headTransform.position +
            flatForward * distance +
            flatRight * sideOffset +
            Vector3.up * heightOffset;

        targetRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private void ResolveHeadTransform()
    {
        if (headTransform != null)
            return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
            headTransform = mainCam.transform;
    }

    private void GetFlatDirections(out Vector3 flatForward, out Vector3 flatRight)
    {
        flatForward = headTransform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = transform.forward;

        flatForward.Normalize();

        flatRight = headTransform.right;
        flatRight.y = 0f;

        if (flatRight.sqrMagnitude < 0.001f)
            flatRight = transform.right;

        flatRight.Normalize();
    }
}
