using UnityEngine;
using System;
using System.Collections.Generic;

public class NavigationRuntimeController : MonoBehaviour
{
    public enum NavState
    {
        Neutral,
        Waiting,
        GettingCloser,
        GettingFarther,
        Lost,
        Arrived
    }

    private struct ClosestPathInfo
    {
        public int segmentIndex;
        public float distanceToPath;
        public float alongPathDistance;
        public Vector3 segmentDirection;
        public Vector3 projectedPoint;
    }

    [Header("References")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationController navigationController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private DogGuideController dogGuideController;

    [Header("Runtime Settings")]
    [SerializeField] private float updateInterval = 0.25f;
    [SerializeField] private float arriveThreshold = 1.5f;

    [Header("Road-like Path Judgment")]
    [Tooltip("允许用户在路径中心线两侧多远仍然算走在正确道路上。4 表示左右偏差 4 米都可以接受。")]
    [SerializeField] private float pathCorridorHalfWidth = 4.0f;

    [Tooltip("用户移动方向和当前路径方向夹角小于这个值，才认为方向正确。")]
    [SerializeField] private float headingAngleTolerance = 30.0f;

    [Tooltip("沿着路径前进多少米才算明显前进。")]
    [SerializeField] private float alongPathProgressEpsilon = 0.04f;

    [Tooltip("沿着路径倒退多少米才算明显走反。")]
    [SerializeField] private float backwardProgressEpsilon = 0.10f;

    [Tooltip("离路径中心线超过这个距离，就直接作为严重 off path 处理。")]
    [SerializeField] private float offPathThreshold = 6.0f;

    [Tooltip("方向或距离错误持续多久后进入 GettingFarther。建议 0.5 到 1.0。")]
    [SerializeField] private float gettingFartherThresholdTime = 0.75f;

    [Tooltip("持续多久 off path / 方向错误，才进入 Lost。")]
    [SerializeField] private float lostThresholdTime = 8.0f;

    [Header("User Movement Detection")]
    [Tooltip("每次检测之间用户移动超过这个值，才认为用户真的在移动。")]
    [SerializeField] private float userMoveEpsilon = 0.015f;

    [Tooltip("用户停多久才进入 Waiting。不要太短，否则小狗会频繁停下。")]
    [SerializeField] private float waitingThresholdTime = 8.0f;

    [Header("Arrival")]
    [SerializeField] private bool autoCompleteOnArrival = false;
    [SerializeField] private float autoCompleteDelay = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool logDetailedState = false;

    public NavState CurrentState { get; private set; } = NavState.Neutral;
    public float CurrentDistanceToGoal { get; private set; }
    public Vector3 CurrentRecommendedDirection { get; private set; }

    private readonly List<Transform> currentWaypoints = new List<Transform>();
    private readonly List<float> cumulativePathDistances = new List<float>();

    private bool isRunning;
    private float timer;

    private int currentSegmentIndex;
    private float previousAlongPathDistance;
    private Vector3 previousUserPosition;
    private float timeStandingStill;
    private float timeWrongDirectionOrOffPath;
    private float timeSeriouslyOffPath;

    private bool arrivalHandled;
    private float arrivalTimer;

    public event Action<NavState> OnNavStateChanged;

    public void StartRuntime()
    {
        PathDefinition path = previewController != null ? previewController.GetCurrentPathInstance() : null;

        if (path == null || path.waypoints == null || path.waypoints.Count < 2)
        {
            Debug.LogWarning("NavigationRuntimeController: no valid path to run.");
            return;
        }

        if (xrCamera == null)
        {
            Debug.LogWarning("NavigationRuntimeController: xrCamera is missing.");
            return;
        }

        currentWaypoints.Clear();

        foreach (Transform wp in path.waypoints)
        {
            if (wp != null)
                currentWaypoints.Add(wp);
        }

        if (currentWaypoints.Count < 2)
        {
            Debug.LogWarning("NavigationRuntimeController: not enough valid waypoints.");
            return;
        }

        BuildCumulativePathDistances();

        currentSegmentIndex = 0;
        timer = 0f;
        timeStandingStill = 0f;
        timeWrongDirectionOrOffPath = 0f;
        timeSeriouslyOffPath = 0f;
        arrivalHandled = false;
        arrivalTimer = 0f;
        isRunning = true;

        Vector3 userPos = xrCamera.position;
        Vector3 goalPos = currentWaypoints[currentWaypoints.Count - 1].position;

        CurrentDistanceToGoal = GetFlatDistance(userPos, goalPos);

        ClosestPathInfo pathInfo = FindClosestPathInfo(userPos);
        currentSegmentIndex = pathInfo.segmentIndex;
        previousAlongPathDistance = pathInfo.alongPathDistance;
        previousUserPosition = userPos;

        CurrentRecommendedDirection = pathInfo.segmentDirection;

        SetState(NavState.Neutral, true);

        if (dogGuideController != null)
            dogGuideController.BeginGuiding(currentWaypoints, xrCamera);
    }

    public void StopRuntime()
    {
        isRunning = false;
        currentWaypoints.Clear();
        cumulativePathDistances.Clear();

        currentSegmentIndex = 0;
        timer = 0f;
        timeStandingStill = 0f;
        timeWrongDirectionOrOffPath = 0f;
        timeSeriouslyOffPath = 0f;
        arrivalHandled = false;
        arrivalTimer = 0f;
        previousAlongPathDistance = 0f;
        previousUserPosition = Vector3.zero;

        if (dogGuideController != null)
            dogGuideController.StopGuiding();

        SetState(NavState.Neutral, true);
    }

    private void Update()
    {
        if (!isRunning || xrCamera == null || currentWaypoints.Count < 2)
            return;

        if (CurrentState == NavState.Arrived)
        {
            if (autoCompleteOnArrival && !arrivalHandled)
            {
                arrivalTimer += Time.deltaTime;

                if (arrivalTimer >= autoCompleteDelay)
                {
                    arrivalHandled = true;

                    if (navigationController != null)
                        navigationController.CompleteNavigation();
                }
            }

            return;
        }

        timer += Time.deltaTime;

        if (timer < updateInterval)
            return;

        timer = 0f;
        EvaluateNavigation();
    }

    private void EvaluateNavigation()
    {
        Vector3 userPos = xrCamera.position;
        Vector3 goalPos = currentWaypoints[currentWaypoints.Count - 1].position;

        CurrentDistanceToGoal = GetFlatDistance(userPos, goalPos);

        if (CurrentDistanceToGoal <= arriveThreshold)
        {
            SetState(NavState.Arrived);
            NotifyDog();

            if (navigationController != null)
                navigationController.CompleteNavigation();

            return;
        }

        ClosestPathInfo pathInfo = FindClosestPathInfo(userPos);
        currentSegmentIndex = pathInfo.segmentIndex;
        CurrentRecommendedDirection = pathInfo.segmentDirection;

        float userMoveDistance = GetFlatDistance(userPos, previousUserPosition);
        bool userIsMoving = userMoveDistance > userMoveEpsilon;

        Vector3 userMoveDirection = userPos - previousUserPosition;
        userMoveDirection.y = 0f;

        float headingAngle = 0f;
        bool headingIsGood = false;

        if (userIsMoving && userMoveDirection.sqrMagnitude > 0.0001f)
        {
            headingAngle = Vector3.Angle(userMoveDirection.normalized, pathInfo.segmentDirection);
            headingIsGood = headingAngle <= headingAngleTolerance;
        }

        float alongDelta = pathInfo.alongPathDistance - previousAlongPathDistance;
        bool progressingAlongPath = alongDelta > alongPathProgressEpsilon;
        bool clearlyGoingBackward = alongDelta < -backwardProgressEpsilon;

        bool insideRoadCorridor = pathInfo.distanceToPath <= pathCorridorHalfWidth;
        bool seriouslyOffPath = pathInfo.distanceToPath > offPathThreshold;

        if (!userIsMoving)
            timeStandingStill += updateInterval;
        else
            timeStandingStill = 0f;

        NavState nextState;

        if (!userIsMoving)
        {
            timeWrongDirectionOrOffPath = Mathf.Max(0f, timeWrongDirectionOrOffPath - updateInterval);
            timeSeriouslyOffPath = Mathf.Max(0f, timeSeriouslyOffPath - updateInterval);

            if (timeStandingStill >= waitingThresholdTime)
                nextState = NavState.Waiting;
            else
                nextState = NavState.Neutral;
        }
        else
        {
            bool correctDistance = insideRoadCorridor;
            bool correctHeading = headingIsGood;
            bool notGoingBackward = !clearlyGoingBackward;

            bool userDirectionIsCorrect =
                correctDistance &&
                correctHeading &&
                notGoingBackward;

            if (seriouslyOffPath)
            {
                timeSeriouslyOffPath += updateInterval;
                timeWrongDirectionOrOffPath += updateInterval;

                if (timeSeriouslyOffPath >= lostThresholdTime)
                {
                    nextState = NavState.Lost;
                }
                else if (timeWrongDirectionOrOffPath >= gettingFartherThresholdTime)
                {
                    nextState = NavState.GettingFarther;
                }
                else
                {
                    nextState = NavState.Neutral;
                }
            }
            else if (userDirectionIsCorrect)
            {
                timeWrongDirectionOrOffPath = 0f;
                timeSeriouslyOffPath = 0f;

                if (progressingAlongPath)
                    nextState = NavState.GettingCloser;
                else
                    nextState = NavState.Neutral;
            }
            else
            {
                timeWrongDirectionOrOffPath += updateInterval;
                timeSeriouslyOffPath = Mathf.Max(0f, timeSeriouslyOffPath - updateInterval * 0.5f);

                if (timeWrongDirectionOrOffPath >= lostThresholdTime)
                {
                    nextState = NavState.Lost;
                }
                else if (timeWrongDirectionOrOffPath >= gettingFartherThresholdTime)
                {
                    nextState = NavState.GettingFarther;
                }
                else
                {
                    nextState = NavState.Neutral;
                }
            }
        }

        SetState(nextState);

        if (logDetailedState)
        {
            Debug.Log(
                $"Nav Debug | State={CurrentState}, " +
                $"DistToPath={pathInfo.distanceToPath:F2}, " +
                $"AllowedDist={pathCorridorHalfWidth:F2}, " +
                $"HeadingAngle={headingAngle:F1}, " +
                $"AllowedAngle={headingAngleTolerance:F1}, " +
                $"AlongDelta={alongDelta:F2}, " +
                $"Moving={userIsMoving}, " +
                $"InsideCorridor={insideRoadCorridor}, " +
                $"HeadingGood={headingIsGood}, " +
                $"Backward={clearlyGoingBackward}, " +
                $"WrongTime={timeWrongDirectionOrOffPath:F2}"
            );
        }

        previousAlongPathDistance = pathInfo.alongPathDistance;
        previousUserPosition = userPos;

        NotifyDog();
    }

    private void NotifyDog()
    {
        if (dogGuideController != null)
        {
            dogGuideController.ApplyNavigationState(
                CurrentState,
                CurrentDistanceToGoal,
                CurrentRecommendedDirection
            );
        }
    }

    private void BuildCumulativePathDistances()
    {
        cumulativePathDistances.Clear();

        if (currentWaypoints.Count == 0)
            return;

        cumulativePathDistances.Add(0f);

        float total = 0f;

        for (int i = 0; i < currentWaypoints.Count - 1; i++)
        {
            Vector3 a = currentWaypoints[i].position;
            Vector3 b = currentWaypoints[i + 1].position;

            a.y = 0f;
            b.y = 0f;

            total += Vector3.Distance(a, b);
            cumulativePathDistances.Add(total);
        }
    }

    private ClosestPathInfo FindClosestPathInfo(Vector3 userPos)
    {
        ClosestPathInfo bestInfo = new ClosestPathInfo
        {
            segmentIndex = 0,
            distanceToPath = float.MaxValue,
            alongPathDistance = 0f,
            segmentDirection = Vector3.forward,
            projectedPoint = userPos
        };

        Vector2 p = new Vector2(userPos.x, userPos.z);

        for (int i = 0; i < currentWaypoints.Count - 1; i++)
        {
            Vector3 a3 = currentWaypoints[i].position;
            Vector3 b3 = currentWaypoints[i + 1].position;

            Vector2 a = new Vector2(a3.x, a3.z);
            Vector2 b = new Vector2(b3.x, b3.z);

            Vector2 segment = b - a;
            float segmentLengthSq = segment.sqrMagnitude;

            if (segmentLengthSq < 0.0001f)
                continue;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / segmentLengthSq);
            Vector2 projected = a + segment * t;

            float distance = Vector2.Distance(p, projected);

            if (distance < bestInfo.distanceToPath)
            {
                float segmentLength = Mathf.Sqrt(segmentLengthSq);
                float alongDistance = cumulativePathDistances[i] + segmentLength * t;

                Vector3 direction = b3 - a3;
                direction.y = 0f;

                bestInfo.segmentIndex = i;
                bestInfo.distanceToPath = distance;
                bestInfo.alongPathDistance = alongDistance;
                bestInfo.segmentDirection = direction.sqrMagnitude > 0.0001f
                    ? direction.normalized
                    : Vector3.forward;
                bestInfo.projectedPoint = new Vector3(projected.x, userPos.y, projected.y);
            }
        }

        return bestInfo;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void SetState(NavState newState, bool forceUpdate = false)
    {
        if (!forceUpdate && CurrentState == newState)
            return;

        CurrentState = newState;

        Debug.Log("Nav State Changed: " + newState);

        if (hudController != null)
            hudController.UpdateStateText(GetStateDisplayText(newState));

        if (newState == NavState.Arrived)
        {
            arrivalTimer = 0f;
            arrivalHandled = false;
        }

        OnNavStateChanged?.Invoke(newState);
    }

    private string GetStateDisplayText(NavState state)
    {
        switch (state)
        {
            case NavState.Neutral:
                return "Getting ready...";

            case NavState.Waiting:
                return "Take your time. I'm waiting for you.";

            case NavState.GettingCloser:
                return "Good job! You're on the right path.";

            case NavState.GettingFarther:
                return "Hmm... this way doesn't look right.";

            case NavState.Lost:
                return "Oops, you're off the path.";

            case NavState.Arrived:
                return "You made it!";

            default:
                return "";
        }
    }
}