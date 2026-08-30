# 《细胞防卫战：伤口入侵》文档导航

> 文档基线：2026-08-30  
> 当前 Unity 版本：2022.3.53f1c1  
> 当前阶段：新工程首个 PC 纵向切片；M1–M3 已完成，下一步为 M4 ATP 与最小战斗闭环

这组文档既是游戏的设计基线，也是开发者的学习地图。任何实现与本文档冲突时，先记录证据，再修改相应文档；不要让过时的文档反过来强迫代码。

当前事实的优先级为：实际可运行代码与场景证据 > 本地 Markdown 当前状态段落 > Notion 镜像与开发日志 > 2026-08-10 旧工程审计 > 原始 GDD。原始 PDF 和旧工程记录保留用于追溯设计演进，不能作为新工程的直接实现要求。

## 1. 单一事实来源

| 主题 | 权威文档 | 负责回答的问题 |
|---|---|---|
| 产品、玩法、关卡、数值与范围 | [GAME_DESIGN_FRAMEWORK.md](GAME_DESIGN_FRAMEWORK.md) | 游戏为什么好玩、做哪些内容、三关如何成立 |
| 模块、职责、数据流与迁移 | [TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md) | 代码应该如何组织、谁依赖谁、怎样从当前原型渐进迁移 |
| 大量敌人与性能验收 | [PERFORMANCE_MANY_ENEMIES.md](PERFORMANCE_MANY_ENEMIES.md) | 怎样避免卡顿、如何测量、优化到什么程度才算完成 |
| 求职知识与学习分工 | [UNITY_JOB_LEARNING_ROADMAP.md](UNITY_JOB_LEARNING_ROADMAP.md) | 哪些知识亲自写、哪些可交给 AI、怎样形成面试证据 |
| 统一画风与资产交付规范 | [ART_DIRECTION.md](ART_DIRECTION.md) | 所有 AI 图片如何保持同一世界、尺寸、颜色和可读性 |
| 塔的图片提示词 | [Prompts/TOWERS_IMAGE_PROMPTS.md](Prompts/TOWERS_IMAGE_PROMPTS.md) | 六种防御塔的概念图、游戏精灵与升级分支如何生成 |
| 怪物的图片提示词 | [Prompts/ENEMIES_IMAGE_PROMPTS.md](Prompts/ENEMIES_IMAGE_PROMPTS.md) | 四类核心敌人与 Boss 阶段如何生成 |
| 地图的图片提示词 | [Prompts/MAPS_IMAGE_PROMPTS.md](Prompts/MAPS_IMAGE_PROMPTS.md) | 三关背景、路径、塔位和宣传场景如何生成 |
| 子弹与特效提示词 | [Prompts/PROJECTILES_VFX_IMAGE_PROMPTS.md](Prompts/PROJECTILES_VFX_IMAGE_PROMPTS.md) | 子弹、命中反馈、状态与技能特效如何生成 |

## 2. 已锁定的项目决策

- 2D 横屏俯视塔防，首发目标为 Windows PC 与横屏手机。
- 只能在预定塔位建造；后续关卡可出现不同塔位类型。
- 最小完整作品为三关，每关引入一种地图结构和一组新知识。
- 六种防御塔，首通教学逐步指定，之后战前从已解锁塔中六选四。
- 波次自动开始，也可提前开始换取奖励；倍速后续加入。
- 敌人泄露扣除生命，生命归零失败；标准/挑战难度不允许续关。
- 三种资源：ATP、局内生物酶、局外免疫记忆点。
- 科普通过战斗提示、知识卡和图鉴呈现，并明确区分真实知识与玩法改编。
- 美术先用占位，核心玩法稳定后再使用权利清晰的 AI 素材或寻找美术合作。

## 3. 当前代码事实与目标架构的关系

旧 Unity 工程已经删除，当前项目于 2026-08-28 从新的 2D Core 工程开始。现在不存在需要迁移的 `TowerPlacer`、`BaseTower`、`GridManager`、旧 `.asset` 或双建塔路径。

1. 自有资源统一放在 `Assets/_Project/`；当前代码按 Core、Paths、Enemies、Combat、Building、Towers、Waves、Economy、UI 和 Data 分组。
2. `WaypointPath + PathFollower` 已完成：路径由有序 `Transform[]` 配置，敌人使用帧率无关移动并在终点发布一次事件。
3. `Health + EnemyController + WaveController` 已实现死亡、泄露、清场三种互斥退出，并正确销毁敌人 GameObject。
4. `BuildSlot + BuildController + TowerDefinition` 已通过三个固定塔位的一塔建造测试；重复点击和重新启用控制器不会重复建塔或重复订阅。
5. 当前使用显式 Inspector 引用和 `Initialize` 传递依赖，没有引入场景全局查找、全局 EventBus 或大型 DI 容器。
6. 当前性能数据为空白。只有完整战斗循环可运行后才建立 Profiler 基线；目标表不能提前写成已达成结果。
7. 固定路线和固定塔位继续使用可测量的 MonoBehaviour 实现；A*、对象池、分频和空间桶必须由玩法需要或 Profiler 证据触发。

## 4. 推荐阅读顺序

1. 先读总策划的产品承诺、三关范围和核心循环。
2. 再读技术架构的模块边界、数据流与迁移顺序。
3. 开始每一个里程碑前，查求职路线中该知识点的掌握标准。
4. 敌人数量开始上升前，先建立性能基线，不等到卡顿后才补测量。
5. 玩法稳定后读美术规范，再分别生成塔、敌人、地图和特效；不要直接混用不同批次图片。

## 5. 文档维护规则

- 玩法数字优先写入总策划；性能数字只写入性能文档；不要复制出多个真相。
- 模块职责变化必须同步技术架构的 ADR 或变更日志。
- 每完成一个开发里程碑，更新求职路线中的掌握等级和面试证据链接。
- AI 提示词修改时，先更新统一美术规范，再修改单类提示词。
- 所有性能结论必须附设备、构建类型、场景规模和采样数据；未测量的数字只能标为目标。
- 任何医学说明都必须标明“真实知识”“简化表达”或“玩法改编”。

## 6. 本轮文档验收

- [x] 代码与现有文档事实盘点
- [x] 总策划吸引力、原创机制、组合玩法与试玩门槛补齐
- [x] 目标代码框架、迁移风险与当前数据资产问题补齐
- [x] 大量敌人性能预算、方案、Profiler 流程与压力测试补齐
- [x] 当前 Unity 求职技能证据、学习责任和作品集交付物补齐
- [x] 统一美术规范补齐
- [x] 塔、敌人、地图、子弹/特效分别拥有独立提示词文档
- [x] 交叉链接、术语、文件范围和验收标准通过最终检查
