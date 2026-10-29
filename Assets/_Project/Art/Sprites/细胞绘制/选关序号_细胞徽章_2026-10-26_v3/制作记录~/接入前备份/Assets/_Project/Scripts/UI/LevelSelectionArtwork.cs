using UnityEngine;
using UnityEngine.UI;

// The number page and details share reusable sprites; saved scores stay in LevelProgress.
public sealed class LevelSelectionArtwork : ScriptableObject
{
    public Sprite background, node, selectedNode, panel, button, selectedButton;
    public Sprite historyLabel, bestStarsLabel, unplayedLabel, startLabel, returnLabel, overviewHint;
    private static LevelSelectionArtwork defaults;
    public static LevelSelectionArtwork Defaults => defaults != null ? defaults :
        (defaults = Resources.Load<LevelSelectionArtwork>("LevelSelectionArtwork"));

    public void Style(Button control, bool numbered = false)
    {
        control.targetGraphic.color = Color.white;
        Image image = (Image)control.targetGraphic;
        image.sprite = numbered ? node : button;
        image.type = Image.Type.Sliced;
        control.transition = Selectable.Transition.SpriteSwap;
        Sprite highlight = numbered ? selectedNode : selectedButton;
        control.spriteState = new SpriteState
        {
            highlightedSprite = highlight, selectedSprite = highlight,
            pressedSprite = highlight, disabledSprite = image.sprite
        };
    }
}
