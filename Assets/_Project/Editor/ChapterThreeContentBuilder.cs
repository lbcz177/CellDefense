using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ChapterThreeContentBuilder
{
    const string Data="Assets/_Project/Data/Chapter03";
    const string Prefabs="Assets/_Project/Prefabs/Chapter03";
    static readonly string[] Intro={
        "炎症源进入边框内才会积累。20起伤害提高15%，70起预警4秒后伤害区内细胞。清源后会恢复。",
        "辅助T支持射程内最近的一座抗体塔，标记延长2秒。分路布置仍要保留火力。",
        "Treg只调节附近一个炎症区，多塔不叠加。不治疗细胞，清源和后方布置同样有效。注意右侧入口。",
        "左右炎症区独立积累与恢复。错峰分配火力与调节，末波有真菌首领和孢子。"
    };
    static void Set(Object obj,string field,object value)
    {
        var so=new SerializedObject(obj);var p=so.FindProperty(field);
        if(p==null)throw new InvalidOperationException(field);
        if(value is Object reference)p.objectReferenceValue=reference;
        else if(value is bool flag)p.boolValue=flag;
        else if(value is int n)p.intValue=n;
        else if(value is float number)p.floatValue=number;
        else if(value is string text)p.stringValue=text;
        else if(value is Vector2Int cell)p.vector2IntValue=cell;
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj);
    }
    static T Load<T>(string p)where T:Object=>AssetDatabase.LoadAssetAtPath<T>(p)??throw new InvalidOperationException(p);
    static T Asset<T>(string name)where T:ScriptableObject
    {
        string p=Data+"/"+name+".asset";var a=AssetDatabase.LoadAssetAtPath<T>(p);
        if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,p);}return a;
    }
    static EnemyDefinition Enemy(string n)=>Load<EnemyDefinition>("Assets/_Project/Data/Enemies/"+n+"Definition.asset");
    static WaveEnemyGroup G(EnemyDefinition e,int n,int entrance=0,float delay=3f)=>new WaveEnemyGroup
        {enemy=e,count=n,useExplicitEntrance=true,entranceIndex=entrance,burstSize=3,afterDelay=delay};
    static WaveEnemyGroup[] W(params WaveEnemyGroup[] groups)=>groups;
    static TowerDefinition Support(string name,bool regulatory)
    {
        var go=new GameObject(name);var render=go.AddComponent<SpriteRenderer>();
        render.sprite=Load<GameObject>("Assets/_Project/Prefabs/Towers/"+(regulatory?"NeuralPulseTower":"Tower 1")+".prefab").GetComponentInChildren<SpriteRenderer>().sprite;
        render.color=regulatory?new Color(.7f,1f,.65f):new Color(.9f,.7f,1f);render.sortingOrder=10;
        go.transform.localScale=Vector3.one*(.9f/Mathf.Max(render.sprite.bounds.size.x,render.sprite.bounds.size.y));
        var tower=go.AddComponent<TowerController>();Set(tower,"enemyLayerMask",LayerMask.GetMask("Enemy"));
        var behavior=go.AddComponent<ImmuneSupportBehaviour>();Set(behavior,"regulatory",regulatory);
        var prefab=PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/"+name+".prefab");Object.DestroyImmediate(go);
        var def=Asset<TowerDefinition>(name+"Definition");Set(def,"displayName",regulatory?"调节性T细胞":"辅助性T细胞");
        Set(def,"prefab",prefab.GetComponent<TowerController>());Set(def,"buildIcon",prefab.GetComponent<SpriteRenderer>().sprite);
        Set(def,"damage",0f);Set(def,"attackRange",2.6f);Set(def,"attackInterval",1f);
        Set(def,"maxHealth",100f);Set(def,"buildCost",60);return def;
    }
    static void Cells(Object obj,string field,IEnumerable<Vector2Int> values)
    {
        var so=new SerializedObject(obj);var p=so.FindProperty(field);var array=values.ToArray();p.arraySize=array.Length;
        for(int i=0;i<array.Length;i++)p.GetArrayElementAtIndex(i).vector2IntValue=array[i];so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Vector2Int[] Blocks(int level,Vector2Int[] entries)
    {
        var cells=new HashSet<Vector2Int>();
        for(int x=0;x<15;x++){cells.Add(new Vector2Int(x,0));cells.Add(new Vector2Int(x,10));}
        for(int y=0;y<11;y++){cells.Add(new Vector2Int(0,y));cells.Add(new Vector2Int(14,y));}
        Action<int,int,int,int> rect=(a,b,c,d)=>{for(int x=a;x<=c;x++)for(int y=b;y<=d;y++)cells.Add(new Vector2Int(x,y));};
        if(level==0){rect(5,7,7,8);rect(8,3,9,4);}
        if(level==1){rect(3,4,8,6);rect(10,7,12,8);rect(10,2,12,4);}
        if(level==2){rect(4,6,6,8);rect(4,2,6,4);rect(8,2,10,3);}
        if(level==3){rect(5,7,9,9);rect(1,1,3,5);rect(11,1,13,5);}
        foreach(var e in entries)cells.Remove(e);return cells.OrderBy(c=>c.x).ThenBy(c=>c.y).ToArray();
    }
    static void Region(SerializedProperty region,string path,string label)
    {
        var sprite=Load<Sprite>(path);var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));
        int left=texture.width,right=0,top=0,bottom=texture.height;var pixels=texture.GetPixels32();
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>0)
        {left=Mathf.Min(left,x);right=Mathf.Max(right,x+1);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y+1);}
        region.FindPropertyRelative("label").stringValue=label;
        region.FindPropertyRelative("bounds").rectValue=Rect.MinMaxRect(left*15f/texture.width,bottom*11f/texture.height,right*15f/texture.width,top*11f/texture.height);
        region.FindPropertyRelative("overlay").objectReferenceValue=sprite;Object.DestroyImmediate(texture);
    }
    [MenuItem("胞卫战/关卡/生成第三章试玩内容")]
    public static void Build()
    {
        Directory.CreateDirectory(Data);Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        var helper=Support("HelperT",false);var regulator=Support("RegulatoryT",true);
        var sourceObject=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/_Project/Prefabs/Enemies/Staphylococcus.prefab"));
        sourceObject.name="InflammationSource";foreach(var a in sourceObject.GetComponentsInChildren<Animator>())Object.DestroyImmediate(a);
        Set(sourceObject.GetComponent<EnemyController>(),"playCoreAttackAnimation",false);
        sourceObject.GetComponentInChildren<SpriteRenderer>().color=new Color(1f,.6f,.35f);sourceObject.AddComponent<ChapterTwoLinkVisual>();
        var sourcePrefab=PrefabUtility.SaveAsPrefabAsset(sourceObject,Prefabs+"/InflammationSource.prefab");Object.DestroyImmediate(sourceObject);
        var source=Asset<EnemyDefinition>("InflammationSourceDefinition");Set(source,"enemyPrefab",sourcePrefab.GetComponent<EnemyController>());
        Set(source,"displayName","炎症诱导菌");Set(source,"maxHealth",180f);Set(source,"killReward",20);Set(source,"baseMoveSpeed",.8f);
        Set(source,"antigenId",(int)AntigenId.InflammationSource);Set(source,"inflammationSource",true);Set(source,"contactDamage",6f);Set(source,"contactAttackInterval",1f);
        var basic=Enemy("Staphylococcus");var fast=Enemy("Flagellate");var armored=Enemy("ArmoredBacillus");var boss=Enemy("FungalBoss");
        var elite=Load<EnemyDefinition>("Assets/_Project/Data/Chapter02/ComplementDiplococcusDefinition.asset");
        WaveEnemyGroup[][][] levels={
            new[]{W(G(basic,5),G(source,1)),W(G(source,1),G(basic,12)),W(G(source,2,0,4),G(basic,16)),W(G(basic,22))},
            new[]{W(G(basic,10)),W(G(basic,14),G(basic,8,1)),W(G(source,1),G(basic,14)),W(G(fast,8,1)),W(G(elite,3),G(basic,16,1),G(source,1))},
            new[]{W(G(source,1),G(basic,10)),W(G(basic,18),G(source,1)),W(G(source,2,0,5),G(basic,12)),W(G(basic,14,1)),W(G(source,2),G(armored,4),G(basic,12,1))},
            new[]{W(G(basic,12)),W(G(source,1,1),G(basic,12,1)),W(G(source,1),G(fast,7,1)),W(G(elite,3),G(basic,15,1)),W(G(source,2,1,5),G(basic,12)),W(G(basic,10),G(basic,10,1)),W(G(source,1),G(boss,1,0,5),G(basic,18,1))}
        };
        // Scene assets are copied through AssetDatabase; the existing eight definitions are explicit.
        var towerNames=new[]{"TowerDefinition","TowerDefinition 1","InterceptorTowerDefinition","FibrinTowerDefinition","AcidTowerDefinition","NeuralPulseTowerDefinition"};
        var common=towerNames.Select(n=>Load<TowerDefinition>("Assets/_Project/Data/Towers/"+n+".asset")).Concat(new[]{
            Load<TowerDefinition>("Assets/_Project/Data/Chapter02/PlateletTowerDefinition.asset"),Load<TowerDefinition>("Assets/_Project/Data/Chapter02/ComplementTowerDefinition.asset")}).ToArray();
        string front="Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_前两关_2026-10-26_v1/第三章/";
        string back="Assets/_Project/Art/Sprites/细胞绘制/第二三章地图_后两关_2026-10-26_v1/第三章/";
        string[][] overlays={new[]{front+"3-1_炎症区域_独立覆盖.png"},new[]{front+"3-2_炎症区域_独立覆盖.png"},
            new[]{back+"3-3_炎症区域_主前线_独立覆盖.png"},new[]{back+"3-4_炎症区域_左前线_独立覆盖.png",back+"3-4_炎症区域_右前线_独立覆盖.png"}};
        var buildScenes=EditorBuildSettings.scenes.ToList();
        for(int level=0;level<4;level++)
        {
            helper=Load<TowerDefinition>(Data+"/HelperTDefinition.asset");regulator=Load<TowerDefinition>(Data+"/RegulatoryTDefinition.asset");
            EditorUtility.SetDirty(helper);EditorUtility.SetDirty(regulator);
            var catalog=Load<LevelSelectionArtwork>("Assets/_Project/Resources/LevelSelectionArtwork.asset");EditorUtility.SetDirty(catalog);
            string name="Chapter03Level"+(level+1).ToString("00");var sequence=Asset<WaveSequenceDefinition>(name+"Sequence");
            var seq=new SerializedObject(sequence);var array=seq.FindProperty("waves");array.arraySize=levels[level].Length;
            for(int w=0;w<levels[level].Length;w++)
            {
                var wave=Asset<WaveDefinition>(name+"Wave"+(w+1).ToString("00"));Set(wave,"enemy",basic);Set(wave,"spawnInterval",.7f);Set(wave,"burstInterval",2.5f);
                var so=new SerializedObject(wave);var groups=so.FindProperty("groups");groups.arraySize=levels[level][w].Length;
                for(int g=0;g<groups.arraySize;g++)
                {
                    var input=levels[level][w][g];var p=groups.GetArrayElementAtIndex(g);
                    p.FindPropertyRelative("enemy").objectReferenceValue=input.enemy;p.FindPropertyRelative("count").intValue=input.count;
                    p.FindPropertyRelative("burstSize").intValue=input.burstSize;p.FindPropertyRelative("afterDelay").floatValue=input.afterDelay;
                    p.FindPropertyRelative("useExplicitEntrance").boolValue=true;p.FindPropertyRelative("entranceIndex").intValue=input.entranceIndex;
                }
                so.ApplyModifiedPropertiesWithoutUndo();wave.Validate();array.GetArrayElementAtIndex(w).objectReferenceValue=wave;
            }
            seq.ApplyModifiedPropertiesWithoutUndo();
            string path="Assets/_Project/Scenes/"+name+".unity";
            if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/_Project/Scenes/Chapter02Level04.unity",path);
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var owner=Object.FindObjectOfType<OpenFieldPrototypeController>();
            var entries=level==0?new[]{new Vector2Int(0,5)}:level==1?new[]{new Vector2Int(0,8),new Vector2Int(0,2)}:
                level==2?new[]{new Vector2Int(0,6),new Vector2Int(14,2)}:new[]{new Vector2Int(0,8),new Vector2Int(14,8)};
            var core=level<2?new Vector2Int(12,5):level==2?new Vector2Int(11,5):new Vector2Int(7,2);
            catalog=Load<LevelSelectionArtwork>("Assets/_Project/Resources/LevelSelectionArtwork.asset");
            Set(owner,"mapArtwork",catalog.chapters[2].levels[level].map);Set(owner,"coreCell",core);Cells(owner,"spawnCells",entries);Cells(owner,"blockedCells",Blocks(level,entries));
            Set(owner,"waveSequence",sequence);Set(owner,"initialATP",250);Set(owner,"betweenRoundATP",35);Set(owner,"initialCoreLife",20);
            Set(owner,"nextSceneName",level<3?"Chapter03Level"+(level+2).ToString("00"):"");
            helper=Load<TowerDefinition>(Data+"/HelperTDefinition.asset");regulator=Load<TowerDefinition>(Data+"/RegulatoryTDefinition.asset");
            var choices=common.Concat(level>=1?new[]{helper}:new TowerDefinition[0]).Concat(level>=2?new[]{regulator}:new TowerDefinition[0]).ToArray();
            var ownerSo=new SerializedObject(owner);var towers=ownerSo.FindProperty("towerDefinitions");towers.arraySize=choices.Length;
            for(int t=0;t<choices.Length;t++)towers.GetArrayElementAtIndex(t).objectReferenceValue=choices[t];ownerSo.ApplyModifiedPropertiesWithoutUndo();
            Set(owner.GetComponent<ChapterTwoBriefing>(),"introduction",Intro[level]);
            var system=owner.GetComponent<ChapterThreeInflammation>()??owner.gameObject.AddComponent<ChapterThreeInflammation>();
            var systemSo=new SerializedObject(system);var regions=systemSo.FindProperty("regions");regions.arraySize=overlays[level].Length;
            for(int i=0;i<regions.arraySize;i++)Region(regions.GetArrayElementAtIndex(i),overlays[level][i],regions.arraySize==1?"炎症区":i==0?"左区":"右区");
            systemSo.ApplyModifiedPropertiesWithoutUndo();
            var grid=new OpenFieldGridModel(15,11,core,entries,Blocks(level,entries));var route=new List<Vector2Int>();
            foreach(var entry in entries)if(!grid.TryGetPath(entry,route))throw new InvalidOperationException(name+" route");
            EditorSceneManager.SaveScene(scene);catalog=Load<LevelSelectionArtwork>("Assets/_Project/Resources/LevelSelectionArtwork.asset");
            catalog.chapters[2].levels[level].sceneName=name;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            if(!buildScenes.Any(s=>s.path==path))buildScenes.Add(new EditorBuildSettingsScene(path,true));
        }
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Chapter02Level04.unity",OpenSceneMode.Single);
        Set(Object.FindObjectOfType<OpenFieldPrototypeController>(),"nextSceneName","Chapter03Level01");EditorSceneManager.SaveOpenScenes();
        EditorBuildSettings.scenes=buildScenes.ToArray();AssetDatabase.SaveAssets();Debug.Log("CHAPTER_THREE_CONTENT_READY");
    }
}
