using TMPro;
using UnityEngine;

public sealed class LevelResultScoreView : MonoBehaviour
{
    private PixelImageText score, breakdown, best;
    private StarRatingGraphic stars;

    public static LevelResultScoreView Create(Transform content, float verticalOffset)
    {
        GameObject root = new GameObject("Score Summary", typeof(RectTransform), typeof(LevelResultScoreView));
        root.layer = content.gameObject.layer;
        root.transform.SetParent(content, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = Vector2.up * verticalOffset;
        rect.sizeDelta = new Vector2(540f, 100f);
        LevelResultScoreView view = root.GetComponent<LevelResultScoreView>();
        view.stars = Stars(root.transform, "Stars", new Vector2(115f, 18f), new Vector2(110f, 28f), 0);
        // Reuse the existing image glyphs; no dynamic font layer or new atlas is needed.
        view.score = Label(root.transform, "Run Score", new Vector2(-85f, 18f), new Vector2(280f, 28f), 22f);
        view.breakdown = Label(root.transform, "Score Breakdown", Vector2.up * -12f, new Vector2(520f, 22f), 16f);
        view.best = Label(root.transform, "Best Score", Vector2.up * -36f, new Vector2(500f, 22f), 16f);
        return view;
    }

    public void Show(LevelScoreResult result, int bestScore, bool cleared)
    {
        stars.Stars = result.Stars;
        score.text = $"得 {result.Score} / 10000";
        breakdown.text = $"击杀 {result.KillScore} / 4000 · 核心 {result.CoreScore} / 6000";
        best.text = cleared ? $"最高 {bestScore}" : "未通关";
    }

    private static PixelImageText Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject obj = Element(parent, name, position, size, typeof(PixelImageText));
        PixelImageText text = obj.GetComponent<PixelImageText>();
        text.fontSize = fontSize;
        text.color = new Color(.28f, .08f, .17f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
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
