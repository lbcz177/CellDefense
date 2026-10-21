# 首次 Android APK 构建与校验

日期：2026-10-19；Unity 2022.3.53f1c1。

用户先选择暂缓安装组件，随后明确要求实际打包。本次已通过 Unity Hub 安装匹配版本的 Android Build Support、SDK/NDK Tools、OpenJDK，并成功构建 APK。此前无 APK 的原文见 [构建前快照](../History/2026-10-19_before_android_apk/INDEX.md)。

## 交付

- 文件：E:/unity/CellDefense/Builds/Android/CellDefense.apk
- 大小：73,175,093 字节，69.79 MiB。
- SHA256：fc0325f0f8e929ee89a54efa67fdd6408655e7fbfa4f856f1747bd695fc70c72
- 版本：0.1.0，versionCode=1；包名 com.celldefense.game。
- Development 试玩包，IL2CPP，ARMv7 + ARM64；最低 API 23，目标 API 35。
- 内容：MainMenu 和五个 OpenField 主线场景，包含本次等比投影修复；旧固定路线 Prototype / Level02 不在构建清单。

## 已验证

Unity BuildPipeline 返回 Succeeded，Gradle assemble 成功，批处理以 0 退出。APK 178 个 ZIP 条目 CRC 完整性通过；apksigner 验证 v1 / v2 签名有效，证书为 Android Debug。aapt 确认包名、版本、SDK、启动 Activity 和两种 ABI。通过读取 ELF 文件头，确认两种架构的 libunity.so 和 libil2cpp.so 的位数及机器类型正确。最终目录的 APK 与构建输出 SHA256 一致。

打包快照中的 96 份项目 C# 文件与主工程一致。此前源码测试为 EditMode 127/127、PlayMode 31/31；本次未改玩法代码，未重新运行这些测试，也不将编辑器测试视为 Android 真机结果。

没有连接设备安装、实际启动、通关、性能、后台 / 旋转或设备兼容性验收。此文件是本地试玩包，未发布商店。

## 工具与缓存

Android 组件安装在 E:/UnityEditors/2022.3.53f1c1/Editor/Data/PlaybackEngines/AndroidPlayer；NDK 23.1.7779620、OpenJDK 11、SDK 平台 33/34/35、Build Tools 34.0.0。构建缓存及 TEMP/TMP 放在 Logs/AndroidBuild/GradleCache 和 Logs/AndroidBuild/Temp；系统和默认签名配置仍可使用用户目录。

在 Logs/CodexTradingTests 隔离工程构建，未关闭用户主工程。临时 Editor 调用脚本只写入隔离工程，最终移除；SDK 组件和 Gradle 缓存保留供下次打包复用。没有上传 APK 或更新 Notion。

## 证据

- [构建 / 校验摘要](2026-10-19_AndroidApk_summary.json)
- [Unity 构建记录](2026-10-19_AndroidApk_build.json)
- [APK 完整性与架构](2026-10-19_AndroidApk_verification.json)
- [aapt 清单](2026-10-19_AndroidApk_manifest.txt)
- [apksigner 签名](2026-10-19_AndroidApk_signature.txt)
- [构建快照源码 SHA256](2026-10-19_AndroidApk_sources.json)

完整本机日志保存在 Logs/AndroidBuild/build.log 与 hub-install.txt。
