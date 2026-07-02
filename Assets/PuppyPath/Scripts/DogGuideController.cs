using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DogGuideController : MonoBehaviour
{
    private enum DogBehavior
    {
        SniffAhead,
        ShortCanter,
        TurnCircleUser,
        WalkBeside,
        WalkBehind,
        PauseAndLook,
        BarkAtUser,
        SitWait,
        Arrived
    }

    [Header("References")]
    [SerializeField] private GameObject dogPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip[] barkClips;
    [SerializeField] private AudioClip barkClip; // Optional fallback / old single bark clip.
    [SerializeField] private bool avoidRepeatingSameBarkClip = true;
    [SerializeField] private float barkVolume = 0.75f;
    [SerializeField] private float barkMinInterval = 0.45f;
    [SerializeField] private float barkRepeatInterval = 0.55f;
    [SerializeField] private float barkRepeatChance = 0.65f;

    [Header("Loop / Main Animation State Names")]
    [SerializeField] private string standState = "Stand";
    [SerializeField] private string walkState = "walk";
    [SerializeField] private string trotState = "Trot";
    [SerializeField] private string canterState = "Canter";
    [SerializeField] private string sniffState = "Sniff";
    [SerializeField] private string barkState = "Bark";
    [SerializeField] private string happyState = "Happy";
    [SerializeField] private string sitState = "Sit";

    [Header("One-shot Transition Animation State Names")]
    [SerializeField] private string happyStartState = "HappyStart";
    [SerializeField] private string standToWalkState = "StandWalk";
    [SerializeField] private string walkToStandState = "WalkStand";
    [SerializeField] private string standToSitState = "StandSit";
    [SerializeField] private string sitToStandState = "SitStand";

    [Header("Turn Animation State Names")]
    [SerializeField] private string turnFrontState = "TurnFront";
    [SerializeField] private string turnLoopState = "TurnLoop";

    [Header("Animation Settings")]
    [SerializeField] private float animationCrossFadeTime = 0.12f;
    [SerializeField] private float shortTransitionDuration = 0.45f;
    [SerializeField] private float longTransitionDuration = 0.75f;
    [SerializeField] private float minLocomotionAnimationSpeed = 0.6f;
    [SerializeField] private float maxLocomotionAnimationSpeed = 1.5f;

    [Header("Turn Settings")]
    [SerializeField] private float turnInPlaceAngleThreshold = 35f;
    [SerializeField] private float turnFrontDuration = 0.55f;

    [Header("Animation Stability")]
    [SerializeField] private float minAnimationHoldTime = 0.28f;
    [SerializeField] private float locomotionStartExtraDistance = 0.12f;
    [SerializeField] private float locomotionStopRatio = 0.55f;
    [SerializeField] private float nearTargetStandDelay = 0.25f;

    [Header("Expression Material Search")]
    [SerializeField] private string eyesMaterialKeyword = "eyes";
    [SerializeField] private string mouthMaterialKeyword = "mouth";

    [Header("Expression Switching")]
    [SerializeField] private float expressionChangeCooldown = 1.5f;
    [SerializeField] private float expressionFadeDuration = 0.18f;

    [System.Serializable]
    private class DogStateExpressionGroup
    {
        public NavigationRuntimeController.NavState state;

        // eyes[i] 和 mouths[i] 是设计好的一套表情组合，必须成对切换，
        // 不能各自独立随机挑选，否则会拼出"生气眼+开心嘴"这类不协调组合。
        public Texture[] eyes;
        public Texture[] mouths;

        [System.NonSerialized] public int lastPickedIndex = -1;
    }

    [Header("State Expression Groups")]
    [SerializeField] private DogStateExpressionGroup[] expressionGroups;

    [Header("Guide Position")]
    [SerializeField] private float leadDistance = 1.8f;
    [SerializeField] private float besideDistance = 0.75f;
    [SerializeField] private float behindDistance = 0.9f;
    [SerializeField] private float minDistanceToUser = 0.85f;
    [SerializeField] private float maxDistanceToUser = 2.2f;

    [Header("Movement Speeds")]
    [SerializeField] private float sniffMoveSpeed = 0.55f;
    [SerializeField] private float walkMoveSpeed = 0.85f;
    [SerializeField] private float trotMoveSpeed = 1.35f;
    [SerializeField] private float canterMoveSpeed = 1.85f;
    [SerializeField] private float turnMoveSpeed = 0.7f;
    [SerializeField] private float catchUpMoveSpeed = 0.9f;
    [SerializeField] private float rotateSpeed = 7.5f;

    [Header("Locomotion Animation Gate")]
    [SerializeField] private float minDistanceForWalkAnimation = 0.18f;
    [SerializeField] private float minDistanceForSniffAnimation = 0.16f;
    [SerializeField] private float minDistanceForTrotAnimation = 0.25f;
    [SerializeField] private float minDistanceForCanterAnimation = 0.35f;
    [SerializeField] private float minDistanceForTurnAnimation = 0.28f;

    [Header("Random Behaviors")]
    [SerializeField] private bool enableRandomBehaviors = true;
    [SerializeField] private float randomBehaviorMinInterval = 1.4f;
    [SerializeField] private float randomBehaviorMaxInterval = 3.2f;
    [SerializeField] private float randomBehaviorMinDuration = 1.4f;
    [SerializeField] private float randomBehaviorMaxDuration = 2.6f;
    [SerializeField] private float circleRadius = 1.1f;
    [SerializeField] private float circleAngularSpeed = 130f;

    [Header("Negative / Waiting State Behavior")]
    [SerializeField] private float barkDuration = 1.2f;
    [SerializeField] private float sitWaitDuration = 2.4f;
    [SerializeField] private float sitWaitRandomExtraDuration = 0.8f;
    [SerializeField] private float negativeReactionCooldown = 2.0f;

    [Header("Arrival")]
    [SerializeField] private float celebrationDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logAnimationChanges = false;

    [Header("Venue Walkability")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform venueContentRoot;
    [SerializeField] private bool constrainMovementToWalkableMap = true;
    [SerializeField] private int movementWalkabilitySamples = 12;
    [SerializeField] private float interactionStandOffRadius = 0.55f;
    [SerializeField] private float dogBodyRadius = 0.22f;
    [SerializeField] private float dogBodyHeight = 0.35f;
    [SerializeField] private LayerMask movementBlockerMask = ~0;

    private GameObject currentDog;
    private Transform xrCamera;
    private Animator dogAnimator;
    private AudioSource dogAudioSource;
    private float lastBarkTime = -999f;
    private int lastBarkClipIndex = -1;

    private readonly List<Transform> path = new List<Transform>();

    private bool isGuiding;
    private bool isPerformingBehavior;
    private bool currentBehaviorIsStateReaction;
    private bool positiveRandomBehaviorsSuppressed;
    private bool interactionHold;

    private readonly List<Vector3> interactionRoute = new List<Vector3>();
    private static readonly Collider[] MovementOverlapBuffer = new Collider[12];

    private void Awake()
    {
        ResolveVenueWalkabilityReferences();
    }

    public void ConfigureVenueWalkability(VenueMapDefinition definition, Transform contentRoot)
    {
        if (definition != null)
            mapDefinition = definition;

        if (contentRoot != null)
            venueContentRoot = contentRoot;
    }

    private NavigationRuntimeController.NavState currentState = NavigationRuntimeController.NavState.Neutral;
    private NavigationRuntimeController.NavState previousState = NavigationRuntimeController.NavState.Neutral;

    private Vector3 currentRecommendedDirection = Vector3.forward;
    private Vector3 guidanceTargetOverride;
    private bool hasGuidanceTargetOverride;
    private bool hasAppliedExpression;

    private Coroutine randomBehaviorRoutine;
    private Coroutine behaviorRoutine;

    private Renderer eyesRenderer;
    private Renderer mouthRenderer;
    private Material eyesRuntimeMaterial;
    private Material mouthRuntimeMaterial;
    private float lastExpressionChangeTime = -999f;
    private Coroutine eyesFadeRoutine;
    private Coroutine mouthFadeRoutine;

    private string currentAnimationState = "";
    private float lastAnimationChangeTime = -999f;
    private float nearTargetTimer = 0f;
    private float lastNegativeReactionTime = -999f;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Raised once after a dog instance finishes spawning/setup in BeginGuiding, so V2 systems (accessories, rewards) can hook in without coupling to internal guide state.</summary>
    public event System.Action<GameObject> DogSpawned;

    public GameObject CurrentDog => currentDog;

    /// <summary>Public passthrough to the existing animation crossfade path, for V2 callers (e.g. RewardRevealController) that need to trigger a one-shot state like "HappyStart" without duplicating CrossFade logic.</summary>
    public void PlayOneShotState(string stateName)
    {
        PlayAnimation(stateName, 1f, true);
    }

    /// <summary>Test/preview helper: forces the same arrival celebration flow (turn toward user, HappyStart, then Happy) used when the dog actually reaches the destination, without requiring real navigation distance. Stays on Happy afterward since it leaves currentState as Arrived.</summary>
    public void ForcePlayHappyPreview()
    {
        if (!isGuiding || currentDog == null)
            return;

        if (isPerformingBehavior)
            StopCurrentBehavior();

        StartBehavior(DogBehavior.Arrived, true);
    }

    public void BeginGuiding(List<Transform> runtimePath, Transform userCamera)
    {
        xrCamera = userCamera;
        path.Clear();

        foreach (Transform wp in runtimePath)
        {
            if (wp != null)
                path.Add(wp);
        }

        if (dogPrefab == null || xrCamera == null || path.Count < 2)
        {
            Debug.LogWarning("DogGuideController: missing dogPrefab / xrCamera / path.");
            return;
        }

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }

        if (randomBehaviorRoutine != null)
        {
            StopCoroutine(randomBehaviorRoutine);
            randomBehaviorRoutine = null;
        }

        bool createdDog = currentDog == null;
        if (createdDog)
        {
            Vector3 spawnPos = xrCamera.position + GetFlatForward(xrCamera) * 1.2f;
            spawnPos.y = path[0].position.y;

            currentDog = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
            dogAnimator = currentDog.GetComponentInChildren<Animator>();
        }
        else if (dogAnimator == null)
        {
            dogAnimator = currentDog.GetComponentInChildren<Animator>();
        }

        if (dogAnimator != null)
        {
            dogAnimator.applyRootMotion = false;
            dogAnimator.speed = 1f;
        }

        SetupAudioSource();
        SetupExpressionMaterials();
        DogSpawned?.Invoke(currentDog);

        currentState = NavigationRuntimeController.NavState.Neutral;
        previousState = NavigationRuntimeController.NavState.Neutral;

        isGuiding = true;
        isPerformingBehavior = false;
        currentBehaviorIsStateReaction = false;
        hasGuidanceTargetOverride = false;

        if (createdDog)
        {
            currentAnimationState = "";
            lastAnimationChangeTime = -999f;
        }

        nearTargetTimer = 0f;
        lastBarkTime = -999f;
        hasAppliedExpression = false;
        lastExpressionChangeTime = -999f;

        SetRandomExpressionForState(NavigationRuntimeController.NavState.Neutral);
        hasAppliedExpression = true;

        if (createdDog)
            PlayAnimation(standState, 1f, true);

        if (enableRandomBehaviors)
        {
            if (randomBehaviorRoutine != null)
                StopCoroutine(randomBehaviorRoutine);

            randomBehaviorRoutine = StartCoroutine(RandomBehaviorLoop());
        }
    }

    public void StopGuiding()
    {
        StopGuiding(true);
    }

    public void StopGuiding(bool destroyDog)
    {
        isGuiding = false;
        isPerformingBehavior = false;
        currentBehaviorIsStateReaction = false;
        hasAppliedExpression = false;

        if (randomBehaviorRoutine != null)
        {
            StopCoroutine(randomBehaviorRoutine);
            randomBehaviorRoutine = null;
        }

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }

        if (destroyDog && currentDog != null)
        {
            Destroy(currentDog);
            currentDog = null;
        }

        if (destroyDog)
        {
            dogAnimator = null;
            dogAudioSource = null;
            eyesRenderer = null;
            mouthRenderer = null;
            eyesRuntimeMaterial = null;
            mouthRuntimeMaterial = null;
            currentAnimationState = "";
        }

        path.Clear();
        xrCamera = null;
        hasGuidanceTargetOverride = false;
        nearTargetTimer = 0f;
    }

    public void SetGuidanceTargetOverride(Vector3 targetPosition, Vector3 recommendedDirection)
    {
        guidanceTargetOverride = targetPosition;
        hasGuidanceTargetOverride = true;

        currentRecommendedDirection = recommendedDirection;
        currentRecommendedDirection.y = 0f;
        if (currentRecommendedDirection.sqrMagnitude < 0.0001f && xrCamera != null)
            currentRecommendedDirection = GetFlatForward(xrCamera);
        currentRecommendedDirection.Normalize();
    }

    public void ClearGuidanceTargetOverride()
    {
        hasGuidanceTargetOverride = false;
    }

    public void SetPositiveRandomBehaviorsSuppressed(bool suppressed)
    {
        positiveRandomBehaviorsSuppressed = suppressed;

        if (suppressed && isPerformingBehavior && !currentBehaviorIsStateReaction)
            StopCurrentBehavior();
    }

    public void SetInteractionHold(bool hold, string holdAnimationState = null)
    {
        interactionHold = hold;
        SetPositiveRandomBehaviorsSuppressed(hold);

        if (!hold)
            return;

        if (isPerformingBehavior && !currentBehaviorIsStateReaction)
            StopCurrentBehavior();

        if (!string.IsNullOrWhiteSpace(holdAnimationState))
            PlayOneShotState(holdAnimationState);
    }

    public IEnumerator WalkToInteractionTarget(
        Vector3 targetPosition,
        float stopDistance = 0.22f,
        string arrivalAnimationState = null,
        float maxDuration = 12f)
    {
        if (currentDog == null)
            yield break;

        interactionHold = true;
        SetPositiveRandomBehaviorsSuppressed(true);

        if (isPerformingBehavior && !currentBehaviorIsStateReaction)
            StopCurrentBehavior();

        Vector3 lookTarget = targetPosition;
        lookTarget.y = currentDog.transform.position.y;

        float safeStopDistance = Mathf.Max(0.05f, stopDistance);
        Vector3 approachTarget = ComputeInteractionApproachPoint(lookTarget, safeStopDistance);
        approachTarget.y = currentDog.transform.position.y;

        BuildInteractionRoute(approachTarget);
        int routeIndex = interactionRoute.Count > 1 ? 1 : 0;

        float closeEnoughDistance = safeStopDistance + minDistanceForWalkAnimation + locomotionStartExtraDistance;
        float elapsed = 0f;
        int stuckFrames = 0;
        Vector3 lastPosition = currentDog.transform.position;
        const float stuckMoveThreshold = 0.01f;
        const int stuckFrameLimit = 24;

        while (currentDog != null)
        {
            Vector3 segmentTarget = interactionRoute[routeIndex];
            segmentTarget.y = currentDog.transform.position.y;

            float distance = GetFlatDistance(currentDog.transform.position, segmentTarget);

            if (distance <= safeStopDistance)
            {
                if (routeIndex < interactionRoute.Count - 1)
                {
                    routeIndex++;
                    stuckFrames = 0;
                    lastPosition = currentDog.transform.position;
                    continue;
                }

                break;
            }

            if (elapsed >= maxDuration || (routeIndex >= interactionRoute.Count - 1 && distance <= closeEnoughDistance))
                break;

            string moveState = distance > 0.75f ? trotState : walkState;
            float moveSpeed = distance > 0.75f ? trotMoveSpeed : walkMoveSpeed;
            float animationGate = distance > 0.75f ? minDistanceForTrotAnimation : minDistanceForWalkAnimation;

            TryPlayLocomotionAndMove(segmentTarget, moveState, moveSpeed, animationGate);

            float moved = GetFlatDistance(lastPosition, currentDog.transform.position);
            if (moved < stuckMoveThreshold)
                stuckFrames++;
            else
                stuckFrames = 0;

            if (stuckFrames >= stuckFrameLimit)
            {
                if (routeIndex < interactionRoute.Count - 1)
                {
                    routeIndex++;
                    stuckFrames = 0;
                    lastPosition = currentDog.transform.position;
                    continue;
                }

                break;
            }

            lastPosition = currentDog.transform.position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentDog != null)
        {
            Vector3 lookDirection = lookTarget - currentDog.transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.0001f)
                currentDog.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            if (!string.IsNullOrWhiteSpace(arrivalAnimationState))
                PlayOneShotState(arrivalAnimationState);
            else
                PlayStand(1f);
        }
    }

    public void TickFreeRoamFollow(Vector3 targetPosition, Vector3 recommendedDirection)
    {
        if (!isGuiding || currentDog == null || xrCamera == null)
            return;

        if (interactionHold)
            return;

        if (isPerformingBehavior)
            StopCurrentBehavior();

        currentState = NavigationRuntimeController.NavState.Neutral;
        SetGuidanceTargetOverride(targetPosition, recommendedDirection);

        Vector3 targetPos = targetPosition;
        targetPos.y = currentDog.transform.position.y;

        float distanceToTarget = GetFlatDistance(currentDog.transform.position, targetPos);
        bool dogIsBehindUser = IsDogBehindUser(GetRecommendedDirection());

        if (dogIsBehindUser || distanceToTarget > 1.35f)
        {
            TryPlayLocomotionAndMove(
                targetPos,
                canterState,
                catchUpMoveSpeed,
                minDistanceForCanterAnimation);
            return;
        }

        if (distanceToTarget > 0.65f)
        {
            TryPlayLocomotionAndMove(
                targetPos,
                trotState,
                trotMoveSpeed,
                minDistanceForTrotAnimation);
            return;
        }

        if (distanceToTarget > 0.25f)
        {
            TryPlayLocomotionAndMove(
                targetPos,
                walkState,
                walkMoveSpeed,
                minDistanceForWalkAnimation);
            return;
        }

        PlayStand(1f);
    }

    public void ApplyNavigationState(
        NavigationRuntimeController.NavState navState,
        float distanceToGoal,
        Vector3 recommendedDirection
    )
    {
        if (interactionHold)
            return;

        NavigationRuntimeController.NavState effectiveState = navState;

        if (distanceToGoal <= celebrationDistance &&
            navState != NavigationRuntimeController.NavState.Arrived)
        {
            effectiveState = NavigationRuntimeController.NavState.Arrived;
        }

        previousState = currentState;
        bool stateChanged = currentState != effectiveState;
        currentState = effectiveState;

        currentRecommendedDirection = recommendedDirection;
        currentRecommendedDirection.y = 0f;

        if (currentRecommendedDirection.sqrMagnitude < 0.0001f && xrCamera != null)
            currentRecommendedDirection = GetFlatForward(xrCamera);

        currentRecommendedDirection.Normalize();

        if (!hasAppliedExpression || stateChanged)
        {
            SetRandomExpressionForState(currentState);
            hasAppliedExpression = true;
        }

        if (currentState == NavigationRuntimeController.NavState.Arrived)
        {
            StartBehavior(DogBehavior.Arrived, true);
            return;
        }

        if ((currentState == NavigationRuntimeController.NavState.GettingCloser ||
             currentState == NavigationRuntimeController.NavState.Neutral) &&
            isPerformingBehavior &&
            currentBehaviorIsStateReaction)
        {
            StopCurrentBehavior();
            StartBehavior(DogBehavior.ShortCanter, false);
            return;
        }

        if (isPerformingBehavior)
            return;

        switch (currentState)
        {
            case NavigationRuntimeController.NavState.Waiting:
                TryStartWaitingReaction();
                break;

            case NavigationRuntimeController.NavState.GettingFarther:
                TryStartNegativeReaction(false);
                break;

            case NavigationRuntimeController.NavState.Lost:
                TryStartNegativeReaction(true);
                break;

            case NavigationRuntimeController.NavState.GettingCloser:
            case NavigationRuntimeController.NavState.Neutral:
            default:
                // Do not play locomotion here. DoNormalGuideMovement decides it based on real distance.
                break;
        }
    }

    private void Update()
    {
        if (!isGuiding || currentDog == null || xrCamera == null)
            return;

        if (interactionHold)
            return;

        if (isPerformingBehavior)
            return;

        if (currentState == NavigationRuntimeController.NavState.Arrived)
            return;

        if (positiveRandomBehaviorsSuppressed && hasGuidanceTargetOverride)
            return;

        if (currentState == NavigationRuntimeController.NavState.Lost ||
            currentState == NavigationRuntimeController.NavState.GettingFarther ||
            currentState == NavigationRuntimeController.NavState.Waiting)
        {
            // Important: no free body rotation here.
            // If the dog needs to turn while static, it must do it through TurnFront only.
            // TurnBack is intentionally not used anywhere in this script.
            return;
        }

        DoNormalGuideMovement();
    }

    private void DoNormalGuideMovement()
    {
        Vector3 userPos = xrCamera.position;
        Vector3 flatDir = GetRecommendedDirection();

        Vector3 leadTargetPos = hasGuidanceTargetOverride
            ? guidanceTargetOverride
            : userPos + flatDir * leadDistance;
        leadTargetPos.y = currentDog.transform.position.y;

        float userDogDistance = GetFlatDistance(userPos, currentDog.transform.position);
        float distanceToLeadTarget = GetFlatDistance(currentDog.transform.position, leadTargetPos);

        bool dogIsBehindUser = IsDogBehindUser(flatDir);

        // 狗已经明显落后：直接跑，不要走。
        if (dogIsBehindUser || userDogDistance > maxDistanceToUser || distanceToLeadTarget > 1.35f)
        {
            TryPlayLocomotionAndMove(
                leadTargetPos,
                canterState,
                catchUpMoveSpeed,
                minDistanceForCanterAnimation
            );

            return;
        }

        // 狗离引导位置还有一段距离：默认小跑。
        if (distanceToLeadTarget > 0.65f)
        {
            TryPlayLocomotionAndMove(
                leadTargetPos,
                trotState,
                trotMoveSpeed,
                minDistanceForTrotAnimation
            );

            return;
        }

        // 狗快到引导位置了：才慢走。
        if (distanceToLeadTarget > 0.25f)
        {
            TryPlayLocomotionAndMove(
                leadTargetPos,
                walkState,
                walkMoveSpeed,
                minDistanceForWalkAnimation
            );

            return;
        }

        // 狗已经在合适位置附近：不要频繁切 Stand，保持自然的小动作。
        if (Random.value < 0.015f)
        {
            TryPlayLocomotionAndMove(
                leadTargetPos,
                sniffState,
                sniffMoveSpeed,
                minDistanceForSniffAnimation
            );
        }
        else
        {
            PlayAnimation(trotState, 0.85f);
        }
    }

    private IEnumerator RandomBehaviorLoop()
    {
        while (isGuiding)
        {
            float waitTime = Random.Range(randomBehaviorMinInterval, randomBehaviorMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!CanDoPositiveRandomBehavior())
                continue;

            DogBehavior behavior = PickPositiveBehavior();
            yield return StartCoroutine(PerformBehavior(behavior, false));
        }
    }

    private bool CanDoPositiveRandomBehavior()
    {
        if (!isGuiding || currentDog == null || xrCamera == null)
            return false;

        if (positiveRandomBehaviorsSuppressed)
            return false;

        if (isPerformingBehavior)
            return false;

        return currentState == NavigationRuntimeController.NavState.Neutral ||
               currentState == NavigationRuntimeController.NavState.GettingCloser;
    }

    private DogBehavior PickPositiveBehavior()
    {
        float r = Random.value;

        if (r < 0.42f) return DogBehavior.ShortCanter;

        if (r < 0.62f) return DogBehavior.SniffAhead;

        if (r < 0.82f) return DogBehavior.WalkBeside;

        if (r < 0.92f) return DogBehavior.PauseAndLook;

        return DogBehavior.TurnCircleUser;
    }

    private void TryStartWaitingReaction()
    {
        if (Time.time - lastNegativeReactionTime < negativeReactionCooldown)
        {
            // During Waiting, prefer staying seated instead of falling back to Stand.
            PlayAnimation(sitState, 1f);
            return;
        }

        lastNegativeReactionTime = Time.time;
        StartBehavior(DogBehavior.SitWait, true);
    }

    private void TryStartNegativeReaction(bool strong)
    {
        if (Time.time - lastNegativeReactionTime < negativeReactionCooldown)
        {
            PlayStand(1f);
            return;
        }

        lastNegativeReactionTime = Time.time;

        if (strong)
        {
            // Lost feels more serious, but still lets the dog sit more often than before.
            StartBehavior(Random.value < 0.45f ? DogBehavior.BarkAtUser : DogBehavior.SitWait, true);
        }
        else
        {
            float r = Random.value;

            if (r < 0.45f)
                StartBehavior(DogBehavior.BarkAtUser, true);
            else if (r < 0.80f)
                StartBehavior(DogBehavior.SitWait, true);
            else
                StartBehavior(DogBehavior.PauseAndLook, true);
        }
    }

    private void StartBehavior(DogBehavior behavior, bool isStateReaction)
    {
        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        behaviorRoutine = StartCoroutine(PerformBehavior(behavior, isStateReaction));
    }

    private void StopCurrentBehavior()
    {
        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }

        isPerformingBehavior = false;
        currentBehaviorIsStateReaction = false;
    }

    private IEnumerator PerformBehavior(DogBehavior behavior, bool isStateReaction)
    {
        isPerformingBehavior = true;
        currentBehaviorIsStateReaction = isStateReaction;
        nearTargetTimer = 0f;

        switch (behavior)
        {
            case DogBehavior.SniffAhead:
                yield return StartCoroutine(DoSniffAhead());
                break;

            case DogBehavior.ShortCanter:
                yield return StartCoroutine(DoShortCanter());
                break;

            case DogBehavior.TurnCircleUser:
                yield return StartCoroutine(DoTurnCircleUser());
                break;

            case DogBehavior.WalkBeside:
                yield return StartCoroutine(DoWalkBeside());
                break;

            case DogBehavior.WalkBehind:
                yield return StartCoroutine(DoWalkBehind());
                break;

            case DogBehavior.PauseAndLook:
                yield return StartCoroutine(DoPauseAndLook());
                break;

            case DogBehavior.BarkAtUser:
                yield return StartCoroutine(DoBarkAtUser());
                break;

            case DogBehavior.SitWait:
                yield return StartCoroutine(DoSitWait());
                break;

            case DogBehavior.Arrived:
                yield return StartCoroutine(DoArrived());
                break;
        }

        isPerformingBehavior = false;
        currentBehaviorIsStateReaction = false;
        behaviorRoutine = null;
        nearTargetTimer = 0f;

        if (currentState != NavigationRuntimeController.NavState.Arrived)
        {
            ApplyNavigationState(currentState, 999f, currentRecommendedDirection);
        }
    }

    private IEnumerator DoSniffAhead()
    {
        float duration = Random.Range(2f, 5f);
        float elapsed = 0f;

        while (elapsed < duration && CanContinuePositiveMovement())
        {
            elapsed += Time.deltaTime;

            Vector3 targetPos = xrCamera.position + GetRecommendedDirection() * (leadDistance * 0.78f);
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                sniffState,
                sniffMoveSpeed,
                minDistanceForSniffAnimation
            );

            yield return null;
        }
    }

    private IEnumerator DoShortCanter()
    {
        yield return PlayTransitionOnly(standToWalkState, shortTransitionDuration);

        float duration = Random.Range(1.0f, 5.0f);
        float elapsed = 0f;

        while (elapsed < duration && CanContinuePositiveMovement())
        {
            elapsed += Time.deltaTime;

            Vector3 targetPos = xrCamera.position + GetRecommendedDirection() * (leadDistance + 0.65f);            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                canterState,
                canterMoveSpeed,
                minDistanceForCanterAnimation
            );

            yield return null;
        }
    }

    private IEnumerator DoTurnCircleUser()
    {
        float duration = Random.Range(1.2f, 2.0f);
        float elapsed = 0f;
        float angle = Random.Range(0f, 360f);
        float direction = Random.value < 0.5f ? -1f : 1f;

        while (elapsed < duration && CanContinuePositiveMovement())
        {
            elapsed += Time.deltaTime;
            angle += direction * circleAngularSpeed * Time.deltaTime;

            float radians = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(radians) * circleRadius,
                0f,
                Mathf.Sin(radians) * circleRadius
            );

            Vector3 targetPos = xrCamera.position + offset;
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                turnLoopState,
                turnMoveSpeed,
                minDistanceForTurnAnimation
            );

            yield return null;
        }
    }

    private IEnumerator DoWalkBeside()
    {
        float duration = Random.Range(randomBehaviorMinDuration, randomBehaviorMaxDuration);
        float elapsed = 0f;
        float sideSign = Random.value < 0.5f ? -1f : 1f;

        while (elapsed < duration && CanContinuePositiveMovement())
        {
            elapsed += Time.deltaTime;

            Vector3 dir = GetRecommendedDirection();
            Vector3 sideDir = new Vector3(dir.z, 0f, -dir.x) * sideSign;

            Vector3 targetPos = xrCamera.position + sideDir * besideDistance + dir * 0.15f;
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                walkState,
                walkMoveSpeed,
                minDistanceForWalkAnimation
            );

            yield return null;
        }
    }

    private IEnumerator DoWalkBehind()
    {
        float duration = Random.Range(randomBehaviorMinDuration, randomBehaviorMaxDuration);
        float elapsed = 0f;

        while (elapsed < duration && CanContinuePositiveMovement())
        {
            elapsed += Time.deltaTime;

            Vector3 dir = GetRecommendedDirection();

            Vector3 targetPos = xrCamera.position - dir * behindDistance;
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                walkState,
                walkMoveSpeed,
                minDistanceForWalkAnimation
            );

            yield return null;
        }
    }

    private IEnumerator DoPauseAndLook()
    {
        yield return PlayTransitionOnly(walkToStandState, shortTransitionDuration);
        yield return TurnTowardUserIfNeeded();

        PlayAnimation(standState, 1f, true);

        float duration = Random.Range(0.45f, 0.9f);
        float elapsed = 0f;

        while (elapsed < duration && currentState != NavigationRuntimeController.NavState.Arrived)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return PlayTransitionOnly(standToWalkState, shortTransitionDuration);
    }

    private IEnumerator DoBarkAtUser()
    {
        yield return TurnTowardUserIfNeeded();

        if (!string.IsNullOrWhiteSpace(barkState))
            PlayAnimation(barkState, 1f, true);
        else
            PlayAnimation(standState, 1f, true);

        PlayBarkSound();

        float elapsed = 0f;
        float barkTimer = 0f;

        while (elapsed < barkDuration)
        {
            elapsed += Time.deltaTime;
            barkTimer += Time.deltaTime;

            if (barkTimer >= barkRepeatInterval)
            {
                barkTimer = 0f;

                if (Random.value <= barkRepeatChance)
                    PlayBarkSound();
            }

            yield return null;
        }
    }

    private IEnumerator DoSitWait()
    {
        yield return TurnTowardUserIfNeeded();
        yield return PlayTransitionOnly(standToSitState, longTransitionDuration);

        PlayAnimation(sitState, 1f, true);

        float elapsed = 0f;
        float duration = sitWaitDuration + Random.Range(0f, sitWaitRandomExtraDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (currentState == NavigationRuntimeController.NavState.GettingCloser ||
                currentState == NavigationRuntimeController.NavState.Neutral)
            {
                break;
            }

            yield return null;
        }

        yield return PlayTransitionOnly(sitToStandState, longTransitionDuration);
    }

    private IEnumerator DoArrived()
    {
        currentState = NavigationRuntimeController.NavState.Arrived;

        yield return TurnTowardUserIfNeeded();
        yield return PlayTransitionOnly(happyStartState, shortTransitionDuration);

        PlayAnimation(happyState, 1f, true);

        float elapsed = 0f;
        float duration = 3.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator PlayTransitionOnly(string stateName, float duration)
    {
        if (!string.IsNullOrWhiteSpace(stateName))
        {
            PlayAnimation(stateName, 1f, true);
            yield return new WaitForSeconds(duration);
        }
    }

    private IEnumerator TurnTowardUserIfNeeded()
    {
        if (currentDog == null || xrCamera == null)
            yield break;

        Vector3 targetDir = xrCamera.position - currentDog.transform.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.0001f)
            yield break;

        targetDir.Normalize();

        Vector3 currentForward = currentDog.transform.forward;
        currentForward.y = 0f;

        if (currentForward.sqrMagnitude < 0.0001f)
            currentForward = Vector3.forward;

        currentForward.Normalize();

        float angle = Vector3.Angle(currentForward, targetDir);

        if (angle < turnInPlaceAngleThreshold)
            yield break;

        // TurnBack is completely disabled. Even for large angles, use TurnFront only.
        string turnState = turnFrontState;
        float duration = turnFrontDuration;

        if (string.IsNullOrWhiteSpace(turnState))
            yield break;

        Quaternion startRot = currentDog.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(targetDir, Vector3.up);

        PlayAnimation(turnState, 1f, true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            currentDog.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        currentDog.transform.rotation = targetRot;
    }

    private bool CanContinuePositiveMovement()
    {
        return isGuiding &&
               currentDog != null &&
               xrCamera != null &&
               currentState != NavigationRuntimeController.NavState.Arrived &&
               currentState != NavigationRuntimeController.NavState.Lost &&
               currentState != NavigationRuntimeController.NavState.GettingFarther &&
               currentState != NavigationRuntimeController.NavState.Waiting;
    }

    private void SetupAudioSource()
    {
        dogAudioSource = null;

        if (currentDog == null)
            return;

        dogAudioSource = currentDog.GetComponent<AudioSource>();

        if (dogAudioSource == null)
            dogAudioSource = currentDog.AddComponent<AudioSource>();

        dogAudioSource.playOnAwake = false;
        dogAudioSource.loop = false;
        dogAudioSource.spatialBlend = 1f;
        dogAudioSource.volume = barkVolume;
        dogAudioSource.minDistance = 1f;
        dogAudioSource.maxDistance = 8f;
        dogAudioSource.dopplerLevel = 0f;
    }

    private void PlayBarkSound()
    {
        if (dogAudioSource == null)
            return;

        AudioClip selectedClip = PickRandomBarkClip();

        if (selectedClip == null)
            return;

        if (Time.time - lastBarkTime < barkMinInterval)
            return;

        lastBarkTime = Time.time;
        dogAudioSource.PlayOneShot(selectedClip, barkVolume);
    }

    private AudioClip PickRandomBarkClip()
    {
        if (barkClips != null && barkClips.Length > 0)
        {
            int validCount = 0;

            for (int i = 0; i < barkClips.Length; i++)
            {
                if (barkClips[i] != null)
                    validCount++;
            }

            if (validCount > 0)
            {
                int selectedIndex = -1;

                if (validCount == 1)
                {
                    for (int i = 0; i < barkClips.Length; i++)
                    {
                        if (barkClips[i] != null)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        int candidateIndex = Random.Range(0, barkClips.Length);

                        if (barkClips[candidateIndex] == null)
                            continue;

                        if (avoidRepeatingSameBarkClip && candidateIndex == lastBarkClipIndex)
                            continue;

                        selectedIndex = candidateIndex;
                        break;
                    }

                    if (selectedIndex < 0)
                    {
                        for (int i = 0; i < barkClips.Length; i++)
                        {
                            if (barkClips[i] != null && i != lastBarkClipIndex)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }
                }

                if (selectedIndex >= 0)
                {
                    lastBarkClipIndex = selectedIndex;
                    return barkClips[selectedIndex];
                }
            }
        }

        return barkClip;
    }

    private void SetupExpressionMaterials()
    {
        eyesRenderer = null;
        mouthRenderer = null;
        eyesRuntimeMaterial = null;
        mouthRuntimeMaterial = null;

        if (eyesFadeRoutine != null)
        {
            StopCoroutine(eyesFadeRoutine);
            eyesFadeRoutine = null;
        }

        if (mouthFadeRoutine != null)
        {
            StopCoroutine(mouthFadeRoutine);
            mouthFadeRoutine = null;
        }

        if (currentDog == null)
            return;

        Renderer[] renderers = currentDog.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material sharedMat = sharedMaterials[i];

                if (sharedMat == null)
                    continue;

                string matName = sharedMat.name.ToLower();

                if (eyesRuntimeMaterial == null &&
                    matName.Contains(eyesMaterialKeyword.ToLower()))
                {
                    eyesRenderer = renderer;
                    Material[] runtimeMaterials = renderer.materials;
                    eyesRuntimeMaterial = runtimeMaterials[i];
                }

                if (mouthRuntimeMaterial == null &&
                    matName.Contains(mouthMaterialKeyword.ToLower()))
                {
                    mouthRenderer = renderer;
                    Material[] runtimeMaterials = renderer.materials;
                    mouthRuntimeMaterial = runtimeMaterials[i];
                }
            }
        }

        if (eyesRuntimeMaterial == null)
            Debug.LogWarning("DogGuideController: eyes material not found.");

        if (mouthRuntimeMaterial == null)
            Debug.LogWarning("DogGuideController: mouth material not found.");
    }

    private void SetRandomExpressionForState(NavigationRuntimeController.NavState state)
    {
        DogStateExpressionGroup group = FindExpressionGroup(state);

        if (group == null)
        {
            Debug.LogWarning($"DogGuideController: no expression group configured for state {state}.");
            return;
        }

        int optionCount = Mathf.Min(
            group.eyes != null ? group.eyes.Length : 0,
            group.mouths != null ? group.mouths.Length : 0);

        if (optionCount <= 0)
        {
            Debug.LogWarning($"DogGuideController: expression group for state {state} has no matching eyes/mouth pair.");
            return;
        }

        // 防抖：状态在短时间内反复横跳时，不要跟着一起闪烁表情。
        if (Time.time - lastExpressionChangeTime < expressionChangeCooldown)
            return;

        int index = PickExpressionIndex(group, optionCount);

        group.lastPickedIndex = index;
        lastExpressionChangeTime = Time.time;

        CrossFadeExpressionTexture(eyesRuntimeMaterial, group.eyes[index], ref eyesFadeRoutine);
        CrossFadeExpressionTexture(mouthRuntimeMaterial, group.mouths[index], ref mouthFadeRoutine);
    }

    private int PickExpressionIndex(DogStateExpressionGroup group, int optionCount)
    {
        if (optionCount <= 1)
            return 0;

        int index;

        do
        {
            index = Random.Range(0, optionCount);
        } while (index == group.lastPickedIndex);

        return index;
    }

    private DogStateExpressionGroup FindExpressionGroup(NavigationRuntimeController.NavState state)
    {
        if (expressionGroups == null)
            return null;

        foreach (DogStateExpressionGroup group in expressionGroups)
        {
            if (group != null && group.state == state)
                return group;
        }

        return null;
    }

    private void CrossFadeExpressionTexture(Material material, Texture texture, ref Coroutine routine)
    {
        if (material == null || texture == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeSwapExpressionTexture(material, texture));
    }

    private IEnumerator FadeSwapExpressionTexture(Material material, Texture texture)
    {
        yield return FadeMaterialAlpha(material, 0f);
        SetMaterialBaseMap(material, texture);
        yield return FadeMaterialAlpha(material, 1f);
    }

    private IEnumerator FadeMaterialAlpha(Material material, float targetAlpha)
    {
        float halfDuration = expressionFadeDuration * 0.5f;

        if (halfDuration <= 0f)
        {
            SetMaterialAlpha(material, targetAlpha);
            yield break;
        }

        float startAlpha = GetMaterialAlpha(material);
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            SetMaterialAlpha(material, Mathf.Lerp(startAlpha, targetAlpha, elapsed / halfDuration));
            yield return null;
        }

        SetMaterialAlpha(material, targetAlpha);
    }

    private float GetMaterialAlpha(Material material)
    {
        if (material.HasProperty(BaseColorId))
            return material.GetColor(BaseColorId).a;

        if (material.HasProperty(ColorId))
            return material.GetColor(ColorId).a;

        return 1f;
    }

    private void SetMaterialAlpha(Material material, float alpha)
    {
        if (material.HasProperty(BaseColorId))
        {
            Color color = material.GetColor(BaseColorId);
            color.a = alpha;
            material.SetColor(BaseColorId, color);
        }
        else if (material.HasProperty(ColorId))
        {
            Color color = material.GetColor(ColorId);
            color.a = alpha;
            material.SetColor(ColorId, color);
        }
    }

    private void SetMaterialBaseMap(Material material, Texture texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty(BaseMapId))
            material.SetTexture(BaseMapId, texture);
        else if (material.HasProperty(MainTexId))
            material.SetTexture(MainTexId, texture);
        else
            material.mainTexture = texture;
    }

    private void PlayStand(float speed)
    {
        PlayAnimation(standState, speed);
    }

    private void PlayAnimation(string stateName, float speed)
    {
        PlayAnimation(stateName, speed, false);
    }

    private void PlayAnimation(string stateName, float speed, bool force)
    {
        if (dogAnimator == null)
            return;

        if (string.IsNullOrWhiteSpace(stateName))
            return;

        dogAnimator.speed = speed;

        if (currentAnimationState == stateName)
            return;

        if (!force && Time.time - lastAnimationChangeTime < minAnimationHoldTime)
            return;

        currentAnimationState = stateName;
        lastAnimationChangeTime = Time.time;

        dogAnimator.CrossFadeInFixedTime(stateName, animationCrossFadeTime);

        if (logAnimationChanges)
            Debug.Log("Dog animation changed to: " + stateName);
    }

    private bool TryPlayLocomotionAndMove(
        Vector3 targetPos,
        string locomotionState,
        float moveSpeed,
        float minDistanceForAnimation
    )
    {
        if (currentDog == null)
            return false;

        targetPos.y = currentDog.transform.position.y;

        float distanceToTarget = GetFlatDistance(
            currentDog.transform.position,
            targetPos
        );

        float animationSpeed = GetLocomotionAnimationSpeed(locomotionState, moveSpeed);

        bool currentlyUsingThisLocomotion = currentAnimationState == locomotionState;

        float startMoveDistance = minDistanceForAnimation + locomotionStartExtraDistance;
        float stopMoveDistance = minDistanceForAnimation * locomotionStopRatio;

        if (currentlyUsingThisLocomotion)
        {
            if (distanceToTarget <= stopMoveDistance)
            {
                nearTargetTimer += Time.deltaTime;

                if (nearTargetTimer >= nearTargetStandDelay)
                {
                    PlayStand(1f);
                    nearTargetTimer = 0f;
                    return false;
                }

                // 还没决定切回 Stand 之前，动画仍在播放，位移也要跟着走完最后这一小段，
                // 避免出现"腿在动、狗没动"的原地打滑。
                MoveDogTo(targetPos, moveSpeed);
                return true;
            }

            nearTargetTimer = 0f;
            PlayAnimation(locomotionState, animationSpeed);
            MoveDogTo(targetPos, moveSpeed);
            return true;
        }

        if (distanceToTarget < startMoveDistance)
        {
            nearTargetTimer += Time.deltaTime;

            if (nearTargetTimer >= nearTargetStandDelay)
            {
                PlayStand(1f);
                nearTargetTimer = 0f;
            }

            return false;
        }

        nearTargetTimer = 0f;
        PlayAnimation(locomotionState, animationSpeed);
        MoveDogTo(targetPos, moveSpeed);
        return true;
    }

    // 让动画播放节奏跟着实际移动速度按比例变化，避免"移动很快但腿部动画节奏没跟上"造成的打滑感。
    // referenceSpeed 是该步态在被 moveSpeed 常量本身驱动时(比例=1)所对应的基准移动速度。
    private float GetLocomotionAnimationSpeed(string locomotionState, float moveSpeed)
    {
        float referenceSpeed = walkMoveSpeed;

        if (locomotionState == trotState)
            referenceSpeed = trotMoveSpeed;
        else if (locomotionState == canterState)
            referenceSpeed = canterMoveSpeed;
        else if (locomotionState == sniffState)
            referenceSpeed = sniffMoveSpeed;
        else if (locomotionState == turnLoopState || locomotionState == turnFrontState)
            referenceSpeed = turnMoveSpeed;

        if (referenceSpeed <= 0f)
            return 1f;

        return Mathf.Clamp(
            moveSpeed / referenceSpeed,
            minLocomotionAnimationSpeed,
            maxLocomotionAnimationSpeed);
    }

    private void MoveDogTo(Vector3 targetPos, float speed)
    {
        if (currentDog == null)
            return;

        Vector3 currentPos = currentDog.transform.position;
        Vector3 desiredPos = Vector3.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);

        if (!TryApplyConstrainedMove(currentPos, desiredPos, out Vector3 appliedPos))
            appliedPos = currentPos;

        Vector3 moveDir = targetPos - currentPos;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);

            currentDog.transform.rotation = Quaternion.Slerp(
                currentDog.transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }

        currentDog.transform.position = appliedPos;
    }

    private bool IsDogBehindUser(Vector3 routeDirection)
    {
        if (currentDog == null || xrCamera == null)
            return false;

        routeDirection.y = 0f;

        if (routeDirection.sqrMagnitude < 0.0001f)
            return false;

        routeDirection.Normalize();

        Vector3 userToDog = currentDog.transform.position - xrCamera.position;
        userToDog.y = 0f;

        if (userToDog.sqrMagnitude < 0.0001f)
            return false;

        userToDog.Normalize();

        float dot = Vector3.Dot(routeDirection, userToDog);

        // dot < 0 表示狗在用户行进方向的反方向，也就是落在用户后面。
        return dot < -0.15f;
    }

    private Vector3 GetRecommendedDirection()
    {
        Vector3 flatDir = currentRecommendedDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f && xrCamera != null)
            flatDir = GetFlatForward(xrCamera);

        return flatDir.normalized;
    }

    private Vector3 GetFlatForward(Transform t)
    {
        Vector3 forward = t.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void ResolveVenueWalkabilityReferences()
    {
        if (venueContentRoot == null)
        {
            VenueAlignmentManager alignmentManager = FindFirstObjectByType<VenueAlignmentManager>();
            if (alignmentManager != null)
                venueContentRoot = alignmentManager.VenueContentRoot;
        }

        if (mapDefinition == null)
        {
            VenueNavigationRuntime navigationRuntime = FindFirstObjectByType<VenueNavigationRuntime>();
            if (navigationRuntime != null)
                mapDefinition = navigationRuntime.MapDefinition;
        }
    }

    private Vector3 ComputeInteractionApproachPoint(Vector3 targetPosition, float stopDistance)
    {
        if (currentDog == null)
            return targetPosition;

        float standOff = Mathf.Max(stopDistance, interactionStandOffRadius);
        Vector3 toDog = currentDog.transform.position - targetPosition;
        toDog.y = 0f;

        if (toDog.sqrMagnitude <= standOff * standOff)
            return currentDog.transform.position;

        return targetPosition + toDog.normalized * standOff;
    }

    private void BuildInteractionRoute(Vector3 targetWorld)
    {
        interactionRoute.Clear();

        if (currentDog == null)
        {
            interactionRoute.Add(targetWorld);
            return;
        }

        interactionRoute.Add(currentDog.transform.position);

        if (!TryBuildMapPath(currentDog.transform.position, targetWorld, out List<Vector3> pathPoints) || pathPoints.Count < 2)
        {
            interactionRoute.Add(targetWorld);
            return;
        }

        for (int i = 1; i < pathPoints.Count; i++)
            interactionRoute.Add(pathPoints[i]);
    }

    private bool TryBuildMapPath(Vector3 startWorld, Vector3 endWorld, out List<Vector3> worldPath)
    {
        worldPath = new List<Vector3>();

        if (mapDefinition == null || !mapDefinition.IsScaleReady())
            return false;

        Vector2 startPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(startWorld));
        Vector2 endPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(endWorld));

        List<Vector3> venueLocalPath = new List<Vector3>();
        if (!VenuePathfinder.TryFindWorldPath(mapDefinition, startPixel, endPixel, venueLocalPath))
            return false;

        for (int i = 0; i < venueLocalPath.Count; i++)
            worldPath.Add(VenueLocalToWorld(venueLocalPath[i]));

        return worldPath.Count >= 2;
    }

    private bool TryApplyConstrainedMove(Vector3 from, Vector3 to, out Vector3 appliedPosition)
    {
        appliedPosition = to;

        if (IsMovePositionAllowed(from, to))
            return true;

        Vector3 xSlide = new Vector3(to.x, from.y, from.z);
        if (IsMovePositionAllowed(from, xSlide))
        {
            appliedPosition = xSlide;
            return true;
        }

        Vector3 zSlide = new Vector3(from.x, from.y, to.z);
        if (IsMovePositionAllowed(from, zSlide))
        {
            appliedPosition = zSlide;
            return true;
        }

        Vector3 shortened = to;
        for (int i = 0; i < 5; i++)
        {
            shortened = Vector3.Lerp(from, shortened, 0.5f);
            if (IsMovePositionAllowed(from, shortened))
            {
                appliedPosition = shortened;
                return true;
            }
        }

        appliedPosition = from;
        return false;
    }

    private bool IsMovePositionAllowed(Vector3 from, Vector3 to)
    {
        if (GetFlatDistance(from, to) <= 0.0001f)
            return true;

        if (WouldIntersectCollectible(to))
            return false;

        if (IsPhysicsMoveBlocked(from, to, out float allowedDistance))
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            if (allowedDistance + 0.001f < delta.magnitude)
                return false;
        }

        if (constrainMovementToWalkableMap && mapDefinition != null && mapDefinition.IsScaleReady())
        {
            Vector2 startPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(from));
            Vector2 endPixel = mapDefinition.WorldToMapPixel(WorldToVenueLocal(to));
            if (!mapDefinition.IsMapSegmentWalkable(startPixel, endPixel, movementWalkabilitySamples))
                return false;
        }

        return true;
    }

    private bool WouldIntersectCollectible(Vector3 worldPosition)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            worldPosition + Vector3.up * dogBodyHeight * 0.5f,
            dogBodyRadius,
            MovementOverlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = MovementOverlapBuffer[i];
            if (hitCollider == null)
                continue;

            if (currentDog != null && hitCollider.transform.IsChildOf(currentDog.transform))
                continue;

            if (hitCollider.GetComponentInParent<CollectibleGrabHandler>() != null)
                return true;
        }

        return false;
    }

    private bool IsPhysicsMoveBlocked(Vector3 from, Vector3 to, out float allowedDistance)
    {
        allowedDistance = GetFlatDistance(from, to);

        Vector3 delta = to - from;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
            return false;

        Vector3 origin = from + Vector3.up * dogBodyHeight * 0.5f;
        if (Physics.SphereCast(
                origin,
                dogBodyRadius * 0.85f,
                delta.normalized,
                out RaycastHit hit,
                distance,
                movementBlockerMask,
                QueryTriggerInteraction.Ignore))
        {
            allowedDistance = Mathf.Max(0f, hit.distance - dogBodyRadius * 0.25f);
            return true;
        }

        return false;
    }

    private Vector3 WorldToVenueLocal(Vector3 worldPosition)
    {
        return venueContentRoot != null
            ? venueContentRoot.InverseTransformPoint(worldPosition)
            : worldPosition;
    }

    private Vector3 VenueLocalToWorld(Vector3 venueLocalPosition)
    {
        return venueContentRoot != null
            ? venueContentRoot.TransformPoint(venueLocalPosition)
            : venueLocalPosition;
    }
}
