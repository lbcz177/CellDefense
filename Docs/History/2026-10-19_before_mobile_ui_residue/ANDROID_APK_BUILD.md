# Android APK 打包与安装试玩

日期：2026-10-19。首个 APK 已成功生成并完成签名、包清单、CRC 与 ARM32/64 原生库验证；未连接手机安装或启动。

交付文件：E:/unity/CellDefense/Builds/Android/CellDefense.apk，版本 0.1.0，约 70 MB（69.79 MiB）。复制到安卓手机后安装；系统如询问安装来源，按自己的设备提示处理。

## 下一次打包

1. 若 Unity 在安装 Android 模块前已经打开，先保存工作并重新启动 Editor，使其识别新模块。
2. 停止 Play，使用 Unity 2022.3.53f1c1 打开当前工程。
3. 点“胞卫战 → Android → 检查构建环境”。
4. 点“胞卫战 → Android → 构建横屏试玩 APK”。
5. 成功后在 Builds/Android/CellDefense.apk 找到新包。下一次覆盖前可自行保留旧包。

构建菜单配置左右横屏、禁止竖屏、IL2CPP、ARMv7 + ARM64、主菜单和五关，输出 Development 试玩 APK。最低 API 23，目标 SDK 为自动选择；本次实际为 35。应用名称 CellDefense、包名 com.celldefense.game、版本 0.1.0。

## 组件和缓存

Android 模块及 SDK、NDK、OpenJDK 已安装在 E 盘现有 Unity 目录。本次命令行构建的 Gradle 缓存与临时文件放在 Logs/AndroidBuild；通过普通 Editor 菜单运行时会使用该进程自己的缓存 / 临时目录，因此不能据此保证 C 盘完全无占用。

构建代码：[CellDefenseAndroidBuild.cs](../Assets/_Project/Editor/CellDefenseAndroidBuild.cs)。当前以首次手机试玩为目标；设备实测和发行包优化另行进行。

详见 [构建证据](Testing/2026-10-19_AndroidApk.md)、[手机源码学习](MOBILE_ANDROID.md)和[投影修复](VIEWPORT_ASPECT.md)。
