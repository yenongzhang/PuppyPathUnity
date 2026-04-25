using UnityEngine;
using TMPro;

public class NavigationHUDController : MonoBehaviour
{
    [Header("Main UI Groups")]
    [SerializeField] private GameObject friendListPanel;
    [SerializeField] private GameObject mapPanel;

    [Header("Intro Phases")]
    [SerializeField] private GameObject phase1;
    [SerializeField] private GameObject phase2;
    [SerializeField] private GameObject phase3;
    [SerializeField] private GameObject phase4;

    [Header("Phase 4 Texts")]
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private TMP_Text stateText;

    public void EnterNavigationMode(string targetName)
    {
        Debug.Log("EnterNavigationMode called. target = " + targetName);
        Debug.Log("Phase4 ref = " + (phase4 != null ? phase4.name : "NULL"));
        if (friendListPanel != null)
            friendListPanel.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (phase1 != null)
            phase1.SetActive(false);

        if (phase2 != null)
            phase2.SetActive(false);

        if (phase3 != null)
            phase3.SetActive(false);

        if (phase4 != null)
            phase4.SetActive(true);

        if (targetNameText != null)
            targetNameText.text = targetName;

        if (stateText != null)
            stateText.text = "Let's go!";
    }

    public void ExitNavigationMode()
    {
        if (friendListPanel != null)
            friendListPanel.SetActive(true);

        if (mapPanel != null)
            mapPanel.SetActive(true);

        if (phase1 != null)
            phase1.SetActive(true);

        if (phase2 != null)
            phase2.SetActive(false);

        if (phase3 != null)
            phase3.SetActive(false);

        if (phase4 != null)
            phase4.SetActive(false);

        if (targetNameText != null)
            targetNameText.text = "";

        if (stateText != null)
            stateText.text = "";
    }

    public void UpdateStateText(string text)
    {
        if (stateText != null)
            stateText.text = text;
    }
}