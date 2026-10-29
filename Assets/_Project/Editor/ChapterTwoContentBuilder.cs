using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Run in the validation project first. Existing .meta GUIDs and first-chapter balance stay intact.
public static class ChapterTwoContentBuilder
{
    const string Data = "Assets/_Project/Data/Chapter02";
    const string Prefabs = "Assets/_Project/Prefabs/Chapter02";
    static readonly string[] Names = { "凝血缺口", "厚膜岔口", "长廊窗口", "生物膜边缘" };
    static readonly string[] Intros = {
        "血小板修补塔只修补壁垒。每座壁垒总共可修补300生命，多塔共享上限。保留后备防线。",
        "重甲有外壳减伤。酸化炮可破壳，墙负责留住目标；两路需要各自的火力。",
        "补体持续组装4秒，标记时速度加倍。目标离开射程会失去进度。第4波注意次入口。",
        "生物膜核心会支援附近敌人。前方削减护卫，后方集中火力；第7波迎战首领。"
    };
    static void Set(Object obj, string name, object value)
    {
        var serialized = new SerializedObject(obj);var property = serialized.FindProperty(name);
        if (property == null) throw new InvalidOperationException(obj.name + "." + name);
        if (value is Object reference) property.objectReferenceValue = reference;
        else if (value == null) property.objectReferenceValue = null;
        else if (value is int integer) property.intValue = integer;
        else if (value is float number) property.floatValue = number;
        else if (value is bool boolean) property.boolValue = boolean;
        else if (value is string text) property.stringValue = text;
        else if (value is Vector2Int cell) property.vector2IntValue = cell;
        else throw new ArgumentException(name);
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj);
    }
    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path)
        ?? throw new InvalidOperationException("Missing " + path);
    static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Data + "/" + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset, path); }
        return asset;
    }
    static EnemyDefinition Enemy(string name) => Load<EnemyDefinition>("Assets/_Project/Data/Enemies/" + name + "Definition.asset");
    static WaveEnemyGroup Group(EnemyDefinition enemy, int count, int entry = 0, float pause = 2f, int burst = 3)
        => new WaveEnemyGroup { enemy = enemy, count = count, useExplicitEntrance = entry >= 0, entranceIndex = entry, burstSize = burst, afterDelay = pause };
    static WaveEnemyGroup[] Groups(params WaveEnemyGroup[] groups) => groups;
    static TowerDefinition Tower(string name, string display, string artSource, Type behaviour, int cost, float range, float damage, float interval, Color color)
    {
        var root = new GameObject(name);
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = Load<GameObject>("Assets/_Project/Prefabs/Towers/" + artSource + ".prefab").GetComponentInChildren<SpriteRenderer>().sprite;
        string dedicatedName = name == "PlateletTower" ? "01_血小板修补塔" : name == "ComplementTower" ? "02_补体裂解塔" : null;
        Sprite dedicated = dedicatedName == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Art/Sprites/细胞绘制/第二章新塔_2026-10-29_v1/战斗与图标/" + dedicatedName + ".png");
        if (dedicated != null) { renderer.sprite = dedicated;color = Color.white; }
        renderer.color = color;renderer.sortingOrder = 10;
        root.transform.localScale = Vector3.one * (.9f / Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y));
        var controller = root.AddComponent<TowerController>();Set(controller, "enemyLayerMask", LayerMask.GetMask("Enemy"));
        root.AddComponent(behaviour);
        ChapterTwoActionAnimation action = root.GetComponent<ChapterTwoActionAnimation>();
        string animations = "Assets/_Project/Art/Sprites/细胞绘制/第二章新塔动画_2026-10-29_v1/";
        string actionFolder = name == "PlateletTower" ? "血小板修补_动作" : "补体裂解_动作";
        string contactFolder = name == "PlateletTower" ? "修补接触_特效" : "裂解命中_特效";
        if (action != null && Directory.Exists(animations + actionFolder))
        {
            Sprite[] poses = Enumerable.Range(1, 10).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(animations + actionFolder + "/frames/frame_" + i.ToString("00") + ".png")).ToArray();
            Sprite[] contacts = Enumerable.Range(1, 6).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(animations + contactFolder + "/frames/frame_" + i.ToString("00") + ".png")).ToArray();
            if (poses.All(s => s != null) && contacts.All(s => s != null)) action.Configure(poses, contacts, name == "ComplementTower");
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab");Object.DestroyImmediate(root);
        var definition = Asset<TowerDefinition>(name + "Definition");
        Set(definition, "displayName", display);Set(definition, "prefab", prefab.GetComponent<TowerController>());
        Set(definition, "buildIcon", renderer != null ? renderer.sprite : prefab.GetComponent<SpriteRenderer>().sprite);
        Set(definition, "buildCost", cost);Set(definition, "attackRange", range);Set(definition, "damage", damage);
        Set(definition, "attackInterval", interval);Set(definition, "maxHealth", 100f);Set(definition, "interceptCapacity", 1);
        return definition;
    }
    static void Cells(Object obj, string field, IEnumerable<Vector2Int> cells)
    {
        var so = new SerializedObject(obj);var array = so.FindProperty(field);var values = cells.ToArray();array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).vector2IntValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static HashSet<Vector2Int> Blocks(Vector2Int[] entrances, int level)
    {
        var blocked = new HashSet<Vector2Int>();
        for (int x = 0; x < 15; x++) { blocked.Add(new Vector2Int(x, 0));blocked.Add(new Vector2Int(x, 10)); }
        for (int y = 0; y < 11; y++) { blocked.Add(new Vector2Int(0, y));blocked.Add(new Vector2Int(14, y)); }
        Action<int,int,int,int> rect = (x0,y0,x1,y1) => { for (int x=x0;x<=x1;x++) for (int y=y0;y<=y1;y++) blocked.Add(new Vector2Int(x,y)); };
        // Cell footprints checked against the final PNG, rather than copying pre-drawing grayboxes.
        if (level == 0) { rect(4,4,5,8);rect(8,2,10,5); }
        if (level == 1) rect(3,4,10,6);
        if (level == 2) { rect(4,4,5,8);rect(8,2,10,5); }
        if (level == 3) { rect(3,4,5,6);rect(7,2,9,3); }
        foreach (Vector2Int entry in entrances) blocked.Remove(entry);
        return blocked;
    }
    [MenuItem("胞卫战/关卡/生成第二章试玩内容")]
    public static void Build()
    {
        Directory.CreateDirectory(Data);Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        var platelet = Tower("PlateletTower", "血小板修补塔", "InterceptorTower", typeof(PlateletRepairBehaviour), 50, 2.6f, 0, 1f, new Color(1f,.83f,.55f));
        var complement = Tower("ComplementTower", "补体裂解塔", "Tower 1", typeof(ComplementAttackBehaviour), 75, 3f, 240f, 1f, new Color(.5f,1f,1f));
        var eliteObject = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/_Project/Prefabs/Enemies/Staphylococcus.prefab"));
        eliteObject.name = "ComplementDiplococcus";
        foreach (Animator animator in eliteObject.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(animator);
        eliteObject.GetComponentInChildren<SpriteRenderer>().color = new Color(.45f,.9f,1f);
        Set(eliteObject.GetComponent<EnemyController>(), "playCoreAttackAnimation", false);
        var elitePrefab = PrefabUtility.SaveAsPrefabAsset(eliteObject, Prefabs + "/ComplementDiplococcus.prefab");Object.DestroyImmediate(eliteObject);
        var elite = Asset<EnemyDefinition>("ComplementDiplococcusDefinition");
        Set(elite,"enemyPrefab",elitePrefab.GetComponent<EnemyController>());Set(elite,"displayName","补体敏感双球菌");
        Set(elite,"maxHealth",420f);Set(elite,"baseMoveSpeed",1.05f);Set(elite,"killReward",25);Set(elite,"leakDamage",3);
        Set(elite,"antigenId",(int)AntigenId.ComplementDiplococcus);Set(elite,"complementSensitive",true);
        Set(elite,"contactDamage",12f);Set(elite,"contactAttackInterval",1f);
        var basic = Enemy("Staphylococcus");var fast = Enemy("Flagellate");var armor = Enemy("ArmoredBacillus");var boss = Enemy("MembraneBoss");
        WaveEnemyGroup[][][] chapters = {
            new[]{Groups(Group(basic,8)),Groups(Group(basic,8,0,3),Group(basic,8)),Groups(Group(basic,24)),Groups(Group(basic,12),Group(fast,5)),Groups(Group(basic,25),Group(fast,8))},
            new[]{Groups(Group(armor,3,0,3,1)),Groups(Group(armor,5,0,3,1)),Groups(Group(basic,16),Group(armor,5)),Groups(Group(fast,8,1),Group(basic,8,0)),Groups(Group(armor,6,0),Group(basic,18,1),Group(armor,4,1))},
            new[]{Groups(Group(elite,1),Group(basic,6)),Groups(Group(elite,3,0,4,1)),Groups(Group(basic,12),Group(elite,3)),Groups(Group(fast,8,1)),Groups(Group(elite,5),Group(basic,14,1)),Groups(Group(basic,16,1),Group(elite,7,0),Group(fast,5,1))},
            new[]{Groups(Group(basic,16)),Groups(Group(armor,4),Group(basic,12)),Groups(Group(fast,8,1)),Groups(Group(elite,4)),Groups(Group(basic,14,1),Group(armor,5),Group(elite,2)),Groups(Group(basic,12)),Groups(Group(basic,8,0,4),Group(boss,1,0,4,1),Group(armor,4),Group(basic,10,1))}
        };
        var catalog = Load<LevelSelectionArtwork>("Assets/_Project/Resources/LevelSelectionArtwork.asset");
        var towers = new[]{"TowerDefinition","TowerDefinition 1","InterceptorTowerDefinition","FibrinTowerDefinition","AcidTowerDefinition","NeuralPulseTowerDefinition"}
            .Select(n=>Load<TowerDefinition>("Assets/_Project/Data/Towers/"+n+".asset")).ToList();
        var buildScenes = EditorBuildSettings.scenes.ToList();
        for (int level=0;level<4;level++)
        {
            catalog = Load<LevelSelectionArtwork>("Assets/_Project/Resources/LevelSelectionArtwork.asset");
            EditorUtility.SetDirty(catalog);
            string sceneName = "Chapter02Level"+(level+1).ToString("00");
            var sequence = Asset<WaveSequenceDefinition>(sceneName+"Sequence");
            var waves = new List<WaveDefinition>();
            for (int w=0;w<chapters[level].Length;w++)
            {
                var wave = Asset<WaveDefinition>(sceneName+"Wave"+(w+1).ToString("00"));
                Set(wave,"enemy",basic);Set(wave,"enemyCount",1);Set(wave,"spawnInterval",.65f);Set(wave,"burstInterval",2.5f);
                var so = new SerializedObject(wave);var groups = so.FindProperty("groups");groups.arraySize = chapters[level][w].Length;
                for(int g=0;g<groups.arraySize;g++)
                {
                    var input=chapters[level][w][g];var value=groups.GetArrayElementAtIndex(g);
                    value.FindPropertyRelative("enemy").objectReferenceValue=input.enemy;value.FindPropertyRelative("count").intValue=input.count;
                    value.FindPropertyRelative("burstSize").intValue=input.burstSize;value.FindPropertyRelative("afterDelay").floatValue=input.afterDelay;
                    value.FindPropertyRelative("entranceIndex").intValue=input.entranceIndex;
                    value.FindPropertyRelative("useExplicitEntrance").boolValue=input.useExplicitEntrance;
                }
                so.ApplyModifiedPropertiesWithoutUndo();wave.Validate();waves.Add(wave);
            }
            var sequenceSo=new SerializedObject(sequence);var waveArray=sequenceSo.FindProperty("waves");waveArray.arraySize=waves.Count;
            for(int w=0;w<waves.Count;w++)waveArray.GetArrayElementAtIndex(w).objectReferenceValue=waves[w];sequenceSo.ApplyModifiedPropertiesWithoutUndo();
            string path="Assets/_Project/Scenes/"+sceneName+".unity";
            if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/_Project/Scenes/OpenFieldLevel03.unity",path);
            Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var owner=Object.FindObjectOfType<OpenFieldPrototypeController>();
            Set(owner,"mapArtwork",catalog.chapters[1].levels[level].map);Set(owner,"coreCell",new Vector2Int(12,5));
            Vector2Int[] entrances = level==0 ? new[]{new Vector2Int(0,5)} : level==1 ? new[]{new Vector2Int(0,8),new Vector2Int(0,2)}
                : level==2 ? new[]{new Vector2Int(0,5),new Vector2Int(0,3)} : new[]{new Vector2Int(0,8),new Vector2Int(5,0)};
            Cells(owner,"spawnCells",entrances);Cells(owner,"blockedCells",Blocks(entrances,level));
            var choices=towers.Concat(new[]{platelet}).Concat(level>=2 ? new[]{complement}:new TowerDefinition[0]).ToArray();
            var ownerSo=new SerializedObject(owner);var array=ownerSo.FindProperty("towerDefinitions");array.arraySize=choices.Length;
            for(int i=0;i<choices.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=choices[i];ownerSo.ApplyModifiedPropertiesWithoutUndo();
            Set(owner,"waveSequence",sequence);Set(owner,"initialATP",level==0?200:250);Set(owner,"betweenRoundATP",35);
            Set(owner,"initialCoreLife",20);Set(owner,"enemySpeedMultiplier",.6375f);Set(owner,"autoWaveDelay",10f);
            Set(owner,"enableFirstLevelTutorial",false);Set(owner,"nextSceneName",level<3 ? "Chapter02Level"+(level+2).ToString("00") : "");
            var hint=owner.GetComponent<ChapterTwoBriefing>()??owner.gameObject.AddComponent<ChapterTwoBriefing>();Set(hint,"introduction",Intros[level]);
            var grid=new OpenFieldGridModel(15,11,new Vector2Int(12,5),entrances,Blocks(entrances,level).ToArray());
            var route=new List<Vector2Int>();foreach(var entry in entrances)if(!grid.TryGetPath(entry,route))throw new InvalidOperationException("Unreachable "+sceneName);
            EditorSceneManager.SaveScene(scene);catalog.chapters[1].levels[level].sceneName=sceneName;
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            if(!buildScenes.Any(s=>s.path==path))buildScenes.Add(new EditorBuildSettingsScene(path,true));
        }
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/OpenFieldLevel05.unity",OpenSceneMode.Single);
        Set(Object.FindObjectOfType<OpenFieldPrototypeController>(),"nextSceneName","Chapter02Level01");EditorSceneManager.SaveOpenScenes();
        EditorBuildSettings.scenes=buildScenes.ToArray();AssetDatabase.SaveAssets();
        Debug.Log("CHAPTER_TWO_CONTENT_READY: 4 levels / 5,5,6,7 waves / repair and complement / chapter progression");
    }
}
