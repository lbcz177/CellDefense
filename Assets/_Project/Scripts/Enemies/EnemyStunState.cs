using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController), typeof(PathFollower))]
public class EnemyStunState : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.5f;
    [SerializeField] private bool stunImmune;
    private EnemyController enemy;
    private float stunnedUntil;
    private float immuneUntil;
    private float effectiveness = 1f;

    public void Initialize(EnemyDefinition definition)
    {
        effectiveness = definition.StunEffectiveness;
        recoveryDuration = definition.StunRecoveryDuration;
        ResetState();
    }

    public bool IsStunned => isActiveAndEnabled && Time.time < stunnedUntil;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
    }

    private void OnEnable() => ResetState();
    private void OnDisable() => ResetState();

    public void ResetState()
    {
        stunnedUntil = 0f;
        immuneUntil = 0f;
    }

    public bool TryStun(float duration)
    {
        EnemyRangeQuery.RequirePositive(duration, nameof(duration));
        EnemyRangeQuery.RequirePositive(recoveryDuration, nameof(recoveryDuration));
        if (!isActiveAndEnabled || stunImmune || effectiveness <= 0f || enemy == null ||
            !enemy.isActiveAndEnabled || !enemy.CanBeTargeted || Time.time < immuneUntil)
            return false;

        stunnedUntil = Time.time + duration * effectiveness;
        // Further hits cannot refresh the active stun or bypass its recovery window.
        immuneUntil = stunnedUntil + recoveryDuration;
        return true;
    }
}
