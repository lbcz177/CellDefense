using UnityEngine;
[CreateAssetMenu(menuName="CellDefense/Guidance Artwork")]
public sealed class GuidanceArtwork : ScriptableObject
{
    public Sprite tutorialFrame, regionFrame, tissue, particle, warning;
    public static GuidanceArtwork Load() => Resources.Load<GuidanceArtwork>("GuidanceArtwork");
}
