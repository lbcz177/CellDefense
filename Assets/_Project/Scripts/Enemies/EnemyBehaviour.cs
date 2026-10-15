using UnityEngine;

public abstract class EnemyBehaviour : MonoBehaviour
{
    protected EnemyController Enemy { get; private set; }
    protected uint Version { get; private set; }
    protected bool IsCurrent => Enemy != null && Enemy.isActiveAndEnabled &&
        Enemy.CanBeTargeted && Enemy.SpawnVersion == Version && isActiveAndEnabled;

    public virtual void Initialize(EnemyDefinition definition)
    {
        Enemy = GetComponent<EnemyController>();
        Version = Enemy.SpawnVersion;
        ResetState();
    }
    public virtual void ResetState() { }
    protected virtual void OnDisable() => ResetState();
}
