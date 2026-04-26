using UnityEngine;

public class DogEyeFollowRay : MonoBehaviour
{
    [Header("Eye")]
    [SerializeField] private RectTransform[] eyeRects;

    [Header("Canvas")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Camera eventCamera;

    [Header("Ray Sources")]
    [Tooltip("Drag both controller pointer pose and hand pointer pose here.")]
    [SerializeField] private Transform[] rayOrigins;

    [SerializeField] private bool invertRayDirection = false;

    [Header("Editor Fallback")]
    [SerializeField] private bool useMouseInEditor = true;

    [Header("Rotation")]
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float angleOffset = 0f;
    [SerializeField] private float neutralAngle = 0f;
    [SerializeField] private float smoothSpeed = 12f;

    [Header("Behavior")]
    [SerializeField] private bool returnToNeutralWhenNoHit = true;

    private void Reset()
    {
        RectTransform self = GetComponent<RectTransform>();
        if (self != null)
            eyeRects = new RectTransform[] { self };
    }

    private void Awake()
    {
        if (eyeRects == null || eyeRects.Length == 0)
        {
            RectTransform self = GetComponent<RectTransform>();
            if (self != null)
                eyeRects = new RectTransform[] { self };
        }
    }

    private void LateUpdate()
    {
        if (eyeRects == null || eyeRects.Length == 0)
            return;

        bool hasTarget = TryGetTargetScreenPosition(out Vector2 targetScreenPosition);

        if (!hasTarget)
        {
            if (returnToNeutralWhenNoHit)
                RotateEyesTo(neutralAngle);

            return;
        }

        foreach (RectTransform eye in eyeRects)
        {
            if (eye == null)
                continue;

            Vector2 eyeScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, eye.position);
            Vector2 direction = targetScreenPosition - eyeScreenPosition;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float finalAngle = angle + angleOffset;

            finalAngle = NormalizeAngle(finalAngle);
            finalAngle = Mathf.Clamp(finalAngle, -maxAngle, maxAngle);

            RotateEyeTo(eye, finalAngle);
        }
    }

    private bool TryGetTargetScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

#if UNITY_EDITOR
        if (useMouseInEditor && Input.mousePresent)
        {
            screenPosition = Input.mousePosition;
            return true;
        }
#endif

        if (rayOrigins == null || rayOrigins.Length == 0 || canvasRect == null)
            return false;

        foreach (Transform origin in rayOrigins)
        {
            if (origin == null)
                continue;

            if (!origin.gameObject.activeInHierarchy)
                continue;

            if (TryGetCanvasHitFromRay(origin, out screenPosition))
                return true;
        }

        return false;
    }

    private bool TryGetCanvasHitFromRay(Transform origin, out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        Vector3 direction = invertRayDirection ? -origin.forward : origin.forward;
        Ray ray = new Ray(origin.position, direction);

        Plane canvasPlane = new Plane(canvasRect.forward, canvasRect.position);

        if (!canvasPlane.Raycast(ray, out float enter))
            return false;

        Vector3 hitWorldPosition = ray.GetPoint(enter);
        Vector2 hitScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, hitWorldPosition);

        bool gotLocalPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            hitScreenPosition,
            eventCamera,
            out Vector2 localPoint
        );

        if (!gotLocalPoint)
            return false;

        if (!canvasRect.rect.Contains(localPoint))
            return false;

        screenPosition = hitScreenPosition;
        return true;
    }

    private void RotateEyesTo(float zAngle)
    {
        foreach (RectTransform eye in eyeRects)
        {
            if (eye == null)
                continue;

            RotateEyeTo(eye, zAngle);
        }
    }

    private void RotateEyeTo(RectTransform eye, float targetZ)
    {
        float currentZ = NormalizeAngle(eye.localEulerAngles.z);
        float newZ = Mathf.LerpAngle(currentZ, targetZ, Time.deltaTime * smoothSpeed);

        eye.localRotation = Quaternion.Euler(0f, 0f, newZ);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
            angle -= 360f;

        if (angle < -180f)
            angle += 360f;

        return angle;
    }
}