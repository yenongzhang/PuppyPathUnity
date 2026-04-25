using UnityEngine;
using System;
using System.Collections.Generic;

public class NavigationRuntimeController : MonoBehaviour
{
    public enum NavState
    {
        Neutral,
        GettingCloser,
        GettingFarther,
        Lost,
        Arrived
    }

    [Header("References")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationController navigationController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private DogGuideController dogGuideController;

    [Header("Runtime Settings")]
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float arriveThreshold = 0.8f;
    [SerializeField] private float progressEpsilon = 0.05f;
    [SerializeField] private float offPathThreshold = 1.2f;
    [SerializeField] private float lostThresholdTime = 2.0f;

    [Header("Arrival")]
    [SerializeField] private bool autoCompleteOnArrival = false;
    [SerializeField] private float autoCompleteDelay = 2.0f;

    public NavState CurrentState { get; private set; } = NavState.Neutral;
    public float CurrentDistanceToGoal { get; private set; }
    public Vector3 CurrentRecommendedDirection { get; private set; }

    private readonly List<Transform> currentWaypoints = new List<Transform>();

    private bool isRunning;
    private float timer;
    private float previousDistanceToGoal;
    private float timeWithoutProgress;
    private int currentSegmentIndex;

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

        currentSegmentIndex = 0;
        timeWithoutProgress = 0f;
        timer = 0f;
        arrivalHandled = false;
        arrivalTimer = 0f;
        isRunning = true;

        CurrentDistanceToGoal = GetFlatDistance(
            xrCamera.position,
            currentWaypoints[currentWaypoints.Count - 1].position
        );

        previousDistanceToGoal = CurrentDistanceToGoal;

        UpdateRecommendedDirection();
        SetState(NavState.Neutral, true);

        if (dogGuideController != null)
            dogGuideController.BeginGuiding(currentWaypoints, xrCamera);
    }

    public void StopRuntime()
    {
        isRunning = false;
        currentWaypoints.Clear();
        currentSegmentIndex = 0;
        timeWithoutProgress = 0f;
        timer = 0f;
        arrivalHandled = false;
        arrivalTimer = 0f;

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

            if (dogGuideController != null)
                dogGuideController.ApplyNavigationState(
                    CurrentState,
                    CurrentDistanceToGoal,
                    CurrentRecommendedDirection
                );

            return;
        }

        currentSegmentIndex = FindClosestSegmentIndex(userPos);
        UpdateRecommendedDirection();

        float distanceToPath = GetDistanceToSegmentXZ(
            userPos,
            currentWaypoints[currentSegmentIndex].position,
            currentWaypoints[currentSegmentIndex + 1].position
        );

        bool isProgressing = CurrentDistanceToGoal < previousDistanceToGoal - progressEpsilon;

        if (distanceToPath > offPathThreshold)
        {
            timeWithoutProgress += updateInterval;

            if (timeWithoutProgress >= lostThresholdTime)
                SetState(NavState.Lost);
            else
                SetState(NavState.GettingFarther);
        }
        else
        {
            if (isProgressing)
            {
                timeWithoutProgress = 0f;
                SetState(NavState.GettingCloser);
            }
            else
            {
                timeWithoutProgress += updateInterval;

                if (timeWithoutProgress >= lostThresholdTime)
                    SetState(NavState.Lost);
                else
                    SetState(NavState.GettingFarther);
            }
        }

        previousDistanceToGoal = CurrentDistanceToGoal;

        if (dogGuideController != null)
            dogGuideController.ApplyNavigationState(
                CurrentState,
                CurrentDistanceToGoal,
                CurrentRecommendedDirection
            );
    }

    private void UpdateRecommendedDirection()
    {
        if (currentWaypoints.Count < 2)
        {
            CurrentRecommendedDirection = Vector3.forward;
            return;
        }

        Vector3 from = currentWaypoints[currentSegmentIndex].position;
        Vector3 to = currentWaypoints[Mathf.Min(currentSegmentIndex + 1, currentWaypoints.Count - 1)].position;

        Vector3 dir = to - from;
        dir.y = 0f;

        CurrentRecommendedDirection = dir.sqrMagnitude > 0.0001f
            ? dir.normalized
            : Vector3.forward;
    }

    private int FindClosestSegmentIndex(Vector3 userPos)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < currentWaypoints.Count - 1; i++)
        {
            float d = GetDistanceToSegmentXZ(
                userPos,
                currentWaypoints[i].position,
                currentWaypoints[i + 1].position
            );

            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private float GetDistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 p1 = new Vector2(a.x, a.z);
        Vector2 p2 = new Vector2(b.x, b.z);

        Vector2 segment = p2 - p1;
        float lenSq = segment.sqrMagnitude;

        if (lenSq < 0.0001f)
            return Vector2.Distance(p, p1);

        float t = Mathf.Clamp01(Vector2.Dot(p - p1, segment) / lenSq);
        Vector2 projection = p1 + segment * t;

        return Vector2.Distance(p, projection);
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