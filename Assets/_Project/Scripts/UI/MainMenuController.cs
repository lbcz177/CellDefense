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
    private RectTransform listViewport;
    private GridLayoutGroup levelGrid;
    private ScrollRect levelScroll;
    private GameObject previousSelection;
    private float lastListWidth = -1f;
    private bool loadingLevel;
    private bool previousInteractable;
    private bool previousBlocksRaycasts;
    public bool IsSettingsOpen => settingsCanvas != null && settingsCanvas.activeSelf;
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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
    }

    public void HandleEscape()
    {
        if (IsSettingsOpen) CloseSettings();
        else if (selectionCanvas != null && selectionCanvas.activeSelf) CloseLevelSelection();
    }

    private void LateUpdate()
    {
        if (selectionCanvas != null && selectionCanvas.activeSelf) RefreshCardLayout();
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
        shade.GetComponent<Image>().color = new Color(0.16f, 0.04f, 0.1f, 0.94f);
        GameObject panel = CreateUI("Level Selection", shade.transform, typeof(Image));
        selectionPanel = panel.GetComponent<RectTransform>();
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.04f, 0.05f),
            new Vector2(0.96f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        panel.GetComponent<Image>().color = new Color(0.28f, 0.1f, 0.17f, 1f);

        TextMeshProUGUI heading = CreateLabel("Title", panel.transform, "选择关卡", 32f);
        SetRect(heading.rectTransform, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-240f, 44f));
        TextMeshProUGUI hint = CreateLabel("Hint", panel.transform, "选择地图，开始防守", 18f);
        SetRect(hint.rectTransform, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(-40f, 26f));
        Button back = CreateButton("Back", panel.transform, "返回");
        SetRect(back.GetComponent<RectTransform>(), Vector2.one, Vector2.one,
            Vector2.one, new Vector2(-20f, -18f), new Vector2(104f, 42f));
        back.onClick.AddListener(CloseLevelSelection);

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
        for (int i = 0; i < LevelScenes.Length; i++)
        {
            int levelIndex = i;
            Button card = CreateButton($"Level {i + 1}", content.transform, LevelTitles[i]);
            levelButtons[i] = card;
            card.interactable = Application.CanStreamedLevelBeLoaded(LevelScenes[i]);
            card.onClick.AddListener(() => LoadLevel(levelIndex));
            TextMeshProUGUI label = card.GetComponentInChildren<TextMeshProUGUI>();
            SetRect(label.rectTransform, Vector2.zero, Vector2.right,
                new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-16f, 38f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 22f;
            GameObject preview = CreateUI("Map Preview", card.transform, typeof(Image));
            Image image = preview.GetComponent<Image>();
            image.sprite = levelArtwork != null && i < levelArtwork.Length ? levelArtwork[i] : null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = image.sprite != null;
            SetRect(image.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-20f, 128f));
        }

        TextMeshProUGUI footer = CreateLabel("Footer", panel.transform,
            MobileDisplay.IsActive ? "滑动浏览关卡 · 返回键关闭" : "Esc 返回主菜单", 16f);
        SetRect(footer.rectTransform, Vector2.zero, Vector2.right,
            new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-40f, 24f));
        selectionCanvas.SetActive(false);
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
        if (width <= 0f || Mathf.Abs(width - lastListWidth) < 1f) return;
        lastListWidth = width;
        int columns = Mathf.Clamp(Mathf.FloorToInt((width + 16f) / 288f), 1, 3);
        levelGrid.constraintCount = columns;
        levelGrid.cellSize = new Vector2(Mathf.Min(340f, (width - (columns - 1) * 16f) / columns), 196f);
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
        TextMeshProUGUI label = CreateLabel("Label", obj.transform, text, 28f);
        label.color = new Color(0.25f, 0.07f, 0.17f, 1f);
        Stretch(label.rectTransform);
        return button;
    }

    private TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size)
    {
        GameObject obj = CreateUI(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.font = menuFont;
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
