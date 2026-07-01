using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VenueMapReferencePlane : MonoBehaviour
{
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private float yOffset = -0.02f;
    [SerializeField, Range(0.05f, 1f)] private float opacity = 0.45f;

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

    [ContextMenu("Rebuild Map Plane")]
    public void Rebuild()
    {
        ResolveComponents();

        if (meshFilter == null || meshRenderer == null || mapDefinition == null || !mapDefinition.IsScaleReady())
            return;

        Vector2 size = mapDefinition.mapPixelSize;
        if (size.x <= 0f || size.y <= 0f)
            return;

        Vector3 topLeft = mapDefinition.MapPixelToWorld(Vector2.zero) + Vector3.up * yOffset;
        Vector3 topRight = mapDefinition.MapPixelToWorld(new Vector2(size.x, 0f)) + Vector3.up * yOffset;
        Vector3 bottomRight = mapDefinition.MapPixelToWorld(size) + Vector3.up * yOffset;
        Vector3 bottomLeft = mapDefinition.MapPixelToWorld(new Vector2(0f, size.y)) + Vector3.up * yOffset;

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = "Venue Map Reference Plane" };
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.vertices = new[] { topLeft, topRight, bottomRight, bottomLeft };
        mesh.uv = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();

        ApplyMaterial();
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
        if (meshRenderer == null)
            return;

        Material materialToUse = overrideMaterial;
        if (materialToUse == null)
            materialToUse = GetOrCreateRuntimeMaterial();

        if (materialToUse == null)
            return;

        if (mapDefinition.mapTexture != null)
            materialToUse.mainTexture = mapDefinition.mapTexture;

        if (materialToUse.HasProperty("_Color"))
        {
            Color color = materialToUse.color;
            color.a = opacity;
            materialToUse.color = color;
        }

        meshRenderer.sharedMaterial = materialToUse;
    }

    private Material GetOrCreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return runtimeMaterial;

        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        if (shader == null)
            return null;

        runtimeMaterial = new Material(shader)
        {
            name = "Venue Map Reference Runtime Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        return runtimeMaterial;
    }
}
