using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum OpenFieldPhase
{
    Planning,
    Assault,
    Victory,
    Defeat
}

public sealed class OpenFieldPrototypeController : MonoBehaviour
{
    private const int EnemyPoolPrewarmCount = 16;
    private const int EnemyPoolMaxInactivePerPrefab = 128;
    private static readonly ProfilerMarker SpawnEnemyMarker =
        new ProfilerMarker("CellDefense.Enemy.Spawn");

    [Header("Map")]
    [SerializeField, Min(3)] private int width = 15;
    [SerializeField, Min(3)] private int height = 11;
    [SerializeField, Min(0.1f)] private float cellSize = 1f;
    [SerializeField] private Vector2Int coreCell = new Vector2Int(7, 5);
    [SerializeField] private Vector2Int[] spawnCells =
    {
        new Vector2Int(0, 5),
        new Vector2Int(14, 5),
        new Vector2Int(7, 10)
    };
    [SerializeField] private Vector2Int[] blockedCells = Array.Empty<Vector2Int>();

    [Header("Artwork")]
    [SerializeField] private Sprite mapArtwork;
    [SerializeField] private bool showDebugGrid;

    [Header("Rounds")]
    [SerializeField] private WaveSequenceDefinition waveSequence;
    [SerializeField, Min(0)] private int initialATP = 100;
    [SerializeField, Min(0)] private int betweenRoundATP = 40;
    [SerializeField, Min(.1f)] private float autoWaveDelay = 10f;
    [SerializeField, Min(1)] private int initialCoreLife = 10;
    [SerializeField, Min(0.1f)] private float enemySpeedMultiplier = 0.85f;

