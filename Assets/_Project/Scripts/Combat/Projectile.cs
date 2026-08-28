using UnityEngine;

public class Projectile : MonoBehaviour
{
    public void Initialize(Transform target, float damage)
    {
        // TODO: Store the target and immutable attack result for this shot.
    }

    private void Update()
    {
        // TODO: Move only while the projectile has a valid flight state.
    }

    private void Move()
    {
        // TODO: Move toward the target without deciding combat rules here.
    }

    private void Hit()
    {
        // TODO: Deliver damage once and finish this projectile's lifetime.
    }
}
