using UnityEngine;
using UnityEngine.UI;

// Native UI geometry keeps the pixel stars sharp without changing existing artwork.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StarRatingGraphic : MaskableGraphic
{
    private static readonly string[] Shape =
    {
        "00000100000", "00001110000", "00001110000", "11111111111",
        "01111111110", "00111111100", "00011111000", "00011111000",
        "00111011100", "00110001100", "01100000110"
    };
    private int stars;
    public int Stars
    {
        get => stars;
        set { stars = Mathf.Clamp(value, 0, 3); SetVerticesDirty(); }
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect bounds = GetPixelAdjustedRect();
        float unit = Mathf.Min(bounds.width / 39f, bounds.height / 11f);
        float left = bounds.center.x - 19.5f * unit;
        for (int star = 0; star < 3; star++)
        for (int row = 0; row < 11; row++)
        for (int column = 0; column < 11; column++)
        {
            if (Shape[row][column] != '1') continue;
            bool border = row == 0 || row == 10 || column == 0 || column == 10 ||
                Shape[Mathf.Max(0, row - 1)][column] == '0' ||
                Shape[Mathf.Min(10, row + 1)][column] == '0' ||
                Shape[row][Mathf.Max(0, column - 1)] == '0' || Shape[row][Mathf.Min(10, column + 1)] == '0';
            Color tint = star < stars
                ? (border ? new Color(.61f, .30f, .12f) : new Color(1f, .78f, .19f))
                : (border ? new Color(.54f, .38f, .35f) : new Color(.77f, .67f, .56f));
            float x = left + (star * 14 + column) * unit;
            float y = bounds.center.y + (4.5f - row) * unit;
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x, y + unit), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + unit, y + unit), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + unit, y), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
