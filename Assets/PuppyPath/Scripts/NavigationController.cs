using UnityEngine;
using TMPro;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathPreviewController previewController;
    [SerializeField] private NavigationRuntimeController runtimeController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private PuppyPathSelectionUI selectionUI;

    [Header("Optional UI")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private TMP_Text previewTitleText;
    [SerializeField] private TMP_Text previewSubtitleText;

    private string pendingPathId;
    private string pendingTargetName = "Destination";

    public void SetPendingPathId(string pathId)
    {
        pendingPathId = pathId;
    }

    public void SetPendingTargetName(string targetName)
    {
        pendingTargetName = targetName;
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
            previewSubtitleText.text = "Here is your full path. Ready to go?";
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
        if (runtimeController == null)
        {
            Debug.LogWarning("NavigationController: runtimeController missing.");
            return;
        }

        if (startButton != null) startButton.SetActive(false);
        if (backButton != null) backButton.SetActive(false);

        ClearOnlyPreviewLine();

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
    }

    private void ClearOnlyPreviewLine()
    {
        Transform routeRoot = previewController != null ? previewController.GetCurrentRouteRoot() : null;
        if (routeRoot == null) return;

        Transform previewLine = routeRoot.Find("PreviewLine");
        if (previewLine != null)
            Destroy(previewLine.gameObject);
    }
}