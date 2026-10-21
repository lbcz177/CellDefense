using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Editor-only checks; never initializes combat components or changes configuration.
public static class PlanningConfigValidation
{
    public static void Collect(Object target, List<string> errors)
    {
        if (target == null) { errors.Add("请选择一个有效配置。"); return; }
        try
        {
            if (target is TowerDefinition tower) CheckTower(tower, errors);
            else if (target is EnemyDefinition enemy) CheckEnemy(enemy, errors);
            else if (target is WaveDefinition wave) CheckWave(wave, errors);
            else if (target is OpenFieldPrototypeController level) CheckLevel(level, errors);
            else errors.Add("该对象不是支持的策划配置。");
        }
        catch (Exception error) { errors.Add("配置不完整：" + error.Message); }
    }

    private static void CheckTower(TowerDefinition tower, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(tower.DisplayName)) errors.Add("细胞名称不能为空。");
        if (tower.BuildCost < 1) errors.Add("建造花费必须至少为 1 ATP。");
        Positive(tower.MaxHealth, "细胞最大生命", errors);
        Positive(tower.AttackRange, "攻击射程", errors);
        Positive(tower.AttackInterval, "攻击间隔", errors);
        if (tower.InterceptCapacity < 1) errors.Add("接敌容量必须至少为 1。");
        if (!Enum.IsDefined(typeof(TargetingMode), tower.TargetingMode)) errors.Add("选敌方式无效。");
        if (tower.Prefab == null) { errors.Add("尚未指定细胞预制体。"); return; }
        TowerAttackBehaviour[] strategies = tower.Prefab.GetComponents<TowerAttackBehaviour>();
        if (strategies.Length > 1) errors.Add("细胞预制体只能有一种特殊攻击策略。");
        TowerAttackBehaviour strategy = strategies.Length > 0 ? strategies[0] : null;
        bool requiresDamage = strategy == null || strategy.RequiresDamage;
        if (!Finite(tower.Damage) || tower.Damage < 0f || (requiresDamage && tower.Damage <= 0f))
            errors.Add(requiresDamage ? "输出塔伤害必须大于 0。" : "辅助塔伤害不能为负数。");
        if ((strategy == null || strategy.RequiresProjectile) && tower.ProjectilePrefab == null)
            errors.Add("该攻击方式需要投射物配置。");
        if (tower.Prefab.GetComponentInChildren<SpriteRenderer>() == null)
            errors.Add("当前放置预览要求细胞预制体包含 SpriteRenderer。");
    }

    private static void CheckEnemy(EnemyDefinition enemy, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(enemy.DisplayName)) errors.Add("敌人名称不能为空。");
        Positive(enemy.MaxHealth, "敌人最大生命", errors);
        Positive(enemy.BaseMoveSpeed, "基础移动速度", errors);
        Positive(enemy.ContactDamage, "近战单次伤害", errors);
        Positive(enemy.ContactAttackInterval, "近战间隔", errors);
        Positive(enemy.ArmorBreakDuration, "破壳持续时间", errors);
        Positive(enemy.StunRecoveryDuration, "停顿恢复窗口", errors);
        Range(enemy.ArmorReduction, 0f, .95f, "护甲减伤必须为 0%–95%。", errors);
        Range(enemy.SlowEffectiveness, 0f, 1f, "减速生效比例必须为 0%–100%。", errors);
        Range(enemy.StunEffectiveness, 0f, 1f, "停顿生效比例必须为 0%–100%。", errors);
        if (!Finite(enemy.InitialShield) || enemy.InitialShield < 0f) errors.Add("初始护盾必须为非负有限数。");
        if (enemy.KillReward < 0) errors.Add("击杀奖励不能为负数。");
        if (enemy.LeakDamage < 1) errors.Add("漏怪扣核心生命必须至少为 1。");
        if (!Enum.IsDefined(typeof(AntigenId), enemy.AntigenId)) errors.Add("抗原类别无效。");
        if (enemy.EnemyPrefab == null) { errors.Add("尚未指定敌人预制体。"); return; }
        GameObject prefab = enemy.EnemyPrefab.gameObject;
        if (prefab.GetComponent<Health>() == null || prefab.GetComponent<PathFollower>() == null)
            errors.Add("敌人预制体需要生命与沿路移动组件。");
        if (enemy.ArmorReduction > 0f && prefab.GetComponent<EnemyArmorState>() == null)
            errors.Add("有护甲的敌人需要护甲状态组件。");
        if (enemy.InitialShield > 0f && prefab.GetComponent<EnemyShieldState>() == null)
            errors.Add("有初始护盾的敌人需要护盾状态组件。");
        if (enemy.IsBoss && (prefab.GetComponent<BossPhaseController>() == null ||
            prefab.GetComponents<BossAbilityBehaviour>().Length != 1))
            errors.Add("Boss 预制体需要阶段控制器和唯一一种 Boss 能力。");
        if (!enemy.IsBoss && prefab.GetComponent<BossAbilityBehaviour>() != null)
            errors.Add("当前预制体带有 Boss 能力；不能仅取消 Boss 标识，请换用普通敌人预制体。");
    }

    private static void CheckWave(WaveDefinition wave, List<string> errors)
    {
        NonNegative(wave.SpawnInterval, "逐只生成间隔", errors);
        NonNegative(wave.BurstInterval, "批次间隔", errors);
        using (var serialized = new SerializedObject(wave))
            if (!wave.HasMixedGroups && serialized.FindProperty("burstSize").intValue < 1)
                errors.Add("每批数量必须至少为 1。");
        if (wave.GroupCount < 1) errors.Add("波次至少要生成一组敌人。");
        var checkedEnemies = new HashSet<EnemyDefinition>();
        long total = 0;
        for (int i = 0; i < wave.GroupCount; i++)
        {
            string prefix = $"第 {i + 1} 组：";
            EnemyDefinition enemy = wave.GetGroupEnemy(i);
            if (enemy == null) errors.Add(prefix + "未指定敌人。");
            else if (checkedEnemies.Add(enemy))
            {
                var enemyErrors = new List<string>();
                CheckEnemy(enemy, enemyErrors);
                foreach (string message in enemyErrors) errors.Add(prefix + message);
            }
            int count = wave.GetGroupCount(i);
            if (count < 1) errors.Add(prefix + "数量必须至少为 1。");
            if (wave.GetGroupBurstSize(i) < 1) errors.Add(prefix + "每批数量必须至少为 1。");
            NonNegative(wave.GetGroupAfterDelay(i), prefix + "组结束等待", errors);
            total += count;
        }
        if (total > int.MaxValue) errors.Add("波次总数超过可用范围，请减少敌人数。");
    }

    private static void CheckLevel(OpenFieldPrototypeController level, List<string> errors)
    {
        using (var current = new SerializedObject(level))
        {
            Positive(current.FindProperty("enemySpeedMultiplier").floatValue, "关卡速度倍率", errors);
            Positive(current.FindProperty("signalRadiusCells").floatValue, "抗原共享半径", errors);
            Positive(current.FindProperty("cellSize").floatValue, "地图格尺寸", errors);
            if (current.FindProperty("initialATP").intValue < 0 ||
                current.FindProperty("betweenRoundATP").intValue < 0)
                errors.Add("开局 ATP 和轮间奖励不能为负数。");
            if (current.FindProperty("initialCoreLife").intValue < 1) errors.Add("核心生命必须至少为 1。");
            Range(current.FindProperty("sellRefundPercent").intValue, 0f, 100f, "出售返还比例必须为 0%–100%。", errors);
            if (current.FindProperty("projectilePool").objectReferenceValue == null ||
                current.FindProperty("gameplayCamera").objectReferenceValue == null ||
                current.FindProperty("buildMenuFont").objectReferenceValue == null)
                errors.Add("关卡缺少子弹池、摄像机或中文字体引用，请检查场景配置。");
            SerializedProperty towers = current.FindProperty("towerDefinitions");
            var unique = new HashSet<TowerDefinition>();
            for (int i = 0; i < towers.arraySize; i++)
            {
                TowerDefinition tower = towers.GetArrayElementAtIndex(i).objectReferenceValue as TowerDefinition;
                if (tower == null) { errors.Add($"第 {i + 1} 个可建细胞未配置。"); continue; }
                if (!unique.Add(tower)) { errors.Add("可建细胞列表含有重复配置。"); continue; }
                var towerErrors = new List<string>();
                CheckTower(tower, towerErrors);
                foreach (string message in towerErrors) errors.Add(tower.DisplayName + "：" + message);
            }
            WaveSequenceDefinition sequence = current.FindProperty("waveSequence").objectReferenceValue as WaveSequenceDefinition;
            if (sequence == null || sequence.WaveCount != 3) errors.Add("关卡必须引用一个包含三轮的顺序资源。");
            else
                for (int i = 0; i < sequence.WaveCount; i++)
                {
                    WaveDefinition wave;
                    try { wave = sequence.GetWave(i); }
                    catch (Exception) { errors.Add($"第 {i + 1} 轮尚未指定波次。"); continue; }
                    var waveErrors = new List<string>();
                    CheckWave(wave, waveErrors);
                    foreach (string message in waveErrors) errors.Add($"第 {i + 1} 轮：" + message);
                }
            try
            {
                new OpenFieldGridModel(current.FindProperty("width").intValue,
                    current.FindProperty("height").intValue, current.FindProperty("coreCell").vector2IntValue,
                    Cells(current.FindProperty("spawnCells")), Cells(current.FindProperty("blockedCells")));
            }
            catch (Exception) { errors.Add("地图格、入口或固定障碍配置无效，必须保留所有入口可达。"); }
        }
    }

    private static Vector2Int[] Cells(SerializedProperty property)
    {
        var cells = new Vector2Int[property.arraySize];
        for (int i = 0; i < cells.Length; i++) cells[i] = property.GetArrayElementAtIndex(i).vector2IntValue;
        return cells;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void Positive(float value, string label, List<string> errors)
    {
        if (!Finite(value) || value <= 0f) errors.Add(label + "必须是大于 0 的有限数。");
    }
    private static void NonNegative(float value, string label, List<string> errors)
    {
        if (!Finite(value) || value < 0f) errors.Add(label + "必须是非负有限数。");
    }
    private static void Range(float value, float min, float max, string message, List<string> errors)
    {
        if (!Finite(value) || value < min || value > max) errors.Add(message);
    }
}

