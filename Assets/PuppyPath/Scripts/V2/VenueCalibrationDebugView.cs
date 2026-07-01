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
    [SerializeField] private bool drawAttractions = true;
    [SerializeField] private bool drawLabels = true;
    [SerializeField] private float markerRadius = 0.12f;
    [SerializeField] private float mapBoundsHeight = 0.01f;
    [SerializeField] private Color originColor = Color.red;
    [SerializeField] private Color scaleColor = Color.red;
    [SerializeField] private Color boundsColor = new Color(0.8f, 0.8f, 0.8f, 0.45f);
    [SerializeField] private Color attractionColor = new Color(1f, 0.85f, 0.1f, 1f);

    public VenueMapDefinition MapDefinition
    {
        get { return mapDefinition; }
        set { mapDefinition = value; }
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

        if (drawAttractions)
            DrawAttractionMarkers();
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

    private void DrawSceneLabel(Vector3 position, string label)
    {
        if (!drawLabels || string.IsNullOrEmpty(label))
            return;

#if UNITY_EDITOR
        Handles.Label(position, label);
#endif
    }
}
