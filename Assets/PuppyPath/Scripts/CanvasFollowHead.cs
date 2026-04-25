using System.Collections;
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
    [SerializeField] private float positionSmoothSpeed = 8f;
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("Dead Zone")]
    [SerializeField] private float maxAngleBeforeFollow = 20f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasInitialized;

    private void Start()
    {
        if (headTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                headTransform = mainCam.transform;
        }

        StartCoroutine(DelayedInitialSnap());
    }

    private IEnumerator DelayedInitialSnap()
    {
        yield return null;
        yield return new WaitForSeconds(0.2f);

        SnapToHeadView();
        hasInitialized = true;
    }

    private void LateUpdate()
    {
        if (headTransform == null)
            return;

        if (!hasInitialized)
            return;

        Vector3 flatForward = headTransform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = transform.forward;

        flatForward.Normalize();

        Vector3 flatRight = headTransform.right;
        flatRight.y = 0f;

        if (flatRight.sqrMagnitude < 0.001f)
            flatRight = transform.right;

        flatRight.Normalize();

        Vector3 desiredPosition =
            headTransform.position +
            flatForward * distance +
            flatRight * sideOffset +
            Vector3.up * heightOffset;

        Quaternion desiredRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        float angle = Vector3.Angle(transform.forward, flatForward);

        if (angle > maxAngleBeforeFollow)
        {
            targetPosition = desiredPosition;
            targetRotation = desiredRotation;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * positionSmoothSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSmoothSpeed
        );
    }

    public void SnapToHeadView()
    {
        if (headTransform == null)
            return;

        Vector3 flatForward = headTransform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;

        flatForward.Normalize();

        Vector3 flatRight = headTransform.right;
        flatRight.y = 0f;

        if (flatRight.sqrMagnitude < 0.001f)
            flatRight = Vector3.right;

        flatRight.Normalize();

        targetPosition =
            headTransform.position +
            flatForward * distance +
            flatRight * sideOffset +
            Vector3.up * heightOffset;

        targetRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}