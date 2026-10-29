using System;
using UnityEditor;

// Only detached validation processes set this flag; the user's editor is untouched.
public static class ChapterTwoValidationView
{
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        if (Environment.GetEnvironmentVariable("CELLDEFENSE_VALIDATION_LANDSCAPE") != "1") return;
        EditorApplication.delayCall += Configure;
    }
    private static void Configure()
    {
        const string key="CellDefense.ChapterTwoValidationView";
        if (SessionState.GetBool(key,false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var assembly=typeof(EditorWindow).Assembly;
        Type type=assembly.GetType("UnityEditor.GameViewSizes",true);
        object sizes=type.BaseType.GetProperty("instance").GetValue(null);
        object group=type.GetProperty("currentGroup").GetValue(sizes);
        int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        Type kind=assembly.GetType("UnityEditor.GameViewSizeType",true);
        object size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize",true),
            Enum.Parse(kind,"FixedResolution"),1280,720,"Chapter two validation baseline");
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView",true));
        view.GetType().GetMethod("SizeSelectionCallback").Invoke(view,new object[]{index,null});
        view.Repaint();SessionState.SetBool(key,true);
    }
}
