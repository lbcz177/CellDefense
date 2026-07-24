using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemyData enemyData;
    public float currentHp;
    bool isDead = false;

    void Start()
    {
        currentHp = enemyData.maxHp;
        FindObjectOfType<EnemyManager>().AddEnemy(this);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        GameManager.Instance.AddMoney(enemyData.rewardMoney);
        FindObjectOfType<WaveSpawner>().OnEnemyDeath();
        EnemyManager mgr = FindObjectOfType<EnemyManager>();
        if (mgr != null) mgr.RemoveEnemy(this);
        Destroy(gameObject, 0.5f);
    }

    void OnDestroy()
    {
        if (!isDead)
        {
            WaveSpawner ws = FindObjectOfType<WaveSpawner>();
            if (ws != null) ws.OnEnemyDeath();
        }
        EnemyManager mgr = FindObjectOfType<EnemyManager>();
        if (mgr != null) mgr.RemoveEnemy(this);
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            Die();
        }
    }
}