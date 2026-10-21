using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class OpenFieldPlaybackTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private Component owner;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private object Get(string name) => owner.GetType().GetProperty(name, Flags).GetValue(owner);
    private object Call(string name, params object[] args) => owner.GetType().GetMethod(name, Flags).Invoke(owner, args);
    private void Phase(string name) => owner.GetType().GetField("<Phase>k__BackingField", Flags)
        .SetValue(owner, Enum.Parse(Runtime("OpenFieldPhase"), name));

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("Playback rules");
        host.SetActive(false);
        owner = host.AddComponent(Runtime("OpenFieldPrototypeController"));
        Phase("Assault");
        Time.timeScale = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        Time.timeScale = 1f;
    }

    [TestCase("Planning")]
    [TestCase("Assault")]
    public void PauseBlocksGameplayAndResumeRestoresSpeedWithoutChangingRoundPhase(string phase)
    {
        Phase(phase);
        Assert.That(Call("SetPlaybackSpeed", 2), Is.True);
        Assert.That(Call("Pause"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Get("HasActiveRun"), Is.True);
        Assert.That(Get("CanBuild"), Is.False);
        Assert.That(Get("CanStartWave"), Is.False);
        Assert.That(Get("Phase").ToString(), Is.EqualTo(phase));
        Assert.That(Call("Pause"), Is.False);
        Assert.That(Call("SetPlaybackSpeed", 1), Is.False);
        Assert.That(Call("Resume"), Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(2f));
        Assert.That(Get("CanBuild"), Is.True);
        Assert.That(Get("Phase").ToString(), Is.EqualTo(phase));
        Assert.That(Call("Resume"), Is.False);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(3)]
    public void UnsupportedSpeedIsRejectedWithoutChangingClock(int speed)
    {
        Assert.That(Call("SetPlaybackSpeed", speed), Is.False);
        Assert.That(Get("PlaybackSpeed"), Is.EqualTo(1));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [TestCase("Victory")]
    [TestCase("Defeat")]
    public void ResultsRejectPauseResumeAndSpeedChanges(string phase)
    {
        Phase(phase);
        Assert.That(Call("Pause"), Is.False);
        Assert.That(Call("Resume"), Is.False);
        Assert.That(Call("TogglePlaybackSpeed"), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void EscapeClosesSelectionBeforePausingAndResumesBeforeOpeningAnything()
    {
        owner.GetType().GetField("selectedBuildCell", Flags).SetValue(owner, (Vector2Int?)new Vector2Int(2, 2));
        Call("HandleEscape");
        Assert.That(Get("IsPaused"), Is.False);
        Assert.That(owner.GetType().GetField("selectedBuildCell", Flags).GetValue(owner), Is.Null);
        Call("HandleEscape");
        Assert.That(Get("IsPaused"), Is.True);
        Call("HandleEscape");
        Assert.That(Get("IsPaused"), Is.False);
    }
}
