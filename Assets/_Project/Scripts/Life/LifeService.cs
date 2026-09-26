using System;

public class LifeService
{
    public event Action<int> LifeChanged;

    public int CurrentLife { get; private set; }
    public bool IsDepleted => CurrentLife <= 0;

    public void Initialize(int initialLife)
    {
        if (initialLife <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialLife), "Initial life must be greater than zero.");
        }

        CurrentLife = initialLife;
        LifeChanged?.Invoke(CurrentLife);
    }

    public bool TryLoseLife(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Life loss must be greater than zero.");
        }

        if (IsDepleted)
        {
            return false;
        }

        CurrentLife = Math.Max(0, CurrentLife - amount);
        LifeChanged?.Invoke(CurrentLife);
        return true;
    }
}