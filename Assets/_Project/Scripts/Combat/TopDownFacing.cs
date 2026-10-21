using UnityEngine;

// Sprite artwork faces +X. Turn in the XY plane without changing its handedness or size.
public static class TopDownFacing
{
    public static void FaceDirection(Transform body, SpriteRenderer renderer, Vector3 direction)
    {
        direction.z = 0f;
        if (direction.sqrMagnitude <= 0.000001f) return;
        body.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        if (renderer != null)
        {
            renderer.flipX = false;
            renderer.flipY = false;
        }
    }
}
