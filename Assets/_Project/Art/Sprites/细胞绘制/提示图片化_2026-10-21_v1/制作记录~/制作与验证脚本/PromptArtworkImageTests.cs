using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PromptArtworkImageTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object value, string name) => value.GetType().GetField(name, Flags).GetValue(value);
    private static object Get(object value, string name) => value.GetType().GetProperty(name, Flags).GetValue(value);

    [Test]
    public void AllFixedPhrasesHaveImportedSpritesAndEveryCharacterHasAnImage()
    {
        Object art = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Resources/PromptImageResources.asset");
        Assert.That(art, Is.Not.Null);
        var phrases = (Array)Field(art, "phrases");
        Assert.That(phrases.Length, Is.GreaterThanOrEqualTo(96));
        string glyphs = (string)Field(art, "characters");
        Assert.That(glyphs.Distinct().Count(), Is.EqualTo(glyphs.Length));
        Assert.That(((Array)Field(art, "advances")).Length, Is.EqualTo(glyphs.Length));
        Assert.That(((Array)Field(art, "smallAdvances")).Length, Is.EqualTo(glyphs.Length));
        Assert.That(((Texture2D)Field(art, "smallGlyphAtlas")).filterMode, Is.EqualTo(FilterMode.Point));
        foreach (object entry in phrases)
        {
            string copy = (string)Field(entry, "text");
            Sprite sprite = (Sprite)Field(entry, "sprite");
            Assert.That(sprite, Is.Not.Null, copy);
            Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point), copy);
            foreach (char c in copy.Where(c => !char.IsWhiteSpace(c)))
                Assert.That(glyphs.Contains(c), Is.True, $"Missing image for {c} in {copy}");
        }
        foreach (string name in new[] { "panel", "enemyPanel", "button", "selectedButton" })
            Assert.That((Sprite)Field(art, name), Is.Not.Null, name);
    }

    [Test]
    public void FixedCopyUsesPhraseSpriteAndChangingValuesUseGlyphImagesWithoutTruncation()
    {
        GameObject obj = new GameObject("Image text validation", typeof(RectTransform), Runtime("PixelImageText"));
        try
        {
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 160f);
            Component label = obj.GetComponent(Runtime("PixelImageText"));
            label.GetType().GetProperty("fontSize").SetValue(label, 18f);
            MethodInfo populate = label.GetType().GetMethod("OnPopulateMesh", Flags, null,
                new[] { typeof(VertexHelper) }, null);
            foreach (string copy in new[] { "跳过引导", "生命 99999/100000\n出售 +2147483647 ATP\n拦截 127 · 无上限" })
            {
                label.GetType().GetProperty("text").SetValue(label, copy);
                using (var mesh = new VertexHelper())
                {
                    populate.Invoke(label, new object[] { mesh });
                    Assert.That(mesh.currentVertCount, Is.GreaterThan(0));
                    Assert.That((int)Get(label, "MissingGlyphCount"), Is.Zero);
                    Assert.That((int)Get(label, "RenderedGlyphCount"), Is.GreaterThan(0));
                    Assert.That((bool)Get(label, "UsesPhraseArtwork"), Is.EqualTo(copy == "跳过引导"));
                }
            }
            Assert.That(obj.GetComponent("TextMeshProUGUI"), Is.Null);
        }
        finally { Object.DestroyImmediate(obj); }
    }
}
