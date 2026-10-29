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
using Object = UnityEngine.Object;

public sealed class ZzzzzChapterPagingPlayModeTests
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static Component Menu() => (Component)Object.FindObjectOfType(Runtime("MainMenuController"));
    static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    static void Click(Button button)
    {
        Assert.That(button.interactable, Is.True, button.name);
        var pointer = new PointerEventData(EventSystem.current) { position = Bounds(button.GetComponent<RectTransform>()).center };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), button.name);
        Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button), button.name);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    static void Capture(string name)
    {
        string destination = Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_PAGING_CAPTURE");
        if (string.IsNullOrEmpty(destination)) return;
        OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main, name);
        Directory.CreateDirectory(destination);
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PauseArtwork", name)), Path.Combine(destination, name), true);
    }

    [UnityTest] public IEnumerator ArrowsPageFrom123To456AndRespectBoundariesWithoutOpeningLevels()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        Component menu = Menu(); Call(menu, "OpenLevelSelection"); yield return null;
        Button[] cards = (Button[])Field(menu, "chapterButtons");
        Button previous = (Button)Field(menu, "previousChapterPage"), next = (Button)Field(menu, "nextChapterPage");
        Assert.That(cards.Select(b => b.name), Is.EqualTo(new[] { "Chapter 1", "Chapter 2", "Chapter 3" }));
        Sprite[] originalCovers = cards.Select(b => b.transform.Find("Chapter Cover").GetComponent<Image>().sprite).ToArray();
        Assert.That(previous.interactable, Is.False); Assert.That(next.interactable, Is.True);
        Click(next); yield return null;
        Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(1));
        Assert.That(cards.Select(b => b.name), Is.EqualTo(new[] { "Chapter 4", "Chapter 5", "Chapter 6" }));
        Assert.That(cards.All(b => !b.interactable), Is.True);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
        foreach (Button card in cards)
        {
            Assert.That(card.transform.Find("Chapter Cover").GetComponent<Image>().enabled, Is.False);
            Assert.That(Get(card.transform.Find("Availability").GetComponent(Runtime("PixelImageText")), "text"), Is.EqualTo("开发中"));
            card.onClick.Invoke();
        }
        Assert.That(Get(menu, "SelectedChapter"), Is.EqualTo(-1));
        Assert.That(Get(menu, "IsLevelDetailsOpen"), Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(next.interactable, Is.False); Assert.That(previous.interactable, Is.True);
        Call(menu, "ChangeChapterPage", 1); Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(1));
        Call(menu, "FocusSelectionForNavigation");
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(previous.gameObject));
        Click(previous); yield return null;
        Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(0));
        Assert.That(cards.Select(b => b.transform.Find("Chapter Cover").GetComponent<Image>().sprite), Is.EqualTo(originalCovers));
        Assert.That(cards.All(b => b.interactable), Is.True);
        Call(menu, "ChangeChapterPage", -1); Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(0));
        Click(cards[1]); yield return null;
        Assert.That(Get(menu, "SelectedChapter"), Is.EqualTo(1));
        Assert.That(((GameObject)Field(menu, "chapterPaging")).activeSelf, Is.False);
        Call(menu, "ChangeChapterPage", 1); Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(0));
        Call(menu, "ReturnToChapters"); yield return null;
        Click(next); Call(menu, "HandleEscape");
        Assert.That(((GameObject)Field(menu, "selectionCanvas")).activeSelf, Is.False);
        Call(menu, "OpenLevelSelection");
        Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(0));
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
    }

    [UnityTest] public IEnumerator BothPagesAndArrowHitAreasFitDesktopAndMobileWindows()
    {
        foreach (Vector2Int size in new[] { new Vector2Int(640, 480), new Vector2Int(1280, 720), new Vector2Int(2048, 928) })
        using (new ZzMobileLandscapePlayModeTests.Resolution(size.x, size.y))
        foreach (bool mobile in new[] { false, true })
        {
            yield return null; yield return null; MobilePlayModeTests.Preview(mobile);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Component menu = Menu(); Call(menu, "OpenLevelSelection");
            for (int page = 0; page < 2; page++)
            {
                yield return null; Canvas.ForceUpdateCanvases();
                Rect viewport = Bounds((RectTransform)Field(menu, "chapterViewport"));
                Button[] cards = (Button[])Field(menu, "chapterButtons");
                Button[] arrows = { (Button)Field(menu, "previousChapterPage"), (Button)Field(menu, "nextChapterPage") };
                foreach (Button card in cards)
                {
                    Rect bounds = Bounds(card.GetComponent<RectTransform>());
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - .5f));
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(viewport.xMax + .5f));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - .5f));
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(viewport.yMax + .5f));
                    foreach (Button arrow in arrows) Assert.That(bounds.Overlaps(Bounds(arrow.GetComponent<RectTransform>())), Is.False);
                }
                foreach (Button arrow in arrows) MobilePlayModeTests.Inside(arrow.GetComponent<RectTransform>());
                foreach (Graphic text in ((GameObject)Field(menu, "selectionCanvas")).GetComponentsInChildren<Graphic>())
                    if (text.isActiveAndEnabled && text.GetType().Name == "PixelImageText")
                        Assert.That(Get(text, "MissingGlyphCount"), Is.Zero, text.name);
                Capture($"chapter-page-{page + 1}-{mobile}-{size.x}x{size.y}.png");
                if (page == 0) Click(arrows[1]); else Click(arrows[0]);
            }
        }
    }

    [UnityTest] public IEnumerator AddedCatalogChaptersUseGlobalIndicesAndReturnToTheirOwnPage()
    {
        object art = Runtime("LevelSelectionArtwork").GetProperty("Defaults").GetValue(null);
        Array original = (Array)Field(art, "chapters");
        Array expanded = Array.CreateInstance(original.GetType().GetElementType(), 7);
        Array.Copy(original, expanded, original.Length);
        expanded.SetValue(original.GetValue(0), 4); expanded.SetValue(original.GetValue(1), 6);
        art.GetType().GetField("chapters").SetValue(art, expanded);
        try
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Component menu = Menu(); Call(menu, "OpenLevelSelection"); yield return null;
            Assert.That(Get(menu, "ChapterPageCount"), Is.EqualTo(3));
            Call(menu, "ChangeChapterPage", 1); yield return null;
            Button[] cards = (Button[])Field(menu, "chapterButtons");
            Assert.That(cards.Select(b => b.interactable), Is.EqualTo(new[] { false, true, false }));
            Click(cards[1]); yield return null;
            Assert.That(Get(menu, "SelectedChapter"), Is.EqualTo(4));
            Call(menu, "ReturnToChapters");
            Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(1));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(cards[1].gameObject));
            Call(menu, "ChangeChapterPage", 1); yield return null;
            Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(2));
            Assert.That(cards.Select(b => b.interactable), Is.EqualTo(new[] { true, false, false }));
            Assert.That(((Button)Field(menu, "nextChapterPage")).interactable, Is.False);
            Click(cards[0]); Assert.That(Get(menu, "SelectedChapter"), Is.EqualTo(6));
            Call(menu, "ReturnToChapters"); Assert.That(Get(menu, "ChapterPage"), Is.EqualTo(2));
        }
        finally { art.GetType().GetField("chapters").SetValue(art, original); }
    }

    [UnityTearDown] public IEnumerator Restore()
    {
        MobilePlayModeTests.Preview(false); Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainMenu");
    }
}
#endif
