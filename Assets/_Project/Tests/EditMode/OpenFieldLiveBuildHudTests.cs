using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class OpenFieldLiveBuildHudTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] arguments)
    {
        try { return target.GetType().GetMethod(name, Flags).Invoke(target, arguments); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private GameObject Host(string name)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        owned.Add(obj);
        return obj;
    }

    private Component Controller(int phase = 1)
    {
        Component controller = Host("Live build controller").AddComponent(Runtime("OpenFieldPrototypeController"));
        Set(controller, "<Phase>k__BackingField", Enum.ToObject(Runtime("OpenFieldPhase"), phase));
        Set(controller, "width", 7);
        Set(controller, "height", 5);
        Set(controller, "grid", Activator.CreateInstance(Runtime("OpenFieldGridModel"), 7, 5,
            new Vector2Int(6, 2), new[] { new Vector2Int(0, 2) }, null));
        Call(Field(controller, "economy"), "Initialize", 100);
        Call(Field(controller, "life"), "Initialize", 20);
        Set(controller, "projectilePool", Host("Pool").AddComponent(Runtime("ProjectilePool")));
        return controller;
    }

    private Object Definition(string name) => AssetDatabase.LoadAssetAtPath<Object>(
        "Assets/_Project/Data/Towers/" + name + ".asset");

    private void Choose(Component controller, Object definition)
    {
        Array definitions = Array.CreateInstance(Runtime("TowerDefinition"), 1);
        definitions.SetValue(definition, 0);
        Set(controller, "towerDefinitions", definitions);
    }

    private Component Path(params Vector3[] positions)
    {
        GameObject obj = Host("Test route");
        Component path = obj.AddComponent(Runtime("WaypointPath"));
        Transform[] points = positions.Select(p =>
        {
            GameObject point = new GameObject("Point");
            point.transform.SetParent(obj.transform);
            point.transform.position = p;
            return point.transform;
        }).ToArray();
        Call(path, "Configure", (object)points);
        return path;
    }

    private Component MovingEnemy(Component controller)
    {
        GameObject obj = Host("Live enemy");
        Component health = obj.AddComponent(Runtime("Health"));
        Component follower = obj.AddComponent(Runtime("PathFollower"));
        Component enemy = obj.AddComponent(Runtime("EnemyController"));
        // Simulate an already initialized unit without resetting its spawn/combat state.
        Set(enemy, "health", health);
        Set(enemy, "pathFollower", follower);
        Set(enemy, "isInitialized", true);
        Set(enemy, "<SpawnVersion>k__BackingField", 17u);
        Set(enemy, "<Definition>k__BackingField", AssetDatabase.LoadAssetAtPath<Object>(
            "Assets/_Project/Data/Enemies/StaphylococcusDefinition.asset"));
        Call(health, "Initialize", 80f);
        Vector3 start = (Vector3)Call(controller, "CellToWorld", new Vector2Int(0, 2));
        Vector3 next = (Vector3)Call(controller, "CellToWorld", new Vector2Int(1, 2));
        Vector3 end = (Vector3)Call(controller, "CellToWorld", new Vector2Int(6, 2));
        Component path = Path(start, next, end);
        Call(follower, "Initialize", path);
        Call(follower, "SetSpeedMultiplier", .6375f);
        Call(follower, "SetAbilitySpeedMultiplier", .8f);
        Call(follower, "Begin");
        obj.transform.position = Vector3.Lerp(start, next, .35f);
        obj.SetActive(true);
        Call(Field(controller, "activeEnemies"), "Add", enemy);
        return enemy;
    }

    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    [TestCase(3, false)]
    public void BuildingIsAvailableInPlanningAndAssaultOnly(int phase, bool allowed)
    {
        Component controller = Controller(phase);
        Choose(controller, Definition("TowerDefinition"));
        Assert.That(Get(controller, "CanBuild"), Is.EqualTo(allowed));
        if (!allowed)
        {
            Assert.That(Call(controller, "TryBuildAt", new Vector2Int(3, 2)), Is.False);
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(100));
        }
    }

    [TestCase("TowerDefinition", false)]
    [TestCase("FibrinTowerDefinition", true)]
    public void AssaultBuildSpendsOnceAndKeepsTerrainRoutes(string definitionName, bool barrier)
    {
        Component controller = Controller();
        Object definition = Definition(definitionName);
        Choose(controller, definition);
        Call(controller, "CreateRoundPaths");
        Component enemy = MovingEnemy(controller);
        Component follower = enemy.GetComponent(Runtime("PathFollower"));
        object oldPath = Get(follower, "CurrentPath");
        Vector3 position = enemy.transform.position;
        Func<Vector3, bool> movementGate = _ => true;
        follower.GetType().GetProperty("BeforeMove").SetValue(follower, movementGate);
        Assert.That(Call(controller, "TryBuildAt", new Vector2Int(3, 2)), Is.True);
        Component tower = ((Component)((IDictionary)Field(controller, "placedSlots"))[new Vector2Int(3, 2)])
            .GetComponent(Runtime("BuildSlot"));
        owned.Add(((Component)Get(tower, "CurrentTower")).gameObject);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(100 - (int)Get(definition, "BuildCost")));
        Assert.That(Call(controller, "TryBuildAt", new Vector2Int(3, 2)), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(100 - (int)Get(definition, "BuildCost")));
        Assert.That(Get(((Component)Get(tower, "CurrentTower")).GetComponent(Runtime("TowerCombatState")),
            "InterceptsAllEnemies"), Is.EqualTo(barrier));
        Assert.That(enemy.transform.position, Is.EqualTo(position));
        Assert.That(Get(enemy, "CurrentHealth"), Is.EqualTo(80f));
        Assert.That(Get(enemy, "SpawnVersion"), Is.EqualTo(17u));
        Assert.That(Get(follower, "SceneSpeedMultiplier"), Is.EqualTo(.6375f));
        Assert.That(Field(follower, "abilitySpeedMultiplier"), Is.EqualTo(.8f));
        Assert.That(Get(follower, "BeforeMove"), Is.SameAs(movementGate));
        Assert.That(Field(follower, "isMoving"), Is.True);
        Assert.That(Get(follower, "CurrentPath"), Is.SameAs(oldPath));
        List<Vector2Int> route = new List<Vector2Int>();
        Call(Field(controller, "grid"), "TryGetPath", new Vector2Int(0, 2), route);
        Assert.That(route, Has.Member(new Vector2Int(3, 2)));
        object request = Activator.CreateInstance(Runtime("EnemySpawnRequest"), enemy,
            AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Enemies/StaphylococcusDefinition.asset"), 1, false);
        Assert.That(Field(request, "Path"), Is.SameAs(oldPath));
    }

    [Test]
    public void BarrierMayBeBuiltOnUpcomingRouteNodeWithoutReroutingEnemy()
    {
        Component controller = Controller();
        Choose(controller, Definition("FibrinTowerDefinition"));
        Component enemy = MovingEnemy(controller);
        Component follower = enemy.GetComponent(Runtime("PathFollower"));
        object path = Get(follower, "CurrentPath");
        Vector3 position = enemy.transform.position;
        Assert.That(Call(controller, "TryBuildAt", new Vector2Int(1, 2)), Is.True);
        owned.Add(((Component)Get(((IDictionary)Field(controller, "placedSlots"))[new Vector2Int(1, 2)], "CurrentTower")).gameObject);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(70));
        Assert.That(Get(follower, "CurrentPath"), Is.SameAs(path));
        Assert.That(enemy.transform.position, Is.EqualTo(position));
    }

    [Test]
    public void BarrierMustKeepStrandedSurvivorReachableEvenIfEntranceCanReachCore()
    {
        // Pocket (3,0) has a single exit through (3,1); entrance/core use the top half.
        object grid = Activator.CreateInstance(Runtime("OpenFieldGridModel"), 5, 5,
            new Vector2Int(4, 4), new[] { new Vector2Int(0, 4) },
            new[] { new Vector2Int(2, 0), new Vector2Int(4, 0) });
        Vector2Int barrier = new Vector2Int(3, 1);
        Assert.That(Call(grid, "CheckPlacement", barrier, true).ToString(), Is.EqualTo("Valid"));
        Assert.That(Call(grid, "CheckPlacementKeepingRoutes", barrier, true,
            new[] { new Vector2Int(3, 0) }).ToString(), Is.EqualTo("BlocksRoute"));
    }

    [Test]
    public void AssaultBarrierBuildStillHonorsATP()
    {
        Component controller = Controller();
        Choose(controller, Definition("FibrinTowerDefinition"));
        Call(Field(controller, "economy"), "Initialize", 29);
        int before = (int)Get(controller, "CurrentATP");
        Assert.That(Call(controller, "TryBuildAt", new Vector2Int(3, 2)), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(before));
        Assert.That(Call(Field(controller, "grid"), "IsOccupied", new Vector2Int(3, 2)), Is.False);
    }

    [Test]
    public void MoreThanSixBarriersCanBeBuiltAndBrokenWithoutChangingRoutesOrRefundingDeath()
    {
        Component controller = Controller();
        Choose(controller, Definition("FibrinTowerDefinition"));
        Call(Field(controller, "economy"), "Initialize", 300);
        Call(controller, "CreateRoundPaths");
        object path = ((IList)Field(controller, "roundPaths"))[0];
        for (int x = 1; x <= 5; x++)
        {
            Vector2Int cell = new Vector2Int(x, 2);
            Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
            Component tower = (Component)Get(((IDictionary)Field(controller, "placedSlots"))[cell], "CurrentTower");
            owned.Add(tower.gameObject);
            if (x <= 3)
            {
                int before = (int)Get(controller, "CurrentATP");
                Call(tower.GetComponent(Runtime("TowerCombatState")), "TakeDamage", 500f);
                Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(before));
                Assert.That(Call(Field(controller, "grid"), "IsOccupied", cell), Is.False);
                Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
                owned.Add(((Component)Get(((IDictionary)Field(controller, "placedSlots"))[cell], "CurrentTower")).gameObject);
            }
        }
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(60), "Eight successful builds at 30 ATP.");
        Assert.That(((IList)Field(controller, "roundPaths"))[0], Is.SameAs(path));
    }

    [Test]
    public void RerouteKeepsStoppedStateAndRejectsDiscontinuousNextNode()
    {
        Component follower = Host("Stopped follower").AddComponent(Runtime("PathFollower"));
        Component old = Path(Vector3.zero, Vector3.right, Vector3.right * 3f);
        Call(follower, "Initialize", old);
        follower.transform.position = Vector3.right * .4f;
        Component replacement = Path(Vector3.right, Vector3.right, Vector3.right + Vector3.up, Vector3.right * 3f);
        Call(follower, "Reroute", replacement);
        Assert.That(follower.transform.position, Is.EqualTo(Vector3.right * .4f));
        Assert.That(Field(follower, "isMoving"), Is.False);
        Assert.That((float)Get(follower, "RemainingDistance"), Is.GreaterThan(3f));
        Component invalid = Path(Vector3.zero, Vector3.up);
        Assert.Throws<ArgumentException>(() => Call(follower, "Reroute", invalid));
        Assert.That(Get(follower, "CurrentPath"), Is.SameAs(replacement));
    }

    private string Displayed(object hud, string field)
    {
        Array slots = (Array)Field(Field(hud, field), "slots");
        return string.Concat(slots.Cast<Component>().Where(s => s.gameObject.activeSelf)
            .Select(s => ((Sprite)Get(s, "sprite")).name));
    }

    [Test]
    public void HudBindsChangingGameplayValuesAndHidesAtVictory()
    {
        Component controller = Controller();
        Set(controller, "waveSequence", AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Waves/EnemyRoster01_WaveSequence.asset"));
        controller.gameObject.SetActive(true);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        owned.Add(instance);
        Component hud = instance.GetComponent(Runtime("OpenFieldBattleHud"));
        Call(hud, "Initialize", controller);
        Assert.That(Displayed(hud, "atp"), Is.EqualTo("100"));
        Assert.That(Displayed(hud, "life"), Is.EqualTo("20"));
        Assert.That(Displayed(hud, "round"), Is.EqualTo("1"));
        Assert.That(Displayed(hud, "roundTotal"), Is.EqualTo("3"));
        Call(Field(controller, "economy"), "TrySpend", 30);
        Call(Field(controller, "life"), "TryLoseLife", 2);
        Set(controller, "roundIndex", 2);
        Call(hud, "Refresh");
        Assert.That(Displayed(hud, "atp"), Is.EqualTo("70"));
        Assert.That(Displayed(hud, "life"), Is.EqualTo("18"));
        Assert.That(Displayed(hud, "round"), Is.EqualTo("3"));
        Assert.That(Get(Field(hud, "defenseText"), "text"), Is.EqualTo("壁垒可承伤 · 拦截全部经过的敌人"));
        Set(controller, "<Phase>k__BackingField", Enum.ToObject(Runtime("OpenFieldPhase"), 2));
        Call(hud, "Refresh");
        Assert.That(((Component)Field(hud, "canvas")).gameObject.activeSelf, Is.False);
    }

    [TestCase(0, "0")]
    [TestCase(1250, "1250")]
    [TestCase(20, "20")]
    [TestCase(9, "9")]
    [TestCase(2147483647, "2147483647")]
    public void PrefabDigitsDisplayActualValueWithoutCreatingChildren(int value, string expected)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        owned.Add(instance);
        Component hud = instance.GetComponent(Runtime("OpenFieldBattleHud"));
        object resources = Runtime("OpenFieldUiResources").GetMethod("LoadDefaults").Invoke(null, null);
        Component number = (Component)Field(hud, "atp");
        Call(number, "Initialize", resources);
        int childCount = instance.GetComponentsInChildren<Transform>(true).Length;
        Call(number, "SetValue", int.MaxValue);
        Call(number, "SetValue", value);
        Array slots = (Array)Field(number, "slots");
        string actual = string.Concat(slots.Cast<Component>().Where(s => s.gameObject.activeSelf)
            .Select(s => ((Sprite)Get(s, "sprite")).name));
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(instance.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(childCount));
        RectTransform last = (RectTransform)((Component)slots.GetValue(expected.Length - 1)).transform;
        Assert.That(last.anchoredPosition.x + last.rect.width, Is.LessThanOrEqualTo(((RectTransform)number.transform).rect.width + .001f));
        Assert.That(((RectTransform)Field(hud, "panelRect")).rect.width, Is.EqualTo(512f));
    }

    [TestCase("OpenFieldPrototype")]
    [TestCase("OpenFieldLevel02")]
    [TestCase("OpenFieldLevel03")]
    [TestCase("OpenFieldLevel04")]
    [TestCase("OpenFieldLevel05")]
    public void EachSceneContainsLinkedHudPrefabWithAllBindings(string name)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity", OpenSceneMode.Additive);
        try
        {
            Component controller = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(Runtime("OpenFieldPrototypeController"), true)).Single();
            Component hud = (Component)Field(controller, "battleHud");
            Assert.That(hud, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud.gameObject),
                Is.EqualTo("Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab"));
            foreach (string field in new[] { "canvas", "panelRect", "frameRect", "atp", "life", "round", "roundTotal", "defenseText" })
                Assert.That(Field(hud, field), Is.Not.Null, field);
            Object prefab = (Object)Get(Runtime("OpenFieldUiResources").GetMethod("LoadDefaults").Invoke(null, null), "BattleHudPrefab");
            Assert.That(prefab, Is.Not.Null);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in owned.AsEnumerable().Reverse()) if (obj != null) Object.DestroyImmediate(obj);
        owned.Clear();
    }
}
