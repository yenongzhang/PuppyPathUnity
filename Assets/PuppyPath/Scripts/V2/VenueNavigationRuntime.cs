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

    [Header("Debug")]
    [SerializeField] private bool logRouteEvents = true;

    public bool IsNavigating { get; private set; }
    public string CurrentDestinationAttractionId { get; private set; }
    public NavigationRuntimeController.NavState CurrentState { get; private set; } = NavigationRuntimeController.NavState.Neutral;
    public float CurrentDistanceToGoal { get; private set; }
    public Vector3 CurrentRecommendedDirection { get; private set; } = Vector3.forward;

    private readonly List<Vector3> routeWorldPoints = new List<Vector3>();
    private readonly List<Vector3> candidateRouteWorldPoints = new List<Vector3>();
    private readonly List<float> cumulativeRouteDistances = new List<float>();
    private readonly List<Transform> dogRuntimeWaypoints = new List<Transform>();

    private Transform dogWaypointRoot;
    private AttractionDefinition currentAttraction;
    private float updateTimer;
    private float repathTimer;
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

        currentAttraction = attraction;
        CurrentDestinationAttractionId = attractionId;

        if (!RebuildRouteFromCurrentUserPosition(true))
            return false;

        IsNavigating = true;
        updateTimer = 0f;
        repathTimer = 0f;
        previousUserPosition = xrCamera.position;
        lastRepathUserPosition = xrCamera.position;
        previousAlongRouteDistance = FindClosestRouteInfo(xrCamera.position).alongRouteDistance;
        SetState(NavigationRuntimeController.NavState.Neutral);

        StartDogGuidingIfNeeded();
        EvaluateNavigation();

        if (logRouteEvents)
            Debug.Log("VenueNavigationRuntime: started navigation to " + attractionId);

        return true;
    }

    public void StopNavigation()
    {
        IsNavigating = false;
        CurrentDestinationAttractionId = null;
        currentAttraction = null;
        routeWorldPoints.Clear();
        candidateRouteWorldPoints.Clear();
        cumulativeRouteDistances.Clear();
        previousAlongRouteDistance = 0f;
        updateTimer = 0f;
        repathTimer = 0f;

        if (routeLineController != null)
            routeLineController.ClearRoute();

        if (dogGuideController != null && dogGuidingStarted)
            dogGuideController.StopGuiding();

        dogGuidingStarted = false;
        ClearDogRuntimeWaypoints();
        SetState(NavigationRuntimeController.NavState.Neutral);
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
        if (currentAttraction == null || mapDefinition == null || xrCamera == null)
            return false;

        Vector2 startPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(xrCamera.position));
        Vector2 endPixel = currentAttraction.GetArrivalPixel();

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
                Debug.LogWarning("VenueNavigationRuntime: no route found to " + currentAttraction.id);

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
        if (dogGuideController == null || dogGuidingStarted || xrCamera == null)
            return;

        RebuildDogRuntimeWaypoints();
        if (dogRuntimeWaypoints.Count < 2)
            return;

        dogGuideController.BeginGuiding(dogRuntimeWaypoints, xrCamera);
        dogGuidingStarted = true;
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
            GameObject waypointObject = new GameObject("dog_route_wp_" + i);
            waypointObject.transform.SetParent(dogWaypointRoot, false);
            waypointObject.transform.position = routeWorldPoints[i];
            dogRuntimeWaypoints.Add(waypointObject.transform);
        }
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
