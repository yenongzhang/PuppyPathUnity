using UnityEngine;
using TMPro;

public class NavigationHUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject navigationHudRoot;
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private TMP_Text stateText;

    public void EnterNavigationMode(string targetName)
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (navigationHudRoot != null)
            navigationHudRoot.SetActive(true);

        if (targetNameText != null)
            targetNameText.text = targetName;

        if (stateText != null)
            stateText.text = "Let's go!";
    }

    public void ExitNavigationMode()
    {
        if (navigationHudRoot != null)
            navigationHudRoot.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        if (stateText != null)
            stateText.text = "";
    }

    public void UpdateStateText(string text)
    {
        if (stateText != null)
            stateText.text = text;
    }
}