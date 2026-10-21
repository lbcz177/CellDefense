using UnityEngine;

// Defaults also work for scenes that were already open when a new UI field was added.
[CreateAssetMenu(fileName = "OpenFieldUiResources", menuName = "Cell Defense/Open Field UI Resources")]
public sealed class OpenFieldUiResources : ScriptableObject
{
    [SerializeField] private Sprite waveButtonArtwork;
    [SerializeField] private OpenFieldBattleHud battleHudPrefab;
    public OpenFieldBattleHud BattleHudPrefab => battleHudPrefab;
    [Header("Battle HUD")]
    [SerializeField] private Sprite hudFrame;
    [SerializeField] private Sprite atpIcon;
    [SerializeField] private Sprite atpLabel;
    [SerializeField] private Sprite lifeIcon;
    [SerializeField] private Sprite lifeLabel;
    [SerializeField] private Sprite waveIcon;
    [SerializeField] private Sprite waveLabel;
    [SerializeField] private Sprite numberSlash;
    [SerializeField] private Sprite[] digits;

    [Header("Playback Toolbar")]
    [SerializeField] private Sprite playbackPauseNormal;
    [SerializeField] private Sprite playbackPauseHighlighted;
    [SerializeField] private Sprite playbackNormalSpeedNormal;
    [SerializeField] private Sprite playbackNormalSpeedHighlighted;
    [SerializeField] private Sprite playbackDoubleSpeedNormal;
    [SerializeField] private Sprite playbackDoubleSpeedHighlighted;
    public Sprite PlaybackPauseNormal => playbackPauseNormal;
    public Sprite PlaybackPauseHighlighted => playbackPauseHighlighted;
    public Sprite PlaybackNormalSpeedNormal => playbackNormalSpeedNormal;
    public Sprite PlaybackNormalSpeedHighlighted => playbackNormalSpeedHighlighted;
    public Sprite PlaybackDoubleSpeedNormal => playbackDoubleSpeedNormal;
    public Sprite PlaybackDoubleSpeedHighlighted => playbackDoubleSpeedHighlighted;
    public bool HasPlaybackArtwork => playbackPauseNormal != null && playbackPauseHighlighted != null &&
        playbackNormalSpeedNormal != null && playbackNormalSpeedHighlighted != null &&
        playbackDoubleSpeedNormal != null && playbackDoubleSpeedHighlighted != null;

    [Header("Pause Menu")]
    [SerializeField] private Sprite pausePanel;
    [SerializeField] private Sprite pauseTitle;
    [SerializeField] private Sprite pauseResumeNormal;
    [SerializeField] private Sprite pauseResumeSelected;
    [SerializeField] private Sprite pauseSettingsNormal;
    [SerializeField] private Sprite pauseSettingsSelected;
    [SerializeField] private Sprite pauseMenuNormal;
    [SerializeField] private Sprite pauseMenuSelected;
    public Sprite PausePanel => pausePanel;
    public Sprite PauseTitle => pauseTitle;
    public Sprite PauseResumeNormal => pauseResumeNormal;
    public Sprite PauseResumeSelected => pauseResumeSelected;
    public Sprite PauseSettingsNormal => pauseSettingsNormal;
    public Sprite PauseSettingsSelected => pauseSettingsSelected;
    public Sprite PauseMenuNormal => pauseMenuNormal;
    public Sprite PauseMenuSelected => pauseMenuSelected;
    public bool HasPauseArtwork => pausePanel != null && pauseTitle != null &&
        pauseResumeNormal != null && pauseResumeSelected != null && pauseSettingsNormal != null &&
        pauseSettingsSelected != null && pauseMenuNormal != null && pauseMenuSelected != null;

    [Header("Settings Audio")]
    [SerializeField] private Sprite settingsSoundIcon;
    [SerializeField] private Sprite settingsMutedIcon;
    [SerializeField] private Sprite settingsVolumeTrack;
    [SerializeField] private Sprite settingsVolumeFill;
    [SerializeField] private Sprite settingsVolumeHandle;
    public Sprite SettingsSoundIcon => settingsSoundIcon;
    public Sprite SettingsMutedIcon => settingsMutedIcon;
    public Sprite SettingsVolumeTrack => settingsVolumeTrack;
    public Sprite SettingsVolumeFill => settingsVolumeFill;
    public Sprite SettingsVolumeHandle => settingsVolumeHandle;
    public bool HasSettingsArtwork => settingsSoundIcon != null && settingsMutedIcon != null &&
        settingsVolumeTrack != null && settingsVolumeFill != null && settingsVolumeHandle != null;

