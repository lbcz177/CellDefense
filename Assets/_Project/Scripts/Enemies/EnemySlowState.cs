using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PathFollower))]
public class EnemySlowState : MonoBehaviour
{
    private readonly Dictionary<int, SlowEntry> slowBySource = new Dictionary<int, SlowEntry>();
    private readonly List<int> expiredSource = new List<int>();
    public float SpeedMultiplier { get; private set; } = 1f;
    private float effectiveness = 1f;

    public void Initialize(EnemyDefinition definition)
    {
        effectiveness = definition.SlowEffectiveness;
        ResetState();
    }

    public void ResetState()
    {
        SpeedMultiplier = 1f;
        slowBySource.Clear();
        expiredSource.Clear();
    }

    public void ApplySlow(float fraction, int sourceID, float duration)
    {
        if (float.IsNaN(fraction) || float.IsInfinity(fraction) || fraction < 0f || fraction >= 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be finite and in [0, 1).");
        }
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be finite and greater than 0.");
        }
        SlowEntry entry = new SlowEntry
        {
            Fraction = fraction * effectiveness,
            ExpiresAt = Time.time + duration
        };
        slowBySource[sourceID] = entry;
        RecalculateMultiplier();
    }

    private void RecalculateMultiplier()
    {
        float maxFraction = 0f;
        foreach (var entry in slowBySource.Values)
        {
            if (Time.time >= entry.ExpiresAt)
            {
                continue; // Skip expired entries
            }
            if (entry.Fraction > maxFraction)
            {
                maxFraction = entry.Fraction;
            }
        }
        SpeedMultiplier = 1 - maxFraction;
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || slowBySource.Count == 0) return;

        expiredSource.Clear();
        foreach (var kvp in slowBySource)
        {
            if (Time.time >= kvp.Value.ExpiresAt)
            {
                expiredSource.Add(kvp.Key);
            }
        }
        foreach (var sourceID in expiredSource)
        {
            slowBySource.Remove(sourceID);
        }
        if (expiredSource.Count > 0)
        {
            RecalculateMultiplier();
        }
    }
}
