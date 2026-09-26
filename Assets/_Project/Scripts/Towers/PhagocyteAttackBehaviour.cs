using System;
using UnityEngine;

public class PhagocyteAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(0f)] private float baseEngulfHealth = 20f;
    [SerializeField, Min(0f)] private float markedEngulfHealth = 50f;
    [SerializeField, Min(0.01f)] private float digestionDuration = 3f;

    private float digestionUntil;

    public override bool CanAttack => Time.time >= digestionUntil;

    public override int GetTargetPriority(EnemyController target)
    {
        return ShouldEngulf(target) ? 1 : 0;
    }

    public override void Attack(EnemyController target, float damage, ProjectilePool projectilePool)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (digestionDuration <= 0f)
        {
            throw new InvalidOperationException("Digestion duration must be greater than zero.");
        }
        if (baseEngulfHealth < 0f || markedEngulfHealth < baseEngulfHealth)
        {
            throw new InvalidOperationException("Marked engulf health must be at least the base engulf health.");
        }

        if (ShouldEngulf(target))
        {
            Health health = target.GetComponent<Health>();
            if (health == null)
            {
                throw new InvalidOperationException("Engulf target needs a Health component.");
            }

            digestionUntil = Time.time + digestionDuration;
            target.TakeDamage(health.CurrentHealth);
            return;
        }

        target.TakeDamage(damage);
    }

    private bool ShouldEngulf(EnemyController target)
    {
        // TODO(USER): Decide eligibility using the target's current health,
        // engulf immunity, mark state, and the two health thresholds above.
        throw new NotImplementedException("Complete PhagocyteAttackBehaviour.ShouldEngulf before using this tower.");
    }
}
