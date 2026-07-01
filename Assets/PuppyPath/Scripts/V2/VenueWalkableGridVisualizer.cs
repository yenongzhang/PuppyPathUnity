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
    [SerializeField] private Color gridColor = new Color(1f, 0.82f, 0.08f, 0.42f);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;

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
        List<int> triangles = new List<int>();
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

                AddCell(vertices, triangles, centerPixel, cellSizePixels * cellFillRatio);
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
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        ApplyMaterial();
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

        ConfigureTransparentMaterial(materialToUse);
        meshRenderer.sharedMaterial = materialToUse;
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
