using System.Collections.Generic;
using UnityEngine;

public class VenueNavigationRuntime : MonoBehaviour
{
    private struct ClosestRouteInfo
    {
        public int segmentIndex;
        public float distanceToRoute;
        public float alongRouteDistance;
        public Vector3 segmentDirection;
        public Vector3 projectedPoint;
    }

    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform xrCamera;
    [SerializeField] private Transform venueContentRoot;
    [SerializeField] private VenueRouteLineController routeLineController;
    [SerializeField] private DogGuideController dogGuideController;

    [Header("Navigation")]
    [SerializeField] private string testDestinationAttractionId = "photo_wall";
    [SerializeField] private bool drawRouteLine = true;
    [SerializeField] private bool startTestNavigationOnPlay;
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float repathInterval = 1.0f;
    [SerializeField] private float minUserMoveBeforeRepath = 0.45f;
    [SerializeField] private float arriveDistanceMeters = 1.25f;
    [SerializeField] private float userMoveEpsilon = 0.025f;
    [SerializeField] private bool allowNearestNavNodeFallback = true;

    [Header("Free Roam Dog")]
    [SerializeField] private bool keepDogInFreeRoam = true;
    [SerializeField] private float freeRoamUpdateInterval = 0.2f;
    [SerializeField] private float freeRoamLeadDistance = 1.8f;
    [SerializeField] private float freeRoamFallbackDistance = 1.2f;
    [SerializeField] private float freeRoamDirectionProbeDegrees = 45f;

    [Header("Debug")]
    [SerializeField] private bool logRouteEvents = true;

    public bool IsNavigating { get; private set; }
    public bool IsFreeRoaming { get; private set; }
    public string CurrentDestinationAttractionId { get; private set; }
    public string CurrentDestinationDisplayName { get; private set; }
    public NavigationRuntimeController.NavState CurrentState { get; private set; } = NavigationRuntimeController.NavState.Neutral;
    public float CurrentDistanceToGoal { get; private set; }
    public Vector3 CurrentRecommendedDirection { get; private set; } = Vector3.forward;

    private readonly List<Vector3> routeWorldPoints = new List<Vector3>();
    private readonly List<Vector3> candidateRouteWorldPoints = new List<Vector3>();
    private readonly List<float> cumulativeRouteDistances = new List<float>();
    private readonly List<Transform> dogRuntimeWaypoints = new List<Transform>();

    private Transform dogWaypointRoot;
    private AttractionDefinition currentAttraction;
    private Vector2 currentDestinationPixel;
    private float updateTimer;
    private float repathTimer;
    private float freeRoamUpdateTimer;
    private float previousAlongRouteDistance;
    private Vector3 previousUserPosition;
    private Vector3 lastRepathUserPosition;
    private bool dogGuidingStarted;

    private void Start()
    {
        if (startTestNavigationOnPlay)
            StartNavigationToAttraction(testDestinationAttractionId);
    }

    private void Update()
    {
        if (IsFreeRoaming && !IsNavigating)
        {
            UpdateFreeRoamDog();
            return;
        }

        if (!IsNavigating || xrCamera == null || routeWorldPoints.Count < 2)
            return;

        updateTimer += Time.deltaTime;
        repathTimer += Time.deltaTime;

        if (repathTimer >= repathInterval &&
            GetFlatDistance(xrCamera.position, lastRepathUserPosition) >= minUserMoveBeforeRepath)
        {
            RebuildRouteFromCurrentUserPosition(false);
            repathTimer = 0f;
        }

        if (updateTimer < updateInterval)
            return;

        updateTimer = 0f;
        EvaluateNavigation();
    }

    public bool StartNavigationToAttraction(string attractionId)
    {
        if (IsNavigating)
            StopNavigation();

        if (mapDefinition == null || xrCamera == null || string.IsNullOrEmpty(attractionId))
        {
            Debug.LogWarning("VenueNavigationRuntime: missing map, XR camera, or attraction id.");
            return false;
        }

        AttractionDefinition attraction = mapDefinition.FindAttraction(attractionId);
        if (attraction == null)
        {
            Debug.LogWarning("VenueNavigationRuntime: attraction id not found: " + attractionId);
            return false;
        }

        string displayName = !string.IsNullOrWhiteSpace(attraction.displayName) ? attraction.displayName : attractionId;

        return StartNavigationToMapPixel(attractionId, displayName, attraction.GetArrivalPixel(), attraction);
    }

