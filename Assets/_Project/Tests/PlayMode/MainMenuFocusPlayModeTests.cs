using System;
using System.Collections;
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

public sealed class MainMenuFocusPlayModeTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static Component Menu()=>(Component)Object.FindObjectOfType(Runtime("MainMenuController"));
    static object Get(object o,string n)=>o.GetType().GetProperty(n,Flags).GetValue(o);
    static object Field(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Flags).Invoke(o,a);
    static Component Feedback(Button b)=>b.GetComponent(Runtime("MainMenuButtonFeedback"));
    static void Capture(string name)
    {
        string folder=Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_TWO_CAPTURE");if(string.IsNullOrEmpty(folder))return;
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,name);Directory.CreateDirectory(folder);
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork",name)),Path.Combine(folder,name),true);
    }
    [UnityTest] public IEnumerator PointerKeyboardAndTouchHaveOneClearFocusAndModalReturnDoesNotStick()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
        Component menu=Menu();Button[] buttons=(Button[])Field(menu,"mainMenuButtons");
        Assert.That(buttons.Length,Is.EqualTo(4));
        Assert.That(buttons[0].image.color.a,Is.Zero,"Keep the original baked blue button visible through its transparent hit area.");
        Assert.That(buttons[0].transform.Find("Start Caption"),Is.Null,"Do not replace the original lettering.");
        Assert.That(buttons.All(b=>!(bool)Get(Feedback(b),"IsHighlighted")),Is.True);
        var pointer=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left};
        foreach(Button button in buttons)
        {
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            Assert.That(Get(Feedback(button),"IsHighlighted"),Is.True);
            Assert.That(buttons.Count(b=>(bool)Get(Feedback(b),"IsHighlighted")),Is.EqualTo(1));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Assert.That(Get(Feedback(button),"IsPressed"),Is.True);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            Assert.That(Get(Feedback(button),"IsPressed"),Is.False);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            Assert.That(Get(Feedback(button),"IsHighlighted"),Is.False);
        }
        Call(menu,"NavigateMainMenu",1);
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(buttons[0].gameObject));
        Call(menu,"NavigateMainMenu",-1);
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(buttons[3].gameObject));
        ExecuteEvents.Execute(buttons[2].gameObject,pointer,ExecuteEvents.pointerEnterHandler);
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.Null);
        Assert.That(buttons.Count(b=>(bool)Get(Feedback(b),"IsHighlighted")),Is.EqualTo(1));
        Call(menu,"OpenSettings");yield return null;
        Assert.That(buttons.All(b=>!(bool)Get(Feedback(b),"IsHighlighted")),Is.True);
        Call(menu,"CloseSettings");yield return null;
        Assert.That(buttons.All(b=>!(bool)Get(Feedback(b),"IsHighlighted")),Is.True);
        pointer.pointerId=0;
        ExecuteEvents.Execute(buttons[1].gameObject,pointer,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(buttons[1].gameObject,pointer,ExecuteEvents.pointerUpHandler);
        Assert.That(Get(Feedback(buttons[1]),"IsHighlighted"),Is.False);
        Call(menu,"NavigateMainMenu",1);Call(menu,"OpenLevelSelection");Call(menu,"CloseLevelSelection");
        Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(buttons[0].gameObject));
        Assert.That(Get(Feedback(buttons[0]),"IsHighlighted"),Is.True);
    }
    [UnityTest] public IEnumerator FeedbackFitsHitAreasAndDoesNotInterceptClicksAcrossLandscapeSizes()
    {
        foreach(Vector2Int size in new[]{new Vector2Int(640,480),new Vector2Int(1280,720),new Vector2Int(2048,928)})
        using(new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        {
            yield return null;yield return null;MobilePlayModeTests.Preview(size.x!=1280);
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            Component menu=Menu();Button[] buttons=(Button[])Field(menu,"mainMenuButtons");
            var pointer=new PointerEventData(EventSystem.current){pointerId=-1};
            foreach(Button b in buttons)
            {
                ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                Canvas.ForceUpdateCanvases();MobilePlayModeTests.Inside(b.GetComponent<RectTransform>());
                Graphic frame=b.GetComponentInChildren(Runtime("MainMenuFocusGraphic")) as Graphic;
                Assert.That(frame.raycastTarget,Is.False);Assert.That(Get(Feedback(b),"IsHighlighted"),Is.True);
                Assert.That(frame.canvasRenderer,Is.Not.Null);
                Mesh mesh=frame.canvasRenderer.GetMesh();
                Assert.That(mesh,Is.Not.Null);
                Assert.That(mesh.vertexCount,Is.GreaterThan(0),"Enabled feedback must actually draw a mesh.");
            }
            Capture($"menu-focus-{size.x}x{size.y}.png");
        }
    }
    [UnityTearDown] public IEnumerator Restore()
    {
        MobilePlayModeTests.Preview(false);Time.timeScale=1f;yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
