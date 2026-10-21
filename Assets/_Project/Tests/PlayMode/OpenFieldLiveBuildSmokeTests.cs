using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class OpenFieldLiveBuildSmokeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] arguments)
    {
        try { return target.GetType().GetMethod(name, Flags).Invoke(target, arguments); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    [UnityTest]
    public IEnumerator FirstLevelSupportsBarrierBuildingAndSummonsWithoutRerouting()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype", LoadSceneMode.Single);
        Component controller = (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
        Assert.That(controller, Is.Not.Null);
        Assert.That(Object.FindObjectsOfType(Runtime("OpenFieldBattleHud")).Length, Is.EqualTo(1));
        Assert.That(Call(controller, "StartAssault"), Is.True);
        IEnumerable active = (IEnumerable)Field(controller, "activeEnemies");
        float deadline = Time.realtimeSinceStartup + 3f;
        while (!active.Cast<Component>().Any() && Time.realtimeSinceStartup < deadline) yield return null;
        Component enemy = active.Cast<Component>().First();
        Call(controller, "SelectTower", 0);
        Assert.That(Call(controller, "TryBuildAt", new Vector2Int(2, 3)), Is.True);
        Call(controller, "SelectTower", 3);
        object grid = Field(controller, "grid");
        List<Vector2Int> routeCells = new List<Vector2Int>();
        Assert.That(Call(grid, "TryGetPath", (Vector2Int)Call(grid, "GetSpawnCell", 0), routeCells), Is.True);
        Vector2Int barrier = routeCells.Skip(2).First(cell =>
            Call(grid, "CheckPlacement", cell, false).ToString() == "Valid");
        Component follower = enemy.GetComponent(Runtime("PathFollower"));
        object oldPath = Get(follower, "CurrentPath");
        Vector3 position = enemy.transform.position;
        object version = Get(enemy, "SpawnVersion");
        Assert.That(Call(controller, "TryBuildAt", barrier), Is.True);
        Assert.That(enemy.transform.position, Is.EqualTo(position));
        Assert.That(Get(enemy, "SpawnVersion"), Is.EqualTo(version));
        Assert.That(Get(follower, "CurrentPath"), Is.SameAs(oldPath));
        Assert.That(Get(controller, "CurrentATP"), Is.EqualTo(45));
        int count = active.Cast<Component>().Count();
        Call(enemy, "RequestSummon", Get(enemy, "Definition"), 1, true);
        Assert.That(active.Cast<Component>().Count(), Is.EqualTo(count + 1));
        Component child = active.Cast<Component>().Last();
        Assert.That(Get(child.GetComponent(Runtime("PathFollower")), "CurrentPath"), Is.SameAs(Get(follower, "CurrentPath")));
        yield return new WaitForSeconds(.2f);
        Assert.That((enemy.transform.position - position).sqrMagnitude, Is.GreaterThan(0f));
        Component hud = (Component)Field(controller, "battleHud");
        Assert.That(((Component)Field(hud, "canvas")).gameObject.activeInHierarchy, Is.True);
        Array slots = (Array)Field(Field(hud, "atp"), "slots");
        string value = string.Concat(slots.Cast<Component>().Where(s => s.gameObject.activeSelf)
            .Select(s => ((Sprite)Get(s, "sprite")).name));
        Assert.That(value, Is.EqualTo("45"));
        controller.gameObject.SetActive(false);
    }
}
