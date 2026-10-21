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

public sealed class OpenFieldPlaybackPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    private static object Get(object obj, string name) => obj.GetType().GetProperty(name, Flags).GetValue(obj);
    private static object Call(object obj, string name, params object[] args)
    {
        MethodInfo method = obj.GetType().GetMethods(Flags).First(m => m.Name == name &&
            m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) =>
                args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(valid => valid));
        try { return method.Invoke(obj, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static Component Owner() => (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    private static Vector2Int FreeCell(Component owner)
    {
        object grid = Field(owner, "grid");
        for (int x = 2; x < 13; x++)
            for (int y = 2; y < 9; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (Call(grid, "CheckPlacement", cell, false).ToString() == "Valid") return cell;
            }
        throw new AssertionException("Expected a buildable cell.");
    }
    private static Button Button(object controls, string name) => ((Component)controls)
        .GetComponentsInChildren<Button>(true).Single(b => b.name == name);

    [UnityTest]
    public IEnumerator PauseArtworkLoadsSeparateLayersHighlightsAtZeroTimeAndKeepsPanelInsideSafeArea()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Owner();
        object controls = Field(owner, "playbackControls");
        Call(owner, "Pause");
        yield return null;
        Canvas.ForceUpdateCanvases();
        Call(controls, "Refresh");
        Canvas.ForceUpdateCanvases();
        RectTransform panel = (RectTransform)Field(controls, "pausePanel");
        Assert.That(panel.GetComponent<Image>().sprite.name, Is.EqualTo("01_空面板"));
        Assert.That(panel.Find("Pause Title").GetComponent<Image>().sprite.name, Is.EqualTo("02_游戏暂停_标题"));
        Assert.That(panel.GetComponentsInChildren(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true)).Length, Is.Zero,
            "Title and button text are part of the supplied artwork; do not draw duplicate labels.");
        Button resume = Button(controls, "Resume");
        Button menu = Button(controls, "Return to Main Menu");
        Button settings = Button(controls, "Settings");
        Assert.That(resume.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
        Assert.That(resume.image.sprite.name, Is.EqualTo("01_返回游戏_未选中"));
        Assert.That(menu.image.sprite.name, Is.EqualTo("05_退出到主菜单_未选中"));
        Assert.That(settings.interactable, Is.True);
        Assert.That(settings.image.sprite.name, Is.EqualTo("03_设置_未选中"));
        settings.onClick.Invoke();
        Assert.That(Get(owner, "IsPaused"), Is.True);
        Assert.That(Get(controls, "IsSettingsVisible"), Is.True);
        Call(owner, "HandleEscape");
        Assert.That(Get(controls, "IsSettingsVisible"), Is.False);
        Assert.That(Get(owner, "IsPaused"), Is.True);
        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        Rect safe = Screen.safeArea;
        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(safe.xMin - 1f));
        Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(safe.yMin - 1f));
        Assert.That(corners[2].x, Is.LessThanOrEqualTo(safe.xMax + 1f));
        Assert.That(corners[2].y, Is.LessThanOrEqualTo(safe.yMax + 1f));
        Assert.That((corners[2].x - corners[0].x) / (corners[2].y - corners[0].y),
            Is.EqualTo(.8f).Within(.003f));
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(resume.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(resume.image.overrideSprite.name, Is.EqualTo("02_返回游戏_蓝色选中"));
        CaptureUi((Camera)Field(owner, "gameplayCamera"));
        ExecuteEvents.Execute(resume.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Assert.That(resume.image.overrideSprite, Is.EqualTo(resume.image.sprite));
        resume.onClick.Invoke();
        Assert.That(Get(owner, "IsPaused"), Is.False);
    }

    // Export an actual Unity render when a graphics device is available; never modify source artwork.
    internal static void CaptureUi(Camera camera, string filename = "pause-menu.png")
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Canvas[] overlays = Object.FindObjectsOfType<Canvas>()
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        Camera[] previousCameras = overlays.Select(c => c.worldCamera).ToArray();
        float[] previousDistances = overlays.Select(c => c.planeDistance).ToArray();
        RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
        int previousMask = camera.cullingMask;
        RenderTexture target = new RenderTexture(Screen.width, Screen.height, 24);
        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        GameObject captureObject = new GameObject("Pause UI capture camera");
        Camera uiCamera = captureObject.AddComponent<Camera>();
        uiCamera.enabled = false;
        uiCamera.orthographic = true;
        uiCamera.orthographicSize = Screen.height * .5f;
        uiCamera.aspect = (float)Screen.width / Screen.height;
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        uiCamera.transform.position = new Vector3(0f, 0f, -100f);
        try
        {
            target.Create();
            camera.targetTexture = target;
            camera.cullingMask &= ~uiCamera.cullingMask;
            uiCamera.targetTexture = target;
            foreach (Canvas overlay in overlays)
            {
                overlay.renderMode = RenderMode.ScreenSpaceCamera;
                overlay.worldCamera = uiCamera;
                overlay.planeDistance = uiCamera.nearClipPlane + 1f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            uiCamera.Render();
            RenderTexture.active = target;
            screenshot.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            screenshot.Apply();
            string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                "../Logs/PauseArtwork"));
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, filename), screenshot.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < overlays.Length; i++)
            {
                overlays[i].renderMode = RenderMode.ScreenSpaceOverlay;
                overlays[i].worldCamera = previousCameras[i];
                overlays[i].planeDistance = previousDistances[i];
            }
            camera.targetTexture = previousTarget;
            camera.cullingMask = previousMask;
            RenderTexture.active = previousActive;
            uiCamera.targetTexture = null;
            target.Release();
            Object.Destroy(captureObject);
            Object.Destroy(target);
            Object.Destroy(screenshot);
            Canvas.ForceUpdateCanvases();
        }
    }

    [UnityTest]
    public IEnumerator FiveLevelsWireButtonsAndPauseBlocksBuildSellAndWaveWithHudStillVisible()
    {
        foreach (string scene in new[] { "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05" })
        {
            yield return SceneManager.LoadSceneAsync(scene);
            Component owner = Owner();
            object controls = Field(owner, "playbackControls");
            Vector2Int cell = FreeCell(owner);
            Call(owner, "TryBuildAt", cell);
            int atp = (int)Get(owner, "CurrentATP");
            Button speed = Button(controls, "Playback Speed");
            EventSystem.current.SetSelectedGameObject(speed.gameObject);
            speed.onClick.Invoke();
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            Button(controls, "Pause").onClick.Invoke();
            Button(controls, "Settings").onClick.Invoke();
            Assert.That(Get(controls, "IsSettingsVisible"), Is.True);
            Button(controls, "Back to Pause").onClick.Invoke();
            Assert.That(Get(controls, "IsSettingsVisible"), Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(Get(controls, "IsPauseVisible"), Is.True);
            Assert.That(Call(controls, "ContainsPointer", Vector2.zero), Is.True);
            Assert.That(Call(owner, "TrySellAt", cell), Is.False);
            Assert.That(Call(owner, "TryBuildAt", FreeCell(owner)), Is.False);
            Assert.That(Call(owner, "StartAssault"), Is.False);
            Assert.That(Get(owner, "CurrentATP"), Is.EqualTo(atp));
            yield return null;
            Assert.That(((Component)Field(Field(owner, "battleHud"), "canvas")).gameObject.activeInHierarchy, Is.True);
            Button(controls, "Resume").onClick.Invoke();
            Assert.That(Get(controls, "IsPauseVisible"), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(2f));
        }
    }

    [UnityTest]
    public IEnumerator SettingsControlsApplySaveAndReturnWithoutResumingTheBattle()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel03");
        Component owner = Owner();
        object controls = Field(owner, "playbackControls");
        object settings = Runtime("GameSettings").GetProperty("Current").GetValue(null);
        string prefix = (string)Runtime("GameSettings").GetField("PreferencePrefix").GetRawConstantValue();
        string[] keys = { prefix + ".Volume", prefix + ".Muted", prefix + ".Fullscreen" };
        bool[] hadKeys = keys.Select(PlayerPrefs.HasKey).ToArray();
        float oldVolume = (float)Get(settings, "MasterVolume");
        bool oldMuted = (bool)Get(settings, "Muted"), oldFullscreen = (bool)Get(settings, "Fullscreen");
        try
        {
            Call(owner, "SetPlaybackSpeed", 2);
            Call(owner, "Pause");
            Button(controls, "Settings").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Component view = (Component)Field(controls, "settingsPanel");
            RectTransform settingsPanel = (RectTransform)Field(view, "panel");
            Assert.That(settingsPanel.GetComponentsInChildren(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true), true).Length,
                Is.Zero, "Settings labels and percentages now use the supplied artwork without duplicate TMP text.");
            foreach (var label in new[]
            {
                new[] { "Title", "01_设置" }, new[] { "Volume Label", "02_总音量" },
                new[] { "Fullscreen/Label", "04_全屏" }, new[] { "Restore Defaults/Label", "05_恢复默认" },
                new[] { "Back to Pause/Label", "06_返回" }, new[] { "Mute Hint", "07_点击喇叭切换静音" },
                new[] { "Volume Value/Percent", "10_百分号" }
            })
            {
                Image caption = settingsPanel.Find(label[0]).GetComponent<Image>();
                Assert.That(caption.sprite.name, Is.EqualTo(label[1]));
                Assert.That(caption.preserveAspect, Is.True);
                Assert.That(caption.raycastTarget, Is.False, "Letter images must leave button clicks to the parent.");
            }
            Assert.That(settingsPanel.Find("Display Hint").GetComponent<Image>().sprite.name,
                Is.EqualTo((bool)Runtime("GameSettings").GetProperty("SupportsDisplayMode").GetValue(null)
                    ? "08_关闭全屏后使用窗口模式" : "09_全屏切换在独立运行时生效"));
            Slider volume = view.GetComponentInChildren<Slider>(true);
            Toggle mute = view.GetComponentsInChildren<Toggle>(true).Single(t => t.name == "Mute");
            Toggle fullscreen = view.GetComponentsInChildren<Toggle>(true).Single(t => t.name == "Fullscreen");
            Image track = volume.GetComponentsInChildren<Image>(true).Single(i => i.name == "Track");
            Image fill = volume.GetComponentsInChildren<Image>(true).Single(i => i.name == "Fill");
            Assert.That(track.sprite.name, Is.EqualTo("03_音量槽_底条"));
            Assert.That(track.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(fill.sprite.name, Is.EqualTo("04_音量条_蓝色填充"));
            Assert.That(volume.handleRect.GetComponent<Image>().sprite.name, Is.EqualTo("05_音量滑块_拖动柄"));
            Assert.That(volume.fillRect.GetComponent<RectMask2D>(), Is.Not.Null);
            Call(settings, "SetMuted", false);
            foreach (int value in new[] { 0, 1, 9, 10, 37, 65, 99, 100 })
            {
                volume.value = value;
                Canvas.ForceUpdateCanvases();
                Assert.That(mute.image.sprite.name, Is.EqualTo(value == 0 ? "02_静音图标" : "01_声音图标"));
                Assert.That(volume.fillRect.rect.width, Is.EqualTo(416f * value / 100f).Within(.01f));
                Assert.That(fill.rectTransform.rect.size, Is.EqualTo(new Vector2(432f, 32f)),
                    "Clip the fixed artwork so low volume does not squash its pixels.");
                Vector3 fillEnd = volume.fillRect.TransformPoint(new Vector3(volume.fillRect.rect.xMax, 0f));
                Vector3 handleCenter = volume.handleRect.TransformPoint(volume.handleRect.rect.center);
                Assert.That(fillEnd.x, Is.EqualTo(handleCenter.x).Within(.01f), "Fill and handle must use the same inner range.");
                Assert.That(volume.handleRect.rect.size, Is.EqualTo(new Vector2(48f, 64f)));
                string displayedValue = string.Concat(settingsPanel.Find("Volume Value").GetComponentsInChildren<Image>()
                    .Where(i => i.name.StartsWith("Digit ")).OrderBy(i => i.rectTransform.anchoredPosition.x)
                    .Select(i => i.sprite.name));
                Assert.That(displayedValue, Is.EqualTo(value.ToString()), "Image digits must update without leading zeros.");
            }
            RectTransform dragArea = (RectTransform)volume.handleRect.parent;
            PointerEventData drag = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, dragArea.TransformPoint(
                    new Vector3(Mathf.Lerp(dragArea.rect.xMin, dragArea.rect.xMax, .75f), 0f, 0f)))
            };
            volume.OnPointerDown(drag);
            volume.OnDrag(drag);
            volume.OnPointerUp(drag);
            Assert.That(volume.value, Is.EqualTo(75f).Within(1f), "The slider must respond to pointer dragging while paused.");
            volume.value = 37f;
            Assert.That(AudioListener.volume, Is.EqualTo(.37f).Within(.001f));
            ExecuteEvents.Execute(mute.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(mute.isOn, Is.True);
            Assert.That(mute.image.sprite.name, Is.EqualTo("02_静音图标"));
            Assert.That(AudioListener.volume, Is.Zero);
            volume.value = 63f;
            Assert.That(AudioListener.volume, Is.Zero, "Dragging while muted must not unmute.");
            mute.isOn = false;
            Assert.That(mute.image.sprite.name, Is.EqualTo("01_声音图标"));
            Assert.That(AudioListener.volume, Is.EqualTo(.63f).Within(.001f));
            Assert.That(PlayerPrefs.GetFloat(keys[0]), Is.EqualTo(.63f).Within(.001f));
            Assert.That(PlayerPrefs.GetInt(keys[1]), Is.Zero);
            Assert.That(fullscreen.interactable, Is.EqualTo((bool)Runtime("GameSettings")
                .GetProperty("SupportsDisplayMode").GetValue(null)));
            Button(controls, "Restore Defaults").onClick.Invoke();
            Assert.That(volume.value, Is.EqualTo(100f));
            Assert.That(mute.isOn, Is.False);
            Assert.That(fullscreen.isOn, Is.True);
            Assert.That(AudioListener.volume, Is.EqualTo(1f));
            volume.value = 37f;
            Canvas.ForceUpdateCanvases();
            RectTransform panel = (RectTransform)Field(view, "panel");
            RectTransform pausePanel = (RectTransform)Field(controls, "pausePanel");
            Assert.That(panel.rect.size, Is.EqualTo(pausePanel.rect.size));
            Assert.That(panel.localScale, Is.EqualTo(pausePanel.localScale));
            Vector3[] corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            Assert.That(corners[2].y - corners[0].y, Is.GreaterThan(Screen.safeArea.height * .8f),
                "Small windows must use the available height, including the first displayed frame.");
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(Screen.safeArea.xMin - 1f));
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(Screen.safeArea.yMin - 1f));
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.safeArea.xMax + 1f));
            Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.safeArea.yMax + 1f));
            volume.value = 65f;
            CaptureUi((Camera)Field(owner, "gameplayCamera"), "settings-art.png");
            foreach (var state in new[]
            {
                new[] { "Restore Defaults", "01_恢复默认_空框_未选中", "02_恢复默认_空框_蓝色选中", "settings-frames-default-hover.png" },
                new[] { "Back to Pause", "03_返回_空框_未选中", "04_返回_空框_蓝色选中", "settings-frames-back-hover.png" }
            })
            {
                Button framedButton = Button(controls, state[0]);
                Assert.That(framedButton.image.sprite.name, Is.EqualTo(state[1]));
                Assert.That(framedButton.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
                PointerEventData pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(framedButton.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                Assert.That(framedButton.image.overrideSprite.name, Is.EqualTo(state[2]));
                Assert.That(Time.timeScale, Is.Zero);
                CaptureUi((Camera)Field(owner, "gameplayCamera"), state[3]);
                ExecuteEvents.Execute(framedButton.gameObject, pointer, ExecuteEvents.pointerExitHandler);
                Assert.That(framedButton.image.overrideSprite, Is.EqualTo(framedButton.image.sprite));
            }
            mute.isOn = true;
            CaptureUi((Camera)Field(owner, "gameplayCamera"), "settings-art-muted.png");
            mute.isOn = false;
            volume.value = 0f;
            CaptureUi((Camera)Field(owner, "gameplayCamera"), "settings-art-zero.png");
            volume.value = 100f;
            CaptureUi((Camera)Field(owner, "gameplayCamera"), "settings-art-full.png");
            volume.value = 37f;
            float clock = Time.time;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Time.time, Is.EqualTo(clock));
            Assert.That(Call(owner, "StartAssault"), Is.False);
            Call(owner, "HandleEscape");
            Assert.That(Get(controls, "IsSettingsVisible"), Is.False);
            Assert.That(Get(owner, "IsPaused"), Is.True);
            Call(owner, "HandleEscape");
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            Call(owner, "Pause");
            Button(controls, "Settings").onClick.Invoke();
            Assert.That(volume.value, Is.EqualTo(37f));
            Call(owner, "Resume");
            Assert.That(Get(controls, "IsSettingsVisible"), Is.False);
            Call(owner, "Pause");
            Assert.That(Get(controls, "IsSettingsVisible"), Is.False, "A new pause opens the pause menu.");
            Call(owner, "ReturnToMainMenu");
            yield return null;
            Assert.That(Get(settings, "MasterVolume"), Is.EqualTo(.37f).Within(.001f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
        finally
        {
            Call(settings, "SetMasterVolume", oldVolume);
            Call(settings, "SetMuted", oldMuted);
            Call(settings, "SetFullscreen", oldFullscreen);
            for (int i = 0; i < keys.Length; i++) if (!hadKeys[i]) PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
        }
    }

    [UnityTest]
    public IEnumerator PauseFreezesMovementSpawningProjectilesAndAntigenTimersThenResumes()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldLevel02");
        Component owner = Owner();
        Assert.That(Call(owner, "StartAssault"), Is.True);
        yield return new WaitForSeconds(.04f);
        object[] enemies = ((IEnumerable)Field(owner, "activeEnemies")).Cast<object>().ToArray();
        Assert.That(enemies.Length, Is.GreaterThan(0));
        Component enemy = (Component)enemies[0];
        Component immune = enemy.GetComponent(Runtime("EnemyImmuneState"));
        Call(immune, "ApplyMark", .12f);
        object pool = Field(owner, "projectilePool");
        Component projectile = (Component)Call(pool, "Get", enemy.transform.position + Vector3.up * 5f);
        Call(projectile, "Initialize", enemy, 1f);
        Call(owner, "Pause");
        yield return null;
        Vector3 position = enemy.transform.position;
        Vector3 shotPosition = projectile.transform.position;
        int count = ((IEnumerable)Field(owner, "activeEnemies")).Cast<object>().Count();
        float pausedTime = Time.time;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(Time.time, Is.EqualTo(pausedTime));
        Assert.That(enemy.transform.position, Is.EqualTo(position));
        Assert.That(projectile.transform.position, Is.EqualTo(shotPosition));
        Assert.That(((IEnumerable)Field(owner, "activeEnemies")).Cast<object>().Count(), Is.EqualTo(count));
        Assert.That(Get(immune, "IsMarked"), Is.True);
        Call(owner, "Resume");
        yield return new WaitForSeconds(.16f);
        Assert.That(enemy.transform.position, Is.Not.EqualTo(position));
        Assert.That(Get(immune, "IsMarked"), Is.False);
        Assert.That(((IEnumerable)Field(owner, "activeEnemies")).Cast<object>().Count(), Is.GreaterThan(count));
    }

    [UnityTest]
    public IEnumerator DoubleSpeedAdvancesGameClockTwiceAsFastAndSceneTransitionsResetIt()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Owner();
        yield return null;
        float realStart = Time.realtimeSinceStartup, gameStart = Time.time;
        yield return new WaitForSecondsRealtime(.16f);
        float normalRate = (Time.time - gameStart) / (Time.realtimeSinceStartup - realStart);
        Call(owner, "SetPlaybackSpeed", 2);
        yield return null;
        realStart = Time.realtimeSinceStartup;
        gameStart = Time.time;
        yield return new WaitForSecondsRealtime(.16f);
        float fastRate = (Time.time - gameStart) / (Time.realtimeSinceStartup - realStart);
        Assert.That(normalRate, Is.InRange(.75f, 1.25f));
        Assert.That(fastRate / normalRate, Is.InRange(1.6f, 2.4f));
        Call(owner, "Pause");
        Call(owner, "RestartPrototype");
        yield return null;
        owner = Owner();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Get(owner, "PlaybackSpeed"), Is.EqualTo(1));
        Call(owner, "SetPlaybackSpeed", 2);
        Call(owner, "Pause");
        Button(Field(owner, "playbackControls"), "Return to Main Menu").onClick.Invoke();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator ResultsAndDisabledControllerClearPauseAndHideControls()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");
        Component owner = Owner();
        Call(owner, "SetPlaybackSpeed", 2);
        Call(owner, "Pause");
        Call(owner, "FinishRun", Enum.Parse(Runtime("OpenFieldPhase"), "Victory"));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Get(owner, "IsPaused"), Is.False);
        Assert.That(Get(Field(owner, "playbackControls"), "IsVisible"), Is.False);
        Assert.That(Call(owner, "Pause"), Is.False);
        Call(owner, "LoadNextLevel");
        yield return null;
        owner = Owner();
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("OpenFieldLevel02"));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Call(owner, "Pause");
        ((Behaviour)owner).enabled = false;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Get(owner, "IsPaused"), Is.False);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
