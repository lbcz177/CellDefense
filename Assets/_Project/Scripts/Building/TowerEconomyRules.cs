using System;

// Shared by the legacy fixed-slot levels and open-field levels.
public static class TowerEconomyRules
{
    public const int DefaultSellRefundPercent = 50;

    public static int GetBuildCost(TowerDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (definition.BuildCost <= 0)
            throw new InvalidOperationException("Tower build cost must be greater than zero.");
        return definition.BuildCost;
    }

    public static int GetSellRefund(TowerController tower, int percent)
    {
        if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        if (tower == null) return 0;
        // Use money actually paid, including upgrades, never today's config price.
        long totalPaid = (long)tower.BuildCostPaid + tower.UpgradeCostPaid;
        return checked((int)(totalPaid * percent / 100));
    }
}
