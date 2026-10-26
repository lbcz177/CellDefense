using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Fixed copy is a sliced phrase sprite. Changing values are quads from the same pixel glyph PNG.
// No Font/TMP renderer, dynamic font atlas, or per-character GameObjects are created.
[AddComponentMenu("UI/Pixel Image Text")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PixelImageText : MaskableGraphic
{
    [SerializeField, TextArea] private string displayText = "";
    [SerializeField] private float pointSize = 18f;
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.TopLeft;
    public bool enableAutoSizing;
    public float fontSizeMin = 11f, fontSizeMax = 18f;
    // Optional image-only lettering style, with the same cells and advances as the shared atlas.
    public Texture2D glyphAtlasOverride, smallGlyphAtlasOverride;
    private readonly List<Line> lines = new List<Line>();
    private Sprite phrase;
    private Texture texture;
    public int RenderedGlyphCount { get; private set; }
    public int RenderedLineCount { get; private set; }
    public bool UsesPhraseArtwork { get; private set; }
    public bool UsesSmallGlyphs { get; private set; }
    public int MissingGlyphCount { get; private set; }
    public string text
    {
        get => displayText;
        set { if (displayText == value) return; displayText = value ?? ""; phrase = null; SetAllDirty(); }
    }
    public float fontSize { get => pointSize; set { pointSize = value; SetVerticesDirty(); } }
    public TextAlignmentOptions alignment { get => textAlignment; set { textAlignment = value; SetVerticesDirty(); } }
    public override Texture mainTexture => texture != null ? texture :
        PromptImageResources.Defaults != null ? PromptImageResources.Defaults.glyphAtlas : Texture2D.whiteTexture;

    private struct Line { public int start, end; public float width; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        RenderedGlyphCount = RenderedLineCount = MissingGlyphCount = 0;
        UsesPhraseArtwork = false;
        UsesSmallGlyphs = false;
        PromptImageResources art = PromptImageResources.Defaults;
        if (art == null || art.glyphAtlas == null || string.IsNullOrEmpty(displayText)) return;
        Rect bounds = GetPixelAdjustedRect();
        if (bounds.width <= 0f || bounds.height <= 0f) return;
        float size = enableAutoSizing ? fontSizeMax : pointSize;
        float screenScale = canvas != null ? Mathf.Max(.01f, Mathf.Abs(canvas.scaleFactor *
            rectTransform.lossyScale.x / Mathf.Max(.001f, canvas.transform.lossyScale.x))) : 1f;
        bool small = size * screenScale < 12f && art.smallGlyphAtlas != null;
        float nativeSize = small ? 8f : 12f;
        float lineRatio = small ? 1.25f : 4f / 3f;
        float minimum = Mathf.Max(fontSizeMin, nativeSize / screenScale);
        size = Mathf.Max(size, nativeSize / screenScale);
        // A fixed phrase uses its hand-checked line breaks when it fits at the requested size.
        if (!small && glyphAtlasOverride == null && art.TryPhrase(displayText, out phrase) &&
            phrase.rect.width * size / 12f <= bounds.width + .01f &&
            phrase.rect.height * size / 12f <= bounds.height + .01f)
        {
            texture = phrase.texture;
            Vector2 dimensions = phrase.rect.size * (size / 12f);
            Vector2 origin = AlignedOrigin(bounds, dimensions);
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(phrase);
            Quad(mesh, origin.x, origin.y, dimensions.x, dimensions.y, uv, color);
            UsesPhraseArtwork = true;
            RenderedGlyphCount = displayText.Replace("\n", "").Length;
            RenderedLineCount = Mathf.RoundToInt(phrase.rect.height / 16f);
            return;
        }
        phrase = null;
        Texture2D atlas = small
            ? (smallGlyphAtlasOverride != null ? smallGlyphAtlasOverride : art.smallGlyphAtlas)
            : (glyphAtlasOverride != null ? glyphAtlasOverride : art.glyphAtlas);
        float[] advances = small ? art.smallAdvances : art.advances;
        int cellSize = small ? 14 : art.cellSize;
        texture = atlas;
        UsesSmallGlyphs = small;
        Layout(art, advances, nativeSize, size, bounds.width);
        if (enableAutoSizing)
            while (size > minimum && (lines.Count * size * lineRatio > bounds.height + .01f || TooWide(bounds.width)))
            {
                size = Mathf.Max(minimum, size - .5f);
                Layout(art, advances, nativeSize, size, bounds.width);
            }
        float scale = size / nativeSize, lineHeight = size * lineRatio;
        float height = lines.Count * lineHeight;
        Vector2 top = AlignedOrigin(bounds, new Vector2(0f, height));
        RenderedLineCount = lines.Count;
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            Line line = lines[lineIndex];
            float x = AlignedOrigin(bounds, new Vector2(line.width, height)).x;
            float y = top.y + height - (lineIndex + 1) * lineHeight;
            for (int i = line.start; i < line.end; i++)
            {
                char c = displayText[i];
                if (c == '\r') continue;
                if (!art.TryGlyph(c, out int index))
                {
                    MissingGlyphCount++;
                    art.TryGlyph('?', out index);
                }
                if (!char.IsWhiteSpace(c))
                {
                    int column = index % art.columns, row = index / art.columns;
                    float left = (float)(column * cellSize) / atlas.width;
                    float right = (float)((column + 1) * cellSize) / atlas.width;
                    float upper = 1f - (float)(row * cellSize) / atlas.height;
                    float lower = 1f - (float)((row + 1) * cellSize) / atlas.height;
                    Quad(mesh, x - 3f * scale, y - (small ? 2f : 1f) * scale, cellSize * scale, cellSize * scale,
                        new Vector4(left, lower, right, upper), color);
                    RenderedGlyphCount++;
                }
                x += advances[index] * scale;
            }
        }
    }

    private bool TooWide(float width)
    {
        foreach (Line line in lines) if (line.width > width + .01f) return true;
        return false;
    }

    private void Layout(PromptImageResources art, float[] advances, float nativeSize, float size, float width)
    {
        lines.Clear();
        int start = 0;
        float current = 0f;
        for (int i = 0; i < displayText.Length; i++)
        {
            char c = displayText[i];
            if (c == '\r') continue;
            if (c == '\n')
            {
                lines.Add(new Line {start = start, end = i, width = current});
                start = i + 1; current = 0; continue;
            }
            if (!art.TryGlyph(c, out int index)) art.TryGlyph('?', out index);
            float advance = advances[index] * size / nativeSize;
            if (current + advance > width && i > start)
            {
                lines.Add(new Line {start = start, end = i, width = current});
                start = i; current = 0;
            }
            current += advance;
        }
        lines.Add(new Line {start = start, end = displayText.Length, width = current});
    }

    private Vector2 AlignedOrigin(Rect bounds, Vector2 size)
    {
        int value = (int)textAlignment;
        bool left = (value & 1) != 0, right = (value & 4) != 0;
        bool upper = (value & 256) != 0, lower = (value & 1024) != 0;
        return new Vector2(left ? bounds.xMin : right ? bounds.xMax - size.x : bounds.center.x - size.x * .5f,
            upper ? bounds.yMax - size.y : lower ? bounds.yMin : bounds.center.y - size.y * .5f);
    }

    private static void Quad(VertexHelper mesh, float x, float y, float w, float h, Vector4 uv, Color32 tint)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector3(x, y), tint, new Vector2(uv.x, uv.y));
        mesh.AddVert(new Vector3(x, y + h), tint, new Vector2(uv.x, uv.w));
        mesh.AddVert(new Vector3(x + w, y + h), tint, new Vector2(uv.z, uv.w));
        mesh.AddVert(new Vector3(x + w, y), tint, new Vector2(uv.z, uv.y));
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first + 2, first + 3, first);
    }

    // The selected phrase and glyph atlases use the same material but different textures.
    public override void Rebuild(CanvasUpdate update)
    {
        base.Rebuild(update);
        if (update == CanvasUpdate.PreRender) canvasRenderer.SetTexture(mainTexture);
    }
}
