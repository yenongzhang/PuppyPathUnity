using UnityEngine;

/// <summary>
/// Descriptive label for where on the dog an accessory conceptually belongs. Kept for
/// authoring clarity (data assets can say "this is a head item"), but per design direction
/// accessories attach to the dog's overall root, not to per-slot bones, so this no longer
/// drives a lookup to a specific bone Transform.
/// </summary>
public enum DogAccessorySlot
{
    Head,
    Face,
    Neck,
    Back,
    Tail
}

/// <summary>
/// Marks the shared attachment point for dog accessories: the dog's overall root, not any
/// individual bone. Accessories therefore move/turn with the dog's overall body but do not
/// follow per-bone animation detail (head bob, tail wag, etc). Per-accessory placement is
/// handled by DogAccessoryDefinition's local position/rotation offsets.
/// </summary>
public class DogAccessoryAnchors : MonoBehaviour
{
    [Tooltip("Optional override for the shared accessory attachment point. Leave empty to use this GameObject's own transform (the dog root).")]
    [SerializeField] private Transform rootAnchor;

    public Transform GetAnchor(DogAccessorySlot slot)
    {
        return rootAnchor != null ? rootAnchor : transform;
    }
}
