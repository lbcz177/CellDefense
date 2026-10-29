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

// Game View resizing fixtures run after the other UI checks.
public sealed class ZzzzNumberedLevelSelectionPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly Dictionary<string, int?> previous = new Dictionary<string, int?>();
    private static readonly string[] Scenes = { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" };
    private static Component Menu() => (Component)Object.FindObjectOfType(Type.GetType("MainMenuController, Assembly-CSharp", true));
    private static object Field(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
    private static object Get(object o, string n) => o.GetType().GetProperty(n, Flags).GetValue(o);
    private static object Call(object o, string n, params object[] args) => o.GetType().GetMethods(Flags).Single(m => m.Name == n && m.GetParameters().Length == args.Length).Invoke(o, args);
    private static string Key(string scene) => "CellDefense.LevelProgress.v1." + scene + ".BestScore";

    [SetUp]
    public void PreserveRecords()
    {
        foreach (string scene in Scenes)
        {
            string key = Key(scene);
            previous[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
            PlayerPrefs.DeleteKey(key);
        }
    }

    [UnityTest]
    public IEnumerator NumbersOpenDetailsAndOnlyExplicitStartLoadsTheSelectedMap()
    {
        PlayerPrefs.SetInt(Key(Scenes[0]), 9600);
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu = Menu();
        Call(menu, "OpenLevelSelection");
        Call(menu, "OpenChapter", 0);
        yield return null;
        Button[] nodes = (Button[])Field(menu, "levelButtons");
        Assert.That(nodes.Length, Is.EqualTo(5));
        for (int i = 0; i < nodes.Length; i++)
        {
            Graphic number = nodes[i].GetComponentsInChildren<Graphic>().Single(g => g.GetType().Name == "PixelImageText" && g.name == "Label");
            Assert.That(Get(number, "text"), Is.EqualTo((i + 1).ToString()));
            Assert.That(nodes[i].transform.Find("Map Preview"), Is.Null);
        }
        nodes[0].onClick.Invoke();
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(Get(menu, "IsLevelDetailsOpen"), Is.True);
        Assert.That(Get(Field(menu, "detailsScore"), "text"), Is.EqualTo("9600"));
        Assert.That(Get(Field(menu, "detailsStars"), "Stars"), Is.EqualTo(3));
        Assert.That(((Image)Field(menu, "detailsMap")).sprite, Is.EqualTo(((Sprite[])Field(menu, "levelArtwork"))[0]));
        Assert.That(((CanvasGroup)Field(menu, "listInteraction")).interactable, Is.False);
        Call(menu, "HandleEscape");
        Assert.That(Get(menu, "IsLevelDetailsOpen"), Is.False);
        Assert.That(((GameObject)Field(menu, "selectionCanvas")).activeSelf, Is.True);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(nodes[0].gameObject));
        PlayerPrefs.SetInt(Key(Scenes[0]), 9800);
        nodes[0].onClick.Invoke();
        Assert.That(Get(Field(menu, "detailsScore"), "text"), Is.EqualTo("9800"), "Read the latest saved record on every open.");
        Call(menu, "CloseLevelDetails");
        nodes[1].onClick.Invoke();
        Assert.That(((Graphic)Field(menu, "detailsScore")).gameObject.activeSelf, Is.False);
        Assert.That(((Image)Field(menu, "detailsUnplayed")).gameObject.activeSelf, Is.True);
        ((Button)Field(menu, "detailsStart")).onClick.Invoke();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(Scenes[1]));
    }

    [UnityTest]
    public IEnumerator DetailsStayInTheSafeAreaAndPixelLabelsRenderAcrossWindowSizes()
    {
        PlayerPrefs.SetInt(Key(Scenes[0]), 9600);
        foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(640, 480), new Vector2Int(2048, 928) })
        using (new ZzMobileLandscapePlayModeTests.Resolution(size.x, size.y))
        {
            yield return null;
            MobilePlayModeTests.Preview(size.x == 2048);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Component menu = Menu();
            Call(menu, "OpenLevelSelection");
        Call(menu, "OpenChapter", 0);
            yield return null;
            Capture("numbers-" + size.x + "x" + size.y + ".png");
            Call(menu, "OpenLevelDetails", 0);
            yield return null;
            Canvas.ForceUpdateCanvases();
            RectTransform panel = (RectTransform)Field(menu, "detailsPanel");
            Vector3[] corners = new Vector3[4]; panel.GetWorldCorners(corners);
            Rect safe = MobilePlayModeTests.Safe;
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                Assert.That(point.x, Is.InRange(safe.xMin - .5f, safe.xMax + .5f));
                Assert.That(point.y, Is.InRange(safe.yMin - .5f, safe.yMax + .5f));
            }
            foreach (Graphic g in ((GameObject)Field(menu, "selectionCanvas")).GetComponentsInChildren<Graphic>().Where(g => g.GetType().Name == "PixelImageText"))
                Assert.That(Get(g, "MissingGlyphCount"), Is.EqualTo(0), g.name);
            Capture("details-" + size.x + "x" + size.y + ".png");
            Call(menu, "CloseLevelSelection");
            Assert.That(Get(menu, "IsLevelDetailsOpen"), Is.False);
        }
    }

    private static void Capture(string name)
    {
        string destination = Environment.GetEnvironmentVariable("CELLDEFENSE_NUMBER_SELECTION_CAPTURE_DIR");
        if (string.IsNullOrEmpty(destination)) return;
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, name);
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PauseArtwork", name));
        Directory.CreateDirectory(destination);
        File.Copy(source, Path.Combine(destination, name), false);
    }

    [UnityTearDown]
    public IEnumerator RestoreRecords()
    {
        foreach (var entry in previous)
            if (entry.Value.HasValue) PlayerPrefs.SetInt(entry.Key, entry.Value.Value); else PlayerPrefs.DeleteKey(entry.Key);
        PlayerPrefs.Save();
        MobilePlayModeTests.Preview(false);
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
