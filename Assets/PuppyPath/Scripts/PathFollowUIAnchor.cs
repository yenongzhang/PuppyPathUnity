using UnityEngine;
using System.Collections.Generic;

public class PathFollowUIAnchor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationRuntimeController runtimeController;

    [Header("Follow Settings")]
    [SerializeField] private float uiLeadDistance = 1.6f;
    [SerializeField] private float uiHeightOffset = 1.2f;
    [SerializeField] private float moveSmooth = 5f;
    [SerializeField] private float rotateSmooth = 6f;

    [Header("Visibility")]
    [SerializeField] private bool onlyFollowWhenActive = true;

    private void Update()
    {
        if (onlyFollowWhenActive && !gameObject.activeInHierarchy)
            return;

        if (xrCamera == null || previewController == null || runtimeController == null)
            return;

        PathDefinition path = previewController.GetCurrentPathInstance();
        if (path == null || path.waypoints == null || path.waypoints.Count < 2)
            return;

        Vector3 userPos = xrCamera.position;
        Vector3 targetPos = GetPointAheadOnPath(path.waypoints, userPos, uiLeadDistance);
        targetPos.y += uiHeightOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            moveSmooth * Time.deltaTime
        );

        Vector3 lookDir = xrCamera.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotateSmooth * Time.deltaTime
            );
        }
    }

    private Vector3 GetPointAheadOnPath(List<Transform> waypoints, Vector3 userPos, float leadDistance)
    {
        int closestSegment = FindClosestSegmentIndex(waypoints, userPos);

        Vector3 a = waypoints[closestSegment].position;
        Vector3 b = waypoints[closestSegment + 1].position;

        Vector3 flatA = new Vector3(a.x, 0f, a.z);
        Vector3 flatB = new Vector3(b.x, 0f, b.z);
        Vector3 flatUser = new Vector3(userPos.x, 0f, userPos.z);

        Vector3 ab = flatB - flatA;
        float abLen = ab.magnitude;

        if (abLen < 0.001f)
            return a;

        Vector3 abDir = ab / abLen;
        float t = Mathf.Clamp(Vector3.Dot(flatUser - flatA, abDir), 0f, abLen);

        float remainingDistance = leadDistance;
        Vector3 currentPoint = flatA + abDir * t;
        int seg = closestSegment;

        while (seg < waypoints.Count - 1)
        {
            Vector3 segStart = currentPoint;
            Vector3 segEnd = new Vector3(
                waypoints[seg + 1].position.x,
                0f,
                waypoints[seg + 1].position.z
            );

            float segDist = Vector3.Distance(segStart, segEnd);

            if (remainingDistance <= segDist)
            {
                Vector3 result = Vector3.Lerp(segStart, segEnd, remainingDistance / segDist);
                return new Vector3(result.x, waypoints[seg].position.y, result.z);
            }

            remainingDistance -= segDist;
            seg++;

            if (seg < waypoints.Count - 1)
            {
                currentPoint = new Vector3(
                    waypoints[seg].position.x,
                    0f,
                    waypoints[seg].position.z
                );
            }
        }

        Transform last = waypoints[waypoints.Count - 1];
        return last.position;
    }

    private int FindClosestSegmentIndex(List<Transform> waypoints, Vector3 userPos)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float d = GetDistanceToSegmentXZ(userPos, waypoints[i].position, waypoints[i + 1].position);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private float GetDistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 p1 = new Vector2(a.x, a.z);
        Vector2 p2 = new Vector2(b.x, b.z);

        Vector2 segment = p2 - p1;
        float lenSq = segment.sqrMagnitude;

        if (lenSq < 0.0001f)
            return Vector2.Distance(p, p1);

        float t = Mathf.Clamp01(Vector2.Dot(p - p1, segment) / lenSq);
        Vector2 projection = p1 + segment * t;

        return Vector2.Distance(p, projection);
    }
}