    [Header("Settings Text")]
    [SerializeField] private Sprite settingsTitle;
    [SerializeField] private Sprite settingsVolumeLabel;
    [SerializeField] private Sprite settingsFullscreenLabel;
    [SerializeField] private Sprite settingsDefaultsLabel;
    [SerializeField] private Sprite settingsBackLabel;
    [SerializeField] private Sprite settingsMuteHint;
    [SerializeField] private Sprite settingsWindowHint;
    [SerializeField] private Sprite settingsEditorHint;
    [SerializeField] private Sprite settingsPercent;
    public Sprite SettingsTitle => settingsTitle;
    public Sprite SettingsVolumeLabel => settingsVolumeLabel;
    public Sprite SettingsFullscreenLabel => settingsFullscreenLabel;
    public Sprite SettingsDefaultsLabel => settingsDefaultsLabel;
    public Sprite SettingsBackLabel => settingsBackLabel;
    public Sprite SettingsMuteHint => settingsMuteHint;
    public Sprite SettingsWindowHint => settingsWindowHint;
    public Sprite SettingsEditorHint => settingsEditorHint;
    public Sprite SettingsPercent => settingsPercent;
    [Header("Settings Buttons")]
    [SerializeField] private Sprite settingsDefaultsFrame;
    [SerializeField] private Sprite settingsDefaultsFrameSelected;
    [SerializeField] private Sprite settingsBackFrame;
    [SerializeField] private Sprite settingsBackFrameSelected;
    public Sprite SettingsDefaultsFrame => settingsDefaultsFrame;
    public Sprite SettingsDefaultsFrameSelected => settingsDefaultsFrameSelected;
    public Sprite SettingsBackFrame => settingsBackFrame;
    public Sprite SettingsBackFrameSelected => settingsBackFrameSelected;
    public bool HasSettingsButtonArtwork => settingsDefaultsFrame != null && settingsDefaultsFrameSelected != null &&
        settingsBackFrame != null && settingsBackFrameSelected != null;
    public bool HasSettingsTextArtwork => settingsTitle != null && settingsVolumeLabel != null &&
        settingsFullscreenLabel != null && settingsDefaultsLabel != null && settingsBackLabel != null &&
        settingsMuteHint != null && settingsWindowHint != null && settingsEditorHint != null &&
        settingsPercent != null && HasNumberArtwork;
    public bool HasNumberArtwork
    {
        get
        {
            if (digits == null || digits.Length != 10) return false;
            foreach (Sprite digit in digits) if (digit == null) return false;
            return true;
        }
    }

    public Sprite HudFrame => hudFrame;
    public Sprite AtpIcon => atpIcon;
    public Sprite AtpLabel => atpLabel;
    public Sprite LifeIcon => lifeIcon;
    public Sprite LifeLabel => lifeLabel;
    public Sprite WaveIcon => waveIcon;
    public Sprite WaveLabel => waveLabel;
    public Sprite NumberSlash => numberSlash;
    public Sprite GetDigit(int digit) => digits[digit];
    public bool HasBattleHudArtwork
    {
        get
        {
            if (hudFrame == null || atpIcon == null || atpLabel == null || lifeIcon == null ||
                lifeLabel == null || waveIcon == null || waveLabel == null || numberSlash == null ||
                !HasNumberArtwork) return false;
            return true;
        }
    }

    public static OpenFieldUiResources LoadDefaults() => Resources.Load<OpenFieldUiResources>("OpenFieldUiResources");

    public static Sprite ResolveWaveButtonArtwork(Sprite sceneArtwork)
    {
        if (sceneArtwork != null) return sceneArtwork;
        OpenFieldUiResources defaults = LoadDefaults();
        return defaults != null ? defaults.waveButtonArtwork : null;
    }
}
