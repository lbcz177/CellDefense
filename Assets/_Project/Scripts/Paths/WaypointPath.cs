using System;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    private float[] remainingDistances;

    // TODO: Return the number of configured path points.
    public int PointCount => points?.Length ?? 0;

    private void Awake()
    {
        if (PointCount >= 2)
        {
            RebuildRemainingDistances();
        }
    }

    // Individual paths stay immutable while followers use them. Route changes replace
    // the path and rebind followers; Configure builds the new shared distance cache.
    public float GetRemainingDistanceFromPoint(int index)
    {
        if (index < 0 || index >= PointCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (remainingDistances == null || remainingDistances.Length != PointCount)
        {
            RebuildRemainingDistances();
        }
        return remainingDistances[index];
    }

    private void RebuildRemainingDistances()
    {
        remainingDistances = new float[PointCount];
        for (int i = PointCount - 2; i >= 0; i--)
        {
            remainingDistances[i] = remainingDistances[i + 1] +
                Vector3.Distance(points[i].position, points[i + 1].position);
        }
    }


    public Transform GetPoint(int index)
    {
        if(index >= 0 && index < PointCount)
        {
            return points[index];
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public void Configure(Transform[] configuredPoints)
    {
        if (configuredPoints == null || configuredPoints.Length < 2)
        {
            throw new ArgumentException("Path must contain at least two points.", nameof(configuredPoints));
        }
        foreach (Transform point in configuredPoints)
        {
            if (point == null)
            {
                throw new ArgumentException("Path points cannot be null.", nameof(configuredPoints));
            }
        }
        points = (Transform[])configuredPoints.Clone();
        RebuildRemainingDistances();
    }
}
