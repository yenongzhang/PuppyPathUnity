using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationRuntimeController runtimeController;
    [SerializeField] private VenueNavigationRuntime venueNavigationRuntime;
    [SerializeField] private VenueMapUiController venueMapUiController;
    [SerializeField] private VenueCollectibleSpawner venueCollectibleSpawner;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private PuppyPathSelectionUI selectionUI;

    [Header("Intro Phases")]
    [SerializeField] private GameObject introPhase1;

    [Header("Preview UI")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;

    [Header("Fireworks")]
    [SerializeField] private List<GameObject> fireworkPrefabs = new List<GameObject>();
    [SerializeField] private Transform fireworkSpawnPoint;
    [SerializeField] private float fireworkHeightOffset = 1.4f;
    [SerializeField] private float fireworkRadius = 0.8f;
    [SerializeField] private int fireworkWaves = 1;
    [SerializeField] private float fireworkWaveInterval = 0.35f;
    [SerializeField] private bool spawnAllFireworkTypesPerWave = true;
    [SerializeField] private int randomFireworksPerWave = 4;
    [SerializeField] private bool destroyFireworksAfterDelay = true;
    [SerializeField] private float fireworkDestroyDelay = 4f;
    [SerializeField] private AudioClip fireworkSound;
    [SerializeField] private float fireworkSoundVolume = 0.75f;
    [SerializeField] private bool debugFireworks = true;
    [Header("Arrival")]
    [SerializeField] private float arrivalFreeRoamDelay = 0f;

    [Header("Destination Beacon")]
    [SerializeField] private GameObject destinationBeaconPrefab;
    [SerializeField] private float destinationBeaconHeightOffset = 0.08f;
    [SerializeField] private bool destinationBeaconFollowWaypoint = true;

    private GameObject currentDestinationBeacon;

    private string pendingPathId;
    private string pendingTargetName = "Destination";
    private bool isInNavigationMode;
    private bool hasCompletedNavigation;
    private Coroutine fireworkRoutine;
    private Coroutine arrivalFreeRoamRoutine;

    private void Start()
    {
        if (venueNavigationRuntime == null)
            venueNavigationRuntime = FindFirstObjectByType<VenueNavigationRuntime>();

        if (venueMapUiController == null)
            venueMapUiController = FindFirstObjectByType<VenueMapUiController>();

        if (venueCollectibleSpawner == null)
            venueCollectibleSpawner = FindFirstObjectByType<VenueCollectibleSpawner>();

        ResetToIntro();
    }

    private void Update()
    {
        if (!isInNavigationMode || hasCompletedNavigation || venueNavigationRuntime == null)
            return;

        if (venueNavigationRuntime.IsNavigating &&
            venueNavigationRuntime.CurrentState == NavigationRuntimeController.NavState.Arrived)
        {
            CompleteNavigation();
        }
    }

    public void SetPendingPathId(string pathId)
    {
        pendingPathId = pathId;
    }

    public void SetPendingTargetName(string targetName)
    {
        pendingTargetName = string.IsNullOrWhiteSpace(targetName) ? "Destination" : targetName;
    }

    public string GetPendingPathId()
    {
        return pendingPathId;
    }

    public string GetPendingTargetName()
    {
        return pendingTargetName;
    }

    public void ShowPathSelectionPhase()
    {
        if (isInNavigationMode)
            return;

        SetIntroVisible(true);

        if (showPathButton != null)
            showPathButton.SetActive(!string.IsNullOrEmpty(pendingPathId));

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    public void ShowPreview()
    {
        if (isInNavigationMode)
            return;

        if (string.IsNullOrEmpty(pendingPathId))
        {
            Debug.LogWarning("NavigationController: no pending path id.");
            ShowPathSelectionPhase();
            return;
        }

        if (previewController == null)
        {
            Debug.LogWarning("NavigationController: previewController missing.");
            return;
        }

        bool success = previewController.ShowPreview(pendingPathId);
        if (!success)
        {
            Debug.LogWarning("NavigationController: failed to show preview.");
            ShowPathSelectionPhase();
            return;
        }

        ShowPreviewPhase();
    }

    public void CancelPreview()
    {
        if (isInNavigationMode)
            return;

        if (previewController != null)
            previewController.ClearAll();

        ShowPathSelectionPhase();
    }

    public void StartNavigation()
    {
        if (isInNavigationMode)
            return;

        if (runtimeController == null)
        {
            Debug.LogWarning("NavigationController: runtimeController missing.");
            return;
        }

        if (previewController == null || previewController.GetCurrentPathInstance() == null)
        {
            Debug.LogWarning("NavigationController: preview path missing. Show preview first.");
            return;
        }

        isInNavigationMode = true;
        hasCompletedNavigation = false;

        ClearOnlyPreviewLine();

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (hudController != null)
            hudController.EnterNavigationMode();

        ShowDestinationBeacon();

        runtimeController.StartRuntime();
    }

    public bool StartVenueNavigationToAttraction(string attractionId, string targetName)
    {
        if (string.IsNullOrWhiteSpace(attractionId))
            return false;

        if (venueNavigationRuntime == null)
        {
            Debug.LogWarning("NavigationController: venueNavigationRuntime missing.");
            return false;
        }

        if (isInNavigationMode)
            StopNavigationAndReturnToFreeRoam();

        bool started = venueNavigationRuntime.StartNavigationToAttraction(attractionId);
        if (!started)
            return false;

        pendingPathId = attractionId;
        pendingTargetName = string.IsNullOrWhiteSpace(targetName) ? attractionId : targetName;
        isInNavigationMode = true;
        hasCompletedNavigation = false;

        HideDestinationBeacon();

        if (previewController != null)
            previewController.ClearAll();

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (hudController != null)
            hudController.EnterNavigationMode(pendingTargetName);

        return true;
    }

    public void DismissIntroAndStartWalking()
    {
        StopNavigationAndReturnToFreeRoam();
    }

    public void StartDogAfterLogo()
    {
        if (venueNavigationRuntime == null)
            venueNavigationRuntime = FindFirstObjectByType<VenueNavigationRuntime>();

        if (venueNavigationRuntime != null)
            venueNavigationRuntime.StartFreeRoamGuiding();
    }

    public void OpenMapFromHud()
    {
        if (hudController != null)
            hudController.SetMapVisible(true);

        if (venueMapUiController != null)
            venueMapUiController.OpenLargeMap();
    }

    public void CloseIntroAndMap()
    {
        if (isInNavigationMode)
        {
            if (hudController != null)
                hudController.EnterNavigationMode(pendingTargetName);

            if (venueMapUiController != null)
                venueMapUiController.CloseLargeMap();

            return;
        }

        DismissIntroAndStartWalking();
    }

    public void CompleteNavigation()
    {
        if (hasCompletedNavigation)
            return;

        hasCompletedNavigation = true;

        if (debugFireworks)
            Debug.Log("NavigationController: CompleteNavigation() called.");

        HideDestinationBeacon();

        string arrivedAttractionId = pendingPathId;
        bool treasureDiscoveryStarted = venueCollectibleSpawner != null
            && venueCollectibleSpawner.TryStartDiscoveryForAttraction(arrivedAttractionId);

        if (!treasureDiscoveryStarted)
            PlayDestinationFireworks();

        if (!treasureDiscoveryStarted && venueMapUiController != null)
            venueMapUiController.MarkAttractionVisited(arrivedAttractionId);

        isInNavigationMode = false;

        if (arrivalFreeRoamRoutine != null)
            StopCoroutine(arrivalFreeRoamRoutine);

        if (arrivalFreeRoamDelay <= 0f)
            StopNavigationAndReturnToFreeRoam();
        else
            arrivalFreeRoamRoutine = StartCoroutine(ReturnToFreeRoamAfterArrival());
    }

    public void GiveUpNavigation()
    {
        StopNavigationAndReturnToFreeRoam();
    }

    private IEnumerator ReturnToFreeRoamAfterArrival()
    {
        yield return new WaitForSeconds(arrivalFreeRoamDelay);

        arrivalFreeRoamRoutine = null;

        StopNavigationAndReturnToFreeRoam();
    }

    private void ResetToIntro()
    {
        SetIntroVisible(true);

        pendingPathId = null;
        pendingTargetName = "Destination";
        isInNavigationMode = false;
        hasCompletedNavigation = false;

        HideDestinationBeacon();
        if (previewController != null)
            previewController.ClearAll();

        if (hudController != null)
            hudController.ShowIntroAndMap();

        if (selectionUI != null)
            selectionUI.ResetToDefaultState();

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    private void ShowPreviewPhase()
    {
        SetIntroVisible(true);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(true);

        if (backButton != null)
            backButton.SetActive(true);
    }

    private void SetIntroVisible(bool visible)
    {
        if (introPhase1 != null)
            introPhase1.SetActive(visible);
    }

    private void StopNavigationAndReturnToFreeRoam()
    {
        if (arrivalFreeRoamRoutine != null)
        {
            StopCoroutine(arrivalFreeRoamRoutine);
            arrivalFreeRoamRoutine = null;
        }

        if (runtimeController != null)
            runtimeController.StopRuntime();

        if (venueNavigationRuntime != null)
            venueNavigationRuntime.StopNavigation();

        if (previewController != null)
            previewController.ClearAll();

        HideDestinationBeacon();

        pendingPathId = null;
        pendingTargetName = "Destination";
        isInNavigationMode = false;
        hasCompletedNavigation = false;

        if (selectionUI != null)
            selectionUI.ResetToDefaultState();

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (venueMapUiController != null)
            venueMapUiController.CloseLargeMap();

        if (venueNavigationRuntime != null)
            venueNavigationRuntime.StartFreeRoamGuiding();

        if (hudController != null && !hudController.IsTreasureRevealSequenceActive)
            hudController.EnterFreeRoamMode();
    }

    private void ClearOnlyPreviewLine()
    {
        Transform routeRoot = previewController != null ? previewController.GetCurrentRouteRoot() : null;
        if (routeRoot == null)
            return;

        Transform previewLine = routeRoot.Find("PreviewLine");
        if (previewLine != null)
            Destroy(previewLine.gameObject);
    }

    private void ShowDestinationBeacon()
    {
        HideDestinationBeacon();

        if (destinationBeaconPrefab == null)
        {
            Debug.LogWarning("NavigationController: destinationBeaconPrefab is not assigned.");
            return;
        }

        if (previewController == null)
            return;

        Transform destinationWaypoint = previewController.GetCurrentDestinationWaypoint();
        if (destinationWaypoint == null)
        {
            Debug.LogWarning("NavigationController: destination waypoint not found.");
            return;
        }

        Vector3 spawnPosition = destinationWaypoint.position + Vector3.up * destinationBeaconHeightOffset;

        currentDestinationBeacon = Instantiate(
            destinationBeaconPrefab,
            spawnPosition,
            destinationBeaconPrefab.transform.rotation
        );

        currentDestinationBeacon.name = "DestinationBeacon_Runtime";

        if (destinationBeaconFollowWaypoint)
        {
            currentDestinationBeacon.transform.SetParent(destinationWaypoint, true);
            currentDestinationBeacon.transform.localPosition = Vector3.up * destinationBeaconHeightOffset;

            currentDestinationBeacon.transform.localRotation = destinationBeaconPrefab.transform.localRotation;
        }
    }

    private void HideDestinationBeacon()
    {
        if (currentDestinationBeacon != null)
        {
            Destroy(currentDestinationBeacon);
            currentDestinationBeacon = null;
        }
    }

    private void PlayDestinationFireworks()
    {
        PlayCelebrationFireworksAtBase(GetFireworkBasePosition());
    }

    public void PlayCelebrationFireworks(Vector3 worldPosition)
    {
        PlayCelebrationFireworksAtBase(worldPosition + Vector3.up * fireworkHeightOffset);
    }

    private void PlayCelebrationFireworksAtBase(Vector3 basePosition)
    {
        if (fireworkPrefabs == null || fireworkPrefabs.Count == 0)
        {
            Debug.LogWarning("NavigationController: no firework prefabs assigned.");
            return;
        }

        if (fireworkRoutine != null)
            StopCoroutine(fireworkRoutine);

        if (fireworkSound != null)
            AudioSource.PlayClipAtPoint(fireworkSound, basePosition, fireworkSoundVolume);

        fireworkRoutine = StartCoroutine(PlayFireworkRoutine(basePosition));
    }

    private IEnumerator PlayFireworkRoutine(Vector3 basePosition)
    {
        int waves = Mathf.Max(1, fireworkWaves);

        for (int wave = 0; wave < waves; wave++)
        {
            if (spawnAllFireworkTypesPerWave)
            {
                for (int i = 0; i < fireworkPrefabs.Count; i++)
                    SpawnOneFirework(fireworkPrefabs[i], basePosition);
            }
            else
            {
                int count = Mathf.Max(1, randomFireworksPerWave);
                for (int i = 0; i < count; i++)
                    SpawnOneFirework(GetRandomFireworkPrefab(), basePosition);
            }

            if (wave < waves - 1 && fireworkWaveInterval > 0f)
                yield return new WaitForSeconds(fireworkWaveInterval);
        }

        fireworkRoutine = null;
    }

    private void SpawnOneFirework(GameObject prefab, Vector3 basePosition)
    {
        if (prefab == null)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * fireworkRadius;

        Vector3 spawnPosition = basePosition + new Vector3(
            randomCircle.x,
            Random.Range(0f, fireworkRadius * 0.6f),
            randomCircle.y
        );

        Quaternion spawnRotation = prefab.transform.rotation;

        GameObject firework = Instantiate(prefab, spawnPosition, spawnRotation);

        ParticleSystem[] particleSystems = firework.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        if (destroyFireworksAfterDelay)
            Destroy(firework, fireworkDestroyDelay);
    }

    private Vector3 GetFireworkBasePosition()
    {
        if (fireworkSpawnPoint != null)
            return fireworkSpawnPoint.position + Vector3.up * fireworkHeightOffset;

        if (previewController != null && previewController.TryGetCurrentDestinationPosition(out Vector3 destinationPosition))
            return destinationPosition + Vector3.up * fireworkHeightOffset;

        return transform.position + Vector3.up * fireworkHeightOffset;
    }

    private GameObject GetRandomFireworkPrefab()
    {
        if (fireworkPrefabs == null || fireworkPrefabs.Count == 0)
            return null;

        int index = Random.Range(0, fireworkPrefabs.Count);
        return fireworkPrefabs[index];
    }
}
