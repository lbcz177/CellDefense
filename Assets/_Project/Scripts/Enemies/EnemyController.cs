using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{

    [SerializeField] private PathFollower pathFollower;
    public event Action<EnemyController, EnemyExitReason> Exited;
    private bool hasResolved;
    private Health health;//接入health引用
    public EnemyDefinition Definition { get; private set; }
    
    public bool CanBeTargeted => !hasResolved && health != null && !health.IsDead;
    

    void Awake()
    {
        health = GetComponent<Health>();
        pathFollower = GetComponent<PathFollower>();
    }

    public void Initialize(WaypointPath path, EnemyDefinition definition)
    {
        if(pathFollower == null)
        {
            throw new ArgumentNullException(nameof(pathFollower));
        }
        if(definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }
        if(path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        Definition = definition;
        hasResolved = false;
        health.Initialize(definition.MaxHealth);
        pathFollower.Initialize(path);
        pathFollower.Begin();
        
    }

    private void OnEnable()
    {
        pathFollower.ReachedEnd += HandleReachedEnd;
        health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        pathFollower.ReachedEnd -= HandleReachedEnd;
        health.Died -= HandleDeath;
    }

    private void HandleReachedEnd()
    {
        if (hasResolved)
        {
            return;
        }
        TryExit(EnemyExitReason.Leaked);
    }

    private void HandleDeath()
    {
        TryExit(EnemyExitReason.Killed);
    }

    private bool TryExit(EnemyExitReason reason)
    {
        if (hasResolved)
        {
            return false;
        }
        hasResolved = true;
        pathFollower.Stop();
        Exited?.Invoke(this, reason);
        return true;
    }

    public void Clear()
    {
        TryExit(EnemyExitReason.Cleared);
    }

    [ContextMenu("Test Take Damage")]
    void TestTakeDamage()
    {
        TakeDamage(50f);
    }

    public void TakeDamage(float amount)
    {
        if (CanBeTargeted)
        {
            health.TakeDamage(amount);
        }
    }
}
