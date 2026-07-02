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

    [System.Serializable]
    private class DogStateExpressionGroup
    {
        public NavigationRuntimeController.NavState state;
        public Texture[] eyes;
        public Texture[] mouths;
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

    private string currentAnimationState = "";
    private float lastAnimationChangeTime = -999f;
    private float nearTargetTimer = 0f;
    private float lastNegativeReactionTime = -999f;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    /// <summary>Raised once after a dog instance finishes spawning/setup in BeginGuiding, so V2 systems (accessories, rewards) can hook in without coupling to internal guide state.</summary>
    public event System.Action<GameObject> DogSpawned;

    public GameObject CurrentDog => currentDog;

    /// <summary>Public passthrough to the existing animation crossfade path, for V2 callers (e.g. RewardRevealController) that need to trigger a one-shot state like "HappyStart" without duplicating CrossFade logic.</summary>
    public void PlayOneShotState(string stateName)
    {
        PlayAnimation(stateName, 1f, true);
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

    public void TickFreeRoamFollow(Vector3 targetPosition, Vector3 recommendedDirection)
    {
        if (!isGuiding || currentDog == null || xrCamera == null)
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
                1.05f,
                catchUpMoveSpeed,
                minDistanceForCanterAnimation);
            return;
        }

        if (distanceToTarget > 0.65f)
        {
            TryPlayLocomotionAndMove(
                targetPos,
                trotState,
                1.0f,
                trotMoveSpeed,
                minDistanceForTrotAnimation);
            return;
        }

        if (distanceToTarget > 0.25f)
        {
            TryPlayLocomotionAndMove(
                targetPos,
                walkState,
                0.95f,
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
                1.05f,
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
                1.0f,
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
                0.95f,
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
                1f,
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
                1f,
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
                1.1f,
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
                0.9f,
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
                0.85f,
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
                0.8f,
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

        Texture eyeTexture = PickRandomTexture(group.eyes);
        Texture mouthTexture = PickRandomTexture(group.mouths);

        SetEyesTexture(eyeTexture);
        SetMouthTexture(mouthTexture);
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

    private Texture PickRandomTexture(Texture[] textures)
    {
        if (textures == null || textures.Length == 0)
            return null;

        return textures[Random.Range(0, textures.Length)];
    }

    private void SetEyesTexture(Texture texture)
    {
        SetMaterialBaseMap(eyesRuntimeMaterial, texture);
    }

    private void SetMouthTexture(Texture texture)
    {
        SetMaterialBaseMap(mouthRuntimeMaterial, texture);
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
        float animationSpeed,
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
                }

                return false;
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

    private void MoveDogTo(Vector3 targetPos, float speed)
    {
        if (currentDog == null)
            return;

        Vector3 currentPos = currentDog.transform.position;

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

        currentDog.transform.position = Vector3.MoveTowards(
            currentPos,
            targetPos,
            speed * Time.deltaTime
        );
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
}
