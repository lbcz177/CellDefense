using UnityEngine;
using UnityEngine.UI;

// Shared mobile policy. Editor preview exercises the same layout code without an APK.
public static class MobileDisplay
{
#if UNITY_EDITOR
    public static bool PreviewEnabled = UnityEditor.SessionState.GetBool("CellDefense.MobilePreview", false);
    public static Rect? PreviewSafeArea;
#endif
    public static bool IsActive
    {
        get
        {
#if UNITY_EDITOR
            return Application.isMobilePlatform || PreviewEnabled;
#else
            return Application.isMobilePlatform;
#endif
        }
    }
    public static Rect SafeArea
    {
        get
        {
#if UNITY_EDITOR
            if (IsActive && PreviewSafeArea.HasValue) return PreviewSafeArea.Value;
#endif
            return Screen.safeArea;
        }
    }
    public static void ConfigureCanvas(Canvas canvas)
    {
        if (!IsActive || canvas == null) return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            scaler.matchWidthOrHeight = 1f;
    }
    public static Rect LocalSafeArea(RectTransform canvasRect)
    {
        Canvas canvas = canvasRect.GetComponent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Rect safe = SafeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safe.min, camera, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safe.max, camera, out Vector2 max);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!Application.isMobilePlatform) return;
        Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Application.targetFrameRate = 60;
    }
}
