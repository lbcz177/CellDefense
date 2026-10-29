#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class CellCodexPlayModeTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Field(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static object Get(object o,string name)=>o.GetType().GetProperty(name,Flags).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==name && m.GetParameters().Length==args.Length).Invoke(o,args);
    static Component Menu()=>(Component)Object.FindObjectOfType(Runtime("MainMenuController"));

    [UnityTest]
    public IEnumerator BakedMenuEntryOpensFourteenUniquePagesAndBlocksUnderlyingActions()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu=Menu();Button entry=(Button)Field(menu,"codexEntry");
        yield return null;yield return null;
        Canvas.ForceUpdateCanvases();
        RectTransform rect=entry.GetComponent<RectTransform>();
        Vector2 screenPoint=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var pointer=new PointerEventData(EventSystem.current){position=screenPoint};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(entry));
        ExecuteEvents.Execute(entry.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
        Assert.That(Get(menu,"IsCodexOpen"),Is.True);
        object view=Field(menu,"cellCodex");Assert.That(Get(view,"EntryCount"),Is.EqualTo(14));
        Call(menu,"OpenSettings");Call(menu,"OpenLevelSelection");Call(menu,"QuitGame");
        Assert.That(Get(menu,"IsSettingsOpen"),Is.False);
        Assert.That(((GameObject)Field(menu,"selectionCanvas")).activeSelf,Is.False);
        Assert.That(Get(menu,"QuitRequested"),Is.False);
        HashSet<Sprite> pages=new HashSet<Sprite>();
        foreach(int category in new[]{0,1})
        {
            Call(view,"ShowCategory",category);
            Button[] buttons=(Button[])Field(view,"entryButtons");
            Assert.That(buttons.Length,Is.EqualTo(category==0?6:8));
            foreach(Button button in buttons)
            {
                button.onClick.Invoke();Canvas.ForceUpdateCanvases();
                Assert.That(Get(view,"IsDetailOpen"),Is.True);
                Image detail=(Image)Field(view,"detail");
                Assert.That(detail.sprite,Is.Not.Null);
                Assert.That(detail.sprite.rect.size,Is.EqualTo(new Vector2(1280,720)));
                Assert.That(pages.Add(detail.sprite),Is.True,"Each entry has a distinct complete page.");
            }
        }
        ((Button)Field(view,"nextButton")).onClick.Invoke();
        Assert.That(Get(view,"SelectedEntry"),Is.EqualTo(6),"Next wraps within the selected category.");
        Call(menu,"HandleEscape");Assert.That(Get(view,"IsDetailOpen"),Is.False);
        Assert.That(Get(menu,"IsCodexOpen"),Is.True);
        Call(menu,"HandleEscape");Assert.That(Get(menu,"IsCodexOpen"),Is.False);
        Assert.That(((CanvasGroup)Field(menu,"menuInteraction")).interactable,Is.True);
        Call(menu,"OpenSettings");Assert.That(Get(menu,"IsSettingsOpen"),Is.True);
        Call(menu,"CloseSettings");
    }

    [UnityTest]
    public IEnumerator IndexDetailsAndNavigationFitDesktopAndMobileSafeAreas()
    {
        foreach(Vector2Int size in new[]{new Vector2Int(1280,720),new Vector2Int(640,480),new Vector2Int(2048,928)})
        using(new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(size.x!=1280);
            yield return SceneManager.LoadSceneAsync("MainMenu");Component menu=Menu();Call(menu,"OpenCellCodex");yield return null;
            object view=Field(menu,"cellCodex");
            foreach(int category in new[]{0,1})
            {
                Call(view,"ShowCategory",category);yield return null;Canvas.ForceUpdateCanvases();
                MobilePlayModeTests.Inside((RectTransform)Field(view,"panel"));
                foreach(Button button in (Button[])Field(view,"entryButtons"))MobilePlayModeTests.Inside(button.GetComponent<RectTransform>());
                Capture($"codex-index-{category}-{size.x}x{size.y}.png");
                ((Button[])Field(view,"entryButtons"))[category==0?0:7].onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
                foreach(string name in new[]{"indexButton","closeButton","previousButton","nextButton"})MobilePlayModeTests.Inside(((Button)Field(view,name)).GetComponent<RectTransform>());
                Capture($"codex-detail-{category}-{size.x}x{size.y}.png");
            }
            Call(menu,"CloseCellCodex");
        }
    }

    static void Capture(string name)
    {
        string output=Environment.GetEnvironmentVariable("CELLDEFENSE_CODEX_CAPTURE_DIR");if(string.IsNullOrEmpty(output))return;
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);
        Directory.CreateDirectory(output);
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(output,name),false);
    }

    [UnityTearDown]public IEnumerator Restore()
    {
        Time.timeScale=1f;MobilePlayModeTests.Preview(false);
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
