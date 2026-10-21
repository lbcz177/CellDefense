using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Audio artwork uses its native pixel dimensions and a clipped fill inside the track.
public sealed class GameSettingsPanel : MonoBehaviour
{
    private RectTransform panel;
    private Slider volume;
    private Toggle mute, fullscreen;
    private readonly Image[] volumeDigits = new Image[3];
    private int displayedVolume = -1;
    private GameSettings settings;
    private OpenFieldUiResources artwork;
    private Image soundIcon;
    public bool IsVisible => panel != null && panel.gameObject.activeSelf;

    public void Initialize(Transform parent, OpenFieldUiResources resources, Action close)
    {
        if (resources == null || !resources.HasSettingsArtwork || !resources.HasSettingsTextArtwork ||
            !resources.HasSettingsButtonArtwork)
            throw new InvalidOperationException("Settings audio or text artwork is missing from default UI resources.");
        artwork = resources;
        settings = GameSettings.Current;
        panel = Create("Settings Panel", parent, typeof(Image)).GetComponent<RectTransform>();
        Place(panel, Vector2.zero, new Vector2(640f, 800f));
        panel.GetComponent<Image>().sprite = artwork.PausePanel;
        panel.GetComponent<Image>().preserveAspect = true;
        Artwork("Title", panel, artwork.SettingsTitle, new Vector2(0f, 256f));
        Artwork("Volume Label", panel, artwork.SettingsVolumeLabel, new Vector2(-200f, 142f), .5f);
        RectTransform valueRow = Create("Volume Value", panel).GetComponent<RectTransform>();
        Place(valueRow, new Vector2(200f, 142f), new Vector2(136f, 40f));
        Artwork("Percent", valueRow, artwork.SettingsPercent, new Vector2(48f, 0f), .5f);
        for (int i = 0; i < volumeDigits.Length; i++)
            volumeDigits[i] = Artwork("Digit " + i, valueRow, artwork.GetDigit(0), new Vector2(12f - i * 32f, 0f), .5f);
        volume = CreateSlider(panel, artwork);
        Place(volume.GetComponent<RectTransform>(), new Vector2(48f, 84f), new Vector2(448f, 64f));
        volume.onValueChanged.AddListener(value => settings.SetMasterVolume(value / 100f));
        GameObject sound = Create("Mute", panel, typeof(Image), typeof(Toggle));
        Place(sound.GetComponent<RectTransform>(), new Vector2(-240f, 84f), new Vector2(64f, 64f));
        soundIcon = sound.GetComponent<Image>();
        soundIcon.preserveAspect = true;
        mute = sound.GetComponent<Toggle>();
        mute.targetGraphic = soundIcon;
        mute.transition = Selectable.Transition.None;
        mute.toggleTransition = Toggle.ToggleTransition.None;
        mute.navigation = new Navigation { mode = Navigation.Mode.None };
        mute.onValueChanged.AddListener(settings.SetMuted);
        Artwork("Mute Hint", panel, artwork.SettingsMuteHint, new Vector2(0f, 4f));
        fullscreen = CreateToggle("Fullscreen", panel, artwork.SettingsFullscreenLabel, new Vector2(0f, -74f));
        fullscreen.onValueChanged.AddListener(settings.SetFullscreen);
        fullscreen.interactable = GameSettings.SupportsDisplayMode;
        Image displayHint = Artwork("Display Hint", panel, GameSettings.SupportsDisplayMode ? artwork.SettingsWindowHint :
            artwork.SettingsEditorHint, new Vector2(0f, -128f));
        if (MobileDisplay.IsActive)
        {
            fullscreen.gameObject.SetActive(false);
            displayHint.gameObject.SetActive(false);
        }
        CreateButton("Restore Defaults", panel, artwork.SettingsDefaultsLabel, artwork.SettingsDefaultsFrame,
            artwork.SettingsDefaultsFrameSelected, new Vector2(0f, -206f), settings.RestoreDefaults);
        CreateButton("Back to Pause", panel, artwork.SettingsBackLabel, artwork.SettingsBackFrame,
            artwork.SettingsBackFrameSelected, new Vector2(0f, -294f), () => close());
        settings.Changed += Refresh;
        Refresh();
        Hide();
    }

