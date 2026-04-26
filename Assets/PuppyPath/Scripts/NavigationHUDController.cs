using UnityEngine;
using TMPro;

public class NavigationHUDController : MonoBehaviour
{
    [Header("Main UI Groups")]
    [SerializeField] private GameObject friendListPanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject mapPanel;

    [Header("Navigation HUD")]
    [SerializeField] private GameObject navigationHudPanel;
    [SerializeField] private TMP_Text stateText;

    public void EnterNavigationMode()
    {
        if (friendListPanel != null)
            friendListPanel.SetActive(false);

        if (introPanel != null)
            introPanel.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (navigationHudPanel != null)
            navigationHudPanel.SetActive(true);

        UpdateStateText("Getting ready...");
    }

    public void ExitNavigationMode()
    {
        if (friendListPanel != null)
            friendListPanel.SetActive(true);

        if (introPanel != null)
            introPanel.SetActive(true);

        if (mapPanel != null)
            mapPanel.SetActive(true);

        if (navigationHudPanel != null)
            navigationHudPanel.SetActive(false);

        UpdateStateText("");
    }

    public void UpdateStateText(string text)
    {
        if (stateText != null)
            stateText.text = text;
    }
}