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
    [SerializeField] private string turnBackState = "TurnBack";
    [SerializeField] private string turnFrontState = "TurnFront";
    [SerializeField] private string turnLoopState = "TurnLoop";

    [Header("Animation Settings")]
    [SerializeField] private float animationCrossFadeTime = 0.12f;
    [SerializeField] private float shortTransitionDuration = 0.45f;
    [SerializeField] private float longTransitionDuration = 0.75f;

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
    [SerializeField] private float leadDistance = 1.45f;
    [SerializeField] private float besideDistance = 0.75f;
    [SerializeField] private float behindDistance = 0.9f;
    [SerializeField] private float minDistanceToUser = 0.7f;
    [SerializeField] private float maxDistanceToUser = 2.3f;

    [Header("Movement Speeds")]
    [SerializeField] private float sniffMoveSpeed = 0.28f;
    [SerializeField] private float walkMoveSpeed = 0.52f;
    [SerializeField] private float trotMoveSpeed = 0.78f;
    [SerializeField] private float canterMoveSpeed = 1.05f;
    [SerializeField] private float turnMoveSpeed = 0.7f;
    [SerializeField] private float catchUpMoveSpeed = 0.9f;
    [SerializeField] private float rotateSpeed = 5.5f;

    [Header("Locomotion Animation Gate")]
    [SerializeField] private float minDistanceForWalkAnimation = 0.20f;
    [SerializeField] private float minDistanceForSniffAnimation = 0.14f;
    [SerializeField] private float minDistanceForTrotAnimation = 0.28f;
    [SerializeField] private float minDistanceForCanterAnimation = 0.40f;
    [SerializeField] private float minDistanceForTurnAnimation = 0.28f;

    [Header("Random Behaviors")]
    [SerializeField] private bool enableRandomBehaviors = true;
    [SerializeField] private float randomBehaviorMinInterval = 1.4f;
    [SerializeField] private float randomBehaviorMaxInterval = 3.2f;
    [SerializeField] private float randomBehaviorMinDuration = 1.4f;
    [SerializeField] private float randomBehaviorMaxDuration = 2.6f;
    [SerializeField] private float circleRadius = 1.1f;
    [SerializeField] private float circleAngularSpeed = 130f;

    [Header("Negative State Behavior")]
    [SerializeField] private float barkDuration = 1.2f;
    [SerializeField] private float sitWaitDuration = 1.1f;
    [SerializeField] private float negativeReactionCooldown = 2.0f;

    [Header("Arrival")]
    [SerializeField] private float celebrationDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logAnimationChanges = false;

    private GameObject currentDog;
    private Transform xrCamera;
    private Animator dogAnimator;

    private readonly List<Transform> path = new List<Transform>();

    private bool isGuiding;
    private bool isPerformingBehavior;
    private bool currentBehaviorIsStateReaction;

    private NavigationRuntimeController.NavState currentState = NavigationRuntimeController.NavState.Neutral;
    private NavigationRuntimeController.NavState previousState = NavigationRuntimeController.NavState.Neutral;

    private Vector3 currentRecommendedDirection = Vector3.forward;
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

        if (currentDog != null)
            Destroy(currentDog);

        Vector3 spawnPos = xrCamera.position + GetFlatForward(xrCamera) * 1.2f;
        spawnPos.y = path[0].position.y;

        currentDog = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        dogAnimator = currentDog.GetComponentInChildren<Animator>();

        if (dogAnimator != null)
        {
            dogAnimator.applyRootMotion = false;
            dogAnimator.speed = 1f;
        }

        SetupExpressionMaterials();

        currentState = NavigationRuntimeController.NavState.Neutral;
        previousState = NavigationRuntimeController.NavState.Neutral;

        isGuiding = true;
        isPerformingBehavior = false;
        currentBehaviorIsStateReaction = false;

        currentAnimationState = "";
        lastAnimationChangeTime = -999f;
        nearTargetTimer = 0f;
        hasAppliedExpression = false;

        SetRandomExpressionForState(NavigationRuntimeController.NavState.Neutral);
        hasAppliedExpression = true;

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

        if (currentDog != null)
        {
            Destroy(currentDog);
            currentDog = null;
        }

        dogAnimator = null;
        eyesRenderer = null;
        mouthRenderer = null;
        eyesRuntimeMaterial = null;
        mouthRuntimeMaterial = null;

        path.Clear();
        xrCamera = null;
        currentAnimationState = "";
        nearTargetTimer = 0f;
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
            case NavigationRuntimeController.NavState.GettingCloser:
            case NavigationRuntimeController.NavState.Neutral:
                // Animation for movement is decided in Update(), based on actual distance to target.
                break;

            case NavigationRuntimeController.NavState.Waiting:
                PlayStand(1f);
                LookAtUser();
                break;

            case NavigationRuntimeController.NavState.GettingFarther:
                TryStartNegativeReaction(false);
                break;

            case NavigationRuntimeController.NavState.Lost:
                TryStartNegativeReaction(true);
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

        if (currentState == NavigationRuntimeController.NavState.Lost ||
            currentState == NavigationRuntimeController.NavState.GettingFarther)
        {
            LookAtUser();
            return;
        }

        if (currentState == NavigationRuntimeController.NavState.Waiting)
        {
            LookAtUser();
            return;
        }

        DoNormalGuideMovement();
    }

    private void DoNormalGuideMovement()
    {
        Vector3 userPos = xrCamera.position;
        Vector3 flatDir = GetRecommendedDirection();

        Vector3 targetPos = userPos + flatDir * leadDistance;
        targetPos.y = currentDog.transform.position.y;

        float userDogDistance = GetFlatDistance(userPos, currentDog.transform.position);

        if (userDogDistance > maxDistanceToUser)
        {
            targetPos = userPos + flatDir * maxDistanceToUser;
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                trotState,
                0.9f,
                catchUpMoveSpeed,
                minDistanceForTrotAnimation
            );

            return;
        }

        if (userDogDistance < minDistanceToUser)
        {
            targetPos = userPos + flatDir * minDistanceToUser;
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                walkState,
                0.75f,
                sniffMoveSpeed,
                minDistanceForWalkAnimation
            );

            return;
        }

        TryPlayLocomotionAndMove(
            targetPos,
            walkState,
            0.85f,
            walkMoveSpeed,
            minDistanceForWalkAnimation
        );
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

        if (isPerformingBehavior)
            return false;

        return currentState == NavigationRuntimeController.NavState.Neutral ||
               currentState == NavigationRuntimeController.NavState.GettingCloser;
    }

    private DogBehavior PickPositiveBehavior()
    {
        float r = Random.value;

        if (r < 0.34f) return DogBehavior.SniffAhead;
        if (r < 0.56f) return DogBehavior.WalkBeside;
        if (r < 0.74f) return DogBehavior.WalkBehind;
        if (r < 0.90f) return DogBehavior.ShortCanter;
        if (r < 0.96f) return DogBehavior.PauseAndLook;

        return DogBehavior.TurnCircleUser;
    }

    private void TryStartNegativeReaction(bool strong)
    {
        if (Time.time - lastNegativeReactionTime < negativeReactionCooldown)
        {
            PlayStand(1f);
            LookAtUser();
            return;
        }

        lastNegativeReactionTime = Time.time;

        if (strong)
        {
            StartBehavior(Random.value < 0.7f ? DogBehavior.BarkAtUser : DogBehavior.SitWait, true);
        }
        else
        {
            StartBehavior(Random.value < 0.6f ? DogBehavior.BarkAtUser : DogBehavior.PauseAndLook, true);
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
        float duration = Random.Range(2.0f, 5.0f);
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

            Vector3 targetPos = xrCamera.position + GetRecommendedDirection() * (leadDistance + 0.35f);
            targetPos.y = currentDog.transform.position.y;

            TryPlayLocomotionAndMove(
                targetPos,
                canterState,
                0.82f,
                canterMoveSpeed,
                minDistanceForCanterAnimation
            );

            yield return null;
        }
    }

    private IEnumerator DoTurnCircleUser()
    {
        float duration = Random.Range(1.2f, 2.6f);
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

        PlayAnimation(standState, 1f, true);

        float duration = Random.Range(0.45f, 0.9f);
        float elapsed = 0f;

        while (elapsed < duration && currentState != NavigationRuntimeController.NavState.Arrived)
        {
            elapsed += Time.deltaTime;
            LookAtUser();
            yield return null;
        }

        yield return PlayTransitionOnly(standToWalkState, shortTransitionDuration);
    }

    private IEnumerator DoBarkAtUser()
    {
        if (!string.IsNullOrWhiteSpace(barkState))
            PlayAnimation(barkState, 1f, true);
        else
            PlayAnimation(standState, 1f, true);

        float elapsed = 0f;

        while (elapsed < barkDuration)
        {
            elapsed += Time.deltaTime;
            LookAtUser();
            yield return null;
        }
    }

    private IEnumerator DoSitWait()
    {
        yield return PlayTransitionOnly(standToSitState, longTransitionDuration);

        PlayAnimation(sitState, 1f, true);

        float elapsed = 0f;

        while (elapsed < sitWaitDuration)
        {
            elapsed += Time.deltaTime;
            LookAtUser();

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

        yield return PlayTransitionOnly(happyStartState, shortTransitionDuration);

        PlayAnimation(happyState, 1f, true);

        float elapsed = 0f;
        float duration = 3.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            LookAtUser();
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

    private void PlaySit(float speed)
    {
        PlayAnimation(sitState, speed);
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

                FaceDirectionOrUser(targetPos);

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

            FaceDirectionOrUser(targetPos);

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

    private void FaceDirectionOrUser(Vector3 targetPos)
    {
        if (currentDog == null)
            return;

        Vector3 lookDir = targetPos - currentDog.transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.0001f)
        {
            LookAtUser();
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        currentDog.transform.rotation = Quaternion.Slerp(
            currentDog.transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );
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

    private void LookAtUser()
    {
        if (currentDog == null || xrCamera == null)
            return;

        Vector3 lookDir = xrCamera.position - currentDog.transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        currentDog.transform.rotation = Quaternion.Slerp(
            currentDog.transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );
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
