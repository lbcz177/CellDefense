using System;
using UnityEngine;

// One shared preference set for all scenes. Test instances can use an isolated key prefix.
public sealed class GameSettings
{
    public const string PreferencePrefix = "CellDefense.Settings.v1";
    private static GameSettings current;
    private readonly string prefix;

    public static GameSettings Current => current ?? (current = new GameSettings(PreferencePrefix));
    public float MasterVolume { get; private set; }
    public bool Muted { get; private set; }
    public bool Fullscreen { get; private set; }
    public static bool SupportsDisplayMode => Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer;
    public event Action Changed;

    public GameSettings(string preferencePrefix)
    {
        if (string.IsNullOrWhiteSpace(preferencePrefix)) throw new ArgumentException("A preference prefix is required.");
        prefix = preferencePrefix;
        Reload();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        current = new GameSettings(PreferencePrefix);
    }

    public void Reload()
    {
        MasterVolume = ValidVolume(PlayerPrefs.GetFloat(prefix + ".Volume", 1f));
        Muted = PlayerPrefs.GetInt(prefix + ".Muted", 0) == 1;
        Fullscreen = PlayerPrefs.GetInt(prefix + ".Fullscreen", 1) != 0;
        ApplyAudio();
        ApplyDisplay();
        Changed?.Invoke();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = ValidVolume(value);
        Commit(false);
    }

    public void SetMuted(bool value)
    {
        Muted = value;
        Commit(false);
    }

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        Commit(true);
    }

    public void RestoreDefaults()
    {
        MasterVolume = 1f;
        Muted = false;
        Fullscreen = true;
        Commit(true);
    }

    private static float ValidVolume(float value) => float.IsNaN(value) || float.IsInfinity(value)
        ? 1f : Mathf.Clamp01(value);

    private void Commit(bool applyDisplay)
    {
        ApplyAudio();
        if (applyDisplay) ApplyDisplay();
        PlayerPrefs.SetFloat(prefix + ".Volume", MasterVolume);
        PlayerPrefs.SetInt(prefix + ".Muted", Muted ? 1 : 0);
        PlayerPrefs.SetInt(prefix + ".Fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    private void ApplyAudio() => AudioListener.volume = Muted ? 0f : MasterVolume;

    private void ApplyDisplay()
    {
        // The Editor Game view cannot switch the standalone player's display mode.
        if (!SupportsDisplayMode) return;
        FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (Screen.fullScreenMode != mode) Screen.fullScreenMode = mode;
    }
}
