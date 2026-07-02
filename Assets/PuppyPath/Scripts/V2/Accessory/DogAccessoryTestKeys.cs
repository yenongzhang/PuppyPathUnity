using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor/test-only keyboard shortcuts for exercising DogAccessoryManager and
/// RewardRevealController without a full XR input rig. Mirrors the style of
/// the existing DogStateTester / DogNavStateTester test scripts.
/// Uses the new Input System (this project has Active Input Handling set to
/// "Input System Package" only, so UnityEngine.Input throws at runtime).
/// </summary>
public class DogAccessoryTestKeys : MonoBehaviour
{
    [SerializeField] private DogAccessoryManager accessoryManager;
    [SerializeField] private RewardRevealController rewardRevealController;

    [System.Serializable]
    private class AccessoryKeyBinding
    {
        public Key key = Key.Digit1;
        public DogAccessoryDefinition accessory;
    }

    [System.Serializable]
    private class RewardKeyBinding
    {
        public Key key = Key.F1;
        public string attractionId;
    }

    [SerializeField] private AccessoryKeyBinding[] accessoryBindings;
    [SerializeField] private RewardKeyBinding[] rewardBindings;
    [SerializeField] private Key clearAllKey = Key.Digit0;
    [SerializeField] private Key hideRewardKey = Key.F12;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (accessoryManager != null && accessoryBindings != null)
        {
            foreach (AccessoryKeyBinding binding in accessoryBindings)
            {
                if (binding == null || binding.accessory == null)
                    continue;

                if (keyboard[binding.key].wasPressedThisFrame)
                {
                    if (accessoryManager.HasAccessory(binding.accessory.id))
                        accessoryManager.DetachAccessory(binding.accessory.id);
                    else
                        accessoryManager.AttachAccessory(binding.accessory);
                }
            }

            if (keyboard[clearAllKey].wasPressedThisFrame)
                accessoryManager.ClearAll();
        }

        if (rewardRevealController != null && rewardBindings != null)
        {
            foreach (RewardKeyBinding binding in rewardBindings)
            {
                if (binding == null || string.IsNullOrEmpty(binding.attractionId))
                    continue;

                if (keyboard[binding.key].wasPressedThisFrame)
                    rewardRevealController.ShowReward(binding.attractionId);
            }

            if (keyboard[hideRewardKey].wasPressedThisFrame)
                rewardRevealController.HideReward();
        }
    }
}
