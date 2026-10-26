using UnityEngine;

// Read on demand so menus and newly loaded levels always see the latest local record.
public sealed class LevelProgress
{
    private readonly string prefix;
    public LevelProgress(string prefix = "CellDefense.LevelProgress.v1")
    {
        this.prefix = prefix;
    }

    private string Key(string scene) => prefix + "." + scene + ".BestScore";
    public static bool IsLevel(string scene) => scene == "OpenFieldPrototype" || scene == "OpenFieldLevel02" ||
        scene == "OpenFieldLevel03" || scene == "OpenFieldLevel04" || scene == "OpenFieldLevel05";
    public bool HasCleared(string scene) => IsLevel(scene) && PlayerPrefs.HasKey(Key(scene));
    public int BestScore(string scene) => HasCleared(scene)
        ? Mathf.Clamp(PlayerPrefs.GetInt(Key(scene)), 0, LevelScoring.MaximumScore) : 0;
    public int BestStars(string scene) => LevelScoring.StarsForScore(HasCleared(scene), BestScore(scene));

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
