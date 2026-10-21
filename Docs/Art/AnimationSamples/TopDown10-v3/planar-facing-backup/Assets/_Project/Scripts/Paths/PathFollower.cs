using System;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public event Action ReachedEnd;
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;
    private float speedMultiplier = 1f;
    private EnemySlowState slowState;
    private EnemyStunState stunState;
    private WaypointPath path;
    private int targetPointIndex;
    private bool isMoving;
    private float abilitySpeedMultiplier = 1f;
    public WaypointPath CurrentPath => path;
    public int NextPointIndex => targetPointIndex;
    public float SceneSpeedMultiplier => speedMultiplier;
    // Open-field contact combat may hold a unit on its current route. Legacy scenes leave this null.
    public Func<Vector3, bool> BeforeMove { get; set; }

    public float RemainingDistance
    {
        get
        {
            if (path == null || targetPointIndex <= 0)
            {
                return float.PositiveInfinity;
            }
            return Vector3.Distance(transform.position, path.GetPoint(targetPointIndex).position) +
                path.GetRemainingDistanceFromPoint(targetPointIndex);
        }
    }

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

    private void Awake()
    {
        slowState = GetComponent<EnemySlowState>();
        stunState = GetComponent<EnemyStunState>();
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
        slowState = GetComponent<EnemySlowState>();
        stunState = GetComponent<EnemyStunState>();
        transform.position = path.GetPoint(0).position;
        targetPointIndex = 1;
        isMoving = false;
        abilitySpeedMultiplier = 1f;
        speedMultiplier = 1f;
        BeforeMove = null;
    }

    public void InitializeAt(WaypointPath sharedPath, Vector3 position, int nextPointIndex)
    {
        Initialize(sharedPath);
        if (nextPointIndex < 1 || nextPointIndex >= sharedPath.PointCount)
            throw new ArgumentOutOfRangeException(nameof(nextPointIndex));
        transform.position = position;
        targetPointIndex = nextPointIndex;
    }

    public void SetBaseSpeed(float speed)
    {
        EnemyRangeQuery.RequirePositive(speed, nameof(speed));
        moveSpeed = speed;
    }

    public void SetAbilitySpeedMultiplier(float multiplier)
    {
        EnemyRangeQuery.RequirePositive(multiplier, nameof(multiplier));
        abilitySpeedMultiplier = multiplier;
    }

    public void Begin()
    {
        if(path == null)
        {
            throw new InvalidOperationException("PathFollower has not been initialized with a valid path.");
        }
        isMoving = true;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }
        speedMultiplier = multiplier;
    }

    private void Update()
    {
        if (!isMoving || (stunState != null && stunState.IsStunned))
        {
            return;
        }
        Move();
    }

    private void Move()
    {
        Transform targetPoint = path.GetPoint(targetPointIndex);
        float slowMultiplier = slowState != null && slowState.isActiveAndEnabled
            ? slowState.SpeedMultiplier : 1f;
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position, targetPoint.position,
            moveSpeed * speedMultiplier * abilitySpeedMultiplier * slowMultiplier * Time.deltaTime);
        if (BeforeMove != null && !BeforeMove(nextPosition)) return;
        transform.position = nextPosition;
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
