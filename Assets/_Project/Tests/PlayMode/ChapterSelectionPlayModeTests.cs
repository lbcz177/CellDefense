#if UNITY_EDITOR
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
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

public sealed class ZzzzzChapterSelectionPlayModeTests
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    readonly Dictionary<string,int?> saved=new Dictionary<string,int?>();
    static readonly string[] Scenes={"OpenFieldPrototype","OpenFieldLevel02","OpenFieldLevel03","OpenFieldLevel04","OpenFieldLevel05"};
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static Component Menu()=>(Component)Object.FindObjectOfType(Runtime("MainMenuController"));
    static object Field(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static object Get(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);
    static string Key(string scene)=>"CellDefense.LevelProgress.v1."+scene+".BestScore";
    [SetUp] public void Preserve()
    {
        foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))).Concat(Enumerable.Range(1,4).Select(i=>"Chapter03Level"+i.ToString("00")))){string key=Key(scene);saved[key]=PlayerPrefs.HasKey(key)?(int?)PlayerPrefs.GetInt(key):null;PlayerPrefs.DeleteKey(key);}
    }
    [UnityTest] public IEnumerator ChapterHierarchyKeepsFirstChapterScoresAndStartsOnlyExplicitly()
    {
        PlayerPrefs.SetInt(Key(Scenes[0]),9600);
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Call(menu,"OpenLevelSelection");
        Assert.That(Get(menu,"IsChapterSelectionOpen"),Is.True);
        Assert.That(((Button[])Field(menu,"chapterButtons")).Length,Is.EqualTo(3));
        Call(menu,"OpenChapter",0);Call(menu,"OpenLevelDetails",0);
        Assert.That(Get(Field(menu,"detailsScore"),"text"),Is.EqualTo("9600"));
        Assert.That(Get(Field(menu,"detailsStars"),"Stars"),Is.EqualTo(3));
        Call(menu,"HandleEscape");Assert.That(Get(menu,"IsLevelDetailsOpen"),Is.False);
        Assert.That(Get(menu,"SelectedChapter"),Is.EqualTo(0));
        Call(menu,"HandleEscape");Assert.That(Get(menu,"IsChapterSelectionOpen"),Is.True);
        Call(menu,"HandleEscape");Assert.That(((GameObject)Field(menu,"selectionCanvas")).activeSelf,Is.False);
        Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",0);Call(menu,"OpenLevelDetails",1);
        ((Button)Field(menu,"detailsStart")).onClick.Invoke();yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(Scenes[1]));
        Assert.That(PlayerPrefs.GetInt(Key(Scenes[0])),Is.EqualTo(9600));
    }
    [UnityTest] public IEnumerator OpeningPagesDoesNotSelectFirstItemButHoverAndKeyboardStillHighlight()
    {
        foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))))PlayerPrefs.SetInt(Key(scene),100);
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Call(menu,"OpenLevelSelection");yield return null;
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null);
        Button[] chapters=(Button[])Field(menu,"chapterButtons");
        foreach(Button button in chapters)Assert.That(button.image.overrideSprite,Is.EqualTo(button.image.sprite));
        var pointer=new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(chapters[0].gameObject,pointer,ExecuteEvents.pointerEnterHandler);
        Assert.That(chapters[0].image.overrideSprite,Is.EqualTo(chapters[0].spriteState.highlightedSprite));
        ExecuteEvents.Execute(chapters[0].gameObject,pointer,ExecuteEvents.pointerExitHandler);
        Assert.That(chapters[0].image.overrideSprite,Is.EqualTo(chapters[0].image.sprite));
        Call(menu,"FocusSelectionForNavigation");
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(chapters[0].gameObject));
        for(int chapter=0;chapter<3;chapter++)
        {
            Call(menu,"OpenChapter",chapter);yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null);
            Button[] nodes=(Button[])Field(menu,"levelButtons");
            foreach(Button button in nodes.Where(b=>b.gameObject.activeSelf))
            {
                Image face=(Image)button.targetGraphic;
                Assert.That(face.overrideSprite,Is.EqualTo(face.sprite));
            }
            Call(menu,"FocusSelectionForNavigation");
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(nodes[0].gameObject));
            Call(menu,"OpenLevelDetails",0);
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null,"Opening details must not select Start automatically.");
            Button start=(Button)Field(menu,"detailsStart");
            Assert.That(start.image.overrideSprite,Is.EqualTo(start.image.sprite));
            if(start.interactable)
            {
                ExecuteEvents.Execute(start.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                Assert.That(start.image.overrideSprite,Is.EqualTo(start.spriteState.highlightedSprite));
                ExecuteEvents.Execute(start.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                Assert.That(start.image.overrideSprite,Is.EqualTo(start.image.sprite));
            }
            Call(menu,"FocusSelectionForNavigation");
            Button expected=start.interactable?start:(Button)Field(menu,"detailsBack");
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(expected.gameObject));
            Call(menu,"CloseLevelDetails");
            Call(menu,"ReturnToChapters");
        }
        Call(menu,"CloseLevelSelection");Call(menu,"OpenLevelSelection");
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null);
    }
    [UnityTest] public IEnumerator ThirdChapterRemainsLockedUntilPreviousChaptersAreCleared()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",2);
        Button[] nodes=(Button[])Field(menu,"levelButtons");
        Assert.That(nodes.Count(b=>b.gameObject.activeSelf),Is.EqualTo(4));
        Assert.That(nodes.Take(4).All(b=>!b.interactable),Is.True);
        for(int i=0;i<4;i++){nodes[i].onClick.Invoke();Assert.That(Get(menu,"IsLevelDetailsOpen"),Is.False);}
        Call(menu,"LoadLevel",0);Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
        foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))))PlayerPrefs.SetInt(Key(scene),100);
        Call(menu,"RefreshLevelProgress");Assert.That(nodes[0].interactable,Is.True);
        Call(menu,"OpenLevelDetails",0);Assert.That(Get(menu,"IsLevelDetailsOpen"),Is.True);
        Assert.That(((Component)Field(menu,"detailsPreviewNotice")).gameObject.activeSelf,Is.False);
        Assert.That(((Button)Field(menu,"detailsStart")).interactable,Is.True);
        Assert.That(PlayerPrefs.HasKey(Key("Chapter03Level01")),Is.False);
    }
    [UnityTest] public IEnumerator SequentialUnlocksBlockBypassDefeatAndUseVictoryRecordsImmediately()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",0);
        Button[] nodes=(Button[])Field(menu,"levelButtons");
        Assert.That(nodes.Select(b=>b.interactable),Is.EqualTo(new[]{true,false,false,false,false}));
        nodes[1].onClick.Invoke();Call(menu,"OpenLevelDetails",4);Call(menu,"LoadLevel",1);
        Assert.That(Get(menu,"IsLevelDetailsOpen"),Is.False);
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
        Capture("sequential-locked.png");
        Call(menu,"OpenLevelDetails",0);((Button)Field(menu,"detailsStart")).onClick.Invoke();yield return null;
        Component owner=(Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
        Call(owner,"FinishRun",Enum.Parse(Runtime("OpenFieldPhase"),"Defeat"));
        Call(owner,"ReturnToMainMenu");yield return null;
        menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",0);
        Assert.That(((Button[])Field(menu,"levelButtons"))[1].interactable,Is.False);
        Call(menu,"OpenLevelDetails",0);((Button)Field(menu,"detailsStart")).onClick.Invoke();yield return null;
        owner=(Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
        Call(owner,"FinishRun",Enum.Parse(Runtime("OpenFieldPhase"),"Victory"));
        Call(owner,"ReturnToMainMenu");yield return null;
        menu=Menu();Call(menu,"OpenLevelSelection");Call(menu,"OpenChapter",0);
        nodes=(Button[])Field(menu,"levelButtons");
        Assert.That(nodes.Select(b=>b.interactable),Is.EqualTo(new[]{true,true,false,false,false}));
        Call(menu,"OpenLevelDetails",1);
        int record=PlayerPrefs.GetInt(Key(Scenes[0]));PlayerPrefs.DeleteKey(Key(Scenes[0]));
        ((Button)Field(menu,"detailsStart")).onClick.Invoke();
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"),"Loading must recheck the prerequisite.");
        PlayerPrefs.SetInt(Key(Scenes[0]),record);
        ((Button)Field(menu,"detailsStart")).onClick.Invoke();yield return null;
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(Scenes[1]));
    }
    [UnityTest] public IEnumerator ChapterPagesAndDetailsFitRealWindowSizesAndUseCompleteArtwork()
    {
        foreach(string scene in Scenes.Concat(Enumerable.Range(1,4).Select(i=>"Chapter02Level"+i.ToString("00"))))PlayerPrefs.SetInt(Key(scene),100);
        foreach(Vector2Int size in new[]{new Vector2Int(1280,720),new Vector2Int(640,480),new Vector2Int(2048,928)})
        using(new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(size.x!=1280);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Component menu=Menu();Call(menu,"OpenLevelSelection");yield return null;
            Capture($"chapters-{size.x}x{size.y}.png");
            for(int chapter=0;chapter<3;chapter++)
            {
                Call(menu,"OpenChapter",chapter);yield return null;
                Canvas.ForceUpdateCanvases();
                MobilePlayModeTests.Inside((RectTransform)Field(menu,"selectionTitlePanel"));
                Capture($"chapter-{chapter+1}-{size.x}x{size.y}.png");
                Call(menu,"OpenLevelDetails",0);yield return null;Canvas.ForceUpdateCanvases();
                MobilePlayModeTests.Inside((RectTransform)Field(menu,"detailsPanel"));
                foreach(Graphic g in ((GameObject)Field(menu,"selectionCanvas")).GetComponentsInChildren<Graphic>())
                    if(g.isActiveAndEnabled&&g.GetType().Name=="PixelImageText")Assert.That(Get(g,"MissingGlyphCount"),Is.Zero,g.name);
                Capture($"details-{chapter+1}-{size.x}x{size.y}.png");
                Call(menu,"CloseLevelDetails");Call(menu,"ReturnToChapters");
            }
        }
    }
    [UnityTest] public IEnumerator AllThreeChapterCardsStayFullyVisibleAndClickableWithoutScrolling()
    {
        foreach(Vector2Int size in new[]{new Vector2Int(640,480),new Vector2Int(1280,720),new Vector2Int(2048,928)})
        using(new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        foreach(bool mobile in new[]{false,true})
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(mobile);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Component menu=Menu();Call(menu,"OpenLevelSelection");yield return null;
            Canvas.ForceUpdateCanvases();
            Button[] buttons=(Button[])Field(menu,"chapterButtons");
            Rect viewport=Bounds((RectTransform)Field(menu,"chapterViewport"));
            Assert.That(((ScrollRect)Field(menu,"chapterScroll")).vertical,Is.False);
            foreach(Button button in buttons)
            {
                Rect card=Bounds(button.GetComponent<RectTransform>());
                Assert.That(card.xMin,Is.GreaterThanOrEqualTo(viewport.xMin-.5f));
                Assert.That(card.xMax,Is.LessThanOrEqualTo(viewport.xMax+.5f));
                Assert.That(card.yMin,Is.GreaterThanOrEqualTo(viewport.yMin-.5f));
                Assert.That(card.yMax,Is.LessThanOrEqualTo(viewport.yMax+.5f));
                MobilePlayModeTests.Inside(button.GetComponent<RectTransform>());
                foreach(Button other in buttons)if(other!=button)
                    Assert.That(card.Overlaps(Bounds(other.GetComponent<RectTransform>())),Is.False);
            }
            Capture($"fit-{mobile}-{size.x}x{size.y}.png");
            for(int i=0;i<buttons.Length;i++)
            {
                var pointer=new PointerEventData(EventSystem.current){position=Bounds(buttons[i].GetComponent<RectTransform>()).center};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(buttons[i]));
                ExecuteEvents.Execute(buttons[i].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                Assert.That(Get(menu,"SelectedChapter"),Is.EqualTo(i));
                Call(menu,"ReturnToChapters");yield return null;Canvas.ForceUpdateCanvases();
            }
        }
    }
    static Rect Bounds(RectTransform rect)
    {
        Vector3[] corners=new Vector3[4];rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    static void Capture(string name)
    {
        string destination=Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_CAPTURE_DIR");if(string.IsNullOrEmpty(destination))return;
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);
        Directory.CreateDirectory(destination);
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(destination,name),false);
    }
    [UnityTearDown] public IEnumerator Restore()
    {
        foreach(var pair in saved)if(pair.Value.HasValue)PlayerPrefs.SetInt(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
        PlayerPrefs.Save();MobilePlayModeTests.Preview(false);Time.timeScale=1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
