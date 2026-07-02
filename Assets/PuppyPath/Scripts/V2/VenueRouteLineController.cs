using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VenueRouteLineController : MonoBehaviour
{
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private VenueWalkableGridVisualizer walkableGridVisualizer;
    [SerializeField] private bool drawLineRenderer;
    [SerializeField] private float lineHeightOffset = 0.08f;
    [SerializeField] private float lineWidth = 0.07f;
    [SerializeField] private Color routeColor = new Color(0.45f, 0.85f, 1f, 1f);
    [SerializeField] private bool configureLineRendererOnStart = true;
    [SerializeField] private bool useRuntimeUnlitMaterial = true;
    [SerializeField] private string destinationAttractionId;
    [SerializeField] private Vector2 testStartPixel;

    private readonly List<Vector3> routeWorldPoints = new List<Vector3>();
    private LineRenderer lineRenderer;
    private Material runtimeMaterial;

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
        ConfigureLineRenderer();
    }

    private void Start()
    {
        if (configureLineRendererOnStart)
            ConfigureLineRenderer();
    }

    private void OnValidate()
    {
        ResolveLineRenderer();
        ConfigureLineRenderer();
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
        if (mapDefinition == null)
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
        if (worldPoints == null || worldPoints.Count < 2)
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

        if (walkableGridVisualizer != null)
            walkableGridVisualizer.ClearRoute();
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
        if (walkableGridVisualizer != null)
            walkableGridVisualizer.ShowWorldRoute(worldPoints);

        if (!drawLineRenderer)
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
            return;
        }

        ResolveLineRenderer();
        if (lineRenderer == null)
            return;

        ConfigureLineRenderer();

        lineRenderer.positionCount = worldPoints.Count;
        for (int i = 0; i < worldPoints.Count; i++)
            lineRenderer.SetPosition(i, worldPoints[i] + Vector3.up * lineHeightOffset);
    }

    [ContextMenu("Configure Line Renderer")]
    private void ConfigureLineRenderer()
    {
        ResolveLineRenderer();
        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.numCornerVertices = Mathf.Max(lineRenderer.numCornerVertices, 4);
        lineRenderer.numCapVertices = Mathf.Max(lineRenderer.numCapVertices, 4);
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.generateLightingData = false;

        if (useRuntimeUnlitMaterial || lineRenderer.sharedMaterial == null || lineRenderer.sharedMaterial.name == "Default-Line")
            lineRenderer.sharedMaterial = GetOrCreateRuntimeMaterial();

        Material material = lineRenderer.sharedMaterial;
        if (material != null)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", routeColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", routeColor);
        }
    }

    private Material GetOrCreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return runtimeMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return null;

        runtimeMaterial = new Material(shader)
        {
            name = "Venue Route Line Runtime Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", routeColor);
        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", routeColor);

        return runtimeMaterial;
    }
}
