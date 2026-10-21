using System;
using System.Collections.Generic;
using UnityEngine;

// Health and contact ownership for open-field cells, including breakable barriers.
[DisallowMultipleComponent]
public sealed class TowerCombatState : MonoBehaviour
{
    private readonly Dictionary<EnemyController, uint> contacts = new Dictionary<EnemyController, uint>();
    public event Action<TowerCombatState> Died;
    public TowerController Tower { get; private set; }
    public Vector2Int Cell { get; private set; }
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public int Capacity { get; private set; }
    public bool InterceptsAllEnemies => Tower != null && Tower.Definition.InterceptsAllEnemies;
    public int ContactCount => contacts.Count;
    public bool IsAlive => isActiveAndEnabled && CurrentHealth > 0f && Tower != null &&
        Tower.isActiveAndEnabled;
    public bool CanTakeContact => IsAlive;

    public void Initialize(TowerController tower, Vector2Int cell)
    {
        if (tower == null || tower.Definition == null) throw new ArgumentNullException(nameof(tower));
        EnemyRangeQuery.RequirePositive(tower.Definition.MaxHealth, nameof(tower.Definition.MaxHealth));
        if (tower.Definition.InterceptCapacity < 1) throw new ArgumentOutOfRangeException(nameof(tower));
        Tower = tower;
        Cell = cell;
        MaxHealth = CurrentHealth = tower.Definition.MaxHealth;
        Capacity = tower.Definition.InterceptCapacity;
        contacts.Clear();
    }

    public bool TryEngage(EnemyController enemy)
    {
        if (!CanTakeContact || enemy == null || !enemy.isActiveAndEnabled || !enemy.CanBeTargeted)
            return false;
        if (contacts.TryGetValue(enemy, out uint version))
        {
            if (version == enemy.SpawnVersion) return true;
            contacts.Remove(enemy);
        }
        if (!InterceptsAllEnemies && contacts.Count >= Capacity) return false;
        contacts.Add(enemy, enemy.SpawnVersion);
        return true;
    }

    public void Release(EnemyController enemy, uint version)
    {
        if (enemy != null && contacts.TryGetValue(enemy, out uint heldVersion) && heldVersion == version)
            contacts.Remove(enemy);
    }

    public void TakeDamage(float amount)
    {
        EnemyRangeQuery.RequirePositive(amount, nameof(amount));
        if (!CanTakeContact) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        SpriteHitFlash flash = GetComponent<SpriteHitFlash>();
        if (flash != null) flash.Play();
        if (CurrentHealth > 0f) return;
        contacts.Clear();
        // Disable immediately: no attack event or aura may fire before deferred Destroy.
        gameObject.SetActive(false);
        Died?.Invoke(this);
    }

    private void OnDisable() => contacts.Clear();
}

