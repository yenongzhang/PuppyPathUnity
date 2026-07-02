using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attaches/detaches DogAccessoryDefinition prefabs onto the currently spawned dog's
/// DogAccessoryAnchors. Can pick up the dog automatically via DogGuideController.DogSpawned,
/// or be pointed at a DogAccessoryAnchors manually for standalone testing.
/// Collected accessories persist for the current session and stack on the dog; new rewards
/// never remove previously collected items.
/// </summary>
public class DogAccessoryManager : MonoBehaviour
{
    [SerializeField] private DogGuideController dogGuideController;
    [SerializeField] private DogAccessoryAnchors manualAnchors;
    [SerializeField] private DogAccessoryDefinition contextMenuTestAccessory;

    private DogAccessoryAnchors currentAnchors;
    private GameObject currentDog;
    private readonly Dictionary<string, GameObject> attached = new();
    private readonly List<DogAccessoryDefinition> collectedDefinitions = new();

    private void Awake()
    {
        currentAnchors = manualAnchors;

        if (dogGuideController != null)
            dogGuideController.DogSpawned += HandleDogSpawned;
    }

    private void OnDestroy()
    {
        if (dogGuideController != null)
            dogGuideController.DogSpawned -= HandleDogSpawned;
    }

    private void HandleDogSpawned(GameObject dog)
    {
        bool dogChanged = currentDog != dog;
        currentDog = dog;
        currentAnchors = dog != null ? dog.GetComponentInChildren<DogAccessoryAnchors>() : null;

        if (currentAnchors == null && dog != null)
            Debug.LogWarning("DogAccessoryManager: spawned dog has no DogAccessoryAnchors component.");

        if (!dogChanged)
            return;

        attached.Clear();

        if (dog != null)
            RestoreCollectedAccessories();
    }

    public bool HasAccessory(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        for (int i = 0; i < collectedDefinitions.Count; i++)
        {
            DogAccessoryDefinition definition = collectedDefinitions[i];
            if (definition != null && definition.id == id)
                return true;
        }

        return false;
    }

    public void AttachAccessory(DogAccessoryDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
        {
            Debug.LogWarning("DogAccessoryManager: cannot attach a null/unidentified accessory definition.");
            return;
        }

        RegisterCollectedDefinition(definition);

        if (IsAttachedInstanceValid(definition.id))
            return;

        EnsureDogAnchors();

        if (currentAnchors == null)
        {
            Debug.LogWarning("DogAccessoryManager: no dog anchors available, cannot attach accessory.");
            return;
        }

        DetachAccessoryInstance(definition.id);
        SpawnAccessoryInstance(definition);
    }

    private void RegisterCollectedDefinition(DogAccessoryDefinition definition)
    {
        for (int i = 0; i < collectedDefinitions.Count; i++)
        {
            DogAccessoryDefinition existing = collectedDefinitions[i];
            if (existing != null && existing.id == definition.id)
                return;
        }

        collectedDefinitions.Add(definition);
    }

    private void RestoreCollectedAccessories()
    {
        for (int i = 0; i < collectedDefinitions.Count; i++)
        {
            DogAccessoryDefinition definition = collectedDefinitions[i];
            if (definition == null || string.IsNullOrEmpty(definition.id))
                continue;

            if (IsAttachedInstanceValid(definition.id))
                continue;

            DetachAccessoryInstance(definition.id);
            SpawnAccessoryInstance(definition);
        }
    }

    private void SpawnAccessoryInstance(DogAccessoryDefinition definition)
    {
        if (definition.accessoryPrefab == null)
        {
            Debug.LogWarning($"DogAccessoryManager: accessory '{definition.id}' has no prefab assigned.");
            return;
        }

        Transform anchor = currentAnchors.GetAnchor(definition.slot);
        GameObject instance = Instantiate(definition.accessoryPrefab, anchor);
        instance.transform.localPosition = definition.localPositionOffset;
        instance.transform.localRotation = Quaternion.Euler(definition.localEulerOffset);
        instance.transform.localScale = definition.localScale;

        RebindSkinnedMeshesToDogSkeleton(instance);

        attached[definition.id] = instance;
    }

    private bool IsAttachedInstanceValid(string id)
    {
        if (string.IsNullOrEmpty(id) || currentDog == null)
            return false;

        if (!attached.TryGetValue(id, out GameObject instance) || instance == null)
            return false;

        return instance.transform.IsChildOf(currentDog.transform);
    }

