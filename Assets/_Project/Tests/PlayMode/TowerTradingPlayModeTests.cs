using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class TowerTradingPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethods(Flags).First(m => m.Name == name &&
            m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) =>
                args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(valid => valid));
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static Component Controller() => (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    private static Vector2Int FreeCell(Component controller)
    {
        object grid = Field(controller, "grid");
        for (int x = 2; x < 13; x++)
            for (int y = 2; y < 9; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (Call(grid, "CheckPlacement", cell, false).ToString() == "Valid" &&
                    !(bool)Call(grid, "IsBlocked", cell + Vector2Int.left)) return cell;
            }
        throw new AssertionException("Expected a free cell.");
    }

    [UnityTest]
    public IEnumerator FiveLevelsUseSelectBuyInspectCancelAndConfirmSaleFlow()
    {
        foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            Component controller = Controller();
            int initial = (int)Get(controller, "CurrentATP");
            Vector2Int cell = FreeCell(controller);
            Assert.That(Call(controller, "SelectCell", cell), Is.True);
            Assert.That(Get(Field(controller, "buildMenu"), "IsVisible"), Is.True);
            Call(controller, "TryBuildSelectedChoice", 0);
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 25));
            Assert.That(Get(Field(controller, "buildMenu"), "IsVisible"), Is.False);
            Assert.That(Call(controller, "SelectCell", cell), Is.True);
            object menu = Field(controller, "towerMenu");
            Assert.That(Get(menu, "IsVisible"), Is.True);
            Assert.That(((LineRenderer)Field(Field(controller, "presentation"), "rangeRenderer")).enabled, Is.True);
            Call(menu, "RequestSell");
            Assert.That(Get(menu, "IsConfirming"), Is.True);
            Assert.That(Call(menu, "ContainsPointer", Vector2.zero), Is.True, "Modal must block map input.");
            Call(menu, "CancelSell");
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 25));
            Assert.That(Get(controller, "SelectedTower"), Is.Not.Null);
            Call(menu, "RequestSell");
            Call(menu, "ConfirmSell");
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 13));
            Assert.That(Get(controller, "SelectedTower"), Is.Null);
            Assert.That(Get(menu, "IsVisible"), Is.False);
            Call(menu, "ConfirmSell");
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 13));
        }
    }

    [UnityTest]
    public IEnumerator SaleImmediatelyReleasesHeldEnemyWithoutResettingItsRoute()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03", LoadSceneMode.Single);
        Component controller = Controller();
        Vector2Int cell = FreeCell(controller);
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Component defender = (Component)((IDictionary)Field(controller, "cellDefenders"))[cell];
        Vector3 center = (Vector3)Call(controller, "CellToWorld", cell);
        object definition = Call(Call(Field(controller, "waveSequence"), "GetWave", 0), "GetGroupEnemy", 0);
        Component enemy = Object.Instantiate((Component)Get(definition, "EnemyPrefab"));
        owned.Add(enemy.gameObject);
        GameObject pathObject = new GameObject("Trading contact route");
        owned.Add(pathObject);
        Component path = pathObject.AddComponent(Runtime("WaypointPath"));
        Transform[] points = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject("Point");
            point.transform.SetParent(pathObject.transform);
            point.transform.position = center + Vector3.right * (i - 1);
            points[i] = point.transform;
        }
        Call(path, "Configure", (object)points);
        Call(enemy, "Initialize", path, definition, 1f, null);
        Component follower = enemy.GetComponent(Runtime("PathFollower"));
        Call(follower, "Stop");
        Component contact = enemy.GetComponent(Runtime("EnemyContactCombat"));
        if (contact == null) contact = enemy.gameObject.AddComponent(Runtime("EnemyContactCombat"));
        Type lookupType = contact.GetType().GetMethod("Initialize").GetParameters()[2].ParameterType;
        Delegate lookup = Expression.Lambda(lookupType, Expression.Constant(defender, Runtime("TowerCombatState")),
            Expression.Parameter(typeof(Vector3))).Compile();
        Call(contact, "Initialize", enemy, 1f, lookup, (Func<bool>)(() => true));
        Call(Field(controller, "activeEnemies"), "Add", enemy);
        Assert.That(Call(contact, "TryMove", center - Vector3.right * .1f), Is.False);
        Assert.That(Get(contact, "Target"), Is.SameAs(defender));
        object route = Get(follower, "CurrentPath");
        Vector3 position = enemy.transform.position;
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(contact, "Target"), Is.Null);
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(0));
        Assert.That(Get(follower, "CurrentPath"), Is.SameAs(route));
        Assert.That(enemy.transform.position, Is.EqualTo(position));
        Assert.That(Call(contact, "TryMove", center), Is.True);
    }

    [UnityTest]
    public IEnumerator SellingBarrierDuringWaveKeepsSurvivorRoutesAndFutureSpawns()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03", LoadSceneMode.Single);
        Component controller = Controller();
        object grid = Field(controller, "grid");
        List<Vector2Int> route = new List<Vector2Int>();
        Call(grid, "TryGetPath", (Vector2Int)Call(grid, "GetSpawnCell", 0), route);
        Vector2Int cell = route.Skip(2).First(c => Call(grid, "CheckPlacement", c, false).ToString() == "Valid");
        Call(controller, "SelectTower", 3);
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Assert.That(Call(controller, "StartAssault"), Is.True);
        yield return null;
        Component[] enemies = ((IEnumerable)Field(controller, "activeEnemies")).Cast<Component>().ToArray();
        Assert.That(enemies.Length, Is.GreaterThan(0));
        Vector3[] positions = enemies.Select(e => e.transform.position).ToArray();
        object[] versions = enemies.Select(e => Get(e, "SpawnVersion")).ToArray();
        object[] paths = enemies.Select(e => Get(e.GetComponent(Runtime("PathFollower")), "CurrentPath")).ToArray();
        int before = (int)Get(controller, "CurrentATP");
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(before + 15));
        for (int i = 0; i < enemies.Length; i++)
        {
            Assert.That(enemies[i].transform.position, Is.EqualTo(positions[i]));
            Assert.That(Get(enemies[i], "SpawnVersion"), Is.EqualTo(versions[i]));
            Assert.That(Get(enemies[i].GetComponent(Runtime("PathFollower")), "CurrentPath"), Is.SameAs(paths[i]));
        }
        Assert.That(Call(grid, "IsOccupied", cell), Is.False);
    }

    [UnityTest]
    public IEnumerator BarrierHoldsCrowdTakesDamageBreaksAndReplacementSaleReleasesEveryEnemy()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype", LoadSceneMode.Single);
        Component controller = Controller();
        Call(controller, "SelectTower", 3);
        object grid = Field(controller, "grid");
        List<Vector2Int> cells = new List<Vector2Int>();
        Call(grid, "TryGetPath", (Vector2Int)Call(grid, "GetSpawnCell", 0), cells);
        Vector2Int cell = cells.Skip(1).First(c => Call(grid, "CheckPlacement", c, false).ToString() == "Valid");
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Component defender = (Component)((IDictionary)Field(controller, "cellDefenders"))[cell];
        Assert.That(Get(defender, "MaxHealth"), Is.EqualTo(500f));
        Call(controller, "SelectCell", cell);
        string label = (string)Get(Field(Field(controller, "towerMenu"), "info"), "text");
        Assert.That(label, Does.Contain("全部经过的敌人").And.Not.Contain("改路").And.Not.Contain("额度"));
        Assert.That((string)Get(controller, "DefenseStatusText"), Does.Not.Contain("额度"));
        Call(controller, "CreateRoundPaths");
        Set(controller, "<Phase>k__BackingField", Enum.Parse(Runtime("OpenFieldPhase"), "Assault"));
        object path = ((IList)Field(controller, "roundPaths"))[0];
        object definition = Call(Call(Field(controller, "waveSequence"), "GetWave", 0), "GetGroupEnemy", 0);
        Component[] enemies = new Component[8];
        object[] versions = new object[8];
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i] = (Component)Call(controller, "SpawnEnemy", definition, path, null);
            versions[i] = Get(enemies[i], "SpawnVersion");
        }
        float deadline = Time.realtimeSinceStartup + 8f;
        while ((int)Get(defender, "ContactCount") < 8 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(8));
        Call(Field(controller, "enemyHud"), "LateUpdate");
        Call(Field(controller, "towerMenu"), "Refresh");
        Canvas.ForceUpdateCanvases();
        OpenFieldPlaybackPlayModeTests.CaptureUi((Camera)Field(controller, "gameplayCamera"), "fibrin-barrier-holding.png");
        Vector3[] heldPositions = enemies.Select(e => e.transform.position).ToArray();
        Call(controller, "Pause");
        float pausedHealth = (float)Get(defender, "CurrentHealth");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(Get(defender, "CurrentHealth"), Is.EqualTo(pausedHealth));
        for (int i = 0; i < enemies.Length; i++) Assert.That(enemies[i].transform.position, Is.EqualTo(heldPositions[i]));
        Call(controller, "Resume");
        int beforeDeath = (int)Get(controller, "CurrentATP");
        deadline = Time.realtimeSinceStartup + 12f;
        while ((bool)Call(grid, "IsOccupied", cell) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Call(grid, "IsOccupied", cell), Is.False, "The whole group must damage and break the barrier.");
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(beforeDeath));
        for (int i = 0; i < enemies.Length; i++)
        {
            Assert.That(Get(enemies[i].GetComponent(Runtime("EnemyContactCombat")), "Target"), Is.Null);
            Assert.That(Get(enemies[i].GetComponent(Runtime("PathFollower")), "CurrentPath"), Is.SameAs(path));
            Assert.That(Get(enemies[i], "SpawnVersion"), Is.EqualTo(versions[i]));
        }
        // No cumulative quota or death cooldown: the same valid cell can be used again.
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Component replacement = (Component)((IDictionary)Field(controller, "cellDefenders"))[cell];
        deadline = Time.realtimeSinceStartup + 2f;
        while ((int)Get(replacement, "ContactCount") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That((int)Get(replacement, "ContactCount"), Is.GreaterThan(0));
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        foreach (Component enemy in enemies)
            Assert.That(Get(enemy.GetComponent(Runtime("EnemyContactCombat")), "Target"), Is.Null);
        Vector3 released = enemies[0].transform.position;
        yield return new WaitForSeconds(.15f);
        Assert.That((enemies[0].transform.position - released).sqrMagnitude, Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator DeadTowerConfirmationCannotSellReplacementTower()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype", LoadSceneMode.Single);
        Component controller = Controller();
        Vector2Int cell = FreeCell(controller);
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Call(controller, "SelectCell", cell);
        object menu = Field(controller, "towerMenu");
        Call(menu, "RequestSell");
        Component original = (Component)Get(controller, "SelectedTower");
        Call(original.GetComponent(Runtime("TowerCombatState")), "TakeDamage", 100f);
        Assert.That(Get(menu, "IsConfirming"), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Call(menu, "ConfirmSell");
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(50));
        Assert.That(Call(Field(controller, "grid"), "IsOccupied", cell), Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TerminalStateClosesConfirmationAndBlocksSale()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype", LoadSceneMode.Single);
        Component controller = Controller();
        Vector2Int cell = FreeCell(controller);
        Call(controller, "TryBuildAt", cell);
        Call(controller, "SelectCell", cell);
        object menu = Field(controller, "towerMenu");
        Call(menu, "RequestSell");
        Call(controller, "FinishRun", Enum.Parse(Runtime("OpenFieldPhase"), "Defeat"));
        Call(menu, "ConfirmSell");
        Assert.That(Get(menu, "IsVisible"), Is.False);
        Assert.That(Call(controller, "TrySellAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        yield return null;
    }

    [UnityTest]
    public IEnumerator FiveLevelsUpgradeThroughMenuKeepsHealthAndRefundsPaidUpgrade()
    {
        foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
        {
            yield return SceneManager.LoadSceneAsync(scene);
            Component controller = Controller();
            int initial = (int)Get(controller, "CurrentATP");
            Vector2Int cell = FreeCell(controller);
            Call(controller, "TryBuildAt", cell);
            Call(controller, "SelectCell", cell);
            Component tower = (Component)Get(controller, "SelectedTower");
            Component state = tower.GetComponent(Runtime("TowerCombatState"));
            Call(state, "TakeDamage", 20f);
            float health = (float)Get(state, "CurrentHealth"), damage = (float)Get(tower, "CurrentDamage");
            object menu = Field(controller, "towerMenu");
            Button upgrade = ((Component)menu).GetComponentsInChildren<UnityEngine.UI.Button>(true)
                .Single(b => b.name == "Upgrade Tower");
            Call(menu, "Refresh");
            Call(Field(controller, "battleHud"), "Refresh");
            Canvas.ForceUpdateCanvases();
            if (scene == "OpenFieldPrototype") OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, "upgrade-before.png");
            Assert.That(upgrade.interactable, Is.True);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(upgrade.gameObject);
            upgrade.onClick.Invoke();
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.Null);
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 50));
            Assert.That(Get(controller, "SelectedTower"), Is.SameAs(tower));
            Assert.That(Get(state, "CurrentHealth"), Is.EqualTo(health));
            Assert.That(Get(tower, "CurrentDamage"), Is.EqualTo(damage * 1.5f));
            Assert.That(upgrade.interactable, Is.False);
            Assert.That((string)Get(Field(menu, "info"), "text"), Does.Contain("Lv.2"));
            Assert.That(Call(controller, "GetSellRefund", cell), Is.EqualTo(25));
            Call(Field(controller, "battleHud"), "Refresh");
            if (scene == "OpenFieldPrototype") OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, "upgrade-after.png");
            upgrade.onClick.Invoke();
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 50));
            Call(menu, "RequestSell");
            Call(menu, "ConfirmSell");
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(initial - 25));
        }
    }

    [UnityTest]
    public IEnumerator AssaultUpgradesFourDamageTowersWithoutReroutingAndKeepsTwoSupportTowersAtLevelOne()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03");
        Component controller = Controller();
        Call(Field(controller, "economy"), "Initialize", 10000);
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int i = 0; i < 6; i++)
        {
            Vector2Int cell = FreeCell(controller);
            Call(controller, "SelectTower", i);
            Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
            cells.Add(cell);
        }
        Assert.That(Call(controller, "StartAssault"), Is.True);
        object revision = Get(Field(controller, "grid"), "Revision");
        int upgraded = 0, support = 0;
        foreach (Vector2Int cell in cells)
        {
            Call(controller, "SelectCell", cell);
            Component tower = (Component)Get(controller, "SelectedTower");
            bool expected = tower.GetComponent(Runtime("AntibodyAttackBehaviour")) == null &&
                !(bool)Get(Get(tower, "Definition"), "InterceptsAllEnemies");
            int before = (int)Get(controller, "CurrentATP");
            int paid = (int)Get(tower, "BuildCostPaid");
            Assert.That(Call(controller, "TryUpgradeAt", cell), Is.EqualTo(expected));
            Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(expected ? before - paid : before));
            if (expected) upgraded++;
            else
            {
                support++;
                Assert.That((string)Get(Field(Field(controller, "towerMenu"), "upgradeLabel"), "text"), Does.Contain("暂未开放"));
                Assert.That(Get(tower, "IsUpgraded"), Is.False);
            }
        }
        Assert.That(upgraded, Is.EqualTo(4));
        Assert.That(support, Is.EqualTo(2));
        Assert.That(Get(Field(controller, "grid"), "Revision"), Is.EqualTo(revision));
    }

    [UnityTest]
    public IEnumerator UnaffordablePausedAndDeadTowerUpgradeCallbacksDoNotSpendATP()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component controller = Controller();
        Vector2Int cell = FreeCell(controller);
        Call(controller, "TryBuildAt", cell);
        Call(controller, "SelectCell", cell);
        object menu = Field(controller, "towerMenu");
        Component tower = (Component)Get(controller, "SelectedTower");
        Call(Field(controller, "economy"), "Initialize", 24);
        Call(menu, "Refresh");
        Assert.That(((UnityEngine.UI.Button)Field(menu, "upgradeButton")).interactable, Is.False);
        Call(menu, "UpgradeTower");
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(24));
        Call(Field(controller, "economy"), "Initialize", 100);
        Call(controller, "Pause");
        Call(menu, "UpgradeTower");
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(100));
        Call(controller, "Resume");
        Call(controller, "SelectCell", cell);
        Call(menu, "RequestSell");
        Call(menu, "UpgradeTower");
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(100));
        Call(menu, "CancelSell");
        Call(tower.GetComponent(Runtime("TowerCombatState")), "TakeDamage", 1000f);
        Call(menu, "UpgradeTower");
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(100));
        Call(controller, "TryBuildAt", cell);
        Call(menu, "UpgradeTower");
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (Object obj in owned.AsEnumerable().Reverse()) if (obj != null) Object.Destroy(obj);
        owned.Clear();
        yield return null;
    }
}
