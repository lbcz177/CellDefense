using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerData", menuName = "CellDefense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("基础信息")]
    public string towerName = "新塔";
    public TowerType towerType;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description = "塔的描述";

    [Header("造价与升级")]
    public int cost = 100;
    public List<int> upgradeCosts = new List<int> { 80, 150 };
    public int maxLevel = 3;

    [Header("属性（按等级）")]
    public List<TowerLevelStats> towerLevelStats = new List<TowerLevelStats> { new TowerLevelStats() };


    [Header("特殊标签")]
    public bool canSlow = false;
    public bool canPierce = false;
    public bool isAOE = false;
    public bool isMelee = false;


    public int LevelCountFind()
    {
        return towerLevelStats.Count;
    }

    public TowerLevelStats GetLevelStats(int level)
    {
        if(!IsValidLevel(level))
        {
            return default(TowerLevelStats);
        }
        return towerLevelStats[level - 1];
    }

    private bool IsValidLevel(int level)
    {
        if(level >= 1 && level <= towerLevelStats.Count)
        {
            return true;
        }
        else
        {
            Debug.LogWarning("等级超出范围或为空");
            return false;
        }
    }
}

public enum TowerType
{
    SentryTCell,
    BioBarrier,
    AcidicBCell,
    NeuralSpiker,
    Macrophage,
    ViralInterceptor
}

public struct TowerLevelStats
{
    public int upgradeCost;
    public float attackRange;
    public float damage;
    public float attackCooldown;
}