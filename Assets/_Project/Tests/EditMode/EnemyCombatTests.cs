using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

// Reflection keeps this existing asmdef independent of the predefined Assembly-CSharp.
// These tests inspect real runtime components; they do not copy combat implementation.
public sealed class EnemyCombatTests
{
    private readonly List<Object> owned = new List<Object>();
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    private GameObject Host(string name)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        owned.Add(obj);
        return obj;
    }

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Get(object target, string property)
        => target.GetType().GetProperty(property, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(Flags).First(m =>
            m.Name == name && m.GetParameters().Length == arguments.Length &&
            m.GetParameters().Select((p, i) => arguments[i] == null ||
                p.ParameterType.IsInstanceOfType(arguments[i]) ||
                Nullable.GetUnderlyingType(p.ParameterType) == arguments[i].GetType()).All(ok => ok));
        try { return method.Invoke(target, arguments); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static void Watch(object target, string eventName, Action<object[]> callback)
    {
        EventInfo info = target.GetType().GetEvent(eventName);
        ParameterExpression[] parameters = info.EventHandlerType.GetMethod("Invoke").GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType)).ToArray();
        NewArrayExpression boxed = Expression.NewArrayInit(typeof(object),
            parameters.Select(p => Expression.Convert(p, typeof(object))));
        Delegate handler = Expression.Lambda(info.EventHandlerType,
            Expression.Invoke(Expression.Constant(callback), boxed), parameters).Compile();
        info.AddEventHandler(target, handler);
    }

    private Rig Create(float hp = 100f, float armor = 0f, float shield = 0f, string boss = null,
        Rig summon = null, int antigen = 2)
    {
        GameObject obj = Host("Enemy Test");
        foreach (string type in new[] { "Health", "PathFollower", "EnemyImmuneState", "EnemyArmorState",
            "EnemyShieldState", "EnemySlowState", "EnemyStunState", "EnemyController" })
            obj.AddComponent(Runtime(type));
        Component enemy = obj.GetComponent(Runtime("EnemyController"));
        Component health = obj.GetComponent(Runtime("Health"));
        Component follower = obj.GetComponent(Runtime("PathFollower"));
        Component ability = null, phases = null;
        if (boss != null)
        {
            ability = obj.AddComponent(Runtime(boss));
            phases = obj.AddComponent(Runtime("BossPhaseController"));
            if (summon != null)
                Set(ability, boss == "FungalBossBehaviour" ? "sporeDefinition" : "cloneDefinition", summon.Definition);
            Type stepType = Runtime("BossPhaseStep");
            Array steps = Array.CreateInstance(stepType, 3);
            float[] thresholds = boss == "VirusBossBehaviour" ? new[] { 1f, .65f, .3f } : new[] { 1f, .7f, .35f };
            for (int i = 0; i < 3; i++)
            {
                object step = Activator.CreateInstance(stepType);
                Set(step, "healthThreshold", thresholds[i]);
                Set(step, "warningDuration", i == 0 ? 0f : .8f);
                Set(step, "label", "Phase " + i);
                steps.SetValue(step, i);
            }
            Set(phases, "steps", steps);
        }
        ScriptableObject definition = ScriptableObject.CreateInstance(Runtime("EnemyDefinition"));
        owned.Add(definition);
        Set(definition, "enemyPrefab", enemy);
        Set(definition, "maxHealth", hp);
        Set(definition, "armorReduction", armor);
        Set(definition, "initialShield", shield);
        Set(definition, "isBoss", boss != null);
        Set(definition, "engulfImmune", boss != null);
        Set(definition, "antigenId", Enum.ToObject(Runtime("AntigenId"), antigen));
        if (boss != null)
        {
            Set(definition, "slowEffectiveness", .5f);
            Set(definition, "stunEffectiveness", .5f);
            Set(definition, "stunRecoveryDuration", 1f);
        }
        GameObject pathObject = Host("Test Path");
        Component path = pathObject.AddComponent(Runtime("WaypointPath"));
        Transform[] points = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject("Point");
            point.transform.SetParent(pathObject.transform);
            point.transform.position = Vector3.right * (i * 10f);
            points[i] = point.transform;
        }
        Call(path, "Configure", (object)points);
        // Explicit caches make this an EditMode test without launching a scene or Play.
        Call(follower, "Awake");
        Call(obj.GetComponent(Runtime("EnemyImmuneState")), "Awake");
        Call(obj.GetComponent(Runtime("EnemyStunState")), "Awake");
        Call(enemy, "Awake");
        obj.SetActive(true);
        Call(enemy, "OnEnable");
        Call(enemy, "Initialize", path, definition);
        return new Rig { Host = obj, Enemy = enemy, Health = health, Path = path,
            Follower = follower, Definition = definition, Ability = ability, Phases = phases };
    }

    private static object Damage(float amount, int kind) => Activator.CreateInstance(
        Runtime("EnemyDamageContext"), amount, Enum.ToObject(Runtime("EnemyDamageKind"), kind));

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void Shield_ExactHit_DoesNotSendZeroDamageToHealth()
    {
        Rig rig = Create(shield: 80f);
        Call(rig.Enemy, "TakeDamage", 80f);
        Assert.That(Get(rig.Enemy, "CurrentShield"), Is.EqualTo(0f));
        Assert.That(Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(100f));
    }

    [Test]
    public void Acid_BreaksArmorBeforeDamage_AndMarkAppliesOnce()
    {
        Rig rig = Create(240f, .25f);
        Call(rig.Host.GetComponent(Runtime("EnemyImmuneState")), "ApplyMark", 4f);
        Call(rig.Enemy, "TakeDamage", 25f);
        Assert.That((float)Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(218.4375f).Within(.001f));
        Call(rig.Enemy, "TakeDamage", Damage(15f, 1));
        Assert.That((float)Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(201.1875f).Within(.001f));
        Assert.That(Get(rig.Enemy, "IsArmorBroken"), Is.True);
        Call(rig.Enemy, "TakeDamage", Damage(25f, 2));
        Assert.That((float)Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(172.4375f).Within(.001f));
    }

    [Test]
    public void Engulf_BypassesArmor_AndExitsOnlyOnce()
    {
        Rig rig = Create(240f, .25f);
        Call(rig.Health, "TakeDamage", 200f);
        int exits = 0;
        Watch(rig.Enemy, "Exited", _ => exits++);
        Assert.That(Call(rig.Enemy, "TryEngulf", 50f), Is.True);
        Assert.That(Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(0f));
        Assert.That(Call(rig.Enemy, "TryEngulf", 50f), Is.False);
        Assert.That(exits, Is.EqualTo(1));
    }

    [Test]
    public void Shield_PreventsEngulf_UntilItIsConsumed()
    {
        Rig rig = Create(40f, shield: 80f);
        Assert.That(Call(rig.Enemy, "TryEngulf", 50f), Is.False);
        Call(rig.Enemy, "TakeDamage", 80f);
        Assert.That(Call(rig.Enemy, "TryEngulf", 50f), Is.True);
    }

    [Test]
    public void Healing_IsCapped_AndCannotReviveAnExitedEnemy()
    {
        Rig rig = Create();
        Call(rig.Enemy, "TakeDamage", 25f);
        Assert.That(Call(rig.Enemy, "Heal", 500f), Is.EqualTo(25f));
        Assert.That(Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(100f));
        Call(rig.Enemy, "TakeDamage", 100f);
        Assert.That(Call(rig.Enemy, "Heal", 500f), Is.EqualTo(0f));
        Assert.That(Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(0f));
    }

    [Test]
    public void Reinitialize_ResetsDefenseMarkAndSpawnIdentity()
    {
        Rig rig = Create(240f, .25f, 80f);
        Component immune = rig.Host.GetComponent(Runtime("EnemyImmuneState"));
        Call(immune, "ApplyMark", 4f);
        Call(rig.Enemy, "TakeDamage", Damage(100f, 1));
        uint version = (uint)Get(rig.Enemy, "SpawnVersion");
        Call(rig.Enemy, "Initialize", rig.Path, rig.Definition);
        Assert.That((uint)Get(rig.Enemy, "SpawnVersion"), Is.EqualTo(version + 1));
        Assert.That(Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(240f));
        Assert.That(Get(rig.Enemy, "CurrentShield"), Is.EqualTo(80f));
        Assert.That(Get(rig.Enemy, "IsArmorBroken"), Is.False);
        Assert.That(Get(immune, "IsMarked"), Is.False);
    }

    [Test]
    public void Summon_InheritsCurrentSegmentAndPosition()
    {
        Rig parent = Create();
        Rig child = Create(30f);
        object request = null;
        Call(parent.Follower, "InitializeAt", parent.Path, Vector3.right * 12f, 2);
        Watch(parent.Enemy, "SummonRequested", values => request = values[0]);
        Call(parent.Enemy, "RequestSummon", child.Definition, 3, true);
        Assert.That(request, Is.Not.Null);
        Call(child.Enemy, "Initialize", parent.Path, child.Definition, .75f, request);
        Assert.That(child.Host.transform.position, Is.EqualTo(Vector3.right * 12f));
        Assert.That((float)Get(child.Enemy, "RemainingPathDistance"), Is.EqualTo(8f).Within(.001f));
        Assert.That(Get(child.Follower, "NextPointIndex"), Is.EqualTo(2));
    }

    [Test]
    public void FungalBoss_ThresholdJump_HasOnlyTwoBoundedBatches()
    {
        Rig spore = Create(30f);
        Rig boss = Create(800f, boss: "FungalBossBehaviour", summon: spore, antigen: 5);
        int total = 0;
        Watch(boss.Enemy, "SummonRequested", values =>
            total += (int)values[0].GetType().GetField("Count").GetValue(values[0]));
        Call(boss.Phases, "Tick", 0f);
        Call(boss.Enemy, "TakeDamage", 600f);
        Call(boss.Phases, "Tick", 0f);
        Call(boss.Enemy, "Heal", 600f);
        Call(boss.Phases, "Tick", 1f);
        Call(boss.Phases, "Tick", 2f);
        Call(boss.Phases, "Tick", 100f);
        Assert.That(total, Is.EqualTo(6));
        Assert.That(Get(boss.Ability, "SummonsIssued"), Is.EqualTo(6));
        Assert.That(Call(boss.Enemy, "TryEngulf", 1000f), Is.False);
    }

    [Test]
    public void Boss_KilledDuringWarning_DoesNotSummon()
    {
        Rig spore = Create(30f);
        Rig boss = Create(800f, boss: "FungalBossBehaviour", summon: spore, antigen: 5);
        int requests = 0;
        Watch(boss.Enemy, "SummonRequested", _ => requests++);
        Call(boss.Phases, "Tick", 0f);
        Call(boss.Enemy, "TakeDamage", 300f);
        Call(boss.Phases, "Tick", 0f);
        Call(boss.Enemy, "TakeDamage", 10000f);
        Call(boss.Phases, "Tick", 10f);
        Assert.That(requests, Is.EqualTo(0));
    }

    [Test]
    public void VirusMutation_ClearsOnlyMotherMark_AndSnapshotsCloneAntigen()
    {
        Rig clone = Create(45f, antigen: 7);
        Rig boss = Create(2000f, boss: "VirusBossBehaviour", summon: clone, antigen: 7);
        List<object> requests = new List<object>();
        Watch(boss.Enemy, "SummonRequested", values => requests.Add(values[0]));
        Component immune = boss.Host.GetComponent(Runtime("EnemyImmuneState"));
        Call(clone.Host.GetComponent(Runtime("EnemyImmuneState")), "ApplyMark", 4f);
        Call(boss.Ability, "ExecutePhase", 0);
        Call(immune, "ApplyMark", 4f);
        Call(boss.Ability, "ExecutePhase", 1);
        Assert.That(Get(immune, "IsMarked"), Is.False);
        Assert.That(Get(clone.Host.GetComponent(Runtime("EnemyImmuneState")), "IsMarked"), Is.True);
        Call(boss.Ability, "ExecutePhase", 2);
        Call(boss.Ability, "ExecutePhase", 2);
        Assert.That(requests.Count, Is.EqualTo(2));
        Assert.That(Convert.ToInt32(requests[0].GetType().GetField("Antigen").GetValue(requests[0])), Is.EqualTo(7));
        Assert.That(Convert.ToInt32(requests[1].GetType().GetField("Antigen").GetValue(requests[1])), Is.EqualTo(8));
        Assert.That(Get(boss.Ability, "SummonsIssued"), Is.EqualTo(8));
    }

    [Test]
    public void OldAntibodyProjectile_DoesNotMarkMutatedTarget()
    {
        Rig rig = Create(antigen: 7);
        GameObject prefab = Host("Projectile Template");
        Component projectilePrefab = prefab.AddComponent(Runtime("Projectile"));
        GameObject host = Host("Test Projectile Pool");
        Component pool = host.AddComponent(Runtime("ProjectilePool"));
        Set(pool, "prefab", projectilePrefab);
        Set(pool, "initialCapacity", 0);
        Call(pool, "Initialize");
        object projectile = Call(pool, "Get", Vector3.zero);
        Call(projectile, "InitializeMark", rig.Enemy, 4f);
        Call(rig.Enemy, "ChangeAntigen", Enum.ToObject(Runtime("AntigenId"), 8));
        Call(projectile, "Hit");
        Assert.That(Get(rig.Host.GetComponent(Runtime("EnemyImmuneState")), "IsMarked"), Is.False);
        Assert.That(Get(rig.Enemy, "CurrentHealth"), Is.EqualTo(100f));
        Assert.That(Get(pool, "ActiveCount"), Is.EqualTo(0));
    }

    [Test]
    public void BossControlResistance_UsesStrongestSlowAndRecoveryWindow()
    {
        Rig spore = Create(30f);
        Rig boss = Create(800f, boss: "FungalBossBehaviour", summon: spore);
        Component slow = boss.Host.GetComponent(Runtime("EnemySlowState"));
        Call(slow, "ApplySlow", .4f, 1, 2f);
        Call(slow, "ApplySlow", .3f, 2, 2f);
        Assert.That((float)Get(slow, "SpeedMultiplier"), Is.EqualTo(.8f).Within(.001f));
        Component stun = boss.Host.GetComponent(Runtime("EnemyStunState"));
        Assert.That(Call(stun, "TryStun", .2f), Is.True);
        float until = (float)stun.GetType().GetField("stunnedUntil", Flags).GetValue(stun);
        Assert.That(until - Time.time, Is.EqualTo(.1f).Within(.02f));
        Assert.That(Call(stun, "TryStun", .2f), Is.False);
    }

    [Test]
    public void MembraneSupport_SelectsThreeNearest_AndShieldDoesNotStack()
    {
        Rig boss = Create(1400f, shield: 80f, boss: "MembraneBossBehaviour", antigen: 6);
        Rig[] allies = new Rig[4];
        for (int i = 0; i < allies.Length; i++)
        {
            allies[i] = Create();
            allies[i].Host.transform.position = Vector3.right * (.2f + i * .3f);
            allies[i].Host.AddComponent<CircleCollider2D>();
            Call(allies[i].Enemy, "TakeDamage", 25f);
        }
        Physics2D.SyncTransforms();
        float now = Time.time;
        Call(boss.Ability, "Tick", now + 6f);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(Get(allies[i].Enemy, "CurrentShield"), Is.EqualTo(40f));
            Assert.That(Get(allies[i].Enemy, "CurrentHealth"), Is.EqualTo(80f));
        }
        Assert.That(Get(allies[3].Enemy, "CurrentShield"), Is.EqualTo(0f));
        Assert.That(Get(allies[3].Enemy, "CurrentHealth"), Is.EqualTo(75f));
        Call(boss.Ability, "Tick", now + 12f);
        Assert.That(Get(allies[0].Enemy, "CurrentShield"), Is.EqualTo(40f));
        Assert.That(Get(boss.Enemy, "CurrentShield"), Is.EqualTo(80f));
        Call(boss.Enemy, "Clear");
        Call(boss.Ability, "Tick", now + 18f);
        Assert.That(Get(allies[0].Enemy, "CurrentHealth"), Is.EqualTo(85f));
    }

    [Test]
    public void HealingFromZeroHealthNotification_CannotPreventDeath()
    {
        Rig rig = Create();
        float attempted = -1f;
        Watch(rig.Health, "HealthChanged", values =>
        {
            if ((float)values[0] == 0f) attempted = (float)Call(rig.Health, "Heal", 500f);
        });
        Call(rig.Enemy, "TakeDamage", 100f);
        Assert.That(attempted, Is.EqualTo(0f));
        Assert.That(Get(rig.Health, "IsDead"), Is.True);
    }

    private Component CreateDefender(int capacity = 1, bool barrier = false)
    {
        ScriptableObject definition = ScriptableObject.CreateInstance(Runtime("TowerDefinition"));
        owned.Add(definition);
        Set(definition, "maxHealth", capacity == 2 ? 250f : 100f);
        Set(definition, "interceptCapacity", capacity);
        Set(definition, "blocksEnemyRoute", barrier);
        GameObject obj = Host("Defense Cell");
        Component tower = obj.AddComponent(Runtime("TowerController"));
        Set(tower, "<Definition>k__BackingField", definition);
        Component state = obj.AddComponent(Runtime("TowerCombatState"));
        Call(state, "Initialize", tower, new Vector2Int(1, 0));
        obj.transform.position = Vector3.right * 10f;
        obj.SetActive(true);
        return state;
    }

    private Component CreateContact(Rig rig, Component defender)
    {
        Component contact = rig.Host.GetComponent(Runtime("EnemyContactCombat"));
        if (contact == null) contact = rig.Host.AddComponent(Runtime("EnemyContactCombat"));
        Type lookupType = typeof(Func<,>).MakeGenericType(typeof(Vector3), Runtime("TowerCombatState"));
        ParameterExpression position = Expression.Parameter(typeof(Vector3));
        Delegate lookup = Expression.Lambda(lookupType, Expression.Constant(defender,
            Runtime("TowerCombatState")), position).Compile();
        Call(contact, "Initialize", rig.Enemy, 1f, lookup, (Func<bool>)(() => true));
        rig.Host.transform.position = Vector3.right * 9.4f;
        return contact;
    }

    [Test]
    public void OrdinaryCell_IsOccupiedButKeepsChokePointTraversable()
    {
        object grid = Activator.CreateInstance(Runtime("OpenFieldGridModel"), 3, 3,
            new Vector2Int(1, 0), new[] { new Vector2Int(1, 2) },
            new[] { new Vector2Int(0, 1), new Vector2Int(2, 1) });
        Vector2Int choke = new Vector2Int(1, 1);
        Assert.That(Call(grid, "CheckPlacement", choke, true).ToString(), Is.EqualTo("BlocksRoute"));
        Assert.That(Call(grid, "TryOccupy", choke, false), Is.True);
        List<Vector2Int> route = new List<Vector2Int>();
        Assert.That(Call(grid, "IsOccupied", choke), Is.True);
        Assert.That(Call(grid, "TryGetPath", new Vector2Int(1, 2), route), Is.True);
        Assert.That(route, Has.Member(choke));
        Assert.That(Call(grid, "CheckPlacement", choke, false).ToString(), Is.EqualTo("OccupiedCell"));
    }

    [Test]
    public void BarrierPreview_ReroutesAndRestoresActualGrid()
    {
        Vector2Int start = new Vector2Int(0, 1), barrier = new Vector2Int(1, 1);
        object grid = Activator.CreateInstance(Runtime("OpenFieldGridModel"), 5, 3,
            new Vector2Int(4, 1), new[] { start }, null);
        List<Vector2Int> before = new List<Vector2Int>(), preview = new List<Vector2Int>(),
            after = new List<Vector2Int>();
        Call(grid, "TryGetPath", start, before);
        Assert.That(Call(grid, "TryGetPreviewPath", start, barrier, preview), Is.True);
        Call(grid, "TryGetPath", start, after);
        Assert.That(preview, Has.No.Member(barrier));
        Assert.That(after, Is.EqualTo(before));
        Assert.That(Call(grid, "IsOccupied", barrier), Is.False);
        Assert.That(Get(grid, "Revision"), Is.EqualTo(0));
    }

    [Test]
    public void BarrierQuotaExhaustion_BlocksOnlyBarrierChoices()
    {
        GameObject obj = Host("Quota Controller");
        Component controller = obj.AddComponent(Runtime("OpenFieldPrototypeController"));
        object grid = Activator.CreateInstance(Runtime("OpenFieldGridModel"), 5, 3,
            new Vector2Int(4, 1), new[] { new Vector2Int(0, 1) }, null);
        Set(controller, "grid", grid);
        Set(controller, "barrierBuildLimit", 6);
        Set(controller, "barriersBuilt", 6);
        Call(controller.GetType().GetField("economy", Flags).GetValue(controller), "Initialize", 1000);
        Component defender = CreateDefender(barrier: true);
        object definition = Get(Get(defender, "Tower"), "Definition");
        Set(definition, "prefab", Get(defender, "Tower"));
        Assert.That(Get(controller, "RemainingBarrierBuilds"), Is.EqualTo(0));
        Assert.That(Call(controller, "GetBuildRejection", definition, new Vector2Int(1, 1)),
            Is.EqualTo("壁垒额度已用尽"));
        Set(definition, "blocksEnemyRoute", false);
        Assert.That(Call(controller, "GetBuildRejection", definition, new Vector2Int(1, 1)), Is.EqualTo(""));
    }

    [Test]
    public void ContactCapacity_RejectsOverflowAndPreservesVersionOwnership()
    {
        Component defender = CreateDefender(2);
        Rig first = Create(), second = Create(), overflow = Create();
        Assert.That(Call(defender, "TryEngage", first.Enemy), Is.True);
        Assert.That(Call(defender, "TryEngage", second.Enemy), Is.True);
        Assert.That(Call(defender, "TryEngage", overflow.Enemy), Is.False);
        uint version = (uint)Get(first.Enemy, "SpawnVersion");
        Call(defender, "Release", first.Enemy, version + 1);
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(2));
        Call(defender, "Release", first.Enemy, version);
        Assert.That(Call(defender, "TryEngage", overflow.Enemy), Is.True);
        Assert.That(Get(defender, "MaxHealth"), Is.EqualTo(250f));
    }

    [Test]
    public void OverflowCommitsToPassing_AndTowerDeathReleasesHeldEnemy()
    {
        Component defender = CreateDefender();
        Rig first = Create(), overflow = Create();
        Component contact = CreateContact(first, defender), passing = CreateContact(overflow, defender);
        Assert.That(Call(contact, "TryMove", Vector3.right * 10f), Is.False);
        Assert.That(first.Host.transform.position.x, Is.EqualTo(9.55f).Within(.001f));
        Assert.That(Call(passing, "TryMove", Vector3.right * 10f), Is.True);
        Call(contact, "ResetState");
        Assert.That(Call(passing, "TryMove", Vector3.right * 10f), Is.True);
        Call(contact, "Initialize", first.Enemy, 1f,
            passing.GetType().GetField("findCell", Flags).GetValue(passing), (Func<bool>)(() => true));
        Assert.That(Call(contact, "TryMove", Vector3.right * 10f), Is.False);
        int deaths = 0;
        Watch(defender, "Died", _ => deaths++);
        Call(defender, "TakeDamage", 100f);
        Call(defender, "TakeDamage", 100f);
        Assert.That(Get(defender, "IsAlive"), Is.False);
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(0));
        Assert.That(Call(contact, "TryMove", Vector3.right * 10f), Is.True);
        Assert.That(deaths, Is.EqualTo(1));
    }

    [Test]
    public void EnemyExitAndReuse_ClearContactAndMovementGate()
    {
        Component defender = CreateDefender();
        Rig rig = Create();
        Component contact = CreateContact(rig, defender);
        Call(contact, "TryMove", Vector3.right * 10f);
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(1));
        Call(rig.Enemy, "Clear");
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(0));
        Assert.That(Get(rig.Follower, "BeforeMove"), Is.Null);
        Call(rig.Enemy, "Initialize", rig.Path, rig.Definition);
        Assert.That(Get(contact, "Target"), Is.Null);
        CreateContact(rig, defender);
        Call(contact, "TryMove", Vector3.right * 10f);
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(1));
        Call(rig.Enemy, "Initialize", rig.Path, rig.Definition);
        Assert.That(Get(defender, "ContactCount"), Is.EqualTo(0));
        Assert.That(Get(rig.Follower, "BeforeMove"), Is.Null);
    }

    [Test]
    public void ContactDamage_WaitsOneInterval_NoCatchUpBurst_AndStunPreventsHit()
    {
        Component defender = CreateDefender();
        Rig rig = Create();
        Set(rig.Definition, "contactDamage", 25f);
        Component contact = CreateContact(rig, defender);
        Call(contact, "TryMove", Vector3.right * 10f);
        float entered = Time.time;
        Call(contact, "Tick", entered + .5f);
        Assert.That(Get(defender, "CurrentHealth"), Is.EqualTo(100f));
        Call(contact, "Tick", entered + 2f);
        Assert.That(Get(defender, "CurrentHealth"), Is.EqualTo(75f));
        Call(contact, "Tick", entered + 10f);
        Assert.That(Get(defender, "CurrentHealth"), Is.EqualTo(50f));
        Call(rig.Host.GetComponent(Runtime("EnemyStunState")), "TryStun", .2f);
        Call(contact, "Tick", entered + 12f);
        Assert.That(Get(defender, "CurrentHealth"), Is.EqualTo(50f));
    }

    private sealed class Rig
    {
        public GameObject Host;
        public Component Enemy, Health, Follower, Path, Ability, Phases;
        public ScriptableObject Definition;
    }
}
