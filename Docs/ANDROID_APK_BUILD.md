# Android APK 打包与安装试玩

> 当前本地上架候选为 Android 0.1.2 / Code 3，57.01 MiB，安装名“胞卫战”、游戏图标、非 Development；已有30秒运行视频、3张截图和宣传图。TapTap草稿已按用户确认完成上传、绑定与填写，资料体检无缺失；待用户检查，未提审或发布。见 [检查页](Publishing/Review/2026-10-20/index.html)和[证据](Testing/2026-10-20_StorePreparation.md)。

日期：2026-10-19。此前 0.1.1 修复包已生成并完成签名、包清单、CRC 与 ARM32/64 原生库验证。用户已在旧版手机上反馈 UI 残影，修复包待同设备复测。

交付文件：E:/unity/CellDefense/Builds/Android/CellDefense-0.1.1.apk，版本 0.1.1 / versionCode 2，约 70 MB（69.81 MiB）；CellDefense.apk 为相同内容。包名和签名沿用 0.1.0，复制到安卓手机后直接更新安装。旧包另存为 CellDefense-0.1.0.apk。

## 下一次打包

1. 若 Unity 在安装 Android 模块前已经打开，先保存工作并重新启动 Editor，使其识别新模块。
2. 停止 Play，使用 Unity 2022.3.53f1c1 打开当前工程。
3. 点“胞卫战 → Android → 检查构建环境”。
4. 点“胞卫战 → Android → 构建横屏试玩 APK”。
5. 成功后在 Builds/Android/CellDefense.apk 找到新包。下一次覆盖前可自行保留旧包。

构建菜单配置左右横屏、禁止竖屏、IL2CPP、ARMv7 + ARM64、主菜单和五关，输出 Development 试玩 APK。最低 API 23，目标 SDK 为自动选择；本次实际为 35。应用名称 CellDefense、包名 com.celldefense.game、版本 0.1.1 / versionCode 2。

## 组件和缓存

Android 模块及 SDK、NDK、OpenJDK 已安装在 E 盘现有 Unity 目录。本次命令行构建的 Gradle 缓存与临时文件放在 Logs/AndroidBuild；通过普通 Editor 菜单运行时会使用该进程自己的缓存 / 临时目录，因此不能据此保证 C 盘完全无占用。

构建代码：[CellDefenseAndroidBuild.cs](../Assets/_Project/Editor/CellDefenseAndroidBuild.cs)。当前以首次手机试玩为目标；设备实测和发行包优化另行进行。

详见 [构建证据](Testing/2026-10-19_AndroidApk.md)、[手机源码学习](MOBILE_ANDROID.md)和[投影修复](VIEWPORT_ASPECT.md)。

本次修复了局部战斗视口外残留旧页面像素，见 [修复说明](MOBILE_UI_RESIDUE_FIX.md)和[0.1.1 验证记录](Testing/2026-10-19_MobileUiResidue.md)。首次构建报告和修复前快照继续保留。

## 上架候选入口

使用“胞卫战 → Android → 构建上架候选 APK”生成非Development包，输出Builds/Android/CellDefense-版本号.apk；安装名统一“胞卫战”，使用现有1024游戏图标。原“构建横屏试玩APK”入口仍用于开发试玩。

当前候选0.1.2 / Code 3，包名与签名沿用；旧0.1.0和0.1.1文件保留。打包后按实际生成文件校验，不把构建成功等同于平台已上传、已审核或已上线。
