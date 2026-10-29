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
using Object = UnityEngine.Object;

public sealed class ChapterTwoPlayModeTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string[] First = { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" };
    static readonly string[] Second = { "Chapter02Level01", "Chapter02Level02", "Chapter02Level03", "Chapter02Level04" };
    readonly Dictionary<string, int?> saved = new Dictionary<string, int?>();
    readonly List<Object> temporary = new List<Object>();
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static Component Owner() => (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    static Component Menu() => (Component)Object.FindObjectOfType(Runtime("MainMenuController"));
    static object Get(object obj, string name) => obj.GetType().GetProperty(name, Flags).GetValue(obj);
    static object Field(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethods(Flags)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(obj, args);
    static string Key(string scene) => "CellDefense.LevelProgress.v1." + scene + ".BestScore";
    static Component[] Enemies(Component owner) => ((IEnumerable)Field(owner, "activeEnemies")).Cast<Component>().ToArray();
    static void Damage(Component enemy, float amount) => enemy.GetType().GetMethod("TakeDamage", new[] { typeof(float) }).Invoke(enemy, new object[] { amount });
    static Component Build(Component owner, Vector2Int cell, int index)
    {
        Call(owner,"SelectTower", index);
        Assert.That(Call(owner,"TryBuildAt", cell), Is.True, $"Build {index} at {cell}");
        return ((IEnumerable)Get(owner,"Defenders")).Cast<Component>().Single(x => (Vector2Int)Get(x,"Cell") == cell);
    }
    static void Capture(string name)
    {
        string folder = Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_TWO_CAPTURE");
        if (string.IsNullOrEmpty(folder)) return;
        Canvas.ForceUpdateCanvases();OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);
        Directory.CreateDirectory(folder);
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(folder,name),true);
    }
    [SetUp] public void Preserve()
    {
        foreach(string scene in First.Concat(Second))
        {
            string key=Key(scene);saved[key]=PlayerPrefs.HasKey(key)?(int?)PlayerPrefs.GetInt(key):null;PlayerPrefs.DeleteKey(key);
        }
    }
    [UnityTest] public IEnumerator ChapterBridgeLocksSequentiallyAndAllFourRealWaveSequencesSaveScores()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",1);
        Button[] nodes=(Button[])Field(menu,"levelButtons");
        Assert.That(nodes.Take(4).All(b=>!b.interactable),Is.True);
        Call(menu,"OpenLevelDetails",0);Call(menu,"LoadLevel",0);
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
        foreach(string scene in First.Take(4))PlayerPrefs.SetInt(Key(scene),100);
        yield return SceneManager.LoadSceneAsync(First.Last());Component owner=Owner();
        Call(owner,"FinishRun",Enum.Parse(Runtime("OpenFieldPhase"),"Victory"));Call(owner,"LoadNextLevel");yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(Second[0]));
        int[] counts={5,5,6,7};
        for(int level=0;level<Second.Length;level++)
        {
            owner=Owner();Assert.That(Get(owner,"TotalRounds"),Is.EqualTo(counts[level]));
            Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
            var rounds=new HashSet<int>();Call(owner,"StartAssault");
            float deadline=Time.realtimeSinceStartup+60f;
            while((bool)Get(owner,"HasActiveRun") && Time.realtimeSinceStartup<deadline)
            {
                Time.timeScale=20f;rounds.Add((int)Get(owner,"CurrentRound"));
                foreach(Component enemy in Enemies(owner))Damage(enemy,1000000f);
                yield return null;
            }
            Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Victory"),Second[level]);
            Assert.That(rounds,Is.EquivalentTo(Enumerable.Range(1,counts[level])));
            Assert.That(PlayerPrefs.GetInt(Key(Second[level])),Is.EqualTo(10000));
            if(level<3){Call(owner,"LoadNextLevel");yield return null;Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(Second[level+1]));}
            else Assert.That(((GameObject)Field(owner,"victoryPanel")).GetComponentsInChildren<Button>().Any(b=>b.name=="Next"),Is.True,"The second chapter now leads to chapter three.");
        }
        yield return SceneManager.LoadSceneAsync("MainMenu");menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",1);
        nodes=(Button[])Field(menu,"levelButtons");Assert.That(nodes.Take(4).All(b=>b.interactable),Is.True);
        Call(menu,"OpenLevelDetails",3);Assert.That(Get(Field(menu,"detailsScore"),"text"),Is.EqualTo("10000"));
        Assert.That(Get(Field(menu,"detailsStars"),"Stars"),Is.EqualTo(3));Capture("chapter-two-cleared-details.png");
        Call(menu,"CloseLevelDetails");Call(menu,"ReturnToChapters");Call(menu,"OpenChapter",2);Call(menu,"OpenLevelDetails",0);
        Assert.That(((Button)Field(menu,"detailsStart")).interactable,Is.True);
    }
    [UnityTest] public IEnumerator ExplicitSecondaryEntranceControlsButtonsAndActualSpawnPath()
    {
        yield return SceneManager.LoadSceneAsync(Second[1]);Component owner=Owner();
        Object original=(Object)Call(Field(owner,"waveSequence"),"GetWave",3);
        Object wave=Object.Instantiate(original);temporary.Add(wave);
        Array groups=(Array)Field(wave,"groups");Array single=Array.CreateInstance(groups.GetType().GetElementType(),1);single.SetValue(groups.GetValue(0),0);Set(wave,"groups",single);
        var sequence=ScriptableObject.CreateInstance(Runtime("WaveSequenceDefinition"));temporary.Add(sequence);
        Array waves=Array.CreateInstance(Runtime("WaveDefinition"),1);waves.SetValue(wave,0);Set(sequence,"waves",waves);Set(owner,"waveSequence",sequence);
        Assert.That(Call(owner,"UpcomingUsesEntrance",0),Is.False);Assert.That(Call(owner,"UpcomingUsesEntrance",1),Is.True);
        Call(Field(owner,"entranceWaveButtons"),"Refresh");
        Button[] buttons=(Button[])Field(Field(owner,"entranceWaveButtons"),"buttons");
        Assert.That(buttons[0].gameObject.activeSelf,Is.False);Assert.That(buttons[1].gameObject.activeSelf,Is.True);
        Call(owner,"StartAssault");
        Vector3 expected=(Vector3)Call(owner,"CellToWorld",new Vector2Int(0,2));
        Assert.That(Vector3.Distance(Enemies(owner).Single().transform.position,expected),Is.LessThan(.001f));
    }
    [UnityTest] public IEnumerator RepairTowersShareWallLifetimeBudgetAndNeverHealOtherCells()
    {
        yield return SceneManager.LoadSceneAsync(Second[0]);Component owner=Owner();
        Component wall=Build(owner,new Vector2Int(3,3),3),repair=Build(owner,new Vector2Int(2,3),6),guard=Build(owner,new Vector2Int(2,4),0);
        Call(wall,"TakeDamage",400f);Call(guard,"TakeDamage",50f);
        yield return new WaitForSeconds(.1f);
        Assert.That((float)Get(wall,"CurrentHealth"),Is.GreaterThan(100f));
        Assert.That(Get(guard,"CurrentHealth"),Is.EqualTo(50f));
        float before=(float)Get(wall,"CurrentHealth");Call(owner,"Pause");yield return new WaitForSecondsRealtime(.15f);
        Assert.That(Get(wall,"CurrentHealth"),Is.EqualTo(before));Call(owner,"Resume");
        Build(owner,new Vector2Int(2,2),6);
        Time.timeScale=10f;yield return new WaitForSeconds(14f);Time.timeScale=1f;
        Assert.That(Get(wall,"TotalRepaired"),Is.EqualTo(300f));Assert.That(Get(wall,"CurrentHealth"),Is.EqualTo(400f));
        Assert.That(Call(wall,"Repair",25f),Is.EqualTo(0f));Assert.That(Call(guard,"Repair",25f),Is.EqualTo(0f));
        Capture("chapter-two-repair.png");
        Call(wall,"TakeDamage",1000f);yield return null;
        Assert.That(((IEnumerable)Get(owner,"Defenders")).Cast<Component>().Contains(wall),Is.False);
    }
    [UnityTest] public IEnumerator ComplementLosesProgressOutOfRangeFreezesOnPauseAndRespectsShieldAndMark()
    {
        yield return SceneManager.LoadSceneAsync(Second[2]);Component owner=Owner();
        Component defender=Build(owner,new Vector2Int(2,5),7);
        Component tower=(Component)Get(defender,"Tower");Component behavior=tower.GetComponent(Runtime("ComplementAttackBehaviour"));
        Set(behavior,"assemblySeconds",.8f);Call(owner,"StartAssault");
        Component enemy=Enemies(owner).Single();((Behaviour)enemy.GetComponent(Runtime("PathFollower"))).enabled=false;
        enemy.transform.position=tower.transform.position+Vector3.right;Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.15f);Assert.That((float)Get(behavior,"AssemblyProgress"),Is.GreaterThan(0f));
        Call(owner,"Pause");float progress=(float)Get(behavior,"AssemblyProgress");yield return new WaitForSecondsRealtime(.15f);
        Assert.That(Get(behavior,"AssemblyProgress"),Is.EqualTo(progress));Call(owner,"Resume");
        enemy.transform.position+=Vector3.right*10f;yield return null;
        Assert.That(Get(behavior,"AssemblyProgress"),Is.EqualTo(0f));
        enemy.transform.position=tower.transform.position+Vector3.right;Physics2D.SyncTransforms();
        Call(enemy.GetComponent(Runtime("EnemyImmuneState")),"ApplyMark",5f);Call(enemy,"GrantShield",100f);
        yield return new WaitForSeconds(.45f);
        Assert.That(Get(enemy,"CurrentShield"),Is.EqualTo(0f));
        Assert.That((float)Get(enemy,"CurrentHealth"),Is.EqualTo(106f).Within(.1f));
        Capture("chapter-two-complement.png");
    }
    [UnityTest] public IEnumerator MapsAndEightTowerMenuFitDesktopAndMobileWithCompleteGlyphs()
    {
        using(new ZzMobileLandscapePlayModeTests.Resolution(1280,720))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(false);
            foreach(string scene in Second)
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return new WaitForSeconds(.2f);
                Component owner=Owner();object grid=Field(owner,"grid");
                var path=new List<Vector2Int>();
                foreach(Vector2Int entry in (Vector2Int[])Field(owner,"spawnCells"))Assert.That(Call(grid,"TryGetPath",entry,path),Is.True);
                Call(Field(owner,"entranceWaveButtons"),"Refresh");
                Capture(scene+"-planning.png");
            }
        }
        foreach(Vector2Int size in new[]{new Vector2Int(640,480),new Vector2Int(1280,720),new Vector2Int(2048,928)})
        using(new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(true);
            Assert.That(Screen.width,Is.EqualTo(size.x));Assert.That(Screen.height,Is.EqualTo(size.y));
            yield return SceneManager.LoadSceneAsync(Second.Last());yield return null;yield return null;
            Component owner=Owner();Assert.That(Call(owner,"SelectBuildCell",new Vector2Int(11,4)),Is.True);
            yield return null;Canvas.ForceUpdateCanvases();
            Component menu=(Component)Field(owner,"buildMenu");
            Assert.That(((Array)Field(menu,"buttons")).Length,Is.EqualTo(8));MobilePlayModeTests.Inside((RectTransform)Get(menu,"PanelRect"));
            foreach(Component label in (Array)Field(menu,"labels"))Assert.That(Get(label,"MissingGlyphCount"),Is.EqualTo(0));
            Capture($"chapter-two-menu-{size.x}x{size.y}.png");Call(owner,"ClearBuildSelection");
        }
    }
    [UnityTest] public IEnumerator ResourceFundedDefensesCanCompleteAllFourChaptersWithoutDirectDamage()
    {
        foreach(string scene in Second)
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
                .ThenBy(c=>Vector2Int.Distance(c,new Vector2Int(12,5))).ToList();
            int[] choices=scene==Second[0]||scene==Second[1] ? new[]{6,0,4,0,4,0,1,0,4,0,0,4,0,0}
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
            Debug.Log($"CHAPTER_TWO_DEFENSE {scene}: {Get(owner,"Phase")}, life={Get(owner,"CurrentCoreLife")}, atp={Get(owner,"CurrentATP")}, built={next}");
            Capture(scene+"-defense-result.png");
            Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Victory"),scene);
        }
    }
    [UnityTearDown] public IEnumerator Restore()
    {
        Time.timeScale=1f;MobilePlayModeTests.Preview(false);yield return SceneManager.LoadSceneAsync("MainMenu");
        foreach(Object asset in temporary)Object.Destroy(asset);temporary.Clear();
        foreach(var pair in saved)if(pair.Value.HasValue)PlayerPrefs.SetInt(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
        PlayerPrefs.Save();
    }
}
