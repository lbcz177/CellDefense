using System;
using TMPro;
using UnityEngine;

// Scene prefab owns all graphics; this component binds data and safe-area placement.
public sealed class OpenFieldBattleHud : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform frameRect;
    [SerializeField] private HudSpriteNumber atp;
    [SerializeField] private HudSpriteNumber life;
    [SerializeField] private HudSpriteNumber round;
    [SerializeField] private HudSpriteNumber roundTotal;
    [SerializeField] private PixelImageText defenseText;
    [SerializeField] private Vector2 margin = new Vector2(12f, 12f);
    private OpenFieldPrototypeController owner;
    private RectTransform canvasRect;
    private readonly Vector3[] corners = new Vector3[4];

    public Rect ScreenRect => ScreenBounds(panelRect);
    public Rect FrameScreenRect => ScreenBounds(frameRect);

    public void Initialize(OpenFieldPrototypeController controller)
    {
        owner = controller != null ? controller : throw new ArgumentNullException(nameof(controller));
        if (canvas == null || panelRect == null || frameRect == null || atp == null || life == null ||
            round == null || roundTotal == null || defenseText == null)
            throw new InvalidOperationException("Battle HUD prefab references are incomplete.");
        OpenFieldUiResources artwork = OpenFieldUiResources.LoadDefaults();
        if (artwork == null || !artwork.HasBattleHudArtwork)
            throw new InvalidOperationException("Battle HUD artwork is missing from default UI resources.");
        MobileDisplay.ConfigureCanvas(canvas);
        canvasRect = canvas.GetComponent<RectTransform>();
        atp.Initialize(artwork);
        life.Initialize(artwork);
        round.Initialize(artwork);
        roundTotal.Initialize(artwork);
        Refresh();
    }

    public void Refresh()
    {
        if (owner == null || canvas == null) return;
        // Resource values remain visible while the pause overlay blocks gameplay input.
        bool visible = owner.isActiveAndEnabled && owner.HasActiveRun;
        if (canvas.gameObject.activeSelf != visible) canvas.gameObject.SetActive(visible);
        if (!visible) return;
        Rect safeArea = MobileDisplay.SafeArea;
        float scale = Mathf.Max(0.01f, canvas.scaleFactor);
        Vector2 size = panelRect.sizeDelta;
        float fit = Mathf.Clamp01(Mathf.Min((safeArea.width / scale - margin.x * 2f) / size.x,
            (safeArea.height / scale - margin.y * 2f) / size.y));
        panelRect.localScale = new Vector3(fit, fit, 1f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            new Vector2(safeArea.xMin, safeArea.yMax), null, out Vector2 topLeft);
        panelRect.anchoredPosition = topLeft - new Vector2(canvasRect.rect.xMin, canvasRect.rect.yMax)
            + new Vector2(margin.x, -margin.y);
        atp.SetValue(owner.CurrentATP);
        life.SetValue(owner.CurrentCoreLife);
        round.SetValue(owner.CurrentRound);
        roundTotal.SetValue(owner.TotalRounds);
        if (defenseText.text != owner.DefenseStatusText) defenseText.text = owner.DefenseStatusText;
    }

    public bool ContainsPointer(Vector2 screenPosition) => canvas != null && canvas.gameObject.activeInHierarchy &&
        RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, null);

    public void Hide()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private Rect ScreenBounds(RectTransform rect)
    {
        if (rect == null) return default;
        rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void LateUpdate() => Refresh();
    private void OnDisable() => Hide();
}
