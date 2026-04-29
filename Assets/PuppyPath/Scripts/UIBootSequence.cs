using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIBootSequence : MonoBehaviour
{
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
}
