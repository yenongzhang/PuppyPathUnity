using System.Collections;
using TMPro;
using UnityEngine;

public class PuppyPathV2FlowController : MonoBehaviour
{
    public enum AppState
    {
        Boot,
        Intro,
        FreeRoam,
        BigMap,
        Navigation,
        RewardPopup
    }

    [Header("Panels")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject freeRoamHud;
    [SerializeField] private GameObject bigMapPanel;
    [SerializeField] private GameObject navigationHud;
    [SerializeField] private GameObject rewardPopupPanel;

    [Header("References")]
    [SerializeField] private VenueMapUiController mapUiController;
    [SerializeField] private VenueNavigationRuntime navigationRuntime;

    [Header("Text")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string introText = "Hi! I'm PuppyPath.";
    [SerializeField] private string freeRoamText = "Free roam";
    [SerializeField] private string navigationText = "Follow PuppyPath";
    [SerializeField] private string rewardPopupText = "Reward collected";

    [Header("Reward Popup")]
    [SerializeField] private float rewardPopupAutoCloseSeconds = 10f;
    [SerializeField] private RectTransform rewardPopupShakeRoot;
    [SerializeField] private float rewardPopupShakeSeconds = 0.6f;
    [SerializeField] private float rewardPopupShakePixels = 10f;

    public AppState CurrentState { get; private set; } = AppState.Boot;

    private Coroutine rewardPopupRoutine;
    private Vector2 rewardPopupOriginalPosition;

    private void Start()
    {
        if (rewardPopupShakeRoot == null && rewardPopupPanel != null)
            rewardPopupShakeRoot = rewardPopupPanel.transform as RectTransform;

        if (rewardPopupShakeRoot != null)
            rewardPopupOriginalPosition = rewardPopupShakeRoot.anchoredPosition;

        EnterBoot();
    }

    public void EnterBoot()
    {
        StopRewardPopupRoutine();
        SetState(AppState.Boot);
    }

    public void EnterIntro()
    {
        StopRewardPopupRoutine();
        SetState(AppState.Intro);
    }

    public void ContinueFromIntro()
    {
        EnterFreeRoam();
    }

    public void EnterFreeRoam()
    {
        StopRewardPopupRoutine();

        if (navigationRuntime != null)
            navigationRuntime.StopNavigation();

        if (mapUiController != null)
            mapUiController.CloseLargeMap();

        SetState(AppState.FreeRoam);
    }

    public void OpenBigMap()
    {
        StopRewardPopupRoutine();

        if (mapUiController != null)
            mapUiController.OpenLargeMap();

        SetState(AppState.BigMap);
    }

    public void CloseBigMap()
    {
        if (mapUiController != null)
            mapUiController.CloseLargeMap();

        SetState(AppState.FreeRoam);
    }

    public void EnterNavigation()
    {
        StopRewardPopupRoutine();
        SetState(AppState.Navigation);
    }

    public void StopNavigation()
    {
        if (mapUiController != null)
            mapUiController.CancelNavigation();
        else if (navigationRuntime != null)
            navigationRuntime.StopNavigation();

        SetState(AppState.FreeRoam);
    }

    public void MarkArrived()
    {
        ShowRewardPopup();
    }

    public void EnterItemGrab()
    {
        SetState(AppState.FreeRoam);
    }

    public void ShowRewardPopup()
    {
        if (navigationRuntime != null)
            navigationRuntime.StopNavigation();

        if (mapUiController != null)
            mapUiController.CloseLargeMap();

        SetState(AppState.RewardPopup);

        StopRewardPopupRoutine();
        rewardPopupRoutine = StartCoroutine(RewardPopupSequence());
    }

    public void CloseRewardPopup()
    {
        StopRewardPopupRoutine();
        SetState(AppState.FreeRoam);
    }

    private IEnumerator RewardPopupSequence()
    {
        yield return ShakeRewardPopup();

        if (rewardPopupAutoCloseSeconds > 0f)
            yield return new WaitForSeconds(rewardPopupAutoCloseSeconds);

        rewardPopupRoutine = null;
        SetState(AppState.FreeRoam);
    }

    private IEnumerator ShakeRewardPopup()
    {
        if (rewardPopupShakeRoot == null || rewardPopupShakeSeconds <= 0f || rewardPopupShakePixels <= 0f)
            yield break;

        rewardPopupOriginalPosition = rewardPopupShakeRoot.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < rewardPopupShakeSeconds)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / rewardPopupShakeSeconds);
            float offsetX = Mathf.Sin(elapsed * 80f) * rewardPopupShakePixels * decay;
            float offsetY = Mathf.Sin(elapsed * 57f) * rewardPopupShakePixels * 0.35f * decay;
            rewardPopupShakeRoot.anchoredPosition = rewardPopupOriginalPosition + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rewardPopupShakeRoot.anchoredPosition = rewardPopupOriginalPosition;
    }

    private void StopRewardPopupRoutine()
    {
        if (rewardPopupRoutine != null)
        {
            StopCoroutine(rewardPopupRoutine);
            rewardPopupRoutine = null;
        }

        if (rewardPopupShakeRoot != null)
            rewardPopupShakeRoot.anchoredPosition = rewardPopupOriginalPosition;
    }

    private void SetState(AppState state)
    {
        CurrentState = state;

        SetActive(introPanel, state == AppState.Intro);
        SetActive(freeRoamHud, state == AppState.FreeRoam || state == AppState.BigMap || state == AppState.Navigation || state == AppState.RewardPopup);
        SetActive(bigMapPanel, state == AppState.BigMap);
        SetActive(navigationHud, state == AppState.Navigation);
        SetActive(rewardPopupPanel, state == AppState.RewardPopup);

        UpdateStatusText(state);
    }

    private void UpdateStatusText(AppState state)
    {
        if (statusText == null)
            return;

        switch (state)
        {
            case AppState.Intro:
                statusText.text = introText;
                break;
            case AppState.Navigation:
                statusText.text = navigationText;
                break;
            case AppState.RewardPopup:
                statusText.text = rewardPopupText;
                break;
            case AppState.FreeRoam:
            case AppState.BigMap:
            default:
                statusText.text = freeRoamText;
                break;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
