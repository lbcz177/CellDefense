using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum FirstLevelTutorialStep
{
    SelectCell, BuildGuards, StartWave, ObserveBattle, BuildBarrier, BarrierInfo,
    BuildPhagocyte, ReinforceDefense, UpgradeTower, FinalBattle, Finished, Skipped
}

// Optional guidance observes successful gameplay actions; it never spends ATP or gates combat.
public sealed class FirstLevelTutorial : MonoBehaviour
{
    private OpenFieldPrototypeController owner;
    private OpenFieldGridModel grid;
    private Func<Vector2Int, Vector3> cellToWorld;
    private Camera gameplayCamera;
    private OpenFieldBuildMenu buildMenu;
    private OpenFieldTowerMenu towerMenu;
    private OpenFieldEntranceWaveButtons entranceButtons;
    private Canvas canvas;
    private RectTransform canvasRect, panel, highlight;
    private PixelImageText title, description, nextLabel;
    private Button next;
    private readonly List<Vector2Int> route = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> routeCells = new HashSet<Vector2Int>();
    private readonly Vector3[] corners = new Vector3[4];
    private Image targetFrame;
    private bool subscribed, skipped, barrierExplained, upgradeExplained, barrierLost;
    private Vector2Int? suggestedCell;
    private int choiceIndex;
    private TowerController upgradeTarget;
    public FirstLevelTutorialStep Step { get; private set; }
    public bool IsVisible => panel != null && panel.gameObject.activeInHierarchy;
    public Vector2Int? SuggestedCell => suggestedCell;

