using System;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public event Action ReachedEnd;
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;
    private WaypointPath path;
    private int targetPointIndex;
    private bool isMoving;

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
