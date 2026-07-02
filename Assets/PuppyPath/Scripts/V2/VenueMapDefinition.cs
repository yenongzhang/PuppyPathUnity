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

    [Header("Walkable Areas")]
    public List<WalkableAreaDefinition> walkableAreas = new List<WalkableAreaDefinition>();

    [Header("Obstacle Areas")]
    public List<ObstacleAreaDefinition> obstacleAreas = new List<ObstacleAreaDefinition>();

    [Header("Navigation Graph")]
    public VenueNavGraphDefinition navGraph = new VenueNavGraphDefinition();

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

    public void SetMapOriginPixelPreservingWorldLayout(Vector2 newMapOriginPixel)
    {
        float metersPerPixel = MetersPerPixel;
        Quaternion yaw = Quaternion.Euler(0f, venueYawDegrees, 0f);
        Vector2 deltaPixel = newMapOriginPixel - mapOriginPixel;

        originWorldPosition += yaw * new Vector3(
            deltaPixel.x * metersPerPixel,
            0f,
            -deltaPixel.y * metersPerPixel);

        mapOriginPixel = newMapOriginPixel;
    }

    public void SetScalePointAPixelPreservingMetersPerPixel(Vector2 newScalePointPixel)
    {
        SetScalePointPreservingMetersPerPixel(newScalePointPixel, scalePointBPixel, true);
    }

    public void SetScalePointBPixelPreservingMetersPerPixel(Vector2 newScalePointPixel)
    {
        SetScalePointPreservingMetersPerPixel(scalePointAPixel, newScalePointPixel, false);
    }

    public bool IsMapPixelWalkable(Vector2 mapPixel)
    {
        bool insideWalkableArea = walkableAreas == null || walkableAreas.Count == 0;

        if (walkableAreas != null)
        {
            foreach (WalkableAreaDefinition area in walkableAreas)
            {
                if (area != null && area.ContainsMapPixel(mapPixel))
                {
                    insideWalkableArea = true;
                    break;
                }
            }
        }

        if (!insideWalkableArea)
            return false;

        if (obstacleAreas != null)
        {
            foreach (ObstacleAreaDefinition obstacle in obstacleAreas)
            {
                if (obstacle != null && obstacle.ContainsMapPixel(mapPixel))
                    return false;
            }
        }

        return true;
    }

    public bool IsWorldPositionWalkable(Vector3 worldPosition)
    {
        return IsMapPixelWalkable(WorldToMapPixel(worldPosition));
    }

    public bool IsMapSegmentWalkable(Vector2 startPixel, Vector2 endPixel, int sampleCount = 16)
    {
        if (sampleCount < 1)
            sampleCount = 1;

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            if (!IsMapPixelWalkable(Vector2.Lerp(startPixel, endPixel, t)))
                return false;
        }

        return true;
    }

    public AttractionDefinition FindAttraction(string attractionId)
    {
        if (string.IsNullOrEmpty(attractionId) || attractions == null)
            return null;

        foreach (AttractionDefinition attraction in attractions)
        {
            if (attraction != null && attraction.id == attractionId)
                return attraction;
        }

        return null;
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

    [ContextMenu("Populate Detected Draft Map Data")]
    public void PopulateDetectedDraftMapData()
    {
        UsePuppyPathSourceMapSize();
        PopulateDetectedAttractionSpawnPixels();
        PopulateDetectedWalkableDraft();
        PopulateDetectedNavGraphDraft();
    }

    [ContextMenu("Populate Detected Attraction Spawn Pixels")]
    public void PopulateDetectedAttractionSpawnPixels()
    {
        if (attractions == null || attractions.Count == 0)
            PopulateDefaultAttractions();

        SetAttractionSpawn("chess", new Vector2(262f, 470f));
        SetAttractionSpawn("couch", new Vector2(198f, 663f));
        SetAttractionSpawn("photo_wall", new Vector2(624f, 859f));
        SetAttractionSpawn("book_wall", new Vector2(1710f, 857f));
        SetAttractionSpawn("goodies", new Vector2(623f, 1388f));
        SetAttractionSpawn("piano", new Vector2(2181f, 1642f));
        SetAttractionSpawn("tap_water", new Vector2(1400f, 1752f));
        SetAttractionSpawn("ice_cream_shop", new Vector2(1660f, 1784f));
        SetAttractionSpawn("drink_shop", new Vector2(1930f, 1804f));
        SetAttractionSpawn("plants", new Vector2(2339f, 1956f));
    }

    [ContextMenu("Populate Detected Walkable Draft")]
    public void PopulateDetectedWalkableDraft()
    {
        walkableAreas.Clear();
        obstacleAreas.Clear();

        walkableAreas.Add(new WalkableAreaDefinition
        {
            id = "main_walkable_auto_draft",
            displayName = "Main Walkable Auto Draft",
            polygonPixels = new List<Vector2>
            {
                new Vector2(88f, 136f),
                new Vector2(632f, 136f),
                new Vector2(640f, 504f),
                new Vector2(832f, 504f),
                new Vector2(840f, 632f),
                new Vector2(1616f, 632f),
                new Vector2(1624f, 584f),
                new Vector2(1840f, 632f),
                new Vector2(1848f, 512f),
                new Vector2(1968f, 512f),
                new Vector2(1976f, 816f),
                new Vector2(2304f, 816f),
                new Vector2(2304f, 1072f),
                new Vector2(2088f, 1072f),
                new Vector2(2088f, 1608f),
                new Vector2(2368f, 1616f),
                new Vector2(2440f, 2096f),
                new Vector2(1608f, 2096f),
                new Vector2(1608f, 1840f),
                new Vector2(1728f, 1840f),
                new Vector2(1632f, 1824f),
                new Vector2(1632f, 1696f),
                new Vector2(1832f, 1664f),
                new Vector2(1440f, 1664f),
                new Vector2(1432f, 1792f),
                new Vector2(1368f, 1792f),
                new Vector2(1360f, 1664f),
                new Vector2(824f, 1664f),
                new Vector2(824f, 1808f),
                new Vector2(416f, 1792f),
                new Vector2(416f, 720f),
                new Vector2(80f, 712f),
            }
        });

        obstacleAreas.Add(new ObstacleAreaDefinition
        {
            id = "central_block_auto_draft",
            displayName = "Central Block Auto Draft",
            polygonPixels = new List<Vector2>
            {
                new Vector2(1560f, 696f),
                new Vector2(1544f, 832f),
                new Vector2(1328f, 832f),
                new Vector2(1328f, 704f),
                new Vector2(1000f, 704f),
                new Vector2(1000f, 832f),
                new Vector2(880f, 832f),
                new Vector2(888f, 704f),
                new Vector2(536f, 704f),
                new Vector2(536f, 816f),
                new Vector2(720f, 816f),
                new Vector2(728f, 896f),
                new Vector2(864f, 896f),
                new Vector2(864f, 1328f),
                new Vector2(664f, 1328f),
                new Vector2(696f, 1456f),
                new Vector2(544f, 1456f),
                new Vector2(536f, 1376f),
                new Vector2(528f, 1576f),
                new Vector2(1872f, 1584f),
                new Vector2(1872f, 1112f),
                new Vector2(1560f, 1104f),
                new Vector2(1560f, 816f),
                new Vector2(1872f, 824f),
                new Vector2(1880f, 704f),
            }
        });
    }

    [ContextMenu("Populate Detected Nav Graph Draft")]
    public void PopulateDetectedNavGraphDraft()
    {
        navGraph.nodes.Clear();

        AddNavNode("chess_arrival", new Vector2(262f, 470f));
        AddNavNode("chess_lane", new Vector2(300f, 560f));
        AddNavNode("couch_arrival", new Vector2(198f, 663f));
        AddNavNode("left_top_mid", new Vector2(270f, 560f));
        AddNavNode("left_top_lower", new Vector2(300f, 675f));
        AddNavNode("left_entry", new Vector2(420f, 690f));
        AddNavNode("photo_wall_arrival", new Vector2(624f, 859f));
        AddNavNode("photo_wall_lane", new Vector2(560f, 775f));
        AddNavNode("left_mid_01", new Vector2(420f, 900f));
        AddNavNode("left_mid_01b", new Vector2(420f, 1030f));
        AddNavNode("left_mid_02", new Vector2(420f, 1120f));
        AddNavNode("left_goodies_entry", new Vector2(520f, 1285f));
        AddNavNode("goodies_arrival", new Vector2(623f, 1388f));
        AddNavNode("left_lower", new Vector2(540f, 1500f));
        AddNavNode("bottom_left_entry", new Vector2(610f, 1580f));
        AddNavNode("bottom_left", new Vector2(760f, 1590f));
        AddNavNode("bottom_left_02", new Vector2(900f, 1595f));
        AddNavNode("bottom_mid_01", new Vector2(1040f, 1590f));
        AddNavNode("bottom_mid_center", new Vector2(1170f, 1595f));
        AddNavNode("bottom_mid_02", new Vector2(1300f, 1600f));
        AddNavNode("tap_water_arrival", new Vector2(1400f, 1752f));
        AddNavNode("tap_water_lane", new Vector2(1475f, 1690f));
        AddNavNode("ice_cream_arrival", new Vector2(1660f, 1784f));
        AddNavNode("ice_cream_lane", new Vector2(1710f, 1720f));
        AddNavNode("drink_shop_arrival", new Vector2(1930f, 1804f));
        AddNavNode("drink_shop_lane", new Vector2(1940f, 1735f));
        AddNavNode("right_bottom_01", new Vector2(2160f, 1810f));
        AddNavNode("right_bottom_02", new Vector2(2280f, 1830f));
        AddNavNode("plants_arrival", new Vector2(2339f, 1956f));
        AddNavNode("piano_arrival", new Vector2(2181f, 1642f));
        AddNavNode("piano_lane", new Vector2(2100f, 1565f));
        AddNavNode("right_mid_low", new Vector2(2020f, 1450f));
        AddNavNode("right_mid_02", new Vector2(1990f, 1280f));
        AddNavNode("right_mid", new Vector2(1950f, 1120f));
        AddNavNode("right_upper", new Vector2(1860f, 940f));
        AddNavNode("book_wall_arrival", new Vector2(1710f, 857f));
        AddNavNode("book_wall_lane", new Vector2(1620f, 810f));
        AddNavNode("top_01", new Vector2(780f, 700f));
        AddNavNode("top_01b", new Vector2(900f, 685f));
        AddNavNode("top_02", new Vector2(1000f, 680f));
        AddNavNode("top_02b", new Vector2(1130f, 680f));
        AddNavNode("top_03", new Vector2(1250f, 680f));
        AddNavNode("top_03b", new Vector2(1380f, 680f));
        AddNavNode("top_04", new Vector2(1500f, 690f));

        ConnectNearbyWalkableNavNodes(265f);
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
            $"- Attraction count: {(attractions == null ? 0 : attractions.Count)}\n" +
            $"- Walkable area count: {(walkableAreas == null ? 0 : walkableAreas.Count)}\n" +
            $"- Nav node count: {(navGraph == null || navGraph.nodes == null ? 0 : navGraph.nodes.Count)}",
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

    private void SetAttractionSpawn(string attractionId, Vector2 spawnPixel)
    {
        AttractionDefinition attraction = FindAttraction(attractionId);
        if (attraction == null)
            return;

        attraction.collectibleSpawnPixel = spawnPixel;
        attraction.arrivalPixel = spawnPixel;
    }

    private void AddNavNode(string id, Vector2 mapPixel, params string[] neighborNodeIds)
    {
        VenueNavNodeDefinition node = new VenueNavNodeDefinition
        {
            id = id,
            mapPixel = mapPixel,
            neighborNodeIds = new List<string>(neighborNodeIds)
        };

        navGraph.nodes.Add(node);
    }

    private void SetScalePointPreservingMetersPerPixel(Vector2 newPointA, Vector2 newPointB, bool movedPointA)
    {
        float metersPerPixel = MetersPerPixel;

        if (movedPointA)
            scalePointAPixel = newPointA;
        else
            scalePointBPixel = newPointB;

        float newPixelDistance = PixelDistanceOfScaleSegment;
        if (metersPerPixel > Mathf.Epsilon && newPixelDistance > Mathf.Epsilon)
            scaleSegmentMeters = metersPerPixel * newPixelDistance;
    }

    private void ConnectNavNodesIfWalkable(string firstNodeId, string secondNodeId)
    {
        VenueNavNodeDefinition first = navGraph.FindNode(firstNodeId);
        VenueNavNodeDefinition second = navGraph.FindNode(secondNodeId);
        if (first == null || second == null)
            return;

        if (!IsMapSegmentWalkable(first.mapPixel, second.mapPixel))
            return;

        AddNeighborIfMissing(first, secondNodeId);
        AddNeighborIfMissing(second, firstNodeId);
    }

    private void ConnectNearbyWalkableNavNodes(float maxDistancePixels)
    {
        if (navGraph == null || navGraph.nodes == null)
            return;

        float maxDistanceSqr = maxDistancePixels * maxDistancePixels;

        for (int i = 0; i < navGraph.nodes.Count; i++)
        {
            VenueNavNodeDefinition a = navGraph.nodes[i];
            if (a == null || !IsMapPixelWalkable(a.mapPixel))
                continue;

            for (int j = i + 1; j < navGraph.nodes.Count; j++)
            {
                VenueNavNodeDefinition b = navGraph.nodes[j];
                if (b == null || !IsMapPixelWalkable(b.mapPixel))
                    continue;

                if ((a.mapPixel - b.mapPixel).sqrMagnitude > maxDistanceSqr)
                    continue;

                ConnectNavNodesIfWalkable(a.id, b.id);
            }
        }
    }

    private static void AddNeighborIfMissing(VenueNavNodeDefinition node, string neighborNodeId)
    {
        if (node.neighborNodeIds == null)
            node.neighborNodeIds = new List<string>();

        if (!node.neighborNodeIds.Contains(neighborNodeId))
            node.neighborNodeIds.Add(neighborNodeId);
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

[Serializable]
public class WalkableAreaDefinition
{
    public string id;
    public string displayName;
    [Tooltip("Closed polygon in source map pixel coordinates. Do not repeat the first point at the end.")]
    public List<Vector2> polygonPixels = new List<Vector2>();

    public bool ContainsMapPixel(Vector2 point)
    {
        if (polygonPixels == null || polygonPixels.Count < 3)
            return false;

        bool inside = false;
        int previousIndex = polygonPixels.Count - 1;

        for (int currentIndex = 0; currentIndex < polygonPixels.Count; currentIndex++)
        {
            Vector2 current = polygonPixels[currentIndex];
            Vector2 previous = polygonPixels[previousIndex];

            bool crossesHorizontalRay = (current.y > point.y) != (previous.y > point.y);
            if (crossesHorizontalRay)
            {
                float intersectionX = (previous.x - current.x) * (point.y - current.y) / (previous.y - current.y) + current.x;
                if (point.x < intersectionX)
                    inside = !inside;
            }

            previousIndex = currentIndex;
        }

        return inside;
    }
}

[Serializable]
public class ObstacleAreaDefinition : WalkableAreaDefinition
{
}

[Serializable]
public class VenueNavGraphDefinition
{
    public List<VenueNavNodeDefinition> nodes = new List<VenueNavNodeDefinition>();

    public VenueNavNodeDefinition FindNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || nodes == null)
            return null;

        foreach (VenueNavNodeDefinition node in nodes)
        {
            if (node != null && node.id == nodeId)
                return node;
        }

        return null;
    }
}

[Serializable]
public class VenueNavNodeDefinition
{
    public string id;
    public Vector2 mapPixel;
    public List<string> neighborNodeIds = new List<string>();
}
