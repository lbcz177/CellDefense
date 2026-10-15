using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Context choices use the original HUD's icon, price and hover-preview layout.
public sealed class OpenFieldBuildMenu : MonoBehaviour
{
    private const float CardSize = 112f;
    private const float Gap = 8f;
    private RectTransform canvasRect;
    private RectTransform panelRect;
    private Camera gameplayCamera;
    private TowerDefinition[] definitions;
    private Button[] buttons;
    private Image[] icons;
    private TextMeshProUGUI[] labels;
    private Func<int, string> rejectionReason;
    private Func<int, bool> canBuild;
    private Vector3 cellPosition;
    public bool IsVisible => panelRect != null && panelRect.gameObject.activeSelf;

    public void Initialize(TowerDefinition[] choices, TMP_FontAsset font, Camera camera,
        Func<int, bool> isAvailable, Action<int> onChoose, Action<int> onPreview, Action onCancel,
        Func<int, string> getRejectionReason = null)
    {
        if (choices == null || choices.Length == 0 || font == null || camera == null ||
            isAvailable == null || onChoose == null || onPreview == null || onCancel == null)
            throw new ArgumentException("Build menu requires choices, font, camera and callbacks.");

        definitions = choices;
        gameplayCamera = camera;
        canBuild = isAvailable;
        rejectionReason = getRejectionReason;
        GameObject canvasObject = new GameObject("Build Choices Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject panel = CreateUIObject("Tower Choices", canvasRect, typeof(Image));
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.21f, 0.96f);
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI title = CreateLabel(panelRect, font, "选择防御细胞", 18f);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 26f));

        buttons = new Button[choices.Length];
        icons = new Image[choices.Length];
        labels = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            TowerDefinition definition = choices[i];
            GameObject card = CreateUIObject($"Tower Choice {i + 1}", panelRect, typeof(Image), typeof(Button));
            Image background = card.GetComponent<Image>();
            background.color = new Color(0.15f, 0.19f, 0.28f, 0.92f);
            Button button = card.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => onChoose(index));
            card.AddComponent<TowerBuildChoiceHover>().Initialize(
                () => onPreview(index), () => onPreview(-1));
            buttons[i] = button;

            GameObject iconObject = CreateUIObject("Tower Icon", card.transform, typeof(Image));
            Image icon = iconObject.GetComponent<Image>();
            SpriteRenderer source = definition != null && definition.Prefab != null
                ? definition.Prefab.GetComponentInChildren<SpriteRenderer>() : null;
            icon.sprite = definition != null && definition.BuildIcon != null ? definition.BuildIcon
                : source != null ? source.sprite : null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(58f, 58f));
            icons[i] = icon;

            string labelText = ChoiceLabel(i);
            TextMeshProUGUI label = CreateLabel(card.transform, font, labelText, 16f);
            labels[i] = label;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 16f;
            SetRect(label.rectTransform, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(-4f, 46f));
        }

        GameObject cancelObject = CreateUIObject("Cancel Build", panelRect, typeof(Image), typeof(Button));
        Image cancelBackground = cancelObject.GetComponent<Image>();
        cancelBackground.color = new Color(0.25f, 0.32f, 0.4f, 1f);
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelBackground;
        cancel.onClick.AddListener(() => onCancel());
        SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(152f, 28f));
        TextMeshProUGUI cancelLabel = CreateLabel(cancelObject.transform, font, "取消（右键 / Esc）", 14f);
        cancelLabel.rectTransform.anchorMin = Vector2.zero;
        cancelLabel.rectTransform.anchorMax = Vector2.one;
        cancelLabel.rectTransform.offsetMin = cancelLabel.rectTransform.offsetMax = Vector2.zero;
        Hide();
    }

    public void Show(Vector3 worldPosition)
    {
        cellPosition = worldPosition;
        panelRect.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Refresh();
    }

    public void Hide()
    {
        if (panelRect != null) panelRect.gameObject.SetActive(false);
    }

    // Also check geometry: EventSystem may process a fresh pointer after the board Update.
    public bool ContainsPointer(Vector2 screenPosition)
    {
        return IsVisible && RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition);
    }

    private void LateUpdate()
    {
        if (IsVisible) Refresh();
    }

    public void Refresh()
    {
        if (!IsVisible) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool available = definitions[i] != null && canBuild(i);
            buttons[i].interactable = available;
            icons[i].color = available ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            string text = ChoiceLabel(i);
            if (labels[i].text != text) labels[i].text = text;
        }

        Rect bounds = canvasRect.rect;
        int columns = Mathf.Clamp(Mathf.FloorToInt((bounds.width - 32f) / (CardSize + Gap)), 1, 3);
        columns = Mathf.Min(columns, buttons.Length);
        int rows = (buttons.Length + columns - 1) / columns;
        Vector2 size = new Vector2(16f + columns * CardSize + (columns - 1) * Gap,
            80f + rows * CardSize + (rows - 1) * Gap);
        panelRect.sizeDelta = size;
        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rect = buttons[i].GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f + i % columns * (CardSize + Gap),
                    -38f - i / columns * (CardSize + Gap)), new Vector2(CardSize, CardSize));
        }

        Vector2 screen = gameplayCamera.WorldToScreenPoint(cellPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        // Keep the panel beneath the compact planning toolbar at the top of the screen.
        float topLimit = bounds.yMax - 112f / canvasRect.GetComponent<Canvas>().scaleFactor;
        float preferredY = local.y + halfHeight + 48f;
        if (preferredY + halfHeight > topLimit)
            preferredY = local.y - halfHeight - 48f;
        float xMin = bounds.xMin + halfWidth + 8f;
        float xMax = bounds.xMax - halfWidth - 8f;
        float yMin = bounds.yMin + halfHeight + 8f;
        float yMax = topLimit - halfHeight;
        panelRect.anchoredPosition = new Vector2(
            xMin <= xMax ? Mathf.Clamp(local.x, xMin, xMax) : bounds.center.x,
            yMin <= yMax ? Mathf.Clamp(preferredY, yMin, yMax) : bounds.center.y);
    }

    private string ChoiceLabel(int index)
    {
        TowerDefinition definition = definitions[index];
        if (definition == null) return "未配置";
        string reason = rejectionReason != null ? rejectionReason(index) : string.Empty;
        string role = definition.BlocksEnemyRoute ? "改路 · 消耗壁垒额度" :
            $"可承伤 · 拦截 {definition.InterceptCapacity}";
        return $"{definition.DisplayName}\n{definition.BuildCost} ATP\n" +
            (string.IsNullOrEmpty(reason) ? role : reason);
    }

    private static GameObject CreateUIObject(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        foreach (Type component in components) obj.AddComponent(component);
        return obj;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, TMP_FontAsset font, string text, float size)
    {
        GameObject obj = CreateUIObject("Label", parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnDisable() => Hide();
}

