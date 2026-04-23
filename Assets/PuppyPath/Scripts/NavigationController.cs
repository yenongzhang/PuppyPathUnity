using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private GameObject dogDummyPrefab;

    [Header("Optional UI")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private TMP_Text previewTitleText;
    [SerializeField] private TMP_Text previewSubtitleText;

    [Header("Dog Spawn")]
    [SerializeField] private Vector3 dogSpawnOffset = Vector3.zero;

    private GameObject currentDog;
    private string pendingPathId;

    public void SetPendingPathId(string pathId)
    {
        pendingPathId = pathId;
    }

    public string GetPendingPathId()
    {
        return pendingPathId;
    }

    public void ShowPreview()
    {
        if (string.IsNullOrEmpty(pendingPathId))
        {
            Debug.LogWarning("NavigationController: no pending path id.");
            return;
        }

        bool success = previewController.ShowPreview(pendingPathId);
        if (!success) return;

        if (showPathButton != null) showPathButton.SetActive(false);
        if (startButton != null) startButton.SetActive(true);
        if (backButton != null) backButton.SetActive(true);

        if (previewTitleText != null)
            previewTitleText.text = "Please face the road ahead.";

        if (previewSubtitleText != null)
            previewSubtitleText.text = "Here is the first part of your path. Ready to go?";
    }

    public void CancelPreview()
    {
        previewController.ClearAll();

        if (startButton != null) startButton.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if (showPathButton != null) showPathButton.SetActive(true);

        if (previewTitleText != null)
            previewTitleText.text = "";

        if (previewSubtitleText != null)
            previewSubtitleText.text = "";
    }

    public void StartNavigation()
    {
        PathDefinition path = previewController.GetCurrentPathInstance();
        Transform routeRoot = previewController.GetCurrentRouteRoot();

        if (path == null || routeRoot == null)
        {
            Debug.LogWarning("NavigationController: preview path or route root missing.");
            return;
        }

        if (currentDog != null)
            Destroy(currentDog);

        if (dogDummyPrefab == null)
        {
            Debug.LogWarning("NavigationController: dog dummy prefab missing.");
            return;
        }

        Vector3 spawnPos = routeRoot.position + dogSpawnOffset;
        Quaternion spawnRot = routeRoot.rotation;

        currentDog = Instantiate(dogDummyPrefab, spawnPos, spawnRot);

        DogDummyFollower follower = currentDog.GetComponent<DogDummyFollower>();
        if (follower == null)
        {
            Debug.LogWarning("NavigationController: dog dummy prefab has no DogDummyFollower.");
            return;
        }

        follower.SetPath(path.waypoints);

        if (startButton != null) startButton.SetActive(false);
        if (backButton != null) backButton.SetActive(false);

        // 这里你可以选择保留 preview line，也可以清掉
        // 如果你想开始后就清掉预览线，可以这样：
        // 只删 line，不删 path 和 root
        ClearOnlyPreviewLine();
    }

    private void ClearOnlyPreviewLine()
    {
        Transform routeRoot = previewController.GetCurrentRouteRoot();
        if (routeRoot == null) return;

        Transform previewLine = routeRoot.Find("PreviewLine");
        if (previewLine != null)
        {
            Destroy(previewLine.gameObject);
        }
    }

    public void StopAndClearNavigation()
    {
        if (currentDog != null)
        {
            Destroy(currentDog);
            currentDog = null;
        }

        previewController.ClearAll();

        if (showPathButton != null) showPathButton.SetActive(true);
        if (startButton != null) startButton.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
    }
}