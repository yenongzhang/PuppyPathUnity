using TMPro;
using UnityEngine;

public class NavigationHUDController : MonoBehaviour
{
    [Header("Main UI Groups")]
    [SerializeField] private GameObject friendListPanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject xPanel;

    [Header("Navigation HUD")]
    [SerializeField] private GameObject navigationHudPanel;
    [SerializeField] private GameObject cancelNavigationButton;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private string freeRoamText = "Sniff around with me!";
    [SerializeField] private string navigationTextTemplate = "Paws this way to {0}!";
    [SerializeField] private string treasureFoundTextTemplate = "Thank you for helping Puppy find the {0} treasure!";

    public void ShowIntroAndMap()
    {
        SetMainPanelGroupVisible(true);
        SetNavigationHudVisible(false, false);

        UpdateStateText("");
    }

    public void EnterFreeRoamMode()
    {
        SetMainPanelGroupVisible(false);
        SetNavigationHudVisible(true, false);

        UpdateStateText(freeRoamText);
    }

    public void EnterNavigationMode()
    {
        EnterNavigationMode(null);
    }

    public void EnterNavigationMode(string destinationName)
    {
        SetMainPanelGroupVisible(false);
        SetNavigationHudVisible(true, true);

        string displayName = string.IsNullOrWhiteSpace(destinationName) ? "the next treasure" : destinationName;
        UpdateStateText(string.Format(navigationTextTemplate, displayName));
    }

    public void ExitNavigationMode()
    {
        ShowIntroAndMap();
    }

    public void ShowTreasureFoundMessage(string placeName)
    {
        SetMainPanelGroupVisible(false);
        SetNavigationHudVisible(true, false);

        string displayName = string.IsNullOrWhiteSpace(placeName) ? "hidden" : placeName;
        UpdateStateText(string.Format(treasureFoundTextTemplate, displayName));
    }

    public void HideNavigationHud()
    {
        SetNavigationHudVisible(false, false);
    }

    public void RestoreFreeRoamHud()
    {
        EnterFreeRoamMode();
    }

    public void SetMapVisible(bool visible)
    {
        SetMainPanelGroupVisible(visible);

        if (visible)
            SetNavigationHudVisible(false, false);
        else
            SetNavigationHudVisible(true, false);
    }

    public void SetIntroAndMapVisible(bool visible)
    {
        SetMainPanelGroupVisible(visible);
    }

    private void SetMainPanelGroupVisible(bool visible)
    {
        if (friendListPanel != null)
            friendListPanel.SetActive(visible);

        if (introPanel != null)
            introPanel.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(visible);

        if (xPanel != null)
            xPanel.SetActive(visible);
    }

    private void SetNavigationHudVisible(bool visible, bool navigationMode)
    {
        if (navigationHudPanel != null)
            navigationHudPanel.SetActive(visible);

        if (cancelNavigationButton != null)
            cancelNavigationButton.SetActive(visible && navigationMode);
    }

    public void UpdateStateText(string text)
    {
        if (stateText != null)
            stateText.text = text;
    }
}
