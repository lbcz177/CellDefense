using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    List<EnemyHealth> activeEnemies = new List<EnemyHealth>();
    
    public void AddEnemy(EnemyHealth e)
    {
        activeEnemies.Add(e);
    }

    public void RemoveEnemy(EnemyHealth e)
    {
        activeEnemies.Remove(e);
    }

    public EnemyHealth GetNearestEnemy(Vector3 position, float range)
    {
        EnemyHealth nearestEnemy = null;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] == null || activeEnemies[i].currentHp <= 0)
            {
                continue;
            }
            float dist = Vector3.Distance(position, activeEnemies[i].transform.position);
            if (dist > range)
            {
                continue;
            }
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestEnemy = activeEnemies[i];
            }
        }
        return nearestEnemy;
    }
}