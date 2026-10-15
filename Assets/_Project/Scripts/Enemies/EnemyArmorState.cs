using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyArmorState : MonoBehaviour
{
    private float reduction;
    private float breakDuration;
    private float brokenUntil;
    public bool HasArmor => isActiveAndEnabled && reduction > 0f;
    public bool IsBroken => HasArmor && Time.time < brokenUntil;
    public float DamageMultiplier => HasArmor && !IsBroken ? 1f - reduction : 1f;
    public void Initialize(EnemyDefinition definition)
    {
        reduction = definition.ArmorReduction;
        breakDuration = definition.ArmorBreakDuration;
        brokenUntil = 0f;
    }
    public void BreakArmor()
    {
        if (HasArmor) brokenUntil = Mathf.Max(brokenUntil, Time.time + breakDuration);
    }
    public void ResetState() => brokenUntil = 0f;
    private void OnDisable() => ResetState();
}
