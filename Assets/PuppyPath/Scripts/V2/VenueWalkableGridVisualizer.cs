using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VenueWalkableGridVisualizer : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private VenueMapDefinition mapDefinition;

    [Header("Grid")]
    [SerializeField] private float cellSizeMeters = 0.5f;
    [SerializeField, Range(0.35f, 1f)] private float cellFillRatio = 0.82f;
    [SerializeField] private float yOffset = 0.015f;
    [SerializeField] private int maxCells = 12000;

    [Header("Material")]
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private Material routeOverrideMaterial;
    [SerializeField] private Color gridColor = new Color(1f, 0.82f, 0.08f, 0.42f);
    [SerializeField] private Color routeGridColor = new Color(0.66f, 0.32f, 1f, 0.72f);
    [SerializeField] private float routeHighlightRadiusMeters = 0.45f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;
    private Material routeRuntimeMaterial;
    private readonly List<Vector2> routeMapPixels = new List<Vector2>();

    public VenueMapDefinition MapDefinition
    {
        get { return mapDefinition; }
        set
        {
            mapDefinition = value;
            Rebuild();
        }
    }

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Walkable Grid")]
    public void Rebuild()
    {
        ResolveComponents();

        if (meshFilter == null || meshRenderer == null || mapDefinition == null || !mapDefinition.IsScaleReady())
            return;

        float metersPerPixel = mapDefinition.MetersPerPixel;
        if (metersPerPixel <= Mathf.Epsilon || cellSizeMeters <= 0.05f)
            return;

        float cellSizePixels = cellSizeMeters / metersPerPixel;
        if (cellSizePixels <= 1f)
            return;

        Vector2 mapSize = mapDefinition.mapPixelSize;
        int columns = Mathf.CeilToInt(mapSize.x / cellSizePixels);
        int rows = Mathf.CeilToInt(mapSize.y / cellSizePixels);

        List<Vector3> vertices = new List<Vector3>();
        List<int> walkableTriangles = new List<int>();
        List<int> routeTriangles = new List<int>();
        int cellCount = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (cellCount >= maxCells)
                    break;

                Vector2 centerPixel = new Vector2(
                    (column + 0.5f) * cellSizePixels,
                    (row + 0.5f) * cellSizePixels);

                if (centerPixel.x > mapSize.x || centerPixel.y > mapSize.y)
                    continue;

                if (!mapDefinition.IsMapPixelWalkable(centerPixel))
                    continue;

                AddCell(
                    vertices,
                    IsRouteCell(centerPixel) ? routeTriangles : walkableTriangles,
                    centerPixel,
                    cellSizePixels * cellFillRatio);
                cellCount++;
            }
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = "Venue Walkable Grid" };
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        if (vertices.Count > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(walkableTriangles, 0);
        mesh.SetTriangles(routeTriangles, 1);
        mesh.RecalculateBounds();

        ApplyMaterial();
    }

    public void ShowRoutePixels(IList<Vector2> mapPixels)
    {
        routeMapPixels.Clear();

        if (mapPixels != null)
        {
            for (int i = 0; i < mapPixels.Count; i++)
                routeMapPixels.Add(mapPixels[i]);
        }

        Rebuild();
    }

    public void ShowWorldRoute(IList<Vector3> worldPoints)
    {
        routeMapPixels.Clear();

        if (mapDefinition != null && worldPoints != null)
        {
            for (int i = 0; i < worldPoints.Count; i++)
                routeMapPixels.Add(mapDefinition.WorldToMapPixel(transform.InverseTransformPoint(worldPoints[i])));
        }

        Rebuild();
    }

    public void ClearRoute()
    {
        routeMapPixels.Clear();
        Rebuild();
    }

    private void AddCell(List<Vector3> vertices, List<int> triangles, Vector2 centerPixel, float sizePixels)
    {
        float halfSize = sizePixels * 0.5f;

        Vector3 a = mapDefinition.MapPixelToWorld(centerPixel + new Vector2(-halfSize, -halfSize)) + Vector3.up * yOffset;
        Vector3 b = mapDefinition.MapPixelToWorld(centerPixel + new Vector2(halfSize, -halfSize)) + Vector3.up * yOffset;
        Vector3 c = mapDefinition.MapPixelToWorld(centerPixel + new Vector2(halfSize, halfSize)) + Vector3.up * yOffset;
        Vector3 d = mapDefinition.MapPixelToWorld(centerPixel + new Vector2(-halfSize, halfSize)) + Vector3.up * yOffset;

        int startIndex = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
    }

    private bool IsRouteCell(Vector2 centerPixel)
    {
        if (routeMapPixels.Count < 2 || mapDefinition == null)
            return false;

        float metersPerPixel = mapDefinition.MetersPerPixel;
        if (metersPerPixel <= Mathf.Epsilon)
            return false;

        float radiusPixels = Mathf.Max(0f, routeHighlightRadiusMeters) / metersPerPixel;
        float radiusSqr = radiusPixels * radiusPixels;

        for (int i = 0; i < routeMapPixels.Count - 1; i++)
        {
            if (DistancePointToSegmentSqr(centerPixel, routeMapPixels[i], routeMapPixels[i + 1]) <= radiusSqr)
                return true;
        }

        return false;
    }

    private static float DistancePointToSegmentSqr(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float lengthSqr = segment.sqrMagnitude;
        if (lengthSqr <= Mathf.Epsilon)
            return (point - a).sqrMagnitude;

        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSqr);
        Vector2 projection = a + segment * t;
        return (point - projection).sqrMagnitude;
    }

    private void ResolveComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    private void ApplyMaterial()
    {
        Material materialToUse = overrideMaterial;
        if (materialToUse == null)
            materialToUse = GetOrCreateRuntimeMaterial();

        if (materialToUse == null)
            return;

        if (materialToUse.HasProperty("_Color"))
            materialToUse.color = gridColor;
        if (materialToUse.HasProperty("_BaseColor"))
            materialToUse.SetColor("_BaseColor", gridColor);

        ConfigureTransparentMaterial(materialToUse);
        Material routeMaterialToUse = routeOverrideMaterial;
        if (routeMaterialToUse == null)
            routeMaterialToUse = GetOrCreateRouteRuntimeMaterial();

        if (routeMaterialToUse != null)
        {
            if (routeMaterialToUse.HasProperty("_Color"))
                routeMaterialToUse.color = routeGridColor;
            if (routeMaterialToUse.HasProperty("_BaseColor"))
                routeMaterialToUse.SetColor("_BaseColor", routeGridColor);
            ConfigureTransparentMaterial(routeMaterialToUse);
        }

        meshRenderer.sharedMaterials = new[] { materialToUse, routeMaterialToUse != null ? routeMaterialToUse : materialToUse };
    }

    private Material GetOrCreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return runtimeMaterial;

        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return null;

        runtimeMaterial = new Material(shader)
        {
            name = "Venue Walkable Grid Runtime Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.color = gridColor;

        return runtimeMaterial;
    }

    private Material GetOrCreateRouteRuntimeMaterial()
    {
        if (routeRuntimeMaterial != null)
            return routeRuntimeMaterial;

        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return null;

        routeRuntimeMaterial = new Material(shader)
        {
            name = "Venue Route Grid Runtime Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (routeRuntimeMaterial.HasProperty("_Color"))
            routeRuntimeMaterial.color = routeGridColor;
        if (routeRuntimeMaterial.HasProperty("_BaseColor"))
            routeRuntimeMaterial.SetColor("_BaseColor", routeGridColor);

        ConfigureTransparentMaterial(routeRuntimeMaterial);
        return routeRuntimeMaterial;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
