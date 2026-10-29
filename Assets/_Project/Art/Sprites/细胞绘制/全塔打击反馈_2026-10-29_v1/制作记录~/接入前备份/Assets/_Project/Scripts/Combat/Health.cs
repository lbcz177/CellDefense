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
        if(float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth <= 0)
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

        if(float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount must be greater than zero.");
        }
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if(CurrentHealth <= 0 && !IsDead)
        {
            Die();
        }
    }

    public float Heal(float amount)
    {
        EnemyRangeQuery.RequirePositive(amount, nameof(amount));
        if (IsDead || CurrentHealth <= 0f || MaxHealth <= 0f) return 0f;
        float previous = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        if (CurrentHealth > previous) HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        return CurrentHealth - previous;
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
