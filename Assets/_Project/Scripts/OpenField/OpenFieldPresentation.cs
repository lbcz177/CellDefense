using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class OpenFieldPresentation : MonoBehaviour
{
    private const int CircleSegments = 64;
    private static readonly Color FreeCellColor = new Color(0.12f, 0.21f, 0.25f, 1f);
    private static readonly Color AlternateCellColor = new Color(0.14f, 0.24f, 0.28f, 1f);
    private static readonly Color CoreColor = new Color(0.27f, 0.8f, 0.55f, 1f);
    private static readonly Color SpawnColor = new Color(0.9f, 0.3f, 0.32f, 1f);
    private static readonly Color BlockedColor = new Color(0.44f, 0.24f, 0.27f, 1f);
    private static readonly Color ValidColor = new Color(0.35f, 0.95f, 0.95f, 0.7f);
    private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f, 0.7f);

    private Sprite tileSprite;
    private GameObject previewRoot;
    private SpriteRenderer ghostRenderer;
    private SpriteRenderer cellHintRenderer;
    private LineRenderer rangeRenderer;
    private Material rangeMaterial;
    private GameObject routesRoot;
    private LineRenderer[] routeLines;
    private readonly List<Vector2Int> routeCells = new List<Vector2Int>();

    public void Initialize(OpenFieldGridModel grid, Func<Vector2Int, Vector3> cellToWorld,
        float cellSize, Sprite mapArtwork, bool showDebugGrid)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (cellToWorld == null) throw new ArgumentNullException(nameof(cellToWorld));

        Texture2D white = Texture2D.whiteTexture;
        tileSprite = Sprite.Create(white,
            new Rect(0f, 0f, white.width, white.height),
            new Vector2(0.5f, 0.5f), white.width);

        if (mapArtwork != null)
        {
            GameObject background = new GameObject("Hand Painted Map");
            background.transform.SetParent(transform);
            Vector3 bottomLeft = cellToWorld(Vector2Int.zero);
            Vector3 topRight = cellToWorld(new Vector2Int(grid.Width - 1, grid.Height - 1));
            background.transform.position = (bottomLeft + topRight) * 0.5f;
            background.transform.localScale = new Vector3(
                grid.Width * cellSize / mapArtwork.bounds.size.x,
                grid.Height * cellSize / mapArtwork.bounds.size.y,
                1f);
            SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = mapArtwork;
            backgroundRenderer.sortingOrder = -30;
        }

        if (showDebugGrid || mapArtwork == null)
        {
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
                            : grid.IsBlocked(cell)
                                ? BlockedColor
                                : (x + y) % 2 == 0 ? FreeCellColor : AlternateCellColor;
                }
            }
        }

        previewRoot = new GameObject("Placement Preview");
        previewRoot.transform.SetParent(transform);
        GameObject cellHint = new GameObject("Cell Hint");
        cellHint.transform.SetParent(previewRoot.transform, false);
        cellHint.transform.localScale = Vector3.one * cellSize * 0.92f;
        cellHintRenderer = cellHint.AddComponent<SpriteRenderer>();
        cellHintRenderer.sprite = tileSprite;
        cellHintRenderer.sortingOrder = -10;
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
        cellHintRenderer.enabled = true;
        ghostRenderer.enabled = true;
        rangeRenderer.enabled = true;
        cellHintRenderer.color = new Color(tint.r, tint.g, tint.b, 0.2f);
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

    public void ShowCellHint(Vector3 position, bool valid)
    {
        if (previewRoot == null) return;
        previewRoot.transform.position = position;
        cellHintRenderer.enabled = true;
        Color tint = valid ? ValidColor : InvalidColor;
        cellHintRenderer.color = new Color(tint.r, tint.g, tint.b, 0.3f);
        ghostRenderer.enabled = false;
        rangeRenderer.enabled = false;
        previewRoot.SetActive(true);
    }

    public void ShowBuiltTower(TowerController tower)
    {
        if (tower == null) { HidePreview(); return; }
        ShowPreview(tower.Definition, tower.transform.position, true);
        ghostRenderer.enabled = false;
        cellHintRenderer.enabled = false;
    }

    public void HidePreview()
    {
        if (previewRoot != null) previewRoot.SetActive(false);
    }

    public void ShowRoutes(OpenFieldGridModel grid, Func<Vector2Int, Vector3> cellToWorld,
        float cellSize, Vector2Int? previewBarrier = null)
    {
        if (routesRoot == null)
        {
            routesRoot = new GameObject("Planning Routes");
            routesRoot.transform.SetParent(transform, false);
            routeLines = new LineRenderer[grid.SpawnCount];
            for (int i = 0; i < routeLines.Length; i++)
            {
                GameObject line = new GameObject($"Entrance Route {i + 1}");
                line.transform.SetParent(routesRoot.transform, false);
                LineRenderer renderer = line.AddComponent<LineRenderer>();
                renderer.sharedMaterial = rangeMaterial;
                renderer.useWorldSpace = true;
                renderer.widthMultiplier = cellSize * 0.07f;
                renderer.sortingOrder = -12;
                routeLines[i] = renderer;
            }
        }
        routesRoot.SetActive(true);
        for (int i = 0; i < routeLines.Length; i++)
        {
            bool valid = previewBarrier.HasValue
                ? grid.TryGetPreviewPath(grid.GetSpawnCell(i), previewBarrier.Value, routeCells)
                : grid.TryGetPath(grid.GetSpawnCell(i), routeCells);
            LineRenderer line = routeLines[i];
            line.enabled = valid;
            if (!valid) continue;
            Color tint = previewBarrier.HasValue ? new Color(1f, 0.8f, 0.3f, 0.75f)
                : new Color(0.4f, 0.85f, 1f, 0.55f);
            line.startColor = line.endColor = tint;
            line.positionCount = routeCells.Count;
            for (int point = 0; point < routeCells.Count; point++)
                line.SetPosition(point, cellToWorld(routeCells[point]));
        }
    }

    public void HideRoutes()
    {
        if (routesRoot != null) routesRoot.SetActive(false);
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
