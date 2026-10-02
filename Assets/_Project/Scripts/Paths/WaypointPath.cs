using System;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    // TODO: Return the number of configured path points.
    public int PointCount => points?.Length ?? 0;


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
    }
}
