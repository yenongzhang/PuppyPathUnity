using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VenueRouteLineController : MonoBehaviour
{
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private float lineHeightOffset = 0.03f;
    [SerializeField] private string destinationAttractionId;
    [SerializeField] private Vector2 testStartPixel;

    private readonly List<Vector3> routeWorldPoints = new List<Vector3>();
    private LineRenderer lineRenderer;

    public VenueMapDefinition MapDefinition
    {
        get { return mapDefinition; }
        set { mapDefinition = value; }
    }

    public IReadOnlyList<Vector3> RouteWorldPoints
    {
        get { return routeWorldPoints; }
    }

    private void Awake()
    {
        ResolveLineRenderer();
    }

    private void OnValidate()
    {
        ResolveLineRenderer();
    }

    public bool ShowRouteToAttraction(string attractionId, Vector2 startPixel)
    {
        if (mapDefinition == null)
            return false;

        AttractionDefinition attraction = mapDefinition.FindAttraction(attractionId);
        if (attraction == null)
            return false;

        return ShowRoute(startPixel, attraction.GetArrivalPixel());
    }

    public bool ShowRoute(Vector2 startPixel, Vector2 endPixel)
    {
        ResolveLineRenderer();
        if (lineRenderer == null || mapDefinition == null)
            return false;

        if (!VenuePathfinder.TryFindWorldPath(mapDefinition, startPixel, endPixel, routeWorldPoints))
        {
            ClearRoute();
            return false;
        }

        ApplyWorldRoute(routeWorldPoints);

        return true;
    }

    public bool ShowWorldRoute(IList<Vector3> worldPoints)
    {
        ResolveLineRenderer();
        if (lineRenderer == null || worldPoints == null || worldPoints.Count < 2)
        {
            ClearRoute();
            return false;
        }

        routeWorldPoints.Clear();
        for (int i = 0; i < worldPoints.Count; i++)
            routeWorldPoints.Add(worldPoints[i]);

        ApplyWorldRoute(routeWorldPoints);
        return true;
    }

    public void ClearRoute()
    {
        routeWorldPoints.Clear();

        ResolveLineRenderer();
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    [ContextMenu("Show Test Route")]
    private void ShowTestRoute()
    {
        ShowRouteToAttraction(destinationAttractionId, testStartPixel);
    }

    private void ResolveLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void ApplyWorldRoute(IList<Vector3> worldPoints)
    {
        lineRenderer.positionCount = worldPoints.Count;
        for (int i = 0; i < worldPoints.Count; i++)
            lineRenderer.SetPosition(i, worldPoints[i] + Vector3.up * lineHeightOffset);
    }
}
