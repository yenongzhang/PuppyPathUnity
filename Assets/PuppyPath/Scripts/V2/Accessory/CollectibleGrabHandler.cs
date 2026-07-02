using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Sits on a collectible item prefab alongside a Meta XR Interaction SDK Grabbable
/// (+ HandGrabInteractable). On release, checks whether the item was dropped near the dog and,
/// if so, marks the attraction collected for the current session and fires the reward flow.
/// </summary>
public class CollectibleGrabHandler : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private FloatingCollectibleItem floatingItem;
    [SerializeField] private float collectDistance = 0.6f;

    private static readonly HashSet<string> collectedThisSession = new();

    private string attractionId;
    private RewardRevealController rewardRevealController;
    private DogGuideController dogGuideController;
    private bool collected;
    public bool IsCollected => collected;

    private void Awake()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();

        if (floatingItem == null)
            floatingItem = GetComponent<FloatingCollectibleItem>();
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    public void Configure(string newAttractionId, RewardRevealController newRewardRevealController, DogGuideController newDogGuideController)
    {
        attractionId = newAttractionId;
        rewardRevealController = newRewardRevealController;
        dogGuideController = newDogGuideController;

        if (!string.IsNullOrEmpty(attractionId) && collectedThisSession.Contains(attractionId) && floatingItem != null)
        {
            collected = true;
            floatingItem.SetCollected(true);
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Unselect)
            TryCollectAt(evt.Pose.position);
    }

    public bool TryCollectNow()
    {
        return TryCollectAt(transform.position);
    }

    public bool TryCollectAt(Vector3 releasePosition)
    {
        if (collected || string.IsNullOrEmpty(attractionId))
            return false;

        GameObject dog = dogGuideController != null ? dogGuideController.CurrentDog : null;

        if (dog == null)
            return false;

        float distance = Vector3.Distance(releasePosition, dog.transform.position);

        if (distance > collectDistance)
            return false;

        collected = true;
        collectedThisSession.Add(attractionId);

        if (floatingItem != null)
            floatingItem.SetCollected(true);

        if (rewardRevealController != null)
            rewardRevealController.ShowReward(attractionId);

        return true;
    }

    public bool TryCollectAutomatically()
    {
        if (collected || string.IsNullOrEmpty(attractionId))
            return false;

        collected = true;
        collectedThisSession.Add(attractionId);

        if (floatingItem != null)
            floatingItem.SetCollected(true);

        if (rewardRevealController != null)
            rewardRevealController.ShowReward(attractionId);

        return true;
    }
}
