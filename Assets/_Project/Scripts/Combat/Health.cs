using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public event Action<float, float> HealthChanged;
    public event Action Died;

    public float CurrentHealth { get; private set; }

    public void Initialize(float maxHealth)
    {
        // TODO: Validate and assign maximum and current health.
    }

    public void TakeDamage(float amount)
    {
        // TODO: Apply damage, notify observers, and trigger death when needed.
    }

    private void Die()
    {
        // TODO: Publish the death result exactly once.
    }
}
