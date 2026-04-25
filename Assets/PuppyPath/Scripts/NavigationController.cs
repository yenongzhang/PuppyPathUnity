using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationRuntimeController runtimeController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private PuppyPathSelectionUI selectionUI;

    [Header("Intro Phases")]
    [SerializeField] private GameObject introPhase1;
    [SerializeField] private GameObject introPhase2;
    [SerializeField] private GameObject introPhase3;
    [SerializeField] private GameObject introPhase4;

    [Header("Preview UI")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private TMP_Text previewTitleText;
    [SerializeField] private TMP_Text previewSubtitleText;

    [Header("Fireworks")]
    [Tooltip("Drag several different firework prefabs here. On arrival, all of them can be spawned together.")]
    [SerializeField] private List<GameObject> fireworkPrefabs = new List<GameObject>();

    [Tooltip("Optional. If assigned, fireworks spawn around this point. If empty, they spawn around this NavigationController.")]
    [SerializeField] private Transform fireworkSpawnPoint;

    [Tooltip("Height added above the spawn point.")]
    [SerializeField] private float fireworkHeightOffset = 1.4f;

    [Tooltip("Random horizontal spread around the spawn point.")]
    [SerializeField] private float fireworkRadius = 0.8f;

    [Tooltip("How many waves of fireworks to play. 1 means one simultaneous burst.")]
    [SerializeField] private int fireworkWaves = 1;

    [Tooltip("Delay between waves. Only used when Firework Waves is greater than 1.")]
    [SerializeField] private float fireworkWaveInterval = 0.35f;

    [Tooltip("If true, every prefab in Firework Prefabs will be spawned in each wave.")]
    [SerializeField] private bool spawnAllFireworkTypesPerWave = true;

    [Tooltip("Used only when Spawn All Firework Types Per Wave is false.")]
    [SerializeField] private int randomFireworksPerWave = 4;

    [SerializeField] private bool randomizeFireworkRotation = true;
    [SerializeField] private bool destroyFireworksAfterDelay = true;
    [SerializeField] private float fireworkDestroyDelay = 4f;
    [SerializeField] private bool debugFireworks = true;

    private string pendingPathId;
    private string pendingTargetName = "Destination";
    private bool isInNavigationMode;
    private bool hasCompletedNavigation;
    private Coroutine fireworkRoutine;

    private void Start()
    {
        ResetToMainMenu();
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
        ShowRuntimeHUDPhase();

        if (hudController != null)
            hudController.EnterNavigationMode(pendingTargetName);

        runtimeController.StartRuntime();
    }

    public void GiveUpNavigation()
    {
        StopNavigationAndReturnToMenu();
    }

    public void CompleteNavigation()
    {
        if (hasCompletedNavigation)
            return;

        hasCompletedNavigation = true;

        if (debugFireworks)
            Debug.Log("NavigationController: CompleteNavigation() called. Playing destination fireworks.");

        PlayDestinationFireworks();
        StopNavigationAndReturnToMenu();
    }

    private void PlayDestinationFireworks()
    {
        if (fireworkPrefabs == null || fireworkPrefabs.Count == 0)
        {
            Debug.LogWarning("NavigationController: no firework prefabs assigned.");
            return;
        }

        if (debugFireworks)
            Debug.Log($"NavigationController: Firework prefab count = {fireworkPrefabs.Count}.");

        if (fireworkRoutine != null)
            StopCoroutine(fireworkRoutine);

        // Capture the destination before StopNavigationAndReturnToMenu() clears the path.
        Vector3 basePosition = GetFireworkBasePosition();

        if (debugFireworks)
            Debug.Log($"NavigationController: Firework base position = {basePosition}.");

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

        Quaternion spawnRotation = randomizeFireworkRotation
            ? Random.rotation
            : Quaternion.identity;

        GameObject firework = Instantiate(prefab, spawnPosition, spawnRotation);

        if (debugFireworks)
            Debug.Log($"NavigationController: spawned firework {prefab.name} at {spawnPosition}.");

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
        // Optional manual override.
        if (fireworkSpawnPoint != null)
        {
            if (debugFireworks)
                Debug.Log("NavigationController: using manual Firework Spawn Point.");

            return fireworkSpawnPoint.position + Vector3.up * fireworkHeightOffset;
        }

        // Default: use the last waypoint of the current path.
        if (previewController != null && previewController.TryGetCurrentDestinationPosition(out Vector3 destinationPosition))
        {
            if (debugFireworks)
                Debug.Log("NavigationController: using current path last waypoint as firework spawn position.");

            return destinationPosition + Vector3.up * fireworkHeightOffset;
        }

        Debug.LogWarning("NavigationController: could not find current destination waypoint. Falling back to NavigationController position.");
        return transform.position + Vector3.up * fireworkHeightOffset;
    }
    private GameObject GetRandomFireworkPrefab()
    {
        if (fireworkPrefabs == null || fireworkPrefabs.Count == 0)
            return null;

        int index = Random.Range(0, fireworkPrefabs.Count);
        return fireworkPrefabs[index];
    }

    private void StopNavigationAndReturnToMenu()
    {
        if (runtimeController != null)
            runtimeController.StopRuntime();

        if (previewController != null)
            previewController.ClearAll();

        if (hudController != null)
            hudController.ExitNavigationMode();

        isInNavigationMode = false;
        pendingPathId = null;
        pendingTargetName = "Destination";

        if (selectionUI != null)
            selectionUI.ResetToDefaultState();

        ResetToMainMenu();
    }

    private void ResetToMainMenu()
    {
        // Phase1: main intro text only
        SetPhaseMode(1);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    public void ShowPathSelectionPhase()
    {
        if (isInNavigationMode)
            return;

        // Phase2:
        // Keep IntroPhase1 active because PuppyPathSelectionUI writes dynamic text into Phase1 texts.
        // Also enable IntroPhase2 because it contains the Show Path button.
        SetPhaseMode(2);

        if (showPathButton != null)
            showPathButton.SetActive(!string.IsNullOrEmpty(pendingPathId));

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    private void ShowPreviewPhase()
    {
        // Phase3: preview instruction + Start / Back
        SetPhaseMode(3);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(true);

        if (backButton != null)
            backButton.SetActive(true);

        if (previewTitleText != null)
            previewTitleText.text = "Please face the road ahead.";

        if (previewSubtitleText != null)
            previewSubtitleText.text = "Here is the first part of your path.\n\nReady to go?";
    }

    private void ShowRuntimeHUDPhase()
    {
        // Phase4: runtime navigation HUD
        SetPhaseMode(4);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);
    }

    private void SetPhaseMode(int phase)
    {
        if (introPhase1 != null)
            introPhase1.SetActive(phase == 1 || phase == 2);

        if (introPhase2 != null)
            introPhase2.SetActive(phase == 2);

        if (introPhase3 != null)
            introPhase3.SetActive(phase == 3);

        if (introPhase4 != null)
            introPhase4.SetActive(phase == 4);
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
}
