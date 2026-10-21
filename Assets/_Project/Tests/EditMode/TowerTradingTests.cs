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

public sealed class TowerTradingTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args)
    {
        try { return target.GetType().GetMethod(name, Flags).Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private GameObject Host(string name)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        owned.Add(obj);
        return obj;
    }
    private Component Controller(int phase = 0, bool barrier = false)
    {
        Component controller = Host("Trading controller").AddComponent(Runtime("OpenFieldPrototypeController"));
        Set(controller, "<Phase>k__BackingField", Enum.ToObject(Runtime("OpenFieldPhase"), phase));
        Set(controller, "width", 7);
        Set(controller, "height", 5);
        Set(controller, "grid", Activator.CreateInstance(Runtime("OpenFieldGridModel"), 7, 5,
            new Vector2Int(6, 2), new[] { new Vector2Int(0, 2) }, null));
        Call(Field(controller, "economy"), "Initialize", 100);
        Object definition = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Towers/" +
            (barrier ? "FibrinTowerDefinition" : "TowerDefinition") + ".asset");
        // Clone config so the price mutation test cannot touch the actual asset.
        definition = Object.Instantiate(definition);
        owned.Add(definition);
        Array definitions = Array.CreateInstance(Runtime("TowerDefinition"), 1);
        definitions.SetValue(definition, 0);
        Set(controller, "towerDefinitions", definitions);
        Set(controller, "projectilePool", Host("Trading pool").AddComponent(Runtime("ProjectilePool")));
        return controller;
    }
    private Component Build(Component controller, Vector2Int cell)
    {
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Component slot = (Component)((IDictionary)Field(controller, "placedSlots"))[cell];
        Component tower = (Component)Get(slot, "CurrentTower");
        owned.Add(tower.gameObject);
        return tower;
    }

    [TestCase(0)]
    [TestCase(1)]
    public void SaleRefundsHalfRoundedDownAndCannotBeRepeated(int phase)
    {
        Component controller = Controller(phase);
        Vector2Int cell = new Vector2Int(3, 2);
        Build(controller, cell);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        Assert.That(Call(controller, "GetSellRefund", cell), Is.EqualTo(12));
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(87));
        Assert.That(Call(Field(controller, "grid"), "IsOccupied", cell), Is.False);
        Assert.That(Call(controller, "TrySellAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(87));
        Build(controller, cell);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(62));
    }

    [TestCase(2)]
    [TestCase(3)]
    public void TerminalPhaseBlocksSelling(int phase)
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Build(controller, cell);
        Set(controller, "<Phase>k__BackingField", Enum.ToObject(Runtime("OpenFieldPhase"), phase));
        Assert.That(Call(controller, "TrySellAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        Assert.That(Call(Field(controller, "grid"), "IsOccupied", cell), Is.True);
    }

    [Test]
    public void LegacyAndOpenFieldRefundSamePaidCostsIncludingUpgrade()
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        Assert.That(Call(tower, "TryUpgrade", 25), Is.True);
        Component legacy = Host("Legacy build rules").AddComponent(Runtime("BuildController"));
        Set(legacy, "slots", Array.CreateInstance(Runtime("BuildSlot"), 0));
        Component slot = (Component)((IDictionary)Field(controller, "placedSlots"))[cell];
        Assert.That(Call(legacy, "GetSellRefund", slot), Is.EqualTo(25));
        Assert.That(Call(controller, "GetSellRefund", cell), Is.EqualTo(Call(legacy, "GetSellRefund", slot)));
    }

    [TestCase(25, true)]
    [TestCase(24, false)]
    public void OriginalPurchaseStillUsesConfigPriceAndAffordability(int atp, bool expected)
    {
        Component controller = Controller();
        object economy = Field(controller, "economy");
        Call(economy, "Initialize", atp);
        Component legacy = Host("Legacy buyer").AddComponent(Runtime("BuildController"));
        Set(legacy, "slots", Array.CreateInstance(Runtime("BuildSlot"), 0));
        Component flow = Host("Legacy flow").AddComponent(Runtime("GameFlowController"));
        Set(flow, "<CurrentState>k__BackingField", Enum.Parse(Runtime("GameState"), "Running"));
        Call(legacy, "Initialize", economy, flow, Field(controller, "projectilePool"));
        Component slot = Host("Legacy fixed slot").AddComponent(Runtime("BuildSlot"));
        object definition = ((Array)Field(controller, "towerDefinitions")).GetValue(0);
        Assert.That(Call(legacy, "TryBuild", slot, definition), Is.EqualTo(expected));
        if (expected) owned.Add(((Component)Get(slot, "CurrentTower")).gameObject);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(expected ? atp - 25 : atp));
    }

    [Test]
    public void LaterPriceChangesDoNotChangeSaleRefund()
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Build(controller, cell);
        Set(((Array)Field(controller, "towerDefinitions")).GetValue(0), "buildCost", 999);
        Assert.That(Call(controller, "GetSellRefund", cell), Is.EqualTo(12));
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(87));
    }

    [Test]
    public void BarrierSaleKeepsRouteAndAllowsRebuilding()
    {
        Component controller = Controller(1, true);
        Call(controller, "CreateRoundPaths");
        Vector2Int cell = new Vector2Int(3, 2);
        Build(controller, cell);
        IList paths = (IList)Field(controller, "roundPaths");
        object oldPath = paths[0];
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(85));
        Assert.That(paths.Count, Is.EqualTo(1));
        Assert.That(paths[0], Is.SameAs(oldPath));
        List<Vector2Int> route = new List<Vector2Int>();
        Assert.That(Call(Field(controller, "grid"), "TryGetPath", new Vector2Int(0, 2), route), Is.True);
        Assert.That(route, Does.Contain(cell));
        Assert.That(Call(controller, "TryBuildAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(55));
    }

    [Test]
    public void DeathGivesNoRefundAndClearsSelectedTower()
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        Assert.That(Call(controller, "SelectPlacedTower", cell), Is.True);
        Call(tower.GetComponent(Runtime("TowerCombatState")), "TakeDamage", 100f);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        Assert.That(Get(controller, "SelectedTower"), Is.Null);
        Assert.That(Call(controller, "TrySellAt", cell), Is.False);
    }

    [Test]
    public void SelectingSameTowerTogglesOffAndSellingClearsSelection()
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        Assert.That(Call(controller, "SelectCell", cell), Is.True);
        Assert.That(Get(controller, "SelectedTower"), Is.SameAs(tower));
        Assert.That(Call(controller, "SelectCell", cell), Is.False);
        Assert.That(Get(controller, "SelectedTower"), Is.Null);
        Assert.That(Call(controller, "SelectCell", cell), Is.True);
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(controller, "SelectedTowerCell"), Is.Null);
    }

    [TestCase("Level02", "BuildController")]
    [TestCase("OpenFieldPrototype", "OpenFieldPrototypeController")]
    [TestCase("OpenFieldLevel02", "OpenFieldPrototypeController")]
    [TestCase("OpenFieldLevel03", "OpenFieldPrototypeController")]
    [TestCase("OpenFieldLevel04", "OpenFieldPrototypeController")]
    [TestCase("OpenFieldLevel05", "OpenFieldPrototypeController")]
    public void SceneUsesOriginalFiftyPercentRefund(string name, string type)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity", OpenSceneMode.Additive);
        try
        {
            Component controller = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(Runtime(type), true)).Single();
            Assert.That(Field(controller, "sellRefundPercent"), Is.EqualTo(50));
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    [TestCase(0)]
    [TestCase(1)]
    public void OpenFieldUpgradeChargesOnceKeepsPlacementAndRefundsBothPayments(int phase)
    {
        Component controller = Controller(phase);
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        float damage = (float)Get(tower, "CurrentDamage");
        object revision = Get(Field(controller, "grid"), "Revision");
        Assert.That(Call(controller, "GetUpgradeCost", cell), Is.EqualTo(25));
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(50));
        Assert.That(Get(tower, "UpgradeCostPaid"), Is.EqualTo(25));
        Assert.That(Get(tower, "CurrentDamage"), Is.EqualTo(damage * 1.5f));
        Assert.That(Get(Field(controller, "grid"), "Revision"), Is.EqualTo(revision));
        Assert.That(Call(controller, "GetUpgradeCost", cell), Is.Zero);
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(50));
        Assert.That(Call(controller, "GetSellRefund", cell), Is.EqualTo(25));
        Assert.That(Call(controller, "TrySellAt", cell), Is.True);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.False);
    }

    [TestCase(24, false)]
    [TestCase(25, true)]
    public void OpenFieldUpgradeUsesPaidBuildPriceAndExactAffordability(int balance, bool expected)
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        Set(Get(tower, "Definition"), "buildCost", 999);
        Call(Field(controller, "economy"), "Initialize", balance);
        Assert.That(Call(controller, "GetUpgradeCost", cell), Is.EqualTo(25));
        Assert.That(Call(controller, "CanUpgradeAt", cell), Is.EqualTo(expected));
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.EqualTo(expected));
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(expected ? 0 : balance));
        Assert.That(Get(tower, "IsUpgraded"), Is.EqualTo(expected));
    }

    [TestCase(2, false)]
    [TestCase(3, false)]
    [TestCase(0, true)]
    public void ResultsOrPauseRejectUpgradeWithoutTakingATP(int phase, bool paused)
    {
        Component controller = Controller();
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        Set(controller, "<Phase>k__BackingField", Enum.ToObject(Runtime("OpenFieldPhase"), phase));
        Set(controller, "<IsPaused>k__BackingField", paused);
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(75));
        Assert.That(Get(tower, "IsUpgraded"), Is.False);
    }

    [Test]
    public void SupportBarrierAndEmptyCellCannotConsumeUpgradeATP()
    {
        Component controller = Controller(0, true);
        Vector2Int cell = new Vector2Int(3, 2);
        Component tower = Build(controller, cell);
        Assert.That(Get(tower, "SupportsDamageUpgrade"), Is.False);
        Assert.That(Call(controller, "GetUpgradeCost", cell), Is.Zero);
        Assert.That(Call(controller, "TryUpgradeAt", cell), Is.False);
        Assert.That(Call(controller, "TryUpgradeAt", new Vector2Int(99, 99)), Is.False);
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(70));
    }

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
}
