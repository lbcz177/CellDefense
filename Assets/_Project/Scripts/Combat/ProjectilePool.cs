using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField]
    private Projectile prefab;
    [SerializeField, Min(0)]
    private int initialCapacity = 8;
    [SerializeField, Min(1)]
    private int maxInactiveCount = 32;

    private readonly Stack<Projectile> inactiveProjectiles = new Stack<Projectile>();
    private readonly HashSet<Projectile> activeProjectiles = new HashSet<Projectile>();
    private bool isInitialized;

    public int ActiveCount => activeProjectiles.Count;
    public int InactiveCount => inactiveProjectiles.Count;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }
        if (prefab == null)
        {
            throw new InvalidOperationException("Projectile prefab is not set in the inspector.");
        }
        if (initialCapacity < 0)
        {
            throw new InvalidOperationException("Initial capacity cannot be negative.");
        }
        if (maxInactiveCount <= 0)
        {
            throw new InvalidOperationException("Max inactive count must be greater than zero.");
        }
        if (initialCapacity > maxInactiveCount)
        {
            throw new InvalidOperationException(
                "Initial capacity cannot be greater than max inactive count.");
        }

        for (int i = 0; i < initialCapacity; i++)
        {
            inactiveProjectiles.Push(CreateProjectile());
        }

        isInitialized = true;
    }

    public Projectile Get(Vector3 position)
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException("ProjectilePool is not initialized. Call Initialize() before using the pool.");
        }
        if (inactiveProjectiles.Count > 0)
        {
            Projectile projectile = inactiveProjectiles.Pop();
            if (!activeProjectiles.Add(projectile))
            {
                throw new InvalidOperationException("Projectile is already checked out.");
            }
            projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
            projectile.gameObject.SetActive(true);
            return projectile;
        }
        else
        {
            Projectile projectile = CreateProjectile();
            if (!activeProjectiles.Add(projectile))
            {
                throw new InvalidOperationException("Projectile is already checked out.");
            }
            projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
            projectile.gameObject.SetActive(true);
            return projectile;
        }


    }

    public void Release(Projectile projectile)
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException("ProjectilePool is not initialized. Call Initialize() before using the pool.");
        }
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile));
        }
        if (!activeProjectiles.Remove(projectile))
        {
            throw new InvalidOperationException("The projectile being released is not part of the active projectiles.");
        }
        projectile.ResetForPool();
        if (inactiveProjectiles.Count < maxInactiveCount)
        {
            projectile.gameObject.SetActive(false);
            inactiveProjectiles.Push(projectile);
        }
        else
        {
            Destroy(projectile.gameObject);
        }

    }

    private Projectile CreateProjectile()
    {
        Projectile projectile = Instantiate(prefab, transform);
        projectile.AssignPool(this);
        projectile.gameObject.SetActive(false);
        return projectile;
    }
}
