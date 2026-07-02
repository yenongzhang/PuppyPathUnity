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
    [SerializeField] private string freeRoamText = "随便逛逛，探索一下吧";
    [SerializeField] private string navigationTextTemplate = "正在前往 {0} 中";
    [SerializeField] private string fallbackNavigationTargetName = "目的地";
    [SerializeField] private string rewardPopupText = "Reward collected";

    [Header("Reward Popup")]
    [SerializeField] private float rewardPopupAutoCloseSeconds = 10f;
    [SerializeField] private RectTransform rewardPopupShakeRoot;
    [SerializeField] private float rewardPopupShakeSeconds = 0.6f;
    [SerializeField] private float rewardPopupShakePixels = 10f;

    public AppState CurrentState { get; private set; } = AppState.Boot;

    private Coroutine rewardPopupRoutine;
    private Vector2 rewardPopupOriginalPosition;
    private string currentNavigationTargetName;

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

    public void CloseIntroAndMap()
    {
        EnterFreeRoam();
    }

    public void EnterFreeRoam()
    {
        StopRewardPopupRoutine();
        currentNavigationTargetName = null;

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

        if (CurrentState == AppState.Navigation)
        {
            SetActive(bigMapPanel, true);
            return;
        }

        SetState(AppState.BigMap);
    }

    public void CloseBigMap()
    {
        if (mapUiController != null)
            mapUiController.CloseLargeMap();

        if (CurrentState == AppState.Navigation)
        {
            SetActive(bigMapPanel, false);
            return;
        }

        SetState(AppState.FreeRoam);
    }

    public void EnterNavigation()
    {
        EnterNavigation(null);
    }

    public void EnterNavigation(string targetDisplayName)
    {
        StopRewardPopupRoutine();
        currentNavigationTargetName = string.IsNullOrWhiteSpace(targetDisplayName)
            ? fallbackNavigationTargetName
            : targetDisplayName;

        if (mapUiController != null)
            mapUiController.CloseLargeMap();

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

        bool introAndMapOpen = state == AppState.Intro || state == AppState.BigMap;
        SetActive(introPanel, introAndMapOpen);
        SetActive(freeRoamHud, false);
        SetActive(bigMapPanel, introAndMapOpen);
        SetActive(navigationHud, state == AppState.FreeRoam || state == AppState.Navigation);
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
                statusText.text = string.Format(navigationTextTemplate, string.IsNullOrWhiteSpace(currentNavigationTargetName)
                    ? fallbackNavigationTargetName
                    : currentNavigationTargetName);
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
