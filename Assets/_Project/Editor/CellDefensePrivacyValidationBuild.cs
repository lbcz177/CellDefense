using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds an explicitly local validation package. It does not replace the store candidate.
public static class CellDefensePrivacyValidationBuild
{
    [MenuItem("胞卫战/Android/构建隐私整改验证 APK")]
    public static void Build()
    {
        string previousVersion = PlayerSettings.bundleVersion;
        int previousCode = PlayerSettings.Android.bundleVersionCode;
        string output = Path.GetFullPath("Builds/Android/CellDefense-0.1.4-privacy-validation.apk");
        if (File.Exists(output)) throw new InvalidOperationException("保留已有验证包，请先另存旧包再重新构建：" + output);
        try
        {
            CellDefenseAndroidBuild.Configure();
            PlayerSettings.bundleVersion = "0.1.4";
            PlayerSettings.Android.bundleVersionCode = 5;
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string[] scenes = CellDefenseAndroidBuild.GetGameplayScenes();
            if (scenes.Length != 6 || !scenes[0].EndsWith("/MainMenu.unity", StringComparison.Ordinal))
                throw new InvalidOperationException("隐私验证包需要当前主菜单及五关的六场景构建配置。");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = output, target = BuildTarget.Android, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("隐私整改验证包构建失败，请查看日志。");
            Debug.Log("本地隐私整改验证包（未上传；政策联系渠道待补）：" + output);
        }
        finally
        {
            PlayerSettings.bundleVersion = previousVersion;
            PlayerSettings.Android.bundleVersionCode = previousCode;
        }
    }
}
