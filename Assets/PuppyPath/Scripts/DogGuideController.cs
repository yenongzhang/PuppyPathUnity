using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DogGuideController : MonoBehaviour
{
    public enum DogExpression
    {
        Happy,
        LookingAround,
        Angry,
        Calm,
        Tearful,
        Barking,
        Resting,
        Smug
    }

    [Header("References")]
    [SerializeField] private GameObject dogPrefab;

    [Header("Expression Renderers")]
    [SerializeField] private Renderer eyesRenderer;
    [SerializeField] private Renderer mouthRenderer;

    [Header("Eye Textures")]
    [SerializeField] private Texture happyEyes;
    [SerializeField] private Texture lookingAroundEyes;
    [SerializeField] private Texture angryEyes;
    [SerializeField] private Texture calmEyes;
    [SerializeField] private Texture tearfulEyes;
    [SerializeField] private Texture barkingEyes;
    [SerializeField] private Texture restingEyes;
    [SerializeField] private Texture smugEyes;

    [Header("Mouth Textures")]
    [SerializeField] private Texture happyMouth;
    [SerializeField] private Texture lookingAroundMouth;
    [SerializeField] private Texture angryMouth;
    [SerializeField] private Texture calmMouth;
    [SerializeField] private Texture tearfulMouth;
    [SerializeField] private Texture barkingMouth;
    [SerializeField] private Texture restingMouth;
    [SerializeField] private Texture smugMouth;

    [Header("Guide Movement")]
    [SerializeField] private float leadDistance = 1.8f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private float minDistanceToUser = 1.0f;
    [SerializeField] private float maxDistanceToUser = 3.0f;

    [Header("Debug State Colors")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color happyColor = Color.green;
    [SerializeField] private Color angryColor = Color.red;
    [SerializeField] private Color arrivedColor = Color.yellow;

    [Header("Animation")]
    [SerializeField] private Animator dogAnimator;
    [SerializeField] private float celebrationDistance = 0.5f;

    [Header("Random Behaviors")]
    [SerializeField] private bool enableRandomBehaviors = true;
    [SerializeField] private float randomBehaviorMinInterval = 12f;
    [SerializeField] private float randomBehaviorMaxInterval = 42f;
    [SerializeField] private float randomBehaviorDuration = 2f;
    [SerializeField] private float runSpeed = 2.0f;
    [SerializeField] private float circleRadius = 1.3f;
    [SerializeField] private float circleSpeed = 180f;

    private GameObject currentDog;
    private Renderer dogRenderer;
    private Transform xrCamera;
    private List<Transform> path = new List<Transform>();
    private bool isGuiding;
    private NavigationRuntimeController.NavState currentState = NavigationRuntimeController.NavState.Neutral;
    private Vector3 currentRecommendedDirection = Vector3.forward;

    private bool isPerformingRandomBehavior;
    private Coroutine randomBehaviorRoutine;

    public void BeginGuiding(List<Transform> runtimePath, Transform userCamera)
    {
        xrCamera = userCamera;
        path.Clear();

        foreach (var wp in runtimePath)
        {
            if (wp != null)
                path.Add(wp);
        }

        if (dogPrefab == null || xrCamera == null || path.Count < 2)
        {
            Debug.LogWarning("DogGuideController: missing dogPrefab/xrCamera/path.");
            return;
        }

        if (currentDog != null)
            Destroy(currentDog);

        Vector3 spawnPos = xrCamera.position + xrCamera.forward * 1.2f;
        spawnPos.y = path[0].position.y;

        /**
        currentDog = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        dogRenderer = currentDog.GetComponentInChildren<Renderer>();
        ApplyColor(neutralColor);

        isGuiding = true;
        **/
        currentDog = Instantiate(dogPrefab, spawnPos, Quaternion.identity);

        dogAnimator = currentDog.GetComponentInChildren<Animator>();

        SetupExpressionRenderers();
        SetExpression(DogExpression.Calm);
        PlayStand();

        isGuiding = true;
        isPerformingRandomBehavior = false;

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
        isPerformingRandomBehavior = false;

        if (randomBehaviorRoutine != null)
        {
            StopCoroutine(randomBehaviorRoutine);
            randomBehaviorRoutine = null;
        }

        if (currentDog != null)
        {
            Destroy(currentDog);
            currentDog = null;
        }

        dogRenderer = null;
        dogAnimator = null;
        eyesRenderer = null;
        mouthRenderer = null;

        path.Clear();
        xrCamera = null;
    }

    public void ApplyNavigationState(NavigationRuntimeController.NavState navState, float distanceToGoal, Vector3 recommendedDirection)
    {
        currentState = navState;
        currentRecommendedDirection = recommendedDirection;

        if (isPerformingRandomBehavior)
            return;

        if (distanceToGoal <= celebrationDistance && currentState != NavigationRuntimeController.NavState.Arrived)
        {
            currentState = NavigationRuntimeController.NavState.Arrived;
            SetExpression(DogExpression.Happy);
            PlayArrive();
            return;
        }

        /**
        switch (navState)
        {
            case NavigationRuntimeController.NavState.GettingCloser:
                ApplyColor(happyColor);
                break;

            case NavigationRuntimeController.NavState.GettingFarther:
            case NavigationRuntimeController.NavState.Lost:
                ApplyColor(angryColor);
                break;

            case NavigationRuntimeController.NavState.Arrived:
                ApplyColor(arrivedColor);
                break;

            default:
                ApplyColor(neutralColor);
                break;
        }
        **/

        switch (navState)
        {
            case NavigationRuntimeController.NavState.GettingCloser:
                SetExpression(DogExpression.Happy);
                break;

            case NavigationRuntimeController.NavState.GettingFarther:
                SetExpression(DogExpression.Angry);
                break;

            case NavigationRuntimeController.NavState.Lost:
                SetExpression(DogExpression.Barking);
                break;

            case NavigationRuntimeController.NavState.Arrived:
                SetExpression(DogExpression.Happy);
                break;

            case NavigationRuntimeController.NavState.Waiting:
                SetExpression(DogExpression.Calm);
                PlayStand();
                break;

            case NavigationRuntimeController.NavState.Neutral:
            default:
                SetExpression(DogExpression.Calm);
                break;
}
    }

    private void Update()
    {
        if (!isGuiding || currentDog == null || xrCamera == null)
            return;

        if (isPerformingRandomBehavior)
            return;

        if (currentState == NavigationRuntimeController.NavState.GettingFarther ||
            currentState == NavigationRuntimeController.NavState.Lost ||
            currentState == NavigationRuntimeController.NavState.Arrived)
        {
            return;
        }

        Vector3 userPos = xrCamera.position;
        Vector3 flatDir = currentRecommendedDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = xrCamera.forward;

        flatDir.y = 0f;
        flatDir.Normalize();

        Vector3 targetPos = userPos + flatDir * leadDistance;
        targetPos.y = currentDog.transform.position.y;

        float userDogDistance = Vector3.Distance(
            new Vector3(userPos.x, 0f, userPos.z),
            new Vector3(currentDog.transform.position.x, 0f, currentDog.transform.position.z)
        );

        if (userDogDistance < minDistanceToUser)
        {
            targetPos = userPos + flatDir * minDistanceToUser;
            targetPos.y = currentDog.transform.position.y;
        }
        else if (userDogDistance > maxDistanceToUser)
        {
            targetPos = userPos + flatDir * maxDistanceToUser;
            targetPos.y = currentDog.transform.position.y;
        }

        Vector3 moveDir = targetPos - currentDog.transform.position;
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
            currentDog.transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );
    }

    private void ApplyColor(Color color)
    {
        if (dogRenderer != null && dogRenderer.material != null)
            dogRenderer.material.color = color;
    }

    private void SetupExpressionRenderers()
    {
        eyesRenderer = null;
        mouthRenderer = null;

        if (currentDog == null)
            return;

        Renderer[] renderers = currentDog.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r.gameObject.name == "Eyes")
                eyesRenderer = r;

            if (r.gameObject.name == "Mouth")
                mouthRenderer = r;
        }

        if (eyesRenderer == null)
            Debug.LogWarning("DogGuideController: Eyes renderer not found. Make sure there is a child object named Eyes.");

        if (mouthRenderer == null)
            Debug.LogWarning("DogGuideController: Mouth renderer not found. Make sure there is a child object named Mouth.");
    }

    private void SetExpression(DogExpression expression)
    {
        SetEyesTexture(GetEyesTexture(expression));
        SetMouthTexture(GetMouthTexture(expression));
    }

    private Texture GetEyesTexture(DogExpression expression)
    {
        switch (expression)
        {
            case DogExpression.Happy:
                return happyEyes;
            case DogExpression.LookingAround:
                return lookingAroundEyes;
            case DogExpression.Angry:
                return angryEyes;
            case DogExpression.Calm:
                return calmEyes;
            case DogExpression.Tearful:
                return tearfulEyes;
            case DogExpression.Barking:
                return barkingEyes;
            case DogExpression.Resting:
                return restingEyes;
            case DogExpression.Smug:
                return smugEyes;
            default:
                return calmEyes;
        }
    }

    private Texture GetMouthTexture(DogExpression expression)
    {
        switch (expression)
        {
            case DogExpression.Happy:
                return happyMouth;
            case DogExpression.LookingAround:
                return lookingAroundMouth;
            case DogExpression.Angry:
                return angryMouth;
            case DogExpression.Calm:
                return calmMouth;
            case DogExpression.Tearful:
                return tearfulMouth;
            case DogExpression.Barking:
                return barkingMouth;
            case DogExpression.Resting:
                return restingMouth;
            case DogExpression.Smug:
                return smugMouth;
            default:
                return calmMouth;
        }
    }

    private void SetEyesTexture(Texture texture)
    {
        if (eyesRenderer == null || texture == null)
            return;

        eyesRenderer.material.mainTexture = texture;
    }

    private void SetMouthTexture(Texture texture)
    {
        if (mouthRenderer == null || texture == null)
            return;

        mouthRenderer.material.mainTexture = texture;
    }
    
    private void PlayStand()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsStanding", true);
    }

    private void PlayWalk()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsWalking", true);
        dogAnimator.SetBool("IsWaggingTail", true);
    }

    private void PlaySniffing()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsSniffing", true);
    }

    private void PlayArrive()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetTrigger("Arrive");
        dogAnimator.SetBool("IsWaggingTail", true);
    }

    private void PlayBark()
    {
        if (dogAnimator == null)
            return;

        dogAnimator.SetTrigger("Bark");
    }

    private void ResetAnimationBools()
    {
        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsWalking", false);
        dogAnimator.SetBool("IsSniffing", false);
        dogAnimator.SetBool("IsStanding", false);
        dogAnimator.SetBool("IsWaggingTail", false);
    }

    private void MoveDogTo(Vector3 targetPos, float speed)
    {
        if (currentDog == null)
            return;

        Vector3 moveDir = targetPos - currentDog.transform.position;
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
            currentDog.transform.position,
            targetPos,
            speed * Time.deltaTime
        );
    }

    private IEnumerator RandomBehaviorLoop()
    {
        while (isGuiding)
        {
            float waitTime = Random.Range(
                randomBehaviorMinInterval,
                randomBehaviorMaxInterval
            );

            yield return new WaitForSeconds(waitTime);

            if (!isGuiding || currentDog == null || xrCamera == null)
                continue;

            // 到达、迷路、走错方向时，不触发随机行为
            if (currentState == NavigationRuntimeController.NavState.Arrived ||
                currentState == NavigationRuntimeController.NavState.Lost ||
                currentState == NavigationRuntimeController.NavState.GettingFarther)
            {
                continue;
            }

            yield return StartCoroutine(PerformRandomBehavior());
        }
    }

    private IEnumerator PerformRandomBehavior()
    {
        isPerformingRandomBehavior = true;

        int randomIndex = Random.Range(0, 3);

        if (randomIndex == 0)
        {
            yield return StartCoroutine(RandomExcitedBehavior());
        }
        else if (randomIndex == 1)
        {
            yield return StartCoroutine(RandomCircleUserBehavior());
        }
        else
        {
            yield return StartCoroutine(RandomLazyBehavior());
        }

        isPerformingRandomBehavior = false;

        // 随机行为结束后，恢复当前导航状态对应表情/动画
        ApplyNavigationState(
            currentState,
            999f,
            currentRecommendedDirection
        );
    }

    private IEnumerator RandomExcitedBehavior()
    {
        SetExpression(DogExpression.Happy);
        PlayWalk();

        float elapsed = 0f;

        while (elapsed < randomBehaviorDuration)
        {
            if (currentState == NavigationRuntimeController.NavState.Arrived ||
                currentState == NavigationRuntimeController.NavState.Lost)
            {
                break;
            }

            elapsed += Time.deltaTime;

            Vector3 userPos = xrCamera.position;
            Vector3 flatDir = currentRecommendedDirection;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude < 0.0001f)
                flatDir = xrCamera.forward;

            flatDir.y = 0f;
            flatDir.Normalize();

            Vector3 targetPos = userPos + flatDir * (leadDistance + 0.5f);
            targetPos.y = currentDog.transform.position.y;

            MoveDogTo(targetPos, runSpeed);

            yield return null;
        }
    }

    private IEnumerator RandomCircleUserBehavior()
    {
        SetExpression(DogExpression.LookingAround);
        PlayWalk();

        float elapsed = 0f;
        float angle = 0f;

        while (elapsed < randomBehaviorDuration)
        {
            if (currentState == NavigationRuntimeController.NavState.Arrived ||
                currentState == NavigationRuntimeController.NavState.Lost)
            {
                break;
            }

            elapsed += Time.deltaTime;
            angle += circleSpeed * Time.deltaTime;

            Vector3 userPos = xrCamera.position;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(radians) * circleRadius,
                0f,
                Mathf.Sin(radians) * circleRadius
            );

            Vector3 targetPos = userPos + offset;
            targetPos.y = currentDog.transform.position.y;

            MoveDogTo(targetPos, moveSpeed);

            yield return null;
        }
    }

    private IEnumerator RandomLazyBehavior()
    {
        SetExpression(DogExpression.Resting);
        PlayStand();

        yield return new WaitForSeconds(randomBehaviorDuration);
    }
}