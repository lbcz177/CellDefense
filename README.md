# 细胞防卫战：伤口入侵

> Unity 2D 塔防学习项目
>
> Unity `2022.3.53f1c1` · Windows PC
>
> 当前里程碑：`v0.1.0` 首个可玩纵向切片

《细胞防卫战：伤口入侵》是一款以免疫系统为主题的 2D 横屏塔防原型。当前版本使用一条固定路线、一类敌人、一座塔和少量固定塔位，完整跑通建造、攻击、奖励、泄露、胜负、暂停与重新开始。

## 当前可玩内容

- 敌人沿固定路径移动，并以 `Killed / Leaked / Cleared` 三种互斥原因退出。
- 固定塔位点击建造，ATP 不足或塔位占用时拒绝建造。
- 防御塔选择最近目标，按攻击间隔发射投射物并造成伤害。
- 击杀敌人获得 ATP；敌人泄露扣除基地生命。
- `WaveDefinition` 驱动敌人数量、生成间隔与敌人配置。
- 游戏流程包含 `Ready / Running / Paused / Victory / Defeat`。
- HUD 显示 ATP、基地生命和当前状态。
- 支持暂停、恢复、胜利、失败和终局后重新开始。
- 已生成并运行验证 Windows x64 独立版本。

## 操作方式

- 点击空塔位：建造当前测试塔。
- 点击“暂停／继续”：切换暂停状态。
- 胜利或失败后点击“重新开始”：重载当前关卡。
- `Alt + F4`：退出 Windows 版本。

## 运行项目

1. 使用 Unity `2022.3.53f1c1` 打开项目。
2. 打开 `Assets/_Project/Scenes/Prototype.unity`。
3. 进入 Play Mode。

## 构建 Windows 版本

1. 打开 `File → Build Settings`。
2. 选择 `PC, Mac & Linux Standalone`。
3. 设置 `Target Platform = Windows`、`Architecture = x86_64`。
4. 确认 `Prototype.unity` 是 Scenes In Build 中第一个启用场景。
5. 构建到 `Builds/Windows/CellDefense.exe`。

`Builds/` 不进入 Git。发布时应压缩完整 Windows 构建目录，不能只分发 exe，并排除 `CellDefense_BurstDebugInformation_DoNotShip`。

当前 Windows zip 仅用于本地里程碑验证，尚未作为公开 Release 分发。

## 技术结构

- **组合根**：`GameplayCompositionRoot` 统一创建普通 C# 服务并注入场景组件。
- **静态配置**：`EnemyDefinition`、`TowerDefinition`、`WaveDefinition` 使用 ScriptableObject。
- **运行时服务**：`EconomyService` 与 `LifeService` 不依赖 GameObject 生命周期。
- **游戏流程**：`GameFlowController` 统一管理合法状态转换和终局优先级。
- **事件驱动 UI**：HUD 订阅资源、生命和状态事件；重新启用时主动读取当前快照。
- **事务式建造**：建造失败时回滚 ATP，终局和暂停状态拒绝操作。
- **生命周期清理**：组件停用或销毁时取消事件与按钮订阅。

## 已完成验证

- 全部敌人退出且生命大于 0时进入胜利。
- 生命归零时失败优先于波次完成。
- 暂停期间敌人停止，建造被拒绝；恢复后继续运行。
- 胜利和失败后均不能继续建塔。
- 胜利和失败结算后可以重新开始。
- Windows Player 中完成 `Ready → Running → Victory → 重载 → Ready → Running`。
- 最新 Windows Player 日志中字体缺字警告为 0、运行时异常为 0。

独立版本日志位于：

```text
%USERPROFILE%\AppData\LocalLow\ichen\CellDefense\Player.log
```

## 当前限制

- 仅有一类敌人、一座测试塔和一段短波次。
- 仅支持固定塔位和鼠标点击，尚未加入升级、出售、技能和完整菜单。
- 暂未加入对象池、压力测试和正式 Profiler 对比数据。
- 美术和 UI 仍以原型可读性为主。
- Android、iOS 和多分辨率适配尚未完成。
- 当前中文字体在公开分发前仍需确认授权范围。

## AI 协作说明

本项目使用 AI 辅助完成部分样板、事件接线、机械重构、文档与排错。开发者负责核心规则取舍、关键方法实现、Unity 场景配置、运行验证和面试解释。

简历与面试只描述已经完成并能独立解释、修改和验证的内容，不把计划功能写成已实现。

## 文档导航

| 主题 | 文档 |
|---|---|
| 游戏设计与范围 | [Docs/GAME_DESIGN_FRAMEWORK.md](Docs/GAME_DESIGN_FRAMEWORK.md) |
| 技术架构 | [Docs/TECHNICAL_ARCHITECTURE.md](Docs/TECHNICAL_ARCHITECTURE.md) |
| 求职学习路线 | [Docs/UNITY_JOB_LEARNING_ROADMAP.md](Docs/UNITY_JOB_LEARNING_ROADMAP.md) |
| 敌人性能专项 | [Docs/PERFORMANCE_MANY_ENEMIES.md](Docs/PERFORMANCE_MANY_ENEMIES.md) |
| 美术方向 | [Docs/ART_DIRECTION.md](Docs/ART_DIRECTION.md) |
| 文档总入口 | [Docs/README.md](Docs/README.md) |

## 下一步

1. 为 `v0.1.0` 补充截图和短演示视频。
2. 增加一个真正不同的塔或锁敌策略，练习需求变更与策略边界。
3. 在敌人规模足够后实现对象池，并用 Profiler 记录优化前后证据。
