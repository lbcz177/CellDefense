using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class PromptPhraseArtwork
{
    public string text;
    public Sprite sprite;
}

// Runtime UI uses the exported PNGs; the source font is only kept in the art production archive.
public sealed class PromptImageResources : ScriptableObject
{
    public Texture2D glyphAtlas;
    public Texture2D smallGlyphAtlas;
    public string characters;
    public float[] advances;
    public float[] smallAdvances;
    public int columns = 56;
    public int cellSize = 18;
    public Sprite panel, enemyPanel, button, selectedButton;
    public PromptPhraseArtwork[] phrases;
    private Dictionary<char, int> glyphs;
    private Dictionary<string, Sprite> phraseLookup;
    private static PromptImageResources defaults;
    public static PromptImageResources Defaults => defaults != null ? defaults :
        (defaults = Resources.Load<PromptImageResources>("PromptImageResources"));

    public bool TryGlyph(char character, out int index)
    {
        if (glyphs == null)
        {
            glyphs = new Dictionary<char, int>(characters.Length);
            for (int i = 0; i < characters.Length; i++) glyphs[characters[i]] = i;
        }
        return glyphs.TryGetValue(character, out index);
    }

    public bool TryPhrase(string value, out Sprite sprite)
    {
        if (phraseLookup == null)
        {
            phraseLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (PromptPhraseArtwork phrase in phrases)
                if (phrase.sprite != null) phraseLookup[phrase.text] = phrase.sprite;
        }
        return phraseLookup.TryGetValue(value, out sprite);
    }

    public static void StylePanel(Image image, bool enemy = false)
    {
        PromptImageResources artwork = Defaults;
        if (artwork == null) throw new InvalidOperationException("Prompt image resources are missing.");
        image.sprite = enemy ? artwork.enemyPanel : artwork.panel;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = 1f;
    }

    public static void StyleButton(Button control)
    {
        PromptImageResources artwork = Defaults;
        if (artwork == null) throw new InvalidOperationException("Prompt image resources are missing.");
        control.image.sprite = artwork.button;
        control.image.type = Image.Type.Sliced;
        control.image.color = Color.white;
        control.transition = Selectable.Transition.SpriteSwap;
        control.spriteState = new SpriteState
        {
            highlightedSprite = artwork.selectedButton,
            selectedSprite = artwork.selectedButton,
            pressedSprite = artwork.selectedButton,
            disabledSprite = artwork.button
        };
    }
}
