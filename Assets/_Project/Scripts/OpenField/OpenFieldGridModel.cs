using System;
using System.Collections.Generic;
using UnityEngine;

public enum OpenFieldPlacementResult
{
    Valid,
    OutsideMap,
    ReservedCell,
    OccupiedCell,
    BlocksRoute,
    BlockedCell
}

public sealed class OpenFieldGridModel
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    private readonly bool[,] occupied;
    private readonly bool[,] routeBlocked;
    private readonly bool[,] blocked;
    private readonly Vector2Int[] spawnCells;
    private int[,] distances;

    public int Width { get; }
    public int Height { get; }
    public Vector2Int CoreCell { get; }
    public int SpawnCount => spawnCells.Length;
    public int Revision { get; private set; }

    public OpenFieldGridModel(int width, int height, Vector2Int coreCell, Vector2Int[] spawnCells,
        Vector2Int[] blockedCells = null)
    {
        if (width < 3 || height < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid must be at least 3 by 3.");
        }
        if (spawnCells == null || spawnCells.Length == 0)
        {
            throw new ArgumentException("At least one spawn cell is required.", nameof(spawnCells));
        }

        Width = width;
        Height = height;
        CoreCell = coreCell;
        if (!Contains(coreCell))
        {
            throw new ArgumentOutOfRangeException(nameof(coreCell));
        }

        this.spawnCells = (Vector2Int[])spawnCells.Clone();
        HashSet<Vector2Int> uniqueSpawns = new HashSet<Vector2Int>();
        foreach (Vector2Int spawn in this.spawnCells)
        {
            if (!Contains(spawn) || spawn == coreCell || !uniqueSpawns.Add(spawn))
            {
                throw new ArgumentException("Spawn cells must be distinct, inside the grid, and outside the core.", nameof(spawnCells));
            }
        }

        occupied = new bool[width, height];
        routeBlocked = new bool[width, height];
        blocked = new bool[width, height];
        if (blockedCells != null)
        {
            foreach (Vector2Int cell in blockedCells)
            {
                if (!Contains(cell) || IsReserved(cell) || blocked[cell.x, cell.y])
                {
                    throw new ArgumentException("Blocked cells must be distinct, inside the grid, and outside the core and spawns.", nameof(blockedCells));
                }
                blocked[cell.x, cell.y] = true;
            }
        }
        distances = CalculateDistances();
        foreach (Vector2Int spawn in this.spawnCells)
        {
            if (distances[spawn.x, spawn.y] < 0)
            {
                throw new ArgumentException("Every spawn must have a route to the core.", nameof(blockedCells));
            }
        }
    }

    public Vector2Int GetSpawnCell(int index)
    {
        if (index < 0 || index >= spawnCells.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return spawnCells[index];
    }

    public bool Contains(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
    }

    public bool IsOccupied(Vector2Int cell)
    {
        return Contains(cell) && occupied[cell.x, cell.y];
    }

    public bool IsBlocked(Vector2Int cell)
    {
        return Contains(cell) && blocked[cell.x, cell.y];
    }

    public bool IsReserved(Vector2Int cell)
    {
        if (cell == CoreCell)
        {
            return true;
        }
        foreach (Vector2Int spawn in spawnCells)
        {
            if (cell == spawn)
            {
                return true;
            }
        }
        return false;
    }

    public OpenFieldPlacementResult CheckPlacement(Vector2Int cell, bool blocksRoute = true)
    {
        if (!Contains(cell))
        {
            return OpenFieldPlacementResult.OutsideMap;
        }
        if (IsReserved(cell))
        {
            return OpenFieldPlacementResult.ReservedCell;
        }
        if (blocked[cell.x, cell.y])
        {
            return OpenFieldPlacementResult.BlockedCell;
        }
        if (occupied[cell.x, cell.y])
        {
            return OpenFieldPlacementResult.OccupiedCell;
        }

        if (!blocksRoute) return OpenFieldPlacementResult.Valid;
        routeBlocked[cell.x, cell.y] = true;
        int[,] candidateDistances;
        try
        {
            candidateDistances = CalculateDistances();
        }
        finally
        {
            routeBlocked[cell.x, cell.y] = false;
        }

        foreach (Vector2Int spawn in spawnCells)
        {
            if (candidateDistances[spawn.x, spawn.y] < 0)
            {
                return OpenFieldPlacementResult.BlocksRoute;
            }
        }
        return OpenFieldPlacementResult.Valid;
    }

    public bool TryOccupy(Vector2Int cell, bool blocksRoute = true)
    {
        if (CheckPlacement(cell, blocksRoute) != OpenFieldPlacementResult.Valid)
        {
            return false;
        }
        occupied[cell.x, cell.y] = true;
        routeBlocked[cell.x, cell.y] = blocksRoute;
        if (blocksRoute) distances = CalculateDistances();
        Revision++;
        return true;
    }

    public void Release(Vector2Int cell)
    {
        if (!IsOccupied(cell))
        {
            throw new ArgumentException("Cell is not occupied.", nameof(cell));
        }
        occupied[cell.x, cell.y] = false;
        Revision++;
        if (routeBlocked[cell.x, cell.y])
        {
            routeBlocked[cell.x, cell.y] = false;
            distances = CalculateDistances();
        }
    }

    public bool TryGetPath(Vector2Int start, List<Vector2Int> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }
        result.Clear();
        if (!Contains(start) || distances[start.x, start.y] < 0)
        {
            return false;
        }

        Vector2Int current = start;
        result.Add(current);
        while (current != CoreCell)
        {
            int nextDistance = distances[current.x, current.y] - 1;
            bool found = false;
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = current + direction;
                if (!Contains(next) || distances[next.x, next.y] != nextDistance)
                {
                    continue;
                }
                current = next;
                result.Add(current);
                found = true;
                break;
            }
            if (!found)
            {
                result.Clear();
                return false;
            }
        }
        return true;
    }

    public bool TryGetPreviewPath(Vector2Int start, Vector2Int barrier, List<Vector2Int> result)
    {
        if (CheckPlacement(barrier) != OpenFieldPlacementResult.Valid)
        {
            result.Clear();
            return false;
        }
        int[,] savedDistances = distances;
        routeBlocked[barrier.x, barrier.y] = true;
        try
        {
            distances = CalculateDistances();
            return TryGetPath(start, result);
        }
        finally
        {
            routeBlocked[barrier.x, barrier.y] = false;
            distances = savedDistances;
        }
    }

    private int[,] CalculateDistances()
    {
        int[,] result = new int[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                result[x, y] = -1;
            }
        }

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        result[CoreCell.x, CoreCell.y] = 0;
        frontier.Enqueue(CoreCell);
        while (frontier.Count > 0)
        {
            Vector2Int cell = frontier.Dequeue();
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = cell + direction;
                if (!Contains(next) || blocked[next.x, next.y] || routeBlocked[next.x, next.y] || result[next.x, next.y] >= 0)
                {
                    continue;
                }
                result[next.x, next.y] = result[cell.x, cell.y] + 1;
                frontier.Enqueue(next);
            }
        }
        return result;
    }
}
