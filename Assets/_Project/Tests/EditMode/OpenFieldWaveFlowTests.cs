using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

// Exercise the real controller's transitions without starting Play mode or spawn coroutines.
public sealed class OpenFieldWaveFlowTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private Component controller;
    private object phaseAssault;
    private object survivors;
    private IList paths;

    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value)
        => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args)
        => target.GetType().GetMethod(name, Flags).Invoke(target, args);

    private GameObject Host(string name)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        owned.Add(obj);
        return obj;
    }

    [SetUp]
    public void SetUp()
    {
        controller = Host("Wave flow test").AddComponent(Runtime("OpenFieldPrototypeController"));
        Type waveType = Runtime("WaveDefinition");
        Array waves = Array.CreateInstance(waveType, 3);
        for (int i = 0; i < 3; i++)
        {
            ScriptableObject wave = ScriptableObject.CreateInstance(waveType);
            owned.Add(wave);
            Set(wave, "enemyCount", i + 1);
            waves.SetValue(wave, i);
        }
        ScriptableObject sequence = ScriptableObject.CreateInstance(Runtime("WaveSequenceDefinition"));
        owned.Add(sequence);
        Set(sequence, "waves", waves);
        Set(controller, "waveSequence", sequence);
        Set(controller, "betweenRoundATP", 40);
        Call(Field(controller, "economy"), "Initialize", 100);
        object grid = Activator.CreateInstance(Runtime("OpenFieldGridModel"), 5, 3,
            new Vector2Int(4, 1), new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, null);
        Set(controller, "grid", grid);
        paths = (IList)Field(controller, "roundPaths");
        for (int i = 0; i < 3; i++) paths.Add(Host("Survivor path " + i).AddComponent(Runtime("WaypointPath")));
        survivors = Field(controller, "activeEnemies");
        phaseAssault = Enum.Parse(Runtime("OpenFieldPhase"), "Assault");
    }

    private void SetPhase(int phase) => Set(controller, "<Phase>k__BackingField",
        Enum.ToObject(Runtime("OpenFieldPhase"), phase));

    private Component Survivor(string name)
    {
        GameObject obj = Host(name);
        Component health = obj.AddComponent(Runtime("Health"));
        Component follower = obj.AddComponent(Runtime("PathFollower"));
        Component enemy = obj.AddComponent(Runtime("EnemyController"));
        Set(enemy, "health", health);
        Set(enemy, "pathFollower", follower);
        return enemy;
    }

    [TestCase(0, 0, false, true)]
    [TestCase(0, 1, false, true)]
    [TestCase(1, 0, false, false)]
    [TestCase(1, 0, true, true)]
    [TestCase(1, 1, true, true)]
    [TestCase(1, 2, true, false)]
    [TestCase(2, 1, true, false)]
    [TestCase(3, 1, true, false)]
    public void WaveStartRequiresPlanningOrFinishedSpawningWithAnotherWave(
        int phase, int round, bool spawned, bool expected)
    {
        SetPhase(phase);
        Set(controller, "roundIndex", round);
        Set(controller, "allEnemiesSpawned", spawned);
        Assert.That(Get(controller, "CanStartWave"), Is.EqualTo(expected));
    }

    [Test]
    public void EarlyStartKeepsSurvivorsAndPathsAndGrantsRoundATPOnce()
    {
        SetPhase(1);
        Set(controller, "allEnemiesSpawned", true);
        Component survivor = Survivor("Previous wave survivor");
        Call(survivors, "Add", survivor);
        object originalPath = paths[0];

        Assert.That(Call(controller, "TryPrepareAssault"), Is.EqualTo(true));
        Assert.That(Get(controller, "CurrentRound"), Is.EqualTo(2));
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(140));
        Assert.That(Get(controller, "Phase"), Is.EqualTo(phaseAssault));
        Assert.That(Field(controller, "allEnemiesSpawned"), Is.EqualTo(false));
        Assert.That(paths.Count, Is.EqualTo(3));
        Assert.That(paths[0], Is.SameAs(originalPath));
        Assert.That(Call(survivors, "Contains", survivor), Is.EqualTo(true));

        // Another entrance click cannot repeat the transition while this wave is spawning.
        Assert.That(Call(controller, "TryPrepareAssault"), Is.EqualTo(false));
        Assert.That(Get(controller, "CurrentRound"), Is.EqualTo(2));
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(140));

        Set(controller, "allEnemiesSpawned", true);
        Assert.That(Call(controller, "TryPrepareAssault"), Is.EqualTo(true));
        Assert.That(Get(controller, "CurrentRound"), Is.EqualTo(3));
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(180));
        Set(controller, "allEnemiesSpawned", true);
        Assert.That(Call(controller, "TryPrepareAssault"), Is.EqualTo(false));
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(180));
    }

    [Test]
    public void LastWaveCannotFinishWhileAnyEarlierSurvivorRemains()
    {
        SetPhase(1);
        Set(controller, "roundIndex", 2);
        Set(controller, "allEnemiesSpawned", true);
        Call(survivors, "Add", Survivor("Earlier survivor"));
        Call(controller, "FinishRoundIfReady");
        Assert.That(Get(controller, "Phase"), Is.EqualTo(phaseAssault));
        Assert.That(paths.Count, Is.EqualTo(3));
    }

    [Test]
    public void EmptyFieldCannotFinishWhileWaveIsStillSpawning()
    {
        SetPhase(1);
        Set(controller, "roundIndex", 2);
        Set(controller, "allEnemiesSpawned", false);
        Call(controller, "FinishRoundIfReady");
        Assert.That(Get(controller, "Phase"), Is.EqualTo(phaseAssault));
        Assert.That(paths.Count, Is.EqualTo(3));
    }

    [Test]
    public void EntranceCountFollowsTheUpcomingWaveAndHidesAtTheEnd()
    {
        SetPhase(0);
        Assert.That(Get(controller, "UpcomingEntranceCount"), Is.EqualTo(1));
        Assert.That(Get(controller, "UpcomingRound"), Is.EqualTo(1));
        SetPhase(1);
        Assert.That(Get(controller, "UpcomingEntranceCount"), Is.EqualTo(2));
        Assert.That(Get(controller, "UpcomingRound"), Is.EqualTo(2));
        Set(controller, "roundIndex", 2);
        Assert.That(Get(controller, "UpcomingEntranceCount"), Is.EqualTo(0));
        SetPhase(2);
        Assert.That(Get(controller, "UpcomingEntranceCount"), Is.EqualTo(0));
    }

    [TestCase(160, 90)]
    [TestCase(210, 90)]
    [TestCase(120, 90)]
    [TestCase(90, 160)]
    public void ViewportFitsMapWithoutDistortingWorldGeometryOrCellInput(int targetWidth, int targetHeight)
    {
        Camera camera = Host("Map viewport camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 0f, -10f);
        RenderTexture target = new RenderTexture(targetWidth, targetHeight, 0);
        owned.Add(target);
        camera.targetTexture = target;
        controller.transform.position = new Vector3(3f, -2f, 0f);
        Set(controller, "gameplayCamera", camera);
        Set(controller, "width", 15);
        Set(controller, "height", 11);
        Set(controller, "cellSize", 1f);
        Call(controller, "ConfigureMapViewport");

        Vector3 center = controller.transform.position;
        Vector3 bottomLeft = camera.WorldToViewportPoint(center + new Vector3(-7.5f, -5.5f));
        Vector3 topRight = camera.WorldToViewportPoint(center + new Vector3(7.5f, 5.5f));
        Assert.That(camera.aspect, Is.EqualTo((float)targetWidth / targetHeight).Within(.0001f));
        Assert.That(camera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        Assert.That(bottomLeft.x, Is.GreaterThanOrEqualTo(-.0001f));
        Assert.That(bottomLeft.y, Is.GreaterThanOrEqualTo(-.0001f));
        Assert.That(topRight.x, Is.LessThanOrEqualTo(1.0001f));
        Assert.That(topRight.y, Is.LessThanOrEqualTo(1.0001f));
        Assert.That(bottomLeft.x + topRight.x, Is.EqualTo(1f).Within(.0001f));
        Assert.That(bottomLeft.y + topRight.y, Is.EqualTo(1f).Within(.0001f));
        Assert.That(Mathf.Max(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y),
            Is.EqualTo(1f).Within(.0001f), "At least one board axis should fit the available viewport.");

        Vector3 origin = camera.WorldToScreenPoint(center);
        float unitX = Vector3.Distance(origin, camera.WorldToScreenPoint(center + Vector3.right));
        float unitY = Vector3.Distance(origin, camera.WorldToScreenPoint(center + Vector3.up));
        Assert.That(unitX / unitY, Is.EqualTo(1f).Within(.0001f),
            "A world-space circle must stay circular in screen pixels.");
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            Vector3 edge = camera.WorldToScreenPoint(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)));
            Assert.That(Vector3.Distance(origin, edge), Is.EqualTo(unitX).Within(.0001f));
        }
        for (int x = 0; x < 15; x++)
            for (int y = 0; y < 11; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 world = (Vector3)Call(controller, "CellToWorld", cell);
                Vector3 viewport = camera.WorldToViewportPoint(world);
                Assert.That(viewport.x, Is.EqualTo(Mathf.Lerp(bottomLeft.x, topRight.x, (x + .5f) / 15f)).Within(.0001f));
                Assert.That(viewport.y, Is.EqualTo(Mathf.Lerp(bottomLeft.y, topRight.y, (y + .5f) / 11f)).Within(.0001f));
                Vector3 roundTrip = camera.ScreenToWorldPoint(camera.WorldToScreenPoint(world));
                Assert.That(Call(controller, "WorldToCell", roundTrip), Is.EqualTo(cell));
            }
        camera.targetTexture = null;
    }

    [Test]
    public void ViewportRecalculatesWhenTheSameCameraChangesAspectRatio()
    {
        Camera camera = Host("Resizable map camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 0f, -10f);
        Set(controller, "gameplayCamera", camera);
        Set(controller, "width", 15);
        Set(controller, "height", 11);
        Set(controller, "cellSize", 1f);
        foreach (Vector2Int size in new[] { new Vector2Int(320, 180), new Vector2Int(180, 320),
            new Vector2Int(480, 180), new Vector2Int(320, 180) })
        {
            RenderTexture target = new RenderTexture(size.x, size.y, 0);
            owned.Add(target);
            camera.targetTexture = target;
            Call(controller, "ConfigureMapViewport");
            float aspect = (float)size.x / size.y;
            Assert.That(camera.aspect, Is.EqualTo(aspect).Within(.0001f));
            Assert.That(camera.orthographicSize, Is.EqualTo(Mathf.Max(5.5f, 7.5f / aspect)).Within(.0001f));
        }
        camera.targetTexture = null;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WaveArtworkUsesDefaultOnlyWhenSceneReferenceIsMissing(bool assigned)
    {
        Sprite sceneSprite = null;
        if (assigned)
        {
            sceneSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            owned.Add(sceneSprite);
        }
        MethodInfo resolve = Runtime("OpenFieldUiResources").GetMethod("ResolveWaveButtonArtwork",
            BindingFlags.Public | BindingFlags.Static);
        Sprite result = (Sprite)resolve.Invoke(null, new object[] { sceneSprite });
        Assert.That(result, Is.Not.Null);
        if (assigned) Assert.That(result, Is.SameAs(sceneSprite));
        else Assert.That(result.name, Is.EqualTo("下一波_圆形细胞按钮"));
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
}
