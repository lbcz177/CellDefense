using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private static readonly string[] LevelScenes =
    {
        "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03",
        "OpenFieldLevel04", "OpenFieldLevel05"
    };
    private static readonly string[] LevelTitles =
    {
        "第一关  守住伤口", "第二关  集中清除", "第三关  认出敌人",
        "第四关  两侧告急", "第五关  感染加剧"
    };

    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private TMP_FontAsset menuFont;
    [SerializeField] private Sprite[] levelArtwork;

    private Transform menuContent;
    private RectTransform selectionPanel;
    private GameObject selectionCanvas;
    private GameObject settingsCanvas;
    private GameSettingsPanel settingsPanel;
    private CanvasGroup menuInteraction;
    private Button settingsEntry, quitEntry;
    private Button[] mainMenuButtons;
    private bool mainMenuKeyboardFocus;
    public bool MainMenuKeyboardFocus => mainMenuKeyboardFocus;
    public bool IsMainMenuInteractive => isActiveAndEnabled && menuInteraction != null && menuInteraction.interactable &&
        !loadingLevel && !IsSettingsOpen && !IsCodexOpen && (selectionCanvas == null || !selectionCanvas.activeSelf);
    private Button codexEntry;
    private CellCodexView cellCodex;
    private Button[] levelButtons;
    private Button[] chapterButtons;
    private const int ChaptersPerPage = 3;
    private int chapterPage;
    private GameObject chapterPaging;
    private Button previousChapterPage, nextChapterPage;
    private PixelImageText chapterPageLabel;
    public int ChapterPage => chapterPage;
    public int ChapterPageCount => Mathf.Max(2,
        Mathf.CeilToInt(LevelSelectionArtwork.Defaults.chapters.Length / (float)ChaptersPerPage));
    private GameObject chapterList;
    private RectTransform chapterViewport, selectionTitlePanel;
    private RectTransform chapterCards;
    private ScrollRect chapterScroll;
    private Image selectionBackground, selectionTitle, selectionChapterLabel, selectionHint, selectionBackCaption;
    private PixelImageText selectionFooter;
    private int selectedChapter = -1;
    private StarRatingGraphic[] levelStars;
    private PixelImageText[] levelLockHints;
    private CanvasGroup listInteraction;
    private GameObject levelDetails;
    private RectTransform detailsPanel;
    private Image detailsMap, detailsUnplayed;
    private Image detailsTitle, detailsHistoryLabel, detailsBestStarsLabel;
    private PixelImageText detailsPreviewNotice;
    private PixelImageText detailsName, detailsScore;
    private StarRatingGraphic detailsStars;
    private Button detailsStart, detailsBack;
    private int selectedLevel = -1;
    private readonly LevelProgress progress = new LevelProgress();
    private RectTransform listViewport;
    private GridLayoutGroup levelGrid;
    private ScrollRect levelScroll;
    private GameObject previousSelection;
    private float lastListWidth = -1f;
    private float lastListHeight = -1f;
    private bool loadingLevel;
    private bool previousInteractable;
    private bool previousBlocksRaycasts;
    public bool IsSettingsOpen => settingsCanvas != null && settingsCanvas.activeSelf;
    public bool IsLevelDetailsOpen => levelDetails != null && levelDetails.activeSelf;
    public bool IsChapterSelectionOpen => selectionCanvas != null && selectionCanvas.activeSelf && selectedChapter < 0;
    public int SelectedChapter => selectedChapter;
    private SelectionChapterArtwork CurrentChapter => selectedChapter >= 0
        ? LevelSelectionArtwork.Defaults.chapters[selectedChapter] : null;
    public bool QuitRequested { get; private set; }
    public bool IsCodexOpen => cellCodex != null && cellCodex.IsOpen;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (menuCanvas == null) menuCanvas = GetComponentInParent<Canvas>();
        if (menuCanvas == null || menuFont == null)
        {
            Debug.LogError("Main menu level selection requires its Canvas and Chinese font.", this);
            return;
        }
        menuInteraction = menuCanvas.GetComponent<CanvasGroup>();
        if (menuInteraction == null) menuInteraction = menuCanvas.gameObject.AddComponent<CanvasGroup>();

        menuContent = MobileDisplay.IsActive ? MobileSafeFrame.Wrap(menuCanvas, new Vector2(1920f, 1080f)) : menuCanvas.transform;
        MainMenuCellFlow.Attach(menuCanvas);
        CreateLevelSelection();
        // The background already contains the Settings artwork. Match its normalized
        // rectangle so the hit area follows the same stretch at every window size.
        GameObject settingsHitArea = CreateUI("Settings Entry", menuContent, typeof(Image), typeof(Button));
        settingsHitArea.GetComponent<Image>().color = Color.clear;
        settingsEntry = settingsHitArea.GetComponent<Button>();
        settingsEntry.targetGraphic = settingsHitArea.GetComponent<Image>();
        settingsEntry.transition = Selectable.Transition.None;
        settingsEntry.navigation = new Navigation { mode = Navigation.Mode.None };
        SetRect(settingsEntry.GetComponent<RectTransform>(), new Vector2(.093f, .216f),
            new Vector2(.345f, .322f), Vector2.one * .5f, Vector2.zero, Vector2.zero);
        settingsEntry.onClick.AddListener(OpenSettings);
        GameObject quitHitArea = CreateUI("Quit Entry", menuContent, typeof(Image), typeof(Button));
        quitHitArea.GetComponent<Image>().color = Color.clear;
        quitEntry = quitHitArea.GetComponent<Button>();
        quitEntry.targetGraphic = quitHitArea.GetComponent<Image>();
        quitEntry.transition = Selectable.Transition.None;
        quitEntry.navigation = new Navigation { mode = Navigation.Mode.None };
        SetRect(quitEntry.GetComponent<RectTransform>(), new Vector2(.093f, .096f),
            new Vector2(.345f, .202f), Vector2.one * .5f, Vector2.zero, Vector2.zero);
        quitEntry.onClick.AddListener(QuitGame);
        CreateSettings();
        cellCodex = CellCodexView.Create(CloseCellCodex);
        GameObject codexHitArea = CreateUI("Cell Codex Entry", menuContent, typeof(Image), typeof(Button));
        codexHitArea.GetComponent<Image>().color = Color.clear;
        codexEntry = codexHitArea.GetComponent<Button>();
        codexEntry.targetGraphic = codexHitArea.GetComponent<Image>();
        codexEntry.transition = Selectable.Transition.None;
        codexEntry.navigation = new Navigation { mode = Navigation.Mode.None };
        SetRect(codexEntry.GetComponent<RectTransform>(), new Vector2(.093f,.336f),
            new Vector2(.345f,.442f),Vector2.one*.5f,Vector2.zero,Vector2.zero);
        codexEntry.onClick.AddListener(OpenCellCodex);
        Button start = GetComponent<Button>();
        if (start != null)
        {
            mainMenuButtons = new[] { start, codexEntry, settingsEntry, quitEntry };
            foreach (Button entry in mainMenuButtons)
            {
                entry.transition = Selectable.Transition.None;
                entry.navigation = new Navigation { mode = Navigation.Mode.None };
                entry.gameObject.AddComponent<MainMenuButtonFeedback>().Initialize(this, entry);
            }
        }
        AndroidPrivacy.SetMenuEntryVisible(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
        if (IsChapterSelectionOpen && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
        {
            ChangeChapterPage(Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 1);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow)) FocusSelectionForNavigation();
    }

    private void FocusSelectionForNavigation()
    {
        if (IsMainMenuInteractive)
        {
            NavigateMainMenu(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Tab)) ? -1 : 1);
            return;
        }
        if (selectionCanvas == null || !selectionCanvas.activeSelf ||
            EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null) return;
        EventSystem.current.SetSelectedGameObject(IsLevelDetailsOpen
            ? (detailsStart.interactable ? detailsStart.gameObject : detailsBack.gameObject)
            : selectedChapter < 0 ? ChapterNavigationTarget() : levelButtons[0].gameObject);
    }

    private GameObject ChapterNavigationTarget()
    {
        foreach (Button card in chapterButtons)
            if (card.interactable) return card.gameObject;
        return previousChapterPage.interactable ? previousChapterPage.gameObject : nextChapterPage.gameObject;
    }

    private void NavigateMainMenu(int direction)
    {
        if (!IsMainMenuInteractive || mainMenuButtons == null || EventSystem.current == null) return;
        int current = System.Array.FindIndex(mainMenuButtons, b => b.gameObject == EventSystem.current.currentSelectedGameObject);
        bool hadFocus = mainMenuKeyboardFocus;
        foreach (Button entry in mainMenuButtons) entry.GetComponent<MainMenuButtonFeedback>().ResetPointer();
        mainMenuKeyboardFocus = true;
        int next = hadFocus && current >= 0 ? (current + direction + mainMenuButtons.Length) % mainMenuButtons.Length
            : direction < 0 ? mainMenuButtons.Length - 1 : 0;
        EventSystem.current.SetSelectedGameObject(mainMenuButtons[next].gameObject);
        mainMenuButtons[next].GetComponent<MainMenuButtonFeedback>().RefreshVisual();
    }

    public void UseMainMenuPointer()
    {
        mainMenuKeyboardFocus = false;
        if (mainMenuButtons == null) return;
        foreach (Button entry in mainMenuButtons) entry.GetComponent<MainMenuButtonFeedback>().ResetPointer();
        if (EventSystem.current != null && System.Array.Exists(mainMenuButtons,
            b => b.gameObject == EventSystem.current.currentSelectedGameObject)) EventSystem.current.SetSelectedGameObject(null);
    }

    public void HandleEscape()
    {
        if (IsCodexOpen) cellCodex.HandleEscape();
        else if (IsSettingsOpen) CloseSettings();
        else if (IsLevelDetailsOpen) CloseLevelDetails();
        else if (selectionCanvas != null && selectionCanvas.activeSelf)
        {
            if (selectedChapter >= 0) ReturnToChapters();
            else CloseLevelSelection();
        }
    }

    private void LateUpdate()
    {
        if (selectionCanvas != null && selectionCanvas.activeSelf)
        {
            RefreshCardLayout();
            PositionLevelDetails();
        }
    }

    public void StartGame()
    {
        OpenLevelSelection();
    }

    public void QuitGame()
    {
        if (!isActiveAndEnabled || loadingLevel || QuitRequested || IsSettingsOpen || IsCodexOpen ||
            (selectionCanvas != null && selectionCanvas.activeSelf)) return;
        PlayerPrefs.Save();
        QuitRequested = true;
#if UNITY_EDITOR
        Debug.Log("退出请求已触发；独立运行时关闭游戏窗口。", this);
#endif
        Application.Quit();
    }

    public void OpenLevelSelection()
    {
        if (!isActiveAndEnabled || selectionCanvas == null || selectionCanvas.activeSelf || loadingLevel || IsSettingsOpen || IsCodexOpen) return;
        BlockMenu();
        selectionCanvas.SetActive(true);
        ShowChapters(-1);
    }

    public void CloseLevelSelection()
    {
        if (selectionCanvas == null || !selectionCanvas.activeSelf || loadingLevel) return;
        CloseLevelDetails();
        selectionCanvas.SetActive(false);
        RestoreMenu(null);
    }

    public void OpenSettings()
    {
        if (!isActiveAndEnabled || settingsCanvas == null || IsSettingsOpen || IsCodexOpen || loadingLevel ||
            (selectionCanvas != null && selectionCanvas.activeSelf)) return;
        BlockMenu();
        settingsCanvas.SetActive(true);
        settingsPanel.Show();
        Canvas.ForceUpdateCanvases();
        PositionSettings();
    }

    public void CloseSettings()
    {
        if (!IsSettingsOpen) return;
        settingsPanel.Hide();
        settingsCanvas.SetActive(false);
        RestoreMenu(settingsEntry);
    }

    public void OpenCellCodex()
    {
        if (!isActiveAndEnabled || cellCodex == null || IsCodexOpen || loadingLevel || IsSettingsOpen ||
            (selectionCanvas != null && selectionCanvas.activeSelf)) return;
        BlockMenu();cellCodex.Show();
    }

    public void CloseCellCodex()
    {
        if (!IsCodexOpen) return;
        cellCodex.Hide();RestoreMenu(codexEntry);
    }

    private void BlockMenu()
    {
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        previousInteractable = menuInteraction.interactable;
        previousBlocksRaycasts = menuInteraction.blocksRaycasts;
        menuInteraction.interactable = false;
        menuInteraction.blocksRaycasts = false;
        if (mainMenuButtons != null)
            foreach (Button entry in mainMenuButtons) entry.GetComponent<MainMenuButtonFeedback>().ResetPointer();
    }

    private void RestoreMenu(Button fallback)
    {
        menuInteraction.interactable = previousInteractable;
        menuInteraction.blocksRaycasts = previousBlocksRaycasts;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null
                ? previousSelection : fallback != null ? fallback.gameObject : null);
    }

    public void LoadLevel(int index)
    {
        if (!isActiveAndEnabled || loadingLevel || IsSettingsOpen || !IsLevelDetailsOpen ||
            index != selectedLevel || CurrentChapter == null || index < 0 || index >= CurrentChapter.levels.Length) return;
        SelectionLevelArtwork entry = CurrentChapter.levels[index];
        if (entry.IsPreview) return;
        string sceneName = entry.sceneName;
        if (!progress.IsUnlocked(sceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Level scene is missing from Scenes In Build: {sceneName}", this);
            return;
        }
        loadingLevel = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void CreateLevelSelection()
    {
        // Keep this overlay outside the original Canvas so it has its own reference resolution.
        selectionCanvas = new GameObject("Level Selection Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        selectionCanvas.layer = LayerMask.NameToLayer("UI");
        SceneManager.MoveGameObjectToScene(selectionCanvas, gameObject.scene);
        Canvas canvas = selectionCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = selectionCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        MobileDisplay.ConfigureCanvas(canvas);

        // A full-screen raycast target prevents clicks from reaching the original Start button.
        GameObject shade = CreateUI("Backdrop", selectionCanvas.transform, typeof(Image));
        Stretch(shade.GetComponent<RectTransform>());
        LevelSelectionArtwork art = LevelSelectionArtwork.Defaults;
        shade.GetComponent<Image>().color = new Color(1f, .95f, .85f);
        GameObject background = CreateUI("Chapter Background", shade.transform, typeof(Image), typeof(AspectRatioFitter));
        selectionBackground = background.GetComponent<Image>();
        selectionBackground.raycastTarget = false;
        background.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        background.GetComponent<AspectRatioFitter>().aspectRatio = 1600f / 900f;
        if (art == null || art.chapters == null || art.chapters.Length == 0)
            throw new System.InvalidOperationException("Chapter artwork catalog is missing.");
        GameObject panel = CreateUI("Level Selection", shade.transform, typeof(Image));
        selectionPanel = panel.GetComponent<RectTransform>();
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.04f, 0.05f),
            new Vector2(0.96f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        panel.GetComponent<Image>().color = art != null ? Color.clear : new Color(0.28f, 0.1f, 0.17f, 1f);
        // The detail overlay is a sibling, so disable both pointer and keyboard access to the page underneath it.
        listInteraction = panel.AddComponent<CanvasGroup>();

        PixelImageText heading = CreateLabel("Title", panel.transform, "伤口防线", 32f);
        heading.gameObject.SetActive(false);
        Image titlePlate = AddTopArtwork("Title Plate", panel.transform, art.titlePlate, 16f, 72f, 0f);
        selectionTitlePanel = titlePlate.rectTransform;
        selectionTitlePanel.anchorMin = selectionTitlePanel.anchorMax = new Vector2(.5f, 1f);
        selectionTitlePanel.sizeDelta = new Vector2(560f, 72f);
        titlePlate.type = Image.Type.Sliced;
        titlePlate.preserveAspect = false;
        selectionTitle = CreateUI("Title Artwork", titlePlate.transform, typeof(Image)).GetComponent<Image>();
        selectionTitle.preserveAspect = true;
        selectionTitle.raycastTarget = false;
        Stretch(selectionTitle.rectTransform);
        selectionTitle.rectTransform.offsetMin = new Vector2(30f, 14f);
        selectionTitle.rectTransform.offsetMax = new Vector2(-30f, -14f);
        selectionChapterLabel = AddTopArtwork("Chapter Label", panel.transform, null, 8f, 22f, 0f);
        selectionChapterLabel.rectTransform.anchorMin = selectionChapterLabel.rectTransform.anchorMax = new Vector2(.5f, 1f);
        selectionChapterLabel.rectTransform.sizeDelta = new Vector2(220f, 22f);
        GameObject hint = CreateUI("Hint", panel.transform, typeof(Image));
        Image hintImage = hint.GetComponent<Image>();
        selectionHint = hintImage;
        hintImage.sprite = art != null ? art.overviewHint : null;
        hintImage.enabled = hintImage.sprite != null;
        hintImage.preserveAspect = true;
        hintImage.raycastTarget = false;
        SetRect(hint.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -108f), new Vector2(420f, 28f));
        Button back = CreateButton("Back", panel.transform, "返回");
        SetRect(back.GetComponent<RectTransform>(), Vector2.up, Vector2.up,
            Vector2.up, new Vector2(8f, -18f), new Vector2(156f, 64f));
        back.onClick.AddListener(BackFromSelection);
        back.GetComponentInChildren<PixelImageText>().gameObject.SetActive(false);
        back.image.color = Color.clear;
        Image arrow = CreateUI("Arrow Artwork", back.transform, typeof(Image)).GetComponent<Image>();
        arrow.sprite = art.backArrow;
        arrow.preserveAspect = true;
        arrow.raycastTarget = false;
        SetRect(arrow.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(72f, 64f));
        back.targetGraphic = arrow;
        ColorBlock arrowColors = back.colors;
        arrowColors.normalColor = Color.white;
        arrowColors.highlightedColor = arrowColors.selectedColor = new Color(.7f, 1f, 1f);
        back.colors = arrowColors;
        selectionBackCaption = CreateUI("Back Caption", back.transform, typeof(Image)).GetComponent<Image>();
        selectionBackCaption.preserveAspect = true;
        selectionBackCaption.raycastTarget = false;
        SetRect(selectionBackCaption.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(78f, 12f), new Vector2(72f, 40f));

        CreateChapterList(panel.transform, art);

        GameObject viewport = CreateUI("Level List", panel.transform,
            typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        listViewport = viewport.GetComponent<RectTransform>();
        Stretch(listViewport);
        listViewport.offsetMin = new Vector2(20f, 44f);
        listViewport.offsetMax = new Vector2(-20f, -154f);
        viewport.GetComponent<Image>().color = Color.clear;
        GameObject content = CreateUI("Level Cards", viewport.transform,
            typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        SetRect(contentRect, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        levelGrid = content.GetComponent<GridLayoutGroup>();
        levelGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        levelGrid.constraintCount = 3;
        levelGrid.spacing = new Vector2(16f, 16f);
        levelGrid.childAlignment = TextAnchor.UpperCenter;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        levelScroll = viewport.GetComponent<ScrollRect>();
        levelScroll.viewport = listViewport;
        levelScroll.content = contentRect;
        levelScroll.horizontal = false;
        levelScroll.vertical = true;
        levelScroll.movementType = ScrollRect.MovementType.Clamped;
        levelScroll.scrollSensitivity = 35f;

        levelButtons = new Button[LevelScenes.Length];
        levelStars = new StarRatingGraphic[LevelScenes.Length];
        levelLockHints = new PixelImageText[LevelScenes.Length];
        for (int i = 0; i < LevelScenes.Length; i++)
        {
            int levelIndex = i;
            Button card = CreateButton($"Level {i + 1}", content.transform, (i + 1).ToString());
            levelButtons[i] = card;
            card.interactable = Application.CanStreamedLevelBeLoaded(LevelScenes[i]);
            card.onClick.AddListener(() => OpenLevelDetails(levelIndex));
            card.image.color = Color.clear;
            GameObject face = CreateUI("Number Button", card.transform, typeof(Image));
            SetRect(face.GetComponent<RectTransform>(), Vector2.up, Vector2.one,
                new Vector2(.5f, 1f), new Vector2(0f, -4f), new Vector2(-8f, 152f));
            Image faceImage = face.GetComponent<Image>();
            faceImage.raycastTarget = false;
            card.targetGraphic = faceImage;
            if (art != null) art.Style(card, true, i);
            PixelImageText label = card.GetComponentInChildren<PixelImageText>();
            label.rectTransform.SetParent(face.transform, false);
            Stretch(label.rectTransform);
            label.fontSize = 80f;
            UseCleanLettering(label);
            label.enabled = art == null || !art.HasNumberBadge(i);
            GameObject rating = CreateUI("Best Stars", card.transform, typeof(StarRatingGraphic));
            levelStars[i] = rating.GetComponent<StarRatingGraphic>();
            levelStars[i].raycastTarget = false;
            SetRect(rating.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(.5f, 0f), new Vector2(0f, 12f), new Vector2(112f, 32f));
            levelLockHints[i] = CreateLabel("Level Locked", card.transform, "未开放", 18f);
            UseCleanLettering(levelLockHints[i]);
            SetRect(levelLockHints[i].rectTransform, Vector2.zero, Vector2.right, new Vector2(.5f, 0f),
                new Vector2(0f, 12f), new Vector2(-12f, 32f));
        }

        PixelImageText footer = CreateLabel("Footer", panel.transform,
            MobileDisplay.IsActive ? "滑动浏览 · 返回键返回" : "Esc 返回", 16f);
        selectionFooter = footer;
        SetRect(footer.rectTransform, Vector2.zero, Vector2.right,
            new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-40f, 24f));
        UseCleanLettering(footer);
        CreateLevelDetails();
        selectionCanvas.SetActive(false);
    }

    private void RefreshLevelProgress()
    {
        for (int i = 0; i < LevelScenes.Length; i++)
        {
            bool visible = CurrentChapter != null && i < CurrentChapter.levels.Length;
            levelButtons[i].gameObject.SetActive(visible);
            if (!visible) continue;
            SelectionLevelArtwork entry = CurrentChapter.levels[i];
            bool unlocked = entry.IsPreview || progress.IsUnlocked(entry.sceneName);
            levelButtons[i].interactable = unlocked && (entry.IsPreview || Application.CanStreamedLevelBeLoaded(entry.sceneName));
            levelButtons[i].targetGraphic.color = unlocked ? Color.white : new Color(.48f, .48f, .48f, .85f);
            levelLockHints[i].gameObject.SetActive(!unlocked);
            levelStars[i].gameObject.SetActive(!entry.IsPreview && unlocked);
            levelStars[i].Stars = entry.IsPreview ? 0 : progress.BestStars(entry.sceneName);
        }
    }

    private Image AddTopArtwork(string name, Transform parent, Sprite sprite, float top, float height, float padding = 20f)
    {
        Image image = CreateUI(name, parent, typeof(Image)).GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        SetRect(image.rectTransform, Vector2.up, Vector2.one, new Vector2(.5f, 1f),
            new Vector2(0f, -top), new Vector2(-padding * 2f, height));
        return image;
    }

    private void CreateChapterList(Transform parent, LevelSelectionArtwork art)
    {
        chapterList = CreateUI("Chapter List", parent, typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        chapterList.GetComponent<Image>().color = Color.clear;
        chapterViewport = chapterList.GetComponent<RectTransform>();
        Stretch(chapterViewport);
        chapterViewport.offsetMin = new Vector2(92f, 60f);
        chapterViewport.offsetMax = new Vector2(-92f, -154f);
        GameObject cards = CreateUI("Chapter Cards", chapterList.transform);
        chapterCards = cards.GetComponent<RectTransform>();
        Stretch(chapterCards);
        chapterScroll = chapterList.GetComponent<ScrollRect>();
        chapterScroll.viewport = chapterViewport;
        chapterScroll.content = cards.GetComponent<RectTransform>();
        chapterScroll.horizontal = false;
        chapterScroll.vertical = false;
        chapterScroll.movementType = ScrollRect.MovementType.Clamped;
        chapterScroll.scrollSensitivity = 35f;
        // Rebind three slots when paging; the frame and existing chapter artwork stay unchanged.
        chapterButtons = new Button[ChaptersPerPage];
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            int slot = i;
            Button card = CreateButton("Chapter " + (i + 1), cards.transform, "");
            card.GetComponentInChildren<PixelImageText>().gameObject.SetActive(false);
            chapterButtons[i] = card;
            art.StyleChapter(card);
            card.onClick.AddListener(() => OpenChapter(chapterPage * ChaptersPerPage + slot));
            AddTopArtwork("Chapter Cover", card.transform, null, 20f, 180f, 20f);
            AddTopArtwork("Chapter Label", card.transform, null, 212f, 24f);
            AddTopArtwork("Chapter Name", card.transform, null, 245f, 38f);
            AddTopArtwork("Chapter Tagline", card.transform, null, 292f, 24f);
            AddTopArtwork("Level Count", card.transform, null, 332f, 30f);
            PixelImageText number = CreateLabel("Planned Chapter Number", card.transform, "", 112f);
            UseCleanLettering(number);
            SetRect(number.rectTransform, Vector2.up, Vector2.one, new Vector2(.5f, 1f),
                new Vector2(0f, -48f), new Vector2(-40f, 140f));
            PixelImageText planned = CreateLabel("Planned Chapter Name", card.transform, "", 30f);
            UseCleanLettering(planned);
            SetRect(planned.rectTransform, Vector2.up, Vector2.one, new Vector2(.5f, 1f),
                new Vector2(0f, -228f), new Vector2(-40f, 50f));
            PixelImageText status = CreateLabel("Availability", card.transform, i == 0 ? "" : "开发中", 16f);
            UseCleanLettering(status);
            SetRect(status.rectTransform, Vector2.zero, Vector2.right, new Vector2(.5f, 0f),
                new Vector2(0f, 14f), new Vector2(-32f, 22f));
        }
        // Siblings of the masked viewport keep both arrow hit areas clear of the cards.
        chapterPaging = CreateUI("Chapter Paging", parent);
        Stretch(chapterPaging.GetComponent<RectTransform>());
        previousChapterPage = CreateChapterPageArrow("Previous Chapter Page", false, art);
        nextChapterPage = CreateChapterPageArrow("Next Chapter Page", true, art);
        previousChapterPage.onClick.AddListener(() => ChangeChapterPage(-1));
        nextChapterPage.onClick.AddListener(() => ChangeChapterPage(1));
        chapterPageLabel = CreateLabel("Chapter Page", chapterPaging.transform, "", 20f);
        UseCleanLettering(chapterPageLabel);
        SetRect(chapterPageLabel.rectTransform, Vector2.zero, Vector2.right, new Vector2(.5f, 0f),
            new Vector2(0f, 42f), new Vector2(-184f, 28f));
        RefreshChapterPage();
    }

    private Button CreateChapterPageArrow(string name, bool right, LevelSelectionArtwork art)
    {
        Button button = CreateButton(name, chapterPaging.transform, "");
        button.GetComponentInChildren<PixelImageText>().gameObject.SetActive(false);
        button.image.color = Color.clear;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Vector2 anchor = new Vector2(right ? 1f : 0f, .43f);
        SetRect(button.GetComponent<RectTransform>(), anchor, anchor, Vector2.one * .5f,
            new Vector2(right ? -44f : 44f, 0f), new Vector2(80f, 96f));
        Image arrow = CreateUI("Arrow Artwork", button.transform, typeof(Image)).GetComponent<Image>();
        arrow.sprite = art.backArrow;
        arrow.preserveAspect = true;
        arrow.raycastTarget = false;
        SetRect(arrow.rectTransform, Vector2.one * .5f, Vector2.one * .5f, Vector2.one * .5f,
            Vector2.zero, new Vector2(72f, 64f));
        if (right) arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        button.targetGraphic = arrow;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = colors.selectedColor = new Color(.7f, 1f, 1f);
        colors.disabledColor = new Color(.55f, .5f, .48f, .4f);
        button.colors = colors;
        return button;
    }

    public void ChangeChapterPage(int direction)
    {
        if (!IsChapterSelectionOpen || loadingLevel || direction == 0) return;
        int page = Mathf.Clamp(chapterPage + (direction > 0 ? 1 : -1), 0, ChapterPageCount - 1);
        if (page == chapterPage) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        chapterPage = page;
        RefreshChapterPage();
    }

    private void RefreshChapterPage()
    {
        LevelSelectionArtwork art = LevelSelectionArtwork.Defaults;
        for (int slot = 0; slot < chapterButtons.Length; slot++)
        {
            int index = chapterPage * ChaptersPerPage + slot;
            SelectionChapterArtwork chapter = index < art.chapters.Length ? art.chapters[index] : null;
            bool available = chapter != null && chapter.levels != null && chapter.levels.Length > 0;
            Button card = chapterButtons[slot];
            card.name = "Chapter " + (index + 1);
            card.interactable = available;
            SetChapterArtwork(card, "Chapter Cover", chapter?.cover);
            SetChapterArtwork(card, "Chapter Label", chapter?.label);
            SetChapterArtwork(card, "Chapter Name", chapter?.cardTitle);
            SetChapterArtwork(card, "Chapter Tagline", chapter?.tagline);
            SetChapterArtwork(card, "Level Count", chapter?.count);
            PixelImageText number = card.transform.Find("Planned Chapter Number").GetComponent<PixelImageText>();
            number.text = (index + 1).ToString();
            number.gameObject.SetActive(!available);
            PixelImageText planned = card.transform.Find("Planned Chapter Name").GetComponent<PixelImageText>();
            planned.text = "第 " + (index + 1) + " 章";
            planned.gameObject.SetActive(!available);
            card.transform.Find("Availability").GetComponent<PixelImageText>().text = available ? "" : "开发中";
        }
        previousChapterPage.interactable = chapterPage > 0;
        nextChapterPage.interactable = chapterPage < ChapterPageCount - 1;
        chapterPageLabel.text = (chapterPage + 1) + " / " + ChapterPageCount;
    }

    private static void SetChapterArtwork(Button card, string child, Sprite sprite)
    {
        Image image = card.transform.Find(child).GetComponent<Image>();
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void OpenChapter(int index)
    {
        LevelSelectionArtwork art = LevelSelectionArtwork.Defaults;
        if (!isActiveAndEnabled || loadingLevel || IsSettingsOpen || IsLevelDetailsOpen ||
            selectionCanvas == null || !selectionCanvas.activeSelf || index < 0 || index >= art.chapters.Length ||
            art.chapters[index] == null || art.chapters[index].levels == null || art.chapters[index].levels.Length == 0) return;
        selectedChapter = index;
        selectedLevel = -1;
        chapterList.SetActive(false);
        chapterPaging.SetActive(false);
        listViewport.gameObject.SetActive(true);
        selectionBackground.sprite = CurrentChapter.background;
        selectionTitle.sprite = CurrentChapter.title;
        selectionChapterLabel.sprite = CurrentChapter.label;
        selectionChapterLabel.enabled = true;
        selectionTitlePanel.anchoredPosition = new Vector2(0f, -38f);
        selectionHint.sprite = art.overviewHint;
        selectionBackCaption.sprite = art.chaptersCaption;
        selectionFooter.text = index == 0 ? "通关后开启下一关 · Esc 返回" :
            (MobileDisplay.IsActive ? "返回键返回" : "Esc 返回");
        RefreshLevelProgress();
        levelScroll.verticalNormalizedPosition = 1f;
        lastListWidth = -1f;
        Canvas.ForceUpdateCanvases();
        RefreshCardLayout();
        Canvas.ForceUpdateCanvases();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void ShowChapters(int focus)
    {
        LevelSelectionArtwork art = LevelSelectionArtwork.Defaults;
        selectedChapter = -1;
        selectedLevel = -1;
        chapterPage = focus >= 0 ? focus / ChaptersPerPage : 0;
        RefreshChapterPage();
        chapterList.SetActive(true);
        chapterPaging.SetActive(true);
        listViewport.gameObject.SetActive(false);
        selectionBackground.sprite = art.chapterBackground;
        selectionTitle.sprite = art.chapterTitle;
        selectionChapterLabel.enabled = false;
        selectionTitlePanel.anchoredPosition = new Vector2(0f, -16f);
        selectionHint.sprite = art.chapterHint;
        selectionBackCaption.sprite = art.homeCaption;
        selectionFooter.text = MobileDisplay.IsActive ? "左右切换章节 · 返回键返回" : "左右切换章节 · Esc 返回";
        chapterScroll.verticalNormalizedPosition = 1f;
        lastListWidth = -1f;
        Canvas.ForceUpdateCanvases();
        RefreshCardLayout();
        Canvas.ForceUpdateCanvases();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(focus >= 0
            ? chapterButtons[focus % ChaptersPerPage].gameObject : null);
    }

    public void ReturnToChapters()
    {
        if (loadingLevel || IsLevelDetailsOpen || selectionCanvas == null || !selectionCanvas.activeSelf || selectedChapter < 0) return;
        ShowChapters(selectedChapter);
    }

    private void BackFromSelection()
    {
        if (selectedChapter >= 0) ReturnToChapters();
        else CloseLevelSelection();
    }

    private static void UseCleanLettering(PixelImageText label)
    {
        ScoreArtworkResources art = ScoreArtworkResources.Defaults;
        if (art != null)
        {
            label.glyphAtlasOverride = art.scoreGlyphAtlas;
            label.smallGlyphAtlasOverride = art.smallScoreGlyphAtlas;
        }
        label.color = new Color(.25f, .07f, .17f);
    }

    private void CreateLevelDetails()
    {
        LevelSelectionArtwork art = LevelSelectionArtwork.Defaults;
        levelDetails = CreateUI("Level Details", selectionCanvas.transform, typeof(Image));
        Stretch(levelDetails.GetComponent<RectTransform>());
        levelDetails.GetComponent<Image>().color = new Color(.16f, .05f, .15f, .65f);
        GameObject panel = CreateUI("Details Panel", levelDetails.transform, typeof(Image));
        detailsPanel = panel.GetComponent<RectTransform>();
        SetRect(detailsPanel, Vector2.one * .5f, Vector2.one * .5f,
            Vector2.one * .5f, Vector2.zero, new Vector2(940f, 600f));
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = art != null ? art.panel : null;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;
        detailsName = CreateLabel("Level Name", panel.transform, "", 30f);
        UseCleanLettering(detailsName);
        SetRect(detailsName.rectTransform, Vector2.up, Vector2.one, new Vector2(.5f, 1f),
            new Vector2(0f, -24f), new Vector2(-48f, 44f));
        detailsTitle = AddTopArtwork("Level Title Artwork", panel.transform, null, 24f, 44f, 24f);
        GameObject preview = CreateUI("Map Preview", panel.transform, typeof(Image));
        detailsMap = preview.GetComponent<Image>();
        detailsMap.preserveAspect = true;
        detailsMap.raycastTarget = false;
        SetRect(detailsMap.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(30f, -104f), new Vector2(500f, 350f));
        detailsHistoryLabel = CreateDetailsImage("Historical Best Label", art != null ? art.historyLabel : null,
            new Vector2(560f, -145f), new Vector2(320f, 48f));
        detailsScore = CreateLabel("Historical Best Score", panel.transform, "", 64f);
        UseCleanLettering(detailsScore);
        SetRect(detailsScore.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(560f, -210f), new Vector2(320f, 86f));
        detailsUnplayed = CreateDetailsImage("Not Cleared", art != null ? art.unplayedLabel : null,
            new Vector2(560f, -225f), new Vector2(320f, 58f));
        detailsBestStarsLabel = CreateDetailsImage("Best Stars Label", art != null ? art.bestStarsLabel : null,
            new Vector2(560f, -320f), new Vector2(320f, 32f));
        GameObject stars = CreateUI("Historical Best Stars", panel.transform, typeof(StarRatingGraphic));
        detailsStars = stars.GetComponent<StarRatingGraphic>();
        detailsStars.raycastTarget = false;
        SetRect(detailsStars.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(630f, -370f), new Vector2(180f, 44f));
        Button back = CreateButton("Return To Numbers", panel.transform, "返回");
        detailsBack = back;
        detailsPreviewNotice = CreateLabel("Preview Notice", panel.transform, "开发中\n暂不能开始", 30f);
        UseCleanLettering(detailsPreviewNotice);
        SetRect(detailsPreviewNotice.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(560f, -205f), new Vector2(320f, 140f));
        detailsPreviewNotice.gameObject.SetActive(false);
        SetRect(back.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(.5f, 0f),
            new Vector2(260f, 24f), new Vector2(230f, 62f));
        back.onClick.AddListener(CloseLevelDetails);
        detailsStart = CreateButton("Start Selected Level", panel.transform, "开始游戏");
        SetRect(detailsStart.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(.5f, 0f),
            new Vector2(680f, 24f), new Vector2(230f, 62f));
        detailsStart.onClick.AddListener(() => LoadLevel(selectedLevel));
        UseCleanLettering(detailsStart.GetComponentInChildren<PixelImageText>());
        if (art != null)
        {
            art.Style(back);
            art.Style(detailsStart);
            AddButtonArtwork(back, art.returnLabel);
            AddButtonArtwork(detailsStart, art.startLabel);
        }
        levelDetails.SetActive(false);
    }

    private Image CreateDetailsImage(string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject obj = CreateUI(name, detailsPanel, typeof(Image));
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        SetRect(image.rectTransform, Vector2.up, Vector2.up, Vector2.up, position, size);
        return image;
    }

    private void AddButtonArtwork(Button button, Sprite sprite)
    {
        if (sprite == null) return;
        button.GetComponentInChildren<PixelImageText>().gameObject.SetActive(false);
        GameObject obj = CreateUI("Label Artwork", button.transform, typeof(Image));
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Stretch(image.rectTransform);
        image.rectTransform.offsetMin = new Vector2(16f, 8f);
        image.rectTransform.offsetMax = new Vector2(-16f, -8f);
    }

    public void OpenLevelDetails(int index)
    {
        if (!isActiveAndEnabled || loadingLevel || IsSettingsOpen || selectionCanvas == null ||
            !selectionCanvas.activeSelf || IsLevelDetailsOpen || CurrentChapter == null ||
            index < 0 || index >= CurrentChapter.levels.Length) return;
        SelectionLevelArtwork entry = CurrentChapter.levels[index];
        if (!entry.IsPreview && !progress.IsUnlocked(entry.sceneName)) return;
        selectedLevel = index;
        bool preview = entry.IsPreview;
        string scene = entry.sceneName;
        bool cleared = !preview && progress.HasCleared(scene);
        detailsName.text = selectedChapter == 0 ? LevelTitles[index] : entry.name;
        detailsTitle.sprite = entry.title;
        detailsTitle.gameObject.SetActive(entry.title != null);
        detailsName.gameObject.SetActive(entry.title == null);
        detailsMap.sprite = selectedChapter == 0 && levelArtwork != null && index < levelArtwork.Length
            ? levelArtwork[index] : entry.map;
        detailsMap.enabled = detailsMap.sprite != null;
        detailsScore.text = cleared ? progress.BestScore(scene).ToString() : "";
        detailsScore.gameObject.SetActive(cleared);
        detailsUnplayed.gameObject.SetActive(!preview && !cleared);
        detailsStars.gameObject.SetActive(!preview);
        detailsStars.Stars = preview ? 0 : progress.BestStars(scene);
        detailsHistoryLabel.gameObject.SetActive(!preview);
        detailsBestStarsLabel.gameObject.SetActive(!preview);
        detailsPreviewNotice.gameObject.SetActive(preview);
        detailsStart.interactable = !preview && Application.CanStreamedLevelBeLoaded(scene);
        detailsStart.image.color = preview ? new Color(.75f, .7f, .65f) : Color.white;
        PixelImageText startText = detailsStart.GetComponentInChildren<PixelImageText>(true);
        startText.text = preview ? "开发中" : "开始游戏";
        Transform startArtwork = detailsStart.transform.Find("Label Artwork");
        if (startArtwork != null) startArtwork.gameObject.SetActive(!preview);
        startText.gameObject.SetActive(preview || startArtwork == null);
        listInteraction.interactable = false;
        listInteraction.blocksRaycasts = false;
        levelDetails.SetActive(true);
        PositionLevelDetails();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseLevelDetails()
    {
        if (!IsLevelDetailsOpen || loadingLevel) return;
        levelDetails.SetActive(false);
        listInteraction.interactable = true;
        listInteraction.blocksRaycasts = true;
        if (selectedLevel >= 0 && selectedLevel < levelButtons.Length && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(levelButtons[selectedLevel].gameObject);
    }

    private void PositionLevelDetails()
    {
        if (!IsLevelDetailsOpen) return;
        Canvas canvas = selectionCanvas.GetComponent<Canvas>();
        Rect safe = MobileDisplay.SafeArea;
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        float fit = Mathf.Clamp01(Mathf.Min((safe.width / scale - 24f) / 940f,
            (safe.height / scale - 24f) / 600f));
        detailsPanel.localScale = Vector3.one * fit;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(selectionCanvas.GetComponent<RectTransform>(),
            safe.center, null, out Vector2 center);
        detailsPanel.anchoredPosition = center;
    }

    private void CreateSettings()
    {
        settingsCanvas = new GameObject("Main Menu Settings Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        settingsCanvas.layer = LayerMask.NameToLayer("UI");
        SceneManager.MoveGameObjectToScene(settingsCanvas, gameObject.scene);
        Canvas canvas = settingsCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = settingsCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = .5f;
        MobileDisplay.ConfigureCanvas(canvas);
        GameObject backdrop = CreateUI("Settings Backdrop", settingsCanvas.transform, typeof(Image));
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, .6f);
        settingsPanel = settingsCanvas.AddComponent<GameSettingsPanel>();
        settingsPanel.Initialize(backdrop.transform, OpenFieldUiResources.LoadDefaults(), CloseSettings);
        settingsCanvas.SetActive(false);
    }

    private void PositionSettings()
    {
        if (!IsSettingsOpen) return;
        Canvas canvas = settingsCanvas.GetComponent<Canvas>();
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        Rect safe = MobileDisplay.SafeArea;
        float fit = Mathf.Clamp01(Mathf.Min((safe.width / scale - 24f) / 640f,
            (safe.height / scale - 24f) / 800f));
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(settingsCanvas.GetComponent<RectTransform>(),
            safe.center, eventCamera, out Vector2 center);
        settingsPanel.Position(center, fit);
    }

    private void RefreshCardLayout()
    {
        if (MobileDisplay.IsActive)
        {
            Rect safe = MobileDisplay.SafeArea;
            selectionPanel.anchorMin = new Vector2((safe.x + safe.width * .04f) / Screen.width,
                (safe.y + safe.height * .05f) / Screen.height);
            selectionPanel.anchorMax = new Vector2((safe.xMax - safe.width * .04f) / Screen.width,
                (safe.yMax - safe.height * .05f) / Screen.height);
        }
        selectionTitlePanel.sizeDelta = new Vector2(Mathf.Min(560f,
            Mathf.Max(280f, selectionPanel.rect.width - 352f)), 72f);
        RectTransform viewport = selectedChapter < 0 ? chapterViewport : listViewport;
        float width = viewport.rect.width;
        float height = viewport.rect.height;
        if (width <= 0f || height <= 0f ||
            (Mathf.Abs(width - lastListWidth) < 1f && Mathf.Abs(height - lastListHeight) < 1f)) return;
        lastListWidth = width;
        lastListHeight = height;
        if (selectedChapter < 0)
        {
            // Fit the complete chapter composition, including every card's artwork.
            // The previous fixed-height grid clipped cards and put chapter 3 offscreen.
            int chapterColumns = Mathf.Min(3, chapterButtons.Length);
            int chapterRows = Mathf.CeilToInt((float)chapterButtons.Length / chapterColumns);
            float fit = Mathf.Min(1f, Mathf.Min((width - 16f) / (chapterColumns * 360f - 20f),
                (height - 16f) / (chapterRows * 430f - 20f)));
            fit = Mathf.Max(.01f, fit);
            for (int i = 0; i < chapterButtons.Length; i++)
            {
                RectTransform card = chapterButtons[i].GetComponent<RectTransform>();
                SetRect(card, Vector2.one * .5f, Vector2.one * .5f, Vector2.one * .5f,
                    new Vector2((i % chapterColumns - (chapterColumns - 1) * .5f) * 360f * fit,
                        ((chapterRows - 1) * .5f - i / chapterColumns) * 430f * fit), new Vector2(340f, 410f));
                card.localScale = Vector3.one * fit;
            }
            return;
        }
        int columns = Mathf.Clamp(Mathf.FloorToInt((width + 24f) / 184f), 1, CurrentChapter.levels.Length);
        levelGrid.constraintCount = columns;
        levelGrid.spacing = new Vector2(24f, 24f);
        levelGrid.cellSize = new Vector2(Mathf.Min(160f, (width - (columns - 1) * 24f) / columns), 210f);
        int rows = Mathf.CeilToInt((float)CurrentChapter.levels.Length / columns);
        float rowsHeight = rows * 210f + (rows - 1) * 24f;
        int padding = Mathf.Max(24, Mathf.FloorToInt((height - rowsHeight) * .5f));
        levelGrid.padding = new RectOffset(0, 0, padding, padding);
    }

    private Button CreateButton(string name, Transform parent, string text)
    {
        GameObject obj = CreateUI(name, parent, typeof(Image), typeof(Button));
        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.89f, 0.73f, 1f);
        colors.highlightedColor = new Color(1f, 0.97f, 0.87f, 1f);
        colors.selectedColor = new Color(0.72f, 0.96f, 0.88f, 1f);
        colors.pressedColor = new Color(0.94f, 0.65f, 0.55f, 1f);
        colors.disabledColor = new Color(0.55f, 0.48f, 0.46f, 0.8f);
        button.colors = colors;
        PixelImageText label = CreateLabel("Label", obj.transform, text, 28f);
        label.color = new Color(0.25f, 0.07f, 0.17f, 1f);
        Stretch(label.rectTransform);
        return button;
    }

    private PixelImageText CreateLabel(string name, Transform parent, string text, float size)
    {
        GameObject obj = CreateUI(name, parent, typeof(PixelImageText));
        PixelImageText label = obj.GetComponent<PixelImageText>();

        label.text = text;
        label.fontSize = size;
        label.color = new Color(1f, 0.94f, 0.82f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject CreateUI(string name, Transform parent, params System.Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        foreach (System.Type component in components) obj.AddComponent(component);
        return obj;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnEnable()
    {
        // Subscribe after Awake creates CanvasScaler, so layout uses its updated scale.
        Canvas.preWillRenderCanvases += PositionSettings;
    }

    private void OnDisable()
    {
        AndroidPrivacy.SetMenuEntryVisible(false);
        Canvas.preWillRenderCanvases -= PositionSettings;
        CloseSettings();
        CloseLevelSelection();
        CloseCellCodex();
    }

    private void OnDestroy()
    {
        if (selectionCanvas != null) Destroy(selectionCanvas);
        if (settingsCanvas != null) Destroy(settingsCanvas);
        if (settingsEntry != null) Destroy(settingsEntry.gameObject);
        if (quitEntry != null) Destroy(quitEntry.gameObject);
        if (codexEntry != null) Destroy(codexEntry.gameObject);
        if (cellCodex != null) Destroy(cellCodex.gameObject);
    }
}