    public bool StartNavigationToMapPixel(string destinationId, string displayName, Vector2 destinationPixel)
    {
        return StartNavigationToMapPixel(destinationId, displayName, destinationPixel, null);
    }

    private bool StartNavigationToMapPixel(
        string destinationId,
        string displayName,
        Vector2 destinationPixel,
        AttractionDefinition attraction)
    {
        if (IsFreeRoaming)
            StopFreeRoamGuiding();

        if (IsNavigating)
            StopNavigation();

        if (mapDefinition == null || xrCamera == null)
        {
            Debug.LogWarning("VenueNavigationRuntime: missing map or XR camera.");
            return false;
        }

        currentAttraction = attraction;
        currentDestinationPixel = destinationPixel;
        CurrentDestinationAttractionId = attraction != null ? attraction.id : null;
        CurrentDestinationDisplayName = string.IsNullOrWhiteSpace(displayName) ? destinationId : displayName;

        if (!RebuildRouteFromCurrentUserPosition(true))
        {
            StopNavigation();
            return false;
        }

        IsNavigating = true;
        updateTimer = 0f;
        repathTimer = 0f;
        previousUserPosition = xrCamera.position;
        lastRepathUserPosition = xrCamera.position;
        previousAlongRouteDistance = FindClosestRouteInfo(xrCamera.position).alongRouteDistance;
        SetState(NavigationRuntimeController.NavState.Neutral);

        StartDogGuidingIfNeeded();
        if (dogGuideController != null)
        {
            dogGuideController.ClearGuidanceTargetOverride();
            dogGuideController.SetPositiveRandomBehaviorsSuppressed(false);
        }

        EvaluateNavigation();

        if (logRouteEvents)
            Debug.Log("VenueNavigationRuntime: started navigation to " + CurrentDestinationDisplayName);

        return true;
    }

    public void StopNavigation()
    {
        IsNavigating = false;
        CurrentDestinationAttractionId = null;
        CurrentDestinationDisplayName = null;
        currentAttraction = null;
        currentDestinationPixel = Vector2.zero;
        routeWorldPoints.Clear();
        candidateRouteWorldPoints.Clear();
        cumulativeRouteDistances.Clear();
        previousAlongRouteDistance = 0f;
        updateTimer = 0f;
        repathTimer = 0f;

        if (routeLineController != null)
            routeLineController.ClearRoute();

        StopDogGuiding(false);
        ClearDogRuntimeWaypoints();
        SetState(NavigationRuntimeController.NavState.Neutral);
    }

    public void StartFreeRoamGuiding()
    {
        if (!keepDogInFreeRoam || IsNavigating)
            return;

        if (mapDefinition == null || xrCamera == null || dogGuideController == null)
            return;

        IsFreeRoaming = true;
        CurrentState = NavigationRuntimeController.NavState.Neutral;
        CurrentDistanceToGoal = freeRoamLeadDistance;
        CurrentRecommendedDirection = FindFreeRoamDirection();
        freeRoamUpdateTimer = freeRoamUpdateInterval;

        StartDogGuidingIfNeeded(CurrentRecommendedDirection);
        ApplyFreeRoamDogTarget();
        NotifyDog();
    }

    public void StopFreeRoamGuiding()
    {
        IsFreeRoaming = false;
        StopDogGuiding(false);
        ClearDogRuntimeWaypoints();
    }

    [ContextMenu("Start Test Navigation")]
    private void StartTestNavigation()
    {
        StartNavigationToAttraction(testDestinationAttractionId);
    }

    [ContextMenu("Stop Navigation")]
    private void StopNavigationFromMenu()
    {
        StopNavigation();
    }

    private bool RebuildRouteFromCurrentUserPosition(bool warnOnFailure)
    {
        if (mapDefinition == null || xrCamera == null)
            return false;

        Vector2 startPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(xrCamera.position));
        Vector2 endPixel = currentDestinationPixel;

