using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Entrance buttons share one wave state; none starts a separate entrance-only wave.
public sealed class OpenFieldEntranceWaveButtons : MonoBehaviour
{
    private const float Width = 104f;
    private const float Height = 132f;
    private const float IconSize = 88f;
    private OpenFieldPrototypeController owner;
    private Camera gameplayCamera;
    private RectTransform canvasRect;
    private Canvas overlayCanvas;
    private GameObject canvasObject;
    private Button[] buttons;
    private RectTransform[] buttonRects;
    private PixelImageText[] labels;
    private Vector3[] worldPositions;
    private Vector2[] offsets;
    private int displayedRound = -1;
    private bool displayedEarly;
    public RectTransform FirstVisibleButton => canvasObject != null && canvasObject.activeInHierarchy &&
        buttonRects.Length > 0 && buttonRects[0].gameObject.activeInHierarchy ? buttonRects[0] : null;

    public void Initialize(OpenFieldPrototypeController controller, OpenFieldGridModel grid,
        Func<Vector2Int, Vector3> cellToWorld, TMP_FontAsset font, Camera camera, Sprite artwork)
    {
        if (controller == null || grid == null || cellToWorld == null || font == null || camera == null || artwork == null)
            throw new ArgumentException("Entrance wave buttons require a level, grid, font, camera and artwork.");
        owner = controller;
        gameplayCamera = camera;
        canvasObject = new GameObject("Entrance Wave Buttons Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas = canvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        MobileDisplay.ConfigureCanvas(canvas);
        canvasRect = canvasObject.GetComponent<RectTransform>();
        buttons = new Button[grid.SpawnCount];
        buttonRects = new RectTransform[grid.SpawnCount];
        labels = new PixelImageText[grid.SpawnCount];
        worldPositions = new Vector3[grid.SpawnCount];
        offsets = new Vector2[grid.SpawnCount];
        for (int i = 0; i < grid.SpawnCount; i++)
        {
            Vector2Int cell = grid.GetSpawnCell(i);
            worldPositions[i] = cellToWorld(cell);
            // The map reaches the screen edge now. Nudge the icon into the mouth and
            // account for the caption below it when positioning the button's root.
            offsets[i] = (cell.x == 0 ? Vector2.right * 16f :
                cell.x == grid.Width - 1 ? Vector2.left * 16f :
                cell.y == 0 ? Vector2.up * 16f :
                cell.y == grid.Height - 1 ? Vector2.down * 16f : Vector2.zero)
                + Vector2.down * ((Height - IconSize) * 0.5f);
            GameObject obj = CreateUI($"Entrance {i + 1} Wave Button", canvasRect, typeof(Button));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Width, Height);
            buttonRects[i] = rect;
            GameObject icon = CreateUI("Wave Button Artwork", obj.transform, typeof(Image));
            Image image = icon.GetComponent<Image>();
            image.sprite = artwork;
            image.preserveAspect = true;
            image.color = Color.white;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            image.rectTransform.pivot = new Vector2(0.5f, 1f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = Vector2.one * IconSize;
            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.97f, 0.91f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.78f, 0.72f, 0.72f, 1f);
            colors.disabledColor = new Color(0.52f, 0.52f, 0.52f, 0.8f);
            button.colors = colors;
            button.onClick.AddListener(() => { owner.StartAssault(); });
            buttons[i] = button;
            GameObject caption = CreateUI("Wave Caption", obj.transform, typeof(Image));
            Image captionBackground = caption.GetComponent<Image>();
            captionBackground.color = new Color(0.23f, 0.07f, 0.16f, 0.92f);
            captionBackground.rectTransform.anchorMin = captionBackground.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            captionBackground.rectTransform.pivot = new Vector2(0.5f, 0f);
            captionBackground.rectTransform.anchoredPosition = Vector2.zero;
            captionBackground.rectTransform.sizeDelta = new Vector2(Width, 40f);
            GameObject text = CreateUI("Label", caption.transform, typeof(PixelImageText));
            PixelImageText label = text.GetComponent<PixelImageText>();

            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.96f, 0.82f, 1f);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(4f, 3f);
            label.rectTransform.offsetMax = new Vector2(-4f, -3f);
            labels[i] = label;
        }
        Refresh();
    }

    public void Refresh()
    {
        if (owner == null || canvasObject == null) return;
        int entranceCount = owner.UpcomingEntranceCount;
        bool visible = owner.isActiveAndEnabled && owner.CanStartWave && entranceCount > 0 &&
            (owner.Phase == OpenFieldPhase.Planning || owner.Phase == OpenFieldPhase.Assault);
        if (canvasObject.activeSelf != visible) canvasObject.SetActive(visible);
        if (!visible) return;

        bool early = owner.Phase == OpenFieldPhase.Assault;
        int round = owner.UpcomingRound;
        if (displayedRound != round || displayedEarly != early)
        {
            string caption = $"第{round}波\n{(early ? "提前开始" : "开始")}";
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = true;
                labels[i].text = caption;
            }
            displayedRound = round;
            displayedEarly = early;
        }
        Rect bounds = canvasRect.rect;
        // Keep the whole icon/caption inside the device's usable area.
        Rect safeArea = MobileDisplay.SafeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safeArea.min, null, out Vector2 safeMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safeArea.max, null, out Vector2 safeMax);
        float minX = Mathf.Max(bounds.xMin, safeMin.x);
        float maxX = Mathf.Min(bounds.xMax, safeMax.x);
        float minY = Mathf.Max(bounds.yMin, safeMin.y);
        float maxY = Mathf.Min(bounds.yMax, safeMax.y);
        Rect hud = owner.BattleHudScreenRect;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            new Vector2(hud.xMin, hud.yMin), null, out Vector2 hudMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
            new Vector2(hud.xMax, hud.yMax), null, out Vector2 hudMax);
        for (int i = 0; i < buttons.Length; i++)
        {
            Vector3 screen = gameplayCamera.WorldToScreenPoint(worldPositions[i]);
            bool show = i < entranceCount && screen.z > 0f;
            if (buttons[i].gameObject.activeSelf != show) buttons[i].gameObject.SetActive(show);
            if (!show) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            local += offsets[i];
            float xPadding = Mathf.Min(Width * 0.5f + 8f, (maxX - minX) * 0.5f);
            float yPadding = Mathf.Min(Height * 0.5f + 8f, (maxY - minY) * 0.5f);
            float yMax = maxY - yPadding;
            if (hud.width > 0f && local.x + Width * 0.5f > hudMin.x && local.x - Width * 0.5f < hudMax.x)
                yMax = Mathf.Min(yMax, hudMin.y - 8f / Mathf.Max(0.01f, overlayCanvas.scaleFactor) - Height * 0.5f);
            yMax = Mathf.Max(minY + yPadding, yMax);
            buttonRects[i].anchoredPosition = new Vector2(
                Mathf.Clamp(local.x, minX + xPadding, maxX - xPadding),
                Mathf.Clamp(local.y, minY + yPadding, yMax));
        }
    }

    public bool ContainsPointer(Vector2 screenPosition)
    {
        if (canvasObject == null || !canvasObject.activeInHierarchy) return false;
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i].gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(buttonRects[i],
                    screenPosition, null)) return true;
        return false;
    }

    public void Hide()
    {
        if (canvasObject != null) canvasObject.SetActive(false);
    }

    private void LateUpdate() => Refresh();
    private void OnDisable() => Hide();

    private static GameObject CreateUI(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        foreach (Type component in components) obj.AddComponent(component);
        return obj;
    }
}
