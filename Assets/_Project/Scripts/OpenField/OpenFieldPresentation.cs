using System;
using UnityEngine;

public sealed class OpenFieldPresentation : MonoBehaviour
{
    private const int CircleSegments = 64;
    private static readonly Color FreeCellColor = new Color(0.12f, 0.21f, 0.25f, 1f);
    private static readonly Color AlternateCellColor = new Color(0.14f, 0.24f, 0.28f, 1f);
    private static readonly Color CoreColor = new Color(0.27f, 0.8f, 0.55f, 1f);
    private static readonly Color SpawnColor = new Color(0.9f, 0.3f, 0.32f, 1f);
    private static readonly Color ValidColor = new Color(0.35f, 0.95f, 0.95f, 0.7f);
    private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f, 0.7f);

    private Sprite tileSprite;
    private GameObject previewRoot;
    private SpriteRenderer ghostRenderer;
    private LineRenderer rangeRenderer;
    private Material rangeMaterial;

    public void Initialize(OpenFieldGridModel grid, Func<Vector2Int, Vector3> cellToWorld, float cellSize)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (cellToWorld == null) throw new ArgumentNullException(nameof(cellToWorld));

        Texture2D white = Texture2D.whiteTexture;
        tileSprite = Sprite.Create(white,
            new Rect(0f, 0f, white.width, white.height),
            new Vector2(0.5f, 0.5f), white.width);

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                GameObject tile = new GameObject($"Cell {x},{y}");
                tile.transform.SetParent(transform);
                tile.transform.position = cellToWorld(cell);
                tile.transform.localScale = Vector3.one * cellSize * 0.96f;
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = tileSprite;
                renderer.sortingOrder = -20;
                renderer.color = cell == grid.CoreCell
                    ? CoreColor
                    : IsSpawn(grid, cell)
                        ? SpawnColor
                        : (x + y) % 2 == 0 ? FreeCellColor : AlternateCellColor;
            }
        }

        previewRoot = new GameObject("Placement Preview");
        previewRoot.transform.SetParent(transform);
        GameObject ghost = new GameObject("Tower Ghost");
        ghost.transform.SetParent(previewRoot.transform, false);
        ghostRenderer = ghost.AddComponent<SpriteRenderer>();

        GameObject circle = new GameObject("Attack Range");
        circle.transform.SetParent(previewRoot.transform, false);
        rangeRenderer = circle.AddComponent<LineRenderer>();
        rangeMaterial = new Material(Shader.Find("Sprites/Default"));
        rangeRenderer.material = rangeMaterial;
        rangeRenderer.useWorldSpace = false;
        rangeRenderer.loop = true;
        rangeRenderer.positionCount = CircleSegments;
        rangeRenderer.widthMultiplier = 0.035f;
        previewRoot.SetActive(false);
    }

    public void ShowPreview(TowerDefinition definition, Vector3 position, bool valid)
    {
        if (definition == null || definition.Prefab == null || previewRoot == null)
        {
            HidePreview();
            return;
        }

        SpriteRenderer source = definition.Prefab.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null)
        {
            HidePreview();
            return;
        }

        Color tint = valid ? ValidColor : InvalidColor;
        previewRoot.transform.position = position;
        ghostRenderer.sprite = source.sprite;
        ghostRenderer.flipX = source.flipX;
        ghostRenderer.flipY = source.flipY;
        ghostRenderer.transform.localScale = source.transform.lossyScale;
        ghostRenderer.sortingLayerID = source.sortingLayerID;
        ghostRenderer.sortingOrder = source.sortingOrder + 2;
        ghostRenderer.color = new Color(tint.r, tint.g, tint.b, 0.5f);

        rangeRenderer.sortingLayerID = source.sortingLayerID;
        rangeRenderer.sortingOrder = source.sortingOrder + 1;
        rangeRenderer.startColor = tint;
        rangeRenderer.endColor = tint;
        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = 2f * Mathf.PI * i / CircleSegments;
            rangeRenderer.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * definition.AttackRange,
                Mathf.Sin(angle) * definition.AttackRange,
                0f));
        }
        previewRoot.SetActive(true);
    }

    public void HidePreview()
    {
        if (previewRoot != null) previewRoot.SetActive(false);
    }

    private static bool IsSpawn(OpenFieldGridModel grid, Vector2Int cell)
    {
        for (int i = 0; i < grid.SpawnCount; i++)
        {
            if (grid.GetSpawnCell(i) == cell) return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        if (rangeMaterial != null) Destroy(rangeMaterial);
        if (tileSprite != null) Destroy(tileSprite);
    }
}
