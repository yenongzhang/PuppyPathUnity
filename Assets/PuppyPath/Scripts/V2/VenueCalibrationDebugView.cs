using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class VenueCalibrationDebugView : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private VenueMapDefinition mapDefinition;

    [Header("Debug Drawing")]
    [SerializeField] private bool drawMapBounds = true;
    [SerializeField] private bool drawScaleSegment = true;
    [SerializeField] private bool drawWalkableAreas = true;
    [SerializeField] private bool drawObstacleAreas = true;
    [SerializeField] private bool drawNavGraph = true;
    [SerializeField] private bool drawAttractions = true;
    [SerializeField] private bool drawTestPath = true;
    [SerializeField] private bool drawLabels = true;
    [SerializeField] private float markerRadius = 0.12f;
    [SerializeField] private float mapBoundsHeight = 0.01f;
    [SerializeField] private Color originColor = Color.red;
    [SerializeField] private Color scaleColor = Color.red;
    [SerializeField] private Color boundsColor = new Color(0.8f, 0.8f, 0.8f, 0.45f);
    [SerializeField] private Color walkableColor = new Color(0.2f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color obstacleColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color navGraphColor = new Color(0.2f, 0.65f, 1f, 1f);
    [SerializeField] private Color blockedNavEdgeColor = new Color(1f, 0.25f, 0.05f, 1f);
    [SerializeField] private Color testPathColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private Color attractionColor = new Color(1f, 0.85f, 0.1f, 1f);

    [Header("Test Path")]
    [SerializeField] private Vector2 testStartPixel;
    [SerializeField] private string testDestinationAttractionId;
    [SerializeField] private float testPathHeight = 0.08f;

    [Header("Scene Editing")]
    [SerializeField] private bool editCalibrationPointsInScene = true;
    [SerializeField] private bool editAttractionsInScene = true;
    [SerializeField] private bool editWalkableAreasInScene;
    [SerializeField] private bool editObstacleAreasInScene;
    [SerializeField] private bool editNavGraphInScene = true;

    public VenueMapDefinition MapDefinition
    {
        get { return mapDefinition; }
        set { mapDefinition = value; }
    }

    public bool EditAttractionsInScene { get { return editAttractionsInScene; } }
    public bool EditCalibrationPointsInScene { get { return editCalibrationPointsInScene; } }
    public bool EditWalkableAreasInScene { get { return editWalkableAreasInScene; } }
    public bool EditObstacleAreasInScene { get { return editObstacleAreasInScene; } }
    public bool EditNavGraphInScene { get { return editNavGraphInScene; } }

    [ContextMenu("Create Map Reference Plane")]
    private void CreateMapReferencePlane()
    {
        if (mapDefinition == null)
            return;

        Transform existing = transform.Find("VenueMapReferencePlane");
        GameObject planeObject = existing != null ? existing.gameObject : new GameObject("VenueMapReferencePlane");
        planeObject.transform.SetParent(transform, false);

        VenueMapReferencePlane referencePlane = planeObject.GetComponent<VenueMapReferencePlane>();
        if (referencePlane == null)
            referencePlane = planeObject.AddComponent<VenueMapReferencePlane>();

        referencePlane.MapDefinition = mapDefinition;
        referencePlane.Rebuild();
    }

    [ContextMenu("Show Map Reference Plane")]
    private void ShowMapReferencePlane()
    {
        SetMapReferencePlaneVisible(true);
    }

    [ContextMenu("Hide Map Reference Plane")]
    private void HideMapReferencePlane()
    {
        SetMapReferencePlaneVisible(false);
    }

    private void SetMapReferencePlaneVisible(bool visible)
    {
        Transform existing = transform.Find("VenueMapReferencePlane");
        if (existing != null)
            existing.gameObject.SetActive(visible);
    }

    private void OnDrawGizmos()
    {
        if (mapDefinition == null)
            return;

        DrawOrigin();

        if (drawMapBounds)
            DrawMapBounds();

        if (drawScaleSegment)
            DrawScaleSegment();

        if (drawWalkableAreas)
            DrawWalkableAreas();

        if (drawObstacleAreas)
            DrawObstacleAreas();

        if (drawNavGraph)
            DrawNavGraph();

        if (drawAttractions)
            DrawAttractionMarkers();

        if (drawTestPath)
            DrawTestPath();
    }

    private void DrawOrigin()
    {
        Gizmos.color = originColor;
        Vector3 origin = mapDefinition.originWorldPosition;
        Gizmos.DrawSphere(origin, markerRadius * 1.2f);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, mapDefinition.venueYawDegrees, 0f) * Vector3.forward);
        DrawSceneLabel(origin + Vector3.up * 0.25f, "VenueOrigin / Photo Wall upper-right");
    }

    private void DrawMapBounds()
    {
        Vector2 size = mapDefinition.mapPixelSize;
        if (size.x <= 0f || size.y <= 0f || !mapDefinition.IsScaleReady())
            return;

        Vector3 topLeft = mapDefinition.MapPixelToWorld(Vector2.zero) + Vector3.up * mapBoundsHeight;
        Vector3 topRight = mapDefinition.MapPixelToWorld(new Vector2(size.x, 0f)) + Vector3.up * mapBoundsHeight;
        Vector3 bottomRight = mapDefinition.MapPixelToWorld(size) + Vector3.up * mapBoundsHeight;
        Vector3 bottomLeft = mapDefinition.MapPixelToWorld(new Vector2(0f, size.y)) + Vector3.up * mapBoundsHeight;

        Gizmos.color = boundsColor;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    private void DrawScaleSegment()
    {
        if (!mapDefinition.IsScaleReady())
            return;

        Vector3 a = mapDefinition.MapPixelToWorld(mapDefinition.scalePointAPixel);
        Vector3 b = mapDefinition.MapPixelToWorld(mapDefinition.scalePointBPixel);

        Gizmos.color = scaleColor;
        Gizmos.DrawSphere(a, markerRadius);
        Gizmos.DrawSphere(b, markerRadius);
        Gizmos.DrawLine(a, b);

        Vector3 labelPosition = Vector3.Lerp(a, b, 0.5f) + Vector3.up * 0.25f;
        DrawSceneLabel(labelPosition, $"Scale: {Vector3.Distance(a, b):0.00} m");
    }

    private void DrawAttractionMarkers()
    {
        if (!mapDefinition.IsScaleReady() || mapDefinition.attractions == null)
            return;

        Gizmos.color = attractionColor;

        foreach (AttractionDefinition attraction in mapDefinition.attractions)
        {
            if (attraction == null)
                continue;

            Vector3 spawn = mapDefinition.MapPixelToWorld(attraction.collectibleSpawnPixel);
            Gizmos.DrawSphere(spawn, markerRadius);

            if (attraction.hasCustomArrivalPixel)
            {
                Vector3 arrival = mapDefinition.MapPixelToWorld(attraction.arrivalPixel);
                Gizmos.DrawWireSphere(arrival, markerRadius * 1.4f);
                Gizmos.DrawLine(spawn, arrival);
            }

            string label = string.IsNullOrEmpty(attraction.displayName) ? attraction.id : attraction.displayName;
            DrawSceneLabel(spawn + Vector3.up * 0.22f, label);
        }
    }

    private void DrawWalkableAreas()
    {
        if (!mapDefinition.IsScaleReady() || mapDefinition.walkableAreas == null)
            return;

        Gizmos.color = walkableColor;

        foreach (WalkableAreaDefinition area in mapDefinition.walkableAreas)
        {
            if (area == null || area.polygonPixels == null || area.polygonPixels.Count < 2)
                continue;

            for (int i = 0; i < area.polygonPixels.Count; i++)
            {
                Vector2 currentPixel = area.polygonPixels[i];
                Vector2 nextPixel = area.polygonPixels[(i + 1) % area.polygonPixels.Count];
                Vector3 current = mapDefinition.MapPixelToWorld(currentPixel);
                Vector3 next = mapDefinition.MapPixelToWorld(nextPixel);
                Gizmos.DrawLine(current, next);
            }

            if (area.polygonPixels.Count > 0)
            {
                Vector3 labelPosition = mapDefinition.MapPixelToWorld(area.polygonPixels[0]) + Vector3.up * 0.3f;
                DrawSceneLabel(labelPosition, string.IsNullOrEmpty(area.displayName) ? area.id : area.displayName);
            }
        }
    }

    private void DrawObstacleAreas()
    {
        if (!mapDefinition.IsScaleReady() || mapDefinition.obstacleAreas == null)
            return;

        Gizmos.color = obstacleColor;

        foreach (ObstacleAreaDefinition area in mapDefinition.obstacleAreas)
        {
            if (area == null || area.polygonPixels == null || area.polygonPixels.Count < 2)
                continue;

            for (int i = 0; i < area.polygonPixels.Count; i++)
            {
                Vector2 currentPixel = area.polygonPixels[i];
                Vector2 nextPixel = area.polygonPixels[(i + 1) % area.polygonPixels.Count];
                Vector3 current = mapDefinition.MapPixelToWorld(currentPixel);
                Vector3 next = mapDefinition.MapPixelToWorld(nextPixel);
                Gizmos.DrawLine(current, next);
            }

            if (area.polygonPixels.Count > 0)
            {
                Vector3 labelPosition = mapDefinition.MapPixelToWorld(area.polygonPixels[0]) + Vector3.up * 0.3f;
                DrawSceneLabel(labelPosition, string.IsNullOrEmpty(area.displayName) ? area.id : area.displayName);
            }
        }
    }

    private void DrawNavGraph()
    {
        if (!mapDefinition.IsScaleReady() || mapDefinition.navGraph == null || mapDefinition.navGraph.nodes == null)
            return;

        Gizmos.color = navGraphColor;

        foreach (VenueNavNodeDefinition node in mapDefinition.navGraph.nodes)
        {
            if (node == null)
                continue;

            Vector3 nodeWorld = mapDefinition.MapPixelToWorld(node.mapPixel);
            Gizmos.DrawWireSphere(nodeWorld, markerRadius * 0.85f);
            DrawSceneLabel(nodeWorld + Vector3.up * 0.18f, node.id);

            if (node.neighborNodeIds == null)
                continue;

            foreach (string neighborId in node.neighborNodeIds)
            {
                VenueNavNodeDefinition neighbor = mapDefinition.navGraph.FindNode(neighborId);
                if (neighbor == null)
                    continue;

                Vector3 neighborWorld = mapDefinition.MapPixelToWorld(neighbor.mapPixel);
                Gizmos.color = mapDefinition.IsMapSegmentWalkable(node.mapPixel, neighbor.mapPixel)
                    ? navGraphColor
                    : blockedNavEdgeColor;
                Gizmos.DrawLine(nodeWorld, neighborWorld);
            }
        }
    }

    private void DrawTestPath()
    {
        if (string.IsNullOrEmpty(testDestinationAttractionId))
            return;

        AttractionDefinition attraction = mapDefinition.FindAttraction(testDestinationAttractionId);
        if (attraction == null)
            return;

        if (!VenuePathfinder.TryFindWorldPath(mapDefinition, testStartPixel, attraction.GetArrivalPixel(), outPath))
            return;

        Gizmos.color = testPathColor;
        for (int i = 0; i < outPath.Count - 1; i++)
            Gizmos.DrawLine(outPath[i] + Vector3.up * testPathHeight, outPath[i + 1] + Vector3.up * testPathHeight);
    }

    private void DrawSceneLabel(Vector3 position, string label)
    {
        if (!drawLabels || string.IsNullOrEmpty(label))
            return;

#if UNITY_EDITOR
        Handles.Label(position, label);
#endif
    }

    private readonly System.Collections.Generic.List<Vector3> outPath = new System.Collections.Generic.List<Vector3>();
}
