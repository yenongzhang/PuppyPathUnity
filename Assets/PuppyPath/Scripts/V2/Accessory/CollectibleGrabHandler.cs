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

    private static readonly HashSet<string> collectedRewardIdsThisSession = new();
    private static readonly HashSet<string> collectedVenueAttractionIdsThisSession = new();

    private string rewardAttractionId;
    private string venueAttractionId;
    private RewardRevealController rewardRevealController;
    private DogGuideController dogGuideController;
    private VenueMapUiController mapUiController;
    private bool collected;

    public bool IsCollected => collected;
    public string AttractionId => rewardAttractionId;
    public string VenueAttractionId => venueAttractionId;

    public static bool IsVenueAttractionCollected(string venueId)
    {
        return !string.IsNullOrEmpty(venueId) && collectedVenueAttractionIdsThisSession.Contains(venueId);
    }

    public static IReadOnlyCollection<string> GetCollectedVenueAttractionIds()
    {
        return collectedVenueAttractionIdsThisSession;
    }

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

    public void Configure(
        string newRewardAttractionId,
        string newVenueAttractionId,
        RewardRevealController newRewardRevealController,
        DogGuideController newDogGuideController,
        VenueMapUiController newMapUiController = null)
    {
        rewardAttractionId = newRewardAttractionId;
        venueAttractionId = newVenueAttractionId;
        rewardRevealController = newRewardRevealController;
        dogGuideController = newDogGuideController;
        mapUiController = newMapUiController;

        if (!string.IsNullOrEmpty(venueAttractionId) &&
            collectedVenueAttractionIdsThisSession.Contains(venueAttractionId) &&
            floatingItem != null)
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
        if (collected || string.IsNullOrEmpty(rewardAttractionId))
            return false;

        GameObject dog = dogGuideController != null ? dogGuideController.CurrentDog : null;

        if (dog == null)
            return false;

        float distance = Vector3.Distance(releasePosition, dog.transform.position);

        if (distance > collectDistance)
            return false;

        if (!TryCollectEffectsOnly())
            return false;

        if (rewardRevealController != null)
            rewardRevealController.ShowRewardPanel(rewardAttractionId);

        return true;
    }

    public bool TryCollectAutomatically()
    {
        if (!TryCollectEffectsOnly())
            return false;

        if (rewardRevealController != null)
            rewardRevealController.ShowRewardPanel(rewardAttractionId);

        return true;
    }

    public bool TryCollectEffectsOnly()
    {
        if (collected || string.IsNullOrEmpty(rewardAttractionId))
            return false;

        collected = true;

        if (!string.IsNullOrEmpty(rewardAttractionId))
            collectedRewardIdsThisSession.Add(rewardAttractionId);

        if (!string.IsNullOrEmpty(venueAttractionId))
            collectedVenueAttractionIdsThisSession.Add(venueAttractionId);

        if (floatingItem != null)
            floatingItem.SetCollected(true);

        if (mapUiController != null && !string.IsNullOrEmpty(venueAttractionId))
            mapUiController.MarkAttractionCollected(venueAttractionId);

        if (rewardRevealController != null)
            return rewardRevealController.ApplyRewardEffects(rewardAttractionId);

        return true;
    }
}
