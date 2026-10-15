using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyShieldState : MonoBehaviour
{
    public event Action<float, float> Changed;
    public float Current { get; private set; }
    public float Capacity { get; private set; }
    public bool HasShield => isActiveAndEnabled && Current > 0f;
    public void Initialize(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount));
        Current = Capacity = amount;
        Changed?.Invoke(Current, Capacity);
    }
    public void ApplyAtLeast(float amount)
    {
        EnemyRangeQuery.RequirePositive(amount, nameof(amount));
        if (!isActiveAndEnabled) return;
        Current = Mathf.Max(Current, amount);
        Capacity = Mathf.Max(Capacity, Current);
        Changed?.Invoke(Current, Capacity);
    }
    public float Absorb(float amount)
    {
        EnemyRangeQuery.RequirePositive(amount, nameof(amount));
        if (!HasShield) return amount;
        float absorbed = Mathf.Min(Current, amount);
        Current = Mathf.Max(0f, Current - absorbed);
        Changed?.Invoke(Current, Capacity);
        return Mathf.Max(0f, amount - absorbed);
    }
    private void OnDisable() => Initialize(0f);
}
