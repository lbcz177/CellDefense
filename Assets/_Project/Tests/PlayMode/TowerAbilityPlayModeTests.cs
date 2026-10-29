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

// Exercise shipped tower prefabs against real moving/projectile/status components.
public sealed class TowerAbilityPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private Component controller, pool;
    private Array definitions;
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
    private GameObject Host(string name)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        owned.Add(obj);
        return obj;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03", LoadSceneMode.Single);
        controller = (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
        pool = (Component)Field(controller, "projectilePool");
        definitions = (Array)Field(controller, "towerDefinitions");
    }

    private Component Tower(int index, Vector3 position, Action<Component> configure = null)
    {
        object definition = definitions.GetValue(index);
        Component prefab = (Component)Get(definition, "Prefab");
        Component tower = Object.Instantiate(prefab, position, Quaternion.identity);
        owned.Add(tower.gameObject);
        Component slot = Host("Ability build slot").AddComponent(Runtime("BuildSlot"));
        slot.gameObject.SetActive(true);
        configure?.Invoke(tower);
        Call(tower, "Initialize", definition, pool, slot);
        Call(slot, "Occupy", tower);
        Set(tower, "attackCooldown", 9999f); // Effects are invoked explicitly, auto combat stays idle.
        Component aura = tower.GetComponent(Runtime("FibrinSlowAura"));
        if (aura != null) Set(aura, "scanCooldown", 9999f);
        return tower;
    }

    private Component Enemy(Vector3 position, float health = 100f, int antigen = 2, float remaining = 10f)
    {
        GameObject obj = Host("Ability target");
        obj.layer = 3;
        foreach (string type in new[] { "Health", "PathFollower", "EnemyImmuneState", "EnemyArmorState",
            "EnemyShieldState", "EnemySlowState", "EnemyController", "EnemyStunState" }) obj.AddComponent(Runtime(type));
        CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
        collider.radius = .15f;
        collider.isTrigger = true;
        Component enemy = obj.GetComponent(Runtime("EnemyController"));
        ScriptableObject definition = ScriptableObject.CreateInstance(Runtime("EnemyDefinition"));
        owned.Add(definition);
        Set(definition, "enemyPrefab", enemy);
        Set(definition, "maxHealth", health);
        Set(definition, "antigenId", Enum.ToObject(Runtime("AntigenId"), antigen));
        Component path = Host("Ability target route").AddComponent(Runtime("WaypointPath"));
        Transform[] points = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            GameObject point = new GameObject("Point");
            point.transform.SetParent(path.transform);
            point.transform.position = position + Vector3.right * (i * remaining);
            points[i] = point.transform;
        }
        Call(path, "Configure", (object)points);
        obj.SetActive(true);
        Call(enemy, "Initialize", path, definition);
        Call(obj.GetComponent(Runtime("PathFollower")), "Stop");
        Physics2D.SyncTransforms();
        return enemy;
    }

    private static Component Strategy(Component tower, string name) => tower.GetComponent(Runtime(name));
    private static float Health(Component enemy) => (float)Get(enemy, "CurrentHealth");
    private static object Antigen(int id) => Enum.ToObject(Runtime("AntigenId"), id);
    private static void AddExtraCollider(Component enemy)
    {
        GameObject child = new GameObject("Duplicate query collider");
        child.layer = 3;
        child.transform.SetParent(enemy.transform, false);
        child.AddComponent<CircleCollider2D>().radius = .15f;
        Physics2D.SyncTransforms();
    }
    private static IEnumerator Until(Func<bool> condition, float timeout = 2f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "Ability condition timed out.");
    }

    [UnityTest]
    public IEnumerator SamplingMarksAndAmplifiesDamageThenEnablesEngulf()
    {
        Component guard = Tower(0, Vector3.zero);
        Component antibody = Tower(1, Vector3.right * 2f);
        Component secondAntibody = Tower(1, new Vector3(2f, 1f));
        Component macrophage = Tower(2, Vector3.up);
        Component target = Enemy(Vector3.right, 65f);
        object slot = Field(guard, "occupiedSlot");
        Array neighbors = Array.CreateInstance(Runtime("BuildSlot"), 2);
        neighbors.SetValue(Field(antibody, "occupiedSlot"), 0);
        neighbors.SetValue(Field(secondAntibody, "occupiedSlot"), 1);
        Call(slot, "ConfigureSignalNeighbors", neighbors);
        Component mark = Strategy(antibody, "AntibodyAttackBehaviour");
        Assert.That(Call(mark, "CanTarget", target), Is.False);
        Call(guard, "Attack", target);
        yield return Until(() => (bool)Call(mark, "KnowsAntigen", Antigen(2)));
        Assert.That(Health(target), Is.EqualTo(40f).Within(.001f));
        Assert.That(Call(Strategy(secondAntibody, "AntibodyAttackBehaviour"), "KnowsAntigen", Antigen(2)), Is.True);
        Component feedback = ((Component)slot).GetComponent(Runtime("AntigenSignalFeedback"));
        Assert.That(Get(feedback, "ActiveConnectionCount"), Is.EqualTo(2));
        Component indicator = (Component)Field(mark, "memoryIndicator");
        Assert.That(indicator.GetComponent<SpriteRenderer>().enabled, Is.True);
        Call(mark, "Attack", target, 0f, pool);
        Component immune = target.GetComponent(Runtime("EnemyImmuneState"));
        yield return Until(() => (bool)Get(immune, "IsMarked"));
        Assert.That(Health(target), Is.EqualTo(40f), "Antibody must not deal direct damage.");
        Call(target, "TakeDamage", 10f);
        Assert.That(Health(target), Is.EqualTo(28.5f).Within(.001f));
        Assert.That(Call(target, "CanEngulf", 20f), Is.False);
        Component engulf = Strategy(macrophage, "PhagocyteAttackBehaviour");
        Call(engulf, "Attack", target, 25f, pool);
        Assert.That(Get(target, "CanBeTargeted"), Is.False);
        Assert.That(Get(engulf, "CanAttack"), Is.False);
    }

    [UnityTest]
    public IEnumerator AntibodySkipsUnknownFrontEnemyAndDoesNotSpendIdleCooldown()
    {
        Component tower = Tower(1, Vector3.zero);
        Component strategy = Strategy(tower, "AntibodyAttackBehaviour");
        Component unknownFront = Enemy(Vector3.right, 100f, 3, 1f);
        Component knownRear = Enemy(Vector3.up, 100f, 2, 4f);
        Enemy(Vector3.left, 100f, 2, 8f);
        Set(tower, "attackCooldown", 0f);
        yield return new WaitForSeconds(.06f);
        Assert.That((float)Field(tower, "attackCooldown"), Is.LessThanOrEqualTo(0f));
        Assert.That(Get(pool, "ActiveCount"), Is.EqualTo(0));
        Call(strategy, "RememberAntigen", Antigen(2), 8f);
        Set(tower, "attackCooldown", 9999f);
        Assert.That(Call(strategy, "CanTarget", unknownFront), Is.False);
        Assert.That(Call(tower, "FindTarget"), Is.SameAs(knownRear));
        // The animation release keeps its promise; an invalid target never switches to the rear one.
        Set(tower, "awaitingAttackRelease", true);
        Set(tower, "pendingAttackTarget", unknownFront);
        Set(tower, "pendingAttackSpawnVersion", Get(unknownFront, "SpawnVersion"));
        Call(tower, "ReleaseAttack");
        Assert.That(Get(pool, "ActiveCount"), Is.EqualTo(0));
        Assert.That(Field(tower, "awaitingAttackRelease"), Is.False);
    }

    [UnityTest]
    public IEnumerator MemoryRingExpiresAndDisablingStrategyClearsMemory()
    {
        Component tower = Tower(1, Vector3.zero);
        Component mark = Strategy(tower, "AntibodyAttackBehaviour");
        SpriteRenderer ring = ((Component)Field(mark, "memoryIndicator")).GetComponent<SpriteRenderer>();
        Call(mark, "RememberAntigen", Antigen(2), .05f);
        Assert.That(ring.enabled, Is.True);
        yield return new WaitForSeconds(.12f);
        Assert.That(Get(mark, "KnownAntigenCount"), Is.EqualTo(0));
        Assert.That(ring.enabled, Is.False);
        Call(mark, "RememberAntigen", Antigen(3), 8f);
        ((Behaviour)mark).enabled = false;
        Assert.That(ring.enabled, Is.False);
        ((Behaviour)mark).enabled = true;
        Assert.That(Get(mark, "KnownAntigenCount"), Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator FibrinUsesStrongestAuraAndRejectsColliderEdgeOnlyTargets()
    {
        Component strong = Strategy(Tower(3, Vector3.zero), "FibrinSlowAura");
        Component weak = Strategy(Tower(3, Vector3.zero, t =>
            Set(Strategy(t, "FibrinSlowAura"), "slowFraction", .2f)), "FibrinSlowAura");
        Component target = Enemy(Vector3.right);
        AddExtraCollider(target);
        Component edge = Enemy(Vector3.right * 2.6f);
        Call(weak, "RefreshAura");
        Call(strong, "RefreshAura");
        Component slow = target.GetComponent(Runtime("EnemySlowState"));
        Assert.That(Get(slow, "SpeedMultiplier"), Is.EqualTo(.6f).Within(.001f));
        Assert.That(((IDictionary)Field(slow, "slowBySource")).Count, Is.EqualTo(2));
        Assert.That(Get(edge.GetComponent(Runtime("EnemySlowState")), "SpeedMultiplier"), Is.EqualTo(1f));
        yield return new WaitForSeconds(.35f);
        Assert.That(Get(slow, "SpeedMultiplier"), Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator LethalAcidHitLeavesFieldsAndOverlappingFieldsDealExactlySixtyDamage()
    {
        Component acid = Strategy(Tower(4, Vector3.zero), "AcidAttackBehaviour");
        Component lethal = Enemy(Vector3.right, 10f);
        Component second = Enemy(Vector3.right * 1.4f, 1000f);
        Component bystander = Enemy(new Vector3(1.2f, .3f));
        AddExtraCollider(bystander);
        Call(acid, "Attack", lethal, 15f, pool);
        Call(acid, "Attack", second, 15f, pool);
        yield return Until(() => (int)Get(acid, "ActiveFieldCount") == 2);
        Assert.That(Get(lethal, "CanBeTargeted"), Is.False);
        yield return new WaitForSeconds(3.4f);
        Assert.That(Health(bystander), Is.EqualTo(40f).Within(.02f));
        Assert.That(Get(acid, "ActiveFieldCount"), Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator AcidDisableCancelsFieldsAndPendingProjectileFieldCreation()
    {
        Component acid = Strategy(Tower(4, Vector3.zero), "AcidAttackBehaviour");
        Component target = Enemy(Vector3.right, 1000f);
        Call(acid, "Attack", target, 15f, pool);
        yield return Until(() => (int)Get(acid, "ActiveFieldCount") == 1);
        Call(acid, "Attack", target, 15f, pool);
        ((Behaviour)acid).enabled = false;
        Assert.That(Get(acid, "ActiveFieldCount"), Is.EqualTo(0));
        yield return new WaitForSeconds(.3f);
        Assert.That(Get(acid, "ActiveFieldCount"), Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator NeuralChainHitsOnlyThreeUniqueTargetsAndShowsTheCommittedPath()
    {
        Component neural = Strategy(Tower(5, Vector3.zero), "NeuralPulseAttackBehaviour");
        Component first = Enemy(Vector3.right);
        Component second = Enemy(Vector3.right * 2f);
        Component third = Enemy(Vector3.right * 3f);
        Component fourth = Enemy(Vector3.right * 4f);
        AddExtraCollider(second);
        Call(neural, "Attack", first, 45f, pool);
        Assert.That(Health(first), Is.EqualTo(55f));
        Assert.That(Health(second), Is.EqualTo(64f));
        Assert.That(Health(third), Is.EqualTo(71.2f).Within(.001f));
        Assert.That(Health(fourth), Is.EqualTo(100f));
        Component stun = first.GetComponent(Runtime("EnemyStunState"));
        Assert.That(Get(stun, "IsStunned"), Is.True);
        float until = (float)Field(stun, "stunnedUntil");
        LineRenderer line = (LineRenderer)Field(neural, "pulse");
        Assert.That(line.enabled, Is.True);
        Assert.That(line.positionCount, Is.GreaterThan(4), "The committed chain now has intermediate zigzag vertices.");
        Assert.That(line.GetPosition(line.positionCount-1), Is.EqualTo(third.transform.position));
        var committed=(List<Vector3>)Field(neural,"pulseNodes");
        Assert.That(committed,Is.EqualTo(new[]{neural.transform.position,first.transform.position,second.transform.position,third.transform.position}));
        Call(neural, "Attack", first, 45f, pool);
        Assert.That(Field(stun, "stunnedUntil"), Is.EqualTo(until));
        yield return new WaitForSeconds(.3f);
        Assert.That(Get(stun, "IsStunned"), Is.False);
        Assert.That(line.enabled, Is.False);
    }

    [UnityTest]
    public IEnumerator NeuralChainSkipsReusedNeighborAfterLethalFirstHit()
    {
        Component neural = Strategy(Tower(5, Vector3.zero), "NeuralPulseAttackBehaviour");
        Component first = Enemy(Vector3.right, 1f);
        Component reused = Enemy(Vector3.right * 2f);
        Component third = Enemy(Vector3.right * 3f);
        Component health = first.GetComponent(Runtime("Health"));
        uint version = (uint)Get(reused, "SpawnVersion");
        Action recycle = () => Call(reused, "Initialize", Get(reused.GetComponent(Runtime("PathFollower")), "CurrentPath"), Get(reused, "Definition"));
        health.GetType().GetEvent("Died").AddEventHandler(health, recycle);
        Call(neural, "Attack", first, 45f, pool);
        Assert.That(Get(first, "CanBeTargeted"), Is.False);
        Assert.That(Get(reused, "SpawnVersion"), Is.EqualTo(version + 1));
        Assert.That(Health(reused), Is.EqualTo(100f));
        Assert.That(Health(third), Is.EqualTo(71.2f).Within(.001f));
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
