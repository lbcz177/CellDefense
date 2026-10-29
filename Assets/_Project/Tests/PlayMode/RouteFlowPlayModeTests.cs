#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class RouteFlowPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethods(Flags).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);

    [Test]
    public void DashesRevealAndMoveForwardWithoutCuttingAcrossACorner()
    {
        GameObject go = new GameObject("Route geometry check");
        Material material = new Material(Shader.Find("Sprites/Default"));
        try
        {
            Component route = go.AddComponent(Runtime("RouteFlowRenderer"));
            Call(route, "Initialize", material);
            Call(route, "SetPath", new List<Vector3> { Vector3.zero, new Vector3(3,0), new Vector3(3,3) }, 1f, false);
            Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Call(route, "Render", .1f);
            Vector3[] early = mesh.vertices;
            Assert.That(early.Length, Is.GreaterThanOrEqualTo(16));
            Assert.That(early.Max(v => v.x), Is.LessThanOrEqualTo(1.201f), "The line must grow from the entrance.");
            float firstDash = early[4].x;
            Assert.That(early[8].x - early[7].x, Is.GreaterThan(.2f), "A visible gap separates dashes.");
            Call(route, "Render", .2f);
            Assert.That(mesh.vertices[4].x, Is.GreaterThan(firstDash), "Dashes travel toward the core.");
            Call(route, "Render", 3f);
            Vector3[] full = mesh.vertices;
            Assert.That(full.Max(v => v.y), Is.GreaterThan(2.5f));
            for (int i = 0; i < full.Length; i += 4)
            {
                Vector3 center = (full[i] + full[i+1] + full[i+2] + full[i+3]) * .25f;
                Assert.That(Mathf.Abs(center.y) < .001f || Mathf.Abs(center.x - 3f) < .001f, Is.True,
                    "Each dash stays on the route, including at the turn.");
            }
            Assert.That(mesh.colors32.Any(c => c.r > 240 && c.g > 190 && c.b < 130), Is.True);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(material); }
    }

    [UnityTest]
    public IEnumerator FiveScenesShowAnimatedRoutesAndPauseStopsTheirClock()
    {
        using (new ZzMobileLandscapePlayModeTests.Resolution(1280,720))
        {
            yield return null;
            foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                Component owner = (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
                object tutorial = Field(owner,"tutorial");
                if (tutorial != null) Call(tutorial,"Skip");
                Component view = (Component)Field(owner, "presentation");
                yield return new WaitForSeconds(2f);
                Component[] routes = view.GetComponentsInChildren(Runtime("RouteFlowRenderer")).Cast<Component>().ToArray();
                Assert.That(routes.Length, Is.GreaterThan(0), scene);
                foreach (Component route in routes)
                {
                    Assert.That(route.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0), scene);
                    Assert.That(route.GetComponent<MeshRenderer>().sortingOrder, Is.EqualTo(-12));
                }
                Component first = routes[0];
                float beforeRefresh = (float)Field(first,"elapsed");
                Call(owner,"RefreshPlanningRoutes");
                Assert.That((float)Field(first,"elapsed"), Is.EqualTo(beforeRefresh), "Unchanged paths must not restart on selection.");
                if (scene == "OpenFieldPrototype" || scene == "OpenFieldLevel03")
                {
                    OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, scene + "-yellow-routes.png");
                    yield return new WaitForSeconds(.2f);
                    OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, scene + "-yellow-routes-next.png");
                }
                Call(owner,"Pause");
                float paused = (float)Field(first,"elapsed");
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That((float)Field(first,"elapsed"), Is.EqualTo(paused));
                Call(owner,"Resume");
            }
        }
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
