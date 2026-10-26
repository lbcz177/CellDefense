using UnityEngine;

public readonly struct LevelScoreResult
{
    public readonly int KillScore, CoreScore, Stars;
    public int Score => KillScore + CoreScore;
    public LevelScoreResult(int killScore, int coreScore, int stars)
    {
        KillScore = killScore;
        CoreScore = coreScore;
        Stars = stars;
    }
}

public static class LevelScoring
{
    public const int MaximumScore = 10000;
    public const int TwoStarScore = 7000;
    public const int ThreeStarScore = 9000;

    // Only scheduled enemies count. Summons cannot increase the numerator or denominator.
    // Integer division deliberately rounds down; scores below a threshold stay below it.
    public static LevelScoreResult Evaluate(bool victory, int kills, int scheduled, int coreLife, int initialLife)
    {
        int killScore = scheduled > 0 ? (int)(4000L * Mathf.Clamp(kills, 0, scheduled) / scheduled) : 0;
        int coreScore = victory && initialLife > 0
            ? (int)(6000L * Mathf.Clamp(coreLife, 0, initialLife) / initialLife) : 0;
        return new LevelScoreResult(killScore, coreScore, StarsForScore(victory, killScore + coreScore));
    }

    public static int StarsForScore(bool victory, int score)
    {
        if (!victory) return 0;
        return score >= ThreeStarScore ? 3 : score >= TwoStarScore ? 2 : 1;
    }
}
