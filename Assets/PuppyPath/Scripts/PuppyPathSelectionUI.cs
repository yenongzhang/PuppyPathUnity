using System;
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

    [Serializable]
    public class FriendMarkerData
    {
        [Tooltip("FriendButtonUI.FriendName")]
        public string friendName;

        [Tooltip("Optional. Also match by FriendButtonUI.PathId if needed.")]
        public string pathId;

        [Range(0f, 1f)]
        public float normalizedX = 0.5f;

        [Range(0f, 1f)]
        public float normalizedY = 0.5f;
    }

    [Header("Map")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransform markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private Vector2 markerOffset = Vector2.zero;

    [Header("Map Grid Path Selection")]
    [SerializeField] private int gridRows = 3;
    [SerializeField] private int gridColumns = 5;

    [Tooltip("Order: row 1 left-to-right, then row 2 left-to-right, then row 3 left-to-right. Row 1 is the TOP row.")]
    [SerializeField] private string[] locationGridPathIds = new string[15]
    {
        "path_location_r1_c1",
        "path_location_r1_c2",
        "path_location_r1_c3",
        "path_location_r1_c4",
        "path_location_r1_c5",

        "path_location_r2_c1",
        "path_location_r2_c2",
        "path_location_r2_c3",
        "path_location_r2_c4",
        "path_location_r2_c5",

        "path_location_r3_c1",
        "path_location_r3_c2",
        "path_location_r3_c3",
        "path_location_r3_c4",
        "path_location_r3_c5"
    };

    [Tooltip("Order must match Location Grid Path Ids.")]
    [SerializeField] private string[] locationGridDisplayNames = new string[15]
    {
        "Germany",
        "Ireland - World of Children",
        "England",
        "Austria and Fairy Tale Forest",
        "Spain",

        "Italy and France",
        "Switzerland",
        "Luxembourg and Croatia",
        "Adventure Land and Portugal",
        "Hotel Entrance and Spain",

        "Main Entrance",
        "Greece",
        "Russia",
        "Holland and Scandinavia",
        "Iceland"
    };

    [SerializeField] private bool logGridSelection = true;

    [Header("Friend Marker Positions")]
    [Tooltip("Normalized map positions. (0,0)=bottom-left, (1,1)=top-right")]
    [SerializeField] private FriendMarkerData[] friendMarkerData = new FriendMarkerData[4]
    {
        new FriendMarkerData { friendName = "Kevin",  pathId = "Path_Kevin",  normalizedX = 0.18f, normalizedY = 0.72f },
        new FriendMarkerData { friendName = "Yenong", pathId = "Path_Yenong", normalizedX = 0.48f, normalizedY = 0.52f },
        new FriendMarkerData { friendName = "Si",     pathId = "Path_Si",     normalizedX = 0.73f, normalizedY = 0.60f },
        new FriendMarkerData { friendName = "Ying",   pathId = "Path_Ying",   normalizedX = 0.82f, normalizedY = 0.22f }
    };

    [Header("Phase 1 Next Button Logic")]
    [SerializeField] private GameObject phase1FirstText;
    [SerializeField] private GameObject phase1SecondText;
    [SerializeField] private GameObject phase1ThirdText;
    [SerializeField] private GameObject phase1NextButton;

    [Header("Phase 2 Dynamic Text")]
    [SerializeField] private TMP_Text phase2SelectionText;

    [TextArea(2, 4)]
    [SerializeField] private string locationPhase2TextTemplate =
        "Do you want to go to {0} in Europa-Park?\nClick \"Show Path\" to preview your path.";

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

    [Header("Fallback Location")]
    [SerializeField] private string fallbackLocationPathId = "path_location_r2_c3";
    [SerializeField] private string fallbackLocationDisplayName = "this place";

    private RectTransform currentMarker;
    private SelectionType currentSelectionType = SelectionType.None;
    private FriendButtonUI currentSelectedFriend;

    private string currentPathId;
    private string currentLocationDisplayName;

    private int selectedGridRow = -1;
    private int selectedGridColumn = -1;
    private int selectedGridIndex = -1;

    private void OnValidate()
    {
        gridRows = Mathf.Max(1, gridRows);
        gridColumns = Mathf.Max(1, gridColumns);

        int expectedCount = gridRows * gridColumns;

        EnsurePathIdArraySize(expectedCount);
        EnsureDisplayNameArraySize(expectedCount);
    }

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
        currentLocationDisplayName = friendUI.FriendName;

        selectedGridRow = -1;
        selectedGridColumn = -1;
        selectedGridIndex = -1;

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

        PlaceFriendMarker(friendUI);
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
        ClearSelectedFriendVisual();

        Vector2 clampedPoint = ClampToMap(localPoint);

        selectedGridIndex = GetGridIndexFromLocalPoint(
            clampedPoint,
            out selectedGridRow,
            out selectedGridColumn
        );

        currentPathId = GetPathIdForGridIndex(selectedGridIndex);
        currentLocationDisplayName = GetDisplayNameForGridIndex(selectedGridIndex);

        // 关键修改：marker 放在用户真实点击的位置，而不是格子中心
        PlaceMarker(clampedPoint);

        UpdatePhase2ForLocation();
        ShowShowPathButton();

        if (logGridSelection)
        {
            Debug.Log(
                $"Map grid selected: row={selectedGridRow + 1}, column={selectedGridColumn + 1}, " +
                $"index={selectedGridIndex}, pathId={currentPathId}, destination={currentLocationDisplayName}"
            );
        }

        if (navigationController != null)
        {
            navigationController.SetPendingPathId(currentPathId);
            navigationController.SetPendingTargetName(currentLocationDisplayName);
            navigationController.ShowPathSelectionPhase();
        }
    }

    public void ResetToDefaultState()
    {
        currentSelectionType = SelectionType.None;
        currentPathId = null;
        currentLocationDisplayName = null;

        selectedGridRow = -1;
        selectedGridColumn = -1;
        selectedGridIndex = -1;

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
        {
            string displayName = string.IsNullOrWhiteSpace(currentLocationDisplayName)
                ? fallbackLocationDisplayName
                : currentLocationDisplayName;

            phase2SelectionText.text = string.Format(locationPhase2TextTemplate, displayName);
        }

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

            Transform parent = markerParent != null ? markerParent : mapRect;
            currentMarker = Instantiate(markerPrefab, parent);
        }

        Vector2 clampedPoint = ClampToMap(localPoint);
        currentMarker.anchoredPosition = clampedPoint + markerOffset;
        currentMarker.gameObject.SetActive(true);
    }

    private void PlaceFriendMarker(FriendButtonUI friendUI)
    {
        if (friendUI == null)
            return;

        Vector2 localPoint;
        if (TryGetFriendMarkerLocalPoint(friendUI, out localPoint))
        {
            PlaceMarker(localPoint);
        }
        else
        {
            // 找不到就给一个默认点，避免点了朋友却没有 marker
            PlaceMarker(GetLocalPointFromNormalized(0.5f, 0.5f));
        }
    }

    private bool TryGetFriendMarkerLocalPoint(FriendButtonUI friendUI, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (friendUI == null || mapRect == null)
            return false;

        if (friendMarkerData == null || friendMarkerData.Length == 0)
            return false;

        for (int i = 0; i < friendMarkerData.Length; i++)
        {
            FriendMarkerData data = friendMarkerData[i];
            if (data == null)
                continue;

            bool matchedByName =
                !string.IsNullOrWhiteSpace(data.friendName) &&
                !string.IsNullOrWhiteSpace(friendUI.FriendName) &&
                string.Equals(data.friendName.Trim(), friendUI.FriendName.Trim(), StringComparison.OrdinalIgnoreCase);

            bool matchedByPathId =
                !string.IsNullOrWhiteSpace(data.pathId) &&
                !string.IsNullOrWhiteSpace(friendUI.PathId) &&
                string.Equals(data.pathId.Trim(), friendUI.PathId.Trim(), StringComparison.OrdinalIgnoreCase);

            if (matchedByName || matchedByPathId)
            {
                localPoint = GetLocalPointFromNormalized(data.normalizedX, data.normalizedY);
                return true;
            }
        }

        return false;
    }

    private Vector2 GetLocalPointFromNormalized(float normalizedX, float normalizedY)
    {
        if (mapRect == null)
            return Vector2.zero;

        Rect rect = mapRect.rect;

        float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(normalizedX));
        float y = Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Clamp01(normalizedY));

        return new Vector2(x, y);
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

    private int GetGridIndexFromLocalPoint(Vector2 localPoint, out int row, out int column)
    {
        row = 0;
        column = 0;

        if (mapRect == null)
            return 0;

        Rect rect = mapRect.rect;

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedYFromBottom = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        column = Mathf.Clamp(
            Mathf.FloorToInt(normalizedX * gridColumns),
            0,
            gridColumns - 1
        );

        float normalizedYFromTop = 1f - normalizedYFromBottom;

        row = Mathf.Clamp(
            Mathf.FloorToInt(normalizedYFromTop * gridRows),
            0,
            gridRows - 1
        );

        return row * gridColumns + column;
    }

    private string GetPathIdForGridIndex(int gridIndex)
    {
        if (locationGridPathIds != null &&
            gridIndex >= 0 &&
            gridIndex < locationGridPathIds.Length &&
            !string.IsNullOrWhiteSpace(locationGridPathIds[gridIndex]))
        {
            return locationGridPathIds[gridIndex];
        }

        Debug.LogWarning(
            $"PuppyPathSelectionUI: no path id configured for grid index {gridIndex}. " +
            $"Using fallback path id: {fallbackLocationPathId}"
        );

        return fallbackLocationPathId;
    }

    private string GetDisplayNameForGridIndex(int gridIndex)
    {
        if (locationGridDisplayNames != null &&
            gridIndex >= 0 &&
            gridIndex < locationGridDisplayNames.Length &&
            !string.IsNullOrWhiteSpace(locationGridDisplayNames[gridIndex]))
        {
            return locationGridDisplayNames[gridIndex];
        }

        int row = selectedGridRow + 1;
        int col = selectedGridColumn + 1;

        if (row > 0 && col > 0)
            return $"Location {row}-{col}";

        return fallbackLocationDisplayName;
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

    private void EnsurePathIdArraySize(int expectedCount)
    {
        if (locationGridPathIds != null && locationGridPathIds.Length == expectedCount)
            return;

        string[] oldArray = locationGridPathIds;
        locationGridPathIds = new string[expectedCount];

        if (oldArray != null)
        {
            int copyCount = Mathf.Min(oldArray.Length, locationGridPathIds.Length);
            for (int i = 0; i < copyCount; i++)
                locationGridPathIds[i] = oldArray[i];
        }

        for (int i = 0; i < locationGridPathIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(locationGridPathIds[i]))
            {
                int row = i / gridColumns + 1;
                int col = i % gridColumns + 1;
                locationGridPathIds[i] = $"path_location_r{row}_c{col}";
            }
        }
    }

    private void EnsureDisplayNameArraySize(int expectedCount)
    {
        if (locationGridDisplayNames != null && locationGridDisplayNames.Length == expectedCount)
            return;

        string[] oldArray = locationGridDisplayNames;
        locationGridDisplayNames = new string[expectedCount];

        if (oldArray != null)
        {
            int copyCount = Mathf.Min(oldArray.Length, locationGridDisplayNames.Length);
            for (int i = 0; i < copyCount; i++)
                locationGridDisplayNames[i] = oldArray[i];
        }

        string[] defaultNames = new string[]
        {
            "Germany",
            "Ireland - World of Children",
            "England",
            "Austria and Fairy Tale Forest",
            "Spain",

            "Italy and France",
            "Switzerland",
            "Luxembourg and Croatia",
            "Adventure Land and Portugal",
            "Hotel Entrance and Spain",

            "Main Entrance",
            "Greece",
            "Russia",
            "Holland and Scandinavia",
            "Iceland"
        };

        for (int i = 0; i < locationGridDisplayNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(locationGridDisplayNames[i]))
            {
                if (i < defaultNames.Length)
                {
                    locationGridDisplayNames[i] = defaultNames[i];
                }
                else
                {
                    int row = i / gridColumns + 1;
                    int col = i % gridColumns + 1;
                    locationGridDisplayNames[i] = $"Location {row}-{col}";
                }
            }
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

    public string GetCurrentLocationDisplayName()
    {
        return currentLocationDisplayName;
    }

    public int GetSelectedGridRow()
    {
        return selectedGridRow;
    }

    public int GetSelectedGridColumn()
    {
        return selectedGridColumn;
    }

    public int GetSelectedGridIndex()
    {
        return selectedGridIndex;
    }
}