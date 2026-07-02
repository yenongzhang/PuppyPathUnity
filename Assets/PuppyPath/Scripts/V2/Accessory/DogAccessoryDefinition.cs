using UnityEngine;

[CreateAssetMenu(menuName = "PuppyPath/V2/Dog Accessory", fileName = "DogAccessory_")]
public class DogAccessoryDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public DogAccessorySlot slot;
    public GameObject accessoryPrefab;
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localEulerOffset = Vector3.zero;
    public Vector3 localScale = Vector3.one;
}
