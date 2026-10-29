using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Local sideload build; publishing/signing for a store is a separate step.
public static class CellDefenseAndroidBuild
{
    private static readonly string[] Scenes =
    {
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/OpenFieldPrototype.unity",
        "Assets/_Project/Scenes/OpenFieldLevel02.unity",
        "Assets/_Project/Scenes/OpenFieldLevel03.unity",
        "Assets/_Project/Scenes/OpenFieldLevel04.unity",
        "Assets/_Project/Scenes/OpenFieldLevel05.unity",
        "Assets/_Project/Scenes/Chapter02Level01.unity",
        "Assets/_Project/Scenes/Chapter02Level02.unity",
        "Assets/_Project/Scenes/Chapter02Level03.unity",
        "Assets/_Project/Scenes/Chapter02Level04.unity",
        "Assets/_Project/Scenes/Chapter03Level01.unity",
        "Assets/_Project/Scenes/Chapter03Level02.unity",
        "Assets/_Project/Scenes/Chapter03Level03.unity",
        "Assets/_Project/Scenes/Chapter03Level04.unity"
    };

    internal static string[] GetGameplayScenes() => (string[])Scenes.Clone();

    [MenuItem("胞卫战/Android/检查构建环境")]
    public static void CheckEnvironment()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            Debug.LogWarning("缺少 Android Build Support。请为当前 Unity 安装 Android 模块及 SDK、NDK、OpenJDK。");
        else Debug.Log("Android 构建模块已安装；首次构建还会验证 SDK、NDK、JDK 与许可证。");
    }

    [MenuItem("胞卫战/Android/构建横屏试玩 APK")]
    public static void BuildApk()
    {
        Build(BuildOptions.Development, "CellDefense.apk");
    }

    [MenuItem("胞卫战/Android/构建上架候选 APK")]
    public static void BuildStoreCandidate()
    {
        Build(BuildOptions.None, "CellDefense-" + PlayerSettings.bundleVersion + ".apk");
    }

    private static void Build(BuildOptions options, string filename)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("缺少 Android Build Support；请先为 Unity 2022.3.53f1c1 安装 Android 模块及 SDK、NDK、OpenJDK。");
        Configure();
        EditorUserBuildSettings.buildAppBundle = false;
        string output = Path.GetFullPath(Path.Combine("Builds/Android", filename));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes, locationPathName = output, target = BuildTarget.Android,
            options = options
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Android 构建失败，请查看 Console 与 Editor.log。");
        Debug.Log("Android APK 已生成：" + output);
    }

    public static void Configure()
    {
        PlayerSettings.productName = "胞卫战";
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/_Project/Art/Sprites/细胞绘制/游戏图标_v1/胞卫战_游戏图标_1024.png");
        if (icon == null) throw new InvalidOperationException("Missing CellDefense app icon.");
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.celldefense.game");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
    }

    [MenuItem("胞卫战/Android/手机布局预览（运行前切换）")]
    public static void TogglePreview()
    {
        MobileDisplay.PreviewEnabled = !MobileDisplay.PreviewEnabled;
        SessionState.SetBool("CellDefense.MobilePreview", MobileDisplay.PreviewEnabled);
    }

    [MenuItem("胞卫战/Android/手机布局预览（运行前切换）", true)]
    private static bool ValidatePreview()
    {
        Menu.SetChecked("胞卫战/Android/手机布局预览（运行前切换）", MobileDisplay.PreviewEnabled);
        return !EditorApplication.isPlaying;
    }
}
