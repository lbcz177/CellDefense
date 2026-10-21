# 暂停、继续与倍速

日期：2026-10-20；Unity 2022.3.53f1c1，适用于五个 OpenField 关卡。右上暂停、1× / 2× 已接入用户绘制的六张按钮图；暂停界面 v2 的独立空框、标题、返回 / 设置 / 退出按钮继续沿用。原临时界面记录见 [接图前快照](History/2026-10-18_before_pause_art/PAUSE_PLAYBACK.md)，设置开发前状态见 [历史快照](History/2026-10-18_before_settings/PAUSE_PLAYBACK.md)。

## 操作与规则

- 右上角暂停符号打开暂停面板；图片按钮“返回游戏”或 Esc 继续。点击“1× / 2×”在正常与双倍速之间切换，图片表示当前倍率。
- 暂停菜单“退出到主菜单”复用原返回逻辑；“设置”进入总音量 / 静音 / 全屏 / 恢复默认页，修改自动保存。设置页返回或 Esc 先回到暂停菜单，仍保持暂停。见 [设置说明](SETTINGS.md)。右上工具条使用奶油白常态图，悬停 / 按下为青蓝高亮图，不叠加文字或旧矩形底板。
- Esc 在设置页优先返回暂停菜单；其余情况下优先取消出售确认，再关闭建造 / 塔选择；没有选择时暂停，暂停菜单内 Esc 继续。右键保持原取消行为。
- 布置 / 战斗都可以暂停和切换倍率；暂停时禁止建造、出售、开波与改变倍率。HUD 保持可见，面板遮挡地图和其余按钮。
- 暂停设置 Time.timeScale=0，Phase 仍保持 Planning / Assault，保留敌人、路径、接敌、波次索引和生成协程。移动、攻击、动画、子弹、刷怪等待、抗原记忆和状态计时使用游戏时间冻结。
- 继续恢复暂停前选择的 1x / 2x；倍率改变游戏时间流速，不改敌人基础速度、关卡速度配置或攻击数值。换轮保留倍率。
- 胜负后隐藏控制按钮、清除暂停并恢复 1x；重开、下一关、返回菜单和控制器禁用也恢复 1x。终局不能再次暂停或切倍率。
- 继续按钮的点击不会同帧落到地图；按钮点击后清除 EventSystem 选择，避免 Space 同时触发倍率按钮和开波。

## 代码与接线

| 文件 | 职责 |
|---|---|
| Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs | IsPaused、PlaybackSpeed、Pause / Resume / SetPlaybackSpeed / TogglePlaybackSpeed / HandleEscape，状态门控、退出重置与同帧输入保护 |
| Assets/_Project/Scripts/UI/OpenFieldPlaybackControls.cs | 图片工具条 / 美术暂停面板，倍率切图、按钮事件、Sprite Swap、全屏射线遮挡、安全区定位与 HUD 避让 |
| Assets/_Project/Scripts/UI/GameSettings.cs / GameSettingsPanel.cs | 跨场景音量 / 静音 / 桌面显示模式偏好、即时保存与设置页控件 |
| Assets/_Project/Scripts/UI/OpenFieldUiResources.cs 及 Resources/OpenFieldUiResources.asset | 引用六张工具条与八张暂停菜单 Sprite，供五关统一加载；不依赖运行时 AssetDatabase |
| Assets/_Project/Scripts/UI/OpenFieldBattleHud.cs | 按 HasActiveRun 显示，暂停不因 CanBuild=false 隐藏 |
| Assets/_Project/Scripts/Paths/PathFollower.cs | deltaTime 为零时不调用移动 / 接敌门控，防暂停期间新建立接敌 |

五关现有控制器 Awake 自动创建并接线控制组件，Resources 已引用新图片，无需修改场景或手动拖图。不把 IsPaused 合并进波次 Phase，也不把敌人的 assaultCheck 改成 CanBuild，避免暂停时释放接敌关系。

## 美术布局

工具条素材位于 Assets/_Project/Art/Sprites/细胞绘制/战斗UI/暂停与倍速按钮_v1/。按钮采用 128×64、2:1 原生比例，容器 272×72；安全区定位与 HUD 避让沿用现有逻辑。六张 PNG 和现有 GUID 保留，仅调整对应导入设置为 Point、无压缩、无 Mip Maps。倍率变化同时更新常态 / 高亮 / 按下图，鼠标停留在按钮上时也立即显示新倍率。

素材位于 Assets/_Project/Art/Sprites/细胞绘制/暂停界面_v2_2026-10-18/。面板原生 640×800，标题和按钮均 448×112；以左上角为原点，标题 (96,112)，返回游戏 (96,304)，设置 (96,440)，退出 (96,576)。面板在安全区内等比缩放并留边，保持图片比例，不叠加 TMP 文字；交互界面不直接使用整张组合参考图。

返回 / 设置 / 退出使用未选中图作为 Source Image，Highlighted / Selected / Pressed 使用对应蓝色图，零游戏时间下也可切换。原图、Aseprite 源文件及 .meta 不改写。暂停美术阶段的 Unity 渲染预览保存于同目录 接入验证~/暂停界面_Unity预览_640x480.png，其中设置置灰是当时状态。

## 验证与边界

2026-10-20 工具条图片接入后，现有 PlayMode 回归 32/32 通过；另外运行已有五关按钮测试采集常态 / 高亮截图，1/1 通过，五关 20 条实际 Sprite 状态符合预期。已查看 Unity 渲染，原图和 GUID 保留；详见 [本次接入报告](Testing/2026-10-20_PlaybackToolbar.md)。没有重打 APK 或进行真机验证。

此前 [美术接入报告](Testing/2026-10-18_PauseArtwork.md)：EditMode 103/103、PlayMode 19/19，0 失败 / 跳过；其中“禁用设置”是设置开发前的历史断言。设置开发更新为按钮启用、设置返回和保存验证；最新结果见 [设置测试](Testing/2026-10-19_Settings.md)。此前代码阶段的 103/18 报告保留。

暂停和设置页的渲染检查不等于所有窗口尺寸的观感或五关完整通关验收。手机平台已增加失焦 / 切后台自动暂停，前台返回后手动继续；桌面失焦不自动暂停，跨关记忆倍率仍未提供；音量 / 静音由 GameSettings 控制，已有 CombatHitFeedback 在零时间下暂停自身音效，不代表所有未来音源都有统一暂停管理。

手机输入、横屏与后台生命周期说明见 [Android 适配与源码学习](MOBILE_ANDROID.md)。
