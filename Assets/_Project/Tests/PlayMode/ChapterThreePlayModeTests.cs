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
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class ChapterThreePlayModeTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly string[] Previous={"OpenFieldPrototype","OpenFieldLevel02","OpenFieldLevel03","OpenFieldLevel04","OpenFieldLevel05","Chapter02Level01","Chapter02Level02","Chapter02Level03","Chapter02Level04"};
    static readonly string[] Third={"Chapter03Level01","Chapter03Level02","Chapter03Level03","Chapter03Level04"};
    readonly Dictionary<string,int?> saved=new Dictionary<string,int?>();
    static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    static Component Owner()=>(Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    static object Get(object o,string n)=>o.GetType().GetProperty(n,Flags).GetValue(o);
    static object Field(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static string Key(string s)=>"CellDefense.LevelProgress.v1."+s+".BestScore";
    static Component[] Enemies(Component o)=>((IEnumerable)Field(o,"activeEnemies")).Cast<Component>().ToArray();
    static void Damage(Component e,float n)=>e.GetType().GetMethod("TakeDamage",new[]{typeof(float)}).Invoke(e,new object[]{n});
    static Component Build(Component o,Vector2Int cell,int index)
    {
        Call(o,"SelectTower",index);Assert.That(Call(o,"TryBuildAt",cell),Is.True,$"Build {index} at {cell}");
        return ((IEnumerable)Get(o,"Defenders")).Cast<Component>().Single(c=>(Vector2Int)Get(c,"Cell")==cell);
    }
    static Component SystemOf(Component o)=>o.GetComponent(Runtime("ChapterThreeInflammation"));
    static object[] Regions(Component s)=>((Array)Get(s,"Regions")).Cast<object>().ToArray();
    static bool Source(Component e)=>(bool)Get(Get(e,"Definition"),"IsInflammationSource");
    static void Capture(string name)
    {
        string folder=Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_TWO_CAPTURE");if(string.IsNullOrEmpty(folder))return;
        Canvas.ForceUpdateCanvases();OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);Directory.CreateDirectory(folder);
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(folder,name),true);
    }
    [SetUp] public void Preserve()
    {
        foreach(string s in Previous.Concat(Third)){saved[s]=PlayerPrefs.HasKey(Key(s))?(int?)PlayerPrefs.GetInt(Key(s)):null;PlayerPrefs.DeleteKey(Key(s));}
    }
    [UnityTest] public IEnumerator CrossChapterFlowRunsAllFourSequencesAndRetainsIndependentScores()
    {
        foreach(string s in Previous.Take(8))PlayerPrefs.SetInt(Key(s),100);
        yield return SceneManager.LoadSceneAsync(Previous.Last());Component owner=Owner();
        Call(owner,"FinishRun",Enum.Parse(Runtime("OpenFieldPhase"),"Victory"));Call(owner,"LoadNextLevel");yield return null;yield return null;
        int[] waves={4,5,5,7};
        for(int level=0;level<4;level++)
        {
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(Third[level]));owner=Owner();
            Assert.That(Get(owner,"TotalRounds"),Is.EqualTo(waves[level]));Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
            Call(owner,"StartAssault");float deadline=Time.realtimeSinceStartup+60f;var seen=new HashSet<int>();
            while((bool)Get(owner,"HasActiveRun")&&Time.realtimeSinceStartup<deadline)
            {
                Time.timeScale=20f;seen.Add((int)Get(owner,"CurrentRound"));foreach(var e in Enemies(owner))Damage(e,1000000f);yield return null;
            }
            yield return null;
            Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Victory"));Assert.That(seen,Is.EquivalentTo(Enumerable.Range(1,waves[level])));
            Assert.That(PlayerPrefs.GetInt(Key(Third[level])),Is.EqualTo(10000));
            Assert.That(Regions(SystemOf(owner)).All(r=>(float)Get(r,"Level")==0f),Is.True);
            if(level<3){Call(owner,"LoadNextLevel");yield return null;yield return null;}
            else Assert.That(((GameObject)Field(owner,"victoryPanel")).GetComponentsInChildren<Button>().Any(b=>b.name=="Next"),Is.False);
        }
        yield return SceneManager.LoadSceneAsync("MainMenu");var menu=(Component)Object.FindObjectOfType(Runtime("MainMenuController"));
        Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",2);Call(menu,"OpenLevelDetails",3);
        Assert.That(Get(Field(menu,"detailsScore"),"text"),Is.EqualTo("10000"));Assert.That(Get(Field(menu,"detailsStars"),"Stars"),Is.EqualTo(3));
    }
    [UnityTest] public IEnumerator RegionalSourceGivesWarningThenLocalDamageAndRecoversAfterDeath()
    {
        yield return SceneManager.LoadSceneAsync(Third[0]);yield return null;Component owner=Owner(),system=SystemOf(owner);object region=Regions(system)[0];
        Call(owner,"StartAssault");Component source=null;float deadline=Time.realtimeSinceStartup+8f;
        while(source==null&&Time.realtimeSinceStartup<deadline)
        {
            Time.timeScale=10f;foreach(var e in Enemies(owner))if(!Source(e))Damage(e,1000000f);
            source=Enemies(owner).FirstOrDefault(Source);yield return null;
        }
        Time.timeScale=1f;Assert.That(source,Is.Not.Null);
        ((Behaviour)source.GetComponent(Runtime("PathFollower"))).enabled=false;
        ((Behaviour)source.GetComponent(Runtime("EnemyContactCombat"))).enabled=false;
        Rect bounds=(Rect)region.GetType().GetField("bounds").GetValue(region);Vector2Int cell=Vector2Int.FloorToInt(bounds.center);
        Component wall=Build(owner,cell,3),outside=Build(owner,new Vector2Int(1,1),0),inside=Build(owner,cell+Vector2Int.right,0);
        source.transform.position=(Vector3)Call(owner,"CellToWorld",cell);
        Call(region,"Reset");Call(system,"Tick",3f);
        Assert.That((float)Get(Get(inside,"Tower"),"CurrentDamage"),Is.EqualTo(28.75f).Within(.001f));
        Assert.That(Get(Get(outside,"Tower"),"CurrentDamage"),Is.EqualTo(25f));
        Call(system,"Tick",6f);Assert.That(Get(region,"IsOverheated"),Is.True);Assert.That(Get(region,"IsDamaging"),Is.False);
        float before=(float)Get(region,"Level");Call(owner,"Pause");yield return new WaitForSecondsRealtime(.15f);
        Assert.That(Get(region,"Level"),Is.EqualTo(before));Call(owner,"Resume");Call(system,"Tick",5f);
        Assert.That((float)Get(wall,"CurrentHealth"),Is.EqualTo(490f).Within(.1f));
        Assert.That(Get(outside,"CurrentHealth"),Is.EqualTo(100f));Assert.That(Get(owner,"CurrentCoreLife"),Is.EqualTo(20));
        Capture("inflammation-warning-and-damage.png");Damage(source,1000000f);Call(system,"Tick",14f);
        Assert.That(Get(region,"Level"),Is.EqualTo(0f));Assert.That(Get(Get(inside,"Tower"),"EnvironmentalDamageMultiplier"),Is.EqualTo(1f));
    }
    [UnityTest] public IEnumerator HelperTSelectsOneAntibodyAndDoesNotStackOrLeaveStaleBonuses()
    {
        yield return SceneManager.LoadSceneAsync(Third[1]);yield return null;Component owner=Owner(),system=SystemOf(owner);
        Component b1=Build(owner,new Vector2Int(2,3),2),b2=Build(owner,new Vector2Int(3,2),2);
        Component h1=Build(owner,new Vector2Int(1,2),8),h2=Build(owner,new Vector2Int(1,3),8);
        Call(system,"Tick",.1f);
        Assert.That(Get(b1.GetComponent(Runtime("AntibodyAttackBehaviour")),"SupportMarkDurationBonus"),Is.EqualTo(2f));
        Assert.That(Get(b2.GetComponent(Runtime("AntibodyAttackBehaviour")),"SupportMarkDurationBonus"),Is.EqualTo(0f));
        h1.gameObject.SetActive(false);h2.gameObject.SetActive(false);Call(system,"Tick",.1f);
        Assert.That(Get(b1.GetComponent(Runtime("AntibodyAttackBehaviour")),"SupportMarkDurationBonus"),Is.EqualTo(0f));
    }
    [UnityTest] public IEnumerator RegulatoryTStabilizesOnlyOneRegionWithoutHealingOrStacking()
    {
        yield return SceneManager.LoadSceneAsync(Third[3]);yield return null;Component owner=Owner(),system=SystemOf(owner);var regions=Regions(system);
        Assert.That(((Rect)regions[0].GetType().GetField("bounds").GetValue(regions[0])).Overlaps((Rect)regions[1].GetType().GetField("bounds").GetValue(regions[1])),Is.False);
        Component t1=Build(owner,new Vector2Int(4,4),9),t2=Build(owner,new Vector2Int(4,3),9);Call(t1,"TakeDamage",30f);
        foreach(var r in regions)Call(r,"Tick",9f,1,false);Call(system,"Tick",1f);
        Assert.That(Get(regions[0],"Level"),Is.EqualTo(58f));Assert.That(Get(regions[1],"Level"),Is.EqualTo(64f));
        Assert.That(Get(t1,"CurrentHealth"),Is.EqualTo(70f));
        Assert.That(Get(t1.GetComponent(Runtime("ImmuneSupportBehaviour")),"SupportedRegion"),Is.EqualTo(0));
        Call(owner,"FinishRun",Enum.Parse(Runtime("OpenFieldPhase"),"Defeat"));yield return null;
        Assert.That(regions.All(r=>(float)Get(r,"Level")==0f),Is.True);
    }
    [UnityTest] public IEnumerator FourMapsAndTenChoicesFitThreeRealLandscapeSizes()
    {
        using(new ZzMobileLandscapePlayModeTests.Resolution(1280,720))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(false);
            foreach(string scene in Third){yield return SceneManager.LoadSceneAsync(scene);yield return new WaitForSeconds(.2f);Capture(scene+"-planning.png");}
        }
        foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(1280,720),new Vector2Int(2048,928)})
        using(new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(true);yield return SceneManager.LoadSceneAsync(Third[3]);yield return null;
            Component owner=Owner();Assert.That(Call(owner,"SelectBuildCell",new Vector2Int(7,4)),Is.True);yield return null;Canvas.ForceUpdateCanvases();
            Component menu=(Component)Field(owner,"buildMenu");Assert.That(((Array)Field(menu,"buttons")).Length,Is.EqualTo(10));
            MobilePlayModeTests.Inside((RectTransform)Get(menu,"PanelRect"));
            Component status=(Component)Field(SystemOf(owner),"status");MobilePlayModeTests.Inside((RectTransform)status.transform.parent);
            Assert.That(Get(status,"MissingGlyphCount"),Is.EqualTo(0));
            foreach(Component label in (Array)Field(menu,"labels"))Assert.That(Get(label,"MissingGlyphCount"),Is.EqualTo(0));
            Capture($"ten-towers-{size.x}x{size.y}.png");
        }
    }
    [UnityTest] public IEnumerator ResourceFundedDefensesCanCompleteAllFourChaptersWithoutDirectDamage()
    {
        foreach(string scene in Third)
        {
            yield return SceneManager.LoadSceneAsync(scene);Component owner=Owner();
            object grid=Field(owner,"grid");var route=new List<Vector2Int>();
            var allRoutes=new HashSet<Vector2Int>();var walls=new List<Vector2Int>();
            foreach(Vector2Int entry in (Vector2Int[])Field(owner,"spawnCells"))
            {
                Call(grid,"TryGetPath",entry,route);allRoutes.UnionWith(route);walls.Add(route[route.Count-3]);
            }
            walls=walls.Distinct().ToList();
            // Select safe side cells covering the shared holding positions. Only normal ATP
            // transactions are used; no enemy damage, tower health, or combat stats are changed.
            var side=new List<Vector2Int>();
            for(int x=1;x<14;x++)for(int y=1;y<10;y++)
            {
                var cell=new Vector2Int(x,y);
                if(!allRoutes.Contains(cell)&&Call(grid,"CheckPlacement",cell,false).ToString()=="Valid")side.Add(cell);
            }
            side=side.OrderByDescending(c=>walls.Sum(w=>Mathf.Max(0f,3.5f-Vector2Int.Distance(c,w))))
                .ThenBy(c=>Vector2Int.Distance(c,(Vector2Int)Field(owner,"coreCell"))).ToList();
            int[] choices=scene==Third[0]||scene==Third[1] ? new[]{6,0,4,0,4,0,1,0,4,0,0,4,0,0}
                : new[]{6,0,4,7,0,1,7,4,0,0,4,0,7,0};
            int next=0;bool started=false;float deadline=Time.realtimeSinceStartup+120f;
            while((bool)Get(owner,"HasActiveRun")&&Time.realtimeSinceStartup<deadline)
            {
                Time.timeScale=10f;
                var defenders=((IEnumerable)Get(owner,"Defenders")).Cast<Component>().ToArray();
                var occupied=new HashSet<Vector2Int>(defenders.Select(d=>(Vector2Int)Get(d,"Cell")));
                foreach(Vector2Int wall in walls)
                    if(!occupied.Contains(wall)&&(int)Get(owner,"CurrentATP")>=30)Build(owner,wall,3);
                Array definitions=(Array)Field(owner,"towerDefinitions");
                if(next<choices.Length&&next<side.Count&&(int)Get(owner,"CurrentATP")>=(int)Get(definitions.GetValue(choices[next]),"BuildCost"))
                {
                    Build(owner,side[next],choices[next]);next++;
                }
                if(!started&&next>=3){Call(owner,"StartAssault");started=true;}
                yield return null;
            }
            Debug.Log($"CHAPTER_THREE_DEFENSE {scene}: {Get(owner,"Phase")}, life={Get(owner,"CurrentCoreLife")}, atp={Get(owner,"CurrentATP")}, built={next}");
            Capture(scene+"-defense-result.png");
            Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Victory"),scene);
        }
    }
    [UnityTearDown] public IEnumerator Restore()
    {
        Time.timeScale=1f;MobilePlayModeTests.Preview(false);yield return SceneManager.LoadSceneAsync("MainMenu");
        foreach(var p in saved)if(p.Value.HasValue)PlayerPrefs.SetInt(Key(p.Key),p.Value.Value);else PlayerPrefs.DeleteKey(Key(p.Key));PlayerPrefs.Save();
    }
}
