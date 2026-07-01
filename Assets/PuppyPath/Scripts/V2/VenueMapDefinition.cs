using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VenueMapDefinition", menuName = "PuppyPath/V2/Venue Map Definition")]
public class VenueMapDefinition : ScriptableObject
{
    [Header("Map Image")]
    [Tooltip("Reference map texture. Current PuppyPath source maps are 2468 x 2160.")]
    public Texture2D mapTexture;
    [Tooltip("Source image pixel size used by calibration data. This should match the image used to read pixel coordinates, not necessarily Unity's imported texture size.")]
    public Vector2 mapPixelSize = new Vector2(2468f, 2160f);
    [Tooltip("Only enable this if you intentionally want to use Unity's imported texture size for calibration.")]
    public bool syncMapPixelSizeFromImportedTexture;

    [Header("Coordinate System")]
    [Tooltip("Pixel coordinate of the selected venue origin. Use top-left image coordinates: +X right, +Y down.")]
    public Vector2 mapOriginPixel;
    [Tooltip("World position used for the venue origin. Usually keep this at zero.")]
    public Vector3 originWorldPosition = Vector3.zero;
    [Tooltip("Confirmed direction: map north maps to Unity +Z. Keep yaw at 0 unless onsite calibration needs an offset.")]
    public float venueYawDegrees;

    [Header("Scale")]
    [Tooltip("First pixel endpoint of the 3.45 m red wall segment.")]
    public Vector2 scalePointAPixel;
    [Tooltip("Second pixel endpoint of the 3.45 m red wall segment.")]
    public Vector2 scalePointBPixel;
    public float scaleSegmentMeters = 3.45f;

    [Header("Attractions")]
    public List<AttractionDefinition> attractions = new List<AttractionDefinition>();

    public float PixelDistanceOfScaleSegment
    {
        get { return Vector2.Distance(scalePointAPixel, scalePointBPixel); }
    }

    public float MetersPerPixel
    {
        get
        {
            float pixelDistance = PixelDistanceOfScaleSegment;
            return pixelDistance > Mathf.Epsilon ? scaleSegmentMeters / pixelDistance : 0f;
        }
    }

    public Vector3 MapPixelToWorld(Vector2 mapPixel)
    {
        Vector2 pixelDelta = mapPixel - mapOriginPixel;
        Vector3 northAlignedMeters = new Vector3(
            pixelDelta.x * MetersPerPixel,
            0f,
            -pixelDelta.y * MetersPerPixel);

        Quaternion yaw = Quaternion.Euler(0f, venueYawDegrees, 0f);
        return originWorldPosition + yaw * northAlignedMeters;
    }

    public Vector2 WorldToMapPixel(Vector3 worldPosition)
    {
        Quaternion inverseYaw = Quaternion.Euler(0f, -venueYawDegrees, 0f);
        Vector3 localMeters = inverseYaw * (worldPosition - originWorldPosition);

        float metersPerPixel = MetersPerPixel;
        if (metersPerPixel <= Mathf.Epsilon)
            return mapOriginPixel;

        return mapOriginPixel + new Vector2(
            localMeters.x / metersPerPixel,
            -localMeters.z / metersPerPixel);
    }

    public bool IsScaleReady()
    {
        return scaleSegmentMeters > 0f && PixelDistanceOfScaleSegment > Mathf.Epsilon;
    }

    [ContextMenu("Populate Default Attractions")]
    public void PopulateDefaultAttractions()
    {
        attractions.Clear();
        attractions.Add(CreateAttraction("chess", "Chess", "Checkmate Corner"));
        attractions.Add(CreateAttraction("couch", "Couch", "Cozy Couch Cove"));
        attractions.Add(CreateAttraction("photo_wall", "Photo Wall", "Snapshot Studio"));
        attractions.Add(CreateAttraction("goodies", "Goodies", "Treat Trove"));
        attractions.Add(CreateAttraction("book_wall", "Book Wall", "Storybook Wall"));
        attractions.Add(CreateAttraction("tap_water", "Tap Water", "Splash Stop"));
        attractions.Add(CreateAttraction("ice_cream_shop", "Ice Cream Shop", "Scoop Station"));
        attractions.Add(CreateAttraction("drink_shop", "Drink Shop", "Fizzy Fridge"));
        attractions.Add(CreateAttraction("piano", "Piano", "Melody Corner"));
        attractions.Add(CreateAttraction("plants", "Plants", "Garden Patch"));
    }

    [ContextMenu("Use Confirmed V2 Orientation")]
    public void UseConfirmedV2Orientation()
    {
        venueYawDegrees = 0f;
        originWorldPosition = Vector3.zero;
    }

    [ContextMenu("Use PuppyPath Source Map Size")]
    public void UsePuppyPathSourceMapSize()
    {
        mapPixelSize = new Vector2(2468f, 2160f);
        syncMapPixelSizeFromImportedTexture = false;
    }

    [ContextMenu("Use Imported Texture Size")]
    public void UseImportedTextureSize()
    {
        if (mapTexture == null)
            return;

        mapPixelSize = new Vector2(mapTexture.width, mapTexture.height);
    }

    [ContextMenu("Log Calibration Summary")]
    public void LogCalibrationSummary()
    {
        Debug.Log(
            $"VenueMapDefinition '{name}' calibration:\n" +
            $"- Map pixel size: {mapPixelSize.x:0} x {mapPixelSize.y:0}\n" +
            $"- Origin pixel: {mapOriginPixel}\n" +
            $"- Origin world: {originWorldPosition}\n" +
            $"- Scale pixel distance: {PixelDistanceOfScaleSegment:0.###}\n" +
            $"- Scale meters: {scaleSegmentMeters:0.###}\n" +
            $"- Meters per pixel: {MetersPerPixel:0.######}\n" +
            $"- Scale world distance: {Vector3.Distance(MapPixelToWorld(scalePointAPixel), MapPixelToWorld(scalePointBPixel)):0.###} m\n" +
            $"- Attraction count: {(attractions == null ? 0 : attractions.Count)}",
            this);
    }

    private void OnValidate()
    {
        if (syncMapPixelSizeFromImportedTexture && mapTexture != null)
            mapPixelSize = new Vector2(mapTexture.width, mapTexture.height);

        if (scaleSegmentMeters < 0f)
            scaleSegmentMeters = 0f;
    }

    private static AttractionDefinition CreateAttraction(string id, string sourceMapLabel, string displayName)
    {
        return new AttractionDefinition
        {
            id = id,
            sourceMapLabel = sourceMapLabel,
            displayName = displayName,
            collectibleSpawnPixel = Vector2.zero,
            arrivalPixel = Vector2.zero,
            hasCustomArrivalPixel = false
        };
    }
}

[Serializable]
public class AttractionDefinition
{
    public string id;
    public string sourceMapLabel;
    public string displayName;
    public string chineseDisplayName;

    [Header("Map Positions")]
    [Tooltip("Pixel position of the collectible spawn point, using top-left image coordinates.")]
    public Vector2 collectibleSpawnPixel;
    [Tooltip("Optional pixel position where the user should arrive. If unset, use the collectible spawn point.")]
    public Vector2 arrivalPixel;
    public bool hasCustomArrivalPixel;

    [Header("Reveal")]
    public float fullAlphaDistanceMeters = 3f;
    public float halfAlphaDistanceMeters = 6f;
    public float hiddenDistanceMeters = 10f;

    public Vector2 GetArrivalPixel()
    {
        return hasCustomArrivalPixel ? arrivalPixel : collectibleSpawnPixel;
    }
}
