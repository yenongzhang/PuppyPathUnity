using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Independent, map-agnostic reward reveal API: ShowReward(attractionId) attaches the
/// matching accessory, plays a one-shot dog reaction, optionally spawns fireworks, and
/// shows a placeholder reward panel. Deliberately has no dependency on VenueMapDefinition
/// or AttractionDefinition so it can be tested and integrated independently of map work.
/// </summary>
public class RewardRevealController : MonoBehaviour
{
    [SerializeField] private List<RewardDefinition> rewards;
    [SerializeField] private DogAccessoryManager accessoryManager;
    [SerializeField] private DogGuideController dogGuideController;
    [SerializeField] private string rewardReactionState = "HappyStart";
    [SerializeField] private GameObject fireworkPrefab;
    [SerializeField] private GameObject rewardPanelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private float autoHideSeconds = 4f;

    private Coroutine autoHideRoutine;

    private void Awake()
    {
        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(false);
    }

    public void ShowReward(string attractionId)
    {
        RewardDefinition definition = FindReward(attractionId);

        if (definition == null)
        {
            Debug.LogWarning($"RewardRevealController: no RewardDefinition found for attractionId '{attractionId}'.");
            return;
        }

        if (definition.accessory != null && accessoryManager != null)
            accessoryManager.AttachAccessory(definition.accessory);

        if (dogGuideController != null)
            dogGuideController.PlayOneShotState(rewardReactionState);

        if (definition.triggerFireworks && fireworkPrefab != null)
        {
            Vector3 spawnPos = dogGuideController != null && dogGuideController.CurrentDog != null
                ? dogGuideController.CurrentDog.transform.position
                : transform.position;

            Instantiate(fireworkPrefab, spawnPos, Quaternion.identity);
        }

        ShowPanel(definition);
    }

    public void HideReward()
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(false);
    }

    private RewardDefinition FindReward(string attractionId)
    {
        if (rewards == null || string.IsNullOrEmpty(attractionId))
            return null;

        foreach (RewardDefinition reward in rewards)
        {
            if (reward != null && reward.attractionId == attractionId)
                return reward;
        }

        return null;
    }

    private void ShowPanel(RewardDefinition definition)
    {
        if (titleText != null)
            titleText.text = definition.rewardTitle;

        if (bodyText != null)
            bodyText.text = definition.rewardMessage;

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(true);

        if (autoHideRoutine != null)
            StopCoroutine(autoHideRoutine);

        if (autoHideSeconds > 0f)
            autoHideRoutine = StartCoroutine(AutoHideAfterDelay());
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        autoHideRoutine = null;

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(false);
    }
}
