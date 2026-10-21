# 胞卫战：开放地图核心防御

Unity 2D 免疫主题塔防学习项目，当前使用 Unity `2022.3.53f1c1`，优先完善 Windows PC 体验。

当前主线已有三张开放地图、每张三轮防御，包含六塔基础能力和混合敌群，仍待完整试玩调节。当前策划案 **v0.13（2026-10-14）** 确认“有限改路、自由布置、通道内承伤”：一种专用塔以独立有限额度改变道路，普通细胞负责输出与免疫协作，在通道内接敌承伤。现有代码仍为全塔挡路，新规则尚未接入；完整方向、待定细节和实现状态见 [策划案](Docs/GAME_DESIGN_FRAMEWORK.md)。

## 当前实现

- 15 × 11 逻辑格、中央核心；第一关从左 / 右 / 上进入，第二关从底部双入口进入并带固定组织障碍。
- 布置阶段选择现有三种塔；不能占用入口或核心，也不能封死入口到核心的道路。
- 手动开始每轮，进攻期关闭建造；同入口敌人共用本轮路径。
- 击杀获得 ATP，敌人抵达核心扣生命；前两轮结束发放轮间资源。
- 第三轮清场且核心存活时胜利，核心生命归零时失败。
- 手绘地图、格子提示、塔虚影和射程预览。
- 已接入原 Victory / Defeat 预制体，提供重新挑战、返回主菜单，以及第一关胜利后进入第二关。

布置 HUD 仍为临时 IMGUI；主菜单开始按钮已改为进入 OpenFieldPrototype，正式 HUD 和暂停列入后续工作。旧 Prototype / Level02 场景继续保留。

## 运行与操作

1. 用对应 Unity 版本打开工程，打开 [OpenFieldPrototype 场景](Assets/_Project/Scenes/OpenFieldPrototype.unity)。
2. 也可先打开 MainMenu，从开始游戏按钮进入新场景。如果检出版本尚无该场景，使用菜单 `Cell Defense > Create Open Field Prototype Scene` 创建。
3. 进入 Play；数字键 1 / 2 / 3 或顶部按钮选塔，鼠标指向格子预览，点击有效格建造。
4. 按空格或 Start assault 开战；前两轮结束后再次布置，手动开始下一轮。
5. 结算后 Again 重开，Return 返回主菜单；第一关胜利后按 Next 进入 [第二关场景](Assets/_Project/Scenes/OpenFieldLevel02.unity)。也可以直接打开第二关场景 Play。

## 技术结构

- OpenFieldPrototypeController：当前输入、建造、回合、出敌和结果 UI 的集中编排。
- OpenFieldGridModel：固定障碍、占用格、从核心开始的 BFS 距离场、封路检查和路径提取。
- OpenFieldPresentation：背景、调试格、建造虚影与射程。
- TowerController、特殊攻击行为、EnemyController、PathFollower 和 ProjectilePool：复用既有战斗模块。
- ScriptableObject 保存塔、敌人和波次配置；EconomyService / LifeService 保存本局状态。

总控制器的职责拆分尚未实施。具体依赖、数据流、生命周期、BFS 分配与跨路线锁敌风险见 [技术架构](Docs/TECHNICAL_ARCHITECTURE.md)。

## 验证与发布状态

用户已试玩并认可开放地图方向；第二关场景、固定障碍和 Next 接线于 2026-10-04 完成静态检查，跨场景和第二关完整 Play 验证仍待完成。本次没有运行构建，也没有取得新场景的完整性能数据。

旧固定路线 v0.1.0 曾有独立 Windows 运行验证记录，该结果不覆盖当前开放地图版本。发布新版本前需重新验证主菜单进入新场景、结算、重开和返回的全流程。

## AI 协作与学习

AI 可完成普通业务样板、接线、文档和排错。开发者负责理解与独立修改核心规则、Unity 编辑器操作、运行验证和面试解释；明确选为手写练习的核心任务先说明变量与生命周期再动手。

不把 AI 实现或计划中的功能直接当作个人已掌握能力，也不把未测量的性能收益写进简历。

## 文档入口

- [文档导航](Docs/README.md)
- [游戏策划](Docs/GAME_DESIGN_FRAMEWORK.md)
- [技术架构](Docs/TECHNICAL_ARCHITECTURE.md)
- [三轮试玩说明](Docs/OPEN_FIELD_THREE_ROUND_PROTOTYPE.md)
- [学习路线](Docs/UNITY_JOB_LEARNING_ROADMAP.md)
- [性能专题](Docs/PERFORMANCE_MANY_ENEMIES.md)
- [美术规范](Docs/ART_DIRECTION.md)
