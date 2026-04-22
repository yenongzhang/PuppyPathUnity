using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MapClickMarkerUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Map")]
    [SerializeField] private RectTransform mapRect;

    [Header("Marker")]
    [SerializeField] private RectTransform markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private Vector2 markerOffset = Vector2.zero;

    [Header("Intro Panel Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text noteText;

    [Header("Start Button")]
    [SerializeField] private GameObject startButton;

    [Header("Optional")]
    [SerializeField] private TMP_Text startButtonText;

    private RectTransform currentMarker;

    private const string TITLE_MESSAGE =
        "Do you want to go to the location you marked?\n" +
        "Click \"Start\" to get going right now";

    private const string NOTE_MESSAGE =
        "P.S.: The puppy will lead the way. Keep an eye on its reactions\n" +
        "—it’ll get upset if you take a wrong turn :(";

    private const string START_LABEL = "Start";

    private void Start()
    {
        if (startButton != null)
            startButton.SetActive(false);

        if (markerParent == null && mapRect != null)
            markerParent = mapRect;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (mapRect == null) return;

        Vector2 localPoint;
        bool clicked =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

        if (!clicked) return;

        PlaceMarker(localPoint);
        UpdateIntroPanel();
        ShowStartButton();
    }

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

    private Vector2 ClampToMap(Vector2 localPoint)
    {
        Rect rect = mapRect.rect;

        float x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        float y = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);

        return new Vector2(x, y);
    }

    private void UpdateIntroPanel()
    {
        if (titleText != null)
            titleText.text = TITLE_MESSAGE;

        if (noteText != null)
            noteText.text = NOTE_MESSAGE;

        if (startButtonText != null)
            startButtonText.text = START_LABEL;
    }

    private void ShowStartButton()
    {
        if (startButton != null)
            startButton.SetActive(true);
    }
}
