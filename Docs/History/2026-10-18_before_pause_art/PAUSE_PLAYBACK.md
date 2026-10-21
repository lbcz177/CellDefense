# 暂停、继续与倍速

日期：2026-10-18；Unity 2022.3.53f1c1，适用于五个 OpenField 关卡。按用户要求先完成代码；当前为临时文字按钮，暂停页面美术由用户稍后提供，未生成或接入图片。

## 操作与规则

- 右上角“暂停”打开暂停面板；“继续游戏”或 Esc 继续。点击“1x / 2x”在正常与双倍速之间切换。
- Esc 优先取消出售确认，再关闭建造 / 塔选择；没有选择时暂停，暂停时 Esc 继续。右键保持原取消行为。
- 布置 / 战斗都可以暂停和切换倍率；暂停时禁止建造、出售、开波与改变倍率。HUD 保持可见，面板遮挡地图和其余按钮。
- 暂停设置 Time.timeScale=0，Phase 仍保持 Planning / Assault，保留敌人、路径、接敌、波次索引和生成协程。移动、攻击、动画、子弹、刷怪等待、抗原记忆和状态计时使用游戏时间冻结。
- 继续恢复暂停前选择的 1x / 2x；倍率改变游戏时间流速，不改敌人基础速度、关卡速度配置或攻击数值。换轮保留倍率。
- 胜负后隐藏控制按钮、清除暂停并恢复 1x；重开、下一关、返回菜单和控制器禁用也恢复 1x。终局不能再次暂停或切倍率。
- 继续按钮的点击不会同帧落到地图；临时按钮点击后清除 EventSystem 选择，避免 Space 同时触发倍率按钮和开波。

## 代码与接线

| 文件 | 职责 |
|---|---|
| Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs | IsPaused、PlaybackSpeed、Pause / Resume / SetPlaybackSpeed / TogglePlaybackSpeed / HandleEscape，状态门控、退出重置与同帧输入保护 |
| Assets/_Project/Scripts/UI/OpenFieldPlaybackControls.cs | 临时 UGUI 工具条 / 暂停面板，按钮事件、全屏射线遮挡、安全区定位与 HUD 避让 |
| Assets/_Project/Scripts/UI/OpenFieldBattleHud.cs | 按 HasActiveRun 显示，暂停不因 CanBuild=false 隐藏 |
| Assets/_Project/Scripts/Paths/PathFollower.cs | deltaTime 为零时不调用移动 / 接敌门控，防暂停期间新建立接敌 |

五关现有控制器 Awake 自动创建并接线临时控制组件，无需修改场景或拖新资源。测试汇总见 [实际报告](Testing/2026-10-18_Playback.md)。后续美术只需替换 OpenFieldPlaybackControls 的表现，复用控制器公开方法；不把 IsPaused 合并进波次 Phase，也不把敌人的 assaultCheck 改成 CanBuild，避免暂停时释放接敌关系。

## 验证与边界

独立副本实际运行 EditMode 103/103、PlayMode 18/18，0 失败 / 跳过；新增 8 项 EditMode 和 4 项 PlayMode，其余为既有回归。六个改动 / 新增源码及测试与副本 SHA256 一致。无 Player 构建或 Profiler。

测试验证规则、真实五关按钮、时间冻结、倍速与场景切换；没有验收最终暂停美术、所有窗口尺寸的观感或完整五关通关。暂未增加音频暂停、设置、自动失焦暂停和跨关记忆倍率。
