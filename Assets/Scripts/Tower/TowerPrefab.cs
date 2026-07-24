using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPrefab : MonoBehaviour
{
    public int cost;
    public float attackRange;
    public float damage;
    public float attackCooldown;
    [SerializeField] Transform firePoint;
    ObjectPool bulletPool;
    EnemyManager enemyManager;

    void Awake()
    {
        bulletPool = FindObjectOfType<ObjectPool>();
        enemyManager = FindObjectOfType<EnemyManager>();
        firePoint = transform.Find("FirePoint");
    }

    float cooldownTimer;
    void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if(cooldownTimer > 0)
        {
            return;
        }
        EnemyHealth target = enemyManager.GetNearestEnemy(transform.position, attackRange);
        if(target == null)
        {
            return;
        }
        cooldownTimer = attackCooldown;
        GameObject bullet = bulletPool.Get();
        bullet.transform.position = firePoint.position;
        bullet.GetComponent<Bullet>().Init(target.transform, damage, 10f);
        
    }
}