    [Header("Combat")]
    [SerializeField] private TowerDefinition[] towerDefinitions;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField, Min(0.1f)] private float signalRadiusCells = 3f;
    [SerializeField, Range(0, 100)] private int sellRefundPercent = TowerEconomyRules.DefaultSellRefundPercent;
    [SerializeField] private Camera gameplayCamera;

    [Header("Build UI")]
    [SerializeField] private TMP_FontAsset buildMenuFont;
    [SerializeField] private Sprite waveButtonArtwork;
    [SerializeField] private OpenFieldBattleHud battleHud;
    [SerializeField] private bool enableFirstLevelTutorial;

    [Header("Result UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private string nextSceneName;

    private readonly Dictionary<Vector2Int, BuildSlot> placedSlots = new Dictionary<Vector2Int, BuildSlot>();
    private readonly HashSet<EnemyController> activeEnemies = new HashSet<EnemyController>();
    private readonly HashSet<EnemyController> scheduledEnemies = new HashSet<EnemyController>();
    private readonly LevelProgress progress = new LevelProgress();
    private int scheduledEnemyCount, killedScheduledEnemies;
    private LevelResultScoreView victoryScoreView, defeatScoreView;
    private readonly Dictionary<Vector2Int, TowerCombatState> cellDefenders =
        new Dictionary<Vector2Int, TowerCombatState>();
    private readonly Dictionary<EnemyController, Stack<EnemyController>> inactiveEnemiesByPrefab =
        new Dictionary<EnemyController, Stack<EnemyController>>();
    private readonly List<WaypointPath> roundPaths = new List<WaypointPath>();
    // Retained routing infrastructure for a future tower; current defenders never invoke it.
    private readonly List<WaypointPath> reroutedPaths = new List<WaypointPath>();
    private readonly List<Vector2Int> activeRouteStarts = new List<Vector2Int>();
    private readonly List<Vector2Int> cachedActiveRouteStarts = new List<Vector2Int>();
    private Vector2Int? cachedBarrierCell;
    private int cachedBarrierRevision = -1;
    private OpenFieldPlacementResult cachedBarrierPlacement;
    private readonly List<Vector2Int> pathCells = new List<Vector2Int>();
    private readonly EconomyService economy = new EconomyService();
    private readonly LifeService life = new LifeService();

    private OpenFieldGridModel grid;
    private OpenFieldPresentation presentation;
    private OpenFieldBuildMenu buildMenu;
    private OpenFieldTowerMenu towerMenu;
    private OpenFieldEntranceWaveButtons entranceWaveButtons;
    private EnemyStatusHud enemyHud;
    private OpenFieldPlaybackControls playbackControls;
    private FirstLevelTutorial tutorial;
    private Camera viewportBackgroundCamera;
    private int playbackInputBlockedFrame = -1;
    private readonly BoardTapGesture boardTap = new BoardTapGesture();
    private readonly List<RaycastResult> boardUiHits = new List<RaycastResult>();
    private Vector2Int? selectedBuildCell;
    private Vector2Int? selectedTowerCell;
    private int hoveredBuildIndex = -1;
    private Vector2Int? previewCell;
    private bool previewCellValid;
    private Transform enemyPoolRoot;
    private Coroutine spawnRoutine;
    private int roundIndex;
    private int selectedTowerIndex;
    private bool allEnemiesSpawned;
    private float autoWaveRemaining = -1f;
    private Func<Vector3, TowerCombatState> defenderLookup;
    private Func<bool> assaultCheck;

    public OpenFieldPhase Phase { get; private set; }
    public int CurrentRound => roundIndex + 1;
    public bool HasAutoWaveCountdown => autoWaveRemaining >= 0f && HasActiveRun;
    public float AutoWaveSecondsRemaining => Mathf.Max(0f, autoWaveRemaining);
    public Vector2Int? SelectedBuildCell => selectedBuildCell;
    public IEnumerable<TowerCombatState> Defenders => cellDefenders.Values;
    public IEnumerable<EnemyController> ActiveEnemies => activeEnemies;
    public Vector2 WorldToLogicalPosition(Vector3 world) => new Vector2(
        (world.x-transform.position.x)/cellSize+width*.5f,
        (world.y-transform.position.y)/cellSize+height*.5f);
    public event Action SelectionChanged;
    public event Action RoundStateChanged;
    public event Action<TowerController> TowerBuilt;
    public event Action<TowerController> TowerUpgraded;
    public event Action<TowerController> TowerSold;
    public event Action<TowerController> TowerLost;
    public int CurrentATP => economy.CurrentATP;
    public int CurrentCoreLife => life.CurrentLife;
    public LevelScoreResult FinalScore { get; private set; }
    public bool IsPaused { get; private set; }
    public int PlaybackSpeed { get; private set; } = 1;
    public bool HasActiveRun => Phase == OpenFieldPhase.Planning || Phase == OpenFieldPhase.Assault;
    public bool CanBuild => HasActiveRun && !IsPaused;
    public Vector2Int? SelectedTowerCell => selectedTowerCell;
    public TowerController SelectedTower => selectedTowerCell.HasValue &&
        TryGetLivingTower(selectedTowerCell.Value, out TowerController tower) ? tower : null;
    public Rect BattleHudScreenRect => battleHud != null ? battleHud.ScreenRect : default;
    public int TotalRounds => waveSequence != null ? waveSequence.WaveCount : 0;
    public int UpcomingRound => Phase == OpenFieldPhase.Planning ? CurrentRound : CurrentRound + 1;
    public bool CanStartWave => !IsPaused && waveSequence != null && roundIndex >= 0 && roundIndex < TotalRounds &&
        (Phase == OpenFieldPhase.Planning ||
         (Phase == OpenFieldPhase.Assault && allEnemiesSpawned && roundIndex < TotalRounds - 1));
    public int UpcomingEntranceCount
    {
        get
        {
            if (Phase != OpenFieldPhase.Planning && Phase != OpenFieldPhase.Assault) return 0;
            int index = Phase == OpenFieldPhase.Planning ? roundIndex : roundIndex + 1;
            return grid != null && index >= 0 && index < TotalRounds
                ? Mathf.Min(grid.SpawnCount, waveSequence.GetWave(index).ScheduledEnemyCount) : 0;
        }
    }
    public string DefenseStatusText => "壁垒可承伤 · 拦截全部经过的敌人";
    public string UpcomingWaveSummary
    {
        get
        {
            int index = UpcomingRound - 1;
            if (index < 0 || index >= TotalRounds) return "最后一波";
            WaveDefinition wave = waveSequence.GetWave(index);
            var parts = new List<string>();
            for (int group = 0; group < wave.GroupCount; group++)
                parts.Add($"入口{wave.GetGroupEntrance(group) + 1} {wave.GetGroupEnemy(group).DisplayName}×{wave.GetGroupCount(group)}");
            return string.Join(" / ", parts);
        }
    }

    public bool UpcomingUsesEntrance(int entrance)
    {
        int index = Phase == OpenFieldPhase.Planning ? roundIndex : roundIndex + 1;
        if (grid == null || entrance < 0 || entrance >= grid.SpawnCount || index < 0 || index >= TotalRounds) return false;
        WaveDefinition wave = waveSequence.GetWave(index);
        for (int group = 0; group < wave.GroupCount; group++)
            if (wave.GetGroupEntrance(group) == entrance ||
                (wave.GetGroupEntrance(group) < 0 && entrance < Mathf.Min(grid.SpawnCount, wave.ScheduledEnemyCount))) return true;
        return false;
    }

    private void Awake()
    {
        if (waveSequence == null || waveSequence.WaveCount < 1)
        {
            throw new InvalidOperationException("Open field levels require at least one wave.");
        }
        if (towerDefinitions == null || towerDefinitions.Length == 0)
        {
            throw new InvalidOperationException("Assign at least one tower definition.");
        }
        if (projectilePool == null)
        {
            throw new InvalidOperationException("Assign a projectile pool.");
        }
        if (buildMenuFont == null)
        {
            throw new InvalidOperationException("Assign the build menu's Chinese font.");
        }
        waveButtonArtwork = OpenFieldUiResources.ResolveWaveButtonArtwork(waveButtonArtwork);
        if (waveButtonArtwork == null)
            throw new InvalidOperationException("Entrance wave button artwork is missing from the scene and default UI resources.");
        if (cellSize <= 0f || signalRadiusCells <= 0f)
        {
            throw new InvalidOperationException("Cell size and signal radius must be positive.");
        }
        if (sellRefundPercent < 0 || sellRefundPercent > 100)
            throw new InvalidOperationException("Sell refund percent must be between 0 and 100.");
        defenderLookup = FindDefender;
        assaultCheck = () => isActiveAndEnabled && Phase == OpenFieldPhase.Assault;

        gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (gameplayCamera == null)
        {
            throw new InvalidOperationException("Assign a gameplay camera.");
        }

        grid = new OpenFieldGridModel(width, height, coreCell, spawnCells, blockedCells);
        ConfigureMapViewport();
        for (int i = 0; i < waveSequence.WaveCount; i++)
        {
            WaveDefinition wave = waveSequence.GetWave(i);
            wave.Validate();
            for (int group = 0; group < wave.GroupCount; group++)
                if (wave.GetGroupEntrance(group) >= grid.SpawnCount)
                    throw new InvalidOperationException("Wave group entrance is outside this map.");
            scheduledEnemyCount = checked(scheduledEnemyCount + wave.ScheduledEnemyCount);
        }
        projectilePool.Initialize();
        economy.Initialize(initialATP);
        life.Initialize(initialCoreLife);
        ResetPlayback();
        roundIndex = 0;
        selectedTowerIndex = 0;
        Phase = OpenFieldPhase.Planning;

        GameObject viewObject = new GameObject("Open Field Board and Preview");
        viewObject.transform.SetParent(transform, false);
        presentation = viewObject.AddComponent<OpenFieldPresentation>();
        presentation.Initialize(grid, CellToWorld, cellSize, mapArtwork, showDebugGrid);
        presentation.ShowRoutes(grid, CellToWorld, cellSize);
        if (battleHud == null)
        {
            OpenFieldUiResources defaults = OpenFieldUiResources.LoadDefaults();
            if (defaults == null || defaults.BattleHudPrefab == null)
                throw new InvalidOperationException("Assign the battle HUD scene instance or default prefab.");
            battleHud = Instantiate(defaults.BattleHudPrefab, transform);
        }
        battleHud.Initialize(this);
        buildMenu = viewObject.AddComponent<OpenFieldBuildMenu>();
        buildMenu.Initialize(towerDefinitions, buildMenuFont, gameplayCamera,
            CanBuildChoice, TryBuildSelectedChoice, PreviewBuildChoice, ClearBuildSelection,
            index => selectedBuildCell.HasValue ? GetBuildRejection(towerDefinitions[index],
                selectedBuildCell.Value) : string.Empty, () => BattleHudScreenRect);
        towerMenu = viewObject.AddComponent<OpenFieldTowerMenu>();
        towerMenu.Initialize(this, buildMenuFont, gameplayCamera);
        enemyHud = viewObject.AddComponent<EnemyStatusHud>();
        enemyHud.Initialize(activeEnemies, gameplayCamera, buildMenuFont, cellDefenders.Values);
        entranceWaveButtons = viewObject.AddComponent<OpenFieldEntranceWaveButtons>();
        entranceWaveButtons.Initialize(this, grid, CellToWorld, buildMenuFont, gameplayCamera, waveButtonArtwork);
        playbackControls = viewObject.AddComponent<OpenFieldPlaybackControls>();
        playbackControls.Initialize(this, buildMenuFont);

        enemyPoolRoot = new GameObject("Enemy Pool").transform;
        enemyPoolRoot.SetParent(transform, false);
        PrewarmEnemies();

        if (victoryPanel != null && defeatPanel != null)
        {
            ConfigureResultPanel(victoryPanel, true);
            ConfigureResultPanel(defeatPanel, false);
            victoryScoreView = FitResultToScreen(victoryPanel);
            defeatScoreView = FitResultToScreen(defeatPanel);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
        }
        if (enableFirstLevelTutorial)
        {
            tutorial = viewObject.AddComponent<FirstLevelTutorial>();
            tutorial.Initialize(this, buildMenuFont, gameplayCamera, grid, CellToWorld,
                buildMenu, towerMenu, entranceWaveButtons);
        }
    }

    private void Update()
    {
        ConfigureMapViewport();
        // A resume click must not also select the board underneath in the same frame.
        if (Time.frameCount == playbackInputBlockedFrame) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space) && StartAssault()) return;
        if (TickAutoWave(Time.deltaTime)) return;

        if (!CanBuild)
        {
            boardTap.Reset();
            presentation.HidePreview();
            return;
        }

        // Do not also process Unity's simulated mouse click for the same touch.
        if (Input.touchCount > 0 || (MobileDisplay.IsActive && !Application.isEditor))
        {
            if (Input.touchCount == 0) boardTap.Reset();
            else
            {
                Touch touch = Input.GetTouch(0);
                ProcessBoardTouch(Input.touchCount, touch.fingerId, touch.phase, touch.position);
            }
            RefreshSelectedBuildPreview();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (towerMenu != null && towerMenu.IsConfirming) towerMenu.CancelSell();
            else ClearBuildSelection();
            return;
        }
        if (selectedBuildCell.HasValue)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryBuildSelectedChoice(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryBuildSelectedChoice(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryBuildSelectedChoice(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryBuildSelectedChoice(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TryBuildSelectedChoice(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TryBuildSelectedChoice(5);
        }

        if (battleHud.ContainsPointer(Input.mousePosition) || buildMenu.ContainsPointer(Input.mousePosition) ||
            (tutorial != null && tutorial.ContainsPointer(Input.mousePosition)) ||
            (playbackControls != null && playbackControls.ContainsPointer(Input.mousePosition)) ||
            (towerMenu != null && towerMenu.ContainsPointer(Input.mousePosition)) ||
            entranceWaveButtons.ContainsPointer(Input.mousePosition) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            RefreshSelectedBuildPreview();
            return;
        }

        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = transform.position.z - gameplayCamera.transform.position.z;
        Vector2Int cell = WorldToCell(gameplayCamera.ScreenToWorldPoint(screenPoint));
        if (!grid.Contains(cell))
        {
            if (Input.GetMouseButtonDown(0)) ClearBuildSelection();
            else RefreshSelectedBuildPreview();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SelectCell(cell);
        }
        else if (selectedBuildCell.HasValue || selectedTowerCell.HasValue)
        {
            RefreshSelectedBuildPreview();
        }
        else
        {
            if (!previewCell.HasValue || previewCell.Value != cell)
            {
                previewCell = cell;
                previewCellValid = grid.CheckPlacement(cell, false) == OpenFieldPlacementResult.Valid;
            }
            presentation.ShowCellHint(CellToWorld(cell), previewCellValid);
        }
    }

    public bool ProcessBoardTouch(int touchCount, int fingerId, TouchPhase phase, Vector2 position)
    {
        if (!CanBuild || Time.frameCount == playbackInputBlockedFrame) { boardTap.Reset(); return false; }
        float threshold = Mathf.Max(18f, Screen.dpi > 0f ? Screen.dpi * .08f : 18f);
        bool blocked = !MobileDisplay.SafeArea.Contains(position) || IsBoardPointerBlocked(position);
        if (!boardTap.Process(touchCount, fingerId, phase, position, blocked, threshold)) return false;
        Vector3 screen = new Vector3(position.x, position.y, transform.position.z - gameplayCamera.transform.position.z);
        Vector2Int cell = WorldToCell(gameplayCamera.ScreenToWorldPoint(screen));
        if (!grid.Contains(cell)) { ClearSelection(); return false; }
        return SelectCell(cell);
    }

    private bool IsBoardPointerBlocked(Vector2 position)
    {
        if (battleHud.ContainsPointer(position) || buildMenu.ContainsPointer(position) ||
            (tutorial != null && tutorial.ContainsPointer(position)) ||
            playbackControls.ContainsPointer(position) || towerMenu.ContainsPointer(position) ||
            entranceWaveButtons.ContainsPointer(position)) return true;
        if (EventSystem.current == null) return false;
        boardUiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, boardUiHits);
        foreach (RaycastResult hit in boardUiHits)
            if (hit.module is GraphicRaycaster) return true;
        return false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (!MobileDisplay.IsActive || !paused) return;
        boardTap.Reset();
        PlayerPrefs.Save();
        Pause();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!MobileDisplay.IsActive || focused) return;
        boardTap.Reset();
        Pause();
    }

    public bool SelectBuildCell(Vector2Int cell)
    {
        ClearBuildSelection();
        if (!CanBuild ||
            grid.CheckPlacement(cell, false) != OpenFieldPlacementResult.Valid) return false;
        selectedBuildCell = cell;
        buildMenu.Show(CellToWorld(cell));
        RefreshSelectedBuildPreview();
        SelectionChanged?.Invoke();
        return true;
    }

    public bool SelectCell(Vector2Int cell)
    {
        if (placedSlots.ContainsKey(cell)) return SelectPlacedTower(cell);
        if (selectedBuildCell == cell) { ClearBuildSelection(); return false; }
        return SelectBuildCell(cell);
    }

    public bool SelectPlacedTower(Vector2Int cell)
    {
        bool alreadySelected = selectedTowerCell == cell;
        ClearBuildSelection();
        if (!CanBuild || alreadySelected || !TryGetLivingTower(cell, out TowerController tower)) return false;
        selectedTowerCell = cell;
        if (towerMenu != null) towerMenu.Show(tower);
        RefreshSelectedBuildPreview();
        SelectionChanged?.Invoke();
        return true;
    }

    public void ClearSelection() => ClearBuildSelection();

    public void SuppressBoardInputThisFrame()
    {
        playbackInputBlockedFrame = Time.frameCount;
        boardTap.Reset();
    }

    // Pause is orthogonal to the round phase: keep the spawn coroutine, paths and contacts intact.
    public bool Pause()
    {
        if (!HasActiveRun || IsPaused) return false;
        IsPaused = true;
        boardTap.Reset();
        Time.timeScale = 0f;
        playbackInputBlockedFrame = Time.frameCount;
        ClearBuildSelection();
        RefreshPlaybackUi();
        return true;
    }

    public bool Resume()
    {
        if (!HasActiveRun || !IsPaused) return false;
        IsPaused = false;
        Time.timeScale = PlaybackSpeed;
        playbackInputBlockedFrame = Time.frameCount;
        RefreshPlanningRoutes();
        RefreshPlaybackUi();
        return true;
    }

    public bool SetPlaybackSpeed(int speed)
    {
        if (!HasActiveRun || IsPaused || (speed != 1 && speed != 2)) return false;
        PlaybackSpeed = speed;
        Time.timeScale = speed;
        RefreshPlaybackUi();
        return true;
    }

    public bool TogglePlaybackSpeed() => SetPlaybackSpeed(PlaybackSpeed == 1 ? 2 : 1);

    public void HandleEscape()
    {
        if (playbackControls != null && playbackControls.IsSettingsVisible)
        {
            playbackControls.CloseSettings();
            return;
        }
        if (IsPaused) { Resume(); return; }
        if (!HasActiveRun) return;
        if (towerMenu != null && towerMenu.IsConfirming) towerMenu.CancelSell();
        else if (selectedBuildCell.HasValue || selectedTowerCell.HasValue) ClearBuildSelection();
        else Pause();
    }

    private void ResetPlayback()
    {
        IsPaused = false;
        PlaybackSpeed = 1;
        Time.timeScale = 1f;
    }

    private void RefreshPlaybackUi()
    {
        if (playbackControls != null) playbackControls.Refresh();
        if (entranceWaveButtons != null) entranceWaveButtons.Refresh();
    }

    private bool TryGetLivingTower(Vector2Int cell, out TowerController tower)
    {
        tower = null;
        if (!placedSlots.TryGetValue(cell, out BuildSlot slot) || slot == null || slot.CurrentTower == null ||
            !cellDefenders.TryGetValue(cell, out TowerCombatState defender) || defender == null ||
            !defender.IsAlive || defender.Tower != slot.CurrentTower) return false;
        tower = slot.CurrentTower;
        return true;
    }

    public int GetSellRefund(Vector2Int cell)
    {
        return TryGetLivingTower(cell, out TowerController tower)
            ? TowerEconomyRules.GetSellRefund(tower, sellRefundPercent) : 0;
    }

    public int GetUpgradeCost(Vector2Int cell)
    {
        return TryGetLivingTower(cell, out TowerController tower) && !tower.IsUpgraded && tower.SupportsDamageUpgrade
            ? Mathf.Max(0, tower.BuildCostPaid) : 0;
    }

    public bool CanUpgradeAt(Vector2Int cell)
    {
        int cost = GetUpgradeCost(cell);
        return CanBuild && cost > 0 && economy.CurrentATP >= cost;
    }

    public bool TryUpgradeAt(Vector2Int cell)
    {
        if (!CanUpgradeAt(cell) || !TryGetLivingTower(cell, out TowerController tower)) return false;
        int cost = tower.BuildCostPaid;
        if (!economy.TrySpend(cost)) return false;
        if (!tower.TryUpgrade(cost))
        {
            economy.AddATP(cost);
            return false;
        }
        // Do not reinitialize: health, contacts, antigen memory, cooldowns and routes stay intact.
        if (towerMenu != null) towerMenu.Refresh();
        RefreshSelectedBuildPreview();
        TowerUpgraded?.Invoke(tower);
        return true;
    }

    public bool TrySellAt(Vector2Int cell)
    {
        if (!CanBuild || !TryGetLivingTower(cell, out TowerController tower)) return false;
        int refund = TowerEconomyRules.GetSellRefund(tower, sellRefundPercent);
        RemovePlacedTower(cellDefenders[cell]);
        if (refund > 0) economy.AddATP(refund);
        TowerSold?.Invoke(tower);
        return true;
    }

    private bool CanBuildChoice(int index)
    {
        if (!CanBuild || !selectedBuildCell.HasValue ||
            index < 0 || index >= towerDefinitions.Length) return false;
        return string.IsNullOrEmpty(GetBuildRejection(towerDefinitions[index], selectedBuildCell.Value));
    }

    private string GetBuildRejection(TowerDefinition definition, Vector2Int cell)
    {
        if (definition == null || definition.Prefab == null || definition.BuildCost <= 0) return "未配置";
        // Defenders occupy build cells, but never alter the terrain's routes.
        OpenFieldPlacementResult placement = grid.CheckPlacement(cell, false);
        if (placement != OpenFieldPlacementResult.Valid) return "该格不能建造";
        return economy.CurrentATP < definition.BuildCost ? "ATP 不足" : string.Empty;
    }

    private TowerCombatState FindDefender(Vector3 position)
    {
        cellDefenders.TryGetValue(WorldToCell(position), out TowerCombatState defender);
        return defender;
    }

    // Kept for future routing towers, deliberately not called by GetBuildRejection.
    private OpenFieldPlacementResult CheckLegacyRoutePlacement(Vector2Int cell)
    {
        if (Phase == OpenFieldPhase.Assault)
        {
            activeRouteStarts.Clear();
            foreach (EnemyController enemy in activeEnemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                PathFollower follower = enemy.GetComponent<PathFollower>();
                Vector2Int next = WorldToCell(follower.CurrentPath.GetPoint(follower.NextPointIndex).position);
                if (cell == next || cell == WorldToCell(enemy.transform.position))
                    return OpenFieldPlacementResult.OccupiedCell;
                activeRouteStarts.Add(next);
            }
            bool sameStarts = activeRouteStarts.Count == cachedActiveRouteStarts.Count;
            for (int i = 0; sameStarts && i < activeRouteStarts.Count; i++)
                sameStarts = activeRouteStarts[i] == cachedActiveRouteStarts[i];
            if (!sameStarts || cachedBarrierCell != cell || cachedBarrierRevision != grid.Revision)
            {
                cachedBarrierCell = cell;
                cachedBarrierRevision = grid.Revision;
                cachedActiveRouteStarts.Clear();
                cachedActiveRouteStarts.AddRange(activeRouteStarts);
                cachedBarrierPlacement = grid.CheckPlacementKeepingRoutes(cell, true, activeRouteStarts);
            }
        }
        else if (cachedBarrierCell != cell || cachedBarrierRevision != grid.Revision || cachedActiveRouteStarts.Count > 0)
        {
            cachedBarrierCell = cell;
            cachedBarrierRevision = grid.Revision;
            cachedActiveRouteStarts.Clear();
            cachedBarrierPlacement = grid.CheckPlacement(cell);
        }
        return cachedBarrierPlacement;
    }

    private void HandleDefenderDied(TowerCombatState defender)
    {
        TowerController lost = defender.Tower;
        RemovePlacedTower(defender);
        TowerLost?.Invoke(lost);
    }

    private void RemovePlacedTower(TowerCombatState defender)
    {
        defender.Died -= HandleDefenderDied;
        Vector2Int cell = defender.Cell;
        if (!cellDefenders.TryGetValue(cell, out TowerCombatState registered) || registered != defender) return;
        defender.gameObject.SetActive(false);
        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy == null) continue;
            EnemyContactCombat contact = enemy.GetComponent<EnemyContactCombat>();
            if (contact != null) contact.ReleaseDefender(defender);
        }
        if (selectedTowerCell == cell) ClearBuildSelection();
        cellDefenders.Remove(cell);
        if (placedSlots.TryGetValue(cell, out BuildSlot slot))
        {
            slot.Release();
            slot.gameObject.SetActive(false);
            placedSlots.Remove(cell);
            DestroyPlacedObject(slot.gameObject);
        }
        grid.Release(cell);
        previewCell = null;
        RebuildSignalLinks();
        RefreshPlanningRoutes();
        DestroyPlacedObject(defender.gameObject);
    }

    private static void DestroyPlacedObject(GameObject obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private void TryBuildSelectedChoice(int index)
    {
        if (!CanBuildChoice(index)) return;
        SelectTower(index);
        TryBuildAt(selectedBuildCell.Value);
    }

    private void PreviewBuildChoice(int index)
    {
        hoveredBuildIndex = index;
        RefreshSelectedBuildPreview();
        RefreshPlanningRoutes();
    }

    private void RefreshPlanningRoutes()
    {
        if (presentation == null || grid == null) return;
        if (!isActiveAndEnabled || !CanBuild ||
            (Phase == OpenFieldPhase.Assault && !selectedBuildCell.HasValue))
        {
            presentation.HideRoutes();
            return;
        }
        presentation.ShowRoutes(grid, CellToWorld, cellSize);
    }

    private void RefreshSelectedBuildPreview()
    {
        if (presentation == null) return;
        TowerController selectedTower = SelectedTower;
        if (selectedTower != null && CanBuild)
        {
            presentation.ShowBuiltTower(selectedTower);
            return;
        }
        if (!selectedBuildCell.HasValue || !CanBuild)
        {
            presentation.HidePreview();
            return;
        }
        Vector3 position = CellToWorld(selectedBuildCell.Value);
        if (hoveredBuildIndex >= 0 && hoveredBuildIndex < towerDefinitions.Length)
            presentation.ShowPreview(towerDefinitions[hoveredBuildIndex], position,
                CanBuildChoice(hoveredBuildIndex));
        else presentation.ShowCellHint(position, true);
    }

    private void ClearBuildSelection()
    {
        selectedBuildCell = null;
        selectedTowerCell = null;
        hoveredBuildIndex = -1;
        previewCell = null;
        if (buildMenu != null) buildMenu.Hide();
        if (towerMenu != null) towerMenu.Hide();
        if (presentation != null) presentation.HidePreview();
        RefreshPlanningRoutes();
        SelectionChanged?.Invoke();
    }

    private void OnDisable()
    {
        boardTap.Reset();
        ResetPlayback();
        if (playbackControls != null) playbackControls.Hide();
        ClearBuildSelection();
        if (battleHud != null) battleHud.Hide();
        if (entranceWaveButtons != null) entranceWaveButtons.Hide();
    }

    public void SelectTower(int index)
    {
        if (!CanBuild || index < 0 || index >= towerDefinitions.Length ||
            towerDefinitions[index] == null)
        {
            return;
        }
        selectedTowerIndex = index;
    }

    public bool TryBuildAt(Vector2Int cell)
    {
        TowerDefinition definition = GetSelectedTower();
        if (!CanBuild || definition == null || definition.Prefab == null ||
            !string.IsNullOrEmpty(GetBuildRejection(definition, cell)))
        {
            return false;
        }

        int buildCost = TowerEconomyRules.GetBuildCost(definition);
        if (!economy.TrySpend(buildCost)) return false;
        GameObject slotObject = null;
        TowerController tower = null;
        bool occupied = false;
        try
        {
            Vector3 position = CellToWorld(cell);
            slotObject = new GameObject($"Build Cell {cell.x},{cell.y}");
            slotObject.transform.SetParent(transform);
            slotObject.transform.position = position;
            BuildSlot slot = slotObject.AddComponent<BuildSlot>();

            tower = Instantiate(definition.Prefab, position, Quaternion.identity);
            tower.Initialize(definition, projectilePool, slot);
            slot.Occupy(tower);
            if (!grid.TryOccupy(cell, false))
            {
                throw new InvalidOperationException("The build cell became invalid during placement.");
            }
            occupied = true;
            placedSlots.Add(cell, slot);
            TowerCombatState defender = tower.GetComponent<TowerCombatState>();
            if (defender == null) defender = tower.gameObject.AddComponent<TowerCombatState>();
            defender.Initialize(tower, cell);
            defender.Died += HandleDefenderDied;
            cellDefenders.Add(cell, defender);
            RebuildSignalLinks();
            ClearBuildSelection();
        }
        catch (Exception exception)
        {
            if (occupied)
            {
                grid.Release(cell);
                placedSlots.Remove(cell);
                cellDefenders.Remove(cell);
            }
            if (tower != null) Destroy(tower.gameObject);
            if (slotObject != null) Destroy(slotObject);
            economy.AddATP(buildCost);
            Debug.LogException(exception, this);
            return false;
        }
        // Publish only after the transaction commits; observers cannot cause a build rollback.
        TowerBuilt?.Invoke(tower);
        return true;
    }

    public bool StartAssault()
    {
        if (!TryPrepareAssault()) return false;
        spawnRoutine = StartCoroutine(SpawnRound(waveSequence.GetWave(roundIndex)));
        if (entranceWaveButtons != null) entranceWaveButtons.Refresh();
        RoundStateChanged?.Invoke();
        return true;
    }

    private bool TickAutoWave(float deltaTime)
    {
        if (!isActiveAndEnabled || IsPaused || !HasAutoWaveCountdown || !CanStartWave) return false;
        autoWaveRemaining = Mathf.Max(0f, autoWaveRemaining - Mathf.Max(0f, deltaTime));
        return autoWaveRemaining <= 0f && StartAssault();
    }

    private bool TryPrepareAssault()
    {
        if (!CanStartWave) return false;
        autoWaveRemaining = -1f;
        if (Phase == OpenFieldPhase.Assault)
        {
            // Survivors and their summons still reference these paths; reuse them until all clear.
            AdvanceRound();
        }
        else CreateRoundPaths();
        ClearBuildSelection();
        allEnemiesSpawned = false;
        Phase = OpenFieldPhase.Assault;
        if (presentation != null)
        {
            presentation.HideRoutes();
            presentation.HidePreview();
        }
        return true;
    }

    private void AdvanceRound()
    {
        roundIndex++;
        if (betweenRoundATP > 0) economy.AddATP(betweenRoundATP);
    }

    public void RestartPrototype()
    {
        ResetPlayback();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        ResetPlayback();
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadNextLevel()
    {
        if (Phase != OpenFieldPhase.Victory || string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName) || !progress.IsUnlocked(nextSceneName))
        {
            return;
        }

        ResetPlayback();
        SceneManager.LoadScene(nextSceneName);
    }

    private TowerDefinition GetSelectedTower()
    {
        return selectedTowerIndex >= 0 && selectedTowerIndex < towerDefinitions.Length
            ? towerDefinitions[selectedTowerIndex]
            : null;
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return transform.position + new Vector3(
            (cell.x - (width - 1) * 0.5f) * cellSize,
            (cell.y - (height - 1) * 0.5f) * cellSize,
            0f);
    }

    private void ConfigureMapViewport()
    {
        Rect viewport = new Rect(0f, 0f, 1f, 1f);
        if (MobileDisplay.IsActive)
        {
            // Reserve the mobile HUD row inside the device's safe area.
            Rect safe = MobileDisplay.SafeArea;
            viewport = new Rect(safe.x / Screen.width,
                (safe.y + safe.height * (12f / 720f)) / Screen.height,
                safe.width / Screen.width, safe.height * (1f - 148f / 720f) / Screen.height);
        }
        if (gameplayCamera.rect != viewport) gameplayCamera.rect = viewport;
        ConfigureViewportBackground(viewport);

        // Projection must match the actual pixel rectangle, not the board's 15:11 ratio.
        // Fit the complete board while keeping cells, sprites and range circles undistorted.
        Rect pixels = gameplayCamera.pixelRect;
        float aspect = Mathf.Max(1f, pixels.width) / Mathf.Max(1f, pixels.height);
        float halfHeight = Mathf.Max(height * cellSize * .5f, width * cellSize / (2f * aspect));
        if (!gameplayCamera.orthographic || !Mathf.Approximately(gameplayCamera.aspect, aspect) ||
            !Mathf.Approximately(gameplayCamera.orthographicSize, halfHeight))
        {
            gameplayCamera.orthographic = true;
            gameplayCamera.aspect = aspect;
            gameplayCamera.orthographicSize = halfHeight;
            gameplayCamera.ResetProjectionMatrix();
        }
        Vector3 position = gameplayCamera.transform.position;
        position.x = transform.position.x;
        position.y = transform.position.y;
        if (gameplayCamera.transform.position != position) gameplayCamera.transform.position = position;
    }

    private void ConfigureViewportBackground(Rect viewport)
    {
        bool needsBackground = viewport != new Rect(0f, 0f, 1f, 1f);
        if (viewportBackgroundCamera == null && needsBackground)
        {
            // A partial camera clears only its viewport. Clear the whole display first,
            // otherwise mobile back buffers can retain pixels from menus or result UI.
            GameObject background = new GameObject("Viewport Background Camera", typeof(Camera));
            background.transform.SetParent(gameplayCamera.transform, false);
            viewportBackgroundCamera = background.GetComponent<Camera>();
            viewportBackgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            viewportBackgroundCamera.cullingMask = 0;
            viewportBackgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
            viewportBackgroundCamera.orthographic = true;
            viewportBackgroundCamera.allowHDR = false;
            viewportBackgroundCamera.allowMSAA = false;
            viewportBackgroundCamera.useOcclusionCulling = false;
        }
        if (viewportBackgroundCamera == null) return;
        viewportBackgroundCamera.backgroundColor = gameplayCamera.backgroundColor;
        viewportBackgroundCamera.depth = gameplayCamera.depth - 1f;
        viewportBackgroundCamera.targetDisplay = gameplayCamera.targetDisplay;
        viewportBackgroundCamera.targetTexture = gameplayCamera.targetTexture;
        viewportBackgroundCamera.enabled = needsBackground && gameplayCamera.enabled;
    }

    private Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 relative = world - transform.position;
        return new Vector2Int(
            Mathf.RoundToInt(relative.x / cellSize + (width - 1) * 0.5f),
            Mathf.RoundToInt(relative.y / cellSize + (height - 1) * 0.5f));
    }

    private void RebuildSignalLinks()
    {
        float radiusSquared = signalRadiusCells * signalRadiusCells;
        foreach (KeyValuePair<Vector2Int, BuildSlot> source in placedSlots)
        {
            List<BuildSlot> neighbors = new List<BuildSlot>();
            foreach (KeyValuePair<Vector2Int, BuildSlot> candidate in placedSlots)
            {
                if (candidate.Key != source.Key &&
                    (candidate.Key - source.Key).sqrMagnitude <= radiusSquared)
                {
                    neighbors.Add(candidate.Value);
                }
            }
            source.Value.ConfigureSignalNeighbors(neighbors.ToArray());
        }
    }

    private void CreateRoundPaths()
    {
        DestroyRoundPaths();
        for (int spawnIndex = 0; spawnIndex < grid.SpawnCount; spawnIndex++)
            roundPaths.Add(CreateRoute(grid.GetSpawnCell(spawnIndex)));
    }

    private WaypointPath CreateRoute(Vector2Int start, bool keepNextNode = false)
    {
        if (!grid.TryGetPath(start, pathCells) || pathCells.Count == 0)
            throw new InvalidOperationException($"No route from {start} to the core.");
        GameObject obj = new GameObject($"Round {CurrentRound} Route {start.x},{start.y}");
        obj.transform.SetParent(transform);
        WaypointPath route = obj.AddComponent<WaypointPath>();
        int prefix = keepNextNode ? 1 : 0;
        Transform[] points = new Transform[pathCells.Count + prefix];
        for (int i = 0; i < points.Length; i++)
        {
            GameObject point = new GameObject($"Point {i}");
            point.transform.SetParent(obj.transform);
            point.transform.position = CellToWorld(pathCells[Mathf.Max(0, i - prefix)]);
            points[i] = point.transform;
        }
        route.Configure(points);
        return route;
    }

    // Preserved for a future routing tower; no current build, sale or death calls it.
    private void RebuildAssaultRoutes()
    {
        List<WaypointPath> newSpawns = new List<WaypointPath>();
        Dictionary<Vector2Int, WaypointPath> fromCells = new Dictionary<Vector2Int, WaypointPath>();
        Dictionary<PathFollower, WaypointPath> replacements = new Dictionary<PathFollower, WaypointPath>();
        try
        {
            for (int i = 0; i < grid.SpawnCount; i++) newSpawns.Add(CreateRoute(grid.GetSpawnCell(i)));
            foreach (EnemyController enemy in activeEnemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                PathFollower follower = enemy.GetComponent<PathFollower>();
                Vector2Int next = WorldToCell(follower.CurrentPath.GetPoint(follower.NextPointIndex).position);
                if (!fromCells.TryGetValue(next, out WaypointPath route))
                {
                    route = CreateRoute(next, true);
                    fromCells.Add(next, route);
                }
                replacements.Add(follower, route);
            }
        }
        catch
        {
            foreach (WaypointPath route in newSpawns) DestroyRoute(route);
            foreach (WaypointPath route in fromCells.Values) DestroyRoute(route);
            throw;
        }
        foreach (KeyValuePair<PathFollower, WaypointPath> pair in replacements) pair.Key.Reroute(pair.Value);
        DestroyRoundPaths();
        roundPaths.AddRange(newSpawns);
        reroutedPaths.AddRange(fromCells.Values);
    }

    private static void DestroyRoute(WaypointPath route)
    {
        if (route == null) return;
        if (Application.isPlaying) Destroy(route.gameObject);
        else DestroyImmediate(route.gameObject);
    }

    private IEnumerator SpawnRound(WaveDefinition wave)
    {
        wave.Validate();
        int spawnIndex = 0;
        WaitForSeconds withinBurstDelay = new WaitForSeconds(wave.SpawnInterval);
        WaitForSeconds betweenBurstDelay = new WaitForSeconds(wave.BurstInterval);
        for (int groupIndex = 0; groupIndex < wave.GroupCount && Phase == OpenFieldPhase.Assault; groupIndex++)
        {
            EnemyDefinition definition = wave.GetGroupEnemy(groupIndex);
            int count = wave.GetGroupCount(groupIndex);
            int burstSize = wave.GetGroupBurstSize(groupIndex);
            if (definition.IsBoss)
            {
                enemyHud.Announce(definition);
                yield return new WaitForSeconds(1f);
                if (Phase != OpenFieldPhase.Assault) yield break;
            }
            for (int i = 0; i < count && Phase == OpenFieldPhase.Assault; i++)
            {
                int entrance = wave.GetGroupEntrance(groupIndex);
                int pathIndex = entrance < 0 ? spawnIndex % roundPaths.Count : entrance;
                spawnIndex++;
                if (SpawnEnemy(definition, roundPaths[pathIndex], null) == null)
                {
                    FinishRun(OpenFieldPhase.Defeat);
                    yield break;
                }
                if (i < count - 1)
                    yield return burstSize > 1 && (i + 1) % burstSize == 0
                        ? betweenBurstDelay : withinBurstDelay;
            }
            if (groupIndex < wave.GroupCount - 1 && Phase == OpenFieldPhase.Assault)
                yield return new WaitForSeconds(wave.GetGroupAfterDelay(groupIndex));
        }

        if (Phase != OpenFieldPhase.Assault) yield break;
        spawnRoutine = null;
        allEnemiesSpawned = true;
        autoWaveRemaining = roundIndex < TotalRounds - 1
            ? (float.IsNaN(autoWaveDelay) || float.IsInfinity(autoWaveDelay) ? 10f : Mathf.Max(.1f, autoWaveDelay))
            : -1f;
        FinishRoundIfReady();
        if (entranceWaveButtons != null) entranceWaveButtons.Refresh();
    }

    private EnemyController SpawnEnemy(EnemyDefinition definition, WaypointPath path, EnemySpawnRequest? request)
    {
        EnemyController enemy = null;
        try
        {
            using (SpawnEnemyMarker.Auto())
            {
                enemy = GetEnemy(definition.EnemyPrefab);
                enemy.Exited += HandleEnemyExited;
                enemy.SummonRequested += HandleSummonRequested;
                activeEnemies.Add(enemy);
                if (!request.HasValue) scheduledEnemies.Add(enemy);
                enemy.Initialize(path, definition, enemySpeedMultiplier, request);
                EnemyContactCombat contact = enemy.GetComponent<EnemyContactCombat>();
                if (contact == null) contact = enemy.gameObject.AddComponent<EnemyContactCombat>();
                contact.Initialize(enemy, cellSize, defenderLookup, assaultCheck);
                enemyHud.Announce(definition);
            }
            return enemy;
        }
        catch (Exception exception)
        {
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
                scheduledEnemies.Remove(enemy);
                enemy.Exited -= HandleEnemyExited;
                enemy.SummonRequested -= HandleSummonRequested;
                ReturnEnemy(enemy, definition.EnemyPrefab);
            }
            Debug.LogException(exception, this);
            return null;
        }
    }

    private void HandleSummonRequested(EnemySpawnRequest request)
    {
        for (int i = 0; i < request.Count; i++)
        {
            if (Phase != OpenFieldPhase.Assault || request.Parent == null ||
                request.Parent.SpawnVersion != request.ParentVersion || !request.Parent.isActiveAndEnabled ||
                !request.Parent.CanBeTargeted || !activeEnemies.Contains(request.Parent) ||
                request.Path == null || (!roundPaths.Contains(request.Path) && !reroutedPaths.Contains(request.Path))) return;
            if (SpawnEnemy(request.Definition, request.Path, request) == null)
            {
                FinishRun(OpenFieldPhase.Defeat);
                return;
            }
        }
    }

    private void HandleEnemyExited(EnemyController enemy, EnemyExitReason reason)
    {
        if (!activeEnemies.Remove(enemy))
        {
            return;
        }
        enemy.Exited -= HandleEnemyExited;
        enemy.SummonRequested -= HandleSummonRequested;
        if (scheduledEnemies.Remove(enemy) && reason == EnemyExitReason.Killed)
            killedScheduledEnemies++;
        if (reason == EnemyExitReason.Killed && enemy.Definition.KillReward > 0)
        {
            economy.AddATP(enemy.Definition.KillReward);
        }
        else if (reason == EnemyExitReason.Leaked)
        {
            life.TryLoseLife(enemy.Definition.LeakDamage);
        }
        ReturnEnemy(enemy, enemy.Definition.EnemyPrefab);

        if (life.IsDepleted)
        {
            FinishRun(OpenFieldPhase.Defeat);
            return;
        }
        FinishRoundIfReady();
    }

    private void FinishRoundIfReady()
    {
        if (Phase != OpenFieldPhase.Assault || !allEnemiesSpawned || activeEnemies.Count > 0)
        {
            return;
        }

        DestroyRoundPaths();
        if (roundIndex == waveSequence.WaveCount - 1)
        {
            FinishRun(OpenFieldPhase.Victory);
            return;
        }

        AdvanceRound();
        Phase = OpenFieldPhase.Planning;
        enemyHud.ClearHints();
        RefreshPlanningRoutes();
        if (entranceWaveButtons != null) entranceWaveButtons.Refresh();
        RoundStateChanged?.Invoke();
    }

    private void FinishRun(OpenFieldPhase result)
    {
        if (!HasActiveRun || (result != OpenFieldPhase.Victory && result != OpenFieldPhase.Defeat)) return;
        if (life.IsDepleted) result = OpenFieldPhase.Defeat;
        Phase = result;
        autoWaveRemaining = -1f;
        FinalScore = LevelScoring.Evaluate(result == OpenFieldPhase.Victory,
            killedScheduledEnemies, scheduledEnemyCount, life.CurrentLife, initialCoreLife);
        string scene = gameObject.scene.name;
        if (result == OpenFieldPhase.Victory) progress.SaveVictory(scene, FinalScore.Score);
        LevelResultScoreView scoreView = result == OpenFieldPhase.Victory ? victoryScoreView : defeatScoreView;
        if (scoreView != null) scoreView.Show(FinalScore);
        ResetPlayback();
        if (playbackControls != null) playbackControls.Hide();
        if (battleHud != null) battleHud.Hide();
        if (entranceWaveButtons != null) entranceWaveButtons.Hide();
        ClearBuildSelection();
        // OnDisable hides every status row and stops damaged defenders from restoring it.
        enemyHud.enabled = false;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy == null) continue;
            enemy.Exited -= HandleEnemyExited;
            enemy.SummonRequested -= HandleSummonRequested;
            ReturnEnemy(enemy, enemy.Definition.EnemyPrefab);
        }
        activeEnemies.Clear();
        scheduledEnemies.Clear();
        DestroyRoundPaths();
        presentation.HidePreview();
        if (victoryPanel != null && defeatPanel != null)
        {
            victoryPanel.SetActive(result == OpenFieldPhase.Victory);
            defeatPanel.SetActive(result == OpenFieldPhase.Defeat);
        }
        RoundStateChanged?.Invoke();
    }

    private void ConfigureResultPanel(GameObject panel, bool isVictoryPanel)
    {
        Button again = panel.transform.Find("Canvas/Again")?.GetComponent<Button>();
        Button returnToMenu = panel.transform.Find("Canvas/Return")?.GetComponent<Button>();
        if (again == null || returnToMenu == null)
        {
            throw new InvalidOperationException($"{panel.name} must contain Canvas/Again and Canvas/Return buttons.");
        }

        again.onClick.AddListener(RestartPrototype);
        returnToMenu.onClick.AddListener(ReturnToMainMenu);

        Button next = panel.transform.Find("Canvas/Next")?.GetComponent<Button>();
        bool canAdvance = isVictoryPanel && !string.IsNullOrWhiteSpace(nextSceneName) &&
                          Application.CanStreamedLevelBeLoaded(nextSceneName);
        if (next != null)
        {
            next.gameObject.SetActive(canAdvance);
            if (canAdvance)
            {
                next.onClick.AddListener(LoadNextLevel);
            }
            else
            {
                RectTransform againRect = again.GetComponent<RectTransform>();
                RectTransform returnRect = returnToMenu.GetComponent<RectTransform>();
                againRect.anchoredPosition = new Vector2(0f, -80f);
                returnRect.anchoredPosition = new Vector2(0f, -240f);
            }
        }
    }

    private LevelResultScoreView FitResultToScreen(GameObject panel)
    {
        Canvas resultCanvas = panel.transform.Find("Canvas").GetComponent<Canvas>();
        // The original prefabs use 10x artwork and 6x buttons. Normalize both
        // desktop and mobile, then fit the entire composition when the viewport changes.
        // A 600px panel inside a 720px design height also leaves space above and below.
        Transform content = MobileSafeFrame.Wrap(resultCanvas, new Vector2(800f, 720f));
        List<Button> activeActions = new List<Button>();
        foreach (Button button in content.GetComponentsInChildren<Button>())
            if (button.gameObject.activeSelf) activeActions.Add(button);
        Button[] actions = activeActions.ToArray();
        for (int i = 0; i < actions.Length; i++)
        {
            RectTransform rect = actions[i].GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(260f, 52f);
            rect.anchoredPosition = new Vector2(0f, panel == defeatPanel ? -116f - i * 70f : -82f - i * 62f);
            LevelResultScoreView.StyleActionButton(actions[i]);
        }
        foreach (Image image in content.GetComponentsInChildren<Image>())
        {
            if (image.GetComponent<Button>() != null) continue;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.sizeDelta = new Vector2(600f, 600f);
            image.preserveAspect = true;
        }
        // The failure illustration extends lower than the victory title.
        return LevelResultScoreView.Create(content, panel == defeatPanel ? -28f : 0f);
    }

    private void DestroyRoundPaths()
    {
        foreach (WaypointPath path in roundPaths)
        {
            DestroyRoute(path);
        }
        roundPaths.Clear();
        foreach (WaypointPath path in reroutedPaths) DestroyRoute(path);
        reroutedPaths.Clear();
    }

    private void PrewarmEnemies()
    {
        WaveDefinition firstWave = waveSequence.GetWave(0);
        int budget = EnemyPoolPrewarmCount;
        for (int group = 0; group < firstWave.GroupCount && budget > 0; group++)
        {
            EnemyController prefab = firstWave.GetGroupEnemy(group).EnemyPrefab;
            Stack<EnemyController> inactive = GetInactiveStack(prefab);
            int count = Mathf.Min(budget, firstWave.GetGroupCount(group));
            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = Instantiate(prefab, enemyPoolRoot);
                enemy.gameObject.SetActive(false);
                inactive.Push(enemy);
            }
            budget -= count;
        }
    }

    private EnemyController GetEnemy(EnemyController prefab)
    {
        Stack<EnemyController> inactive = GetInactiveStack(prefab);
        EnemyController enemy = inactive.Count > 0
            ? inactive.Pop()
            : Instantiate(prefab, enemyPoolRoot);
        enemy.gameObject.SetActive(true);
        return enemy;
    }

    private void ReturnEnemy(EnemyController enemy, EnemyController prefab)
    {
        enemy.gameObject.SetActive(false);
        Stack<EnemyController> inactive = GetInactiveStack(prefab);
        if (inactive.Count < EnemyPoolMaxInactivePerPrefab)
        {
            inactive.Push(enemy);
        }
        else
        {
            Destroy(enemy.gameObject);
        }
    }

    private Stack<EnemyController> GetInactiveStack(EnemyController prefab)
    {
        if (!inactiveEnemiesByPrefab.TryGetValue(prefab, out Stack<EnemyController> inactive))
        {
            inactive = new Stack<EnemyController>();
            inactiveEnemiesByPrefab.Add(prefab, inactive);
        }
        return inactive;
    }

}
