using UnityEngine;

public class RewardIconWiggle : MonoBehaviour
{
    [SerializeField] private float wiggleAmplitude = 12f;
    [SerializeField] private float wiggleFrequency = 12f;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private bool isPlaying;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (!isPlaying || rectTransform == null)
            return;

        float offset = Mathf.Sin(Time.unscaledTime * wiggleFrequency * Mathf.PI * 2f) * wiggleAmplitude;
        rectTransform.anchoredPosition = baseAnchoredPosition + Vector2.right * offset;
    }

    public void Play()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;
    }
}
