using System;

public class EconomyService
{
    public event Action<int> ATPChanged;

    public int CurrentATP { get; private set; }

    public void Initialize(int initialATP)
    {
        if(initialATP < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialATP), "Initial ATP cannot be negative.");
        }
        CurrentATP = initialATP;
        ATPChanged?.Invoke(CurrentATP);
    }

    public bool CanAfford(int amount)
    {
        if(amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative or zero.");
        }
        return CurrentATP >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (CanAfford(amount))
        {
            CurrentATP -= amount;
            ATPChanged?.Invoke(CurrentATP);
            return true;
        }

        return false;
    }

    public void AddATP(int amount)
    {
        if(amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative or zero.");
        }
        CurrentATP += amount;
        ATPChanged?.Invoke(CurrentATP);
    }
}
