using System;

public enum EnemyDamageKind { Normal = 0, Acid = 1, Neural = 2 }

public readonly struct EnemyDamageContext
{
    public readonly float Amount;
    public readonly EnemyDamageKind Kind;
    public EnemyDamageContext(float amount, EnemyDamageKind kind = EnemyDamageKind.Normal)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if ((uint)kind > (uint)EnemyDamageKind.Neural)
            throw new ArgumentOutOfRangeException(nameof(kind));
        Amount = amount;
        Kind = kind;
    }
}