    public void Show()
    {
        Refresh();
        panel.gameObject.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            panel != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel))
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void Position(Vector2 center, float scale)
    {
        panel.anchoredPosition = center;
        panel.localScale = Vector3.one * scale;
    }

    private void Refresh()
    {
        volume.SetValueWithoutNotify(Mathf.RoundToInt(settings.MasterVolume * 100f));
        RefreshVolumeValue(Mathf.RoundToInt(settings.MasterVolume * 100f));
        mute.SetIsOnWithoutNotify(settings.Muted);
        soundIcon.sprite = settings.Muted || settings.MasterVolume <= 0f
            ? artwork.SettingsMutedIcon : artwork.SettingsSoundIcon;
        fullscreen.SetIsOnWithoutNotify(settings.Fullscreen);
    }

    private void RefreshVolumeValue(int value)
    {
        if (displayedVolume == value) return;
        displayedVolume = value;
        int count = value >= 100 ? 3 : value >= 10 ? 2 : 1;
        for (int i = 0; i < volumeDigits.Length; i++)
        {
            volumeDigits[i].gameObject.SetActive(i < count);
            if (i >= count) continue;
            volumeDigits[i].sprite = artwork.GetDigit(value % 10);
            value /= 10;
        }
    }

    private void OnDestroy()
    {
        if (settings != null) settings.Changed -= Refresh;
    }

    private static Slider CreateSlider(Transform parent, OpenFieldUiResources artwork)
    {
        GameObject root = Create("Master Volume", parent, typeof(Image), typeof(Slider));
        root.GetComponent<Image>().color = Color.clear;
        Slider slider = root.GetComponent<Slider>();
        Image track = Create("Track", root.transform, typeof(Image)).GetComponent<Image>();
        Place(track.rectTransform, Vector2.zero, new Vector2(448f, 48f));
        track.sprite = artwork.SettingsVolumeTrack;
        track.type = Image.Type.Sliced;
        track.raycastTarget = false;
        RectTransform fillArea = Create("Fill Area", root.transform).GetComponent<RectTransform>();
        Place(fillArea, Vector2.zero, new Vector2(416f, 16f));
        RectTransform reveal = Create("Fill Reveal", fillArea, typeof(RectMask2D)).GetComponent<RectTransform>();
        Stretch(reveal);
        Image fill = Create("Fill", reveal, typeof(Image)).GetComponent<Image>();
        // The 432x32 image has 8 transparent pixels around its 416x16 blue interior.
        // Clip a fixed image instead of shrinking its pixels or counting that padding as volume.
        fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0f, .5f);
        fill.rectTransform.pivot = new Vector2(0f, .5f);
        fill.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
        fill.rectTransform.sizeDelta = new Vector2(432f, 32f);
        fill.sprite = artwork.SettingsVolumeFill;
        fill.raycastTarget = false;
        RectTransform handleArea = Create("Handle Area", root.transform).GetComponent<RectTransform>();
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(16f, 0f);
        handleArea.offsetMax = new Vector2(-16f, 0f);
        Image handle = Create("Handle", handleArea, typeof(Image)).GetComponent<Image>();
        // Slider stretches the handle vertically, so zero delta gives the area's 64px height.
        handle.rectTransform.sizeDelta = new Vector2(48f, 0f);
        handle.sprite = artwork.SettingsVolumeHandle;
        slider.fillRect = reveal;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, Sprite caption, Vector2 point)
    {
        GameObject root = Create(name, parent, typeof(Toggle));
        Place(root.GetComponent<RectTransform>(), point, new Vector2(416f, 56f));
        Artwork("Label", root.transform, caption, new Vector2(-168f, 0f), .5f);
        Image background = Create("Box", root.transform, typeof(Image)).GetComponent<Image>();
        Place(background.rectTransform, new Vector2(184f, 0f), new Vector2(48f, 48f));
        background.color = new Color(.95f, .84f, .65f);
        Image check = Create("Check", background.transform, typeof(Image)).GetComponent<Image>();
        Place(check.rectTransform, Vector2.zero, new Vector2(28f, 28f));
        check.color = new Color(.16f, .52f, .65f);
        // The entire row is a hit target, including its text.
        Image hitArea = root.AddComponent<Image>();
        hitArea.color = Color.clear;
        Toggle toggle = root.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = check;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        return toggle;
    }

    private static void CreateButton(string name, Transform parent, Sprite caption, Sprite frame, Sprite selectedFrame,
        Vector2 point, UnityEngine.Events.UnityAction action)
    {
        GameObject root = Create(name, parent, typeof(Image), typeof(Button));
        Place(root.GetComponent<RectTransform>(), point, new Vector2(448f, 80f));
        Image image = root.GetComponent<Image>();
        image.sprite = frame;
        image.type = Image.Type.Sliced;
        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = selectedFrame,
            pressedSprite = selectedFrame,
            selectedSprite = selectedFrame,
            disabledSprite = frame
        };
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Artwork("Label", root.transform, caption, Vector2.zero, .5f);
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            action();
        });
    }

    private static Image Artwork(string name, Transform parent, Sprite sprite, Vector2 point, float scale = 1f)
    {
        Image image = Create(name, parent, typeof(Image)).GetComponent<Image>();
        Place(image.rectTransform, point, sprite.rect.size * scale);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(parent, false);
        foreach (Type component in components) root.AddComponent(component);
        return root;
    }

    private static void Place(RectTransform rect, Vector2 point, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
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
