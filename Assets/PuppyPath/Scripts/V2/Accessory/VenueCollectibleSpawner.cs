using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VenueCollectibleSpawner : MonoBehaviour
{
    [Serializable]
    public class Placement
    {
        public string venueAttractionId;
        public RewardDefinition reward;
    }

    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform venueContentRoot;
    [SerializeField] private Transform userReference;
    [SerializeField] private Transform collectibleParent;
    [SerializeField] private RewardRevealController rewardRevealController;
    [SerializeField] private DogGuideController dogGuideController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private VenueMapUiController mapUiController;

    [Header("Rewards")]
    [SerializeField] private List<RewardDefinition> rewards = new List<RewardDefinition>();
    [SerializeField] private List<Placement> explicitPlacements = new List<Placement>();
    [SerializeField] private bool cycleRewardsAcrossAttractions = true;

    [Header("Spawn")]
    [SerializeField] private bool rebuildOnStart = true;
    [SerializeField] private float heightOffset = 0.45f;
    [SerializeField] private Vector3 spawnedLocalScale = Vector3.one;
    [SerializeField] private bool addColliderIfMissing = true;
    [SerializeField] private bool makeGeneratedCollidersTriggers = true;
    [SerializeField] private string spawnedNamePrefix = "VenueCollectible_";

    [Header("Collectible Visual")]
    [SerializeField] private GameObject collectibleVisualPrefab;
    [SerializeField] private float collectibleTargetHeightMeters = 0.4f;
    [SerializeField] private bool useTestBallVisual;
    [SerializeField] private float testBallDiameter = 0.28f;
    [SerializeField] private Color testBallColor = new Color(1f, 0.64f, 0.12f, 1f);

    [Header("Auto Discovery")]
    [SerializeField] private bool enableAutoDiscovery = true;
    [SerializeField] private bool alwaysShowGiftVisual = true;
    [SerializeField] private float discoveryDistance = 1.0f;
    [SerializeField] private bool useFlatDiscoveryDistance = true;
    [SerializeField] private float dogStopDistance = 0.18f;
    [SerializeField] private float hudMessageSeconds = 3.0f;
    [SerializeField] private float maxDogWalkSeconds = 10.0f;
    [SerializeField] private string dogTreasureArrivalState = "";

    private readonly List<SpawnedCollectible> spawnedCollectibles = new List<SpawnedCollectible>();

    private class SpawnedCollectible
    {
        public Transform spawnPoint;
        public AttractionDefinition attraction;
        public RewardDefinition reward;
        public FloatingCollectibleItem item;
        public CollectibleGrabHandler grabHandler;
        public bool discoveryStarted;
    }

    private void Start()
    {
        if (mapUiController == null)
            mapUiController = FindFirstObjectByType<VenueMapUiController>();

        if (rebuildOnStart)
            StartCoroutine(RebuildWhenVenueReady());
    }

    private IEnumerator RebuildWhenVenueReady()
    {
        yield return null;
        RebuildCollectibles();
    }

    private void Update()
    {
        if (userReference == null)
            return;

        foreach (SpawnedCollectible spawned in spawnedCollectibles)
        {
            if (spawned == null || spawned.item == null || spawned.attraction == null || spawned.spawnPoint == null)
                continue;

            float distance = useFlatDiscoveryDistance
                ? GetFlatDistance(userReference.position, spawned.spawnPoint.position)
                : Vector3.Distance(userReference.position, spawned.spawnPoint.position);

            if (alwaysShowGiftVisual)
                spawned.item.SetVisibility(1f);
            else
                spawned.item.SetVisibility(ComputeAlpha(spawned.attraction, distance));

            if (enableAutoDiscovery &&
                !spawned.discoveryStarted &&
                spawned.grabHandler != null &&
                !spawned.grabHandler.IsCollected &&
                distance <= discoveryDistance)
            {
                TryStartDiscovery(spawned);
            }
        }
    }

    public bool TryStartDiscoveryForAttraction(string venueAttractionId)
    {
        if (string.IsNullOrWhiteSpace(venueAttractionId))
            return false;

        foreach (SpawnedCollectible spawned in spawnedCollectibles)
        {
            if (spawned == null || spawned.attraction == null || spawned.grabHandler == null)
                continue;

            if (spawned.discoveryStarted || spawned.grabHandler.IsCollected)
                continue;

            if (!string.Equals(spawned.attraction.id, venueAttractionId, StringComparison.Ordinal))
                continue;

            TryStartDiscovery(spawned);
            return true;
        }

        return false;
    }

    private void TryStartDiscovery(SpawnedCollectible spawned)
    {
        if (spawned == null || spawned.discoveryStarted || spawned.grabHandler == null || spawned.grabHandler.IsCollected)
            return;

        spawned.discoveryStarted = true;
        BeginTreasureRevealSequence();
        StartCoroutine(DiscoverCollectibleRoutine(spawned));
    }

    private void BeginTreasureRevealSequence()
    {
        if (hudController != null)
            hudController.BeginTreasureRevealSequence();
    }

    [ContextMenu("Rebuild Venue Collectibles")]
    public void RebuildCollectibles()
    {
        ClearCollectibles();

        if (mapDefinition == null || mapDefinition.attractions == null)
            return;

        Transform parent = GetCollectibleParent();
        Dictionary<string, RewardDefinition> explicitByVenue = BuildExplicitPlacementLookup();
        int rewardIndex = 0;
        int spawnedCount = 0;

        foreach (AttractionDefinition attraction in mapDefinition.attractions)
        {
            if (attraction == null || string.IsNullOrWhiteSpace(attraction.id))
                continue;

            if (!HasCollectibleSpawnPoint(attraction))
                continue;

            if (CollectibleGrabHandler.IsVenueAttractionCollected(attraction.id))
                continue;

            RewardDefinition reward = ResolveRewardForAttraction(attraction.id, explicitByVenue, ref rewardIndex);
            if (reward == null)
                continue;

            SpawnCollectible(attraction, reward, parent);
            spawnedCount++;
        }

        Debug.Log($"VenueCollectibleSpawner: spawned {spawnedCount} gift collectibles.");
    }

    private static bool HasCollectibleSpawnPoint(AttractionDefinition attraction)
    {
        return attraction != null && attraction.collectibleSpawnPixel.sqrMagnitude > 0.0001f;
    }

    private Dictionary<string, RewardDefinition> BuildExplicitPlacementLookup()
    {
        Dictionary<string, RewardDefinition> lookup = new Dictionary<string, RewardDefinition>();

        if (explicitPlacements == null)
            return lookup;

        foreach (Placement placement in explicitPlacements)
        {
            if (placement == null || placement.reward == null || string.IsNullOrWhiteSpace(placement.venueAttractionId))
                continue;

            lookup[placement.venueAttractionId] = placement.reward;
        }

        return lookup;
    }

    private RewardDefinition ResolveRewardForAttraction(
        string venueAttractionId,
        Dictionary<string, RewardDefinition> explicitByVenue,
        ref int rewardIndex)
    {
        if (explicitByVenue.TryGetValue(venueAttractionId, out RewardDefinition explicitReward))
            return explicitReward;

        if (rewards == null || rewards.Count == 0)
            return null;

        if (!cycleRewardsAcrossAttractions && rewardIndex >= rewards.Count)
            return null;

        RewardDefinition reward = rewards[rewardIndex % rewards.Count];
        rewardIndex++;
        return reward;
    }

    [ContextMenu("Clear Venue Collectibles")]
    public void ClearCollectibles()
    {
        Transform parent = GetCollectibleParent();
        if (parent != null)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name.StartsWith(spawnedNamePrefix, StringComparison.Ordinal))
                    DestroySpawned(child.gameObject);
            }
        }

        for (int i = spawnedCollectibles.Count - 1; i >= 0; i--)
        {
            SpawnedCollectible spawned = spawnedCollectibles[i];
            if (spawned != null && spawned.spawnPoint != null)
                DestroySpawned(spawned.spawnPoint.gameObject);
        }

        spawnedCollectibles.Clear();
    }

    private void SpawnExplicitPlacement(Placement placement, Transform parent)
    {
        if (placement == null || placement.reward == null || string.IsNullOrWhiteSpace(placement.venueAttractionId))
            return;

        if (CollectibleGrabHandler.IsVenueAttractionCollected(placement.venueAttractionId))
            return;

        AttractionDefinition attraction = mapDefinition.FindAttraction(placement.venueAttractionId);
        SpawnCollectible(attraction, placement.reward, parent);
    }

    private void SpawnCollectible(AttractionDefinition attraction, RewardDefinition reward, Transform parent)
    {
        if (attraction == null || reward == null)
            return;

        GameObject root = new GameObject(spawnedNamePrefix + attraction.id + "_" + reward.attractionId);
        root.transform.SetParent(parent, false);
        root.transform.position = VenueLocalToWorld(mapDefinition.MapPixelToWorld(attraction.collectibleSpawnPixel)) + Vector3.up * heightOffset;

        GameObject item = CreateVisual(reward, root.transform);
        if (item == null)
        {
            DestroySpawned(root);
            return;
        }

        EnsureRaycastCollider(root);
        ConfigureColliders(root);
        FloatingCollectibleItem floatingItem = root.AddComponent<FloatingCollectibleItem>();
        CollectibleGrabHandler grabHandler = root.AddComponent<CollectibleGrabHandler>();
        grabHandler.Configure(
            reward.attractionId,
            attraction.id,
            rewardRevealController,
            dogGuideController,
            mapUiController);

        spawnedCollectibles.Add(new SpawnedCollectible
        {
            spawnPoint = root.transform,
            attraction = attraction,
            reward = reward,
            item = floatingItem,
            grabHandler = grabHandler
        });
    }

    private IEnumerator DiscoverCollectibleRoutine(SpawnedCollectible spawned)
    {
        if (spawned == null || spawned.spawnPoint == null || spawned.grabHandler == null)
        {
            if (hudController != null)
                hudController.EndTreasureRevealSequence();
            yield break;
        }

        string placeName = spawned.attraction != null && !string.IsNullOrWhiteSpace(spawned.attraction.displayName)
            ? spawned.attraction.displayName
            : spawned.attraction != null ? spawned.attraction.id : "a secret spot";

        if (hudController != null)
            hudController.ShowTreasureFoundMessage(placeName);

        bool dogWalkFinished = dogGuideController == null || dogGuideController.CurrentDog == null;
        if (!dogWalkFinished)
            StartCoroutine(RunDogWalkThenFlag(spawned.spawnPoint.position, () => dogWalkFinished = true));

        float dogWalkElapsed = 0f;
        while (!dogWalkFinished && dogWalkElapsed < maxDogWalkSeconds)
        {
            dogWalkElapsed += Time.deltaTime;
            yield return null;
        }

        spawned.grabHandler.TryCollectEffectsOnly();

        float hudElapsed = 0f;
        while (hudElapsed < hudMessageSeconds)
        {
            hudElapsed += Time.deltaTime;
            yield return null;
        }

        if (rewardRevealController != null && !string.IsNullOrEmpty(spawned.grabHandler.AttractionId))
            rewardRevealController.ShowRewardPanel(spawned.grabHandler.AttractionId);

        if (dogGuideController != null)
            dogGuideController.SetInteractionHold(false);
    }

    private IEnumerator RunDogWalkThenFlag(Vector3 targetPosition, System.Action onFinished)
    {
        string arrivalState = string.IsNullOrWhiteSpace(dogTreasureArrivalState) ? null : dogTreasureArrivalState;
        yield return dogGuideController.WalkToInteractionTarget(
            targetPosition,
            dogStopDistance,
            arrivalState,
            maxDogWalkSeconds);

        onFinished?.Invoke();
    }

    private GameObject CreateVisual(RewardDefinition reward, Transform parent)
    {
        if (collectibleVisualPrefab != null)
        {
            GameObject item = Instantiate(collectibleVisualPrefab, parent);
            item.name = "GiftVisual_" + reward.attractionId;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            FitUniformHeight(item, collectibleTargetHeightMeters);
            item.AddComponent<CollectibleGiftHover>();
            return item;
        }

        if (useTestBallVisual)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.name = "CollectibleBall_" + reward.attractionId;
            item.transform.SetParent(parent, false);
            item.transform.localScale = Vector3.one * testBallDiameter;
            ApplyTestBallMaterial(item);
            return item;
        }

        if (reward.accessory == null || reward.accessory.accessoryPrefab == null)
            return null;

        GameObject accessoryItem = Instantiate(reward.accessory.accessoryPrefab, parent);
        accessoryItem.name = "ItemVisual_" + reward.attractionId;
        accessoryItem.transform.localPosition = Vector3.zero;
        accessoryItem.transform.localRotation = Quaternion.identity;
        accessoryItem.transform.localScale = spawnedLocalScale;
        FitUniformHeight(accessoryItem, collectibleTargetHeightMeters);
        return accessoryItem;
    }

    private static void FitUniformHeight(GameObject visualRoot, float targetHeightMeters)
    {
        if (visualRoot == null || targetHeightMeters <= 0f)
            return;

        visualRoot.transform.localScale = Vector3.one;

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float height = bounds.size.y;
        if (height <= 0.0001f)
            return;

        float scaleFactor = targetHeightMeters / height;
        visualRoot.transform.localScale = Vector3.one * scaleFactor;
    }

    private void ApplyTestBallMaterial(GameObject item)
    {
        Renderer renderer = item != null ? item.GetComponentInChildren<Renderer>() : null;
        if (renderer == null)
            return;

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", testBallColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", testBallColor);

        renderer.sharedMaterial = material;
    }

    private void EnsureRaycastCollider(GameObject root)
    {
        if (!addColliderIfMissing || root.GetComponentInChildren<Collider>() != null)
            return;

        Bounds bounds = new Bounds(root.transform.position, Vector3.one * 0.25f);
        bool hasBounds = false;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = makeGeneratedCollidersTriggers;
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private void ConfigureColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
                collider.isTrigger = makeGeneratedCollidersTriggers;
        }
    }

    private float ComputeAlpha(AttractionDefinition attraction, float distance)
    {
        if (distance <= attraction.fullAlphaDistanceMeters)
            return 1f;

        if (distance <= attraction.halfAlphaDistanceMeters)
        {
            float t = Mathf.InverseLerp(attraction.fullAlphaDistanceMeters, attraction.halfAlphaDistanceMeters, distance);
            return Mathf.Lerp(1f, 0.5f, t);
        }

        if (distance <= attraction.hiddenDistanceMeters)
        {
            float t = Mathf.InverseLerp(attraction.halfAlphaDistanceMeters, attraction.hiddenDistanceMeters, distance);
            return Mathf.Lerp(0.5f, 0f, t);
        }

        return 0f;
    }

    private Transform GetCollectibleParent()
    {
        if (collectibleParent != null)
            return collectibleParent;

        return venueContentRoot != null ? venueContentRoot : transform;
    }

    private Vector3 VenueLocalToWorld(Vector3 venueLocalPosition)
    {
        return venueContentRoot != null ? venueContentRoot.TransformPoint(venueLocalPosition) : venueLocalPosition;
    }

    private static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void DestroySpawned(GameObject spawned)
    {
        if (spawned == null)
            return;

        if (Application.isPlaying)
            Destroy(spawned);
        else
            DestroyImmediate(spawned);
    }
}
