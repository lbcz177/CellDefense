using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public class PixelRefinementVisualTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    object Field(object obj,string name)=>obj.GetType().GetField(name,Flags).GetValue(obj);
    object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Flags).Invoke(obj,args);

    [UnityTest]
    public IEnumerator SixBuildIconsMatchBattleCellsAndRenderWithCurrentHud()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,"refined-main-menu.png");
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03");
        var owner=(Component)Object.FindObjectOfType(Type.GetType("OpenFieldPrototypeController, Assembly-CSharp",true));
        Assert.That(Call(owner,"SelectBuildCell",new Vector2Int(7,5)),Is.True);
        yield return null;
        Canvas.ForceUpdateCanvases();
        var menu=Field(owner,"buildMenu");
        var icons=(Image[])Field(menu,"icons");
        Assert.That(icons.Length,Is.EqualTo(6));
        foreach(var icon in icons)
        {
            Assert.That(icon.sprite,Is.Not.Null);
            Assert.That(icon.sprite.rect.size,Is.EqualTo(new Vector2(128,128)));
            Assert.That(icon.sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));
        }
        OpenFieldPlaybackPlayModeTests.CaptureUi((Camera)Field(owner,"gameplayCamera"),"refined-build-menu.png");
        Call(owner,"ClearSelection");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale=1;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
