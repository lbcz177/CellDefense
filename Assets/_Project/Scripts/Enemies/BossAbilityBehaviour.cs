using System;
using UnityEngine;

public abstract class BossAbilityBehaviour : EnemyBehaviour
{
    public virtual string WarningLabel => string.Empty;
    public override void Initialize(EnemyDefinition definition)
    {
        if (!definition.IsBoss) throw new ArgumentException("Boss abilities require a Boss definition.");
        base.Initialize(definition);
    }
    public abstract void ExecutePhase(int index);
}
