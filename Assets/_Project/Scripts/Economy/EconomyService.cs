using System;

public class EconomyService
{
    public event Action<int> ATPChanged;

    public int CurrentATP { get; private set; }

    public void Initialize(int initialATP)
    {
        // TODO: Validate and assign the starting ATP value.
    }

    public bool CanAfford(int amount)
    {
        // TODO: Check a purchase without changing ATP.
        return false;
    }

    public bool TrySpend(int amount)
    {
        // TODO: Spend ATP only when the complete request is valid.
        return false;
    }

    public void AddATP(int amount)
    {
        // TODO: Add a validated amount and notify ATP observers.
    }
}
