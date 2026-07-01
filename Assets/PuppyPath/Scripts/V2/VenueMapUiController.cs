using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [SerializeField] private PuppyPathV2FlowController flowController;

    [Header("Map Button")]
    [SerializeField] private GameObject mapButtonRoot;

    [Header("Large Map")]
    [SerializeField] private GameObject largeMapPanel;
    [SerializeField] private RectTransform largeMapRect;
    [SerializeField] private RawImage largeMapImage;
    [SerializeField] private RectTransform largeMapMarkerParent;
    [SerializeField] private bool closeLargeMapAfterSelection = true;

    [Header("Marker Prefabs")]
    [SerializeField] private RectTransform attractionMarkerPrefab;
    [SerializeField] private RectTransform userMarkerPrefab;

    [Header("Marker Style")]
    [SerializeField] private Vector2 attractionMarkerSize = new Vector2(34f, 34f);
    [SerializeField] private Vector2 userMarkerSize = new Vector2(22f, 22f);
    [SerializeField] private Color attractionMarkerColor = new Color(1f, 0.64f, 0.08f, 1f);
    [SerializeField] private Color userMarkerColor = new Color(0.16f, 0.72f, 1f, 1f);

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string freeWalkText = "Free roam";
    [SerializeField] private string navigationTextTemplate = "Go to {0}";

    [Header("Update")]
    [SerializeField] private float userMarkerUpdateInterval = 0.05f;
    [SerializeField] private bool hideLargeMapOnStart = true;

    private readonly List<VenueMapMarker> attractionMarkers = new List<VenueMapMarker>();
    private readonly List<MarkerEntry> markerEntries = new List<MarkerEntry>();
    private RectTransform userMarker;
    private float updateTimer;
    private string selectedAttractionId;

    private void Awake()
    {
        ApplyMapTexture();
    }

    private void Start()
    {
        if (hideLargeMapOnStart && largeMapPanel != null)
            largeMapPanel.SetActive(false);

        RebuildMarkers();
        UpdateStatusText();
        UpdateUserMarker();
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < userMarkerUpdateInterval)
            return;

        updateTimer = 0f;
        UpdateUserMarker();
    }

    [ContextMenu("Rebuild Map Markers")]
    public void RebuildMarkers()
    {
        ClearMarkerList(attractionMarkers);
        markerEntries.Clear();
        DestroyMarker(userMarker);
        userMarker = null;

        if (mapDefinition == null || mapDefinition.attractions == null)
            return;

        RectTransform markerParent = GetMarkerParent(largeMapMarkerParent, largeMapRect);
        foreach (AttractionDefinition attraction in mapDefinition.attractions)
            CreateAttractionMarker(attraction, markerParent);

        userMarker = CreateUserMarker(markerParent);

        UpdateSelectedMarkerVisuals();
        UpdateUserMarker();
    }

    public void OpenLargeMap()
    {
        if (largeMapPanel != null)
            largeMapPanel.SetActive(true);

        if (mapButtonRoot != null)
            mapButtonRoot.SetActive(false);

        UpdateUserMarker();
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

        bool navigationStarted = navigationRuntime == null || navigationRuntime.StartNavigationToAttraction(attractionId);

        if (navigationStarted && closeLargeMapAfterSelection)
            CloseLargeMap();

        UpdateStatusText(displayName);

        if (navigationStarted && flowController != null)
            flowController.EnterNavigation();
    }

    public void CancelNavigation()
    {
        selectedAttractionId = null;

        if (navigationRuntime != null)
            navigationRuntime.StopNavigation();

        UpdateSelectedMarkerVisuals();
        UpdateStatusText();

        if (flowController != null)
            flowController.EnterFreeRoam();
    }

    private void ApplyMapTexture()
    {
        if (mapDefinition == null || mapDefinition.mapTexture == null || largeMapImage == null)
            return;

        largeMapImage.texture = mapDefinition.mapTexture;
    }

    private void CreateAttractionMarker(AttractionDefinition attraction, RectTransform parent)
    {
        if (attraction == null || largeMapRect == null || parent == null)
            return;

        RectTransform marker = CreateMarkerRect(
            attractionMarkerPrefab,
            parent,
            "AttractionMarker_" + attraction.id,
            attractionMarkerSize,
            attractionMarkerColor,
            true);

        marker.anchoredPosition = MapPixelToFullMapAnchoredPosition(attraction.GetArrivalPixel(), largeMapRect);

        VenueMapMarker mapMarker = marker.GetComponent<VenueMapMarker>();
        if (mapMarker == null)
            mapMarker = marker.gameObject.AddComponent<VenueMapMarker>();

        mapMarker.Configure(this, attraction, true);
        attractionMarkers.Add(mapMarker);
        markerEntries.Add(new MarkerEntry { attraction = attraction, marker = mapMarker });
    }

    private RectTransform CreateUserMarker(RectTransform parent)
    {
        if (largeMapRect == null || parent == null)
            return null;

        return CreateMarkerRect(
            userMarkerPrefab,
            parent,
            "UserMarker",
            userMarkerSize,
            userMarkerColor,
            false);
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

            Image image = marker.GetComponentInChildren<Image>();
            if (image != null)
                image.color = markerColor;

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

            if (addButton)
                markerObject.AddComponent<Button>();
        }

        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = markerSize;
        marker.localScale = Vector3.one;
        marker.gameObject.SetActive(true);

        return marker;
    }

    private void UpdateUserMarker()
    {
        if (mapDefinition == null || xrCamera == null || userMarker == null || largeMapRect == null)
            return;

        Vector3 venueLocalPosition = venueContentRoot != null
            ? venueContentRoot.InverseTransformPoint(xrCamera.position)
            : xrCamera.position;

        Vector2 userPixel = mapDefinition.WorldToMapPixel(venueLocalPosition);
        userMarker.anchoredPosition = MapPixelToFullMapAnchoredPosition(userPixel, largeMapRect);

        Vector3 forward = venueContentRoot != null
            ? venueContentRoot.InverseTransformDirection(xrCamera.forward)
            : xrCamera.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.0001f)
        {
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            userMarker.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
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

        Rect rect = mapRect.rect;
        return new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, normalizedX),
            Mathf.Lerp(rect.yMin, rect.yMax, normalizedY));
    }

    private void UpdateSelectedMarkerVisuals()
    {
        for (int i = 0; i < attractionMarkers.Count; i++)
        {
            VenueMapMarker marker = attractionMarkers[i];
            if (marker != null)
                marker.SetSelected(!string.IsNullOrEmpty(selectedAttractionId) && marker.AttractionId == selectedAttractionId);
        }
    }

    private void UpdateStatusText(string targetDisplayName = null)
    {
        if (statusText == null)
            return;

        statusText.text = string.IsNullOrEmpty(targetDisplayName)
            ? freeWalkText
            : string.Format(navigationTextTemplate, targetDisplayName);
    }

    private static RectTransform GetMarkerParent(RectTransform configuredParent, RectTransform mapRect)
    {
        return configuredParent != null ? configuredParent : mapRect;
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
}
