using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void ShowPathSelectionPhase()
    {
        if (isInNavigationMode)
            return;

        SetPhaseMode(2);

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

        runtimeController.StartRuntime();
    }

    public void CompleteNavigation()
    {
        if (hasCompletedNavigation)
            return;

        hasCompletedNavigation = true;

        if (debugFireworks)
            Debug.Log("NavigationController: CompleteNavigation() called. Playing destination fireworks.");

        PlayDestinationFireworks();

        if (hudController != null)
            hudController.UpdateStateText("You made it!");

        isInNavigationMode = false;
    }

    public void GiveUpNavigation()
    {
        StopNavigationAndReturnToMenu();
    }

    private void ResetToMainMenu()
    {
        SetPhaseMode(1);

        pendingPathId = null;
        pendingTargetName = "Destination";
        isInNavigationMode = false;
        hasCompletedNavigation = false;

        if (previewController != null)
            previewController.ClearAll();

        if (hudController != null)
            hudController.ExitNavigationMode();

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
        SetPhaseMode(3);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(true);

        if (backButton != null)
            backButton.SetActive(true);
    }

    private void SetPhaseMode(int phase)
    {
        if (introPhase1 != null)
            introPhase1.SetActive(phase == 1);

        if (introPhase2 != null)
            introPhase2.SetActive(phase == 2);

        if (introPhase3 != null)
            introPhase3.SetActive(phase == 3);
    }

    private void StopNavigationAndReturnToMenu()
    {
        if (runtimeController != null)
            runtimeController.StopRuntime();

        if (previewController != null)
            previewController.ClearAll();

        ResetToMainMenu();
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

    private void PlayDestinationFireworks()
    {
        if (fireworkPrefabs == null || fireworkPrefabs.Count == 0)
        {
            Debug.LogWarning("NavigationController: no firework prefabs assigned.");
            return;
        }

        if (fireworkRoutine != null)
            StopCoroutine(fireworkRoutine);

        Vector3 basePosition = GetFireworkBasePosition();
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