        if (allowNearestNavNodeFallback)
        {
            if (!mapDefinition.IsMapPixelWalkable(startPixel))
                TryFindNearestWalkableNavNodePixel(startPixel, out startPixel);

            if (!mapDefinition.IsMapPixelWalkable(endPixel))
                TryFindNearestWalkableNavNodePixel(endPixel, out endPixel);
        }

        if (!VenuePathfinder.TryFindWorldPath(mapDefinition, startPixel, endPixel, candidateRouteWorldPoints))
        {
            if (warnOnFailure)
                Debug.LogWarning("VenueNavigationRuntime: no route found to " + CurrentDestinationDisplayName);

            return false;
        }

        routeWorldPoints.Clear();
        for (int i = 0; i < candidateRouteWorldPoints.Count; i++)
            routeWorldPoints.Add(VenueLocalToWorld(candidateRouteWorldPoints[i]));

        BuildCumulativeRouteDistances();
        lastRepathUserPosition = xrCamera.position;

        if (drawRouteLine && routeLineController != null)
            routeLineController.ShowWorldRoute(routeWorldPoints);

        RebuildDogRuntimeWaypoints();
        return true;
    }

    private void EvaluateNavigation()
    {
        Vector3 userPosition = xrCamera.position;
        Vector3 goalPosition = routeWorldPoints[routeWorldPoints.Count - 1];
        CurrentDistanceToGoal = GetFlatDistance(userPosition, goalPosition);

        if (CurrentDistanceToGoal <= arriveDistanceMeters)
        {
            SetState(NavigationRuntimeController.NavState.Arrived);
            CurrentRecommendedDirection = GetDirectionTo(goalPosition, userPosition);
            NotifyDog();
            return;
        }

        ClosestRouteInfo routeInfo = FindClosestRouteInfo(userPosition);
        CurrentRecommendedDirection = routeInfo.segmentDirection;

        float userMoveDistance = GetFlatDistance(userPosition, previousUserPosition);
        float alongDelta = routeInfo.alongRouteDistance - previousAlongRouteDistance;

        if (userMoveDistance > userMoveEpsilon && alongDelta > -0.05f)
            SetState(NavigationRuntimeController.NavState.GettingCloser);
        else
            SetState(NavigationRuntimeController.NavState.Neutral);

        previousAlongRouteDistance = routeInfo.alongRouteDistance;
        previousUserPosition = userPosition;

        NotifyDog();
    }

    private void NotifyDog()
    {
        if (dogGuideController == null || !dogGuidingStarted)
            return;

        dogGuideController.ApplyNavigationState(
            CurrentState,
            CurrentDistanceToGoal,
            CurrentRecommendedDirection);
    }

    private void StartDogGuidingIfNeeded()
    {
        StartDogGuidingIfNeeded(CurrentRecommendedDirection);
    }

    private void StartDogGuidingIfNeeded(Vector3 initialDirection)
    {
        if (dogGuideController == null || dogGuidingStarted || xrCamera == null)
            return;

        if (IsFreeRoaming && !IsNavigating)
            RebuildFreeRoamDogRuntimeWaypoints(initialDirection);
        else
            RebuildDogRuntimeWaypoints();

        if (dogRuntimeWaypoints.Count < 2)
            return;

        dogGuideController.BeginGuiding(dogRuntimeWaypoints, xrCamera);
        dogGuidingStarted = true;
    }

    private void StopDogGuiding(bool destroyDog)
    {
        if (dogGuideController != null && dogGuidingStarted)
            dogGuideController.StopGuiding(destroyDog);

        dogGuidingStarted = false;
    }

    private void UpdateFreeRoamDog()
    {
        if (!keepDogInFreeRoam || xrCamera == null)
            return;

        if (!dogGuidingStarted)
            StartDogGuidingIfNeeded(CurrentRecommendedDirection);

        freeRoamUpdateTimer += Time.deltaTime;
        if (freeRoamUpdateTimer >= freeRoamUpdateInterval)
        {
            freeRoamUpdateTimer = 0f;
            CurrentRecommendedDirection = FindFreeRoamDirection();
        }

        CurrentDistanceToGoal = freeRoamLeadDistance;
        ApplyFreeRoamDogTarget();
        SetState(NavigationRuntimeController.NavState.Neutral);
    }

    private void ApplyFreeRoamDogTarget()
    {
        if (dogGuideController == null || xrCamera == null)
            return;

        Vector3 direction = CurrentRecommendedDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = GetFlatForward(xrCamera);
        direction.Normalize();

        Vector3 target = GetVenueGroundWorldPosition(xrCamera.position + direction * freeRoamLeadDistance);
        dogGuideController.SetPositiveRandomBehaviorsSuppressed(true);
        dogGuideController.TickFreeRoamFollow(target, direction);
    }

    private void RebuildFreeRoamDogRuntimeWaypoints(Vector3 direction)
    {
        ClearDogRuntimeWaypoints();

        if (xrCamera == null)
            return;

        if (dogWaypointRoot == null)
        {
            GameObject rootObject = new GameObject("VenueNavigationRuntime_DogWaypoints");
            rootObject.transform.SetParent(transform, false);
            dogWaypointRoot = rootObject.transform;
        }

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.0001f)
            flatDirection = GetFlatForward(xrCamera);
        flatDirection.Normalize();

        Vector3 start = GetVenueGroundWorldPosition(xrCamera.position);
        Vector3 end = GetVenueGroundWorldPosition(xrCamera.position + flatDirection * Mathf.Max(freeRoamFallbackDistance, freeRoamLeadDistance));

        CreateDogRuntimeWaypoint("dog_free_roam_wp_0", start);
        CreateDogRuntimeWaypoint("dog_free_roam_wp_1", end);
    }

    private void CreateDogRuntimeWaypoint(string waypointName, Vector3 position)
    {
        GameObject waypointObject = new GameObject(waypointName);
        waypointObject.transform.SetParent(dogWaypointRoot, false);
        waypointObject.transform.position = position;
        dogRuntimeWaypoints.Add(waypointObject.transform);
    }

    private Vector3 GetVenueGroundWorldPosition(Vector3 worldPosition)
    {
        if (venueContentRoot != null)
        {
            Vector3 localPosition = venueContentRoot.InverseTransformPoint(worldPosition);
            localPosition.y = 0f;
            return venueContentRoot.TransformPoint(localPosition);
        }

        worldPosition.y = 0f;
        return worldPosition;
    }

    private void RebuildDogRuntimeWaypoints()
    {
        ClearDogRuntimeWaypoints();

        if (routeWorldPoints.Count < 2)
            return;

        if (dogWaypointRoot == null)
        {
            GameObject rootObject = new GameObject("VenueNavigationRuntime_DogWaypoints");
            rootObject.transform.SetParent(transform, false);
            dogWaypointRoot = rootObject.transform;
        }

        for (int i = 0; i < routeWorldPoints.Count; i++)
        {
            CreateDogRuntimeWaypoint("dog_route_wp_" + i, routeWorldPoints[i]);
        }
    }

    private Vector3 FindFreeRoamDirection()
    {
        Vector3 forward = GetFlatForward(xrCamera);
        float probe = Mathf.Max(5f, freeRoamDirectionProbeDegrees);
        Vector3[] candidates =
        {
            forward,
            Quaternion.AngleAxis(-probe, Vector3.up) * forward,
            Quaternion.AngleAxis(probe, Vector3.up) * forward,
            Quaternion.AngleAxis(-probe * 2f, Vector3.up) * forward,
            Quaternion.AngleAxis(probe * 2f, Vector3.up) * forward
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 direction = candidates[i];
            if (IsFreeRoamDirectionWalkable(direction, freeRoamLeadDistance))
                return direction.normalized;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 direction = candidates[i];
            if (IsFreeRoamDirectionWalkable(direction, freeRoamFallbackDistance))
                return direction.normalized;
        }

        return TryFindNearestWalkableDirection(out Vector3 fallbackDirection)
            ? fallbackDirection
            : forward;
    }

    private bool IsFreeRoamDirectionWalkable(Vector3 worldDirection, float distance)
    {
        if (mapDefinition == null || xrCamera == null)
            return true;

        Vector3 startLocal = WorldToVenueLocal(xrCamera.position);
        Vector3 endLocal = WorldToVenueLocal(xrCamera.position + worldDirection.normalized * distance);
        Vector2 startPixel = mapDefinition.WorldToMapPixel(startLocal);
        Vector2 endPixel = mapDefinition.WorldToMapPixel(endLocal);

        if (!mapDefinition.IsMapPixelWalkable(endPixel))
            return false;

        return mapDefinition.IsMapSegmentWalkable(startPixel, endPixel);
    }

    private bool TryFindNearestWalkableDirection(out Vector3 worldDirection)
    {
        worldDirection = GetFlatForward(xrCamera);

        if (mapDefinition == null || mapDefinition.navGraph == null || mapDefinition.navGraph.nodes == null || xrCamera == null)
            return false;

        Vector2 userPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(xrCamera.position));
        if (!TryFindNearestWalkableNavNodePixel(userPixel, out Vector2 navNodePixel))
            return false;

        Vector3 targetWorld = VenueLocalToWorld(mapDefinition.MapPixelToWorld(navNodePixel));
        worldDirection = GetDirectionTo(targetWorld, xrCamera.position);
        return worldDirection.sqrMagnitude > 0.0001f;
    }

    private void ClearDogRuntimeWaypoints()
    {
        dogRuntimeWaypoints.Clear();

        if (dogWaypointRoot == null)
            return;

        for (int i = dogWaypointRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = dogWaypointRoot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void BuildCumulativeRouteDistances()
    {
        cumulativeRouteDistances.Clear();

        if (routeWorldPoints.Count == 0)
            return;

        cumulativeRouteDistances.Add(0f);
        float total = 0f;

        for (int i = 0; i < routeWorldPoints.Count - 1; i++)
        {
            total += GetFlatDistance(routeWorldPoints[i], routeWorldPoints[i + 1]);
            cumulativeRouteDistances.Add(total);
        }
    }

    private ClosestRouteInfo FindClosestRouteInfo(Vector3 userPosition)
    {
        ClosestRouteInfo bestInfo = new ClosestRouteInfo
        {
            segmentIndex = 0,
            distanceToRoute = float.MaxValue,
            alongRouteDistance = 0f,
            segmentDirection = GetFlatForward(xrCamera),
            projectedPoint = userPosition
        };

        Vector2 p = new Vector2(userPosition.x, userPosition.z);

        for (int i = 0; i < routeWorldPoints.Count - 1; i++)
        {
            Vector3 a3 = routeWorldPoints[i];
            Vector3 b3 = routeWorldPoints[i + 1];
            Vector2 a = new Vector2(a3.x, a3.z);
            Vector2 b = new Vector2(b3.x, b3.z);
            Vector2 segment = b - a;
            float segmentLengthSq = segment.sqrMagnitude;

            if (segmentLengthSq < 0.0001f)
                continue;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / segmentLengthSq);
            Vector2 projected = a + segment * t;
            float distance = Vector2.Distance(p, projected);

            if (distance < bestInfo.distanceToRoute)
            {
                Vector3 direction = b3 - a3;
                direction.y = 0f;

                bestInfo.segmentIndex = i;
                bestInfo.distanceToRoute = distance;
                bestInfo.alongRouteDistance = cumulativeRouteDistances[i] + Mathf.Sqrt(segmentLengthSq) * t;
                bestInfo.segmentDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFlatForward(xrCamera);
                bestInfo.projectedPoint = new Vector3(projected.x, userPosition.y, projected.y);
            }
        }

        return bestInfo;
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

    private void SetState(NavigationRuntimeController.NavState state)
    {
        CurrentState = state;
    }

    private static Vector3 GetDirectionTo(Vector3 target, Vector3 origin)
    {
        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }

    private static Vector3 GetFlatForward(Transform transform)
    {
        if (transform == null)
            return Vector3.forward;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 WorldToVenueLocal(Vector3 worldPosition)
    {
        return venueContentRoot != null ? venueContentRoot.InverseTransformPoint(worldPosition) : worldPosition;
    }

    private Vector3 VenueLocalToWorld(Vector3 venueLocalPosition)
    {
        return venueContentRoot != null ? venueContentRoot.TransformPoint(venueLocalPosition) : venueLocalPosition;
    }
}
