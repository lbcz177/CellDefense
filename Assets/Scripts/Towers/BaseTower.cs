using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTower : MonoBehaviour
{
    [Header("基础属性")]
    protected TowerData towerData;
    protected int currentLevel = 1;
    protected float cooldownTimer;
    
    [Header("引用")]
    protected Transform firePoint;
    protected ObjectPool bulletPool;
    protected EnemyManager enemyManager;

    public TowerData TowerData => towerData;
    public int CurrentLevel => currentLevel;

    protected virtual void Awake()
    {
        bulletPool = FindObjectOfType<ObjectPool>();
        enemyManager = FindObjectOfType<EnemyManager>();
        firePoint = transform.Find("FirePoint");
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    public virtual void Initialize(TowerData data)
    {
        towerData = data;
    }

    protected virtual void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0) return;

        EnemyHealth target = FindTarget();
        if (target == null) return;

        Attack(target);
        cooldownTimer = GetCurrentAttackRate();
    }

    protected virtual EnemyHealth FindTarget()
    {
        if (enemyManager == null) return null;
        return enemyManager.GetNearestEnemy(transform.position, towerData.GetLevelStats(currentLevel).attackRange);
    }

    protected virtual void Attack(EnemyHealth target)
    {
        GameObject bullet = bulletPool.Get();
        bullet.transform.position = firePoint.position;
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        bulletComponent.Init(target.transform, towerData.GetLevelStats(currentLevel).damage, 10f);
    }

    protected virtual float GetCurrentAttackRate()
    {
        return towerData.GetLevelStats(currentLevel).attackCooldown;
    }

    public virtual bool Upgrade()
    {
        if (currentLevel >= towerData.maxLevel) return false;
        
        int cost = towerData.upgradeCosts[currentLevel - 1];
        if (GameManager.Instance.money < cost) return false;

        GameManager.Instance.money -= cost;
        currentLevel++;
        OnUpgrade();
        return true;
    }

    protected virtual void OnUpgrade()
    {
    }

    public virtual int GetSellValue()
    {
        int totalCost = towerData.cost;
        for (int i = 0; i < currentLevel - 1; i++)
        {
            totalCost += towerData.upgradeCosts[i];
        }
        return Mathf.FloorToInt(totalCost * 0.7f);
    }

    public virtual void Sell()
    {
        GameManager.Instance.money += GetSellValue();
        Destroy(gameObject);
    }
}