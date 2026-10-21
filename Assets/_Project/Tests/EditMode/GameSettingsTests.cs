using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class GameSettingsTests
{
    private static Type SettingsType => Type.GetType("GameSettings, Assembly-CSharp", true);
    private string prefix;
    private object settings;
    private float previousVolume;
    private object Get(string name) => SettingsType.GetProperty(name).GetValue(settings);
    private void Call(string name, params object[] args) => SettingsType.GetMethod(name).Invoke(settings, args);

    [SetUp]
    public void Setup()
    {
        previousVolume = AudioListener.volume;
        prefix = "CellDefense.Tests.Settings." + Guid.NewGuid().ToString("N");
        settings = Activator.CreateInstance(SettingsType, prefix);
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (string key in new[] { ".Volume", ".Muted", ".Fullscreen" }) PlayerPrefs.DeleteKey(prefix + key);
        PlayerPrefs.Save();
        AudioListener.volume = previousVolume;
    }

    [Test]
    public void PreferencesSurviveRecreationMuteKeepsVolumeAndDefaultsPersist()
    {
        Call("SetMasterVolume", .37f);
        Call("SetMuted", true);
        Call("SetFullscreen", false);
        settings = Activator.CreateInstance(SettingsType, prefix);
        Assert.That(Get("MasterVolume"), Is.EqualTo(.37f).Within(.0001f));
        Assert.That(Get("Muted"), Is.True);
        Assert.That(Get("Fullscreen"), Is.False);
        Assert.That(AudioListener.volume, Is.Zero);
        Call("SetMuted", false);
        Assert.That(AudioListener.volume, Is.EqualTo(.37f).Within(.0001f));
        Call("RestoreDefaults");
        settings = Activator.CreateInstance(SettingsType, prefix);
        Assert.That(Get("MasterVolume"), Is.EqualTo(1f));
        Assert.That(Get("Muted"), Is.False);
        Assert.That(Get("Fullscreen"), Is.True);
        Assert.That(AudioListener.volume, Is.EqualTo(1f));
    }

    [TestCase(-.5f, 0f)]
    [TestCase(1.5f, 1f)]
    [TestCase(float.NaN, 1f)]
    [TestCase(float.PositiveInfinity, 1f)]
    public void InvalidVolumeCannotReachTheAudioListener(float input, float expected)
    {
        Call("SetMasterVolume", input);
        Assert.That(Get("MasterVolume"), Is.EqualTo(expected));
        Assert.That(AudioListener.volume, Is.EqualTo(expected));
    }

    [Test]
    public void LoadingInvalidStoredVolumeFallsBackToAValidValue()
    {
        PlayerPrefs.SetFloat(prefix + ".Volume", float.NaN);
        Call("Reload");
        Assert.That(Get("MasterVolume"), Is.EqualTo(1f));
        Assert.That(AudioListener.volume, Is.EqualTo(1f));
    }
}
