using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("ËþµÄÔ¤ÖÆÌå")]
    [SerializeField] List<TowerPrefabData> towerPrefabs = new List<TowerPrefabData>();

    TowerData selectedTowerData;
    GameObject selectedTowerPrefab;

    public TowerData SelectedTowerData => selectedTowerData;
    public bool HasSelectedTower => selectedTowerData != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void SelectTower(int index)
    {
        if (index < 0 || index >= towerPrefabs.Count) return;
        
        selectedTowerData = towerPrefabs[index].towerData;
        selectedTowerPrefab = towerPrefabs[index].prefab;
    }

    public void DeselectTower()
    {
        selectedTowerData = null;
        selectedTowerPrefab = null;
    }

    public bool CanAffordTower()
    {
        if (selectedTowerData == null) return false;
        return GameManager.Instance.money >= selectedTowerData.cost;
    }

    public bool TryPlaceTower(Cell cell)
    {
        if (selectedTowerData == null) return false;
        if (!CanAffordTower()) return false;

        GameManager.Instance.money -= selectedTowerData.cost;
        
        GameObject tower = Instantiate(selectedTowerPrefab, cell.transform.position, Quaternion.identity);
        BaseTower baseTower = tower.GetComponent<BaseTower>();
        if (baseTower != null)
        {
            baseTower.Initialize(selectedTowerData);
        }
        
        cell.PlaceTower();
        
        return true;
    }

    public int GetTowerCount()
    {
        return towerPrefabs.Count;
    }

    public TowerData GetTowerData(int index)
    {
        if (index < 0 || index >= towerPrefabs.Count) return null;
        return towerPrefabs[index].towerData;
    }
}

[System.Serializable]
public class TowerPrefabData
{
    public TowerData towerData;
    public GameObject prefab;
}