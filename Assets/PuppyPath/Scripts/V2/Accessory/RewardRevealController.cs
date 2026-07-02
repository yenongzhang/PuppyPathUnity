using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Independent, map-agnostic reward reveal API: ShowReward(attractionId) attaches the
/// matching accessory, plays a one-shot dog reaction, optionally spawns fireworks, and
/// shows a placeholder reward panel. Deliberately has no dependency on VenueMapDefinition
/// or AttractionDefinition so it can be tested and integrated independently of map work.
/// </summary>
public class RewardRevealController : MonoBehaviour
{
    [SerializeField] private List<RewardDefinition> rewards;
    [SerializeField] private DogAccessoryManager accessoryManager;
    [SerializeField] private DogGuideController dogGuideController;
    [SerializeField] private NavigationHUDController hudController;
    [SerializeField] private string rewardReactionState = "HappyStart";
    [SerializeField] private GameObject fireworkPrefab;
    [SerializeField] private bool alwaysTriggerFireworks = true;
    [SerializeField] private float fireworkHeightOffset = 0.8f;
    [SerializeField] private AudioClip fireworkSound;
    [SerializeField] private float fireworkSoundVolume = 0.75f;
    [SerializeField] private GameObject rewardPanelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private RewardIconWiggle rewardIconWiggle;
    [SerializeField] private bool autoFindRewardPanel = true;
    [SerializeField] private string rewardPanelName = "RewardPanel";
    [SerializeField] private float autoHideSeconds = 4f;
    [SerializeField] private bool restoreFreeRoamHudAfterHide = true;

    private Coroutine autoHideRoutine;
    private readonly Dictionary<string, Sprite> fallbackSprites = new Dictionary<string, Sprite>();

    private void Awake()
    {
        ResolveRewardPanelReferences();

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(false);
    }

    public void ShowReward(string attractionId)
    {
        RewardDefinition definition = FindReward(attractionId);

        if (definition == null)
        {
            Debug.LogWarning($"RewardRevealController: no RewardDefinition found for attractionId '{attractionId}'.");
            return;
        }

        if (definition.accessory != null && accessoryManager != null)
            accessoryManager.AttachAccessory(definition.accessory);

        if (dogGuideController != null)
            dogGuideController.PlayOneShotState(rewardReactionState);

        if (alwaysTriggerFireworks || definition.triggerFireworks)
            PlayRewardFireworks();

        ShowPanel(definition);
    }

    public void HideReward()
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(false);

        if (rewardIconWiggle != null)
            rewardIconWiggle.Stop();

