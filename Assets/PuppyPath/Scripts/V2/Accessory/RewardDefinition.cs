using UnityEngine;

[CreateAssetMenu(menuName = "PuppyPath/V2/Reward Definition", fileName = "Reward_")]
public class RewardDefinition : ScriptableObject
{
    public string attractionId;
    public string displayName;
    public DogAccessoryDefinition accessory;
    public string rewardTitle;
    [TextArea] public string rewardMessage;
    public Sprite rewardIcon;
    public bool triggerFireworks;
}