    public void Initialize(OpenFieldPrototypeController controller, TMP_FontAsset font, Camera camera,
        OpenFieldGridModel model, Func<Vector2Int, Vector3> toWorld, OpenFieldBuildMenu shop,
        OpenFieldTowerMenu details, OpenFieldEntranceWaveButtons entrances)
    {
        owner = controller;
        grid = model;
        cellToWorld = toWorld;
        gameplayCamera = camera;
        buildMenu = shop;
        towerMenu = details;
        entranceButtons = entrances;
        GameObject root = Create("First Level Tutorial Canvas", transform, typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 19;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = .5f;
        MobileDisplay.ConfigureCanvas(canvas);
        canvasRect = root.GetComponent<RectTransform>();
        panel = Create("Tutorial Hint", canvasRect, typeof(Image)).GetComponent<RectTransform>();
        PromptImageResources.StylePanel(panel.GetComponent<Image>());
        panel.sizeDelta = new Vector2(448f, 216f);
        title = Text("Lesson Title", panel, font, 21f, new Color(1f, .84f, .46f));
        Place(title.rectTransform, 14f, -12f, 290f, 30f);
        description = Text("Lesson Instructions", panel, font, 18f, new Color(.96f, .97f, 1f));
        description.enableAutoSizing = true;
        description.fontSizeMin = 15f;
        description.fontSizeMax = 18f;
        Place(description.rectTransform, 18f, -54f, 412f, 108f);
        Button skip = MakeButton("Skip Tutorial", panel, font, "跳过引导", Skip);
        Place(skip.GetComponent<RectTransform>(), 320f, -8f, 114f, 40f);
        next = MakeButton("Continue Lesson", panel, font, "知道了", DismissLesson);
        nextLabel = next.GetComponentInChildren<PixelImageText>();
        Place(next.GetComponent<RectTransform>(), 306f, -166f, 128f, 40f);
        PixelImageText footer = Text("Free Play Hint", panel, font, 14f, new Color(.65f, .83f, .87f));
        footer.text = "可自由操作 · 引导不暂停战斗";
        Place(footer.rectTransform, 18f, -174f, 280f, 24f);
        highlight = Create("Tutorial Target Highlight", canvasRect).GetComponent<RectTransform>();
        targetFrame = highlight.gameObject.AddComponent<Image>();
        GuidanceArtwork art = GuidanceArtwork.Load();
        targetFrame.sprite = art != null ? art.tutorialFrame : null;
        targetFrame.type = Image.Type.Sliced;targetFrame.fillCenter = false;
        targetFrame.pixelsPerUnitMultiplier = 2.3f;targetFrame.raycastTarget = false;
        grid.TryGetPath(grid.GetSpawnCell(0), route);
        foreach (Vector2Int cell in route) routeCells.Add(cell);
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (subscribed || owner == null) return;
        owner.SelectionChanged += Refresh;
        owner.RoundStateChanged += Refresh;
        owner.TowerBuilt += HandleTowerChanged;
        owner.TowerUpgraded += HandleTowerChanged;
        owner.TowerSold += HandleTowerChanged;
        owner.TowerLost += HandleTowerLost;
        Canvas.preWillRenderCanvases += PositionBeforeRender;
        subscribed = true;
    }

    private void HandleTowerChanged(TowerController tower) => Refresh();
    private void HandleTowerLost(TowerController tower)
    {
        if (tower != null && tower.Definition.InterceptsAllEnemies) barrierLost = true;
        Refresh();
    }

    public void Skip()
    {
        skipped = true;
        owner.SuppressBoardInputThisFrame();
        Refresh();
    }

    public void DismissLesson()
    {
        if (Step == FirstLevelTutorialStep.BarrierInfo) barrierExplained = true;
        else if (Step == FirstLevelTutorialStep.UpgradeTower) upgradeExplained = true;
        owner.SuppressBoardInputThisFrame();
        Refresh();
    }

    public void Refresh()
    {
        if (owner == null || panel == null) return;
        if (skipped) Step = FirstLevelTutorialStep.Skipped;
        else if (!owner.HasActiveRun) Step = FirstLevelTutorialStep.Finished;
        else
        {
            int guards = 0;
            bool barrier = false, phagocyte = false, upgraded = false;
            upgradeTarget = null;
            foreach (TowerCombatState defender in owner.Defenders)
            {
                if (defender == null || !defender.IsAlive) continue;
                TowerController tower = defender.Tower;
                if (tower.GetComponent<TowerAttackBehaviour>() == null) guards++;
                if (tower.Definition.InterceptsAllEnemies) barrier = true;
                if (tower.GetComponent<PhagocyteAttackBehaviour>() != null) phagocyte = true;
                upgraded |= tower.IsUpgraded;
                if (upgradeTarget == null && tower.SupportsDamageUpgrade && !tower.IsUpgraded) upgradeTarget = tower;
            }
            if (owner.CurrentRound == 1)
                Step = guards < 2 ? (owner.SelectedBuildCell.HasValue ? FirstLevelTutorialStep.BuildGuards : FirstLevelTutorialStep.SelectCell)
                    : owner.Phase == OpenFieldPhase.Planning ? FirstLevelTutorialStep.StartWave : FirstLevelTutorialStep.ObserveBattle;
            else if (owner.CurrentRound == 2)
                Step = !barrier ? FirstLevelTutorialStep.BuildBarrier : !barrierExplained ? FirstLevelTutorialStep.BarrierInfo
                    : owner.Phase == OpenFieldPhase.Planning ? FirstLevelTutorialStep.StartWave : FirstLevelTutorialStep.ObserveBattle;
            else
                Step = !phagocyte ? FirstLevelTutorialStep.BuildPhagocyte : guards < 4 ? FirstLevelTutorialStep.ReinforceDefense
                    : !upgraded && !upgradeExplained && upgradeTarget != null
                    ? FirstLevelTutorialStep.UpgradeTower : owner.Phase == OpenFieldPhase.Planning
                    ? FirstLevelTutorialStep.StartWave : FirstLevelTutorialStep.FinalBattle;
            choiceIndex = Step == FirstLevelTutorialStep.BuildBarrier ? 3 : Step == FirstLevelTutorialStep.BuildPhagocyte ? 2 : 0;
            suggestedCell = FindSuggestedCell(choiceIndex != 0);
            UpdateCopy(guards);
        }
        UpdateVisibility();
    }

    private void UpdateCopy(int guards)
    {
        string heading, body;
        switch (Step)
        {
            case FirstLevelTutorialStep.SelectCell:
            case FirstLevelTutorialStep.BuildGuards:
                heading = $"第1轮 · 布置近卫 {Mathf.Min(guards, 2)}/2";
                body = owner.SelectedBuildCell.HasValue ? "选择高亮的近卫塔（25 ATP）。建议先建两座，让射程交叉覆盖敌人路线。"
                    : "点击高亮空格，再选择近卫塔。先布置两座交叉火力；敌人到达核心会扣生命，归零失败。";
                break;
            case FirstLevelTutorialStep.StartWave:
                heading = $"第{owner.CurrentRound}轮 · 准备开波";
                body = "点击高亮的入口按钮开始这一波。击杀获得 ATP，战斗中也可以继续建造；暂停可调整防线。";
                break;
            case FirstLevelTutorialStep.ObserveBattle:
                heading = $"第{owner.CurrentRound}轮 · 守住核心";
                body = owner.CurrentRound == 1 ? "观察交叉火力。敌人出完后会自动开下一波，可提前开始；暂停可调整防线。"
                    : "壁垒拦敌，后排输出。留意壁垒生命，破裂后可在空位补建；下一波学习巨噬与升级。";
                break;
            case FirstLevelTutorialStep.BuildBarrier:
                heading = barrierLost ? "第2轮 · 补建壁垒" : "第2轮 · 保护防线";
                body = owner.CurrentATP < 30 ? "壁垒需要30 ATP。可等待击杀收入，或点击已有塔确认出售；也可跳过引导继续战斗。"
                    : "点击路线上的高亮格，选择纤维蛋白壁垒（30 ATP）。放在近卫射程内，让后排保护它。";
                break;
            case FirstLevelTutorialStep.BarrierInfo:
                heading = "第2轮 · 壁垒如何承伤";
                body = "壁垒有500生命，拦住所有经过它的敌人，不改变路线。敌人越多，破裂越快；破裂后可补建。";
                break;
            case FirstLevelTutorialStep.BuildPhagocyte:
                heading = "第3轮 · 加入巨噬细胞";
                body = "在高亮格选择巨噬细胞（25 ATP）。它能拦住两名敌人，也能吞噬低血量目标，之后消化3秒。";
                break;
            case FirstLevelTutorialStep.UpgradeTower:
                heading = "第3轮 · 升级与调整";
                body = "点击高亮细胞，再点升级：支付其建造价，伤害提高50%。出售需确认，只返还实付费用的一半。";
                break;
            case FirstLevelTutorialStep.ReinforceDefense:
                heading = $"第3轮 · 增援近卫 {Mathf.Min(guards, 4)}/4";
                body = "末波有真菌Boss和孢子。用积累的ATP补足四座近卫，让交叉火力覆盖壁垒；巨噬不能吞噬Boss。";
                break;
            default:
                heading = "第3轮 · 完成防守";
                body = "清除最后一波全部敌人，核心存活即可胜利。根据战况补建、升级或出售；后续关卡可尝试抗原协作。";
                break;
        }
        if (title.text != heading) title.text = heading;
        if (description.text != body) description.text = body;
        next.gameObject.SetActive(Step == FirstLevelTutorialStep.BarrierInfo || Step == FirstLevelTutorialStep.UpgradeTower);
        nextLabel.text = Step == FirstLevelTutorialStep.UpgradeTower ? "稍后升级" : "知道了";
    }

    private Vector2Int? FindSuggestedCell(bool onRoute)
    {
        int middle = route.Count / 2;
        for (int offset = 0; offset < route.Count; offset++)
        {
            Vector2Int cell = route[(middle + offset) % route.Count];
            if (onRoute)
            {
                if (grid.CheckPlacement(cell, false) == OpenFieldPlacementResult.Valid) return cell;
                continue;
            }
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbor = cell + direction;
                if (!routeCells.Contains(neighbor) && grid.CheckPlacement(neighbor, false) == OpenFieldPlacementResult.Valid)
                    return neighbor;
            }
        }
        return null;
    }
    private static readonly Vector2Int[] Directions = { Vector2Int.down, Vector2Int.up, Vector2Int.right, Vector2Int.left };

