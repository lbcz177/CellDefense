using UnityEngine;
using UnityEngine.UI;

// The number page and details share reusable sprites; saved scores stay in LevelProgress.
public sealed class LevelSelectionArtwork : ScriptableObject
{
    public Sprite background, node, selectedNode, panel, button, selectedButton;
    public Sprite historyLabel, bestStarsLabel, unplayedLabel, startLabel, returnLabel, overviewHint;
    public Sprite[] numberBadges, selectedNumberBadges;
    public Sprite chapterBackground, chapterTitle, chapterHint, chapterFrame, chapterHighlight;
    public Sprite titlePlate, backArrow, homeCaption, chaptersCaption;
    public SelectionChapterArtwork[] chapters;
    private static LevelSelectionArtwork defaults;
    public static LevelSelectionArtwork Defaults => defaults != null ? defaults :
        (defaults = Resources.Load<LevelSelectionArtwork>("LevelSelectionArtwork"));

    public bool HasNumberBadge(int index) => numberBadges != null && index >= 0 &&
        index < numberBadges.Length && numberBadges[index] != null;

    private Sprite NumberBadge(int index, bool selected)
    {
        if (!HasNumberBadge(index)) return selected ? selectedNode : node;
        return selected && selectedNumberBadges != null && index < selectedNumberBadges.Length &&
            selectedNumberBadges[index] != null ? selectedNumberBadges[index] : numberBadges[index];
    }

    public void Style(Button control, bool numbered = false, int numberIndex = -1)
    {
        control.targetGraphic.color = Color.white;
        Image image = (Image)control.targetGraphic;
        image.sprite = numbered ? NumberBadge(numberIndex, false) : button;
        image.type = numbered ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = numbered;
        control.transition = Selectable.Transition.SpriteSwap;
        Sprite highlight = numbered ? NumberBadge(numberIndex, true) : selectedButton;
        control.spriteState = new SpriteState
        {
            highlightedSprite = highlight, selectedSprite = highlight,
            pressedSprite = highlight, disabledSprite = image.sprite
        };
    }

    public void StyleChapter(Button control)
    {
        control.image.sprite = chapterFrame;
        control.image.type = Image.Type.Sliced;
        control.image.color = Color.white;
        control.transition = Selectable.Transition.SpriteSwap;
        control.spriteState = new SpriteState
        {
            highlightedSprite = chapterHighlight, selectedSprite = chapterHighlight,
            pressedSprite = chapterHighlight, disabledSprite = chapterFrame
        };
    }
}

[System.Serializable]
public sealed class SelectionChapterArtwork
{
    public string id, name;
    public Sprite background, cover, label, title, cardTitle, tagline, count;
    public SelectionLevelArtwork[] levels;
}

[System.Serializable]
public sealed class SelectionLevelArtwork
{
    public string id, name, sceneName;
    public Sprite map, title;
    public bool IsPreview => string.IsNullOrEmpty(sceneName);
}
