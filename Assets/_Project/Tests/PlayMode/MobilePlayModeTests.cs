#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static MobilePlayModeTests;

public sealed class MobilePlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    internal static object Field(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    internal static object Get(object obj, string name) => obj.GetType().GetProperty(name, Flags).GetValue(obj);
    internal static object Call(object obj, string name, params object[] args) =>
        obj.GetType().GetMethods(Flags).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(obj, args);
    internal static Component Find(string name) => Object.FindObjectOfType(Runtime(name)) as Component;
    internal static void Preview(bool value)
    {
        Runtime("MobileDisplay").GetField("PreviewEnabled").SetValue(null, value);
        Runtime("MobileDisplay").GetField("PreviewSafeArea").SetValue(null,
            value ? (object)new Rect(Screen.width * .06f, Screen.height * .04f, Screen.width * .88f, Screen.height * .92f) : null);
    }
    internal static Rect Safe => (Rect)Runtime("MobileDisplay").GetProperty("SafeArea").GetValue(null);
    internal static void Inside(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(Safe.xMin - 2));
        Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(Safe.yMin - 2));
        Assert.That(corners[2].x, Is.LessThanOrEqualTo(Safe.xMax + 2));
        Assert.That(corners[2].y, Is.LessThanOrEqualTo(Safe.yMax + 2));
    }

    [UnityTest]
    public IEnumerator MenuHitAreasFollowArtworkSettingsHideDesktopDisplayAndBackClosesOverlay()
    {
        Preview(true);
        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        Canvas.ForceUpdateCanvases();
        Component menu = Find("MainMenuController");
        Transform content = (Transform)Field(menu, "menuContent");
        Inside(content as RectTransform);
        Button settings = (Button)Field(menu, "settingsEntry");
        PointerEventData pointer = new PointerEventData(EventSystem.current) {
            position = settings.transform.TransformPoint(settings.GetComponent<RectTransform>().rect.center)
        };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits[0].gameObject, Is.EqualTo(settings.gameObject));
        ExecuteEvents.Execute(settings.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        yield return null;
        Component panel = (Component)Field(menu, "settingsPanel");
        Inside((RectTransform)Field(panel, "panel"));
        Assert.That(((Toggle)Field(panel, "fullscreen")).gameObject.activeSelf, Is.False);
        Call(menu, "HandleEscape");
        Call(menu, "OpenLevelSelection");
        yield return null;
        Inside((RectTransform)Field(menu, "selectionPanel"));
        Call(menu, "HandleEscape");
        Assert.That(((GameObject)Field(menu, "selectionCanvas")).activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator FiveMapsKeepBoardAndActionMenusInSafeAreaAndTouchCommitsOnlyOnce()
    {
        Preview(true);
        foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Component owner = Find("OpenFieldPrototypeController");
            Camera camera = (Camera)Field(owner, "gameplayCamera");
            Rect viewport = camera.pixelRect;
            Assert.That(viewport.xMin, Is.GreaterThanOrEqualTo(Safe.xMin - 1));
            Assert.That(viewport.yMax, Is.LessThan(Safe.yMax));
            Assert.That(camera.aspect, Is.EqualTo(viewport.width / viewport.height).Within(.002f));
            Vector2Int cell = Vector2Int.zero;
            Vector2 position = Vector2.zero;
            bool found = false;
            object grid = Field(owner, "grid");
            for (int x = 2; x < 13 && !found; x++)
                for (int y = 2; y < 9 && !found; y++)
                {
                    cell = new Vector2Int(x, y);
                    position = camera.WorldToScreenPoint((Vector3)Call(owner, "CellToWorld", cell));
                    found = Call(grid, "CheckPlacement", cell, false).ToString() == "Valid" &&
                        !(bool)Call(owner, "IsBoardPointerBlocked", position);
                }
            Assert.That(found, Is.True);
            Assert.That(Call(owner, "ProcessBoardTouch", 1, 12, TouchPhase.Began, position), Is.False);
            Assert.That(Call(owner, "ProcessBoardTouch", 1, 12, TouchPhase.Ended, position), Is.True);
            Assert.That(Call(owner, "ProcessBoardTouch", 1, 12, TouchPhase.Ended, position), Is.False);
            object build = Field(owner, "buildMenu");
            Inside((RectTransform)Field(build, "panelRect"));
            int atp = (int)Get(owner, "CurrentATP");
            Call(owner, "TryBuildSelectedChoice", 0);
            Assert.That((int)Get(owner, "CurrentATP"), Is.EqualTo(atp - 25));
            Call(owner, "SelectPlacedTower", cell);
            object tower = Field(owner, "towerMenu");
            Inside((RectTransform)Field(tower, "panelRect"));
            Call(tower, "RequestSell");
            Canvas.ForceUpdateCanvases();
            GameObject confirmation = (GameObject)Field(tower, "confirmation");
            // Dialog must remain accessible even with a notch.
            Inside(confirmation.transform.Find("Dialog") as RectTransform);
            Call(tower, "CancelSell");
            Call(owner, "ClearSelection");
            Vector2 off = new Vector2(0, 0);
            Call(owner, "ProcessBoardTouch", 1, 1, TouchPhase.Began, off);
            Assert.That(Call(owner, "ProcessBoardTouch", 1, 1, TouchPhase.Ended, off), Is.False);
            Assert.That(Field(owner, "selectedBuildCell"), Is.Null);
            Call(owner, "FinishRun", Enum.Parse(Runtime("OpenFieldPhase"), "Victory"));
            yield return null;
            Canvas.ForceUpdateCanvases();
            GameObject victory = (GameObject)Field(owner, "victoryPanel");
            Transform frame = victory.transform.Find("Canvas/Mobile Safe Content");
            Inside((RectTransform)frame);
            Button next = frame.Find("Next").GetComponent<Button>();
            Assert.That(next.gameObject.activeSelf, Is.EqualTo(scene != "OpenFieldLevel05"));
            Inside(frame.Find("Again") as RectTransform);
            Inside(frame.Find("Return") as RectTransform);
        }
    }

    [UnityTest]
    public IEnumerator BackgroundDuringCombatKeepsRoundSpeedAndRequiresManualResume()
    {
        Preview(true);
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Find("OpenFieldPrototypeController");
        Call(owner, "SetPlaybackSpeed", 2);
        Call(owner, "StartAssault");
        yield return null;
        int round = (int)Get(owner, "CurrentRound");
        Call(owner, "OnApplicationPause", true);
        yield return null;
        Assert.That(Get(owner, "IsPaused"), Is.True);
        Assert.That(Get(owner, "CurrentRound"), Is.EqualTo(round));
        Call(owner, "OnApplicationPause", false);
        Call(owner, "OnApplicationFocus", true);
        yield return null;
        Assert.That(Get(owner, "IsPaused"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        object controls = Field(owner, "playbackControls");
        Inside((RectTransform)Field(controls, "pausePanel"));
        Call(owner, "Resume");
        Assert.That(Time.timeScale, Is.EqualTo(2f));
        Assert.That(Get(owner, "Phase").ToString(), Is.EqualTo("Assault"));
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Preview(false);
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}

// Keep the Game view-changing fixture last in an unfiltered Editor run.
public sealed class ZzMobileLandscapePlayModeTests
{
    // This helper only runs in Editor tests, against Unity 2022.3's Game view API.
    internal sealed class Resolution : IDisposable
    {
        private readonly UnityEditor.EditorWindow view;
        private readonly object group;
        private readonly int previous, added;
        public Resolution(int width, int height)
        {
            Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            object sizes = sizesType.BaseType.GetProperty("instance").GetValue(null);
            group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            added = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            Type kind = assembly.GetType("UnityEditor.GameViewSizeType", true);
            object size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize", true),
                Enum.Parse(kind, "FixedResolution"), width, height, "CellDefense mobile test");
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            view = UnityEditor.EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView", true));
            previous = (int)view.GetType().GetProperty("selectedSizeIndex").GetValue(view);
            view.GetType().GetMethod("SizeSelectionCallback").Invoke(view, new object[] { added, null });
            view.Repaint();
        }
        public void Dispose()
        {
            view.GetType().GetMethod("SizeSelectionCallback").Invoke(view, new object[] { previous, null });
            group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { added });
            view.Repaint();
        }
    }

    [UnityTest, Category("MobileLandscape")]
    public IEnumerator MobileViewportErasesPreviousScenePixelsAcrossFiveMapsAndSafeAreaChanges()
    {
        Assert.That(SystemInfo.graphicsDeviceType,
            Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null), "This regression requires real GPU rendering.");
        foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(2048, 928) })
        {
            using (new Resolution(size.x, size.y))
            {
                yield return null;
                yield return null;
                Assert.That(Screen.width, Is.EqualTo(size.x));
                Assert.That(Screen.height, Is.EqualTo(size.y));
                Preview(true);
                yield return SceneManager.LoadSceneAsync("MainMenu");
                Call(Find("MainMenuController"), "OpenLevelSelection");
                yield return null;
                foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02",
                    "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
                {
                    yield return SceneManager.LoadSceneAsync(scene);
                    yield return null;
                    Component owner = Find("OpenFieldPrototypeController");
                    Camera camera = (Camera)Field(owner, "gameplayCamera");
                    GameObject seedObject = new GameObject("Prior scene pixel sentinel", typeof(Camera));
                    Camera seed = seedObject.GetComponent<Camera>();
                    seed.depth = camera.depth - 2f;
                    seed.clearFlags = CameraClearFlags.SolidColor;
                    seed.backgroundColor = Color.magenta;
                    seed.cullingMask = 0;
                    seed.allowHDR = false;
                    seed.allowMSAA = false;
                    // HDR world cameras use a viewport-sized intermediate target. Read only
                    // after it resolves, from a full-display camera that draws and clears nothing.
                    GameObject probeObject = new GameObject("Display pixel readback", typeof(Camera));
                    Camera probeCamera = probeObject.GetComponent<Camera>();
                    probeCamera.depth = camera.depth + 1f;
                    probeCamera.clearFlags = CameraClearFlags.Nothing;
                    probeCamera.cullingMask = 0;
                    probeCamera.allowHDR = false;
                    probeCamera.allowMSAA = false;
                    Texture2D pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    int captures = 0;
                    Exception readError = null;
                    Camera.CameraCallback readback = rendered =>
                    {
                        if (rendered != probeCamera) return;
                        try
                        {
                            // Read the actual display after the world camera, before overlay UI.
                            // targetTexture would ignore camera.rect and hide this regression.
                            pixels.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                            pixels.Apply();
                            captures++;
                        }
                        catch (Exception error) { readError = error; }
                    };
                    Camera.onPostRender += readback;
                    try
                    {
                        for (int variant = 0; variant < 3; variant++)
                        {
                            // Simulate left/right notches, then a smaller safe area while paused.
                            Runtime("MobileDisplay").GetField("PreviewSafeArea").SetValue(null,
                                variant == 0 ? new Rect(size.x * .06f, size.y * .04f, size.x * .88f, size.y * .92f) :
                                variant == 1 ? new Rect(size.x * .02f, size.y * .02f, size.x * .88f, size.y * .96f) :
                                    new Rect(size.x * .08f, size.y * .08f, size.x * .84f, size.y * .84f));
                            if (variant == 2) Call(owner, "Pause");
                            int previous = captures;
                            yield return null;
                            yield return null;
                            Assert.That(readError, Is.Null);
                            Assert.That(captures, Is.GreaterThan(previous), "World camera must really render to the display.");
                            Rect viewport = camera.pixelRect;
                            Color expected = pixels.GetPixel(Mathf.RoundToInt(viewport.xMin + 8),
                                Mathf.RoundToInt(viewport.center.y));
                            Assert.That(expected.g, Is.LessThan(.5f), "Baseline is the dark empty board margin.");
                            foreach (Vector2 probe in new[] {
                                new Vector2(size.x * .5f, (viewport.yMax + size.y) * .5f),
                                new Vector2(size.x * .5f, viewport.yMin * .5f),
                                new Vector2(viewport.xMin * .5f, size.y * .5f),
                                new Vector2((viewport.xMax + size.x) * .5f, size.y * .5f) })
                            {
                                Color actual = pixels.GetPixel((int)probe.x, (int)probe.y);
                                Assert.That(Vector3.Distance(new Vector3(actual.r, actual.g, actual.b),
                                    new Vector3(expected.r, expected.g, expected.b)), Is.LessThan(.03f),
                                    $"{scene} {size} safe variant {variant}: previous UI pixels survived at {probe}.");
                            }
                            Color center = pixels.GetPixel((int)viewport.center.x, (int)viewport.center.y);
                            Assert.That(center.r, Is.GreaterThan(expected.r + .2f), "Clearing must preserve the rendered map.");
                            if (scene == "OpenFieldLevel02" && variant == 0)
                            {
                                string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                                    Application.dataPath, "../Logs/ViewportResidue"));
                                System.IO.Directory.CreateDirectory(directory);
                                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,
                                    $"mobile-world-cleared-{size.x}x{size.y}.png"), pixels.EncodeToPNG());
                            }
                        }
                        Assert.That(Get(owner, "IsPaused"), Is.True);
                        Assert.That(Time.timeScale, Is.Zero);
                    }
                    finally
                    {
                        Camera.onPostRender -= readback;
                        Object.Destroy(seedObject);
                        Object.Destroy(probeObject);
                        Object.Destroy(pixels);
                    }
                }
                // No battle background camera should survive a return to the main menu.
                yield return SceneManager.LoadSceneAsync("MainMenu");
                yield return null;
                Assert.That(Object.FindObjectsOfType<Camera>().Any(c => c.name == "Viewport Background Camera"), Is.False);
            }
        }
    }

    [UnityTest, Category("MobileLandscape")]
    public IEnumerator DesktopCameraRespondsToWindowResizesWhilePausedWithoutReloadingTheLevel()
    {
        Preview(false);
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03");
        Component owner = Find("OpenFieldPrototypeController");
        Call(owner, "Pause");
        Camera camera = (Camera)Field(owner, "gameplayCamera");
        int instance = owner.GetInstanceID();
        foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(960, 720),
            new Vector2Int(1600, 720), new Vector2Int(720, 1280) })
        {
            using (new Resolution(size.x, size.y))
            {
                yield return null;
                yield return null;
                Assert.That(Screen.width, Is.EqualTo(size.x));
                Assert.That(Screen.height, Is.EqualTo(size.y));
                Assert.That(Find("OpenFieldPrototypeController").GetInstanceID(), Is.EqualTo(instance));
                Assert.That(Get(owner, "IsPaused"), Is.True);
                Assert.That(camera.aspect, Is.EqualTo((float)size.x / size.y).Within(.001f));
                Vector3 center = owner.transform.position;
                Vector3 screen = camera.WorldToScreenPoint(center);
                float unitX = Vector3.Distance(screen, camera.WorldToScreenPoint(center + Vector3.right));
                float unitY = Vector3.Distance(screen, camera.WorldToScreenPoint(center + Vector3.up));
                Assert.That(unitX / unitY, Is.EqualTo(1f).Within(.001f));
                foreach (Vector2Int cell in new[] { new Vector2Int(0, 0), new Vector2Int(14, 10), new Vector2Int(7, 5) })
                {
                    Vector3 world = (Vector3)Call(owner, "CellToWorld", cell);
                    Vector3 point = camera.WorldToViewportPoint(world);
                    Assert.That(point.x, Is.InRange(0f, 1f));
                    Assert.That(point.y, Is.InRange(0f, 1f));
                    Assert.That(Call(owner, "WorldToCell", camera.ScreenToWorldPoint(camera.WorldToScreenPoint(world))), Is.EqualTo(cell));
                }
            }
        }
    }

    [UnityTest, Category("MobileLandscape")]
    public IEnumerator LandscapeResolutionsKeepMenuPauseAndSettingsUsable()
    {
        foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 720), new Vector2Int(1920, 1080) })
        {
            using (new Resolution(size.x, size.y))
            {
                yield return null;
                yield return null;
                Assert.That(Screen.width, Is.EqualTo(size.x));
                Assert.That(Screen.height, Is.EqualTo(size.y));
                Preview(false);
                yield return SceneManager.LoadSceneAsync("OpenFieldLevel03");
                yield return null;
                Component desktopOwner = Find("OpenFieldPrototypeController");
                Camera desktopCamera = (Camera)Field(desktopOwner, "gameplayCamera");
                Vector3 origin = desktopCamera.WorldToScreenPoint(desktopOwner.transform.position);
                float pixelX = Vector3.Distance(origin, desktopCamera.WorldToScreenPoint(desktopOwner.transform.position + Vector3.right));
                float pixelY = Vector3.Distance(origin, desktopCamera.WorldToScreenPoint(desktopOwner.transform.position + Vector3.up));
                Assert.That(pixelX / pixelY, Is.EqualTo(1f).Within(.001f));
                object grid = Field(desktopOwner, "grid");
                bool built = false;
                for (int x = 2; x < 13 && !built; x++)
                    for (int y = 2; y < 9 && !built; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (Call(grid, "CheckPlacement", cell, false).ToString() != "Valid") continue;
                        built = (bool)Call(desktopOwner, "TryBuildAt", cell);
                        if (built) Call(desktopOwner, "SelectPlacedTower", cell);
                    }
                Assert.That(built, Is.True);
                Call(Field(desktopOwner, "battleHud"), "Refresh");
                if (size.x == 1280)
                {
                    // Reproduce the previous desktop projection for a clearly labelled comparison.
                    desktopCamera.aspect = 15f / 11f;
                    desktopCamera.orthographicSize = 5.5f;
                    OpenFieldPlaybackPlayModeTests.CaptureUi(desktopCamera, $"viewport-old-projection-{size.x}x{size.y}.png");
                    Call(desktopOwner, "ConfigureMapViewport");
                }
                OpenFieldPlaybackPlayModeTests.CaptureUi(desktopCamera, $"viewport-correct-{size.x}x{size.y}.png");
                Preview(true);
                yield return SceneManager.LoadSceneAsync("MainMenu");
                yield return null;
                Canvas.ForceUpdateCanvases();
                Component menu = Find("MainMenuController");
                Inside((RectTransform)Field(menu, "menuContent"));
                Call(menu, "OpenSettings");
                yield return null;
                Inside((RectTransform)Field(Field(menu, "settingsPanel"), "panel"));
                Call(menu, "CloseSettings");
                yield return SceneManager.LoadSceneAsync("OpenFieldLevel03");
                yield return null;
                Call(Find("OpenFieldPrototypeController"), "Pause");
                yield return null;
                object controls = Field(Find("OpenFieldPrototypeController"), "playbackControls");
                Inside((RectTransform)Field(controls, "pausePanel"));
                OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, $"mobile-pause-{size.x}x{size.y}.png");
                Call(controls, "OpenSettings");
                yield return null;
                Inside((RectTransform)Field(Field(controls, "settingsPanel"), "panel"));
            }
        }
    }

    [UnityTest, Category("MobileLandscape")]
    public IEnumerator ResultsFitDesktopAndMobileSafeAreasIncludingVisibleWindowResize()
    {
        foreach (bool mobile in new[] { false, true })
        foreach (Vector2Int size in new[] { new Vector2Int(640, 480), new Vector2Int(1280, 720), new Vector2Int(2048, 928) })
        using (new Resolution(size.x, size.y))
        {
            yield return null;
            yield return null;
            Preview(mobile);
            foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
            foreach (string outcome in new[] { "Victory", "Defeat" })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                Component owner = Find("OpenFieldPrototypeController");
                Call(owner, "FinishRun", Enum.Parse(Runtime("OpenFieldPhase"), outcome));
                yield return null;
                Canvas.ForceUpdateCanvases();
                GameObject panel = (GameObject)Field(owner, outcome == "Victory" ? "victoryPanel" : "defeatPanel");
                if (scene == "OpenFieldPrototype")
                    OpenFieldPlaybackPlayModeTests.CaptureUi((Camera)Field(owner, "gameplayCamera"),
                        $"result-{(mobile ? "mobile" : "desktop")}-{outcome}-{size.x}x{size.y}.png");
                foreach (Image image in panel.GetComponentsInChildren<Image>()) Inside(image.rectTransform);
                Button[] actions = panel.GetComponentsInChildren<Button>();
                Assert.That(actions.Length, Is.EqualTo(outcome == "Victory" && scene != "OpenFieldLevel05" ? 3 : 2));
                foreach (Button action in actions)
                {
                    Vector2 position = action.transform.TransformPoint(action.GetComponent<RectTransform>().rect.center);
                    List<RaycastResult> hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0));
                    Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(action),
                        "The visible button must remain clickable after fitting.");
                }
                if (scene == "OpenFieldLevel05" && outcome == "Defeat")
                {
                    using (new Resolution(960, 540))
                    {
                        yield return null;
                        yield return null;
                        Preview(mobile);
                        Canvas.ForceUpdateCanvases();
                        foreach (Image image in panel.GetComponentsInChildren<Image>()) Inside(image.rectTransform);
                    }
                    yield return null;
                    Preview(mobile);
                }
            }
        }
    }


    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Preview(false);
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}

#endif
