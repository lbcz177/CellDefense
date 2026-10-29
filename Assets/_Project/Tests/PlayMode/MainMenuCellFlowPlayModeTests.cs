#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MainMenuCellFlowPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator CellsMoveWhileTitleButtonsAndArtworkStayFixedAtDesktopAndMobileSizes()
    {
        foreach (Vector2Int size in new[] { new Vector2Int(1280,720), new Vector2Int(640,480), new Vector2Int(2048,928) })
        using (new ZzMobileLandscapePlayModeTests.Resolution(size.x,size.y))
        {
            yield return null; yield return null;
            MobilePlayModeTests.Preview(size.x != 1280);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Type type=Type.GetType("MainMenuCellFlow, Assembly-CSharp",true);
            Component flow=(Component)Object.FindObjectOfType(type);
            Assert.That(flow,Is.Not.Null);
            Material material=(Material)type.GetField("instance",Flags).GetValue(flow);
            Image image=flow.GetComponent<Image>();
            Component stream=(Component)Object.FindObjectOfType(Type.GetType("MainMenuBacteriaFlow, Assembly-CSharp",true));
            Assert.That(stream,Is.Not.Null);
            MethodInfo renderStream=stream.GetType().GetMethod("RenderAt");
            Component defense=(Component)Object.FindObjectOfType(Type.GetType("MainMenuDefenseEffects, Assembly-CSharp",true));
            Assert.That(defense,Is.Not.Null);
            Graphic effects=defense.GetComponent<Graphic>();
            Assert.That(effects.raycastTarget,Is.False);
            MethodInfo renderDefense=defense.GetType().GetMethod("RenderAt");
            {
                renderDefense.Invoke(defense,new object[]{.15f});Canvas.ForceUpdateCanvases();
                Mesh actionMesh=effects.canvasRenderer.GetMesh();
                Assert.That(actionMesh,Is.Not.Null);
                Vector3[] before=actionMesh.vertices;
                Assert.That(before.Length,Is.GreaterThan(0));
                renderDefense.Invoke(defense,new object[]{.8f});Canvas.ForceUpdateCanvases();
                actionMesh=effects.canvasRenderer.GetMesh();
                Vector3[] after=actionMesh.vertices;
                bool moved=before.Length!=after.Length;
                for(int i=0;i<Mathf.Min(before.Length,after.Length);i++)moved|=(before[i]-after[i]).sqrMagnitude>.01f;
                Assert.That(moved,Is.True,"Defense particles must travel rather than remain static.");
                foreach(Vector3 vertex in after)
                    Assert.That(vertex.x,Is.GreaterThan(effects.rectTransform.rect.xMin+effects.rectTransform.rect.width*.46f),"Actions must stay clear of the menu buttons.");
            }
            Image[] germs=(Image[])stream.GetType().GetField("bacteria",Flags).GetValue(stream);
            Assert.That(germs.Length,Is.EqualTo(10));
            renderStream.Invoke(stream,new object[]{0f});
            Vector2[] start=new Vector2[germs.Length];
            Vector2[] sizes=new Vector2[germs.Length];
            for(int i=0;i<germs.Length;i++)
            {
                Assert.That(germs[i].sprite.name,Does.EndWith("_重绘"));
                Assert.That(germs[i].raycastTarget,Is.False);
                start[i]=germs[i].rectTransform.anchoredPosition;
                sizes[i]=germs[i].rectTransform.sizeDelta;
            }
            renderStream.Invoke(stream,new object[]{1.2f});
            for(int i=0;i<germs.Length;i++)
            {
                Assert.That(Vector2.Distance(start[i],germs[i].rectTransform.anchoredPosition),Is.GreaterThan(8f),"Bacteria must travel along the channel.");
                Assert.That(germs[i].rectTransform.sizeDelta,Is.EqualTo(sizes[i]),"Bodies must not stretch while flowing.");
            }
            Assert.That(material,Is.Not.Null);
            Assert.That(material.shader.isSupported,Is.True);
            Assert.That(image.sprite.name,Is.EqualTo("胞卫战_主界面_v1"));
            Assert.That(image.raycastTarget,Is.False);
            // Render two deterministic moments of the actual shader, rather than only checking its clock.
            material.SetFloat("_FlowTime",0f);
            string first=$"menu-flow-{size.x}x{size.y}-0.png";
            OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,first);
            material.SetFloat("_FlowTime",2.7f);
            string second=$"menu-flow-{size.x}x{size.y}-5.png";
            OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,second);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PauseArtwork"));
            Texture2D a=new Texture2D(2,2),b=new Texture2D(2,2);
            try
            {
                a.LoadImage(File.ReadAllBytes(Path.Combine(folder,first)));
                b.LoadImage(File.ReadAllBytes(Path.Combine(folder,second)));
                Color32[] p=a.GetPixels32(),q=b.GetPixels32();
                Vector3[] corners=new Vector3[4];image.rectTransform.GetWorldCorners(corners);
                int changed=0,stable=0;
                for(int y=Mathf.Max(0,(int)corners[0].y);y<Mathf.Min(a.height,(int)corners[2].y);y++)
                for(int x=Mathf.Max(0,(int)corners[0].x);x<Mathf.Min(a.width,(int)corners[2].x);x++)
                {
                    float u=(x-corners[0].x)/(corners[2].x-corners[0].x);
                    int index=y*a.width+x;
                    bool same=p[index].Equals(q[index]);
                    float v=(y-corners[0].y)/(corners[2].y-corners[0].y);
                    if(u<.38f || (u<.445f && v>.65f)) { Assert.That(same,Is.True,"Baked button/title pixels must remain stationary.");stable++; }
                    else if(!same)changed++;
                }
                Assert.That(stable,Is.GreaterThan(1000));
                Assert.That(changed,Is.GreaterThan(300),"The cell regions must visibly move in the actual render.");
            }
            finally { Object.Destroy(a);Object.Destroy(b); }
            if (size.x == 1280 && Environment.GetEnvironmentVariable("CELLDEFENSE_MENU_FLOW_FRAMES") == "1")
            using (new ZzMobileLandscapePlayModeTests.Resolution(640,360))
            {
                yield return null;yield return null;
                for (int frame=0;frame<180;frame++)
                {
                    material.SetFloat("_FlowTime",frame*.03f);
                    renderStream.Invoke(stream,new object[]{frame*.03f});
                    renderDefense.Invoke(defense,new object[]{frame*.03f});
                    Canvas.ForceUpdateCanvases();
                    OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,$"blue-defense-actions-{frame:D3}.png");
                }
            }
            float clock=(float)type.GetField("elapsed",Flags).GetValue(flow);
            Time.timeScale=0f;
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That((float)type.GetField("elapsed",Flags).GetValue(flow),Is.GreaterThan(clock),"Menu ambient motion uses unscaled time.");
            Time.timeScale=1f;
        }
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale=1f;
        MobilePlayModeTests.Preview(false);
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
