using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Edits the actual project definitions and explicitly opened level scene.</summary>
public sealed class PlanningConfigWindow : EditorWindow
{
    private static readonly string[] Tabs = { "塔 / 细胞", "敌人", "关卡", "波次" };
    [SerializeField] private int tab;
    [SerializeField] private Object selected;
    [SerializeField] private string search = "";
    [SerializeField] private bool advanced;
    private readonly List<Object>[] catalog =
    {
        new List<Object>(), new List<Object>(), new List<Object>(), new List<Object>()
    };
    private Vector2 listScroll, detailScroll;
    private SerializedObject data;
    private ReorderableList groups;
    private readonly List<string> errors = new List<string>();

    [MenuItem("胞卫战/策划配置", false, 100)]
    public static void Open()
    {
        PlanningConfigWindow window = GetWindow<PlanningConfigWindow>();
        window.titleContent = new GUIContent("策划配置");
        window.minSize = new Vector2(840f, 540f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("策划配置");
        minSize = new Vector2(840f, 540f);
        tab = Mathf.Clamp(tab, 0, Tabs.Length - 1);
        RefreshCatalog();
        Undo.undoRedoPerformed += OnUndoRedo;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneClosed -= OnSceneClosed;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        ResetEditor();
    }

    private void ResetEditor()
    {
        groups = null;
        data?.Dispose();
        data = null;
    }

    private void OnUndoRedo() { ResetEditor(); Repaint(); }
    private void OnSceneOpened(Scene scene, OpenSceneMode mode) => OnUndoRedo();
    private void OnSceneClosed(Scene scene) => OnUndoRedo();
    private void OnPlayModeChanged(PlayModeStateChange state) => OnUndoRedo();
    private void OnProjectChange() { RefreshCatalog(); Repaint(); }

    private void RefreshCatalog()
    {
        foreach (List<Object> list in catalog) list.Clear();
        AddAssets<TowerDefinition>(0, "t:TowerDefinition", "Assets/_Project/Data");
        AddAssets<EnemyDefinition>(1, "t:EnemyDefinition", "Assets/_Project/Data");
        AddAssets<SceneAsset>(2, "t:Scene", "Assets/_Project/Scenes");
        AddAssets<WaveDefinition>(3, "t:WaveDefinition", "Assets/_Project/Data");
        foreach (List<Object> list in catalog)
            list.Sort((left, right) => string.Compare(AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right), StringComparison.OrdinalIgnoreCase));
        catalog[2].Sort((left, right) =>
        {
            int order = LevelOrder(left).CompareTo(LevelOrder(right));
            return order != 0 ? order : string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
        });
        if (selected == null || !MatchesTab(selected))
            selected = catalog[tab].Count > 0 ? catalog[tab][0] : null;
        ResetEditor();
    }

