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
}
