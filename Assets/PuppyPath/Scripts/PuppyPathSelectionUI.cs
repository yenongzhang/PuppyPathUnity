using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PuppyPathSelectionUI : MonoBehaviour, IPointerClickHandler
{
    public enum SelectionType
    {
        None,
        Friend,
        Location
    }

    [Header("Map")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransform markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private Vector2 markerOffset = Vector2.zero;

    [Header("Intro Panel Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private TMP_Text hintText; // 选择后隐藏的第三句话

    [Header("Start Button")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private TMP_Text startButtonText;

    [Header("Initial Text")]
    [TextArea(2, 5)]
    [SerializeField] private string defaultTitle =
        "Hi, I'm PuppyPath.\n\nI'll have a little beagle take you wherever you want to go or help you find your friends.";

    [TextArea(2, 5)]
    [SerializeField] private string defaultHint =
        "Please select the location you want to visit on the map, or choose the friend you want to find from your friends list.";

    [Header("Selection Text")]
    [TextArea(2, 4)]
    [SerializeField] private string locationTitle =
        "Do you want to go to the location you marked?\nClick \"Start\" to get going right now";

    [TextArea(2, 4)]
    [SerializeField] private string friendTitleTemplate =
        "Do you want to go find {0}?\nClick \"Start\" to get going right now";

    [TextArea(2, 4)]
    [SerializeField] private string selectionNote =
        "P.S.: The puppy will lead the way. Keep an eye on its reactions\n-it'll get upset if you take a wrong turn :(";

    [Header("Friend Button Colors")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.62f, 0.45f, 1f);

    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.black;

    private RectTransform currentMarker;
    private SelectionType currentSelectionType = SelectionType.None;
    private FriendButtonUI currentSelectedFriend;

    private void Start()
    {
        if (markerParent == null && mapRect != null)
            markerParent = mapRect;

        ResetToDefaultState();
    }

    // ========= 地图点击 =========
    public void OnPointerClick(PointerEventData eventData)
    {
        if (mapRect == null) return;

        Vector2 localPoint;
        bool clicked = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        if (!clicked) return;

        SelectLocation(localPoint);
    }

    public void SelectLocation(Vector2 localPoint)
    {
        currentSelectionType = SelectionType.Location;

        ClearSelectedFriendVisual();
        PlaceMarker(localPoint);
        UpdateIntroForLocation();
        ShowStartButton();
    }

    // ========= 朋友点击 =========
    public void SelectFriend(FriendButtonUI friendUI)
    {
        if (friendUI == null) return;

        currentSelectionType = SelectionType.Friend;

        HideMarker();

        if (currentSelectedFriend != null && currentSelectedFriend != friendUI)
        {
            currentSelectedFriend.SetSelected(false, normalButtonColor, selectedButtonColor, normalTextColor, selectedTextColor);
        }

        currentSelectedFriend = friendUI;
        currentSelectedFriend.SetSelected(true, normalButtonColor, selectedButtonColor, normalTextColor, selectedTextColor);

        UpdateIntroForFriend(friendUI.FriendName);
        ShowStartButton();
    }

    // ========= UI更新 =========
    private void UpdateIntroForLocation()
    {
        if (titleText != null)
            titleText.text = locationTitle;

        if (noteText != null)
        {
            noteText.gameObject.SetActive(true);
            noteText.text = selectionNote;
        }

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (startButtonText != null)
            startButtonText.text = "Start";
    }

    private void UpdateIntroForFriend(string friendName)
    {
        if (titleText != null)
            titleText.text = string.Format(friendTitleTemplate, friendName);

        if (noteText != null)
        {
            noteText.gameObject.SetActive(true);
            noteText.text = selectionNote;
        }

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (startButtonText != null)
            startButtonText.text = "Start";
    }

    private void ShowStartButton()
    {
        if (startButton != null)
            startButton.SetActive(true);
    }

    public void ResetToDefaultState()
    {
        currentSelectionType = SelectionType.None;

        HideMarker();
        ClearSelectedFriendVisual();

        if (titleText != null)
            titleText.text = defaultTitle;

        if (noteText != null)
            noteText.gameObject.SetActive(false);

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = defaultHint;
        }

        if (startButton != null)
            startButton.SetActive(false);
    }

    // ========= Marker =========
    private void PlaceMarker(Vector2 localPoint)
    {
        if (currentMarker == null)
        {
            if (markerPrefab == null)
            {
                Debug.LogWarning("Marker Prefab is not assigned.");
                return;
            }

            currentMarker = Instantiate(markerPrefab, markerParent);
        }

        Vector2 clampedPoint = ClampToMap(localPoint);
        currentMarker.anchoredPosition = clampedPoint + markerOffset;
        currentMarker.gameObject.SetActive(true);
    }

    private void HideMarker()
    {
        if (currentMarker != null)
            currentMarker.gameObject.SetActive(false);
    }

    private Vector2 ClampToMap(Vector2 localPoint)
    {
        Rect rect = mapRect.rect;
        float x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        float y = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);
        return new Vector2(x, y);
    }

    // ========= Friend visual =========
    private void ClearSelectedFriendVisual()
    {
        if (currentSelectedFriend != null)
        {
            currentSelectedFriend.SetSelected(false, normalButtonColor, selectedButtonColor, normalTextColor, selectedTextColor);
            currentSelectedFriend = null;
        }
    }

    public SelectionType GetCurrentSelectionType()
    {
        return currentSelectionType;
    }
}