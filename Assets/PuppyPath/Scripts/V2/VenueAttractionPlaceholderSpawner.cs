using System.Collections.Generic;
using UnityEngine;

public class VenueAttractionPlaceholderSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform venueContentRoot;
    [SerializeField] private Transform placeholderParent;

    [Header("Placeholder")]
    [SerializeField] private GameObject placeholderPrefab;
    [SerializeField] private float sphereDiameter = 0.22f;
    [SerializeField] private float heightOffset = 0.55f;
    [SerializeField] private Color defaultColor = new Color(1f, 0.6f, 0.12f, 1f);
    [SerializeField] private bool rebuildOnStart = true;

    private readonly List<GameObject> spawnedPlaceholders = new List<GameObject>();

    private void Start()
    {
        if (rebuildOnStart)
            RebuildPlaceholders();
    }

    [ContextMenu("Rebuild Attraction Placeholders")]
    public void RebuildPlaceholders()
    {
        ClearPlaceholders();

        if (mapDefinition == null || mapDefinition.attractions == null)
            return;

        Transform parent = GetPlaceholderParent();
        for (int i = 0; i < mapDefinition.attractions.Count; i++)
        {
            AttractionDefinition attraction = mapDefinition.attractions[i];
            if (attraction == null || string.IsNullOrWhiteSpace(attraction.id))
                continue;

            GameObject placeholder = CreatePlaceholder(attraction, i);
            placeholder.transform.SetParent(parent, true);
            placeholder.transform.position = VenueLocalToWorld(mapDefinition.MapPixelToWorld(attraction.collectibleSpawnPixel)) + Vector3.up * heightOffset;
            placeholder.name = "CollectiblePlaceholder_" + attraction.id;
            spawnedPlaceholders.Add(placeholder);
        }
    }

    [ContextMenu("Clear Attraction Placeholders")]
    public void ClearPlaceholders()
    {
        Transform parent = GetPlaceholderParent();
        if (parent != null)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name.StartsWith("CollectiblePlaceholder_"))
                    DestroyPlaceholder(child.gameObject);
            }
        }

        for (int i = spawnedPlaceholders.Count - 1; i >= 0; i--)
        {
            GameObject placeholder = spawnedPlaceholders[i];
            if (placeholder == null)
                continue;

            DestroyPlaceholder(placeholder);
        }

        spawnedPlaceholders.Clear();
    }

    private GameObject CreatePlaceholder(AttractionDefinition attraction, int index)
    {
        GameObject placeholder = placeholderPrefab != null
            ? Instantiate(placeholderPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        placeholder.transform.localScale = Vector3.one * sphereDiameter;

        Renderer renderer = placeholder.GetComponentInChildren<Renderer>();
        if (renderer != null && placeholderPrefab == null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color color = Color.HSVToRGB((index * 0.17f) % 1f, 0.75f, 1f);
            color.a = defaultColor.a;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
        }

        return placeholder;
    }

    private Transform GetPlaceholderParent()
    {
        if (placeholderParent != null)
            return placeholderParent;

        return venueContentRoot != null ? venueContentRoot : transform;
    }

    private Vector3 VenueLocalToWorld(Vector3 localPosition)
    {
        return venueContentRoot != null ? venueContentRoot.TransformPoint(localPosition) : localPosition;
    }

    private static void DestroyPlaceholder(GameObject placeholder)
    {
        if (placeholder == null)
            return;

        if (Application.isPlaying)
            Destroy(placeholder);
        else
            DestroyImmediate(placeholder);
    }
}
