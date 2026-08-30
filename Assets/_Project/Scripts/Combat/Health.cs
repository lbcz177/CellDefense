using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public event Action<float, float> HealthChanged;
    public event Action Died;
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }


    public void Initialize(float maxHealth)
    {
        if(maxHealth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be greater than zero.");
        }
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        IsDead = false;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead)
        {
            return;
        }
        if(amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
        }
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if(CurrentHealth <= 0 && !IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }
        IsDead = true;
        Died?.Invoke();
    }
}
