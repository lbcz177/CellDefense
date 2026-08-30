# 《细胞防卫战：伤口入侵》

> 2D 横屏俯视塔防学习项目  
> Unity `2022.3.53f1c1` · 开发中  
> 文档基线：2026-08-30

《细胞防卫战：伤口入侵》是一款以免疫系统为主题的 2D 塔防原型。玩家在预定塔位部署免疫细胞，抵御病原体入侵，并通过战斗提示、知识卡与图鉴了解相关知识。

## 项目状态

- 当前阶段：从零重构后的首个 PC 纵向切片。
- 当前进度：M1–M3 已完成；敌人生命与退出原因、固定塔位及一塔建造均通过 Play Mode 验证。
- 下一里程碑：M4 接入 ATP、最小锁敌与攻击、短波次和胜负闭环。
- 目标平台：Windows PC、Android 与 iOS 横屏设备。
- 最小完整范围：三个关卡、六种防御塔，战前从已解锁塔中六选四。
- 经济系统：ATP、局内生物酶、局外免疫记忆点。
- 美术策略：玩法验证阶段使用占位资产，稳定后替换为权利清晰的正式素材。

## 文档导航

| 主题 | 权威文档 | 负责回答的问题 |
|---|---|---|
| 产品、玩法、关卡、数值与范围 | [游戏设计框架](Docs/GAME_DESIGN_FRAMEWORK.md) | 游戏为什么好玩、做哪些内容、三关如何成立 |
| 模块、职责、数据流与迁移 | [技术架构](Docs/TECHNICAL_ARCHITECTURE.md) | 代码如何组织、谁依赖谁、怎样从当前原型渐进迁移 |
| 大量敌人与性能验收 | [敌人性能专项](Docs/PERFORMANCE_MANY_ENEMIES.md) | 怎样避免卡顿、如何测量、优化到什么程度 |
| 求职知识与学习分工 | [Unity 学习路线](Docs/UNITY_JOB_LEARNING_ROADMAP.md) | 哪些知识亲自写、哪些可交给 AI、怎样形成面试证据 |
| 统一画风与资产交付 | [美术方向](Docs/ART_DIRECTION.md) | AI 图片如何保持世界观、尺寸、颜色与可读性 |
| 防御塔图片提示词 | [防御塔 Prompts](Docs/Prompts/TOWERS_IMAGE_PROMPTS.md) | 六种防御塔的概念图、精灵与升级分支 |
| 敌人图片提示词 | [敌人 Prompts](Docs/Prompts/ENEMIES_IMAGE_PROMPTS.md) | 四类核心敌人与 Boss 阶段 |
| 地图图片提示词 | [地图 Prompts](Docs/Prompts/MAPS_IMAGE_PROMPTS.md) | 三关背景、路径、塔位与宣传场景 |
| 子弹与特效提示词 | [特效 Prompts](Docs/Prompts/PROJECTILES_VFX_IMAGE_PROMPTS.md) | 子弹、命中反馈、状态与技能特效 |

完整的文档阅读顺序和维护规则见 [Docs/README.md](Docs/README.md)。

## 核心设计决策

- 只能在预定塔位建造，后续关卡可引入不同塔位类型。
- 波次自动开始，也可提前开始换取奖励；倍速功能后续加入。
- 敌人泄露扣除生命，生命归零失败；标准和挑战难度不允许续关。
- 科普内容明确区分“真实知识”“简化表达”和“玩法改编”。

## 当前技术关注点

1. `Health + EnemyController` 已实现 `Killed / Leaked / Cleared` 互斥退出，并由 `TryExit` 保证单个敌人只结算一次。
2. `BuildSlot + BuildController + TowerDefinition` 已形成免费建造闭环；`EconomyService`、塔攻击、正式波次、胜负与 HUD 仍是下一阶段范围。
3. 首个纵向切片只使用一座塔和少量固定塔位；完整六塔、三关和免疫反应链属于后续完成范围。
4. 敌人和防御塔增多后，再根据 Profiler 证据逐步引入注册表、分频调度、对象池和空间划分。
5. 固定路线和固定塔位优先使用可测量、可解释的 MonoBehaviour 方案，不为“架构高级”提前引入 A*、DOTS 或大型依赖注入框架。

原始 `cell_defense_gdd.pdf` 只作为初始玩法提案和数值来源；当前玩法规则以 `Docs/GAME_DESIGN_FRAMEWORK.md` 为准，技术实现以 `Docs/TECHNICAL_ARCHITECTURE.md` 和实际代码为准。

## 文档维护原则

- 玩法数字优先写入游戏设计文档，性能数字只写入性能文档。
- 模块职责变化必须同步技术架构的 ADR 或变更日志。
- 所有性能结论必须附设备、构建类型、场景规模与采样数据。
- 任何医学说明都必须标明“真实知识”“简化表达”或“玩法改编”。
