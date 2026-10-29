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
using Object = UnityEngine.Object;

public sealed class ChapterTwoActionAnimationPlayModeTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object obj, string name) => obj.GetType().GetProperty(name, Flags).GetValue(obj);
    static object Field(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethods(Flags)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(obj, args);
    static Component Owner() => (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    static Component Build(Component owner, Vector2Int cell, int index)
    {
        Call(owner,"SelectTower",index);Assert.That(Call(owner,"TryBuildAt",cell),Is.True);
        return ((IEnumerable)Get(owner,"Defenders")).Cast<Component>().Single(d => (Vector2Int)Get(d,"Cell") == cell);
    }
    static Component Tower(Component defender) => (Component)Get(defender,"Tower");
    static Component Action(Component tower) => tower.GetComponent(Runtime("ChapterTwoActionAnimation"));
    static Component Enemy(Component owner) => ((IEnumerable)Field(owner,"activeEnemies")).Cast<Component>().First();
    static void Damage(Component obj, float amount) => obj.GetType().GetMethod("TakeDamage",new[]{typeof(float)}).Invoke(obj,new object[]{amount});
    static void CaptureWorld(Camera camera, string folder, int index)
    {
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        var target = RenderTexture.GetTemporary(640,360,24);RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        var image = new Texture2D(640,360,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture = target;camera.Render();RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(folder,$"frame_{index:000}.png"),image.EncodeToPNG());
        }
        finally { camera.targetTexture=previousTarget;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.Destroy(image); }
    }

    [UnityTest] public IEnumerator RealSupportContactsAnimateAtTheSkillResultAndFreezeOnPause()
    {
        using (new ZzMobileLandscapePlayModeTests.Resolution(1280,720))
        {
            yield return null;yield return SceneManager.LoadSceneAsync("Chapter02Level03");
            Component owner=Owner(),wall=Build(owner,new Vector2Int(2,5),3);
            Component repair=Tower(Build(owner,new Vector2Int(2,6),6));
            Component complement=Tower(Build(owner,new Vector2Int(2,4),7));
            Component repairAction=Action(repair),complementAction=Action(complement);
            foreach(Component action in new[]{repairAction,complementAction})
            {
                var poses=(Sprite[])Field(action,"actionFrames");var contacts=(Sprite[])Field(action,"contactFrames");
                Assert.That(poses.Length,Is.EqualTo(10));Assert.That(contacts.Length,Is.EqualTo(6));
                Assert.That(poses.Distinct().Count(),Is.EqualTo(10));Assert.That(contacts.Distinct().Count(),Is.EqualTo(6));
                Assert.That(poses.Concat(contacts).All(s=>s!=null&&s.texture.filterMode==FilterMode.Point),Is.True);
            }
            Damage(wall,400f);Call(owner,"StartAssault");Component enemy=Enemy(owner);
            ((Behaviour)enemy.GetComponent(Runtime("PathFollower"))).enabled=false;
            enemy.transform.position=complement.transform.position+Vector3.right*1.25f;Physics2D.SyncTransforms();
            Camera camera=Camera.main;Vector3 center=(repair.transform.position+enemy.transform.position)*.5f;
            camera.transform.position=new Vector3(center.x,center.y-.45f,camera.transform.position.z);camera.orthographicSize=2.25f;
            string folder=Environment.GetEnvironmentVariable("CELLDEFENSE_NEW_TOWER_ANIMATION_CAPTURE");
            var repairPoses=new HashSet<Sprite>();var complementPoses=new HashSet<Sprite>();var impacts=new HashSet<Sprite>();
            Time.captureFramerate=30;bool hit=false;
            for(int frame=0;frame<156;frame++)
            {
                yield return null;
                repairPoses.Add(repair.GetComponent<SpriteRenderer>().sprite);
                complementPoses.Add(complement.GetComponent<SpriteRenderer>().sprite);
                var contact=(SpriteRenderer)Field(complementAction,"contact");
                if(contact!=null&&contact.enabled)impacts.Add(contact.sprite);
                if(!hit&&(float)Get(enemy,"CurrentHealth")<420f)
                {
                    hit=true;Assert.That(contact,Is.Not.Null);Assert.That(contact.enabled,Is.True,"Impact starts on the actual damage frame.");
                    Assert.That(Vector3.Distance(contact.transform.position,enemy.transform.position),Is.LessThan(.001f));
                    Assert.That(Get(enemy,"CurrentHealth"),Is.EqualTo(60f));
                    Call(owner,"Pause");Sprite body=complement.GetComponent<SpriteRenderer>().sprite,impact=contact.sprite;
                    float time=(float)Field(complementAction,"release");yield return new WaitForSecondsRealtime(.12f);
                    Assert.That(complement.GetComponent<SpriteRenderer>().sprite,Is.SameAs(body));Assert.That(contact.sprite,Is.SameAs(impact));
                    Assert.That(Field(complementAction,"release"),Is.EqualTo(time));Call(owner,"Resume");
                }
                if(!hit)Assert.That(contact==null||!contact.enabled,Is.True,"Assembly alone must not show a damage burst.");
                CaptureWorld(camera,folder,frame);
            }
            Assert.That(hit,Is.True);Assert.That(repairPoses.Count,Is.GreaterThanOrEqualTo(6));
            Assert.That(complementPoses.Count,Is.GreaterThanOrEqualTo(8));Assert.That(impacts.Count,Is.EqualTo(6));
            Assert.That((float)Get(wall,"CurrentHealth"),Is.GreaterThan(100f));
            complement.gameObject.SetActive(false);Assert.That(((SpriteRenderer)Field(complementAction,"contact")).enabled,Is.False);
        }
    }

    [UnityTest] public IEnumerator LostTargetsCancelChargeAndFullWallsDoNotProduceRepairContacts()
    {
        yield return SceneManager.LoadSceneAsync("Chapter02Level03");Component owner=Owner();
        Component wall=Build(owner,new Vector2Int(2,5),3),repair=Tower(Build(owner,new Vector2Int(2,6),6));
        Component complement=Tower(Build(owner,new Vector2Int(2,4),7));Component action=Action(complement);
        Sprite rest=complement.GetComponent<SpriteRenderer>().sprite;
        Call(owner,"StartAssault");Component enemy=Enemy(owner);((Behaviour)enemy.GetComponent(Runtime("PathFollower"))).enabled=false;
        enemy.transform.position=complement.transform.position+Vector3.right*1.25f;Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.3f);
        Assert.That(complement.GetComponent<SpriteRenderer>().sprite,Is.Not.SameAs(rest));
        Assert.That(Field(Action(repair),"contact"),Is.Null,"A full wall must not generate a successful-repair contact.");
        enemy.transform.position+=Vector3.right*20f;Physics2D.SyncTransforms();yield return null;yield return null;
        Assert.That(complement.GetComponent<SpriteRenderer>().sprite,Is.SameAs(rest));
        Assert.That(Field(action,"contact"),Is.Null,"Losing assembly progress is not a lysis hit.");
        Component behavior=complement.GetComponent(Runtime("ComplementAttackBehaviour"));Set(behavior,"assemblySeconds",.5f);
        Damage(enemy,350f);enemy.transform.position=complement.transform.position+Vector3.right*1.25f;Physics2D.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+2f;
        while(enemy.gameObject.activeInHierarchy&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(enemy.gameObject.activeInHierarchy,Is.False);
        SpriteRenderer contact=(SpriteRenderer)Field(action,"contact");Assert.That(contact.enabled,Is.True,"A lethal result keeps its short contact effect after pooling the enemy.");
        Vector3 position=contact.transform.position;yield return new WaitForSeconds(.1f);
        Assert.That(contact.transform.position,Is.EqualTo(position));yield return new WaitForSeconds(.35f);Assert.That(contact.enabled,Is.False);
    }

    [UnityTearDown] public IEnumerator Restore()
    {
        Time.captureFramerate=0;Time.timeScale=1f;MobilePlayModeTests.Preview(false);
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
