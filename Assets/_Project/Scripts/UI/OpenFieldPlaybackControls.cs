using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pause artwork is shared through Resources; round state and time control remain in the owner.
public sealed class OpenFieldPlaybackControls : MonoBehaviour
{
    private OpenFieldPrototypeController owner;
    private GameObject canvasObject, pauseOverlay;
    private RectTransform canvasRect, toolbar, pausePanel;
    private Canvas canvas;
    private Button pauseButton, speedButton;
    private OpenFieldUiResources artwork;
    private GameSettingsPanel settingsPanel;
    private int displayedSpeed;
    public bool IsPauseVisible => pauseOverlay != null && pauseOverlay.activeInHierarchy;
    public bool IsVisible => canvasObject != null && canvasObject.activeInHierarchy;
    public bool IsSettingsVisible => IsPauseVisible && settingsPanel != null && settingsPanel.IsVisible;

    public void Initialize(OpenFieldPrototypeController controller, TMP_FontAsset font)
    {
        if (controller == null || font == null)
            throw new ArgumentException("Playback controls require a controller and Chinese font.");
        artwork = OpenFieldUiResources.LoadDefaults();
        if (artwork == null || !artwork.HasPauseArtwork)
            throw new InvalidOperationException("Pause menu artwork is missing from default UI resources.");
        if (!artwork.HasPlaybackArtwork)
            throw new InvalidOperationException("Pause and speed button artwork is missing from default UI resources.");
        owner = controller;
        canvasObject = Create("Playback Controls Canvas", transform, typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = 25;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = .5f;
        MobileDisplay.ConfigureCanvas(canvas);
        canvasRect = canvasObject.GetComponent<RectTransform>();

        toolbar = Create("Playback Toolbar", canvasRect).GetComponent<RectTransform>();
        toolbar.anchorMin = toolbar.anchorMax = toolbar.pivot = Vector2.one;
        toolbar.sizeDelta = new Vector2(272f, 72f);
        pauseButton = ArtworkButton("Pause", toolbar, artwork.PlaybackPauseNormal,
            artwork.PlaybackPauseHighlighted, () => owner.Pause());
        Place(pauseButton.GetComponent<RectTransform>(), new Vector2(-68f, 0f), new Vector2(128f, 64f));
        speedButton = ArtworkButton("Playback Speed", toolbar, artwork.PlaybackNormalSpeedNormal,
            artwork.PlaybackNormalSpeedHighlighted, () => owner.TogglePlaybackSpeed());
        Place(speedButton.GetComponent<RectTransform>(), new Vector2(68f, 0f), new Vector2(128f, 64f));

        pauseOverlay = Create("Pause Overlay", canvasRect, typeof(Image));
        Stretch(pauseOverlay.GetComponent<RectTransform>());
        pauseOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, .6f);
        // This full-screen raycast target blocks the board, shop, entrance buttons and speed control.
        pausePanel = Create("Pause Panel", pauseOverlay.transform, typeof(Image)).GetComponent<RectTransform>();
        Image panelImage = pausePanel.GetComponent<Image>();
        panelImage.sprite = artwork.PausePanel;
        panelImage.preserveAspect = true;
        Place(pausePanel, Vector2.zero, new Vector2(640f, 800f));
        Image title = Create("Pause Title", pausePanel, typeof(Image)).GetComponent<Image>();
        title.sprite = artwork.PauseTitle;
        title.preserveAspect = true;
        title.raycastTarget = false;
        PlaceArtwork(title.rectTransform, 112f);
        Button resume = ArtworkButton("Resume", pausePanel, artwork.PauseResumeNormal,
            artwork.PauseResumeSelected, () => owner.Resume());
        PlaceArtwork(resume.GetComponent<RectTransform>(), 304f);
        Button settings = ArtworkButton("Settings", pausePanel, artwork.PauseSettingsNormal,
            artwork.PauseSettingsSelected, OpenSettings);
        PlaceArtwork(settings.GetComponent<RectTransform>(), 440f);
        Button menu = ArtworkButton("Return to Main Menu", pausePanel, artwork.PauseMenuNormal,
            artwork.PauseMenuSelected, owner.ReturnToMainMenu);
        PlaceArtwork(menu.GetComponent<RectTransform>(), 576f);
        settingsPanel = gameObject.AddComponent<GameSettingsPanel>();
        settingsPanel.Initialize(pauseOverlay.transform, artwork, CloseSettings);
        // CanvasScaler updates in preWillRenderCanvases. Register after it so the first
        // displayed frame and a resized window both use the actual canvas scale.
        Canvas.preWillRenderCanvases += PositionControls;
        Refresh();
    }

