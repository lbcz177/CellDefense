using UnityEngine;

// The consent decision is native and precedes Unity; this bridge only exposes review later.
public static class AndroidPrivacy
{
    public static void SetMenuEntryVisible(bool visible)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            activity.Call("setPrivacyButtonVisible", visible);
#endif
    }
}