    private void UpdateVisibility()
    {
        bool visible = isActiveAndEnabled && owner.isActiveAndEnabled && owner.HasActiveRun && !owner.IsPaused && !skipped && !towerMenu.IsConfirming;
        panel.gameObject.SetActive(visible);
        highlight.gameObject.SetActive(visible);
        if (visible) Position();
    }

    private Rect TargetBounds()
    {
        RectTransform target = null;
        if (Step == FirstLevelTutorialStep.StartWave) target = entranceButtons.FirstVisibleButton;
        else if (Step == FirstLevelTutorialStep.UpgradeTower)
        {
            if (owner.SelectedTower == upgradeTarget) target = towerMenu.UpgradeButtonRect;
            else if (upgradeTarget != null) return WorldCellBounds(upgradeTarget.GetComponent<TowerCombatState>().Cell);
        }
        else if (Step == FirstLevelTutorialStep.SelectCell || Step == FirstLevelTutorialStep.BuildGuards ||
            Step == FirstLevelTutorialStep.BuildBarrier || Step == FirstLevelTutorialStep.BuildPhagocyte ||
            Step == FirstLevelTutorialStep.ReinforceDefense)
        {
            target = buildMenu.GetChoiceRect(choiceIndex);
            if (target == null && suggestedCell.HasValue) return WorldCellBounds(suggestedCell.Value);
        }
        return target != null ? ScreenBounds(target) : default;
    }

    private Rect WorldCellBounds(Vector2Int cell)
    {
        Vector3 world = cellToWorld(cell);
        Vector2 center = gameplayCamera.WorldToScreenPoint(world);
        Vector2 half = (Vector2)gameplayCamera.WorldToScreenPoint(world + Vector3.one * .5f) - center;
        return new Rect(center - half, half * 2f);
    }

