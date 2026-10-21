# Android 横屏适配与源码学习

日期：2026-10-19；Unity 2022.3.53f1c1。

本阶段完成项目内适配代码与编辑器验证。用户选择暂不安装 Android 构建组件，因此尚无 APK，也未进行 Android 真机、性能、系统旋转或安装验收。构建环境恢复后才能验证 IL2CPP、Gradle、SDK/NDK/JDK 与设备兼容性。

## 试玩与构建

1. 在 Unity 中停止 Play，勾选菜单“胞卫战 → Android → 手机布局预览（运行前切换）”。
2. 打开 MainMenu 或任意 OpenField 关卡。Game 视图选择横屏，如 1280×720；开始 Play 后可检查手机布局。该开关跨 Play 的域重载保留，结束后手动关闭即可回到桌面布局。
3. 编辑器预览保留鼠标操作地图和 UGUI 按钮，方便在电脑里试玩。手机运行时走触控分支；手指抬起 / 滑动 / 双指行为由自动化测试调用同一输入入口验证。编辑器鼠标试玩不等于真机触摸验收。
4. 日后为当前 Unity 安装 Android Build Support、SDK & NDK Tools、OpenJDK 后，执行“胞卫战 → Android → 检查构建环境”，再执行“构建横屏试玩 APK”。
5. 输出路径为 `Builds/Android/CellDefense.apk`。仅打包 MainMenu 和五个 OpenField 主线场景，不打入旧 Prototype / Level02。当前尚未生成这个文件。

构建配置：左右横屏自动旋转、禁止竖屏、包名 `com.celldefense.game`、最低 API 23、IL2CPP、ARMv7 + ARM64、Development APK。最终可用目标 SDK 及设备兼容性以实际构建和安装结果为准。当前入口用于本地试玩；商店发布和发行签名另行处理。

## 实现思路

同一套场景和玩法在两个平台运行。手机适配集中在输入、屏幕布局和应用生命周期，塔、敌人、经济、路径、升级和波次继续调用现有代码。

| 需要解决的问题 | 入口与做法 |
|---|---|
| 手机没有鼠标悬停 | 移动端只显示已选格子的预览；抬起手指后选择空格或已有塔 |
| 滑动 / 双指误点 | BoardTapGesture 记录 fingerId 和起点；超阈值移动、取消事件、UI 起始 / 结束和双指都会使这次点选失效 |
| 点击 UI 穿透地图 | 几何命中检查，加 EventSystem.RaycastAll / GraphicRaycaster；同一帧不再走模拟鼠标分支 |
| 刘海和系统手势区域 | 使用 Screen.safeArea；主菜单和结算把图片及其按钮一起装进 MobileSafeFrame |
| 宽屏拉伸地图 | 手机相机保留网格的世界距离和比例，在安全区内预留 HUD 行；剩余空间留在两侧 |
| 按钮难按 | 手机布局增大暂停 / 倍速、取消、升级、出售及关闭按钮的点击区；结算按钮独立重排 |
| 手机没有窗口 / 全屏切换需求 | 设置中隐藏桌面全屏行和编辑器专用提示，共用音量、静音及默认值 |
| 切后台后战斗继续 | OnApplicationPause(true) 或失焦调用现有 Pause；返回前台不自动继续，保留原 1x / 2x |

UGUI 的 Button、Slider、Toggle、ScrollRect 本身处理触摸事件，因此设置拉条和选关滑动复用现有组件；地图输入则自行处理手势和 UI 门控。

## 有没有手机版专属 API

本次主要使用 Unity 已封装的跨平台 API。其中触摸、方向与安全区对手机尤其重要，并非都属于 Android 独占接口。没有编写 Java / Kotlin，也没有引入 Android 原生插件。

| API | 本项目的用途 | 官方说明 |
|---|---|---|
| Input.touchCount / Input.GetTouch | 读取当前手指与 Touch 数据 | [GetTouch](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Input.GetTouch.html) |
| Touch.fingerId / phase / position | 区分同一手指；读取按下、移动、抬起、系统取消及屏幕坐标 | [Touch](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Touch.html) |
| Screen.safeArea | 获取可放按钮和重要内容的屏幕像素区域 | [safeArea](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Screen-safeArea.html) |
| Screen.orientation / autorotateTo… | 左右横屏，禁止竖屏 | [orientation](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Screen-orientation.html) |
| Application.isMobilePlatform | 手机走适配路径；编辑器开关可预览同一逻辑 | [isMobilePlatform](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-isMobilePlatform.html) |
| OnApplicationPause / OnApplicationFocus | 收到后台 / 失焦通知时暂停 | [OnApplicationPause](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html) |
| Application.targetFrameRate | 手机目标 60 帧；这只是目标，不代表实测达到 60 FPS | [targetFrameRate](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-targetFrameRate.html) |
| PlayerSettings.Android / BuildPipeline.BuildPlayer | 编辑器端配置架构、SDK 与 APK 构建 | [Android 构建](https://docs.unity3d.com/cn/2022.3/Manual/android-BuildProcess.html) |

## 建议按这个顺序学习代码

1. `Assets/_Project/Scripts/OpenField/BoardTapGesture.cs`：先读小型状态机。它不依赖场景，适合理解“按下不执行、抬起才执行”和取消状态为什么要保留到所有手指离开。
2. `OpenFieldPrototypeController.ProcessBoardTouch`：手势成立后，屏幕坐标 → 相机世界坐标 → 网格坐标 → SelectCell。最后接回现有建造或塔详情，不复制一套手机战斗逻辑。
3. `MobileDisplay.cs` 与 `MobileSafeFrame.cs`：先理解像素坐标和 Canvas 局部坐标，再看统一缩放及平移。图片和按钮同时放进容器，点击位置会跟随图片。
4. `OpenFieldPrototypeController.ConfigureMapViewport`：比较桌面分支和手机分支，理解正交相机的 orthographicSize、aspect 与视口矩形。
5. `OnApplicationPause / OnApplicationFocus / Pause / Resume`：系统通知只触发已有暂停规则。为什么回到前台仍需手动继续、为什么不修改 Phase，都是值得学习的状态管理。
6. `CellDefenseAndroidBuild.cs`：编辑器菜单、PlayerSettings、场景清单与构建失败反馈。它属于编辑器工具，不进入手机运行时。

触控调用链：

`Update → GetTouch → ProcessBoardTouch → UI 门控 → BoardTapGesture.Process → ScreenToWorldPoint → WorldToCell → SelectCell → 建造菜单 / 塔详情`

对应验证代码为 `MobileInputTests.cs` 和 `MobilePlayModeTests.cs`。测试覆盖规则和场景交互，但不能取代 Android 真机触控、来电、旋转、发热、性能或适配验收。

## 当前边界

仅适配 MainMenu 与五个 OpenField 主线关卡；旧固定路线参考场景继续保留原行为。未加入双指缩放、拖动地图、iOS 构建、原生 Android 插件、设备云测或新的存档系统。

Android 组件暂不安装是用户本阶段选择。之后可从现有构建入口继续；本次没有更新 Notion。

验证结果：EditMode 126/126、PlayMode 30/30；三种横屏尺寸另独立 1/1。详见 [完整报告](Testing/2026-10-19_MobileAndroid.md)。
