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
    private Button selectionEntry, settingsEntry, quitEntry;
    private Button[] levelButtons;
    private StarRatingGraphic[] levelStars;
    private CanvasGroup listInteraction;
    private GameObject levelDetails;
    private RectTransform detailsPanel;
    private Image detailsMap, detailsUnplayed;
    private PixelImageText detailsName, detailsScore;
    private StarRatingGraphic detailsStars;
    private Button detailsStart;
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
    public bool QuitRequested { get; private set; }

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
        selectionEntry = CreateButton("Level Selection Entry", menuContent, "选择关卡");
        SetRect(selectionEntry.GetComponent<RectTransform>(), Vector2.right, Vector2.right,
            new Vector2(1f, 0f), new Vector2(-48f, 36f), new Vector2(320f, 76f));
        selectionEntry.onClick.AddListener(OpenLevelSelection);
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
        AndroidPrivacy.SetMenuEntryVisible(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
    }

    public void HandleEscape()
    {
        if (IsSettingsOpen) CloseSettings();
        else if (IsLevelDetailsOpen) CloseLevelDetails();
        else if (selectionCanvas != null && selectionCanvas.activeSelf) CloseLevelSelection();
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
        if (!isActiveAndEnabled || loadingLevel || QuitRequested || IsSettingsOpen ||
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
        if (!isActiveAndEnabled || selectionCanvas == null || selectionCanvas.activeSelf || loadingLevel || IsSettingsOpen) return;
        BlockMenu();
        selectionCanvas.SetActive(true);
        RefreshLevelProgress();
        levelScroll.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
        RefreshCardLayout();
        Canvas.ForceUpdateCanvases();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(levelButtons[0].gameObject);
    }

    public void CloseLevelSelection()
    {
        if (selectionCanvas == null || !selectionCanvas.activeSelf || loadingLevel) return;
        CloseLevelDetails();
        selectionCanvas.SetActive(false);
        RestoreMenu(selectionEntry);
    }

    public void OpenSettings()
    {
        if (!isActiveAndEnabled || settingsCanvas == null || IsSettingsOpen || loadingLevel ||
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

    private void BlockMenu()
    {
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        previousInteractable = menuInteraction.interactable;
        previousBlocksRaycasts = menuInteraction.blocksRaycasts;
        menuInteraction.interactable = false;
        menuInteraction.blocksRaycasts = false;
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
        if (!isActiveAndEnabled || loadingLevel || IsSettingsOpen || index < 0 || index >= LevelScenes.Length) return;
        string sceneName = LevelScenes[index];
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
        shade.GetComponent<Image>().sprite = art != null ? art.background : null;
        shade.GetComponent<Image>().color = art != null ? Color.white : new Color(0.16f, 0.04f, 0.1f, 0.94f);
        GameObject panel = CreateUI("Level Selection", shade.transform, typeof(Image));
        selectionPanel = panel.GetComponent<RectTransform>();
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.04f, 0.05f),
            new Vector2(0.96f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        panel.GetComponent<Image>().color = art != null ? Color.clear : new Color(0.28f, 0.1f, 0.17f, 1f);
        // The detail overlay is a sibling, so disable both pointer and keyboard access to the page underneath it.
        listInteraction = panel.AddComponent<CanvasGroup>();

        PixelImageText heading = CreateLabel("Title", panel.transform, "伤口防线", 32f);
        UseCleanLettering(heading);
        SetRect(heading.rectTransform, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-240f, 44f));
        GameObject hint = CreateUI("Hint", panel.transform, typeof(Image));
        Image hintImage = hint.GetComponent<Image>();
        hintImage.sprite = art != null ? art.overviewHint : null;
        hintImage.enabled = hintImage.sprite != null;
        hintImage.preserveAspect = true;
        hintImage.raycastTarget = false;
        SetRect(hint.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -64f), new Vector2(340f, 28f));
        Button back = CreateButton("Back", panel.transform, "返回");
        SetRect(back.GetComponent<RectTransform>(), Vector2.one, Vector2.one,
            Vector2.one, new Vector2(-20f, -18f), new Vector2(104f, 42f));
        back.onClick.AddListener(CloseLevelSelection);
        UseCleanLettering(back.GetComponentInChildren<PixelImageText>());
        if (art != null) art.Style(back);

        GameObject viewport = CreateUI("Level List", panel.transform,
            typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        listViewport = viewport.GetComponent<RectTransform>();
        Stretch(listViewport);
        listViewport.offsetMin = new Vector2(20f, 44f);
        listViewport.offsetMax = new Vector2(-20f, -104f);
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
            if (art != null) art.Style(card, true);
            PixelImageText label = card.GetComponentInChildren<PixelImageText>();
            label.rectTransform.SetParent(face.transform, false);
            Stretch(label.rectTransform);
            label.fontSize = 80f;
            UseCleanLettering(label);
            GameObject rating = CreateUI("Best Stars", card.transform, typeof(StarRatingGraphic));
            levelStars[i] = rating.GetComponent<StarRatingGraphic>();
            levelStars[i].raycastTarget = false;
            SetRect(rating.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(.5f, 0f), new Vector2(0f, 12f), new Vector2(112f, 32f));
        }

        PixelImageText footer = CreateLabel("Footer", panel.transform,
            MobileDisplay.IsActive ? "滑动浏览关卡 · 返回键关闭" : "Esc 返回主菜单", 16f);
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
            levelStars[i].Stars = progress.BestStars(LevelScenes[i]);
        }
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
        GameObject preview = CreateUI("Map Preview", panel.transform, typeof(Image));
        detailsMap = preview.GetComponent<Image>();
        detailsMap.preserveAspect = true;
        detailsMap.raycastTarget = false;
        SetRect(detailsMap.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(30f, -104f), new Vector2(500f, 350f));
        CreateDetailsImage("Historical Best Label", art != null ? art.historyLabel : null,
            new Vector2(560f, -145f), new Vector2(320f, 48f));
        detailsScore = CreateLabel("Historical Best Score", panel.transform, "", 64f);
        UseCleanLettering(detailsScore);
        SetRect(detailsScore.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(560f, -210f), new Vector2(320f, 86f));
        detailsUnplayed = CreateDetailsImage("Not Cleared", art != null ? art.unplayedLabel : null,
            new Vector2(560f, -225f), new Vector2(320f, 58f));
        CreateDetailsImage("Best Stars Label", art != null ? art.bestStarsLabel : null,
            new Vector2(560f, -320f), new Vector2(320f, 32f));
        GameObject stars = CreateUI("Historical Best Stars", panel.transform, typeof(StarRatingGraphic));
        detailsStars = stars.GetComponent<StarRatingGraphic>();
        detailsStars.raycastTarget = false;
        SetRect(detailsStars.rectTransform, Vector2.up, Vector2.up, Vector2.up,
            new Vector2(630f, -370f), new Vector2(180f, 44f));
        Button back = CreateButton("Return To Numbers", panel.transform, "返回");
        SetRect(back.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(.5f, 0f),
            new Vector2(260f, 24f), new Vector2(230f, 62f));
        back.onClick.AddListener(CloseLevelDetails);
        detailsStart = CreateButton("Start Selected Level", panel.transform, "开始游戏");
        SetRect(detailsStart.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(.5f, 0f),
            new Vector2(680f, 24f), new Vector2(230f, 62f));
        detailsStart.onClick.AddListener(() => LoadLevel(selectedLevel));
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
            !selectionCanvas.activeSelf || index < 0 || index >= LevelScenes.Length) return;
        selectedLevel = index;
        string scene = LevelScenes[index];
        bool cleared = progress.HasCleared(scene);
        detailsName.text = LevelTitles[index];
        detailsMap.sprite = levelArtwork != null && index < levelArtwork.Length ? levelArtwork[index] : null;
        detailsMap.enabled = detailsMap.sprite != null;
        detailsScore.text = cleared ? progress.BestScore(scene).ToString() : "";
        detailsScore.gameObject.SetActive(cleared);
        detailsUnplayed.gameObject.SetActive(!cleared);
        detailsStars.Stars = progress.BestStars(scene);
        detailsStart.interactable = Application.CanStreamedLevelBeLoaded(scene);
        listInteraction.interactable = false;
        listInteraction.blocksRaycasts = false;
        levelDetails.SetActive(true);
        PositionLevelDetails();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(detailsStart.gameObject);
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
        float width = listViewport.rect.width;
        float height = listViewport.rect.height;
        if (width <= 0f || height <= 0f ||
            (Mathf.Abs(width - lastListWidth) < 1f && Mathf.Abs(height - lastListHeight) < 1f)) return;
        lastListWidth = width;
        lastListHeight = height;
        int columns = Mathf.Clamp(Mathf.FloorToInt((width + 24f) / 184f), 1, 5);
        levelGrid.constraintCount = columns;
        levelGrid.spacing = new Vector2(24f, 24f);
        levelGrid.cellSize = new Vector2(Mathf.Min(160f, (width - (columns - 1) * 24f) / columns), 210f);
        int rows = Mathf.CeilToInt((float)LevelScenes.Length / columns);
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
    }

    private void OnDestroy()
    {
        if (selectionCanvas != null) Destroy(selectionCanvas);
        if (selectionEntry != null) Destroy(selectionEntry.gameObject);
        if (settingsCanvas != null) Destroy(settingsCanvas);
        if (settingsEntry != null) Destroy(settingsEntry.gameObject);
        if (quitEntry != null) Destroy(quitEntry.gameObject);
    }
}
