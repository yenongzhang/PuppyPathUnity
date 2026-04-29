using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathPreviewController : MonoBehaviour
{
    [System.Serializable]
    public class PathEntry
    {
        public string id;
        public PathDefinition pathPrefab;
    }

    [Header("References")]
    [SerializeField] private RouteRootSpawner routeRootSpawner;

    [Header("Path Library")]
    [SerializeField] private List<PathEntry> pathEntries = new List<PathEntry>();

    [Header("Preview Line")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float lineHeightOffset = 0.03f;

    [Header("Preview Line Animation")]
    [SerializeField] private bool animatePreviewLine = true;
    [SerializeField] private float previewDrawDuration = 1.5f;
    [SerializeField] private AnimationCurve previewDrawCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<string, PathDefinition> pathMap = new Dictionary<string, PathDefinition>();

    private PathDefinition currentPathInstance;
    private LineRenderer currentLine;
    private string currentPathId;
    private Coroutine drawLineCoroutine;

    private void Awake()
    {
        BuildPathMap();
    }

    private void BuildPathMap()
    {
        pathMap.Clear();

        foreach (var entry in pathEntries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.id) && entry.pathPrefab != null)
            {
                pathMap[entry.id] = entry.pathPrefab;
            }
        }
    }

    public bool ShowPreview(string pathId)
    {
        ClearPreviewOnly();

        if (!pathMap.TryGetValue(pathId, out PathDefinition pathPrefab))
        {
            Debug.LogWarning($"PathPreviewController: path id not found: {pathId}");
            return false;
        }

        if (routeRootSpawner == null)
        {
            Debug.LogWarning("PathPreviewController: RouteRootSpawner is missing.");
            return false;
        }

        Transform routeRoot = routeRootSpawner.SpawnRouteRoot();
        if (routeRoot == null) return false;

        currentPathInstance = Instantiate(pathPrefab, routeRoot);
        currentPathInstance.transform.localPosition = Vector3.zero;
        currentPathInstance.transform.localRotation = Quaternion.identity;
        currentPathInstance.name = $"{pathPrefab.name}_Runtime";

        currentPathId = pathId;

        CreatePreviewLine(routeRoot, currentPathInstance);

        return true;
    }

    private void CreatePreviewLine(Transform routeRoot, PathDefinition pathDefinition)
    {
        List<Vector3> previewPoints = BuildPreviewPoints(pathDefinition);
        if (previewPoints.Count < 2)
        {
            Debug.LogWarning("PathPreviewController: need at least 2 valid waypoints for preview.");
            return;
        }

        GameObject lineObj = new GameObject("PreviewLine");
        lineObj.transform.SetParent(routeRoot, false);

        currentLine = lineObj.AddComponent<LineRenderer>();
        currentLine.useWorldSpace = true;
        currentLine.material = lineMaterial;
        currentLine.widthMultiplier = lineWidth;
        currentLine.positionCount = 0;
        currentLine.numCapVertices = 8;
        currentLine.numCornerVertices = 8;
        currentLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        currentLine.receiveShadows = false;

        if (animatePreviewLine && previewDrawDuration > 0f)
        {
            drawLineCoroutine = StartCoroutine(AnimatePreviewLine(previewPoints));
        }
        else
        {
            SetLineToFullPath(previewPoints);
        }
    }

    private List<Vector3> BuildPreviewPoints(PathDefinition pathDefinition)
    {
        List<Vector3> previewPoints = new List<Vector3>();

        if (pathDefinition == null || pathDefinition.waypoints == null)
            return previewPoints;

        for (int i = 0; i < pathDefinition.waypoints.Count; i++)
        {
            Transform waypoint = pathDefinition.waypoints[i];
            if (waypoint == null) continue;

            Vector3 point = waypoint.position + Vector3.up * lineHeightOffset;
            previewPoints.Add(point);
        }

        return previewPoints;
    }

    private IEnumerator AnimatePreviewLine(List<Vector3> points)
    {
        if (currentLine == null || points == null || points.Count < 2)
            yield break;

        float totalLength = CalculatePathLength(points);
        if (totalLength <= 0.001f)
        {
            SetLineToFullPath(points);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < previewDrawDuration)
        {
            elapsed += Time.deltaTime;

            float rawProgress = Mathf.Clamp01(elapsed / previewDrawDuration);
            float curvedProgress = previewDrawCurve != null ? previewDrawCurve.Evaluate(rawProgress) : rawProgress;
            float targetDistance = Mathf.Clamp01(curvedProgress) * totalLength;

            SetLineByDistance(points, targetDistance);

            yield return null;
        }

        SetLineToFullPath(points);
        drawLineCoroutine = null;
    }

    private float CalculatePathLength(List<Vector3> points)
    {
        float length = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            length += Vector3.Distance(points[i], points[i + 1]);
        }

        return length;
    }

    private void SetLineByDistance(List<Vector3> points, float targetDistance)
    {
        if (currentLine == null || points == null || points.Count < 2)
            return;

        List<Vector3> visiblePoints = new List<Vector3>();
        visiblePoints.Add(points[0]);

        float travelled = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];
            float segmentLength = Vector3.Distance(start, end);

            if (segmentLength <= 0.001f)
                continue;

            if (travelled + segmentLength < targetDistance)
            {
                visiblePoints.Add(end);
                travelled += segmentLength;
            }
            else
            {
                float remainingDistance = targetDistance - travelled;
                float segmentProgress = Mathf.Clamp01(remainingDistance / segmentLength);
                Vector3 animatedEndPoint = Vector3.Lerp(start, end, segmentProgress);
                visiblePoints.Add(animatedEndPoint);
                break;
            }
        }

        if (visiblePoints.Count == 1)
            visiblePoints.Add(points[0]);

        currentLine.positionCount = visiblePoints.Count;
        currentLine.SetPositions(visiblePoints.ToArray());
    }

    private void SetLineToFullPath(List<Vector3> points)
    {
        if (currentLine == null || points == null || points.Count < 2)
            return;

        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());
    }

    public PathDefinition GetCurrentPathInstance()
    {
        return currentPathInstance;
    }

    public Transform GetCurrentRouteRoot()
    {
        return routeRootSpawner != null ? routeRootSpawner.GetCurrentRouteRoot() : null;
    }

    public string GetCurrentPathId()
    {
        return currentPathId;
    }

    public Transform GetCurrentDestinationWaypoint()
    {
        if (currentPathInstance == null || currentPathInstance.waypoints == null)
            return null;

        for (int i = currentPathInstance.waypoints.Count - 1; i >= 0; i--)
        {
            if (currentPathInstance.waypoints[i] != null)
                return currentPathInstance.waypoints[i];
        }

        return null;
    }

    public bool TryGetCurrentDestinationPosition(out Vector3 destinationPosition)
    {
        Transform destinationWaypoint = GetCurrentDestinationWaypoint();
        if (destinationWaypoint == null)
        {
            destinationPosition = Vector3.zero;
            return false;
        }

        destinationPosition = destinationWaypoint.position;
        return true;
    }

    public void ClearPreviewOnly()
    {
        if (drawLineCoroutine != null)
        {
            StopCoroutine(drawLineCoroutine);
            drawLineCoroutine = null;
        }

        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }

        if (currentPathInstance != null)
        {
            Transform root = currentPathInstance.transform.parent;
            Destroy(currentPathInstance.gameObject);
            currentPathInstance = null;

            if (root != null && root.childCount == 0)
            {
                // Destroy is delayed, so do not destroy root here.
            }
        }

        currentPathId = null;
    }

    public void ClearAll()
    {
        ClearPreviewOnly();

        if (routeRootSpawner != null)
            routeRootSpawner.ClearCurrentRouteRoot();
    }
}
