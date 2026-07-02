using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastPinchCollectibleDragger : MonoBehaviour
{
    [Header("Ray")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private float maxRayDistance = 12f;
    [SerializeField] private float raycastRadius = 0.04f;
    [SerializeField] private bool preferCollectibleHits = true;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Ray Visual")]
    [SerializeField] private bool showRayVisual = true;
    [SerializeField] private LineRenderer rayLine;
    [SerializeField] private float rayVisualWidth = 0.01f;
    [SerializeField] private Color rayVisualMissColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color rayVisualHitColor = new Color(1f, 0.72f, 0.15f, 0.85f);
    [SerializeField] private Color rayVisualCollectibleColor = new Color(0.3f, 1f, 0.65f, 0.95f);

    [Header("Pinch / Test Input")]
    [SerializeField] private OVRInput.RawButton primaryPinchButton = OVRInput.RawButton.RIndexTrigger;
    [SerializeField] private OVRInput.RawButton secondaryPinchButton = OVRInput.RawButton.LIndexTrigger;
    [SerializeField] private OVRInput.Controller controllerMask = OVRInput.Controller.All;
    [SerializeField] private bool acceptMouseForEditor = true;

    [Header("Drag")]
    [SerializeField] private float fallbackDragDistance = 2.0f;
    [SerializeField] private bool keepOriginalHitDistance = true;
    [SerializeField] private string dogGrabReactionState = "Sit";
    [SerializeField] private DogGuideController dogGuideController;

    private CollectibleGrabHandler activeCollectible;
    private Transform activeTransform;
    private float activeDragDistance;
    private bool wasPinching;
    private Material rayVisualMaterial;

    private void Update()
    {
        bool isPinching = IsPinching();

        if (isPinching && !wasPinching)
            TryBeginDrag();

        if (isPinching && activeTransform != null)
            UpdateDragPosition();

        if (!isPinching && wasPinching)
            EndDrag();

        UpdateRayVisual();

        wasPinching = isPinching;
    }

    private bool IsPinching()
    {
        bool controllerPinch =
            OVRInput.Get(primaryPinchButton, controllerMask) ||
            OVRInput.Get(secondaryPinchButton, controllerMask);

        if (controllerPinch)
            return true;

        return acceptMouseForEditor && Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private void TryBeginDrag()
    {
        if (!TryRaycast(out RaycastHit hit))
            return;

        CollectibleGrabHandler collectible = hit.collider.GetComponentInParent<CollectibleGrabHandler>();
        if (collectible == null)
            return;

        activeCollectible = collectible;
        activeTransform = collectible.transform;
        activeDragDistance = keepOriginalHitDistance ? Mathf.Max(0.2f, hit.distance) : fallbackDragDistance;

        if (dogGuideController != null)
            dogGuideController.SetInteractionHold(true, dogGrabReactionState);

        UpdateDragPosition();
    }

    private void UpdateDragPosition()
    {
        Ray ray = BuildRay();
        activeTransform.position = ray.GetPoint(activeDragDistance);
    }

    private void EndDrag()
    {
        if (activeCollectible != null)
            activeCollectible.TryCollectNow();

        if (dogGuideController != null)
            dogGuideController.SetInteractionHold(false);

        activeCollectible = null;
        activeTransform = null;
        activeDragDistance = 0f;
    }

    private Ray BuildRay()
    {
        if (rayOrigin != null)
            return new Ray(rayOrigin.position, rayOrigin.forward);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            return mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return new Ray(transform.position, transform.forward);
    }

    private bool TryRaycast(out RaycastHit hit)
    {
        Ray ray = BuildRay();

        if (preferCollectibleHits && TryFindCollectibleHit(ray, out hit))
            return true;

        if (raycastRadius > 0f)
            return Physics.SphereCast(ray, raycastRadius, out hit, maxRayDistance, raycastLayers, triggerInteraction);

        return Physics.Raycast(ray, out hit, maxRayDistance, raycastLayers, triggerInteraction);
    }

    private bool TryFindCollectibleHit(Ray ray, out RaycastHit hit)
    {
        RaycastHit[] hits = raycastRadius > 0f
            ? Physics.SphereCastAll(ray, raycastRadius, maxRayDistance, raycastLayers, triggerInteraction)
            : Physics.RaycastAll(ray, maxRayDistance, raycastLayers, triggerInteraction);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit? firstNonCollectibleHit = null;
        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider == null)
                continue;

            if (firstNonCollectibleHit == null)
                firstNonCollectibleHit = candidate;

            if (candidate.collider.GetComponentInParent<CollectibleGrabHandler>() != null)
            {
                hit = candidate;
                return true;
            }
        }

        if (firstNonCollectibleHit.HasValue)
        {
            hit = firstNonCollectibleHit.Value;
            return true;
        }

        hit = default;
        return false;
    }

    private void UpdateRayVisual()
    {
        if (!showRayVisual)
        {
            if (rayLine != null)
                rayLine.enabled = false;

            return;
        }

        EnsureRayVisual();
        if (rayLine == null)
            return;

        Ray ray = BuildRay();
        bool hitSomething = TryRaycast(out RaycastHit hit);
        Vector3 endPoint = hitSomething ? hit.point : ray.GetPoint(maxRayDistance);
        CollectibleGrabHandler collectible = hitSomething ? hit.collider.GetComponentInParent<CollectibleGrabHandler>() : null;

        rayLine.enabled = true;
        rayLine.SetPosition(0, ray.origin);
        rayLine.SetPosition(1, endPoint);
        SetRayVisualColor(collectible != null ? rayVisualCollectibleColor : hitSomething ? rayVisualHitColor : rayVisualMissColor);
    }

    private void EnsureRayVisual()
    {
        if (rayLine == null)
        {
            GameObject rayObject = new GameObject("CollectiblePhysicsRay");
            rayObject.transform.SetParent(transform, false);
            rayLine = rayObject.AddComponent<LineRenderer>();
        }

        rayLine.useWorldSpace = true;
        rayLine.positionCount = 2;
        rayLine.widthMultiplier = rayVisualWidth;
        rayLine.numCapVertices = 4;
        rayLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rayLine.receiveShadows = false;

        if (rayLine.sharedMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");

            if (shader != null)
            {
                rayVisualMaterial = new Material(shader);
                rayLine.sharedMaterial = rayVisualMaterial;
            }
        }
    }

    private void SetRayVisualColor(Color color)
    {
        if (rayLine == null)
            return;

        rayLine.startColor = color;
        rayLine.endColor = color;

        Material material = rayLine.sharedMaterial;
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
