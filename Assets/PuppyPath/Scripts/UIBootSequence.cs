using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIBootSequence : MonoBehaviour
{
    [Header("Startup Calibration")]
    [SerializeField] private bool waitForStartupCalibration;
    [SerializeField] private GameObject startupCalibrationRoot;
    [SerializeField] private string startupCalibrationRootName = "VenueRoot";
    [SerializeField] private GameObject[] startupCalibrationVisualRoots;
    [SerializeField] private string[] startupCalibrationVisualRootNames = { "VenueContentRoot", "VenueCalibrationDebug" };
    [SerializeField] private bool logStartupCalibration = true;

    [Header("Logo")]
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private GameObject logoRoot;

    [Header("Main UI")]
    [SerializeField] private GameObject mainCanvasRoot;
    [Tooltip("Optional. If empty, the script will try to find CanvasFollowHead on the main canvas root.")]
    [SerializeField] private CanvasFollowHead mainCanvasFollow;
    [Tooltip("Optional. Used to keep the main canvas invisible until it has snapped to the correct position.")]
    [SerializeField] private CanvasGroup mainCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("Optional Positioning")]
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private float logoDistance = 2.0f;
    [SerializeField] private float logoHeightOffset = -0.05f;

    [Header("Events")]
    [SerializeField] private UnityEvent onStartupCalibrationConfirmed;
    [SerializeField] private UnityEvent onBootFinished;

    private readonly List<GameObject> resolvedStartupCalibrationVisualRoots = new List<GameObject>();
    private bool startupCalibrationConfirmed;

    private void Awake()
    {
        ResolveMainCanvasReferences();
    }

    private void Start()
    {
        StartCoroutine(PlayBootSequence());
    }

    private IEnumerator PlayBootSequence()
    {
        ResolveMainCanvasReferences();

        HideMainCanvasWithoutShowingIt();
        HideLogoWithoutShowingIt();

        if (waitForStartupCalibration)
            yield return WaitForStartupCalibration();

        if (logoRoot != null)
            logoRoot.SetActive(true);

        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
            logoCanvasGroup.interactable = false;
            logoCanvasGroup.blocksRaycasts = false;
        }

        PlaceLogoInFrontOfUser();

        yield return FadeLogo(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return FadeLogo(1f, 0f, fadeOutDuration);

        if (logoRoot != null)
            logoRoot.SetActive(false);

        ShowMainCanvasSafely();
        onBootFinished?.Invoke();
    }

    private IEnumerator WaitForStartupCalibration()
    {
        ResolveStartupCalibrationRoot();

        if (startupCalibrationRoot == null)
        {
            Debug.LogWarning("UIBootSequence: startup calibration requested, but VenueRoot was not found.");
            yield break;
        }

        startupCalibrationConfirmed = false;
        startupCalibrationRoot.SetActive(true);
        SetStartupCalibrationVisualsActive(true);

        if (logStartupCalibration)
            Debug.Log("UIBootSequence: waiting for VenueControllerCalibrationInput to confirm startup calibration.");

        while (!startupCalibrationConfirmed)
            yield return null;

        SetStartupCalibrationVisualsActive(false);
        onStartupCalibrationConfirmed?.Invoke();
    }

    [ContextMenu("Confirm Startup Calibration")]
    public void ConfirmStartupCalibration()
    {
        ConfirmStartupCalibration("manual call");
    }

    public void ConfirmStartupCalibrationFromUi()
    {
        ConfirmStartupCalibration("UI event");
    }

    public void ConfirmStartupCalibrationFromCalibration()
    {
        ConfirmStartupCalibration("venue calibration");
    }

    private void ConfirmStartupCalibration(string source)
    {
        if (startupCalibrationConfirmed)
            return;

        startupCalibrationConfirmed = true;

        if (logStartupCalibration)
            Debug.Log("UIBootSequence: startup calibration confirmed by " + source + ".");
    }

    private void HideMainCanvasWithoutShowingIt()
    {
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
            mainCanvasGroup.interactable = false;
            mainCanvasGroup.blocksRaycasts = false;
        }

        if (mainCanvasRoot != null)
            mainCanvasRoot.SetActive(false);
    }

    private void HideLogoWithoutShowingIt()
    {
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
            logoCanvasGroup.interactable = false;
            logoCanvasGroup.blocksRaycasts = false;
        }

        if (logoRoot != null)
            logoRoot.SetActive(false);
    }

    private void ShowMainCanvasSafely()
    {
        ResolveMainCanvasReferences();

        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
            mainCanvasGroup.interactable = false;
            mainCanvasGroup.blocksRaycasts = false;
        }

        if (mainCanvasRoot != null)
            mainCanvasRoot.SetActive(true);

        if (mainCanvasFollow != null)
            mainCanvasFollow.ForceSnapNow();

        Canvas.ForceUpdateCanvases();

        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 1f;
            mainCanvasGroup.interactable = true;
            mainCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator FadeLogo(float from, float to, float duration)
    {
        if (logoCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            logoCanvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float smoothT = t * t * (3f - 2f * t);
            logoCanvasGroup.alpha = Mathf.Lerp(from, to, smoothT);
            yield return null;
        }

        logoCanvasGroup.alpha = to;
    }

    private void PlaceLogoInFrontOfUser()
    {
        if (centerEyeAnchor == null || logoRoot == null)
            return;

        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = centerEyeAnchor.forward;

        forward.Normalize();

        Vector3 targetPosition = centerEyeAnchor.position
                               + forward * logoDistance
                               + Vector3.up * logoHeightOffset;

        logoRoot.transform.position = targetPosition;

        Vector3 lookDirection = logoRoot.transform.position - centerEyeAnchor.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
            logoRoot.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    private void ResolveMainCanvasReferences()
    {
        if (mainCanvasRoot == null)
            return;

        if (mainCanvasFollow == null)
            mainCanvasFollow = mainCanvasRoot.GetComponent<CanvasFollowHead>();

        if (mainCanvasFollow == null)
            mainCanvasFollow = mainCanvasRoot.GetComponentInChildren<CanvasFollowHead>(true);

        if (mainCanvasGroup == null)
            mainCanvasGroup = mainCanvasRoot.GetComponent<CanvasGroup>();

        if (mainCanvasGroup == null)
            mainCanvasGroup = mainCanvasRoot.GetComponentInChildren<CanvasGroup>(true);
    }

    private void ResolveStartupCalibrationRoot()
    {
        if (startupCalibrationRoot != null || string.IsNullOrWhiteSpace(startupCalibrationRootName))
        {
            ResolveStartupCalibrationVisualRoots();
            return;
        }

        startupCalibrationRoot = FindSceneObjectByName(startupCalibrationRootName);
        ResolveStartupCalibrationVisualRoots();
    }

    private void ResolveStartupCalibrationVisualRoots()
    {
        resolvedStartupCalibrationVisualRoots.Clear();

        if (startupCalibrationVisualRoots != null)
        {
            for (int i = 0; i < startupCalibrationVisualRoots.Length; i++)
                AddResolvedVisualRoot(startupCalibrationVisualRoots[i]);
        }

        if (startupCalibrationVisualRootNames == null)
            return;

        for (int i = 0; i < startupCalibrationVisualRootNames.Length; i++)
        {
            string rootName = startupCalibrationVisualRootNames[i];
            if (string.IsNullOrWhiteSpace(rootName))
                continue;

            AddResolvedVisualRoot(FindSceneObjectByName(rootName, startupCalibrationRoot));
        }
    }

    private void AddResolvedVisualRoot(GameObject visualRoot)
    {
        if (visualRoot != null && !resolvedStartupCalibrationVisualRoots.Contains(visualRoot))
            resolvedStartupCalibrationVisualRoots.Add(visualRoot);
    }

    private void SetStartupCalibrationVisualsActive(bool active)
    {
        if (resolvedStartupCalibrationVisualRoots.Count == 0 && startupCalibrationRoot != null)
        {
            startupCalibrationRoot.SetActive(active);
            return;
        }

        for (int i = 0; i < resolvedStartupCalibrationVisualRoots.Count; i++)
            resolvedStartupCalibrationVisualRoots[i].SetActive(active);
    }

    private static GameObject FindSceneObjectByName(string objectName, GameObject searchRoot = null)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        if (searchRoot != null)
        {
            Transform[] childTransforms = searchRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < childTransforms.Length; i++)
            {
                Transform child = childTransforms[i];
                if (child != null && child.name == objectName)
                    return child.gameObject;
            }
        }

        GameObject found = GameObject.Find(objectName);
        if (found != null)
            return found;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate != null && candidate.gameObject.scene.IsValid() && candidate.name == objectName)
                return candidate.gameObject;
        }

        return null;
    }
}