    private void EnsureDogAnchors()
    {
        if (currentAnchors != null && currentDog != null)
            return;

        if (manualAnchors != null)
        {
            currentAnchors = manualAnchors;
            return;
        }

        GameObject dog = dogGuideController != null ? dogGuideController.CurrentDog : null;
        if (dog == null)
            return;

        if (currentDog != dog)
            HandleDogSpawned(dog);

        if (currentAnchors == null)
        {
            currentAnchors = dog.AddComponent<DogAccessoryAnchors>();
            currentDog = dog;
            Debug.LogWarning("DogAccessoryManager: spawned dog had no DogAccessoryAnchors, so a root anchor was added at runtime.");
        }
    }

    /// <summary>
    /// Soft/deforming accessories (e.g. a hat or socks that bend with the dog's own
    /// animation) are exported with their own copy of the dog's skeleton. Unity's
    /// SkinnedMeshRenderer.bones array points at that copy, not the actually-animated
    /// dog instance, so without this the accessory mesh would sit static / bind-pose.
    /// This retargets each bone reference (by matching name) onto the corresponding bone
    /// Transform on the currently spawned dog, so the accessory deforms with it.
    /// Accessories with no SkinnedMeshRenderer (plain rigid props) are left untouched.
    /// </summary>
    private void RebindSkinnedMeshesToDogSkeleton(GameObject accessoryInstance)
    {
        if (currentDog == null)
            return;

        SkinnedMeshRenderer[] renderers = accessoryInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        if (renderers.Length == 0)
            return;

        Dictionary<string, Transform> dogBonesByName = new();

        foreach (Transform boneTransform in currentDog.GetComponentsInChildren<Transform>(true))
        {
            if (!dogBonesByName.ContainsKey(boneTransform.name))
                dogBonesByName[boneTransform.name] = boneTransform;
        }

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Transform[] originalBones = renderer.bones;
            Transform[] remappedBones = new Transform[originalBones.Length];
            bool allBonesMatched = true;

            for (int i = 0; i < originalBones.Length; i++)
            {
                if (originalBones[i] != null && dogBonesByName.TryGetValue(originalBones[i].name, out Transform matchingDogBone))
                {
                    remappedBones[i] = matchingDogBone;
                }
                else
                {
                    remappedBones[i] = originalBones[i];
                    allBonesMatched = false;
                }
            }

            renderer.bones = remappedBones;

            if (renderer.rootBone != null && dogBonesByName.TryGetValue(renderer.rootBone.name, out Transform matchingRootBone))
                renderer.rootBone = matchingRootBone;

            if (!allBonesMatched)
            {
                Debug.LogWarning(
                    $"DogAccessoryManager: some bones on '{renderer.name}' have no same-named bone on the " +
                    "dog's skeleton and will not follow its animation. Check that the accessory was exported " +
                    "with bone names matching the dog's GameRig."
                );
            }
        }
    }

    public void DetachAccessory(string id)
    {
        DetachAccessoryInstance(id);
        RemoveCollectedDefinition(id);
    }

    private void DetachAccessoryInstance(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        if (attached.TryGetValue(id, out GameObject instance))
        {
            if (instance != null)
                Destroy(instance);

            attached.Remove(id);
        }
    }

    private void RemoveCollectedDefinition(string id)
    {
        for (int i = collectedDefinitions.Count - 1; i >= 0; i--)
        {
            DogAccessoryDefinition definition = collectedDefinitions[i];
            if (definition != null && definition.id == id)
                collectedDefinitions.RemoveAt(i);
        }
    }

    public void ClearAll()
    {
        foreach (KeyValuePair<string, GameObject> entry in attached)
        {
            if (entry.Value != null)
                Destroy(entry.Value);
        }

        attached.Clear();
        collectedDefinitions.Clear();
    }

    [ContextMenu("Test Attach Configured Accessory")]
    private void TestAttachConfiguredAccessory()
    {
        AttachAccessory(contextMenuTestAccessory);
    }

    [ContextMenu("Test Detach Configured Accessory")]
    private void TestDetachConfiguredAccessory()
    {
        if (contextMenuTestAccessory != null)
            DetachAccessory(contextMenuTestAccessory.id);
    }
}
