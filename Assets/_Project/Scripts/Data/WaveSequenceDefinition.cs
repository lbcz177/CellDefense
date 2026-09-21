using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveSequenceDefinition", menuName = "Cell Defense/Wave Sequence Definition")]
public class WaveSequenceDefinition : ScriptableObject
{
    [SerializeField]
    private WaveDefinition[] waves;
    [SerializeField, Min(0f)]
    private float interWaveDelay = 2f;

    public int WaveCount => waves?.Length ?? 0;
    public float InterWaveDelay => interWaveDelay;

    public WaveDefinition GetWave(int index)
    {
        if (index < 0 || index >= WaveCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        WaveDefinition wave = waves[index];
        if (wave == null)
        {
            throw new InvalidOperationException($"Wave at index {index} is not assigned.");
        }

        return wave;
    }
}
