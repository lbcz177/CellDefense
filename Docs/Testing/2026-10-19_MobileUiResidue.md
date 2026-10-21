# Android 手机 UI 残影修复与 0.1.1 构建

日期：2026-10-19；Unity 2022.3.53f1c1。

用户提供了 0.1.0 的手机截图：战斗地图上方残留了前一界面的标题 / 边框。本次补充局部视口前的全屏纯色清屏，不修改地图格子、投影比例、触控或玩法。

## 自动化结果

| 验证 | 结果 |
|---|---|
| EditMode | 127/127，0 失败 / 跳过 |
| PlayMode | 32/32，0 失败 / 跳过 |
| 单独运行屏幕像素回归 | 1/1 通过 |
| 仅在隔离副本停用清屏调用 | 按预期 1/1 失败，检测到顶部旧画面像素 |
| 主工程 / 构建副本 C# 摘要 | 96 份一致 |

图形回归使用 D3D11 真实 Game 视图，覆盖五关、1280×720 / 2048×928、三种安全区状态，共 30 组状态，并覆盖暂停时变化和返回主菜单后的清理。测试先用低优先级相机写入洋红色作为旧页面像素，再于世界 HDR 合成后、Overlay UI 之前读回全屏，确认上下和两侧均清理且地图仍可见。停用修复的负向对照在第一关顶部失败；随后恢复源码并运行上述完整回归。

图形截图不含 Overlay UI，不是手机截图。测试在 Logs/CodexTradingTests 隔离副本完成。EditMode XML 通过后 Unity 卡在退出阶段，核对自有进程路径后停止该进程；构建另起进程完成，日志记录正常退出码 0。测试结果以 XML 为证，不把退出清理视为真机验证。

## APK 交付与校验

- 修复包：E:/unity/CellDefense/Builds/Android/CellDefense-0.1.1.apk；CellDefense.apk 同步指向相同内容。
- 大小：73,204,696 字节，69.81 MiB。
- SHA256：2dee1d7d46cc79631d336595728dffde19be3182a3092b06efdb3da791698dc4
- 版本：0.1.1 / versionCode 2；包名 com.celldefense.game。
- Development / IL2CPP，ARMv7 + ARM64，主菜单和五个 OpenField 关卡；最低 API 23，目标 API 35，左右横屏。
- BuildPipeline 和 Gradle 构建成功；ZIP 全部 178 条目 CRC、两种架构的原生 ELF 校验通过；aapt 核对版本与清单。
- apksigner 确认 v1 / v2 签名有效，与 0.1.0 同一证书，版本号递增，可覆盖更新。
- 原包保存在 Builds/Android/CellDefense-0.1.0.apk；原首包证据不改写。

用户已在手机上运行旧包并报告问题；修复包尚未由本机连接设备安装或由用户复测，不能宣称 Android 实机残影已经验收消失。未做设备性能、通关或商店发布。临时 Editor 构建调用脚本已清理，仅保留日志与工具链缓存。

## 证据

[摘要](2026-10-19_MobileUiResidue_summary.json)、[EditMode XML](2026-10-19_MobileUiResidue_editmode.xml)、[PlayMode XML](2026-10-19_MobileUiResidue_playmode.xml)、[单独像素回归](2026-10-19_MobileUiResidue_targeted-display.xml)、[负向对照](2026-10-19_MobileUiResidue_negative-control.xml)、[构建结果](2026-10-19_MobileUiResidue_build.json)、[APK 校验](2026-10-19_MobileUiResidue_verification.json)、[清单](2026-10-19_MobileUiResidue_manifest.txt)、[签名](2026-10-19_MobileUiResidue_signature.txt)、[源码摘要](2026-10-19_MobileUiResidue_sources.json)。

实现见 [修复说明](../MOBILE_UI_RESIDUE_FIX.md)；完整日志保存在 Logs/ViewportResidueValidation 和 Logs/AndroidUiFixBuild。
