using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class GuidanceArtworkPlayModeTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetProperty(n,Flags).GetValue(o);
    static object Field(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    [UnityTest] public IEnumerator MembranesMatchRealRegionsAndShowAllFourStatesWithoutMovingWhilePaused()
    {
        yield return SceneManager.LoadSceneAsync("Chapter03Level04");yield return null;
        Component owner=(Component)Object.FindObjectOfType(Type.GetType("OpenFieldPrototypeController, Assembly-CSharp",true));
        Component system=owner.GetComponent(Type.GetType("ChapterThreeInflammation, Assembly-CSharp",true));
        Array regions=(Array)Get(system,"Regions"),visuals=(Array)Field(system,"regionVisuals");Assert.That(regions.Length,Is.EqualTo(2));
        Call(owner,"Pause");
        RectTransform pause=Object.FindObjectsOfType<RectTransform>().First(r=>r.name=="Pause Panel");
        pause.parent.gameObject.SetActive(false); // Capture the zone, while retaining the actual pause gate.
        foreach(int i in new[]{0,1})
        {
            Component visual=(Component)visuals.GetValue(i);Rect bounds=(Rect)regions.GetValue(i).GetType().GetField("bounds").GetValue(regions.GetValue(i));
            var frame=(SpriteRenderer)Field(visual,"frame");Assert.That(frame.drawMode,Is.EqualTo(SpriteDrawMode.Sliced));Assert.That(frame.size,Is.EqualTo(bounds.size));
            Assert.That(frame.sprite.border.x,Is.GreaterThan(0f));Assert.That(frame.sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));
        }
        object region=regions.GetValue(0);Component display=(Component)visuals.GetValue(0);
        string folder=Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_TWO_CAPTURE");
        string[] names={"stable","activated","warning","overheated"};float[] seconds={0,3,6,5};
        for(int i=0;i<4;i++)
        {
            Call(region,"Tick",seconds[i],1,false);Call(system,"RefreshVisuals");
            Assert.That(((SpriteRenderer)Field(display,"warning")).enabled,Is.EqualTo(i>=2));
            if(!string.IsNullOrEmpty(folder))
            {
                string name="region-"+names[i]+".png";OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);Directory.CreateDirectory(folder);
                File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(folder,name),true);
            }
        }
        var motes=(SpriteRenderer[])Field(display,"motes");Vector3[] positions=motes.Select(m=>m.transform.position).ToArray();
        yield return new WaitForSecondsRealtime(.15f);Assert.That(motes.Select(m=>m.transform.position),Is.EqualTo(positions));
        ((Behaviour)system).enabled=false;Assert.That(display.gameObject.activeSelf,Is.False);
        Call(owner,"Resume");yield return SceneManager.LoadSceneAsync("MainMenu");
    }
    [UnityTearDown]public IEnumerator Restore(){Time.timeScale=1f;yield return SceneManager.LoadSceneAsync("MainMenu");}
}
