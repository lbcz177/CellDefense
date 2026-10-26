using UnityEngine;

// Shared by result panels and level cards; editable sources live in the art folder.
public sealed class ScoreArtworkResources : ScriptableObject
{
    public Texture2D starsAtlas;
    public Sprite scoreRibbon;
    public Texture2D scoreGlyphAtlas, smallScoreGlyphAtlas;
    private static ScoreArtworkResources defaults;
    public static ScoreArtworkResources Defaults => defaults != null ? defaults :
        (defaults = Resources.Load<ScoreArtworkResources>("ScoreArtworkResources"));
}