    public void Refresh()
    {
        if (owner == null || canvasObject == null) return;
        bool visible = owner.isActiveAndEnabled && owner.HasActiveRun;
        if (canvasObject.activeSelf != visible) canvasObject.SetActive(visible);
        if (!visible || !owner.IsPaused) CloseSettings();
        if (!visible) return;
        if (pauseOverlay.activeSelf != owner.IsPaused) pauseOverlay.SetActive(owner.IsPaused);
        pauseButton.interactable = speedButton.interactable = !owner.IsPaused;
        if (displayedSpeed != owner.PlaybackSpeed)
        {
            displayedSpeed = owner.PlaybackSpeed;
            SetButtonArtwork(speedButton,
                displayedSpeed == 2 ? artwork.PlaybackDoubleSpeedNormal : artwork.PlaybackNormalSpeedNormal,
                displayedSpeed == 2 ? artwork.PlaybackDoubleSpeedHighlighted : artwork.PlaybackNormalSpeedHighlighted);
        }
        PositionControls();
    }

    private void PositionControls()
    {
        if (owner == null || !IsVisible) return;
        Rect safe = MobileDisplay.SafeArea;
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        float toolbarFit = Mathf.Clamp01((safe.width / scale - 24f) / toolbar.sizeDelta.x);
        toolbar.localScale = Vector3.one * toolbarFit;
        float panelFit = Mathf.Clamp01(Mathf.Min((safe.width / scale - 24f) / 640f,
            (safe.height / scale - 24f) / 800f));
        pausePanel.localScale = Vector3.one * panelFit;
        Vector2 topRight = new Vector2(safe.xMax - 12f, safe.yMax - 12f);
        // On narrow windows put the toolbar below the HUD instead of covering its digits.
        Rect hud = owner.BattleHudScreenRect;
        Rect bounds = new Rect(topRight.x - toolbar.sizeDelta.x * toolbarFit * scale,
            topRight.y - toolbar.sizeDelta.y * toolbarFit * scale,
            toolbar.sizeDelta.x * toolbarFit * scale, toolbar.sizeDelta.y * toolbarFit * scale);
        if (hud.width > 0f && bounds.Overlaps(hud)) topRight.y = hud.yMin - 12f;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, topRight, eventCamera, out Vector2 point);
        toolbar.anchoredPosition = point - canvasRect.rect.max;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safe.center, eventCamera, out Vector2 center);
        pausePanel.anchoredPosition = center;
        settingsPanel.Position(center, panelFit);
    }

    public void OpenSettings()
    {
        if (owner == null || !owner.IsPaused || !owner.HasActiveRun) return;
        pausePanel.gameObject.SetActive(false);
        settingsPanel.Show();
        PositionControls();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.Hide();
        if (pausePanel != null) pausePanel.gameObject.SetActive(true);
    }

    public bool ContainsPointer(Vector2 screenPosition) => IsVisible &&
        (IsPauseVisible || RectTransformUtility.RectangleContainsScreenPoint(toolbar, screenPosition, null));

    public void Hide()
    {
        CloseSettings();
        if (canvasObject != null) canvasObject.SetActive(false);
    }

    private void LateUpdate() => Refresh();
    private void OnEnable()
    {
        if (canvas != null) Canvas.preWillRenderCanvases += PositionControls;
    }
    private void OnDisable()
    {
        Canvas.preWillRenderCanvases -= PositionControls;
        Hide();
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        foreach (Type component in components) obj.AddComponent(component);
        return obj;
    }

    private static Button ArtworkButton(string name, Transform parent, Sprite normal, Sprite selected,
        UnityEngine.Events.UnityAction action)
    {
        GameObject obj = Create(name, parent, typeof(Image), typeof(Button));
        Image image = obj.GetComponent<Image>();
        image.preserveAspect = true;
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SetButtonArtwork(button, normal, selected);
        WireButton(button, action);
        return button;
    }

    private static void SetButtonArtwork(Button button, Sprite normal, Sprite selected)
    {
        button.image.sprite = normal;
        // Updating SpriteState reapplies the current hover/pressed/disabled state,
        // including when a speed click changes 1x to 2x without moving the pointer.
        button.spriteState = new SpriteState
        {
            highlightedSprite = selected,
            pressedSprite = selected,
            selectedSprite = selected,
            disabledSprite = normal
        };
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        // Leave Space to the wave shortcut after mouse clicks, including at timeScale zero.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (action == null) return;
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            action();
        });
    }

    // Match the Aseprite 640x800 layout; source positions are measured from its top-left corner.
    private static void PlaceArtwork(RectTransform rect, float top)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(96f, -top);
        rect.sizeDelta = new Vector2(448f, 112f);
    }

    private static void Place(RectTransform rect, Vector2 point, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = point;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
