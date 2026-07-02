using UnityEngine;

/// <summary>
/// Sits on a collectible item prefab. Controls distance-based fade (driven by AttractionTrigger)
/// and disables the grab interactable once collected. Requires a transparent-capable material.
/// </summary>
public class FloatingCollectibleItem : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Behaviour interactableToDisableOnCollect;

    private Material[] runtimeMaterials;
    private bool isCollected;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        runtimeMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                runtimeMaterials[i] = renderers[i].material;
        }
    }

    public void SetVisibility(float alpha)
    {
        if (isCollected)
            return;

        alpha = Mathf.Clamp01(alpha);

        bool shouldBeVisible = alpha > 0.001f;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = shouldBeVisible;
        }

        if (interactableToDisableOnCollect != null)
            interactableToDisableOnCollect.enabled = shouldBeVisible;

        if (!shouldBeVisible || runtimeMaterials == null)
            return;

        foreach (Material material in runtimeMaterials)
        {
            if (material == null)
                continue;

            if (material.HasProperty(BaseColorId))
            {
                Color color = material.GetColor(BaseColorId);
                color.a = alpha;
                material.SetColor(BaseColorId, color);
            }
            else if (material.HasProperty(ColorId))
            {
                Color color = material.GetColor(ColorId);
                color.a = alpha;
                material.SetColor(ColorId, color);
            }
        }
    }

    public void SetCollected(bool collected)
    {
        isCollected = collected;

        gameObject.SetActive(!collected);

        if (interactableToDisableOnCollect != null)
            interactableToDisableOnCollect.enabled = !collected;
    }
}
