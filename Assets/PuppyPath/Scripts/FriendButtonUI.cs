using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendButtonUI : MonoBehaviour
{
    [SerializeField] private string friendName;
    [SerializeField] private Image buttonBackground;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private PuppyPathSelectionUI selectionUI;
    [SerializeField] private string pathId;
    public string PathId => pathId;

    public string FriendName => string.IsNullOrEmpty(friendName) && nameText != null ? nameText.text : friendName;

    public void OnFriendClicked()
    {
        if (selectionUI != null)
            selectionUI.SelectFriend(this);
    }

    public void SetSelected(
        bool isSelected,
        Color normalButtonColor,
        Color selectedButtonColor,
        Color normalTextColor,
        Color selectedTextColor)
    {
        if (buttonBackground != null)
            buttonBackground.color = isSelected ? selectedButtonColor : normalButtonColor;

        if (nameText != null)
            nameText.color = isSelected ? selectedTextColor : normalTextColor;
    }
}