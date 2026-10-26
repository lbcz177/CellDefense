using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class LevelScoringTests
{
    private static Type Rules => Type.GetType("LevelScoring, Assembly-CSharp", true);
    private static Type Progress => Type.GetType("LevelProgress, Assembly-CSharp", true);
    private string prefix;
    private object progress;
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private static int Read(object result, string name) => (int)(result.GetType().GetProperty(name)?.GetValue(result)
        ?? result.GetType().GetField(name).GetValue(result));
    private static object Score(bool victory, int kills, int total, int life, int initial) =>
        Rules.GetMethod("Evaluate").Invoke(null, new object[] { victory, kills, total, life, initial });

    [SetUp]
    public void Setup()
    {
        prefix = "CellDefense.Tests.Score." + Guid.NewGuid().ToString("N");
        progress = Activator.CreateInstance(Progress, prefix);
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
            PlayerPrefs.DeleteKey(prefix + "." + scene + ".BestScore");
        PlayerPrefs.Save();
    }

    [TestCase(0, 1)]
    [TestCase(6999, 1)]
    [TestCase(7000, 2)]
    [TestCase(8999, 2)]
    [TestCase(9000, 3)]
    [TestCase(10000, 3)]
    public void VictoryUsesExactScoreThresholds(int score, int stars)
    {
        Assert.That(Rules.GetMethod("StarsForScore").Invoke(null, new object[] { true, score }), Is.EqualTo(stars));
        Assert.That(Rules.GetMethod("StarsForScore").Invoke(null, new object[] { false, score }), Is.Zero);
    }

    [Test]
    public void ScoreNormalizesDifferentWaveSizesAndCoreCapacities()
    {
        foreach (int total in new[] { 22, 150, 151 })
        foreach (int life in new[] { 10, 20 })
        {
            object score = Score(true, total, total, life, life);
            Assert.That(Read(score, "Score"), Is.EqualTo(10000));
            Assert.That(Read(score, "Stars"), Is.EqualTo(3));
        }
        object partial = Score(true, 75, 100, 14, 20);
        Assert.That(Read(partial, "KillScore"), Is.EqualTo(3000));
        Assert.That(Read(partial, "CoreScore"), Is.EqualTo(4200));
        Assert.That(Read(partial, "Stars"), Is.EqualTo(2));
        Assert.That(Read(Score(true, 2, 3, 10, 10), "Score"), Is.EqualTo(8666), "Round down below a threshold.");
    }

    [Test]
    public void InvalidInputsCannotProduceExtraPointsOrDivideByZeroAndDefeatHasNoCoreBonus()
    {
        Assert.That(Read(Score(true, int.MaxValue, 100, int.MaxValue, 10), "Score"), Is.EqualTo(10000));
        Assert.That(Read(Score(true, -1, 100, -1, 10), "Score"), Is.Zero);
        Assert.That(Read(Score(true, 1, 0, 1, 0), "Score"), Is.Zero);
        object defeat = Score(false, 100, 100, 20, 20);
        Assert.That(Read(defeat, "Score"), Is.EqualTo(4000));
        Assert.That(Read(defeat, "Stars"), Is.Zero);
    }

    [Test]
    public void RecordsPersistPerLevelAndLowerRetriesNeverOverwriteTheBest()
    {
        Assert.That(Call(progress, "BestStars", "OpenFieldPrototype"), Is.Zero);
        Assert.That(Call(progress, "SaveVictory", "OpenFieldPrototype", 7500), Is.True);
        progress = Activator.CreateInstance(Progress, prefix);
        Assert.That(Call(progress, "BestScore", "OpenFieldPrototype"), Is.EqualTo(7500));
        Assert.That(Call(progress, "BestStars", "OpenFieldPrototype"), Is.EqualTo(2));
        Assert.That(Call(progress, "SaveVictory", "OpenFieldPrototype", 6500), Is.False);
        Assert.That(Call(progress, "SaveVictory", "OpenFieldPrototype", 7500), Is.False);
        Assert.That(Call(progress, "SaveVictory", "OpenFieldPrototype", 9000), Is.True);
        Assert.That(Call(progress, "BestStars", "OpenFieldPrototype"), Is.EqualTo(3));
        Assert.That(Call(progress, "HasCleared", "OpenFieldLevel02"), Is.False);
        Assert.That(Call(progress, "SaveVictory", "OpenFieldLevel02", 5000), Is.True);
        Assert.That(Call(progress, "BestScore", "OpenFieldPrototype"), Is.EqualTo(9000));
        Assert.That(Call(progress, "BestStars", "OpenFieldLevel02"), Is.EqualTo(1));
        Assert.That(Call(progress, "SaveVictory", "MainMenu", 10000), Is.False);
        Assert.That(Call(progress, "HasCleared", "MainMenu"), Is.False);
    }
}
