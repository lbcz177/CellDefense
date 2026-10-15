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
    [SerializeField, Min(1)] private int initialCoreLife = 10;
    [SerializeField, Min(0.1f)] private float enemySpeedMultiplier = 0.85f;

    [Header("Combat")]
    [SerializeField] private TowerDefinition[] towerDefinitions;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField, Min(0.1f)] private float signalRadiusCells = 3f;
    [SerializeField, Min(0)] private int barrierBuildLimit = 6;
    [SerializeField] private Camera gameplayCamera;

    [Header("Build UI")]
    [SerializeField] private TMP_FontAsset buildMenuFont;

    [Header("Result UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private string nextSceneName;

    private readonly Dictionary<Vector2Int, BuildSlot> placedSlots = new Dictionary<Vector2Int, BuildSlot>();
    private readonly HashSet<EnemyController> activeEnemies = new HashSet<EnemyController>();
    private readonly Dictionary<Vector2Int, TowerCombatState> cellDefenders =
        new Dictionary<Vector2Int, TowerCombatState>();
    private readonly Dictionary<EnemyController, Stack<EnemyController>> inactiveEnemiesByPrefab =
        new Dictionary<EnemyController, Stack<EnemyController>>();
    private readonly List<WaypointPath> roundPaths = new List<WaypointPath>();
    private readonly List<Vector2Int> pathCells = new List<Vector2Int>();
    private readonly EconomyService economy = new EconomyService();
    private readonly LifeService life = new LifeService();

    private OpenFieldGridModel grid;
    private OpenFieldPresentation presentation;
    private OpenFieldBuildMenu buildMenu;
    private EnemyStatusHud enemyHud;
    private Vector2Int? selectedBuildCell;
    private int hoveredBuildIndex = -1;
    private Vector2Int? previewCell;
    private bool previewCellValid;
    private Transform enemyPoolRoot;
    private Coroutine spawnRoutine;
    private int roundIndex;
    private int selectedTowerIndex;
    private bool allEnemiesSpawned;
    private int barriersBuilt;
    private Func<Vector3, TowerCombatState> defenderLookup;
    private Func<bool> assaultCheck;
    private Vector2Int? cachedBarrierCell;
    private int cachedBarrierRevision = -1;
    private OpenFieldPlacementResult cachedBarrierPlacement;

    public OpenFieldPhase Phase { get; private set; }
    public int CurrentRound => roundIndex + 1;
    public int CurrentATP => economy.CurrentATP;
    public int CurrentCoreLife => life.CurrentLife;
    public int RemainingBarrierBuilds => Mathf.Max(0, barrierBuildLimit - barriersBuilt);
    public string DefenseStatusText => $"壁垒剩余建造额度 {RemainingBarrierBuilds}/{barrierBuildLimit}";

    private void Awake()
    {
        if (waveSequence == null || waveSequence.WaveCount != 3)
        {
            throw new InvalidOperationException("Open field prototype requires a three-wave sequence.");
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
        if (cellSize <= 0f || signalRadiusCells <= 0f)
        {
            throw new InvalidOperationException("Cell size and signal radius must be positive.");
        }
        if (barrierBuildLimit < 0) throw new InvalidOperationException("Barrier quota cannot be negative.");
        HashSet<TowerDefinition> barrierTypes = new HashSet<TowerDefinition>();
        foreach (TowerDefinition definition in towerDefinitions)
            if (definition != null && definition.BlocksEnemyRoute) barrierTypes.Add(definition);
        if (barrierTypes.Count != 1)
            throw new InvalidOperationException("Open-field levels require exactly one barrier tower type.");
        defenderLookup = FindDefender;
        assaultCheck = () => isActiveAndEnabled && Phase == OpenFieldPhase.Assault;

        gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (gameplayCamera == null)
        {
            throw new InvalidOperationException("Assign a gameplay camera.");
        }

        grid = new OpenFieldGridModel(width, height, coreCell, spawnCells, blockedCells);
        for (int i = 0; i < waveSequence.WaveCount; i++) waveSequence.GetWave(i).Validate();
        projectilePool.Initialize();
        economy.Initialize(initialATP);
        life.Initialize(initialCoreLife);
        roundIndex = 0;
        selectedTowerIndex = 0;
        Phase = OpenFieldPhase.Planning;

        GameObject viewObject = new GameObject("Open Field Board and Preview");
        viewObject.transform.SetParent(transform, false);
        presentation = viewObject.AddComponent<OpenFieldPresentation>();
        presentation.Initialize(grid, CellToWorld, cellSize, mapArtwork, showDebugGrid);
        presentation.ShowRoutes(grid, CellToWorld, cellSize);
        buildMenu = viewObject.AddComponent<OpenFieldBuildMenu>();
        buildMenu.Initialize(towerDefinitions, buildMenuFont, gameplayCamera,
            CanBuildChoice, TryBuildSelectedChoice, PreviewBuildChoice, ClearBuildSelection,
            index => selectedBuildCell.HasValue ? GetBuildRejection(towerDefinitions[index],
                selectedBuildCell.Value) : string.Empty);
        enemyHud = viewObject.AddComponent<EnemyStatusHud>();
        enemyHud.Initialize(activeEnemies, gameplayCamera, buildMenuFont, cellDefenders.Values,
            () => DefenseStatusText);

        enemyPoolRoot = new GameObject("Enemy Pool").transform;
        enemyPoolRoot.SetParent(transform, false);
        PrewarmEnemies();

        if (victoryPanel != null && defeatPanel != null)
        {
            ConfigureResultPanel(victoryPanel, true);
            ConfigureResultPanel(defeatPanel, false);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Phase != OpenFieldPhase.Planning)
        {
            presentation.HidePreview();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartAssault();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ClearBuildSelection();
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

        if (PointerOverToolbar() || buildMenu.ContainsPointer(Input.mousePosition) ||
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
            SelectBuildCell(cell);
        }
        else if (selectedBuildCell.HasValue)
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

    public bool SelectBuildCell(Vector2Int cell)
    {
        ClearBuildSelection();
        if (Phase != OpenFieldPhase.Planning ||
            grid.CheckPlacement(cell, false) != OpenFieldPlacementResult.Valid) return false;
        selectedBuildCell = cell;
        buildMenu.Show(CellToWorld(cell));
        RefreshSelectedBuildPreview();
        return true;
    }

    private bool CanBuildChoice(int index)
    {
        if (Phase != OpenFieldPhase.Planning || !selectedBuildCell.HasValue ||
            index < 0 || index >= towerDefinitions.Length) return false;
        return string.IsNullOrEmpty(GetBuildRejection(towerDefinitions[index], selectedBuildCell.Value));
    }

    private string GetBuildRejection(TowerDefinition definition, Vector2Int cell)
    {
        if (definition == null || definition.Prefab == null || definition.BuildCost <= 0) return "未配置";
        if (definition.BlocksEnemyRoute && RemainingBarrierBuilds == 0) return "壁垒额度已用尽";
        OpenFieldPlacementResult placement;
        if (definition.BlocksEnemyRoute)
        {
            if (cachedBarrierCell != cell || cachedBarrierRevision != grid.Revision)
            {
                cachedBarrierCell = cell;
                cachedBarrierRevision = grid.Revision;
                cachedBarrierPlacement = grid.CheckPlacement(cell);
            }
            placement = cachedBarrierPlacement;
        }
        else placement = grid.CheckPlacement(cell, false);
        if (placement == OpenFieldPlacementResult.BlocksRoute) return "会封死入口";
        if (placement != OpenFieldPlacementResult.Valid) return "该格不能建造";
        return economy.CurrentATP < definition.BuildCost ? "ATP 不足" : string.Empty;
    }

    private TowerCombatState FindDefender(Vector3 position)
    {
        cellDefenders.TryGetValue(WorldToCell(position), out TowerCombatState defender);
        return defender;
    }

    private void HandleDefenderDied(TowerCombatState defender)
    {
        defender.Died -= HandleDefenderDied;
        Vector2Int cell = defender.Cell;
        if (!cellDefenders.TryGetValue(cell, out TowerCombatState registered) || registered != defender) return;
        cellDefenders.Remove(cell);
        if (placedSlots.TryGetValue(cell, out BuildSlot slot))
        {
            slot.Release();
            placedSlots.Remove(cell);
            Destroy(slot.gameObject);
        }
        grid.Release(cell);
        previewCell = null;
        RebuildSignalLinks();
        Destroy(defender.gameObject);
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
        if (!isActiveAndEnabled || Phase != OpenFieldPhase.Planning)
        {
            presentation.HideRoutes();
            return;
        }
        Vector2Int? candidate = selectedBuildCell.HasValue && hoveredBuildIndex >= 0 &&
            hoveredBuildIndex < towerDefinitions.Length && towerDefinitions[hoveredBuildIndex].BlocksEnemyRoute &&
            CanBuildChoice(hoveredBuildIndex) ? selectedBuildCell : null;
        presentation.ShowRoutes(grid, CellToWorld, cellSize, candidate);
    }

    private void RefreshSelectedBuildPreview()
    {
        if (!selectedBuildCell.HasValue || Phase != OpenFieldPhase.Planning)
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
        hoveredBuildIndex = -1;
        previewCell = null;
        if (buildMenu != null) buildMenu.Hide();
        if (presentation != null) presentation.HidePreview();
        RefreshPlanningRoutes();
    }

    private void OnDisable() => ClearBuildSelection();

    public void SelectTower(int index)
    {
        if (Phase != OpenFieldPhase.Planning || index < 0 || index >= towerDefinitions.Length ||
            towerDefinitions[index] == null)
        {
            return;
        }
        selectedTowerIndex = index;
    }

    public bool TryBuildAt(Vector2Int cell)
    {
        TowerDefinition definition = GetSelectedTower();
        if (Phase != OpenFieldPhase.Planning || definition == null || definition.Prefab == null ||
            !string.IsNullOrEmpty(GetBuildRejection(definition, cell)) ||
            !economy.TrySpend(definition.BuildCost))
        {
            return false;
        }

        GameObject slotObject = null;
        TowerController tower = null;
        bool occupied = false;
        bool quotaConsumed = false;
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
            if (!grid.TryOccupy(cell, definition.BlocksEnemyRoute))
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
            if (definition.BlocksEnemyRoute)
            {
                barriersBuilt++;
                quotaConsumed = true;
            }
            ClearBuildSelection();
            return true;
        }
        catch (Exception exception)
        {
            if (quotaConsumed) barriersBuilt--;
            if (occupied)
            {
                grid.Release(cell);
                placedSlots.Remove(cell);
                cellDefenders.Remove(cell);
            }
            if (tower != null) Destroy(tower.gameObject);
            if (slotObject != null) Destroy(slotObject);
            economy.AddATP(definition.BuildCost);
            Debug.LogException(exception, this);
            return false;
        }
    }

    public bool StartAssault()
    {
        if (Phase != OpenFieldPhase.Planning)
        {
            return false;
        }

        ClearBuildSelection();
        CreateRoundPaths();
        allEnemiesSpawned = false;
        Phase = OpenFieldPhase.Assault;
        presentation.HideRoutes();
        presentation.HidePreview();
        spawnRoutine = StartCoroutine(SpawnRound(waveSequence.GetWave(roundIndex)));
        return true;
    }

    public void RestartPrototype()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadNextLevel()
    {
        if (Phase != OpenFieldPhase.Victory || string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            return;
        }

        Time.timeScale = 1f;
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
        {
            if (!grid.TryGetPath(grid.GetSpawnCell(spawnIndex), pathCells) || pathCells.Count < 2)
            {
                throw new InvalidOperationException($"No route from spawn {spawnIndex} to the core.");
            }

            GameObject pathObject = new GameObject($"Round {CurrentRound} Route {spawnIndex + 1}");
            pathObject.transform.SetParent(transform);
            WaypointPath path = pathObject.AddComponent<WaypointPath>();
            Transform[] points = new Transform[pathCells.Count];
            for (int i = 0; i < pathCells.Count; i++)
            {
                GameObject point = new GameObject($"Point {i}");
                point.transform.SetParent(pathObject.transform);
                point.transform.position = CellToWorld(pathCells[i]);
                points[i] = point.transform;
            }
            path.Configure(points);
            roundPaths.Add(path);
        }
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
                if (SpawnEnemy(definition, roundPaths[spawnIndex++ % roundPaths.Count], null) == null)
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
        FinishRoundIfReady();
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
                request.Path == null || !roundPaths.Contains(request.Path)) return;
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

        roundIndex++;
        if (betweenRoundATP > 0) economy.AddATP(betweenRoundATP);
        Phase = OpenFieldPhase.Planning;
        enemyHud.ClearHints();
        RefreshPlanningRoutes();
    }

    private void FinishRun(OpenFieldPhase result)
    {
        Phase = result;
        ClearBuildSelection();
        enemyHud.ClearHints();
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
        DestroyRoundPaths();
        presentation.HidePreview();
        if (victoryPanel != null && defeatPanel != null)
        {
            victoryPanel.SetActive(result == OpenFieldPhase.Victory);
            defeatPanel.SetActive(result == OpenFieldPhase.Defeat);
        }
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

    private void DestroyRoundPaths()
    {
        foreach (WaypointPath path in roundPaths)
        {
            if (path != null) Destroy(path.gameObject);
        }
        roundPaths.Clear();
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

    private Rect ToolbarRect()
    {
        float width = Mathf.Max(0f, Mathf.Min(Screen.width - 16f, 760f));
        return new Rect(8f, 8f, width, 100f);
    }

    private bool PointerOverToolbar()
    {
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return ToolbarRect().Contains(mouse);
    }

    private void OnGUI()
    {
        if ((Phase == OpenFieldPhase.Victory || Phase == OpenFieldPhase.Defeat) &&
            victoryPanel != null && defeatPanel != null)
        {
            return;
        }

        Rect toolbar = ToolbarRect();
        GUI.Box(toolbar, "");
        GUI.Label(new Rect(18f, 12f, 720f, 24f),
            $"Round {CurrentRound}/3    Core {CurrentCoreLife}    ATP {CurrentATP}    {Phase}");

        if (Phase == OpenFieldPhase.Planning)
        {
            if (GUI.Button(new Rect(16f, 46f, Mathf.Min(160f, toolbar.width - 16f), 30f),
                "Start assault (Space)"))
            {
                StartAssault();
            }
            GUI.Label(new Rect(18f, 80f,
                Mathf.Max(0f, toolbar.width - 20f), 22f),
                "Click an empty cell, then choose a tower. Right-click / Esc cancels.");
        }
        else if (Phase == OpenFieldPhase.Victory || Phase == OpenFieldPhase.Defeat)
        {
            GUI.Label(new Rect(18f, 46f, 180f, 30f), Phase == OpenFieldPhase.Victory ? "Victory!" : "Core lost.");
            if (GUI.Button(new Rect(210f, 46f, 130f, 30f), "Restart")) RestartPrototype();
            if (GUI.Button(new Rect(350f, 46f, 150f, 30f), "Main menu")) ReturnToMainMenu();
            if (Phase == OpenFieldPhase.Victory && !string.IsNullOrWhiteSpace(nextSceneName) &&
                Application.CanStreamedLevelBeLoaded(nextSceneName) &&
                GUI.Button(new Rect(510f, 46f, 130f, 30f), "Next level")) LoadNextLevel();
        }
    }
}
