using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class CellCombatFeedbackPlayModeTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly List<Object> owned=new List<Object>();
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Field(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
    static object Get(object target,string name)=>target.GetType().GetProperty(name,Flags).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethods(Flags).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);
    static object Kind(int value)=>Enum.ToObject(Runtime("CellImpactKind"),value);
    static void Static(string name,params object[] args)=>Runtime("CellCombatFeedback").GetMethod(name,BindingFlags.Static|BindingFlags.Public).Invoke(null,args);
    static Component Owner()=>(Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    static Component Feedback()=>(Component)Object.FindObjectOfType(Runtime("CellCombatFeedback"));
    GameObject Host(string name)
    {
        var obj=new GameObject(name);obj.SetActive(false);owned.Add(obj);return obj;
    }
    Component Enemy(Vector3 position,float health=1000f)
    {
        var owner=Owner();object sequence=Field(owner,"waveSequence");object wave=Call(sequence,"GetWave",0);
        // Reuse a shipped enemy's real sprite/animation/state components, with an isolated health definition.
        var definition=Object.Instantiate((Object)Field(wave,"enemy"));owned.Add(definition);
        Set(definition,"maxHealth",health);Set(definition,"engulfImmune",false);
        Component prefab=(Component)Get(definition,"EnemyPrefab");
        Component enemy=Object.Instantiate(prefab,position,Quaternion.identity);owned.Add(enemy.gameObject);
        var pathObj=Host("Feedback target path");Component path=pathObj.AddComponent(Runtime("WaypointPath"));
        var points=new Transform[2];for(int i=0;i<2;i++){var point=new GameObject("Point");point.transform.SetParent(pathObj.transform);point.transform.position=position+Vector3.right*(i*10f);points[i]=point.transform;}
        pathObj.SetActive(true);Call(path,"Configure",(object)points);Call(enemy,"Initialize",path,definition);
        Call(enemy.GetComponent(Runtime("PathFollower")),"Stop");Physics2D.SyncTransforms();return enemy;
    }
    Component Tower(int index,Vector3 position)
    {
        Component owner=Owner();object definition=((Array)Field(owner,"towerDefinitions")).GetValue(index);
        Component prefab=(Component)Get(definition,"Prefab");Component tower=Object.Instantiate(prefab,position,Quaternion.identity);owned.Add(tower.gameObject);
        var host=Host("Feedback build slot");Component slot=host.AddComponent(Runtime("BuildSlot"));host.SetActive(true);
        Call(tower,"Initialize",definition,Field(owner,"projectilePool"),slot);Call(slot,"Occupy",tower);Set(tower,"attackCooldown",9999f);
        return tower;
    }
    static void Damage(Component enemy,float amount)=>enemy.GetType().GetMethod("TakeDamage",new[]{typeof(float)}).Invoke(enemy,new object[]{amount});

    [UnityTest] public IEnumerator AllNineBoardsInstallBoundedFeedbackAndPauseAndUnloadCleanly()
    {
        foreach(string scene in new[]{"OpenFieldPrototype","OpenFieldLevel02","OpenFieldLevel03","OpenFieldLevel04","OpenFieldLevel05","Chapter02Level01","Chapter02Level02","Chapter02Level03","Chapter02Level04"})
        {
            yield return SceneManager.LoadSceneAsync(scene);yield return null;
            Assert.That(Feedback(),Is.Not.Null,scene);Assert.That(Object.FindObjectOfType(Runtime("CombatHitFeedback")),Is.Not.Null,scene);
        }
        Component feedback=Feedback();
        for(int i=0;i<160;i++)Static("Impact",Kind(i%8),Vector3.zero,false);
        Assert.That(Get(feedback,"ActiveEffectCount"),Is.EqualTo(96));
        var sprites=feedback.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).Select(r=>r.sprite).ToArray();
        Call(Owner(),"Pause");yield return new WaitForSecondsRealtime(.12f);
        Assert.That(feedback.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).Select(r=>r.sprite),Is.EqualTo(sprites));
        Call(Owner(),"Resume");Time.timeScale=2f;yield return new WaitForSecondsRealtime(.22f);Time.timeScale=1f;
        Assert.That(Get(feedback,"ActiveEffectCount"),Is.EqualTo(0));
        yield return SceneManager.LoadSceneAsync("MainMenu");Assert.That(Feedback(),Is.Null);
    }

    [UnityTest] public IEnumerator CancelledAndReusedProjectilesDoNotLeakMarksAndRecoilNeverMovesPhysics()
    {
        yield return SceneManager.LoadSceneAsync("Chapter02Level03");Component owner=Owner(),pool=(Component)Field(owner,"projectilePool"),target=Enemy(Vector3.right*2f);
        Component projectile=(Component)Call(pool,"Get",Vector3.zero);Call(projectile,"InitializeMark",target,4f);Static("PrepareProjectile",projectile,Kind(1));
        Call(pool,"Release",projectile);Assert.That(Field(projectile,"feedbackEnabled"),Is.False);
        projectile=(Component)Call(pool,"Get",Vector3.zero);Assert.That(Field(projectile,"feedbackEnabled"),Is.False);
        Call(projectile,"InitializeMark",target,4f);Static("PrepareProjectile",projectile,Kind(1));target.gameObject.SetActive(false);
        yield return new WaitForSeconds(.08f);Assert.That(Get(pool,"ActiveCount"),Is.EqualTo(0));
        Assert.That(Call(Feedback(),"ContactCount",Kind(1)),Is.EqualTo(0));
        Component tower=Tower(0,Vector3.zero);Vector3 position=tower.transform.position,scale=tower.transform.localScale;Quaternion rotation=tower.transform.rotation;
        Static("Release",Kind(0),tower,Vector3.right);
        Component recoil=tower.GetComponent(Runtime("TowerImpactRecoil"));Assert.That((float)Field(recoil,"elapsed"),Is.EqualTo(0f));
        var properties=new MaterialPropertyBlock();tower.GetComponent<SpriteRenderer>().GetPropertyBlock(properties);
        Assert.That(properties.GetFloat("_ImpactKick"),Is.GreaterThan(0f));
        yield return new WaitForSeconds(.06f);
        Assert.That(tower.transform.position,Is.EqualTo(position));Assert.That(tower.transform.localScale,Is.EqualTo(scale));Assert.That(tower.transform.rotation,Is.EqualTo(rotation));
    }

    static void Capture(Camera camera,string folder,int frame)
    {
        if(string.IsNullOrEmpty(folder))return;Directory.CreateDirectory(folder);
        RenderTexture texture=RenderTexture.GetTemporary(1280,720,24),previous=RenderTexture.active;RenderTexture old=camera.targetTexture;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,$"frame_{frame:000}.png"),image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(texture);Object.Destroy(image);}
    }

    [UnityTest] public IEnumerator EightTowerStylesRenderFromTheirRealSkillEvents()
    {
        yield return SceneManager.LoadSceneAsync("Chapter02Level03");Component owner=Owner();
        var towers=new Component[8];var targets=new Component[8];var states=new Component[8];
        int[] definitions={0,2,1,3,4,5,6,7}; // Chapter two's build menu presents macrophage before antibody.
        for(int i=0;i<8;i++)
        {
            Vector3 position=new Vector3(-6f+(i%4)*4f,i<4?2.2f:-2.2f,0f);
            towers[i]=Tower(definitions[i],position-Vector3.right*.7f);
            if(i!=6)targets[i]=Enemy(position+Vector3.right*.7f,i==2?15f:1000f);
            foreach(SpriteRenderer r in towers[i].GetComponentsInChildren<SpriteRenderer>())r.gameObject.layer=31;
        }
        Component wall=towers[3].gameObject.AddComponent(Runtime("TowerCombatState"));Call(wall,"Initialize",towers[3],new Vector2Int(1,1));states[3]=wall;
        // A separate wall in the repair lane makes the actual repair scan, allowance and pulse run.
        Component repairWall=Tower(3,towers[6].transform.position+Vector3.right*1.4f);
        foreach(SpriteRenderer r in repairWall.GetComponentsInChildren<SpriteRenderer>())r.gameObject.layer=31;
        Component repairState=repairWall.gameObject.AddComponent(Runtime("TowerCombatState"));Call(repairState,"Initialize",repairWall,new Vector2Int(2,1));
        ((IDictionary)Field(owner,"cellDefenders")).Add(new Vector2Int(2,1),repairState);Damage(repairState,300f);
        Component antibody=towers[1].GetComponent(Runtime("AntibodyAttackBehaviour"));Call(antibody,"RememberAntigen",Get(targets[1],"CurrentAntigenId"),20f);
        Component complement=towers[7].GetComponent(Runtime("ComplementAttackBehaviour"));Set(complement,"assemblySeconds",2f);
        // Keep every actor's automatic targeting idle except the two support clocks.
        foreach(Component actor in towers)Set(actor,"attackCooldown",9999f);
        foreach(SpriteRenderer r in Feedback().GetComponentsInChildren<SpriteRenderer>(true))r.gameObject.layer=31;
        foreach(SpriteRenderer r in ((Component)Object.FindObjectOfType(Runtime("CombatHitFeedback"))).GetComponentsInChildren<SpriteRenderer>(true))r.gameObject.layer=31;
        var cameraObject=new GameObject("Feedback showcase camera");owned.Add(cameraObject);Camera camera=cameraObject.AddComponent<Camera>();
        camera.enabled=false;camera.orthographic=true;camera.orthographicSize=5.2f;camera.aspect=16f/9f;
        camera.transform.position=new Vector3(0f,0f,-20f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.19f);camera.cullingMask=(1<<31)|(1<<3);
        string folder=Environment.GetEnvironmentVariable("CELLDEFENSE_COMBAT_FEEL_CAPTURE");Time.captureFramerate=30;
        for(int frame=0;frame<180;frame++)
        {
            if(frame%30==0)
            {
                for(int i=0;i<6;i++)
                {
                    if(i==3){Damage(wall,20f);continue;}
                    if(!targets[i].gameObject.activeSelf)continue;
                    if(i==2 && !(bool)Get(towers[i].GetComponent(Runtime("PhagocyteAttackBehaviour")),"CanAttack"))continue;
                    Set(towers[i],"awaitingAttackRelease",true);Set(towers[i],"pendingAttackTarget",targets[i]);Set(towers[i],"pendingAttackSpawnVersion",Get(targets[i],"SpawnVersion"));
                    Set(towers[i],"attackReleaseTimeout",.6f);
                    towers[i].GetComponent<Animator>().SetTrigger("Attack");
                }
            }
            yield return null;
            // Only staging visibility is changed. Physics layers for target queries remain Enemy.
            foreach(Renderer r in Object.FindObjectsOfType<Renderer>())
                if(r.name=="Neural Chain"||r.name.StartsWith("Acid ")||r.name.EndsWith("contact pixels")||r.name=="Support link"||r.name=="Assembly progress")r.gameObject.layer=31;
            Capture(camera,folder,frame);
        }
        Component feedback=Feedback();for(int i=0;i<6;i++)Assert.That(Call(feedback,"ContactCount",Kind(i)),Is.GreaterThan(0),"style "+i);
        Assert.That((float)Get(repairState,"TotalRepaired"),Is.GreaterThan(0f));
        Assert.That(Field(towers[7].GetComponent(Runtime("ChapterTwoActionAnimation")),"contact"),Is.Not.Null);
    }

    [UnityTest] public IEnumerator NormalBudgetFormationShowsRealHitsInThePlayableBoard()
    {
        using(new ZzMobileLandscapePlayModeTests.Resolution(1280,720))
        {
            yield return null;yield return SceneManager.LoadSceneAsync("Chapter02Level03");Component owner=Owner();
            Vector2Int[] cells={new Vector2Int(2,5),new Vector2Int(2,6),new Vector2Int(1,5),new Vector2Int(3,5),new Vector2Int(3,6),new Vector2Int(1,6)};
            int[] choices={3,0,1,4,2,5};
            for(int i=0;i<choices.Length;i++){Call(owner,"SelectTower",choices[i]);Assert.That(Call(owner,"TryBuildAt",cells[i]),Is.True);}
            Assert.That(Get(owner,"CurrentATP"),Is.EqualTo(35)); // Normal starting 250 minus 215; no test-funded units.
            Call(owner,"StartAssault");Time.captureFramerate=30;
            string folder=Environment.GetEnvironmentVariable("CELLDEFENSE_COMBAT_FEEL_LIVE_CAPTURE");
            if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
            for(int frame=0;frame<180;frame++)
            {
                yield return null;
                if(!string.IsNullOrEmpty(folder))
                {
                    string name=$"live-feel-{frame:000}.png";OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);
                    File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(folder,$"frame_{frame:000}.png"),true);
                }
            }
            Component feedback=Feedback();
            foreach(int kind in new[]{0,4,5})Assert.That(Call(feedback,"ContactCount",Kind(kind)),Is.GreaterThan(0));
            Assert.That((bool)Get(owner,"HasActiveRun"),Is.True);
        }
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.captureFramerate=0;Time.timeScale=1f;
        foreach(Object obj in owned.AsEnumerable().Reverse())if(obj!=null)Object.Destroy(obj);owned.Clear();
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
