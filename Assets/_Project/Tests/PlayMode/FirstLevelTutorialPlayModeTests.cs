using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class FirstLevelTutorialPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Component Find(string name) => Object.FindObjectOfType(Runtime(name)) as Component;
    private static object Field(object value, string name) => value.GetType().GetField(name, Flags).GetValue(value);
    private static object Get(object value, string name) => value.GetType().GetProperty(name, Flags).GetValue(value);
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(Flags)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(value, args);
    private static string Step(Component guide) => Get(guide, "Step").ToString();
    private static Button Button(Component guide, string name) => guide.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
    private static void BuildSuggested(Component owner, Component guide, int index)
    {
        Vector2Int cell = (Vector2Int)Get(guide, "SuggestedCell");
        Assert.That(Call(owner, "SelectBuildCell", cell), Is.True);
        Component menu = (Component)Field(owner, "buildMenu");
        menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "Tower Choice " + (index + 1)).onClick.Invoke();
        Debug.Log($"TUTORIAL_BUILD type={index} cell={cell} atp={Get(owner, "CurrentATP")}");
    }
    private static void Capture(Component owner, string name)
    {
        Canvas.ForceUpdateCanvases();
        OpenFieldPlaybackPlayModeTests.CaptureUi((Camera)Field(owner, "gameplayCamera"), name);
    }

    [UnityTest]
    public IEnumerator RealBuildsAdvanceGuideAndSkipNeverChangesEconomyOrWaveRules()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Find("OpenFieldPrototypeController"), guide = Find("FirstLevelTutorial");
        Assert.That(guide, Is.Not.Null);
        Assert.That(Step(guide), Is.EqualTo("SelectCell"));
        Assert.That(Call(owner, "SelectBuildCell", new Vector2Int(-1, -1)), Is.False);
        Assert.That(Step(guide), Is.EqualTo("SelectCell"));
        Capture(owner, "tutorial-select-cell.png");
        BuildSuggested(owner, guide, 0);
        Assert.That(Step(guide), Is.EqualTo("SelectCell"));
        Vector2Int secondCell = (Vector2Int)Get(guide, "SuggestedCell");
        Assert.That(Call(owner, "SelectBuildCell", secondCell), Is.True);
        Assert.That(Step(guide), Is.EqualTo("BuildGuards"));
        Capture(owner, "tutorial-build-choice.png");
        BuildSuggested(owner, guide, 0);
        Assert.That(Step(guide), Is.EqualTo("StartWave"));
        int atp = (int)Get(owner, "CurrentATP");
        Call(owner, "ClearSelection");
        yield return null;
        Canvas.ForceUpdateCanvases();
        RectTransform panel = (RectTransform)Field(guide, "panel");
        Vector2 position = panel.TransformPoint(panel.rect.center);
        Assert.That(Call(guide, "ContainsPointer", position), Is.True);
        Assert.That(Call(owner, "ProcessBoardTouch", 1, 0, TouchPhase.Began, position), Is.False);
        Assert.That(Call(owner, "ProcessBoardTouch", 1, 0, TouchPhase.Ended, position), Is.False);
        Assert.That(Get(owner, "SelectedBuildCell"), Is.Null);
        Call(owner, "Pause");
        yield return null;
        Assert.That(Get(guide, "IsVisible"), Is.False);
        Call(owner, "Resume");
        yield return null;
        Assert.That(Get(guide, "IsVisible"), Is.True);
        Button(guide, "Skip Tutorial").onClick.Invoke();
        Assert.That(Step(guide), Is.EqualTo("Skipped"));
        Assert.That(Get(guide, "IsVisible"), Is.False);
        Assert.That(Get(owner, "CurrentATP"), Is.EqualTo(atp));
        Assert.That(Get(owner, "CurrentRound"), Is.EqualTo(1));
        Assert.That(Get(owner, "CanStartWave"), Is.True);
        Assert.That(Call(owner, "ProcessBoardTouch", 1, 0, TouchPhase.Began, position), Is.False);
        Call(owner, "RestartPrototype");
        yield return null;
        Assert.That(Step(Find("FirstLevelTutorial")), Is.EqualTo("SelectCell"));
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel02");
        Assert.That(Find("FirstLevelTutorial"), Is.Null);
    }

    [UnityTest]
    public IEnumerator EarlyWaveStartsDoNotLockGuideAndBarrierLossOffersRebuild()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Find("OpenFieldPrototypeController"), guide = Find("FirstLevelTutorial");
        Assert.That(Call(owner, "StartAssault"), Is.True, "Guidance must not gate free play.");
        Assert.That(Step(guide), Is.EqualTo("SelectCell"));
        Call(owner, "FinishRun", Enum.Parse(Runtime("OpenFieldPhase"), "Defeat"));
        Assert.That(Get(guide, "IsVisible"), Is.False);
        Assert.That(Step(guide), Is.EqualTo("Finished"));
        Call(owner, "RestartPrototype");
        yield return null;
        owner = Find("OpenFieldPrototypeController");
        guide = Find("FirstLevelTutorial");
        // Isolate the second-round UI contract without spawning or editing shared assets.
        owner.GetType().GetField("roundIndex", Flags).SetValue(owner, 1);
        Call(guide, "Refresh");
        Assert.That(Step(guide), Is.EqualTo("BuildBarrier"));
        BuildSuggested(owner, guide, 3);
        Assert.That(Step(guide), Is.EqualTo("BarrierInfo"));
        Button(guide, "Continue Lesson").onClick.Invoke();
        Assert.That(Step(guide), Is.EqualTo("StartWave"));
        Component wall = ((IEnumerable)Get(owner, "Defenders")).Cast<Component>()
            .Single(d => (bool)Get(d, "InterceptsAllEnemies"));
        Call(wall, "TakeDamage", 500f);
        Assert.That(Step(guide), Is.EqualTo("BuildBarrier"));
        BuildSuggested(owner, guide, 3);
        Assert.That(Step(guide), Is.EqualTo("StartWave"));
        Vector2Int cell = (Vector2Int)Get(((IEnumerable)Get(owner, "Defenders")).Cast<Component>().Single(), "Cell");
        Assert.That(Call(owner, "TrySellAt", cell), Is.True);
        Assert.That(Step(guide), Is.EqualTo("BuildBarrier"));
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator SuggestedDefenseCompletesThreeRealWavesWithoutChangingCombatOrResources()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Find("OpenFieldPrototypeController"), guide = Find("FirstLevelTutorial");
        BuildSuggested(owner, guide, 0);
        BuildSuggested(owner, guide, 0);
        Assert.That(Get(owner, "CurrentATP"), Is.EqualTo(50));
        Call(owner, "SetPlaybackSpeed", 2);
        for (int round = 1; round <= 3; round++)
        {
            if (round == 2)
            {
                Assert.That(Step(guide), Is.EqualTo("BuildBarrier"));
                BuildSuggested(owner, guide, 3);
                Capture(owner, "tutorial-barrier-info.png");
                Button(guide, "Continue Lesson").onClick.Invoke();
            }
            if (round == 3)
            {
                Assert.That(Step(guide), Is.EqualTo("BuildPhagocyte"));
                BuildSuggested(owner, guide, 2);
                Assert.That(Step(guide), Is.EqualTo("ReinforceDefense"));
                BuildSuggested(owner, guide, 0);
                BuildSuggested(owner, guide, 0);
                Assert.That(Step(guide), Is.EqualTo("UpgradeTower"));
                Component target = (Component)Field(guide, "upgradeTarget");
                Vector2Int cell = (Vector2Int)Get(target.GetComponent(Runtime("TowerCombatState")), "Cell");
                Call(owner, "SelectPlacedTower", cell);
                Capture(owner, "tutorial-upgrade.png");
                Component menu = (Component)Field(owner, "towerMenu");
                menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "Upgrade Tower").onClick.Invoke();
                Assert.That(Step(guide), Is.EqualTo("StartWave"));
            }
            Assert.That(Call(owner, "StartAssault"), Is.True);
            float deadline = Time.realtimeSinceStartup + 32f;
            while (Get(owner, "Phase").ToString() == "Assault" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Get(owner, "Phase").ToString(), Is.EqualTo(round == 3 ? "Victory" : "Planning"),
                "Actual defense must clear the wave with the core alive.");
            Debug.Log($"FIRST_LEVEL_PLAYTHROUGH round={round} life={Get(owner, "CurrentCoreLife")} atp={Get(owner, "CurrentATP")}");
        }
        Assert.That(Step(guide), Is.EqualTo("Finished"));
        Assert.That(Get(guide, "IsVisible"), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}

