using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelResultScoreView : MonoBehaviour
{
    private PixelImageText kills, core;
    private readonly Image[] digits = new Image[5];
    private StarRatingGraphic stars;

    public static LevelResultScoreView Create(Transform content, float verticalOffset)
    {
        GameObject root = new GameObject("Score Summary", typeof(RectTransform), typeof(LevelResultScoreView));
        root.layer = content.gameObject.layer;
        root.transform.SetParent(content, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = Vector2.up * verticalOffset;
        rect.sizeDelta = new Vector2(400f, 80f);
        LevelResultScoreView view = root.GetComponent<LevelResultScoreView>();
        Image ribbon = Picture(root.transform, "Score Ribbon", new Vector2(-58f, 12f), new Vector2(244f, 44f),
            ScoreArtworkResources.Defaults.scoreRibbon);
        ribbon.type = Image.Type.Sliced;
        PixelImageText caption = Label(root.transform, "Score Caption", new Vector2(-140f, 12f), new Vector2(40f, 24f), 16f);
        caption.text = "得分";
        caption.color = new Color(1f, .96f, .82f);
        PixelImageText maximum = Label(root.transform, "Score Maximum", new Vector2(25f, 10f), new Vector2(64f, 22f), 14f);
        maximum.text = "/ 10000";
        maximum.color = new Color(.93f, .78f, .65f);
        for (int i = 0; i < view.digits.Length; i++)
            view.digits[i] = Picture(root.transform, "Score Digit " + i, Vector2.zero, new Vector2(18f, 32f), null);
        view.stars = Stars(root.transform, "Stars", new Vector2(130f, 12f), new Vector2(102f, 32f), 0);
        OpenFieldUiResources hud = OpenFieldUiResources.LoadDefaults();
        Picture(root.transform, "Kill Icon", new Vector2(-168f, -24f), Vector2.one * 22f, hud.WaveIcon);
        Picture(root.transform, "Core Icon", new Vector2(18f, -24f), Vector2.one * 22f, hud.LifeIcon);
        view.kills = Label(root.transform, "Kill Score", new Vector2(-80f, -24f), new Vector2(148f, 22f), 16f);
        view.core = Label(root.transform, "Core Score", new Vector2(106f, -24f), new Vector2(148f, 22f), 16f);
        return view;
    }

    public void Show(LevelScoreResult result)
    {
        stars.Stars = result.Stars;
        string number = result.Score.ToString(System.Globalization.CultureInfo.InvariantCulture);
        OpenFieldUiResources hud = OpenFieldUiResources.LoadDefaults();
        for (int i = 0; i < digits.Length; i++)
        {
            digits[i].gameObject.SetActive(i < number.Length);
            if (i >= number.Length) continue;
            digits[i].sprite = hud.GetDigit(number[i] - '0');
            digits[i].rectTransform.anchoredPosition = new Vector2(-61f + (i - (number.Length - 1) * .5f) * 18f, 12f);
        }
        kills.text = $"击杀 {result.KillScore}/4000";
        core.text = $"核心 {result.CoreScore}/6000";
    }

    public static void StyleActionButton(Button button)
    {
        OpenFieldUiResources artwork = OpenFieldUiResources.LoadDefaults();
        Image frame = button.image;
        frame.sprite = artwork.SettingsBackFrame;
        frame.type = Image.Type.Sliced;
        frame.preserveAspect = false;
        frame.pixelsPerUnitMultiplier = 2f;
        frame.color = Color.white;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = artwork.SettingsBackFrameSelected,
            pressedSprite = artwork.SettingsBackFrameSelected,
            selectedSprite = artwork.SettingsBackFrameSelected,
            disabledSprite = artwork.SettingsBackFrame
        };
        PixelImageText caption = Label(button.transform, "Action Caption", Vector2.zero, new Vector2(228f, 32f), 24f);
        caption.text = button.name == "Next" ? "下一关" : button.name == "Again" ? "重新开始" : "返回主菜单";
    }

    private static PixelImageText Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject obj = Element(parent, name, position, size, typeof(PixelImageText));
        PixelImageText text = obj.GetComponent<PixelImageText>();
        text.fontSize = fontSize;
        text.glyphAtlasOverride = ScoreArtworkResources.Defaults.scoreGlyphAtlas;
        text.smallGlyphAtlasOverride = ScoreArtworkResources.Defaults.smallScoreGlyphAtlas;
        text.color = new Color(.26f, .11f, .21f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Image Picture(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
    {
        Image image = Element(parent, name, position, size, typeof(Image)).GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static StarRatingGraphic Stars(Transform parent, string name, Vector2 position, Vector2 size, int count)
    {
        StarRatingGraphic graphic = Element(parent, name, position, size, typeof(StarRatingGraphic)).GetComponent<StarRatingGraphic>();
        graphic.Stars = count;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static GameObject Element(Transform parent, string name, Vector2 position, Vector2 size, System.Type component)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), component);
        obj.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }
}
