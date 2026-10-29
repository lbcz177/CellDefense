#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Unity's Game View resizes affect later fixtures; keep these after other UI tests.
public sealed class ZzzLevelScoringPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly string[] Scenes = { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" };
    private readonly Dictionary<string, int?> previous = new Dictionary<string, int?>();
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(Flags)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static Component Owner() => (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    private static object Outcome(string name) => Enum.Parse(Runtime("OpenFieldPhase"), name);
    private static string Key(string scene) => "CellDefense.LevelProgress.v1." + scene + ".BestScore";
    private static int Value(object score, string name) => (int)(score.GetType().GetProperty(name)?.GetValue(score)
        ?? score.GetType().GetField(name).GetValue(score));
    private static void Damage(Component enemy) => enemy.GetType().GetMethod("TakeDamage", new[] { typeof(float) }).Invoke(enemy, new object[] { 1000000f });

    [SetUp]
    public void Setup()
    {
        foreach (string scene in Scenes)
        {
            string key = Key(scene);
            previous[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
            PlayerPrefs.DeleteKey(key);
        }
    }

    private static void Capture(Component owner, string name)
    {
        // Check visible sprite digits even when screenshot export is disabled.
        if (owner != null)
        {
            Canvas.ForceUpdateCanvases();
            bool victory = Get(owner, "Phase").ToString() == "Victory";
            GameObject panel = (GameObject)Field(owner, victory ? "victoryPanel" : "defeatPanel");
            Component summary = panel.GetComponentInChildren(Runtime("LevelResultScoreView"));
            foreach (Graphic label in summary.GetComponentsInChildren<Graphic>().Where(graphic => graphic.GetType().Name == "PixelImageText"))
            {
                Assert.That(Get(label, "MissingGlyphCount"), Is.Zero, label.name);
                Assert.That(Get(label, "RenderedLineCount"), Is.EqualTo(1), label.name);
            }
            string expected = Value(Get(owner, "FinalScore"), "Score").ToString(System.Globalization.CultureInfo.InvariantCulture);
            Image[] digits = summary.GetComponentsInChildren<Image>()
                .Where(image => image.name.StartsWith("Score Digit ", StringComparison.Ordinal)).ToArray();
            Assert.That(digits.Length, Is.EqualTo(expected.Length), "Only the actual score digits should be visible.");
            Object hud = Resources.Load("OpenFieldUiResources");
            for (int i = 0; i < expected.Length; i++)
                Assert.That(digits[i].sprite, Is.EqualTo(Call(hud, "GetDigit", expected[i] - '0')),
                    "Displayed score must match the awarded score, including zero and four-digit results.");
            Graphic stars = summary.GetComponentsInChildren<Graphic>().Single(graphic => graphic.GetType().Name == "StarRatingGraphic");
            Assert.That(stars.mainTexture.name, Is.EqualTo("评分星星图集"));
            Assert.That(Get(stars, "Stars"), Is.EqualTo(Value(Get(owner, "FinalScore"), "Stars")));
            Assert.That(summary.GetComponentsInChildren<Image>().Single(image => image.name == "Score Ribbon").sprite,
                Is.Not.Null, "The result artwork must be included in Resources.");
        }
        string destination = Environment.GetEnvironmentVariable("CELLDEFENSE_SCORE_CAPTURE_DIR");
        if (string.IsNullOrEmpty(destination)) return;
        Camera camera = owner != null ? (Camera)Field(owner, "gameplayCamera") : Camera.main;
        Canvas.ForceUpdateCanvases();
        OpenFieldPlaybackPlayModeTests.CaptureUi(camera, name);
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PauseArtwork", name));
        if (File.Exists(source))
        {
            Directory.CreateDirectory(destination);
            string file = Path.Combine(destination, name);
            Assert.That(File.Exists(file), Is.False, "Use a fresh verification directory to preserve previous captures.");
            File.Copy(source, file);
        }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator RealFiveLevelWaveFlowAwardsPerfectScoreAndNextLevelKeepsTheRecord()
    {
        yield return SceneManager.LoadSceneAsync(Scenes[0]);
        foreach (string scene in Scenes)
        {
            Component owner = Owner();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
            float deadline = Time.realtimeSinceStartup + 30f;
            // Accelerate only the test clock. Enemies still use the real spawn and death callbacks.
            while ((bool)Get(owner, "HasActiveRun") && Time.realtimeSinceStartup < deadline)
            {
                if (Get(owner, "Phase").ToString() == "Planning") Call(owner, "StartAssault");
                Time.timeScale = 10f;
                foreach (Component enemy in ((IEnumerable)Field(owner, "activeEnemies")).Cast<Component>().ToArray()) Damage(enemy);
                yield return null;
            }
            Assert.That(Get(owner, "Phase").ToString(), Is.EqualTo("Victory"), scene);
            object score = Get(owner, "FinalScore");
            Assert.That(Value(score, "Score"), Is.EqualTo(10000), scene);
            Assert.That(Value(score, "Stars"), Is.EqualTo(3));
            Assert.That(PlayerPrefs.GetInt(Key(scene)), Is.EqualTo(10000));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Call(owner, "FinishRun", Outcome("Defeat"));
            Assert.That(Get(owner, "Phase").ToString(), Is.EqualTo("Victory"), "A terminal result must be recorded once.");
            Capture(owner, "score-perfect-" + scene + ".png");
            if (scene != Scenes.Last()) { Call(owner, "LoadNextLevel"); yield return null; }
            else Assert.That(((GameObject)Field(owner, "victoryPanel")).GetComponentsInChildren<Button>().Any(b => b.name == "Next"), Is.False);
        }
        Call(Owner(), "ReturnToMainMenu");
        yield return null;
        Component menu = (Component)Object.FindObjectOfType(Runtime("MainMenuController"));
        Call(menu, "OpenLevelSelection");
        yield return null;
        foreach (object star in (IEnumerable)Field(menu, "levelStars")) Assert.That(Get(star, "Stars"), Is.EqualTo(3));
        foreach (object label in (IEnumerable)Field(menu, "levelRecords")) Assert.That(Get(label, "text"), Is.EqualTo("最高 10000"));
        Assert.That(((Button[])Field(menu, "levelButtons")).All(b => b.interactable), Is.True);
        Capture(null, "score-five-level-selection.png");
    }

    [UnityTest]
    public IEnumerator SummonsLeaksAndDuplicateCallbacksCannotIncreaseScheduledKillScore()
    {
        yield return SceneManager.LoadSceneAsync(Scenes[0]);
        Component owner = Owner();
        Call(owner, "StartAssault");
        Component parent = ((IEnumerable)Field(owner, "activeEnemies")).Cast<Component>().First();
        Call(owner, "Pause");
        object definition = Get(parent, "Definition");
        object follower = parent.GetComponent(Runtime("PathFollower"));
        object path = Get(follower, "CurrentPath");
        object request = Activator.CreateInstance(Runtime("EnemySpawnRequest"), parent, definition, 1, false);
        Component summon = (Component)Call(owner, "SpawnEnemy", definition, path, request);
        Damage(summon);
        Assert.That(Field(owner, "killedScheduledEnemies"), Is.Zero);
        Component leaked = (Component)Call(owner, "SpawnEnemy", definition, path, null);
        Call(owner, "HandleEnemyExited", leaked, Enum.Parse(Runtime("EnemyExitReason"), "Leaked"));
        Assert.That(Field(owner, "killedScheduledEnemies"), Is.Zero);
        Damage(parent);
        Assert.That(Field(owner, "killedScheduledEnemies"), Is.EqualTo(1));
        Call(owner, "HandleEnemyExited", parent, Enum.Parse(Runtime("EnemyExitReason"), "Killed"));
        Assert.That(Field(owner, "killedScheduledEnemies"), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator LowerVictoryAndDefeatPreserveBestAndRetryClearsCurrentRun()
    {
        PlayerPrefs.SetInt(Key(Scenes[0]), 9300);
        yield return SceneManager.LoadSceneAsync(Scenes[0]);
        Component owner = Owner();
        owner.GetType().GetField("killedScheduledEnemies", Flags).SetValue(owner, (int)Field(owner, "scheduledEnemyCount"));
        Call(Field(owner, "life"), "TryLoseLife", 4);
        Call(owner, "SetPlaybackSpeed", 2);
        Call(owner, "Pause");
        Call(owner, "FinishRun", Outcome("Victory"));
        object score = Get(owner, "FinalScore");
        Assert.That(Value(score, "Score"), Is.EqualTo(7600));
        Assert.That(Value(score, "Stars"), Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetInt(Key(Scenes[0])), Is.EqualTo(9300));
        Capture(owner, "score-two-stars.png");
        Call(owner, "RestartPrototype");
        yield return null;
        owner = Owner();
        Assert.That(Field(owner, "killedScheduledEnemies"), Is.Zero);
        Assert.That(Value(Get(owner, "FinalScore"), "Score"), Is.Zero);
        Call(owner, "FinishRun", Outcome("Defeat"));
        Assert.That(Value(Get(owner, "FinalScore"), "Stars"), Is.Zero);
        Assert.That(PlayerPrefs.GetInt(Key(Scenes[0])), Is.EqualTo(9300));
        Capture(owner, "score-defeat-retains-best.png");
        yield return SceneManager.LoadSceneAsync(Scenes[1]);
        Call(Owner(), "FinishRun", Outcome("Defeat"));
        Assert.That(PlayerPrefs.HasKey(Key(Scenes[1])), Is.False, "A failed first attempt cannot become a clear.");
        Capture(Owner(), "score-first-defeat.png");
    }

    [UnityTest]
    public IEnumerator ScoreLabelsAndButtonsFitDesktopAndMobileAtSmallAndWideSizes()
    {
        try
        {
            foreach (Vector2Int size in new[] { new Vector2Int(640,480), new Vector2Int(1280,720), new Vector2Int(2048,928) })
            using (new ZzMobileLandscapePlayModeTests.Resolution(size.x, size.y))
            {
                yield return null;
                yield return null;
                Assert.That(Screen.width, Is.EqualTo(size.x));
                Assert.That(Screen.height, Is.EqualTo(size.y));
                foreach (bool mobile in new[] { false, true })
                foreach (string outcome in new[] { "Victory", "Defeat" })
                {
                    MobilePlayModeTests.Preview(mobile);
                    yield return SceneManager.LoadSceneAsync(Scenes[0]);
                    Component owner = Owner();
                    owner.GetType().GetField("killedScheduledEnemies", Flags).SetValue(owner, (int)Field(owner, "scheduledEnemyCount"));
                    Call(owner, "FinishRun", Outcome(outcome));
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    GameObject panel = (GameObject)Field(owner, outcome == "Victory" ? "victoryPanel" : "defeatPanel");
                    Component summary = panel.GetComponentInChildren(Runtime("LevelResultScoreView"));
                    foreach (Graphic graphic in summary.GetComponentsInChildren<Graphic>())
                    {
                        MobilePlayModeTests.Inside(graphic.rectTransform);
                        if (graphic.GetType().Name == "PixelImageText")
                        {
                            Assert.That(Get(graphic, "MissingGlyphCount"), Is.Zero, graphic.name);
                            Assert.That(Get(graphic, "RenderedLineCount"), Is.EqualTo(1), graphic.name);
                        }
                        foreach (Button button in panel.GetComponentsInChildren<Button>())
                            Assert.That(Bounds(graphic.rectTransform).Overlaps(Bounds(button.GetComponent<RectTransform>())), Is.False,
                                graphic.name + " overlaps " + button.name);
                    }
                    foreach (Button button in panel.GetComponentsInChildren<Button>())
                    {
                        foreach (Button other in panel.GetComponentsInChildren<Button>())
                            if (other != button) Assert.That(Bounds(button.GetComponent<RectTransform>()).Overlaps(
                                Bounds(other.GetComponent<RectTransform>())), Is.False, "Button hit areas must remain separate.");
                        var hits = new List<RaycastResult>();
                        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Bounds(button.GetComponent<RectTransform>()).center }, hits);
                        Assert.That(hits.First().gameObject.GetComponentInParent<Button>(), Is.EqualTo(button));
                    }
                    Capture(owner, $"score-{mobile}-{outcome}-{size.x}x{size.y}.png");
                }
            }
        }
        finally { MobilePlayModeTests.Preview(false); }
    }

    private static Rect Bounds(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        MobilePlayModeTests.Preview(false);
        foreach (var entry in previous)
            if (entry.Value.HasValue) PlayerPrefs.SetInt(entry.Key, entry.Value.Value);
            else PlayerPrefs.DeleteKey(entry.Key);
        PlayerPrefs.Save();
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
