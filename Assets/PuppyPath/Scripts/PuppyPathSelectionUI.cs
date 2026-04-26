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

    [Header("Phase 1 Next Button Logic")]
    [SerializeField] private GameObject phase1FirstText;
    [SerializeField] private GameObject phase1SecondText;
    [SerializeField] private GameObject phase1ThirdText;
    [SerializeField] private GameObject phase1NextButton;

    [Header("Phase 2 Dynamic Text")]
    [SerializeField] private TMP_Text phase2SelectionText;

    [TextArea(2, 4)]
    [SerializeField] private string locationPhase2Text =
        "Do you want to go to the location you marked?\nClick \"Show Path\" to preview your path.";

    [TextArea(2, 4)]
    [SerializeField] private string friendPhase2TextTemplate =
        "Do you want to go find {0}?\nClick \"Show Path\" to preview your path.";

    [Header("Show Path Button")]
    [SerializeField] private GameObject showPathButton;
    [SerializeField] private TMP_Text showPathButtonText;

    [Header("Navigation")]
    [SerializeField] private NavigationController navigationController;

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

    public void ShowPhase1SecondPage()
    {
        if (phase1FirstText != null)
            phase1FirstText.SetActive(false);

        if (phase1SecondText != null)
            phase1SecondText.SetActive(false);

        if (phase1ThirdText != null)
            phase1ThirdText.SetActive(true);

        if (phase1NextButton != null)
            phase1NextButton.SetActive(false);
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

        UpdatePhase2ForFriend(friendUI.FriendName);
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

        UpdatePhase2ForLocation();
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

        if (phase1FirstText != null)
            phase1FirstText.SetActive(true);

        if (phase1SecondText != null)
            phase1SecondText.SetActive(true);

        if (phase1ThirdText != null)
            phase1ThirdText.SetActive(false);

        if (phase1NextButton != null)
            phase1NextButton.SetActive(true);

        if (phase2SelectionText != null)
            phase2SelectionText.text = "";

        if (showPathButtonText != null)
            showPathButtonText.text = "Show Path";

        if (showPathButton != null)
            showPathButton.SetActive(false);
    }

    private void UpdatePhase2ForLocation()
    {
        if (phase2SelectionText != null)
            phase2SelectionText.text = locationPhase2Text;

        if (showPathButtonText != null)
            showPathButtonText.text = "Show Path";
    }

    private void UpdatePhase2ForFriend(string friendName)
    {
        if (phase2SelectionText != null)
            phase2SelectionText.text = string.Format(friendPhase2TextTemplate, friendName);

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