using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class VenueOriginTestAttractionGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform venueContentRoot;

    [Header("Generated Attractions")]
    [SerializeField] private string idPrefix = "origin_test";
    [SerializeField] private int attractionCount = 3;
    [SerializeField] private float searchRadiusMeters = 3.5f;
    [SerializeField] private float minSpacingMeters = 0.85f;
    [SerializeField] private float sampleStepMeters = 0.35f;
    [SerializeField] private bool replaceExistingGenerated = true;

    [Header("Nav Graph")]
    [SerializeField] private float connectExistingNodeRadiusMeters = 2.2f;
    [SerializeField] private float connectGeneratedNodeRadiusMeters = 1.8f;

    [ContextMenu("Generate Origin Test Attractions")]
    public void GenerateOriginTestAttractions()
    {
        if (mapDefinition == null || !mapDefinition.IsScaleReady())
        {
            Debug.LogWarning("VenueOriginTestAttractionGenerator: missing map definition or map scale.");
            return;
        }

        if (replaceExistingGenerated)
        {
            RemoveGeneratedAttractionsAndNodes();
            RemoveGeneratedPlaceholders();
        }

        List<Vector2> selectedPixels = FindOriginTestPixels();
        if (selectedPixels.Count == 0)
        {
            Debug.LogWarning("VenueOriginTestAttractionGenerator: no walkable test pixels found near origin.");
            return;
        }

        for (int i = 0; i < selectedPixels.Count; i++)
        {
            string id = idPrefix + "_" + (i + 1);
            Vector2 pixel = selectedPixels[i];

            AddOrUpdateAttraction(id, "Origin Test " + (i + 1), pixel);
            AddOrUpdateNavNode(id + "_arrival", pixel);
        }

        ConnectGeneratedNavNodes(selectedPixels.Count);

#if UNITY_EDITOR
        EditorUtility.SetDirty(mapDefinition);
#endif

        Debug.Log("VenueOriginTestAttractionGenerator: generated " + selectedPixels.Count + " origin test attractions.");
    }

    [ContextMenu("Remove Origin Test Attractions")]
    public void RemoveGeneratedAttractionsAndNodes()
    {
        if (mapDefinition == null)
            return;

        if (mapDefinition.attractions != null)
        {
            for (int i = mapDefinition.attractions.Count - 1; i >= 0; i--)
            {
                AttractionDefinition attraction = mapDefinition.attractions[i];
                if (attraction != null && !string.IsNullOrEmpty(attraction.id) && attraction.id.StartsWith(idPrefix))
                    mapDefinition.attractions.RemoveAt(i);
            }
        }

        if (mapDefinition.navGraph != null && mapDefinition.navGraph.nodes != null)
        {
            HashSet<string> removedIds = new HashSet<string>();
            for (int i = mapDefinition.navGraph.nodes.Count - 1; i >= 0; i--)
            {
                VenueNavNodeDefinition node = mapDefinition.navGraph.nodes[i];
                if (node != null && !string.IsNullOrEmpty(node.id) && node.id.StartsWith(idPrefix))
                {
                    removedIds.Add(node.id);
                    mapDefinition.navGraph.nodes.RemoveAt(i);
                }
            }

            foreach (VenueNavNodeDefinition node in mapDefinition.navGraph.nodes)
            {
                if (node == null || node.neighborNodeIds == null)
                    continue;

                for (int i = node.neighborNodeIds.Count - 1; i >= 0; i--)
                {
                    if (removedIds.Contains(node.neighborNodeIds[i]))
                        node.neighborNodeIds.RemoveAt(i);
                }
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(mapDefinition);
#endif
    }

    [ContextMenu("Remove Legacy Origin Test Placeholder Spheres")]
    public void RemoveGeneratedPlaceholders()
    {
        RemoveGeneratedPlaceholdersInParent(transform);

        if (venueContentRoot != null && venueContentRoot != transform)
            RemoveGeneratedPlaceholdersInParent(venueContentRoot);
    }

    private void RemoveGeneratedPlaceholdersInParent(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (!child.name.StartsWith(idPrefix) || !child.name.Contains("placeholder_sphere"))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private List<Vector2> FindOriginTestPixels()
    {
        List<Vector2> selected = new List<Vector2>();
        int targetCount = Mathf.Max(1, attractionCount);
        float step = Mathf.Max(0.15f, sampleStepMeters);
        float minSpacingSqr = Mathf.Max(0.1f, minSpacingMeters) * Mathf.Max(0.1f, minSpacingMeters);
        Vector3 centerLocal = mapDefinition.originWorldPosition;

        for (float radius = 0f; radius <= searchRadiusMeters && selected.Count < targetCount; radius += step)
        {
            int sampleCount = Mathf.Max(8, Mathf.CeilToInt(Mathf.Max(radius, step) * 12f));
            for (int i = 0; i < sampleCount && selected.Count < targetCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / sampleCount;
                Vector3 local = centerLocal + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector2 pixel = mapDefinition.WorldToMapPixel(local);

                if (!mapDefinition.IsMapPixelWalkable(pixel))
                    continue;

                if (!IsFarEnoughFromSelected(local, selected, minSpacingSqr))
                    continue;

                selected.Add(pixel);
            }
        }

        return selected;
    }

    private bool IsFarEnoughFromSelected(Vector3 candidateLocal, List<Vector2> selectedPixels, float minSpacingSqr)
    {
        for (int i = 0; i < selectedPixels.Count; i++)
        {
            Vector3 selectedLocal = mapDefinition.MapPixelToWorld(selectedPixels[i]);
            Vector3 delta = candidateLocal - selectedLocal;
            delta.y = 0f;
            if (delta.sqrMagnitude < minSpacingSqr)
                return false;
        }

        return true;
    }

    private void AddOrUpdateAttraction(string id, string displayName, Vector2 pixel)
    {
        AttractionDefinition attraction = mapDefinition.FindAttraction(id);
        if (attraction == null)
        {
            attraction = new AttractionDefinition { id = id };
            mapDefinition.attractions.Add(attraction);
        }

        attraction.sourceMapLabel = displayName;
        attraction.displayName = displayName;
        attraction.chineseDisplayName = "测试景点";
        attraction.collectibleSpawnPixel = pixel;
        attraction.arrivalPixel = pixel;
        attraction.hasCustomArrivalPixel = false;
    }

    private void AddOrUpdateNavNode(string nodeId, Vector2 pixel)
    {
        if (mapDefinition.navGraph == null)
            mapDefinition.navGraph = new VenueNavGraphDefinition();
        if (mapDefinition.navGraph.nodes == null)
            mapDefinition.navGraph.nodes = new List<VenueNavNodeDefinition>();

        VenueNavNodeDefinition node = mapDefinition.navGraph.FindNode(nodeId);
        if (node == null)
        {
            node = new VenueNavNodeDefinition { id = nodeId };
            mapDefinition.navGraph.nodes.Add(node);
        }

        node.mapPixel = pixel;
        if (node.neighborNodeIds == null)
            node.neighborNodeIds = new List<string>();
        node.neighborNodeIds.Clear();
    }

    private void ConnectGeneratedNavNodes(int generatedCount)
    {
        if (mapDefinition.navGraph == null || mapDefinition.navGraph.nodes == null)
            return;

        float existingRadiusPixels = connectExistingNodeRadiusMeters / mapDefinition.MetersPerPixel;
        float generatedRadiusPixels = connectGeneratedNodeRadiusMeters / mapDefinition.MetersPerPixel;

        for (int i = 1; i <= generatedCount; i++)
        {
            string nodeId = idPrefix + "_" + i + "_arrival";
            VenueNavNodeDefinition generated = mapDefinition.navGraph.FindNode(nodeId);
            if (generated == null)
                continue;

            ConnectToNearbyNodes(generated, existingRadiusPixels, false);
            ConnectToNearbyNodes(generated, generatedRadiusPixels, true);

            if (generated.neighborNodeIds.Count == 0)
                ConnectToNearestWalkableNode(generated);
        }
    }

    private void ConnectToNearbyNodes(VenueNavNodeDefinition generated, float radiusPixels, bool generatedOnly)
    {
        float radiusSqr = radiusPixels * radiusPixels;
        foreach (VenueNavNodeDefinition candidate in mapDefinition.navGraph.nodes)
        {
            if (candidate == null || candidate == generated)
                continue;
            if (generatedOnly && !candidate.id.StartsWith(idPrefix))
                continue;
            if (!generatedOnly && candidate.id.StartsWith(idPrefix))
                continue;
            if ((candidate.mapPixel - generated.mapPixel).sqrMagnitude > radiusSqr)
                continue;
            if (!mapDefinition.IsMapSegmentWalkable(generated.mapPixel, candidate.mapPixel))
                continue;

            AddNeighborIfMissing(generated, candidate.id);
            AddNeighborIfMissing(candidate, generated.id);
        }
    }

    private void ConnectToNearestWalkableNode(VenueNavNodeDefinition generated)
    {
        VenueNavNodeDefinition best = null;
        float bestDistanceSqr = float.PositiveInfinity;

        foreach (VenueNavNodeDefinition candidate in mapDefinition.navGraph.nodes)
        {
            if (candidate == null || candidate == generated || candidate.id.StartsWith(idPrefix))
                continue;
            if (!mapDefinition.IsMapSegmentWalkable(generated.mapPixel, candidate.mapPixel))
                continue;

            float distanceSqr = (candidate.mapPixel - generated.mapPixel).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                best = candidate;
            }
        }

        if (best == null)
            return;

        AddNeighborIfMissing(generated, best.id);
        AddNeighborIfMissing(best, generated.id);
    }

    private static void AddNeighborIfMissing(VenueNavNodeDefinition node, string neighborNodeId)
    {
        if (node.neighborNodeIds == null)
            node.neighborNodeIds = new List<string>();

        if (!node.neighborNodeIds.Contains(neighborNodeId))
            node.neighborNodeIds.Add(neighborNodeId);
    }
}
