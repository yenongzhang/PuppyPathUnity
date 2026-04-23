using UnityEngine;

public class RouteRootSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private GameObject routeRootPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float forwardDistance = 1.5f;
    [SerializeField] private float eyeToGroundOffset = 1.5f;

    private GameObject currentRouteRoot;

    public Transform SpawnRouteRoot()
    {
        if (xrCamera == null || routeRootPrefab == null)
        {
            Debug.LogWarning("RouteRootSpawner: missing references.");
            return null;
        }

        ClearCurrentRouteRoot();

        Vector3 flatForward = xrCamera.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;

        flatForward.Normalize();

        Vector3 spawnPosition = xrCamera.position + flatForward * forwardDistance;
        spawnPosition.y = xrCamera.position.y - eyeToGroundOffset;

        Quaternion spawnRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        currentRouteRoot = Instantiate(routeRootPrefab, spawnPosition, spawnRotation);
        currentRouteRoot.name = "RouteRoot_Runtime";

        return currentRouteRoot.transform;
    }

    public Transform GetCurrentRouteRoot()
    {
        return currentRouteRoot != null ? currentRouteRoot.transform : null;
    }

    public void ClearCurrentRouteRoot()
    {
        if (currentRouteRoot != null)
        {
            Destroy(currentRouteRoot);
            currentRouteRoot = null;
        }
    }
}