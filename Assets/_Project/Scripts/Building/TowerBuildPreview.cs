using UnityEngine;

public class TowerBuildPreview : MonoBehaviour
{
    private const int CircleSegments = 72;
    private static readonly Color AvailableColor = new Color(0.45f, 0.95f, 1f, 0.7f);
    private static readonly Color UnavailableColor = new Color(1f, 0.4f, 0.4f, 0.7f);
    private static readonly Color BuiltTowerColor = new Color(0.35f, 1f, 0.55f, 0.8f);

    private GameObject previewRoot;
    private SpriteRenderer ghostRenderer;
    private LineRenderer rangeRenderer;
    private Material rangeMaterial;

    private void Awake()
    {
        previewRoot = new GameObject("Tower Build Preview");
        GameObject ghost = new GameObject("Tower Ghost");
        ghost.transform.SetParent(previewRoot.transform, false);
        ghostRenderer = ghost.AddComponent<SpriteRenderer>();

        GameObject range = new GameObject("Attack Range");
        range.transform.SetParent(previewRoot.transform, false);
        rangeRenderer = range.AddComponent<LineRenderer>();
        rangeMaterial = new Material(Shader.Find("Sprites/Default"));
        rangeRenderer.material = rangeMaterial;
        rangeRenderer.useWorldSpace = false;
        rangeRenderer.loop = true;
        rangeRenderer.positionCount = CircleSegments;
        rangeRenderer.widthMultiplier = 0.035f;
        previewRoot.SetActive(false);
    }

    public void Show(BuildSlot slot, TowerDefinition definition, bool canAfford)
    {
        if (slot == null || definition == null || definition.Prefab == null)
        {
            Hide();
            return;
        }

        SpriteRenderer source = definition.Prefab.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null)
        {
            Hide();
            return;
        }

        previewRoot.transform.position = slot.transform.position;
        ghostRenderer.enabled = true;
        ghostRenderer.sprite = source.sprite;
        ghostRenderer.flipX = source.flipX;
        ghostRenderer.flipY = source.flipY;
        ghostRenderer.sortingLayerID = source.sortingLayerID;
        ghostRenderer.sortingOrder = source.sortingOrder + 1;
        ghostRenderer.transform.localScale = definition.Prefab.transform.localScale;

        Color tint = canAfford ? AvailableColor : UnavailableColor;
        ghostRenderer.color = new Color(tint.r, tint.g, tint.b, 0.5f);
        ConfigureRange(source, definition.AttackRange, tint);
        previewRoot.SetActive(true);
    }

    public void ShowBuiltTower(TowerController tower)
    {
        if (tower == null || tower.Definition == null)
        {
            Hide();
            return;
        }

        SpriteRenderer source = tower.GetComponentInChildren<SpriteRenderer>();
        if (source == null)
        {
            Hide();
            return;
        }

        previewRoot.transform.position = tower.transform.position;
        ghostRenderer.enabled = false;
        ConfigureRange(source, tower.Definition.AttackRange, BuiltTowerColor);
        previewRoot.SetActive(true);
    }

    private void ConfigureRange(SpriteRenderer source, float radius, Color tint)
    {
        rangeRenderer.startColor = tint;
        rangeRenderer.endColor = tint;
        rangeRenderer.sortingLayerID = source.sortingLayerID;
        rangeRenderer.sortingOrder = source.sortingOrder - 1;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = 2f * Mathf.PI * i / CircleSegments;
            rangeRenderer.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    public void Hide()
    {
        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (previewRoot != null)
        {
            Destroy(previewRoot);
        }
        if (rangeMaterial != null)
        {
            Destroy(rangeMaterial);
        }
    }
}
