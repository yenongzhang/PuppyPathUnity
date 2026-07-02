using UnityEngine;

/// <summary>
/// Describes one attraction's floating collectible item. itemPrefab should already carry
/// FloatingCollectibleItem plus the Meta XR Interaction SDK's Grabbable + HandGrabInteractable
/// components wired up in the Inspector.
/// </summary>
[CreateAssetMenu(menuName = "PuppyPath/V2/Collectible Item", fileName = "Collectible_")]
public class CollectibleItemDefinition : ScriptableObject
{
    public string attractionId;
    public string displayName;
    public GameObject itemPrefab;
    public float fullyVisibleDistance = 3f;
    public float halfVisibleDistance = 6f;
    public float hiddenDistance = 10f;
}
