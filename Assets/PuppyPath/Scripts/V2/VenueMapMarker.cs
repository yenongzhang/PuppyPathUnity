using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VenueMapMarker : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor = new Color(1f, 0.64f, 0.08f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.85f, 1f, 1f);

    public string AttractionId { get; private set; }

    private VenueMapUiController owner;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Configure(VenueMapUiController mapOwner, AttractionDefinition attraction, bool showLabel)
    {
        ResolveReferences();

        owner = mapOwner;
        AttractionId = attraction != null ? attraction.id : string.Empty;

        if (label != null)
        {
            label.text = attraction != null && !string.IsNullOrWhiteSpace(attraction.displayName)
                ? attraction.displayName
                : AttractionId;
            label.gameObject.SetActive(showLabel);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        ResolveReferences();

        if (iconImage != null)
            iconImage.color = selected ? selectedColor : normalColor;

        transform.localScale = selected ? Vector3.one * 1.18f : Vector3.one;
    }

    private void HandleClick()
    {
        if (owner != null && !string.IsNullOrEmpty(AttractionId))
            owner.SelectAttraction(AttractionId);
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();

        if (button == null)
            button = GetComponent<Button>();
    }
}