        if (restoreFreeRoamHudAfterHide && hudController != null)
            hudController.RestoreFreeRoamHud();
    }

    private RewardDefinition FindReward(string attractionId)
    {
        if (rewards == null || string.IsNullOrEmpty(attractionId))
            return null;

        foreach (RewardDefinition reward in rewards)
        {
            if (reward != null && reward.attractionId == attractionId)
                return reward;
        }

        return null;
    }

    private void ShowPanel(RewardDefinition definition)
    {
        ResolveRewardPanelReferences();

        if (hudController != null)
            hudController.HideNavigationHud();

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(definition.rewardTitle) ? "SURPRISE!" : definition.rewardTitle;

        if (bodyText != null)
            bodyText.text = definition.rewardMessage;

        if (rewardIconImage != null)
        {
            rewardIconImage.sprite = definition.rewardIcon != null ? definition.rewardIcon : GetFallbackSprite(definition);
            rewardIconImage.enabled = rewardIconImage.sprite != null;
        }

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(true);

        if (rewardIconWiggle != null)
            rewardIconWiggle.Play();

        if (autoHideRoutine != null)
            StopCoroutine(autoHideRoutine);

        if (autoHideSeconds > 0f)
            autoHideRoutine = StartCoroutine(AutoHideAfterDelay());
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        autoHideRoutine = null;

        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(false);

        if (rewardIconWiggle != null)
            rewardIconWiggle.Stop();

        if (restoreFreeRoamHudAfterHide && hudController != null)
            hudController.RestoreFreeRoamHud();
    }

    private void PlayRewardFireworks()
    {
        Vector3 spawnPos = dogGuideController != null && dogGuideController.CurrentDog != null
            ? dogGuideController.CurrentDog.transform.position
            : transform.position;

        spawnPos += Vector3.up * fireworkHeightOffset;

        if (fireworkPrefab != null)
            Instantiate(fireworkPrefab, spawnPos, Quaternion.identity);

        if (fireworkSound != null)
            AudioSource.PlayClipAtPoint(fireworkSound, spawnPos, fireworkSoundVolume);
    }

    private void ResolveRewardPanelReferences()
    {
        if (rewardPanelRoot != null && titleText != null && bodyText != null && rewardIconImage != null)
            return;

        if (autoFindRewardPanel)
            AutoFindSceneRewardPanel();

        if (rewardPanelRoot != null)
        {
            AutoBindRewardPanelChildren();
            return;
        }

        CreateFallbackRewardPanel();
    }

    private void AutoFindSceneRewardPanel()
    {
        if (rewardPanelRoot != null || string.IsNullOrWhiteSpace(rewardPanelName))
            return;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in objects)
        {
            if (candidate == null || candidate.name != rewardPanelName || !candidate.scene.IsValid())
                continue;

            rewardPanelRoot = candidate;
            break;
        }
    }

    private void AutoBindRewardPanelChildren()
    {
        if (rewardPanelRoot == null)
            return;

        TMP_Text[] texts = rewardPanelRoot.GetComponentsInChildren<TMP_Text>(true);
        if (titleText == null && texts.Length > 0)
            titleText = texts[0];
        if (bodyText == null && texts.Length > 1)
            bodyText = texts[1];

        if (rewardIconImage == null)
        {
            Image[] images = rewardPanelRoot.GetComponentsInChildren<Image>(true);
            float largestArea = -1f;

            foreach (Image image in images)
            {
                if (image == null || image.gameObject == rewardPanelRoot)
                    continue;

                RectTransform rect = image.transform as RectTransform;
                float area = rect != null ? rect.rect.width * rect.rect.height : 0f;
                if (area > largestArea)
                {
                    largestArea = area;
                    rewardIconImage = image;
                }
            }
        }

        if (rewardIconImage != null && rewardIconWiggle == null)
            rewardIconWiggle = rewardIconImage.GetComponent<RewardIconWiggle>() ?? rewardIconImage.gameObject.AddComponent<RewardIconWiggle>();
    }

    private void CreateFallbackRewardPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject root = new GameObject("RuntimeRewardPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backdrop = root.GetComponent<Image>();
        backdrop.color = new Color(0.04f, 0.04f, 0.05f, 0.58f);

        GameObject panel = new GameObject("RewardCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 520f);
        panelRect.anchoredPosition = Vector2.zero;

        Image cardImage = panel.GetComponent<Image>();
        cardImage.color = new Color(1f, 0.96f, 0.72f, 0.97f);

        titleText = titleText != null ? titleText : CreatePanelText(panel.transform, "RewardTitle", new Vector2(0f, 170f), new Vector2(640f, 90f), 58f, FontStyles.Bold);
        rewardIconImage = rewardIconImage != null ? rewardIconImage : CreatePanelIcon(panel.transform);
        bodyText = bodyText != null ? bodyText : CreatePanelText(panel.transform, "RewardBody", new Vector2(0f, -175f), new Vector2(620f, 120f), 30f, FontStyles.Normal);
        rewardIconWiggle = rewardIconImage != null ? rewardIconImage.gameObject.AddComponent<RewardIconWiggle>() : null;

        rewardPanelRoot = root;
        rewardPanelRoot.SetActive(false);
    }

    private TMP_Text CreatePanelText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.18f, 0.12f, 0.08f, 1f);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;

        return text;
    }

    private Image CreatePanelIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("RewardIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180f, 180f);
        rect.anchoredPosition = new Vector2(0f, 10f);

        Image image = iconObject.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private Sprite GetFallbackSprite(RewardDefinition definition)
    {
        string key = definition != null && !string.IsNullOrWhiteSpace(definition.attractionId)
            ? definition.attractionId
            : "default";

        if (fallbackSprites.TryGetValue(key, out Sprite sprite))
            return sprite;

        sprite = CreateFallbackSprite(key);
        fallbackSprites[key] = sprite;
        return sprite;
    }

    private Sprite CreateFallbackSprite(string key)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RewardIcon_" + key;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.18f, 0.12f, 0.08f, 1f);
        Color fill = key.Contains("shirt") ? new Color(0.35f, 0.62f, 1f, 1f) :
            key.Contains("socks") ? new Color(0.55f, 0.35f, 1f, 1f) :
            new Color(1f, 0.24f, 0.22f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, transparent);
        }

        DrawRect(texture, 34, 18, 94, 110, outline);
        DrawRect(texture, 40, 24, 88, 104, fill);
        DrawRect(texture, 46, 92, 82, 100, new Color(1f, 1f, 1f, 0.9f));
        DrawCircle(texture, 48, 62, 8, new Color(1f, 1f, 1f, 0.75f));
        DrawCircle(texture, 74, 48, 5, new Color(1f, 1f, 1f, 0.6f));
        DrawCircle(texture, 84, 72, 4, new Color(1f, 1f, 1f, 0.55f));

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void DrawRect(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
    {
        for (int y = yMin; y < yMax; y++)
        {
            for (int x = xMin; x < xMax; x++)
                texture.SetPixel(x, y, color);
        }
    }

    private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy <= radiusSquared)
                    texture.SetPixel(x, y, color);
            }
        }
    }
}
