using System;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public event Action ReachedEnd;
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;
    private WaypointPath path;
    private int targetPointIndex;
    private bool isMoving;

    public float Progress
    {
        get
        {
            if (path == null || targetPointIndex <= 0)
            {
                return 0f;
            }

            int segmentStartIndex = targetPointIndex - 1;
            Vector3 segmentStart = path.GetPoint(segmentStartIndex).position;
            Vector3 segmentEnd = path.GetPoint(targetPointIndex).position;
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            if (segmentLength <= Mathf.Epsilon)
            {
                return targetPointIndex;
            }

            float distanceFromStart = Vector3.Distance(segmentStart, transform.position);
            float segmentProgress = Mathf.Clamp01(distanceFromStart / segmentLength);
            return segmentStartIndex + segmentProgress;
        }
    }

    public void Initialize(WaypointPath path)
    {
        if(path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        if(path.PointCount < 2)
        {
            throw new ArgumentException("Path must have at least 2 points.", nameof(path));
        }
        this.path = path;
        transform.position = path.GetPoint(0).position;
        targetPointIndex = 1;
        isMoving = false;
    }

    public void Begin()
    {
        if(path == null)
        {
            throw new InvalidOperationException("PathFollower has not been initialized with a valid path.");
        }
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving)
        {
            return;
        }
        Move();
    }

    private void Move()
    {
        Transform targetPoint = path.GetPoint(targetPointIndex);
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);
        if (transform.position != targetPoint.position)
        {
            return;
        }
        if(targetPointIndex == path.PointCount - 1)
        {
            ReachEnd();
            return;
        }
        targetPointIndex++;
    }

    private void ReachEnd()
    {
        if(!isMoving)
        {
            return;
        }
        isMoving = false;
        ReachedEnd?.Invoke();
    }

    public void Stop()
    {
        isMoving = false;
    }
}
