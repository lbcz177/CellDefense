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
    public List<float> attackRange = new List<float> { 3.5f, 3.8f, 4.2f };
    public List<float> damage = new List<float> { 8f, 12f, 18f };
    public List<float> attackCooldown = new List<float> { 0.25f, 0.18f, 0.15f };

    [Header("特殊标签")]
    public bool canSlow = false;
    public bool canPierce = false;
    public bool isAOE = false;
    public bool isMelee = false;
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