using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class MobileInputTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private object gesture;
    private bool Touch(int count, int id, TouchPhase phase, Vector2 point, bool blocked = false) =>
        (bool)Runtime("BoardTapGesture").GetMethod("Process").Invoke(gesture,
            new object[] { count, id, phase, point, blocked, 20f });

    [SetUp] public void Setup() => gesture = Activator.CreateInstance(Runtime("BoardTapGesture"));

    [Test]
    public void SingleFingerOnlyCommitsOnReleaseAndNeverTwice()
    {
        Assert.That(Touch(1, 7, TouchPhase.Began, Vector2.zero), Is.False);
        Assert.That(Touch(1, 7, TouchPhase.Stationary, Vector2.one), Is.False);
        Assert.That(Touch(1, 7, TouchPhase.Ended, Vector2.one), Is.True);
        Assert.That(Touch(1, 7, TouchPhase.Ended, Vector2.one), Is.False);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void BeginningOrEndingOverUiCancels(bool beginBlocked, bool endBlocked)
    {
        Touch(1, 7, TouchPhase.Began, Vector2.zero, beginBlocked);
        Assert.That(Touch(1, 7, TouchPhase.Ended, Vector2.zero, endBlocked), Is.False);
    }

    [Test]
    public void DraggingOutAndBackStillCancels()
    {
        Touch(1, 7, TouchPhase.Began, Vector2.zero);
        Touch(1, 7, TouchPhase.Moved, new Vector2(21, 0));
        Assert.That(Touch(1, 7, TouchPhase.Ended, Vector2.zero), Is.False);
    }

    [Test]
    public void MultipleFingersSuppressRemainderUntilAllAreReleased()
    {
        Touch(1, 7, TouchPhase.Began, Vector2.zero);
        Touch(2, 7, TouchPhase.Stationary, Vector2.zero);
        Assert.That(Touch(1, 7, TouchPhase.Ended, Vector2.zero), Is.False);
        Touch(0, 0, TouchPhase.Canceled, Vector2.zero);
        Touch(1, 9, TouchPhase.Began, Vector2.zero);
        Assert.That(Touch(1, 9, TouchPhase.Ended, Vector2.zero), Is.True);
    }

    [TestCase(TouchPhase.Canceled)]
    [TestCase(TouchPhase.Ended)]
    public void UnmatchedFingerCannotCommit(TouchPhase phase)
    {
        Touch(1, 7, TouchPhase.Began, Vector2.zero);
        Assert.That(Touch(1, 8, phase, Vector2.zero), Is.False);
        Touch(1, 7, TouchPhase.Canceled, Vector2.zero);
        Assert.That(Touch(1, 7, TouchPhase.Ended, Vector2.zero), Is.False);
    }

    [TestCase("Planning")]
    [TestCase("Assault")]
    public void BackgroundCallbacksPauseAndNeverAutoResume(string phase)
    {
        Type mobile = Runtime("MobileDisplay");
        mobile.GetField("PreviewEnabled").SetValue(null, true);
        GameObject host = new GameObject("Mobile lifecycle");
        host.SetActive(false);
        Component owner = host.AddComponent(Runtime("OpenFieldPrototypeController"));
        owner.GetType().GetField("<Phase>k__BackingField", Flags).SetValue(owner,
            Enum.Parse(Runtime("OpenFieldPhase"), phase));
        try
        {
            owner.GetType().GetMethod("SetPlaybackSpeed").Invoke(owner, new object[] { 2 });
            owner.GetType().GetMethod("OnApplicationPause", Flags).Invoke(owner, new object[] { true });
            Assert.That(Time.timeScale, Is.Zero);
            owner.GetType().GetMethod("OnApplicationPause", Flags).Invoke(owner, new object[] { false });
            owner.GetType().GetMethod("OnApplicationFocus", Flags).Invoke(owner, new object[] { true });
            Assert.That(Time.timeScale, Is.Zero);
            owner.GetType().GetMethod("Resume").Invoke(owner, null);
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            owner.GetType().GetMethod("OnApplicationFocus", Flags).Invoke(owner, new object[] { false });
            Assert.That(Time.timeScale, Is.Zero);
        }
        finally
        {
            mobile.GetField("PreviewEnabled").SetValue(null, false);
            Object.DestroyImmediate(host);
            Time.timeScale = 1f;
        }
    }
}
