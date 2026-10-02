using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum OpenFieldPhase
{
    Planning,
    Assault,
    Victory,
    Defeat
}

public sealed class OpenFieldPrototypeController : MonoBehaviour
{
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

    [Header("Rounds")]
    [SerializeField] private WaveSequenceDefinition waveSequence;
    [SerializeField, Min(0)] private int initialATP = 100;
    [SerializeField, Min(0)] private int betweenRoundATP = 40;
    [SerializeField, Min(1)] private int initialCoreLife = 10;

    [Header("Combat")]
    [SerializeField] private TowerDefinition[] towerDefinitions;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField, Min(0.1f)] private float signalRadiusCells = 3f;
    [SerializeField] private Camera gameplayCamera;

    private readonly Dictionary<Vector2Int, BuildSlot> placedSlots = new Dictionary<Vector2Int, BuildSlot>();
    private readonly HashSet<EnemyController> activeEnemies = new HashSet<EnemyController>();
    private readonly List<WaypointPath> roundPaths = new List<WaypointPath>();
    private readonly List<Vector2Int> pathCells = new List<Vector2Int>();
    private readonly EconomyService economy = new EconomyService();
    private readonly LifeService life = new LifeService();

    private OpenFieldGridModel grid;
    private OpenFieldPresentation presentation;
    private Coroutine spawnRoutine;
    private int roundIndex;
    private int selectedTowerIndex;
    private bool allEnemiesSpawned;

    public OpenFieldPhase Phase { get; private set; }
    public int CurrentRound => roundIndex + 1;
    public int CurrentATP => economy.CurrentATP;
    public int CurrentCoreLife => life.CurrentLife;

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
        if (cellSize <= 0f || signalRadiusCells <= 0f)
        {
            throw new InvalidOperationException("Cell size and signal radius must be positive.");
        }

        gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (gameplayCamera == null)
        {
            throw new InvalidOperationException("Assign a gameplay camera.");
        }

        grid = new OpenFieldGridModel(width, height, coreCell, spawnCells);
        projectilePool.Initialize();
        economy.Initialize(initialATP);
        life.Initialize(initialCoreLife);
        roundIndex = 0;
        selectedTowerIndex = 0;
        Phase = OpenFieldPhase.Planning;

        GameObject viewObject = new GameObject("Open Field Board and Preview");
        viewObject.transform.SetParent(transform, false);
        presentation = viewObject.AddComponent<OpenFieldPresentation>();
        presentation.Initialize(grid, CellToWorld, cellSize);
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

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTower(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectTower(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectTower(2);
        if (Input.GetKeyDown(KeyCode.Space)) StartAssault();

        TowerDefinition selected = GetSelectedTower();
        if (selected == null || selected.Prefab == null || PointerOverToolbar())
        {
            presentation.HidePreview();
            return;
        }

        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = transform.position.z - gameplayCamera.transform.position.z;
        Vector2Int cell = WorldToCell(gameplayCamera.ScreenToWorldPoint(screenPoint));
        if (!grid.Contains(cell))
        {
            presentation.HidePreview();
            return;
        }

        bool valid = grid.CheckPlacement(cell) == OpenFieldPlacementResult.Valid &&
                     selected.BuildCost > 0 && economy.CurrentATP >= selected.BuildCost;
        presentation.ShowPreview(selected, CellToWorld(cell), valid);
        if (Input.GetMouseButtonDown(0))
        {
            TryBuildAt(cell);
        }
    }

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
            definition.BuildCost <= 0 || grid.CheckPlacement(cell) != OpenFieldPlacementResult.Valid ||
            !economy.TrySpend(definition.BuildCost))
        {
            return false;
        }

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
            if (!grid.TryOccupy(cell))
            {
                throw new InvalidOperationException("The build cell became invalid during placement.");
            }
            occupied = true;
            placedSlots.Add(cell, slot);
            RebuildSignalLinks();
            return true;
        }
        catch (Exception exception)
        {
            if (occupied)
            {
                grid.Release(cell);
                placedSlots.Remove(cell);
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

        CreateRoundPaths();
        allEnemiesSpawned = false;
        Phase = OpenFieldPhase.Assault;
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
        if (wave.Enemy == null || wave.Enemy.EnemyPrefab == null)
        {
            throw new InvalidOperationException("Wave enemy prefab is missing.");
        }

        for (int i = 0; i < wave.EnemyCount && Phase == OpenFieldPhase.Assault; i++)
        {
            EnemyController enemy = Instantiate(wave.Enemy.EnemyPrefab);
            enemy.Exited += HandleEnemyExited;
            activeEnemies.Add(enemy);
            enemy.Initialize(roundPaths[i % roundPaths.Count], wave.Enemy);
            if (i < wave.EnemyCount - 1)
            {
                yield return new WaitForSeconds(wave.SpawnInterval);
            }
        }

        spawnRoutine = null;
        allEnemiesSpawned = true;
        FinishRoundIfReady();
    }

    private void HandleEnemyExited(EnemyController enemy, EnemyExitReason reason)
    {
        if (!activeEnemies.Remove(enemy))
        {
            return;
        }
        enemy.Exited -= HandleEnemyExited;
        if (reason == EnemyExitReason.Killed && enemy.Definition.KillReward > 0)
        {
            economy.AddATP(enemy.Definition.KillReward);
        }
        else if (reason == EnemyExitReason.Leaked)
        {
            life.TryLoseLife(1);
        }
        Destroy(enemy.gameObject);

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
    }

    private void FinishRun(OpenFieldPhase result)
    {
        Phase = result;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy == null) continue;
            enemy.Exited -= HandleEnemyExited;
            Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
        DestroyRoundPaths();
        presentation.HidePreview();
    }

    private void DestroyRoundPaths()
    {
        foreach (WaypointPath path in roundPaths)
        {
            if (path != null) Destroy(path.gameObject);
        }
        roundPaths.Clear();
    }

    private static Rect ToolbarRect()
    {
        return new Rect(8f, 8f, Mathf.Max(0f, Mathf.Min(Screen.width - 16f, 760f)), 108f);
    }

    private static bool PointerOverToolbar()
    {
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return ToolbarRect().Contains(mouse);
    }

    private void OnGUI()
    {
        Rect toolbar = ToolbarRect();
        GUI.Box(toolbar, "");
        GUI.Label(new Rect(18f, 12f, 720f, 24f),
            $"Round {CurrentRound}/3    Core {CurrentCoreLife}    ATP {CurrentATP}    {Phase}");

        if (Phase == OpenFieldPhase.Planning)
        {
            int visibleTowerCount = Mathf.Min(towerDefinitions.Length, 3);
            for (int i = 0; i < visibleTowerCount; i++)
            {
                TowerDefinition definition = towerDefinitions[i];
                if (definition == null) continue;
                string label = $"{i + 1}: {definition.DisplayName} ({definition.BuildCost})";
                if (i == selectedTowerIndex) label = "> " + label;
                if (GUI.Button(new Rect(16f + i * 152f, 46f, 146f, 30f),
                        new GUIContent(label, definition.BuildIcon != null ? definition.BuildIcon.texture : null)))
                {
                    SelectTower(i);
                }
            }
            if (GUI.Button(new Rect(476f, 46f, 160f, 30f), "Start assault (Space)"))
            {
                StartAssault();
            }
            GUI.Label(new Rect(18f, 82f, 700f, 22f),
                "Click a cell to build. Red preview means blocked, occupied, or insufficient ATP.");
        }
        else if (Phase == OpenFieldPhase.Victory || Phase == OpenFieldPhase.Defeat)
        {
            GUI.Label(new Rect(18f, 46f, 180f, 30f), Phase == OpenFieldPhase.Victory ? "Victory!" : "Core lost.");
            if (GUI.Button(new Rect(210f, 46f, 130f, 30f), "Restart")) RestartPrototype();
            if (GUI.Button(new Rect(350f, 46f, 150f, 30f), "Main menu")) ReturnToMainMenu();
        }
    }
}
