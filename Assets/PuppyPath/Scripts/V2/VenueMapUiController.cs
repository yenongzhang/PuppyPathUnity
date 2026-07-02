using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class VenueMapUiController : MonoBehaviour
{
    private class MarkerEntry
    {
        public AttractionDefinition attraction;
        public VenueMapMarker marker;
    }

    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform xrCamera;
    [SerializeField] private Transform venueContentRoot;
    [SerializeField] private VenueNavigationRuntime navigationRuntime;
    [SerializeField] private NavigationController navigationController;

    [Header("Map Button")]
    [SerializeField] private GameObject mapButtonRoot;

    [Header("Large Map")]
    [SerializeField] private GameObject largeMapPanel;
    [SerializeField] private RectTransform largeMapRect;
    [FormerlySerializedAs("largeMapSpriteImage")]
    [SerializeField] private Image largeMapImage;
    [SerializeField] private RectTransform largeMapMarkerParent;
    [SerializeField] private bool useMapDefinitionTextureForDisplay;
    [SerializeField] private bool forceOverlayParentsUnderMapImage = true;
    [SerializeField] private bool closeLargeMapAfterSelection = true;

    [Header("Displayed Map Content Bounds")]
    [Tooltip("Normalized left edge of the real map inside the commercial image. 0 = image left edge.")]
    [SerializeField, Range(0f, 1f)] private float mapContentLeft = 0f;
    [Tooltip("Normalized right edge of the real map inside the commercial image. 1 = image right edge.")]
    [SerializeField, Range(0f, 1f)] private float mapContentRight = 1f;
    [Tooltip("Normalized bottom edge of the real map inside the commercial image. 0 = image bottom edge.")]
    [SerializeField, Range(0f, 1f)] private float mapContentBottom = 0f;
    [Tooltip("Normalized top edge of the real map inside the commercial image. 1 = image top edge.")]
    [SerializeField, Range(0f, 1f)] private float mapContentTop = 1f;

    [Header("Marker Prefabs")]
    [SerializeField] private RectTransform attractionMarkerPrefab;

    [Header("Marker Style")]
    [SerializeField] private Vector2 attractionMarkerSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 minimumAttractionMarkerSize = new Vector2(72f, 72f);
    [SerializeField] private Color attractionMarkerColor = new Color(1f, 0.64f, 0.08f, 1f);
    [SerializeField] private Color selectedAttractionMarkerColor = new Color(0.3f, 0.85f, 1f, 1f);
    [SerializeField] private Color visitedAttractionMarkerColor = new Color(0.42f, 0.9f, 0.52f, 1f);

    [Header("Map Roads")]
    [SerializeField] private RectTransform roadLineParent;
    [SerializeField] private RectTransform routeLineParent;
    [SerializeField] private Color roadLineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color selectedRouteColor = new Color(0.22f, 0.78f, 1f, 0.95f);
    [SerializeField] private float roadLineWidth = 3f;
    [SerializeField] private float selectedRouteLineWidth = 7f;
    [SerializeField] private bool drawNavGraphRoads = true;
    [SerializeField] private bool drawSelectedRouteOnMap;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string freeWalkText = "Free roam";
    [SerializeField] private string navigationTextTemplate = "Go to {0}";

    [Header("Startup")]
    [SerializeField] private bool hideLargeMapOnStart = true;

    private readonly List<VenueMapMarker> attractionMarkers = new List<VenueMapMarker>();
    private readonly List<MarkerEntry> markerEntries = new List<MarkerEntry>();
    private readonly List<RectTransform> roadLines = new List<RectTransform>();
    private readonly List<RectTransform> selectedRouteLines = new List<RectTransform>();
    private readonly HashSet<string> visitedAttractionIds = new HashSet<string>();
    private string selectedAttractionId;
    private Sprite runtimeMapSprite;

    private void OnValidate()
    {
        ClampMapContentBounds();
    }

    private void Awake()
    {
        ResolveMapImageReferences();
        ResolveFlowReferences();
        ApplyMapTexture();
    }

    private void Start()
    {
        if (hideLargeMapOnStart && largeMapPanel != null)
            largeMapPanel.SetActive(false);

        PrepareMapImageForMarkerRaycasts();
        RebuildRoadLines();
        RebuildMarkers();
        UpdateStatusText();
    }

    [ContextMenu("Rebuild Map Visuals")]
    public void RebuildMapVisuals()
    {
        ClampMapContentBounds();
        RebuildRoadLines();
        RebuildMarkers();

        if (!string.IsNullOrEmpty(selectedAttractionId) && drawSelectedRouteOnMap)
        {
            AttractionDefinition attraction = mapDefinition != null
                ? mapDefinition.FindAttraction(selectedAttractionId)
                : null;

            if (attraction != null)
                ShowSelectedRouteToMapPixel(attraction.GetArrivalPixel());
        }
    }

    [ContextMenu("Reset Map Content Bounds")]
    private void ResetMapContentBounds()
    {
        mapContentLeft = 0f;
        mapContentRight = 1f;
        mapContentBottom = 0f;
        mapContentTop = 1f;
        RebuildMapVisuals();
    }

    [ContextMenu("Rebuild Map Markers")]
    public void RebuildMarkers()
    {
        ClearMarkerList(attractionMarkers);
        markerEntries.Clear();

        if (mapDefinition == null || mapDefinition.attractions == null)
            return;

        RectTransform markerParent = GetOverlayParent(largeMapMarkerParent, "Markers");
        foreach (AttractionDefinition attraction in mapDefinition.attractions)
            CreateAttractionMarker(attraction, markerParent);

        UpdateSelectedMarkerVisuals();
    }

    [ContextMenu("Rebuild Map Roads")]
    public void RebuildRoadLines()
    {
        ClearRectList(roadLines);

        if (!drawNavGraphRoads || mapDefinition == null || mapDefinition.navGraph == null || mapDefinition.navGraph.nodes == null)
            return;

        RectTransform mapRect = GetMapRect();
        if (mapRect == null)
            return;

        RectTransform parent = GetOverlayParent(roadLineParent, "RoadLines");
        if (parent == null)
            return;

        HashSet<string> drawnEdges = new HashSet<string>();
        foreach (VenueNavNodeDefinition node in mapDefinition.navGraph.nodes)
        {
            if (node == null || node.neighborNodeIds == null)
                continue;

            foreach (string neighborId in node.neighborNodeIds)
            {
                VenueNavNodeDefinition neighbor = mapDefinition.navGraph.FindNode(neighborId);
                if (neighbor == null)
                    continue;

                string edgeKey = string.CompareOrdinal(node.id, neighbor.id) <= 0
                    ? node.id + "|" + neighbor.id
                    : neighbor.id + "|" + node.id;

                if (drawnEdges.Contains(edgeKey))
                    continue;

                drawnEdges.Add(edgeKey);
                Vector2 start = MapPixelToFullMapAnchoredPosition(node.mapPixel, mapRect);
                Vector2 end = MapPixelToFullMapAnchoredPosition(neighbor.mapPixel, mapRect);
                roadLines.Add(CreateLineSegment(parent, "RoadLine_" + edgeKey, start, end, roadLineWidth, roadLineColor));
            }
        }
    }

    public void OpenLargeMap()
    {
        if (largeMapPanel != null)
            largeMapPanel.SetActive(true);

        if (mapButtonRoot != null)
            mapButtonRoot.SetActive(false);
    }

    public void CloseLargeMap()
    {
        if (largeMapPanel != null)
            largeMapPanel.SetActive(false);

        if (mapButtonRoot != null)
            mapButtonRoot.SetActive(true);
    }

    public void ToggleLargeMap()
    {
        if (largeMapPanel == null)
            return;

        if (largeMapPanel.activeSelf)
            CloseLargeMap();
        else
            OpenLargeMap();
    }

    public void SelectAttraction(string attractionId)
    {
        selectedAttractionId = attractionId;
        UpdateSelectedMarkerVisuals();

        AttractionDefinition attraction = mapDefinition != null ? mapDefinition.FindAttraction(attractionId) : null;
        string displayName = attraction != null && !string.IsNullOrWhiteSpace(attraction.displayName)
            ? attraction.displayName
            : attractionId;

        bool navigationStarted = navigationController != null
            ? navigationController.StartVenueNavigationToAttraction(attractionId, displayName)
            : navigationRuntime == null || navigationRuntime.StartNavigationToAttraction(attractionId);

        if (navigationStarted && attraction != null && drawSelectedRouteOnMap)
            ShowSelectedRouteToMapPixel(attraction.GetArrivalPixel());
        else
            ClearSelectedRoute();

        if (navigationStarted && closeLargeMapAfterSelection)
            CloseLargeMap();

        UpdateStatusText(displayName);

        if (navigationStarted && navigationController == null && navigationRuntime == null)
            Debug.LogWarning("VenueMapUiController: no NavigationController or VenueNavigationRuntime assigned.");
    }

    public void CancelNavigation()
    {
        selectedAttractionId = null;

        if (navigationRuntime != null)
            navigationRuntime.StopNavigation();

        ClearSelectedRoute();
        UpdateSelectedMarkerVisuals();
        UpdateStatusText();

        if (navigationController != null)
            navigationController.DismissIntroAndStartWalking();
    }

    public void MarkAttractionVisited(string attractionId)
    {
        if (string.IsNullOrWhiteSpace(attractionId))
            return;

        visitedAttractionIds.Add(attractionId);

        if (selectedAttractionId == attractionId)
            selectedAttractionId = null;

        ClearSelectedRoute();
        UpdateSelectedMarkerVisuals();
    }

    private void ApplyMapTexture()
    {
        if (!useMapDefinitionTextureForDisplay)
            return;

        if (mapDefinition == null || mapDefinition.mapTexture == null)
            return;

        if (largeMapImage != null)
            largeMapImage.sprite = GetOrCreateRuntimeMapSprite();
    }

    private void ResolveMapImageReferences()
    {
        if (largeMapImage == null && largeMapRect != null)
            largeMapImage = largeMapRect.GetComponent<Image>();

        if (largeMapImage != null)
            largeMapRect = largeMapImage.rectTransform;

        if (largeMapRect == null)
            return;
    }

    private void PrepareMapImageForMarkerRaycasts()
    {
        if (largeMapImage != null)
            largeMapImage.raycastTarget = false;
    }

    private void ResolveFlowReferences()
    {
        if (navigationController == null)
            navigationController = FindFirstObjectByType<NavigationController>();

        if (navigationRuntime == null)
            navigationRuntime = FindFirstObjectByType<VenueNavigationRuntime>();
    }

    private Sprite GetOrCreateRuntimeMapSprite()
    {
        if (runtimeMapSprite != null)
            return runtimeMapSprite;

        if (mapDefinition == null || mapDefinition.mapTexture == null)
            return null;

        runtimeMapSprite = Sprite.Create(
            mapDefinition.mapTexture,
            new Rect(0f, 0f, mapDefinition.mapTexture.width, mapDefinition.mapTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        runtimeMapSprite.name = mapDefinition.mapTexture.name + "_RuntimeSprite";
        return runtimeMapSprite;
    }

    private void CreateAttractionMarker(AttractionDefinition attraction, RectTransform parent)
    {
        RectTransform mapRect = GetMapRect();
        if (attraction == null || mapRect == null || parent == null)
            return;

        RectTransform marker = CreateMarkerRect(
            attractionMarkerPrefab,
            parent,
            "AttractionMarker_" + attraction.id,
            GetEffectiveAttractionMarkerSize(),
            attractionMarkerColor,
            true);

        marker.anchoredPosition = MapPixelToFullMapAnchoredPosition(attraction.GetArrivalPixel(), mapRect);

        VenueMapMarker mapMarker = marker.GetComponent<VenueMapMarker>();
        if (mapMarker == null)
            mapMarker = marker.gameObject.AddComponent<VenueMapMarker>();

        mapMarker.Configure(
            this,
            attraction,
            true,
            attractionMarkerColor,
            selectedAttractionMarkerColor,
            visitedAttractionMarkerColor);
        attractionMarkers.Add(mapMarker);
        markerEntries.Add(new MarkerEntry { attraction = attraction, marker = mapMarker });
    }

    private RectTransform CreateMarkerRect(
        RectTransform prefab,
        RectTransform parent,
        string markerName,
        Vector2 markerSize,
        Color markerColor,
        bool addButton)
    {
        RectTransform marker;
        if (prefab != null)
        {
            marker = Instantiate(prefab, parent);
            marker.name = markerName;

            PrepareMarkerGraphics(marker, markerColor);

            if (addButton && marker.GetComponent<Button>() == null)
                marker.gameObject.AddComponent<Button>();
        }
        else
        {
            GameObject markerObject = new GameObject(markerName, typeof(RectTransform), typeof(Image));
            marker = markerObject.GetComponent<RectTransform>();
            marker.SetParent(parent, false);

            Image image = markerObject.GetComponent<Image>();
            image.color = markerColor;
            image.raycastTarget = true;

            if (addButton)
                markerObject.AddComponent<Button>();
        }

        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = markerSize;
        marker.localScale = Vector3.one;
        marker.gameObject.SetActive(true);

        if (addButton)
        {
            Button button = marker.GetComponent<Button>();
            if (button != null)
                button.targetGraphic = marker.GetComponentInChildren<Image>();
        }

        return marker;
    }

    private static Image PrepareMarkerGraphics(RectTransform marker, Color markerColor)
    {
        if (marker == null)
            return null;

        Graphic[] graphics = marker.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        Image image = marker.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.color = markerColor;
            image.raycastTarget = true;
        }

        return image;
    }

    private Vector2 GetEffectiveAttractionMarkerSize()
    {
        return new Vector2(
            Mathf.Max(attractionMarkerSize.x, minimumAttractionMarkerSize.x),
            Mathf.Max(attractionMarkerSize.y, minimumAttractionMarkerSize.y));
    }

    private void ShowSelectedRouteToMapPixel(Vector2 destinationPixel)
    {
        ClearSelectedRoute();

        RectTransform mapRect = GetMapRect();
        if (mapDefinition == null || xrCamera == null || mapRect == null)
            return;

        Vector3 venueLocalPosition = venueContentRoot != null
            ? venueContentRoot.InverseTransformPoint(xrCamera.position)
            : xrCamera.position;

        Vector2 startPixel = mapDefinition.WorldToMapPixel(venueLocalPosition);
        if (!mapDefinition.IsMapPixelWalkable(startPixel))
            TryFindNearestWalkableNavNodePixel(startPixel, out startPixel);
        if (!mapDefinition.IsMapPixelWalkable(destinationPixel))
            TryFindNearestWalkableNavNodePixel(destinationPixel, out destinationPixel);

        if (!VenuePathfinder.TryFindMapPath(mapDefinition, startPixel, destinationPixel, out List<Vector2> mapPath))
            return;

        RectTransform parent = GetOverlayParent(routeLineParent, "RouteLines");
        if (parent == null)
            return;

        for (int i = 0; i < mapPath.Count - 1; i++)
        {
            Vector2 start = MapPixelToFullMapAnchoredPosition(mapPath[i], mapRect);
            Vector2 end = MapPixelToFullMapAnchoredPosition(mapPath[i + 1], mapRect);
            selectedRouteLines.Add(CreateLineSegment(parent, "SelectedRouteLine_" + i, start, end, selectedRouteLineWidth, selectedRouteColor));
        }
    }

    private void ClearSelectedRoute()
    {
        ClearRectList(selectedRouteLines);
    }

    private bool TryFindNearestWalkableNavNodePixel(Vector2 fromPixel, out Vector2 navNodePixel)
    {
        navNodePixel = fromPixel;

        if (mapDefinition == null || mapDefinition.navGraph == null || mapDefinition.navGraph.nodes == null)
            return false;

        float bestDistanceSqr = float.PositiveInfinity;
        bool found = false;

        foreach (VenueNavNodeDefinition node in mapDefinition.navGraph.nodes)
        {
            if (node == null || !mapDefinition.IsMapPixelWalkable(node.mapPixel))
                continue;

            float distanceSqr = (node.mapPixel - fromPixel).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                navNodePixel = node.mapPixel;
                found = true;
            }
        }

        return found;
    }

    private Vector2 MapPixelToFullMapAnchoredPosition(Vector2 mapPixel, RectTransform mapRect)
    {
        if (mapDefinition == null || mapRect == null)
            return Vector2.zero;

        Vector2 size = mapDefinition.mapPixelSize;
        if (size.x <= Mathf.Epsilon || size.y <= Mathf.Epsilon)
            return Vector2.zero;

        float normalizedX = Mathf.Clamp01(mapPixel.x / size.x);
        float normalizedY = Mathf.Clamp01(1f - mapPixel.y / size.y);

        Rect contentRect = GetMapContentRect(mapRect.rect);
        return new Vector2(
            Mathf.Lerp(contentRect.xMin, contentRect.xMax, normalizedX),
            Mathf.Lerp(contentRect.yMin, contentRect.yMax, normalizedY));
    }

    private Rect GetMapContentRect(Rect fullRect)
    {
        ClampMapContentBounds();

        float xMin = Mathf.Lerp(fullRect.xMin, fullRect.xMax, mapContentLeft);
        float xMax = Mathf.Lerp(fullRect.xMin, fullRect.xMax, mapContentRight);
        float yMin = Mathf.Lerp(fullRect.yMin, fullRect.yMax, mapContentBottom);
        float yMax = Mathf.Lerp(fullRect.yMin, fullRect.yMax, mapContentTop);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void ClampMapContentBounds()
    {
        const float minimumSpan = 0.001f;

        mapContentLeft = Mathf.Clamp01(mapContentLeft);
        mapContentRight = Mathf.Clamp01(mapContentRight);
        mapContentBottom = Mathf.Clamp01(mapContentBottom);
        mapContentTop = Mathf.Clamp01(mapContentTop);

        if (mapContentRight <= mapContentLeft + minimumSpan)
            mapContentRight = Mathf.Min(1f, mapContentLeft + minimumSpan);

        if (mapContentTop <= mapContentBottom + minimumSpan)
            mapContentTop = Mathf.Min(1f, mapContentBottom + minimumSpan);
    }

    private RectTransform GetMapRect()
    {
        if (largeMapImage != null)
            return largeMapImage.rectTransform;

        return largeMapRect;
    }

    private RectTransform GetOverlayParent(RectTransform configuredParent, string objectName)
    {
        RectTransform mapRect = GetMapRect();
        if (!forceOverlayParentsUnderMapImage && configuredParent != null && IsChildOf(configuredParent, mapRect))
        {
            DisableOverlayRaycasts(configuredParent);
            return configuredParent;
        }

        RectTransform overlay = GetOrCreateOverlayParent(objectName);
        if (objectName == "RoadLines")
            roadLineParent = overlay;
        else if (objectName == "RouteLines")
            routeLineParent = overlay;
        else if (objectName == "Markers")
            largeMapMarkerParent = overlay;

        SortOverlayParents();
        return overlay;
    }

    private RectTransform GetOrCreateOverlayParent(string objectName)
    {
        RectTransform mapRect = GetMapRect();
        if (mapRect == null)
            return null;

        Transform existing = mapRect.Find(objectName);
        if (existing is RectTransform existingRect)
        {
            DisableOverlayRaycasts(existingRect);
            return existingRect;
        }

        GameObject overlayObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform overlay = overlayObject.GetComponent<RectTransform>();
        overlay.SetParent(mapRect, false);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.pivot = new Vector2(0.5f, 0.5f);
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.localScale = Vector3.one;
        overlay.localRotation = Quaternion.identity;
        DisableOverlayRaycasts(overlay);
        return overlay;
    }

    private static void DisableOverlayRaycasts(RectTransform overlay)
    {
        if (overlay == null)
            return;

        Graphic[] graphics = overlay.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void SortOverlayParents()
    {
        if (roadLineParent != null)
            roadLineParent.SetAsFirstSibling();

        if (routeLineParent != null)
            routeLineParent.SetSiblingIndex(roadLineParent != null ? roadLineParent.GetSiblingIndex() + 1 : 0);

        if (largeMapMarkerParent != null)
            largeMapMarkerParent.SetAsLastSibling();
    }

    private void UpdateSelectedMarkerVisuals()
    {
        for (int i = 0; i < attractionMarkers.Count; i++)
        {
            VenueMapMarker marker = attractionMarkers[i];
            if (marker != null)
                marker.SetState(
                    !string.IsNullOrEmpty(selectedAttractionId) && marker.AttractionId == selectedAttractionId,
                    visitedAttractionIds.Contains(marker.AttractionId));
        }
    }

    private RectTransform CreateLineSegment(
        RectTransform parent,
        string lineName,
        Vector2 start,
        Vector2 end,
        float width,
        Color color)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        RectTransform line = lineObject.GetComponent<RectTransform>();
        line.SetParent(parent, false);

        Image image = lineObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        Vector2 delta = end - start;
        line.anchorMin = new Vector2(0.5f, 0.5f);
        line.anchorMax = new Vector2(0.5f, 0.5f);
        line.pivot = new Vector2(0f, 0.5f);
        line.anchoredPosition = start;
        line.sizeDelta = new Vector2(delta.magnitude, width);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        line.localScale = Vector3.one;
        line.SetAsFirstSibling();
        return line;
    }

    private void UpdateStatusText(string targetDisplayName = null)
    {
        if (statusText == null)
            return;

        statusText.text = string.IsNullOrEmpty(targetDisplayName)
            ? freeWalkText
            : string.Format(navigationTextTemplate, targetDisplayName);
    }

    private static bool IsChildOf(Transform candidate, Transform parent)
    {
        if (candidate == null || parent == null)
            return false;

        Transform current = candidate;
        while (current != null)
        {
            if (current == parent)
                return true;

            current = current.parent;
        }

        return false;
    }

    private static void ClearMarkerList(List<VenueMapMarker> markers)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            VenueMapMarker marker = markers[i];
            if (marker == null)
                continue;

            if (Application.isPlaying)
                Destroy(marker.gameObject);
            else
                DestroyImmediate(marker.gameObject);
        }

        markers.Clear();
    }

    private static void DestroyMarker(RectTransform marker)
    {
        if (marker == null)
            return;

        if (Application.isPlaying)
            Destroy(marker.gameObject);
        else
            DestroyImmediate(marker.gameObject);
    }

    private static void ClearRectList(List<RectTransform> rects)
    {
        for (int i = rects.Count - 1; i >= 0; i--)
        {
            RectTransform rect = rects[i];
            if (rect == null)
                continue;

            if (Application.isPlaying)
                Destroy(rect.gameObject);
            else
                DestroyImmediate(rect.gameObject);
        }

        rects.Clear();
    }
}
