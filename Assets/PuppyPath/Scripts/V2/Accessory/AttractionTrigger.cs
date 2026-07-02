using UnityEngine;

/// <summary>
/// Test-stage stand-in for a real venue-anchored attraction spawn point: this Transform's
/// position IS the collectible spawn point for now (not sourced from VenueMapDefinition).
/// Drives the floating item's distance-based fade and wires it up to the reward flow.
/// </summary>
public class AttractionTrigger : MonoBehaviour
{
    [SerializeField] private Transform userReference;
    [SerializeField] private CollectibleItemDefinition definition;
    [SerializeField] private FloatingCollectibleItem spawnedItem;
    [SerializeField] private RewardRevealController rewardRevealController;
    [SerializeField] private DogGuideController dogGuideController;

    private CollectibleGrabHandler spawnedGrabHandler;

    private void Start()
    {
        if (spawnedItem == null && definition != null && definition.itemPrefab != null)
        {
            GameObject instance = Instantiate(definition.itemPrefab, transform.position, transform.rotation);
            spawnedItem = instance.GetComponent<FloatingCollectibleItem>();
            spawnedGrabHandler = instance.GetComponent<CollectibleGrabHandler>();
        }
        else if (spawnedItem != null)
        {
            spawnedGrabHandler = spawnedItem.GetComponent<CollectibleGrabHandler>();
        }

        if (spawnedGrabHandler != null && definition != null)
        {
            spawnedGrabHandler.Configure(
                definition.attractionId,
                definition.attractionId,
                rewardRevealController,
                dogGuideController);
        }
    }

    private void Update()
    {
        if (userReference == null || spawnedItem == null || definition == null)
            return;

        float distance = Vector3.Distance(userReference.position, transform.position);
        spawnedItem.SetVisibility(ComputeAlpha(distance));
    }

    private float ComputeAlpha(float distance)
    {
        if (distance <= definition.fullyVisibleDistance)
            return 1f;

        if (distance <= definition.halfVisibleDistance)
        {
            float t = Mathf.InverseLerp(definition.fullyVisibleDistance, definition.halfVisibleDistance, distance);
            return Mathf.Lerp(1f, 0.5f, t);
        }

        if (distance <= definition.hiddenDistance)
        {
            float t = Mathf.InverseLerp(definition.halfVisibleDistance, definition.hiddenDistance, distance);
            return Mathf.Lerp(0.5f, 0f, t);
        }

        return 0f;
    }
}
