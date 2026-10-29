using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small dismissible hint also previews explicit entrance scheduling before opening a wave.
public sealed class ChapterTwoBriefing : MonoBehaviour
{
    [SerializeField, TextArea] private string introduction;
    private OpenFieldPrototypeController owner;
    private Canvas canvas;
    private PixelImageText label;
    private int shownRound = -1;
    private bool dismissed;
    private void Start()
    {
        owner = GetComponent<OpenFieldPrototypeController>();
        var root = new GameObject("Chapter Two Briefing", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);root.layer = LayerMask.NameToLayer("UI");
        canvas = root.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceOverlay;canvas.sortingOrder = 7;
        var scaler = root.GetComponent<CanvasScaler>();scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);scaler.matchWidthOrHeight = .5f;
        MobileDisplay.ConfigureCanvas(canvas);
        var panel = new GameObject("Chapter hint (click to close)", typeof(RectTransform), typeof(Image), typeof(Button));
        panel.layer = root.layer;panel.transform.SetParent(root.transform, false);
        var rect = panel.GetComponent<RectTransform>();rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(16, 16);rect.sizeDelta = new Vector2(400, 188);
        PromptImageResources.StylePanel(panel.GetComponent<Image>());
        panel.GetComponent<Button>().onClick.AddListener(() => { dismissed = true;canvas.enabled = false; });
        var text = new GameObject("Chapter hint text", typeof(RectTransform), typeof(PixelImageText));
        text.layer = root.layer;text.transform.SetParent(panel.transform, false);
        label = text.GetComponent<PixelImageText>();label.fontSize = 16;label.enableAutoSizing = true;
        label.fontSizeMin = 12;label.fontSizeMax = 16;label.color = new Color(1f, .95f, .8f);label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.rectTransform.anchorMin = Vector2.zero;label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(12, 10);label.rectTransform.offsetMax = new Vector2(-12, -10);
        MobileSafeFrame.Wrap(canvas, new Vector2(1280, 720));
        Update();
    }
    private void Update()
    {
        if (canvas == null || owner == null) return;
        if (shownRound != owner.UpcomingRound)
        {
            shownRound = owner.UpcomingRound;dismissed = false;
            label.text = owner.CurrentRound == 1 && owner.Phase == OpenFieldPhase.Planning
                ? introduction + "\n下一波：" + owner.UpcomingWaveSummary + "\n点击关闭提示"
                : $"第{shownRound}波\n" + owner.UpcomingWaveSummary.Replace(" / ", "\n") + "\n点击关闭提示";
        }
        canvas.enabled = owner.isActiveAndEnabled && owner.HasActiveRun && !owner.IsPaused && !dismissed && owner.UpcomingRound <= owner.TotalRounds;
    }
    private void OnDisable() { if (canvas != null) canvas.enabled = false; }
}
