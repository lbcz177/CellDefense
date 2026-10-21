using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class TowerSupportTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args)
    {
        try { return target.GetType().GetMethod(name, Flags).Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private Component Antibody()
    {
        GameObject obj = new GameObject("Antibody test");
        obj.SetActive(false);
        owned.Add(obj);
        return obj.AddComponent(Runtime("AntibodyAttackBehaviour"));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void AntigenMemoryRejectsInvalidDuration(float duration)
    {
        Component antibody = Antibody();
        object antigen = Enum.ToObject(Runtime("AntigenId"), 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => Call(antibody, "RememberAntigen", antigen, duration));
    }

    [Test]
    public void RememberingForShorterDurationDoesNotEraseLongerMemory()
    {
        Component antibody = Antibody();
        object antigen = Enum.ToObject(Runtime("AntigenId"), 2);
        Call(antibody, "RememberAntigen", antigen, 8f);
        IDictionary memories = (IDictionary)antibody.GetType().GetField("knownAntigens", Flags).GetValue(antibody);
        float until = (float)memories[antigen];
        Call(antibody, "RememberAntigen", antigen, 1f);
        Assert.That(memories[antigen], Is.EqualTo(until));
        Assert.That(Call(antibody, "KnowsAntigen", antigen), Is.True);
        Call(antibody, "OnDisable");
        Assert.That(Get(antibody, "KnownAntigenCount"), Is.EqualTo(0));
        Assert.That(Call(antibody, "KnowsAntigen", antigen), Is.False);
    }

    [Test]
    public void DisabledOrDeadTargetCannotBeMarked()
    {
        Component antibody = Antibody();
        GameObject obj = new GameObject("Inactive enemy");
        obj.SetActive(false);
        owned.Add(obj);
        Component enemy = obj.AddComponent(Runtime("EnemyController"));
        Assert.That(Call(antibody, "CanTarget", enemy), Is.False);
    }

    [Test]
    public void AntibodyPrefabHasMemoryIndicatorAndZeroDamageDefinition()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Towers/InterceptorTower.prefab");
        Component antibody = prefab.GetComponent(Runtime("AntibodyAttackBehaviour"));
        Component indicator = (Component)antibody.GetType().GetField("memoryIndicator", Flags).GetValue(antibody);
        Assert.That(indicator, Is.Not.Null);
        Assert.That(indicator.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        Object definition = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/Towers/InterceptorTowerDefinition.asset");
        Assert.That(Get(definition, "Damage"), Is.EqualTo(0f));
        Assert.That(Get(definition, "DisplayName"), Is.EqualTo("抗体标记塔"));
    }

    [TestCase(-1f, 50f)]
    [TestCase(20f, 19f)]
    [TestCase(float.NaN, 50f)]
    [TestCase(20f, float.PositiveInfinity)]
    public void EngulfThresholdsAreValidatedDuringInitialization(float normal, float marked)
    {
        GameObject obj = new GameObject("Phagocyte test");
        obj.SetActive(false);
        owned.Add(obj);
        Component attack = obj.AddComponent(Runtime("PhagocyteAttackBehaviour"));
        Set(attack, "baseEngulfHealth", normal);
        Set(attack, "markedEngulfHealth", marked);
        Assert.Throws<InvalidOperationException>(() => Call(attack, "Initialize", null, (LayerMask)8));
    }

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
}
