using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class StarRatingGraphic : MaskableGraphic
{
    private int stars;
    public int Stars
    {
        get => stars;
        set { stars = Mathf.Clamp(value, 0, 3); SetVerticesDirty(); }
    }
    public override Texture mainTexture => ScoreArtworkResources.Defaults != null
        ? ScoreArtworkResources.Defaults.starsAtlas : base.mainTexture;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (ScoreArtworkResources.Defaults == null || ScoreArtworkResources.Defaults.starsAtlas == null) return;
        Rect bounds = GetPixelAdjustedRect();
        float size = Mathf.Min(bounds.width / 3.5f, bounds.height);
        float left = bounds.center.x - size * 1.75f;
        for (int star = 0; star < 3; star++)
        {
            float x = left + star * size * 1.25f;
            float y = bounds.center.y - size * .5f;
            float u = star < stars ? 0f : .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y), color, new Vector2(u, 0f));
            mesh.AddVert(new Vector3(x, y + size), color, new Vector2(u, 1f));
            mesh.AddVert(new Vector3(x + size, y + size), color, new Vector2(u + .5f, 1f));
            mesh.AddVert(new Vector3(x + size, y), color, new Vector2(u + .5f, 0f));
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
