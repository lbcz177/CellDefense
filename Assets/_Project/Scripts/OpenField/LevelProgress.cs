using UnityEngine;

// Read on demand so menus and newly loaded levels always see the latest local record.
public sealed class LevelProgress
{
    private static readonly string[] OrderedScenes =
    {
        "OpenFieldPrototype", "OpenFieldLevel02", "OpenFieldLevel03", "OpenFieldLevel04", "OpenFieldLevel05",
        "Chapter02Level01", "Chapter02Level02", "Chapter02Level03", "Chapter02Level04",
        "Chapter03Level01", "Chapter03Level02", "Chapter03Level03", "Chapter03Level04"
    };
    private readonly string prefix;
    public LevelProgress(string prefix = "CellDefense.LevelProgress.v1")
    {
        this.prefix = prefix;
    }

    private string Key(string scene) => prefix + "." + scene + ".BestScore";
    public static bool IsLevel(string scene) => System.Array.IndexOf(OrderedScenes, scene) >= 0;
    public bool HasCleared(string scene) => IsLevel(scene) && PlayerPrefs.HasKey(Key(scene));
    public int BestScore(string scene) => HasCleared(scene)
        ? Mathf.Clamp(PlayerPrefs.GetInt(Key(scene)), 0, LevelScoring.MaximumScore) : 0;
    public int BestStars(string scene) => LevelScoring.StarsForScore(HasCleared(scene), BestScore(scene));

    public bool IsUnlocked(string scene)
    {
        int index = System.Array.IndexOf(OrderedScenes, scene);
        if (index < 0) return false;
        for (int i = 0; i < index; i++)
            if (!HasCleared(OrderedScenes[i])) return false;
        return true;
    }

    // Call only for victory. A retry can improve a record but never lower it.
    public bool SaveVictory(string scene, int score)
    {
        if (!IsLevel(scene)) return false;
        score = Mathf.Clamp(score, 0, LevelScoring.MaximumScore);
        if (HasCleared(scene) && BestScore(scene) >= score) return false;
        PlayerPrefs.SetInt(Key(scene), score);
        PlayerPrefs.Save();
        return true;
    }
}
