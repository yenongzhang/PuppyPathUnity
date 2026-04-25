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

    private string pendingPathId;
    private string pendingTargetName = "Destination";
    private bool isInNavigationMode;

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
        StopNavigationAndReturnToMenu();
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