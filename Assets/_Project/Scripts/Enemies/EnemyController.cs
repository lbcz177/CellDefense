using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{

    [SerializeField] private PathFollower pathFollower;
    public event Action<EnemyController> ReachedEnd;
    private bool hasResolved;
    public void Initialize(WaypointPath path)
    {
        if(pathFollower == null)
        {
            throw new ArgumentNullException(nameof(pathFollower));
        }
        if(path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        hasResolved = false;
        pathFollower.Initialize(path);
        pathFollower.Begin();
        
    }

    private void OnEnable()
    {
        pathFollower.ReachedEnd += HandleReachedEnd;
    }

    private void OnDisable()
    {
        pathFollower.ReachedEnd -= HandleReachedEnd;
    }

    private void HandleReachedEnd()
    {
        if (hasResolved)
        {
            return;
        }
        hasResolved = true;
        ReachedEnd?.Invoke(this);
    }

    private void HandleDeath()
    {
        // TODO: Resolve this enemy as killed exactly once.
    }
}
