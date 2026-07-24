using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    [SerializeField] GameObject towerPrefab;
    [SerializeField] GridManager gridManager;
    GameManager gameManager;
    TowerPrefab towerData;
    Cell[] allCells;
    void Start()
    {
        gameManager = GameManager.Instance;
        towerData = towerPrefab.GetComponent<TowerPrefab>();
        allCells = FindObjectsOfType<Cell>();
        foreach (Cell cell in allCells)
        {
            cell.OnCellClicked += HandleCellClicked;
        }
    }

    void HandleCellClicked(Cell cell)
    {

        if(Time.timeScale == 0)
        {
            return;
        }
        if(gameManager.money < towerData.cost)
        {
            return;
        }
        gameManager.money -= towerData.cost;
        Instantiate(towerPrefab, cell.transform.position, Quaternion.identity);
        cell.PlaceTower();

    }

    void OnDestroy()
    {
        foreach (Cell cell in allCells)
        {
            cell.OnCellClicked -= HandleCellClicked;
        }
    }
}