    private void AddAssets<T>(int index, string filter, string folder) where T : Object
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (typeof(T) == typeof(SceneAsset) &&
                !Path.GetFileNameWithoutExtension(path).StartsWith("OpenField", StringComparison.OrdinalIgnoreCase) &&
                !Path.GetFileNameWithoutExtension(path).StartsWith("Chapter02", StringComparison.OrdinalIgnoreCase) &&
                !Path.GetFileNameWithoutExtension(path).StartsWith("Chapter03", StringComparison.OrdinalIgnoreCase))
                continue;
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) catalog[index].Add(asset);
        }
    }

    private bool MatchesTab(Object asset) => tab == 0 ? asset is TowerDefinition :
        tab == 1 ? asset is EnemyDefinition : tab == 2 ? asset is SceneAsset : asset is WaveDefinition;

    private static int LevelOrder(Object asset) => asset.name == "OpenFieldPrototype" ? 0 :
        asset.name == "OpenFieldLevel02" ? 1 : asset.name == "OpenFieldLevel03" ? 2 :
        asset.name == "OpenFieldLevel04" ? 3 : asset.name == "OpenFieldLevel05" ? 4 : 5;

    private void Choose(Object asset, int page)
    {
        selected = asset;
        tab = page;
        ResetEditor();
        detailScroll = Vector2.zero;
        GUI.FocusControl(null);
    }

    private void OnGUI()
    {
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            int next = GUILayout.Toolbar(tab, Tabs, EditorStyles.toolbarButton);
            if (next != tab)
            {
                Choose(catalog[next].Count > 0 ? catalog[next][0] : null, next);
                search = "";
            }
            if (GUILayout.Button("刷新列表", EditorStyles.toolbarButton, GUILayout.Width(76f))) RefreshCatalog();
            using (new EditorGUI.DisabledScope(playing))
            {
                if (GUILayout.Button("撤销", EditorStyles.toolbarButton, GUILayout.Width(48f))) Undo.PerformUndo();
                if (GUILayout.Button("重做", EditorStyles.toolbarButton, GUILayout.Width(48f))) Undo.PerformRedo();
            }
        }

        if (playing) EditorGUILayout.HelpBox("请退出运行模式后修改配置。", MessageType.Info);
        using (new EditorGUI.DisabledScope(playing))
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawCatalog();
            using (new EditorGUILayout.VerticalScope())
            {
                if (selected == null)
                {
                    EditorGUILayout.HelpBox("当前分类没有配置资源。", MessageType.Info);
                    return;
                }
                DrawHeader();
                using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
                {
                    detailScroll = scroll.scrollPosition;
                    float oldWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 182f;
                    try { DrawDetail(); }
                    finally { EditorGUIUtility.labelWidth = oldWidth; }
                }
            }
        }
    }

    private void DrawCatalog()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(238f)))
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarTextField);
            using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll, GUI.skin.box))
            {
                listScroll = scroll.scrollPosition;
                foreach (Object asset in catalog[tab])
                {
                    if (asset == null) continue;
                    string path = AssetDatabase.GetAssetPath(asset);
                    string caption = Caption(asset);
                    if (!string.IsNullOrEmpty(search) &&
                        (caption + path).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    bool active = asset == selected;
                    if (GUILayout.Toggle(active, new GUIContent(caption, path), "Button") && !active)
                        Choose(asset, tab);
                }
            }
        }
    }

    private static string Caption(Object asset)
    {
        if (asset is TowerDefinition tower) return tower.DisplayName + "\n" + tower.name;
        if (asset is EnemyDefinition enemy) return enemy.DisplayName + "\n" + enemy.name;
        if (asset is SceneAsset)
        {
            string label = asset.name == "OpenFieldPrototype" ? "第一关" :
                asset.name == "OpenFieldLevel02" ? "第二关" :
                asset.name == "OpenFieldLevel03" ? "第三关" :
                asset.name == "OpenFieldLevel04" ? "第四关" :
                asset.name == "OpenFieldLevel05" ? "第五关" : "开放关卡";
            return label + "\n" + asset.name;
        }
        return asset.name;
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField(Caption(selected).Replace("\n", " · "), EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selected), EditorStyles.miniLabel,
            GUILayout.Height(20f));
        EditorGUILayout.HelpBox(selected is SceneAsset
            ? "场景参数只修改明确打开的这张关卡。共享波次顺序资源会影响引用它的关卡。"
            : "这是共享配置，修改会影响所有引用它的关卡。修改即时应用到编辑会话，点击保存才写入磁盘；Ctrl+Z 可撤销。",
            MessageType.Info);
    }

    private SerializedObject Edit(Object target)
    {
        if (data == null || data.targetObject != target)
        {
            ResetEditor();
            data = new SerializedObject(target);
        }
        data.Update();
        return data;
    }

    private void DrawDetail()
    {
        if (selected is SceneAsset scene) { DrawLevel(scene); return; }
        SerializedObject current = Edit(selected);
        EditorGUI.BeginChangeCheck();
        if (selected is TowerDefinition) DrawTower(current);
        else if (selected is EnemyDefinition) DrawEnemy(current);
        else DrawWave(current);
        if (EditorGUI.EndChangeCheck() && current.hasModifiedProperties)
        {
            if (selected is WaveDefinition) SyncWaveSummary(current);
            Undo.SetCurrentGroupName("修改胞卫战策划配置");
            current.ApplyModifiedProperties();
        }
        DrawValidation(selected);
        DrawSave(selected, null, null);
        if (selected is TowerDefinition tower) DrawPrefabLink(tower.Prefab);
        if (selected is EnemyDefinition enemy) DrawPrefabLink(enemy.EnemyPrefab);
    }

    private void DrawTower(SerializedObject current)
    {
        Section("基础战斗");
        Field(current, "displayName", "界面名称");
        Field(current, "buildCost", "建造花费（ATP）");
        Field(current, "damage", "单次基础伤害", "抗体和减速等辅助策略可以为 0。");
        Field(current, "attackInterval", "攻击间隔（秒）");
        Field(current, "attackRange", "攻击射程");
        SerializedProperty mode = current.FindProperty("targetingMode");
        mode.intValue = EditorGUILayout.Popup("选敌方式", mode.intValue,
            new[] { "离塔最近", "距核心剩余路程最短" });
        Section("承伤与拦截");
        Field(current, "maxHealth", "细胞最大生命");
        Field(current, "interceptsAllEnemies", "拦截全部经过的敌人",
            "壁垒在原路接敌并承伤，破裂后放行，不改变寻路。");
        Field(current, "interceptCapacity", "普通细胞接敌容量", "开启全部拦截时不使用人数上限。");
        EditorGUILayout.HelpBox("壁垒在原路接敌承伤，建造由 ATP、地形和占位决定。", MessageType.None);
        advanced = EditorGUILayout.Foldout(advanced, "资源引用（高级）", true);
        if (advanced)
        {
            Field(current, "prefab", "细胞预制体");
            Field(current, "buildIcon", "选择框图标");
            Field(current, "projectilePrefab", "投射物配置",
                "当前开放关卡的实际子弹模板由场景子弹池决定，此引用不自动切换模板。");
            Field(current, "placementKind", "旧版场景塔位类型");
        }
    }

    private void DrawEnemy(SerializedObject current)
    {
        Section("基础数值");
        Field(current, "displayName", "界面名称");
        Field(current, "maxHealth", "最大生命");
        Field(current, "baseMoveSpeed", "基础移动速度", "实际速度还会乘以关卡倍率、能力加速和减速倍率。");
        Field(current, "killReward", "击杀奖励（ATP）");
        Field(current, "leakDamage", "漏怪扣核心生命");
        Field(current, "contactDamage", "近战单次伤害");
        Field(current, "contactAttackInterval", "近战间隔（秒）");
        Section("防御与控制");
        Percent(current, "armorReduction", "护甲减伤（%）");
        Field(current, "armorBreakDuration", "酸性破壳持续（秒）");
        Field(current, "initialShield", "初始护盾");
        Field(current, "complementSensitive", "补体敏感（裂解伤害×1.5）");
        Percent(current, "slowEffectiveness", "减速生效比例（%）");
        Percent(current, "stunEffectiveness", "停顿生效比例（%）");
        Field(current, "stunRecoveryDuration", "停顿恢复窗口（秒）");
        EditorGUILayout.HelpBox("控制生效比例 50%：40% 减速实际为 20%，0.2 秒停顿实际为 0.1 秒。", MessageType.None);
        Section("身份与吞噬");
        Field(current, "antigenId", "抗原类别");
        Field(current, "isBoss", "属于 Boss");
        Field(current, "engulfImmune", "免疫吞噬");
        if (current.FindProperty("isBoss").boolValue)
            EditorGUILayout.HelpBox("Boss 始终不可吞噬；其阶段与能力需要匹配的 Boss 预制体。", MessageType.None);
        advanced = EditorGUILayout.Foldout(advanced, "资源引用（高级）", true);
        if (advanced) Field(current, "enemyPrefab", "敌人预制体");
    }

    private void DrawWave(SerializedObject current)
    {
        Section("出怪节奏");
        Field(current, "spawnInterval", "逐只生成间隔（秒）");
        Field(current, "burstInterval", "批次间隔（秒）");
        SerializedProperty array = current.FindProperty("groups");
        if (array.arraySize == 0)
        {
            Section("单类型波次");
            Field(current, "enemy", "敌人");
            Field(current, "enemyCount", "数量");
            Field(current, "burstSize", "每批数量");
            EditorGUILayout.HelpBox("下方添加第一组会保留当前单类型的敌人、数量和批次设置。", MessageType.None);
        }
        else
        {
            long total = 0;
            for (int i = 0; i < array.arraySize; i++)
                total += array.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue;
            EditorGUILayout.LabelField($"计划生成 {total} 只（不含 Boss 召唤物）", EditorStyles.boldLabel);
        }
        if (groups == null) groups = CreateGroups(current, array);
        groups.DoLayoutList();
        EditorGUILayout.HelpBox("按列表顺序生成。每批数量控制组内分批，组结束后等待控制下一组何时开始；Boss 另有既定的 1 秒入场预告。删除全部分组后使用上方单类型配置。", MessageType.None);
    }

    private static ReorderableList CreateGroups(SerializedObject current, SerializedProperty array)
    {
        var list = new ReorderableList(current, array, true, true, true, true);
        float line = EditorGUIUtility.singleLineHeight + 4f;
        list.elementHeight = line * 6f + 6f;
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "出怪分组（拖动左侧调整顺序）");
        list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty group = array.GetArrayElementAtIndex(index);
            string[] fields = { "enemy", "count", "burstSize", "afterDelay", "useExplicitEntrance", "entranceIndex" };
            string[] labels = { $"第 {index + 1} 组 · 敌人", "数量", "每批数量", "组结束后等待（秒）", "指定入口（关闭则轮换）", "入口下标（0为第一入口）" };
            for (int i = 0; i < fields.Length; i++)
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + 3f + i * line, rect.width,
                    EditorGUIUtility.singleLineHeight), group.FindPropertyRelative(fields[i]),
                    new GUIContent(labels[i]));
        };
        list.onAddCallback = value =>
        {
            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            SerializedProperty group = array.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("enemy").objectReferenceValue = current.FindProperty("enemy").objectReferenceValue;
            group.FindPropertyRelative("count").intValue = index == 0 ? current.FindProperty("enemyCount").intValue : 6;
            group.FindPropertyRelative("burstSize").intValue = index == 0
                ? Mathf.Max(1, current.FindProperty("burstSize").intValue) : 6;
            group.FindPropertyRelative("afterDelay").floatValue = current.FindProperty("burstInterval").floatValue;
            group.FindPropertyRelative("useExplicitEntrance").boolValue = false;
            group.FindPropertyRelative("entranceIndex").intValue = -1;
            value.index = index;
            SyncWaveSummary(current);
            GUI.changed = true;
        };
        list.onRemoveCallback = value =>
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(value);
            SyncWaveSummary(current);
            GUI.changed = true;
        };
        list.onReorderCallback = value =>
        {
            SyncWaveSummary(current);
            GUI.changed = true;
        };
        return list;
    }

    private static void SyncWaveSummary(SerializedObject current)
    {
        SerializedProperty array = current.FindProperty("groups");
        if (array.arraySize == 0) return;
        long total = 0;
        for (int i = 0; i < array.arraySize; i++)
            total += array.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue;
        if (total > 0 && total <= int.MaxValue) current.FindProperty("enemyCount").intValue = (int)total;
        current.FindProperty("enemy").objectReferenceValue =
            array.GetArrayElementAtIndex(0).FindPropertyRelative("enemy").objectReferenceValue;
    }

    private void DrawLevel(SceneAsset asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        Scene scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorGUILayout.HelpBox("先打开这张关卡，才能读取和修改它的场景参数。打开前 Unity 会询问是否保存当前未保存场景。", MessageType.Info);
            if (GUILayout.Button("打开该关卡"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ResetEditor();
                }
                GUIUtility.ExitGUI();
            }
            return;
        }
        OpenFieldPrototypeController controller = FindController(scene);
        if (controller == null)
        {
            EditorGUILayout.HelpBox("这张场景需要且只能有一个开放地图控制器。请检查场景层级。", MessageType.Error);
            return;
        }
        SerializedObject current = Edit(controller);
        EditorGUI.BeginChangeCheck();
        Section("资源与防御");
        Field(current, "initialATP", "开局 ATP");
        Field(current, "betweenRoundATP", "轮间奖励 ATP");
        Field(current, "autoWaveDelay", "出兵后自动开波等待（秒）");
        Field(current, "initialCoreLife", "核心初始生命");
        Field(current, "sellRefundPercent", "出售返还比例（%）");
        Field(current, "enemySpeedMultiplier", "敌人速度倍率");
        Field(current, "signalRadiusCells", "抗原共享半径（格）");
        Field(current, "showDebugGrid", "显示调试格子");
        Section("本关可建细胞");
        Field(current, "towerDefinitions", "选择框列表", "列表顺序决定选择框顺序，前六项对应数字快捷键 1–6。");
        Section("本关波次");
        Field(current, "waveSequence", "波次顺序资源");
        if (EditorGUI.EndChangeCheck() && current.hasModifiedProperties)
        {
            Undo.SetCurrentGroupName("修改胞卫战关卡配置");
            current.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
        }
        WaveSequenceDefinition sequence = current.FindProperty("waveSequence").objectReferenceValue as WaveSequenceDefinition;
        if (sequence != null) DrawSequence(sequence);
        DrawValidation(controller);
        DrawSave(controller, scene, sequence);
    }

    private void DrawSequence(WaveSequenceDefinition sequence)
    {
        EditorGUILayout.LabelField("顺序资源：" + AssetDatabase.GetAssetPath(sequence), EditorStyles.miniLabel);
        using (var sequenceData = new SerializedObject(sequence))
        {
            sequenceData.Update();
            SerializedProperty waves = sequenceData.FindProperty("waves");
            EditorGUI.BeginChangeCheck();
            if (waves.arraySize < 1)
                EditorGUILayout.HelpBox("关卡至少需要一波。可展开下方列表修正。", MessageType.Error);
            EditorGUILayout.PropertyField(waves, new GUIContent("波次列表"), true);
            if (EditorGUI.EndChangeCheck() && sequenceData.hasModifiedProperties)
            {
                Undo.SetCurrentGroupName("修改胞卫战波次顺序");
                sequenceData.ApplyModifiedProperties();
            }
        }
        for (int i = 0; i < sequence.WaveCount; i++)
        {
            WaveDefinition wave;
            try { wave = sequence.GetWave(i); } catch (Exception) { continue; }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"第 {i + 1} 轮 · {wave.name}", EditorStyles.miniLabel);
                if (GUILayout.Button("编辑这波", GUILayout.Width(86f)))
                {
                    Choose(wave, 3);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private static OpenFieldPrototypeController FindController(Scene scene)
    {
        OpenFieldPrototypeController result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (OpenFieldPrototypeController found in root.GetComponentsInChildren<OpenFieldPrototypeController>(true))
            {
                if (result != null) return null;
                result = found;
            }
        return result;
    }

    private void DrawValidation(Object target)
    {
        errors.Clear();
        PlanningConfigValidation.Collect(target, errors);
        foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
    }

    private void DrawSave(Object target, Scene? scene, WaveSequenceDefinition sequence)
    {
        bool dirty = scene.HasValue ? scene.Value.isDirty || (sequence != null && EditorUtility.IsDirty(sequence))
            : EditorUtility.IsDirty(target);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(dirty ? "有尚未保存的修改" : "当前配置已保存", EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(errors.Count > 0 || !dirty))
                if (GUILayout.Button(scene.HasValue ? "保存关卡与波次顺序" : "保存当前配置", GUILayout.Height(30f)))
                {
                    GUI.FocusControl(null);
                    errors.Clear();
                    PlanningConfigValidation.Collect(target, errors);
                    if (errors.Count == 0)
                    {
                        bool saved = !scene.HasValue || EditorSceneManager.SaveScene(scene.Value);
                        if (saved)
                        {
                            if (scene.HasValue)
                            {
                                if (sequence != null) AssetDatabase.SaveAssetIfDirty(sequence);
                            }
                            else AssetDatabase.SaveAssetIfDirty(target);
                            bool stillDirty = scene.HasValue ? scene.Value.isDirty ||
                                (sequence != null && EditorUtility.IsDirty(sequence)) : EditorUtility.IsDirty(target);
                            ShowNotification(new GUIContent(stillDirty ? "仍有未保存修改，请查看 Console" : "已保存当前配置"));
                        }
                        else ShowNotification(new GUIContent("关卡未保存，请查看 Console"));
                    }
                }
            if (GUILayout.Button("在项目中定位", GUILayout.Width(120f), GUILayout.Height(30f)))
                EditorGUIUtility.PingObject(selected);
        }
    }

    private static void DrawPrefabLink(Component prefab)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox("此页编辑基础配置。减速、酸区、连锁、Boss 阶段与召唤等特殊效果，保存在预制体对应组件中。", MessageType.None);
        using (new EditorGUI.DisabledScope(prefab == null))
            if (GUILayout.Button("打开预制体查看特殊效果")) AssetDatabase.OpenAsset(prefab.gameObject);
    }

    private static void Section(string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private static void Field(SerializedObject current, string name, string label, string tooltip = null)
    {
        SerializedProperty property = current.FindProperty(name);
        if (property == null)
        {
            EditorGUILayout.HelpBox("未找到配置字段：" + name, MessageType.Error);
            return;
        }
        EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip), true);
    }

    private static void Percent(SerializedObject current, string name, string label)
    {
        SerializedProperty property = current.FindProperty(name);
        EditorGUI.BeginChangeCheck();
        float value = EditorGUILayout.FloatField(label, property.floatValue * 100f);
        if (EditorGUI.EndChangeCheck()) property.floatValue = value / 100f;
    }
}

