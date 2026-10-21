using UnityEngine;

// Original artwork and its hit areas move together; no assets or scene references are replaced.
public sealed class MobileSafeFrame : MonoBehaviour
{
    private RectTransform canvasRect, content;
    private Vector2 referenceSize;

    public static Transform Wrap(Canvas canvas, Vector2 size)
    {
        MobileDisplay.ConfigureCanvas(canvas);
        Transform parent = canvas.transform;
        int count = parent.childCount;
        GameObject frame = new GameObject("Mobile Safe Content", typeof(RectTransform));
        frame.layer = parent.gameObject.layer;
        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = size;
        for (int i = 0; i < count; i++) parent.GetChild(0).SetParent(rect, false);
        MobileSafeFrame layout = frame.AddComponent<MobileSafeFrame>();
        layout.canvasRect = (RectTransform)parent;
        layout.content = rect;
        layout.referenceSize = size;
        return rect;
    }
    public void Refresh()
    {
        if (canvasRect == null) return;
        Rect safe = MobileDisplay.LocalSafeArea(canvasRect);
        content.anchoredPosition = safe.center - canvasRect.rect.center;
        content.localScale = Vector3.one * Mathf.Max(.01f,
            Mathf.Min(safe.width / referenceSize.x, safe.height / referenceSize.y));
    }
    private void OnEnable() => Canvas.preWillRenderCanvases += Refresh;
    private void OnDisable() => Canvas.preWillRenderCanvases -= Refresh;
}
