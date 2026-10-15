using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatAnimationSampleInstaller
{
    public const string AnimationRoot = "Assets/_Project/Art/Animations/CombatSample_v1";
    public const string ReportRoot = "Docs/Art/AnimationIntegration/2026-10-12";
    private const string ArtRoot = "Assets/_Project/Art/Sprites/细胞绘制/帧动画/";

    [MenuItem("Cell Defense/Apply Combat Feedback to Existing Levels")]
    public static void Install()
    {
        Require(!EditorApplication.isPlaying,"Apply prefab and scene changes outside Play Mode.");
        Directory.CreateDirectory(AnimationRoot);
        Directory.CreateDirectory(ReportRoot);
        AssetDatabase.Refresh();
        Sprite[] neut = GameplaySprites(ArtRoot + "中性粒细胞_强化攻击_v2/neutrophil_attack_sheet.png","中性粒细胞攻击");
        Sprite[] move = GameplaySprites(ArtRoot + "六塔与普通敌人_强化动画_v2/07_staph_move/animation_sheet.png","葡萄球菌群移动");
        Sprite[] strike = GameplaySprites(ArtRoot + "六塔与普通敌人_强化动画_v2/08_staph_attack/animation_sheet.png","葡萄球菌群攻击");

        AnimationClip idleClip = Clip("Neutrophil_Idle", neut, new[] {0,0}, new[] {0f,0.1f}, true);
        AnimationClip neutAttack = Clip("Neutrophil_Attack", neut,
            new[] {1,2,3,4,5,6,7,7}, new[] {0f,0.05f,0.11f,0.16f,0.23f,0.32f,0.42f,0.44f}, false,
            new AnimationEvent {time=0.11f,functionName="ReleaseAttack"});
        AnimationClip moveClip = Clip("Staph_Move", move, new[] {0,1,2,3,4,5,6,7,0},
            new[] {0f,0.1f,0.2f,0.3f,0.4f,0.5f,0.6f,0.7f,0.8f}, true);
        AnimationClip strikeClip = Clip("Staph_Attack", strike, new[] {0,1,2,3,4,5,6,7,7},
            new[] {0f,0.02f,0.06f,0.11f,0.16f,0.22f,0.3f,0.38f,0.4f}, false,
            new AnimationEvent {time=0.16f,functionName="CompleteCoreAttack"});
        AnimatorController towerController = Controller("Neutrophil", "Idle", idleClip, neutAttack);
        AnimatorController enemyController = Controller("Staph", "Move", moveClip, strikeClip);
        Shader shader = Shader.Find("CellDefense/SpriteHitFlash");
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Hit-flash shader did not compile.");
        string materialPath = AnimationRoot + "/StaphHitFlash.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader);AssetDatabase.CreateAsset(material,materialPath); }
        material.shader = shader;
        material.SetFloat("_FlashAmount",0f);
        EditorUtility.SetDirty(material);

        ConfigureTower(towerController,neut[0],ProjectileSprite());
        ConfigureEnemy(enemyController,move[0],material);
        AssetDatabase.SaveAssets();
        ApplyToExistingLevels();
        Validate();
        Debug.Log("Combat feedback applied to existing gameplay levels.");
    }

    private static Sprite[] Sprites(string path)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(sprite=>sprite.name,StringComparer.Ordinal).ToArray();
        Require(sprites.Length == 8,"Expected eight sprites: " + path);
        foreach (Sprite sprite in sprites) Require(sprite.rect.width==128 && sprite.rect.height==128,"Unexpected sprite rect.");
        return sprites;
    }

    private static Sprite[] GameplaySprites(string source,string name)
    {
        string directory=ArtRoot+"关卡动画_v1";
        Directory.CreateDirectory(directory);
        string path=directory+"/"+name+".png";
        if (!File.Exists(path)) File.Copy(source,path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=96f;importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        SpriteMetaData[] rectangles=new SpriteMetaData[8];
        for (int i=0;i<8;i++) rectangles[i]=new SpriteMetaData {name="frame_"+(i+1).ToString("D2"),
            rect=new Rect((i%4)*128,(1-i/4)*128,128,128),alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*0.5f};
        importer.spritesheet=rectangles;importer.SaveAndReimport();
        return Sprites(path);
    }

    private static AnimationClip Clip(string name,Sprite[] sprites,int[] indices,float[] times,bool loop,AnimationEvent animationEvent=null)
    {
        string path = AnimationRoot + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip();AssetDatabase.CreateAsset(clip,path); }
        clip.name=name;clip.frameRate=100;
        var keys = new ObjectReferenceKeyframe[indices.Length];
        for (int i=0;i<indices.Length;i++) keys[i]=new ObjectReferenceKeyframe {time=times[i],value=sprites[indices[i]]};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding {path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        AnimationClipSettings settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=loop;settings.startTime=0f;settings.stopTime=times[times.Length-1];
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        AnimationUtility.SetAnimationEvents(clip,animationEvent == null ? Array.Empty<AnimationEvent>() : new[] {animationEvent});
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController Controller(string name,string restName,AnimationClip restClip,AnimationClip attackClip)
    {
        string path=AnimationRoot + "/" + name + ".controller";
        AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine=controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
        controller.parameters=new[] {new AnimatorControllerParameter {name="Attack",type=AnimatorControllerParameterType.Trigger}};
        AnimatorState rest=machine.AddState(restName,new Vector3(250,120));rest.motion=restClip;rest.writeDefaultValues=false;
        AnimatorState attack=machine.AddState("Attack",new Vector3(500,120));attack.motion=attackClip;attack.writeDefaultValues=false;
        machine.defaultState=rest;
        AnimatorStateTransition begin=rest.AddTransition(attack);
        begin.hasExitTime=false;begin.duration=0f;begin.hasFixedDuration=true;begin.canTransitionToSelf=false;
        begin.AddCondition(AnimatorConditionMode.If,0f,"Attack");
        AnimatorStateTransition finish=attack.AddTransition(rest);
        finish.hasExitTime=true;finish.exitTime=1f;finish.duration=0f;finish.hasFixedDuration=true;finish.canTransitionToSelf=false;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Sprite ProjectileSprite()
    {
        const string directory="Assets/_Project/Art/Sprites/细胞绘制/帧动画/运行样例_v1";
        const string source="Assets/_Project/Art/Sprites/细胞绘制/2026-09-28_完整补图/08_子弹与特效/070_单体子弹.png";
        string path=directory+"/单体颗粒.png";
        Directory.CreateDirectory(directory);
        if (!File.Exists(path)) File.Copy(source,path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null,"Projectile sprite did not import.");
        return sprite;
    }

    private static void ConfigureTower(AnimatorController controller,Sprite idle,Sprite projectile)
    {
        const string path="Assets/_Project/Prefabs/Towers/Tower.prefab";
        GameObject root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            Animator animator=root.GetComponent<Animator>();animator.runtimeAnimatorController=controller;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            root.GetComponent<SpriteRenderer>().sprite=idle;
            SerializedObject data=new SerializedObject(root.GetComponent<TowerController>());
            data.FindProperty("releaseOnAnimationEvent").boolValue=true;
            data.FindProperty("muzzleLocalOffset").vector2Value=new Vector2(-0.40f,-0.32f);
            data.FindProperty("aimVisualAtTarget").boolValue=true;
            data.FindProperty("projectileVisual").objectReferenceValue=projectile;
            data.FindProperty("projectileVisualSize").floatValue=0.18f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureEnemy(AnimatorController controller,Sprite first,Material material)
    {
        const string path="Assets/_Project/Prefabs/Enemies/Enemy.prefab";
        GameObject root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            Animator animator=root.GetComponent<Animator>();animator.runtimeAnimatorController=controller;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            SpriteRenderer renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=first;renderer.sharedMaterial=material;
            if (root.GetComponent<SpriteHitFlash>() == null) root.AddComponent<SpriteHitFlash>();
            SerializedObject data=new SerializedObject(root.GetComponent<EnemyController>());
            data.FindProperty("playCoreAttackAnimation").boolValue=true;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ApplyToExistingLevels()
    {
        string[] levels={"OpenFieldPrototype","OpenFieldLevel02","Prototype","Level02"};
        Scene original=SceneManager.GetActiveScene();
        Directory.CreateDirectory(ReportRoot+"/gameplay/scene-backup");
        foreach (string name in levels)
        {
            string path="Assets/_Project/Scenes/"+name+".unity";
            Require(File.Exists(path),"Existing gameplay scene is missing: "+path);
            string backup=ReportRoot+"/gameplay/scene-backup/"+name+".unity";
            if (!File.Exists(backup)) File.Copy(path,backup);
            Scene scene=SceneManager.GetSceneByPath(path);
            bool alreadyLoaded=scene.IsValid() && scene.isLoaded;
            if (!alreadyLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            bool found=scene.GetRootGameObjects().Any(root=>root.GetComponentInChildren<CombatHitFeedback>(true)!=null);
            if (!found)
            {
                GameObject feedback=new GameObject("Combat Feedback");
                SceneManager.MoveGameObjectToScene(feedback,scene);
                feedback.AddComponent<CombatHitFeedback>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,path);
            if (!alreadyLoaded) EditorSceneManager.CloseScene(scene,true);
        }
        if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
    }

    public static void Validate()
    {
        AnimatorController enemy=AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationRoot+"/Staph.controller");
        AnimatorState attack=enemy.layers[0].stateMachine.states.First(state=>state.state.name=="Attack").state;
        Require(attack.motion.name=="Staph_Attack","Enemy is bound to the wrong attack clip.");
        AnimationClip move=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot+"/Staph_Move.anim");
        Require(AnimationUtility.GetAnimationClipSettings(move).loopTime,"Movement must loop.");
        AnimationClip tower=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot+"/Neutrophil_Attack.anim");
        Require(!AnimationUtility.GetAnimationClipSettings(tower).loopTime,"Attack must play once.");
        Require(tower.events.Length==1 && tower.events[0].functionName=="ReleaseAttack" && Mathf.Abs(tower.events[0].time-0.11f)<0.001f,"Release event is misplaced.");
        foreach (string name in new[] {"Neutrophil","Staph"})
        {
            AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationRoot+"/"+name+".controller");
            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
                foreach (AnimatorStateTransition transition in state.state.transitions)
                    Require(transition.duration==0f,"Sprite transitions must not blend.");
        }
        File.WriteAllText(ReportRoot+"/asset-validation.json","{\"passed\":true,\"movementLoops\":true,\"attackPlaysOnce\":true,\"releaseTimeSeconds\":0.11,\"correctEnemyAttack\":true,\"zeroTransitionDuration\":true}");
    }

    private static void Require(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
