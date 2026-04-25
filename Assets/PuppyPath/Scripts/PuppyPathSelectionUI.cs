using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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
    [SerializeField] private TMP_Text hintText;

    [Header("Show Path Button")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private TMP_Text showPathButtonText;

    [Header("Navigation")]
    [SerializeField] private NavigationController navigationController;

    [Header("Initial Text")]
    [TextArea(2, 4)]
    [SerializeField] private string defaultTitle =
        "Hi, I'm PuppyPath :)";

    [TextArea(2, 5)]
    [SerializeField] private string defaultNote =
        "I'll have a little beagle take you wherever you want to go or help you find your friends.";

    [TextArea(2, 5)]
    [SerializeField] private string defaultHint =
        "Please select the location you want to visit on the map, or choose the friend you want to find from your friends list.";

    [Header("Selection Text")]
    [TextArea(2, 4)]
    [SerializeField] private string locationTitle =
        "Do you want to go to the location you marked?\nClick \"Show Path\" to preview your path.";

    [TextArea(2, 4)]
    [SerializeField] private string friendTitleTemplate =
        "Do you want to go find {0}?\nClick \"Show Path\" to preview your path.";

    [TextArea(2, 5)]
    [SerializeField] private string selectionNote =
        "P.S.: The puppy will lead the way. Keep an eye on its reactions.\nIt'll get upset if you take a wrong turn :(";

    [Header("Friend Button Colors")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.62f, 0.45f, 1f);
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.black;

    [Header("Location Path Id")]
    [SerializeField] private string locationPathId = "path_location_a";

    private RectTransform currentMarker;
    private SelectionType currentSelectionType = SelectionType.None;
    private FriendButtonUI currentSelectedFriend;
    private string currentPathId;

    private void Start()
    {
        if (markerParent == null && mapRect != null)
            markerParent = mapRect;

        ResetToDefaultState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (mapRect == null)
            return;

        Vector2 localPoint;
        bool clicked = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        if (!clicked)
            return;

        SelectLocation(localPoint);
    }

    public void SelectFriend(FriendButtonUI friendUI)
    {
        if (friendUI == null)
            return;

        currentSelectionType = SelectionType.Friend;
        currentPathId = friendUI.PathId;

        HideMarker();

        if (currentSelectedFriend != null && currentSelectedFriend != friendUI)
        {
            currentSelectedFriend.SetSelected(
                false,
                normalButtonColor,
                selectedButtonColor,
                normalTextColor,
                selectedTextColor
            );
        }

        currentSelectedFriend = friendUI;
        currentSelectedFriend.SetSelected(
            true,
            normalButtonColor,
            selectedButtonColor,
            normalTextColor,
            selectedTextColor
        );

        UpdateIntroForFriend(friendUI.FriendName);
        ShowShowPathButton();

        if (navigationController != null)
        {
            navigationController.SetPendingPathId(currentPathId);
            navigationController.SetPendingTargetName(friendUI.FriendName);
            navigationController.ShowPathSelectionPhase();
        }
    }

    public void SelectLocation(Vector2 localPoint)
    {
        currentSelectionType = SelectionType.Location;
        currentPathId = locationPathId;

        ClearSelectedFriendVisual();
        PlaceMarker(localPoint);

        UpdateIntroForLocation();
        ShowShowPathButton();

        if (navigationController != null)
        {
            navigationController.SetPendingPathId(currentPathId);
            navigationController.SetPendingTargetName("Marked Location");
            navigationController.ShowPathSelectionPhase();
        }
    }

    public void ResetToDefaultState()
    {
        currentSelectionType = SelectionType.None;
        currentPathId = null;

        HideMarker();
        ClearSelectedFriendVisual();

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = defaultTitle;
        }

        if (noteText != null)
        {
            noteText.gameObject.SetActive(true);
            noteText.text = defaultNote;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = defaultHint;
        }

        if (showPathButtonText != null)
            showPathButtonText.text = "Show Path";

        if (showPathButton != null)
            showPathButton.SetActive(false);
    }

    private void UpdateIntroForLocation()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = locationTitle;
        }

        if (noteText != null)
        {
            noteText.gameObject.SetActive(true);
            noteText.text = selectionNote;
        }

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (showPathButtonText != null)
            showPathButtonText.text = "Show Path";
    }

    private void UpdateIntroForFriend(string friendName)
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = string.Format(friendTitleTemplate, friendName);
        }

        if (noteText != null)
        {
            noteText.gameObject.SetActive(true);
            noteText.text = selectionNote;
        }

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (showPathButtonText != null)
            showPathButtonText.text = "Show Path";
    }

    private void ShowShowPathButton()
    {
        if (showPathButton != null)
            showPathButton.SetActive(true);
    }

    private void PlaceMarker(Vector2 localPoint)
    {
        if (currentMarker == null)
        {
            if (markerPrefab == null)
            {
                Debug.LogWarning("PuppyPathSelectionUI: markerPrefab is not assigned.");
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
        if (mapRect == null)
            return localPoint;

        Rect rect = mapRect.rect;

        float x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        float y = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);

        return new Vector2(x, y);
    }

    private void ClearSelectedFriendVisual()
    {
        if (currentSelectedFriend != null)
        {
            currentSelectedFriend.SetSelected(
                false,
                normalButtonColor,
                selectedButtonColor,
                normalTextColor,
                selectedTextColor
            );

            currentSelectedFriend = null;
        }
    }

    public SelectionType GetCurrentSelectionType()
    {
        return currentSelectionType;
    }

    public string GetCurrentPathId()
    {
        return currentPathId;
    }
}