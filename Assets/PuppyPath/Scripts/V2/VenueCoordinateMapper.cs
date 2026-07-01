using UnityEngine;

public static class VenueCoordinateMapper
{
    public static Vector3 MapPixelToWorld(
        Vector2 mapPixel,
        Vector2 mapOriginPixel,
        float metersPerPixel,
        Vector3 originWorldPosition,
        float venueYawDegrees = 0f)
    {
        Vector2 pixelDelta = mapPixel - mapOriginPixel;
        Vector3 northAlignedMeters = new Vector3(
            pixelDelta.x * metersPerPixel,
            0f,
            -pixelDelta.y * metersPerPixel);

        return originWorldPosition + Quaternion.Euler(0f, venueYawDegrees, 0f) * northAlignedMeters;
    }

    public static Vector2 WorldToMapPixel(
        Vector3 worldPosition,
        Vector2 mapOriginPixel,
        float metersPerPixel,
        Vector3 originWorldPosition,
        float venueYawDegrees = 0f)
    {
        if (metersPerPixel <= Mathf.Epsilon)
            return mapOriginPixel;

        Vector3 localMeters = Quaternion.Euler(0f, -venueYawDegrees, 0f) * (worldPosition - originWorldPosition);
        return mapOriginPixel + new Vector2(
            localMeters.x / metersPerPixel,
            -localMeters.z / metersPerPixel);
    }
}
