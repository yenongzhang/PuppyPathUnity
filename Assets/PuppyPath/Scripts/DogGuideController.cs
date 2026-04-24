using UnityEngine;
using System.Collections.Generic;

public class DogGuideController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject dogPrefab;

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

    private GameObject currentDog;
    private Renderer dogRenderer;
    private Transform xrCamera;
    private List<Transform> path = new List<Transform>();
    private bool isGuiding;
    private NavigationRuntimeController.NavState currentState = NavigationRuntimeController.NavState.Neutral;
    private Vector3 currentRecommendedDirection = Vector3.forward;

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

        currentDog = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
        dogRenderer = currentDog.GetComponentInChildren<Renderer>();
        ApplyColor(neutralColor);

        isGuiding = true;
    }

    public void StopGuiding()
    {
        isGuiding = false;

        if (currentDog != null)
        {
            Destroy(currentDog);
            currentDog = null;
        }

        dogRenderer = null;
        path.Clear();
        xrCamera = null;
    }

    public void ApplyNavigationState(NavigationRuntimeController.NavState navState, float distanceToGoal, Vector3 recommendedDirection)
    {
        currentState = navState;
        currentRecommendedDirection = recommendedDirection;

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
    }

    private void Update()
    {
        if (!isGuiding || currentDog == null || xrCamera == null)
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
}