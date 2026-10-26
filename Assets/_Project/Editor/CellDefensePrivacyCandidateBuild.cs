using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CellDefensePrivacyCandidateBuild
{
    [MenuItem("胞卫战/Android/构建隐私整改候选 APK（0.1.4）")]
    public static void Build()
    {
        string policy = File.ReadAllText("Assets/Plugins/Android/CellPrivacy.androidlib/res/raw/cell_privacy_policy.txt");
        if (policy.Contains("本地整改验证稿") || policy.Contains("尚待补充"))
            throw new InvalidOperationException("先补齐政策中的运营者及有效联系渠道。");
        string output = Path.GetFullPath("Builds/Android/CellDefense-0.1.4.apk");
        if (File.Exists(output)) throw new InvalidOperationException("保留已有候选包，请先另存旧包：" + output);
        string previousVersion = PlayerSettings.bundleVersion;
        int previousCode = PlayerSettings.Android.bundleVersionCode;
        try
        {
            PlayerSettings.bundleVersion = "0.1.4";
            PlayerSettings.Android.bundleVersionCode = 5;
            CellDefenseAndroidBuild.BuildStoreCandidate();
            Debug.Log("隐私整改候选包已构建（尚未上传或提审）：" + output);
        }
        finally
        {
            PlayerSettings.bundleVersion = previousVersion;
            PlayerSettings.Android.bundleVersionCode = previousCode;
        }
    }
}
