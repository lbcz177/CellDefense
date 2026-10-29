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

public sealed class MainMenuSettingsPlayModeTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Component Menu() => (Component)Object.FindObjectOfType(Runtime("MainMenuController"));
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static object Call(object target, string name, params object[] args)
    {
        try { return target.GetType().GetMethod(name, Flags).Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static Button Back(Component view) => view.GetComponentsInChildren<Button>(true)
        .Single(b => b.name == "Back to Pause");

    [UnityTest]
    public IEnumerator MainMenuEntryBlocksBackgroundAndSharesPreferencesWithBattle()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu = Menu();
        Button entry = (Button)Field(menu, "settingsEntry");
        CanvasGroup interaction = (CanvasGroup)Field(menu, "menuInteraction");
        object settings = Runtime("GameSettings").GetProperty("Current").GetValue(null);
        string prefix = (string)Runtime("GameSettings").GetField("PreferencePrefix").GetRawConstantValue();
        string[] keys = { prefix + ".Volume", prefix + ".Muted", prefix + ".Fullscreen" };
        bool[] hadKeys = keys.Select(PlayerPrefs.HasKey).ToArray();
        float oldVolume = (float)Get(settings, "MasterVolume");
        bool oldMuted = (bool)Get(settings, "Muted"), oldFullscreen = (bool)Get(settings, "Fullscreen");
        try
        {
            Canvas.ForceUpdateCanvases();
            OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, "main-menu-entry.png");
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null,
                entry.GetComponent<RectTransform>().TransformPoint(entry.GetComponent<RectTransform>().rect.center));
            PointerEventData pointer = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left, position = point };
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.First().gameObject, Is.EqualTo(entry.gameObject), "The visible settings artwork must be clickable.");
            EventSystem.current.SetSelectedGameObject(entry.gameObject);
            ExecuteEvents.Execute(entry.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Get(menu, "IsSettingsOpen"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(interaction.interactable, Is.False);
            Assert.That(interaction.blocksRaycasts, Is.False);
            Call(menu, "OpenLevelSelection");
        Call(menu, "OpenChapter", 0);
            Call(menu, "LoadLevel", 0);
            Assert.That(((GameObject)Field(menu, "selectionCanvas")).activeSelf, Is.False);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            // Raycast depth is assigned when the newly enabled Canvas is rendered.
            yield return null;
            Canvas.ForceUpdateCanvases();
            hits.Clear();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits.Any(h => h.gameObject.transform.IsChildOf(interaction.transform)), Is.False,
                "Settings must block all underlying main menu buttons.");
            Component view = (Component)Field(menu, "settingsPanel");
            Slider volume = view.GetComponentInChildren<Slider>(true);
            Toggle mute = view.GetComponentsInChildren<Toggle>(true).Single(t => t.name == "Mute");
            volume.value = 65f;
            mute.isOn = false;
            Assert.That(AudioListener.volume, Is.EqualTo(.65f).Within(.001f));
            OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, "main-menu-settings.png");
            mute.isOn = true;
            Assert.That(AudioListener.volume, Is.Zero);
            Assert.That(PlayerPrefs.GetFloat(keys[0]), Is.EqualTo(.65f).Within(.001f));
            Assert.That(PlayerPrefs.GetInt(keys[1]), Is.EqualTo(1));
            RectTransform panel = (RectTransform)Field(view, "panel");
            Vector3[] corners = new Vector3[4];
            Canvas.ForceUpdateCanvases();
            panel.GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(Screen.safeArea.xMin - 1f));
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(Screen.safeArea.yMin - 1f));
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.safeArea.xMax + 1f));
            Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.safeArea.yMax + 1f));
            Back(view).onClick.Invoke();
            Assert.That(Get(menu, "IsSettingsOpen"), Is.False);
            Assert.That(interaction.interactable && interaction.blocksRaycasts, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(entry.gameObject));
            Call(menu, "OpenSettings");
            Call(menu, "HandleEscape");
            Assert.That(Get(menu, "IsSettingsOpen"), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Call(menu, "StartGame");
            Assert.That(((GameObject)Field(menu, "selectionCanvas")).activeSelf, Is.True);
            Call(menu, "OpenSettings");
            Assert.That(Get(menu, "IsSettingsOpen"), Is.False, "Settings and level selection must be mutually exclusive.");
            Assert.That(((Button[])Field(menu, "levelButtons")).Length, Is.EqualTo(5));
            Call(menu, "CloseLevelSelection");
            Assert.That(interaction.interactable && interaction.blocksRaycasts, Is.True);
            Call(menu, "StartGame");
            Call(menu, "OpenChapter", 0);
            ((Button[])Field(menu, "levelButtons"))[0].onClick.Invoke();
            Assert.That(Get(menu, "IsLevelDetailsOpen"), Is.True);
            ((Button)Field(menu, "detailsStart")).onClick.Invoke();
            yield return null;
            Component owner = (Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
            Assert.That(owner, Is.Not.Null);
            Call(owner, "Pause");
            Call(Field(owner, "playbackControls"), "OpenSettings");
            Component battleSettings = (Component)Field(Field(owner, "playbackControls"), "settingsPanel");
            Assert.That(battleSettings.GetComponentInChildren<Slider>(true).value, Is.EqualTo(65f));
            Assert.That(battleSettings.GetComponentsInChildren<Toggle>(true).Single(t => t.name == "Mute").isOn, Is.True);
            Call(owner, "ReturnToMainMenu");
            yield return null;
            menu = Menu();
            Assert.That(Get(menu, "IsSettingsOpen"), Is.False);
            Call(menu, "OpenSettings");
            view = (Component)Field(menu, "settingsPanel");
            Assert.That(view.GetComponentInChildren<Slider>(true).value, Is.EqualTo(65f));
            Call(menu, "HandleEscape");
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
    public IEnumerator DisableClosesOverlaysAndDestroyRemovesRuntimeUi()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu = Menu();
        CanvasGroup interaction = (CanvasGroup)Field(menu, "menuInteraction");
        GameObject settingsCanvas = (GameObject)Field(menu, "settingsCanvas");
        GameObject selectionCanvas = (GameObject)Field(menu, "selectionCanvas");
        Button entry = (Button)Field(menu, "settingsEntry");
        interaction.interactable = false;
        Call(menu, "OpenSettings");
        ((Behaviour)menu).enabled = false;
        Assert.That(settingsCanvas.activeSelf, Is.False);
        Assert.That(interaction.interactable, Is.False, "Restore the prior CanvasGroup state rather than enabling everything.");
        Assert.That(interaction.blocksRaycasts, Is.True);
        interaction.interactable = true;
        ((Behaviour)menu).enabled = true;
        Call(menu, "OpenSettings");
        Assert.That(Get(menu, "IsSettingsOpen"), Is.True);
        Call(menu, "CloseSettings");
        Call(menu, "OpenLevelSelection");
        Call(menu, "OpenChapter", 0);
        ((Behaviour)menu).enabled = false;
        Assert.That(selectionCanvas.activeSelf, Is.False);
        Assert.That(interaction.interactable && interaction.blocksRaycasts, Is.True);
        ((Behaviour)menu).enabled = true;
        Call(menu, "OpenSettings");
        Object.Destroy(menu);
        yield return null;
        yield return null;
        Assert.That(settingsCanvas == null, Is.True);
        Assert.That(selectionCanvas == null, Is.True);
        Assert.That(entry == null, Is.True);
        Assert.That(interaction.interactable && interaction.blocksRaycasts, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator QuitEntryOnlyRequestsExitFromIdleMainMenu()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu = Menu();
        Button quit = (Button)Field(menu, "quitEntry");
        Canvas.ForceUpdateCanvases();
        yield return null;
        RectTransform rect = quit.GetComponent<RectTransform>();
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
        };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.First().gameObject, Is.EqualTo(quit.gameObject));
        Call(menu, "OpenSettings");
        quit.onClick.Invoke();
        Assert.That(Get(menu, "QuitRequested"), Is.False);
        Call(menu, "CloseSettings");
        Call(menu, "OpenLevelSelection");
        Call(menu, "OpenChapter", 0);
        Call(menu, "QuitGame");
        Assert.That(Get(menu, "QuitRequested"), Is.False);
        Call(menu, "CloseLevelSelection");
        ((Behaviour)menu).enabled = false;
        Call(menu, "QuitGame");
        Assert.That(Get(menu, "QuitRequested"), Is.False);
        ((Behaviour)menu).enabled = true;
        LogAssert.Expect(LogType.Log, "退出请求已触发；独立运行时关闭游戏窗口。");
        ExecuteEvents.Execute(quit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.That(Get(menu, "QuitRequested"), Is.True);
        Call(menu, "QuitGame");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "Application.Quit is ignored by the Editor.");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