    private Rect ScreenBounds(RectTransform rect)
    {
        if (rect == null) return default;
        rect.GetWorldCorners(corners);
        Canvas source = rect.GetComponentInParent<Canvas>();
        Camera camera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void Position()
    {
        Rect safe = MobileDisplay.LocalSafeArea(canvasRect);
        float fit = Mathf.Min(1f, Mathf.Min((safe.width - 24f) / panel.sizeDelta.x, (safe.height - 24f) / panel.sizeDelta.y));
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Rect targetScreen = TargetBounds();
        float bestScore = float.PositiveInfinity;
        float bestFit = fit;
        Vector2 best = safe.center;
        // Keep the largest readable hint that leaves the enlarged build cards usable.
        for (int attempt = 0; attempt <= 7; attempt++)
        {
            float trialFit = fit * (1f - attempt * .05f);
            Vector2 size = panel.sizeDelta * trialFit;
            for (int i = 0; i < 4; i++)
            {
                Vector2 candidate = new Vector2(i % 2 == 0 ? safe.xMax - 12f - size.x * .5f : safe.xMin + 12f + size.x * .5f,
                    i < 2 ? safe.yMin + 12f + size.y * .5f : safe.yMax - 116f - size.y * .5f);
                candidate.y = Mathf.Clamp(candidate.y, safe.yMin + size.y * .5f, safe.yMax - size.y * .5f);
                Vector2 center = RectTransformUtility.WorldToScreenPoint(eventCamera, canvasRect.TransformPoint(candidate));
                Rect screenRect = new Rect(center - size * canvas.scaleFactor * .5f, size * canvas.scaleFactor);
                float score = (screenRect.Overlaps(targetScreen) ? 10f : 0f) +
                    (screenRect.Overlaps(ScreenBounds(buildMenu.PanelRect)) ? 10f : 0f) +
                    (screenRect.Overlaps(ScreenBounds(towerMenu.PanelRect)) ? 10f : 0f) +
                    (screenRect.Overlaps(owner.BattleHudScreenRect) ? 10f : 0f) + i * .01f;
                if (score < bestScore) { bestScore = score; best = candidate; bestFit = trialFit; }
            }
            if (bestScore < 1f) break;
        }
        panel.localScale = Vector3.one * Mathf.Max(.01f, bestFit);
        panel.anchoredPosition = best;
        bool showTarget = targetScreen.width > 0f && targetScreen.height > 0f;
        highlight.gameObject.SetActive(showTarget);
        if (!showTarget) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen.center, eventCamera, out Vector2 local);
        highlight.anchoredPosition = local;
        highlight.sizeDelta = targetScreen.size / Mathf.Max(.01f, canvas.scaleFactor) + Vector2.one * 8f;
        targetFrame.color = new Color(1f, 1f, 1f, .88f + .12f * Mathf.Sin(Time.unscaledTime * 4f));
    }

    public bool ContainsPointer(Vector2 screenPosition) => IsVisible &&
        RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
    private void LateUpdate() { if (owner != null) UpdateVisibility(); }
    private void PositionBeforeRender() { if (IsVisible) Position(); }
    private void OnEnable() { Subscribe(); Refresh(); }
    private void OnDisable()
    {
        if (subscribed && owner != null)
        {
            owner.SelectionChanged -= Refresh;
            owner.RoundStateChanged -= Refresh;
            owner.TowerBuilt -= HandleTowerChanged;
            owner.TowerUpgraded -= HandleTowerChanged;
            owner.TowerSold -= HandleTowerChanged;
            owner.TowerLost -= HandleTowerLost;
        }
        subscribed = false;
        Canvas.preWillRenderCanvases -= PositionBeforeRender;
        if (panel != null) panel.gameObject.SetActive(false);
        if (highlight != null) highlight.gameObject.SetActive(false);
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        foreach (Type component in components) obj.AddComponent(component);
        return obj;
    }
    private static PixelImageText Text(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        PixelImageText label = Create(name, parent, typeof(PixelImageText)).GetComponent<PixelImageText>();

        label.fontSize = size;
        label.color = color;
        label.raycastTarget = false;
        label.text = string.Empty;
        return label;
    }
    private static Button MakeButton(string name, Transform parent, TMP_FontAsset font, string caption, Action action)
    {
        Button button = Create(name, parent, typeof(Image), typeof(Button)).GetComponent<Button>();
        button.targetGraphic = button.GetComponent<Image>();
        button.image.color = new Color(.16f, .37f, .43f, 1f);
        PromptImageResources.StyleButton(button);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => { EventSystem.current?.SetSelectedGameObject(null); action(); });
        PixelImageText label = Text("Label", button.transform, font, 16f, Color.white);
        label.text = caption;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
