using UnityEngine;
using TMPro;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationRuntimeController runtimeController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private PuppyPathSelectionUI selectionUI;

    [Header("Preview UI")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private TMP_Text previewTitleText;
    [SerializeField] private TMP_Text previewSubtitleText;

    private string pendingPathId;
    private string pendingTargetName = "Destination";
    private bool isInNavigationMode;

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
            return;
        }

        if (previewController == null)
        {
            Debug.LogWarning("NavigationController: previewController missing.");
            return;
        }

        bool success = previewController.ShowPreview(pendingPathId);
        if (!success)
            return;

        if (showPathButton != null)
            showPathButton.SetActive(false);

        if (startButton != null)
            startButton.SetActive(true);

        if (backButton != null)
            backButton.SetActive(true);

        if (previewTitleText != null)
            previewTitleText.text = "Please face the road ahead.";

        if (previewSubtitleText != null)
            previewSubtitleText.text = "Here is your full path. Ready to go?";
    }

    public void CancelPreview()
    {
        if (isInNavigationMode)
            return;

        if (previewController != null)
            previewController.ClearAll();

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (showPathButton != null)
            showPathButton.SetActive(!string.IsNullOrEmpty(pendingPathId));

        ClearPreviewTexts();
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

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        ClearOnlyPreviewLine();
        ClearPreviewTexts();

        if (hudController != null)
            hudController.EnterNavigationMode(pendingTargetName);

        runtimeController.StartRuntime();
    }

    public void GiveUpNavigation()
    {
        if (runtimeController != null)
            runtimeController.StopRuntime();

        if (previewController != null)
            previewController.ClearAll();

        if (hudController != null)
            hudController.ExitNavigationMode();

        if (selectionUI != null)
            selectionUI.ResetToDefaultState();

        isInNavigationMode = false;
        pendingPathId = null;
        pendingTargetName = "Destination";

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        ClearPreviewTexts();
    }

    public void CompleteNavigation()
    {
        if (runtimeController != null)
            runtimeController.StopRuntime();

        if (previewController != null)
            previewController.ClearAll();

        isInNavigationMode = false;

        if (startButton != null)
            startButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (showPathButton != null)
            showPathButton.SetActive(false);

        ClearPreviewTexts();
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

    private void ClearPreviewTexts()
    {
        if (previewTitleText != null)
            previewTitleText.text = "";

        if (previewSubtitleText != null)
            previewSubtitleText.text = "";
    }
}