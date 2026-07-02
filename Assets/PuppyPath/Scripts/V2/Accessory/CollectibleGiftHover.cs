using UnityEngine;

/// <summary>
/// Gentle hovering motion for gift collectibles — slow vertical float with a subtle horizontal drift.
/// </summary>
public class CollectibleGiftHover : MonoBehaviour
{
    [SerializeField] private float verticalAmplitude = 0.04f;
    [SerializeField] private float verticalFrequency = 0.32f;
    [SerializeField] private float horizontalAmplitude = 0.012f;
    [SerializeField] private float horizontalFrequency = 0.18f;
    [SerializeField] private float phaseOffset;

    private Vector3 baseLocalPosition;
    private bool isPlaying = true;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float time = Time.time + phaseOffset;
        float yOffset = Mathf.Sin(time * verticalFrequency * Mathf.PI * 2f) * verticalAmplitude;
        float xOffset = Mathf.Sin(time * horizontalFrequency * Mathf.PI * 2f + 0.6f) * horizontalAmplitude;
        transform.localPosition = baseLocalPosition + new Vector3(xOffset, yOffset, 0f);
    }

    public void Stop()
    {
        isPlaying = false;
        transform.localPosition = baseLocalPosition;
    }
}