#if UNITY_EDITOR
// Game view size changes run after the existing ZzMobileLandscape fixture.
public sealed class ZzzFirstLevelTutorialMobilePlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Component Find(string name) => MobilePlayModeTests.Find(name);
    private static object Field(object value, string name) => MobilePlayModeTests.Field(value, name);
    private static object Get(object value, string name) => MobilePlayModeTests.Get(value, name);
    private static object Call(object value, string name, params object[] args) => MobilePlayModeTests.Call(value, name, args);
    private static string Step(Component guide) => Get(guide, "Step").ToString();
    private static Button Button(Component guide, string name) => guide.GetComponentsInChildren<Button>(true).Single(b => b.name == name);

    [UnityTest]
    public IEnumerator MobileGuidanceFitsSafeAreaAvoidsShopAndResumesAfterReenable()
    {
        Type resolution = typeof(ZzMobileLandscapePlayModeTests).GetNestedType("Resolution", BindingFlags.NonPublic);
        try
        {
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 720), new Vector2Int(1920, 1080) })
            {
                using ((IDisposable)Activator.CreateInstance(resolution, new object[] { size.x, size.y }))
                {
                    yield return null;
                    yield return null;
                    MobilePlayModeTests.Preview(true);
                    yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
                    yield return null;
                    Component owner = Find("OpenFieldPrototypeController"), guide = Find("FirstLevelTutorial");
                    Canvas.ForceUpdateCanvases();
                    RectTransform panel = (RectTransform)Field(guide, "panel");
                    MobilePlayModeTests.Inside(panel);
                    Vector2Int cell = (Vector2Int)Get(guide, "SuggestedCell");
                    Call(owner, "SelectBuildCell", cell);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    MobilePlayModeTests.Inside(panel);
                    RectTransform shop = (RectTransform)Get(Field(owner, "buildMenu"), "PanelRect");
                    Assert.That(Bounds(panel).Overlaps(Bounds(shop)), Is.False, "The hint must leave the tower cards usable.");
                    ((Behaviour)guide).enabled = false;
                    Assert.That(Get(guide, "IsVisible"), Is.False);
                    Call(owner, "ClearSelection");
                    ((Behaviour)guide).enabled = true;
                    Assert.That(Step(guide), Is.EqualTo("SelectCell"));
                    OpenFieldPlaybackPlayModeTests.CaptureUi((Camera)Field(owner, "gameplayCamera"), $"tutorial-mobile-{size.x}x{size.y}.png");
                    Button(guide, "Skip Tutorial").onClick.Invoke();
                    Assert.That(Get(guide, "IsVisible"), Is.False);
                }
            }
        }
        finally { MobilePlayModeTests.Preview(false); }
    }

    private static Rect Bounds(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        MobilePlayModeTests.Preview(false);
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
