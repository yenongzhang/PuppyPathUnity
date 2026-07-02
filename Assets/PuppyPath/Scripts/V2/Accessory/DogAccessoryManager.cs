using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attaches/detaches DogAccessoryDefinition prefabs onto the currently spawned dog's
/// DogAccessoryAnchors. Can pick up the dog automatically via DogGuideController.DogSpawned,
/// or be pointed at a DogAccessoryAnchors manually for standalone testing.
/// </summary>
public class DogAccessoryManager : MonoBehaviour
{
    [SerializeField] private DogGuideController dogGuideController;
    [SerializeField] private DogAccessoryAnchors manualAnchors;
    [SerializeField] private DogAccessoryDefinition contextMenuTestAccessory;

    private DogAccessoryAnchors currentAnchors;
    private readonly Dictionary<string, GameObject> attached = new();

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
        currentAnchors = dog != null ? dog.GetComponentInChildren<DogAccessoryAnchors>() : null;

        if (currentAnchors == null)
            Debug.LogWarning("DogAccessoryManager: spawned dog has no DogAccessoryAnchors component.");
    }

    public bool HasAccessory(string id)
    {
        return !string.IsNullOrEmpty(id) && attached.ContainsKey(id);
    }

    public void AttachAccessory(DogAccessoryDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
        {
            Debug.LogWarning("DogAccessoryManager: cannot attach a null/unidentified accessory definition.");
            return;
        }

        if (currentAnchors == null)
        {
            Debug.LogWarning("DogAccessoryManager: no dog anchors available, cannot attach accessory.");
            return;
        }

        if (definition.accessoryPrefab == null)
        {
            Debug.LogWarning($"DogAccessoryManager: accessory '{definition.id}' has no prefab assigned.");
            return;
        }

        DetachAccessory(definition.id);

        Transform anchor = currentAnchors.GetAnchor(definition.slot);
        GameObject instance = Instantiate(definition.accessoryPrefab, anchor);
        instance.transform.localPosition = definition.localPositionOffset;
        instance.transform.localRotation = Quaternion.Euler(definition.localEulerOffset);
        instance.transform.localScale = definition.localScale;

        attached[definition.id] = instance;
    }

    public void DetachAccessory(string id)
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

    public void ClearAll()
    {
        foreach (KeyValuePair<string, GameObject> entry in attached)
        {
            if (entry.Value != null)
                Destroy(entry.Value);
        }

        attached.Clear();
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
