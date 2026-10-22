using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PromptArtworkPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Component Find(string name) => (Component)Object.FindObjectOfType(Runtime(name));
    private static object Field(object value, string name) => value.GetType().GetField(name, Flags).GetValue(value);
    private static object Get(object value, string name) => value.GetType().GetProperty(name, Flags).GetValue(value);
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(Flags)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(value, args);
    private static void Capture(Component owner, string name)
    {
        Canvas.ForceUpdateCanvases();
        OpenFieldPlaybackPlayModeTests.CaptureUi((Camera)Field(owner, "gameplayCamera"), name);
    }
    private static void AssertImageLabels()
    {
        Canvas.ForceUpdateCanvases();
        Type tmp = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
        Assert.That(Object.FindObjectsOfType(tmp).Cast<Behaviour>().Count(t => t.isActiveAndEnabled), Is.Zero,
            "All current gameplay/menu hints should display PNG artwork without a visible original font layer.");
        foreach (Component label in Object.FindObjectsOfType(Runtime("PixelImageText")))
            Assert.That((int)Get(label, "MissingGlyphCount"), Is.Zero, label.name);
    }
    private static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    [UnityTest]
    public IEnumerator TutorialTradingEnemyIntroductionsAndLevelSelectionUseImageArtwork()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Find("OpenFieldPrototypeController"), tutorial = Find("FirstLevelTutorial");
        yield return null;
        AssertImageLabels();
        Assert.That((int)Get(Field(tutorial, "description"), "RenderedGlyphCount"), Is.GreaterThan(0));
        Capture(owner, "prompt-tutorial.png");
        Vector2Int cell = (Vector2Int)Get(tutorial, "SuggestedCell");
        Call(owner, "SelectBuildCell", cell);
        yield return null;
        Capture(owner, "prompt-build-menu.png");
        Component shop = (Component)Field(owner, "buildMenu");
        shop.GetComponentsInChildren<Button>(true).Single(b => b.name == "Tower Choice 1").onClick.Invoke();
        Call(owner, "SelectPlacedTower", cell);
        yield return null;
        Capture(owner, "prompt-tower-info.png");
        Call(Field(owner, "towerMenu"), "RequestSell");
        yield return null;
        AssertImageLabels();
        Capture(owner, "prompt-sell-confirmation.png");
        Call(Field(owner, "towerMenu"), "CancelSell");
        Call(owner, "ClearSelection");
        Call(tutorial, "Skip");
#if UNITY_EDITOR
        Component enemyHud = (Component)Field(owner, "enemyHud");
        foreach (string name in new[] { "Staphylococcus", "Flagellate", "ArmoredBacillus", "FungalSpore", "VirusClone", "FungalBoss", "MembraneBoss", "VirusBoss" })
        {
            Object definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Enemies/" + name + "Definition.asset");
            Call(enemyHud, "ClearHints");
            Call(enemyHud, "Announce", definition);
            // Only the preview visibility clock is supplied; no enemies, ATP, health or combat rules are changed.
            enemyHud.GetType().GetField("allowEmptyHintUntil", Flags).SetValue(enemyHud, Time.time + 4f);
            Call(enemyHud, "LateUpdate");
            Canvas.ForceUpdateCanvases();
            Component hint = (Component)Field(enemyHud, "hint");
            RectTransform panel = (RectTransform)hint.transform.parent;
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(((Image)Field(enemyHud, "portrait")).sprite, Is.Not.Null);
            Assert.That((int)Get(hint, "RenderedGlyphCount"), Is.GreaterThan(0));
            AssertImageLabels();
            foreach (Button toolbar in ((Component)Field(owner, "playbackControls")).GetComponentsInChildren<Button>(true)
                         .Where(b => b.name == "Pause" || b.name == "Playback Speed"))
                Assert.That(Bounds(panel).Overlaps(Bounds(toolbar.GetComponent<RectTransform>())), Is.False,
                    "The enemy introduction should leave pause and speed controls readable.");
            Capture(owner, "prompt-enemy-" + name + ".png");
        }
#endif
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Call(Find("MainMenuController"), "OpenLevelSelection");
        yield return null;
        AssertImageLabels();
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, "prompt-level-selection.png");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
