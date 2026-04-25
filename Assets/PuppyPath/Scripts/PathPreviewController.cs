using UnityEngine;
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
    [SerializeField] private float secondSegmentPreviewRatio = 0.35f;

    private readonly Dictionary<string, PathDefinition> pathMap = new Dictionary<string, PathDefinition>();

    private PathDefinition currentPathInstance;
    private LineRenderer currentLine;
    private string currentPathId;

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
        List<Transform> waypoints = pathDefinition.waypoints;
        if (waypoints == null || waypoints.Count < 2)
        {
            Debug.LogWarning("PathPreviewController: need at least 2 waypoints for preview.");
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

        List<Vector3> previewPoints = new List<Vector3>();

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            Vector3 point = waypoints[i].position + Vector3.up * lineHeightOffset;
            previewPoints.Add(point);
        }

        if (previewPoints.Count < 2)
        {
            Debug.LogWarning("PathPreviewController: not enough valid preview points.");
            return;
        }

        currentLine.positionCount = previewPoints.Count;
        currentLine.SetPositions(previewPoints.ToArray());
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
                // 不一定可靠，因为 Destroy 是延迟的，所以这里不删 root
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
