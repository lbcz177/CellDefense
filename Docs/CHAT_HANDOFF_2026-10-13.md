# 历史交接：2026-10-13 及后续追加记录

> 2026-10-18 已整理当前基线，请新对话先读 [当前状态](PROJECT_STATUS.md)、[策划 v0.15](GAME_DESIGN_FRAMEWORK.md)与[架构 v0.9](TECHNICAL_ARCHITECTURE.md)。下方原文按各自日期保留；旧 HEAD / 远端、两关三塔、仅布置期建造、无承伤 / 出售、未运行测试等说法不代表当前工程。后续不再向本历史交接堆叠当前状态。

## 历史原文

# 胞卫战对话交接（2026-10-13）

2026-10-17 复用原版购塔 / 出售：按用户要求，原 Level02 与五个开放关卡共用 `TowerEconomyRules` 的配置价格校验和实付退款算法；六张场景退款比例均为 50%，向下取整，读取 BuildCostPaid + UpgradeCostPaid，不随后续配置改价变化。五关已有塔点击后显示属性和射程，出售先确认；取消保留选择，死亡 / 终局关闭，旧确认不会误卖同格新塔。出售清占位、信号邻接和接敌引用；壁垒出售更新入口 / 在场路径，累计建造额度不返还。策划工具新增关卡退款比例字段及 0–100 校验。原 Level02 的固定塔位流程保留，新地图继续自由格建造。83 源码静态诊断无错误；独立副本 EditMode 95/95、PlayMode 14/14 通过，0 失败 / 跳过，用户主工程未被测试器操作、未构建或使用 Profiler。见 [购售接入](OPEN_FIELD_TRADING.md) 与 [测试记录](Testing/2026-10-17_Trading.md)。

2026-10-16 六塔基础补齐（优先于旧抗体选敌 / 无信号反馈描述）：六塔基础伤害、减速、酸区、连锁均已有实现，本次补抗体 `CanTarget` 有效候选筛选，未知抗原前排不再挡住后方有效目标，没有候选不耗冷却；仍在有效候选中按剩余路程选前排，动画释放不换承诺目标。开放 BuildSlot 自动提供一跳采样连线，可复用多连接；抗体 Prefab 接入现有记忆环，记忆到期 / 禁用清理，配置显示名统一为“抗体标记塔”。神经塔增加本次连锁路径反馈，巨噬增加初始化参数校验和消化重置。新测试 11 项 EditMode + 8 项 PlayMode；当前总计 78/78 EditMode、9/9 PlayMode 通过，0 失败 / 跳过，未构建或运行 Profiler。升级分支 / 群落 / 炎症仍是待定扩展，原素材与 .meta 保留。见 [六塔能力与测试](TOWER_ABILITIES_STATUS.md)。

2026-10-16 战中建塔 / HUD 预制体更新（优先于此前“仅布置期建塔”和运行时创建 HUD 的描述）：按用户要求，Planning / Assault 均可点击空位选塔和建造，终局禁建；普通塔不改路，壁垒建成后更新入口及在场敌人的剩余路线，保持当前路段与生命 / 速度 / SpawnVersion / 接敌状态。不能压住敌人当前或下一格，也不能封住入口或在场敌人的路线。HUD 已做成 `Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab`，含 Canvas、框、图标、数字槽和壁垒额度文字，五关均放置关联实例并绑定控制器；框从 640 缩到 512 UI 单位（缩小 20%）。新脚本只绑定数据和布局，不运行时创建图形。

本次用户明确要求执行测试，已在 `Temp/CodexLiveBuildTests` 独立项目副本使用 Unity 2022.3.53f1c1 运行：EditMode 67/67、PlayMode 1/1 全部通过，0 失败 / 跳过。覆盖新功能 22 项 EditMode 回归和真实第一关开波 / 战中建塔 / 改路 / 召唤 / HUD 冒烟，以及原有 45 项回归。77 个源码静态诊断 0 错误。未操作用户已打开的工程、未构建；无图形批处理不代表目视验收或性能实测。报告及源文件哈希见 [测试记录](Testing/2026-10-16_LiveBuildHUD.md)。

2026-10-16 图片 HUD 更新（优先于下文旧工具栏描述）：五关统一由 OpenFieldBattleHud 组合新绘制的三栏空框、ATP / 生命 / 波次图标标签与独立数字，固定在安全区域左上角，实际数值改变时更新。原英文 IMGUI 状态栏与 OnGUI 已移除；壁垒额度提示在新框下方，选塔框和入口按钮避让实际 HUD 区域。Resources 默认配置引用原素材，PNG / .meta 保留。74 个源码静态诊断 0 错误，资源引用 / 数字顺序 / 素材哈希核对通过；未运行 Unity、Play、Runner、构建或 Profiler。详见 [图片 HUD 接入](BATTLE_HUD_ART_IMPLEMENTATION.md)。

2026-10-16 按钮显隐调整：入口按钮仅在 CanStartWave 为 true 时显示；当前波出怪期间隐藏整组图案 / 文字，不显示禁用态，也不拦截地图输入。出怪结束且还有下一波时重新显示，末轮启动和终局继续隐藏。开波条件及其余玩法保持。

2026-10-16 开波按钮报错修复：Editor.log 的当前异常为 `Assign the entrance wave button artwork`；磁盘五关已绑定，但运行临时场景的新字段为空。现新增 Resources/OpenFieldUiResources.asset 默认配置，OpenFieldUiResources 在场景引用缺失时自动读取同一原图，兼容编辑器 / Player 并保留显式覆盖；不依赖 Editor AssetDatabase，不复制 / 修改原图及 .meta。73 个源码静态诊断通过，波次 / 映射 / 素材测试现 18 个用例，均未执行；需用户等待导入后重新 Play 确认原异常消失。

2026-10-16 按钮素材 / 铺满画面补充：入口开波 Button 已使用刚绘制的圆形细胞 Sprite，五关和创建工具均绑定；文字放图案下方，UI 保持图片比例并限制在安全区域。五关初始化时将完整 15×11 棋盘通过正交相机映射铺满游戏视口，允许非等比拉伸，背景 / 单位 / 路径 / 输入共用同一映射；世界坐标和战斗参数不变。原 PNG / .meta 保留，未运行 Unity 或构建。72 个源码静态诊断 0 错误；波次测试现共 16 个用例，新增四种画面比例的边界 / 格子往返测试，均待用户执行。详见 [入口按钮和地图显示](ENTRANCE_WAVE_BUTTONS.md)。

2026-10-16 开波交互更新：五关每个实际出怪口已有 UGUI 开波按钮，所有入口控制同一轮；顶部外置 Start 已移除，Space 复用同一逻辑。用户选择“本波出怪结束后，有残敌时才可提前开下一波”；出怪中禁用，末轮启动及胜负后隐藏。提前开波保留旧敌人 / 召唤物及路径，下一轮复用路径；轮间 ATP 每轮只发一次，建塔仍限布置阶段。72 个源码静态诊断 0 错误，新增 12 个波次流程测试用例尚未执行；未运行 Unity、Play、Runner、构建或 Profiler。详见 [入口开波按钮与验证](ENTRANCE_WAVE_BUTTONS.md)。

2026-10-15 选关补充：MainMenu 的“开始游戏”和右下角“选择关卡”均打开选关界面，显示五关名称及地图预览，允许直接进入任一关。返回 / Esc 关闭，选关时禁用背景主菜单交互。MainMenuController 负责运行时 UGUI 创建，场景已绑定 Canvas、中文字体和五张 Sprite，不新增解锁限制；关卡加载恢复 Time.timeScale=1。此前 62 个运行 / 相关编辑器源文件静态诊断通过，本次入口调整未运行 Unity 或构建，需用户核对显示和交互。

2026-10-15 五关地图更新（优先于下文历史快照）：按用户“基础玩法按当前，只改地图”的要求，第一至第五关已引用新绘制背景，并配置 1 / 2 / 2 / 2 / 3 个入口、图中核心及膜边 / 组织岛阻挡格。主线现为 OpenFieldPrototype → OpenFieldLevel02 → OpenFieldLevel03 → OpenFieldLevel04 → OpenFieldLevel05。第四、五关复用第三关战斗配置和 EnemyRoster03_WaveSequence，第三关末尾真菌 Boss 保留；未改教学解锁或波次节奏。策划配置工具与第一关创建工具同步，旧素材与 .meta 保留。空场连通性、资产引用和 Editor 静态诊断通过；未运行 Unity、Play、Runner、构建或 Profiler，目视对齐与流程仍待用户验证。详见 [五关地图接入记录](LevelPlanning/FIRST_FIVE_MAPS_IMPLEMENTATION_2026-10-15.md)。

2026-10-15 v0.14 更新（优先于下文历史快照）：用户已指定纤维蛋白壁垒为唯一改路塔，每关累计限建 6 座、仍花 30 ATP；普通细胞生命 100 / 容量 1，巨噬 250 / 2，敌人每秒伤害 10、Boss 25。占位 / 通行分离、承伤 / 溢出通行 / 死亡清理、路线预览与额度反馈均已接入，第一、二关增加壁垒选择。保留 15% 调速与第三关末尾真菌 Boss：倍率 0.85 / 0.6375 / 0.6375，第三关末轮 73。运行 / 测试源码静态检查通过，现有 EnemyCombatTests 共 21 项尚未执行；不运行 Unity、Play、Runner、构建或 Profiler。详见 [v0.14 接入与用户验证](LIMITED_ROUTE_DEFENSE_IMPLEMENTATION.md)。

本文件接续 2026-09-30 历史交接，按当前磁盘代码核对。新对话先阅读本文件、Git 状态及 Docs/tutorial.md。

2026-10-14 敌人实现更新（高于下文旧快照）：用户已批准 Docs/ENEMY_DESIGN.md 并授权代码实现。八种敌人的独立配置 / Prefab、护盾 / 破壳 / 治疗 / 吞噬处决、Boss控制抗性、最低生命门槛阶段、有限召唤、病毒抗原变化及混合波次均已落盘；三关引用 EnemyRoster01 / 02 / 03_WaveSequence。计划数量保持原值，另有最多 6 个孢子 / 8 个复制体；子单位从母体当前路径进度出生并影响清场。新Actor参数及模块见 Docs/ENEMY_IMPLEMENTATION.md。保留旧敌人、旧波次、所有素材 / .meta 和既有修改。

Boss生命 / 盾条 / 阶段 / 预告与首次种类提示已接入，其他种类连续帧动画和专用召唤物图仍未制作。已新增 14 个 EditMode 边界测试，尚未运行；本地 Roslyn C#9 语法 / 类型诊断与资产 / 九轮数量核对通过，没有 Emit、Unity、Play、测试Runner、构建、Profiler、commit 或 push。用户继续负责 Unity 导入、测试和试玩；不能把静态检查当作运行验收。

2026-10-14 后续补充（优先于下文旧快照）：主线已接为 OpenFieldPrototype → OpenFieldLevel02 → OpenFieldLevel03，各三轮。第三关使用已有左右入口 / 双竖短墙背景，20 个固定障碍格，初始 ATP 250；三波沿用 30 / 48 / 72、每组 6 只和速度倍率 0.75，使用独立波次与敌人配置。第三关接齐六塔，新增 FibrinTower / AcidTower / NeuralPulseTower 的 Prefab 和 Definition；第三关独立敌人 Prefab 有 EnemySlowState / EnemyStunState。第一、二关保留原三塔。抗体塔为零伤害辅助，标记带 15% 易伤，仍需近卫提供抗原。

三关建造交互已改为点击有效空位→UGUI 图标 / 价格选择框→悬停预览→点击选项建造；右键 / Esc / 取消 / 开战关闭。数字键仅在选中空位后建造。OpenFieldBuildMenu 使用已有中文字体，场景生成工具也已补字体引用。旧素材和 .meta 保留；新资产GUID、组件接线、场景链及地图路径通过只读静态核对，尚未运行 Unity、Play、构建或 Profiler。资源导入、障碍视觉对齐、按钮不穿透、六塔与三关流程需用户验收，步骤见 Docs/OPEN_FIELD_THREE_ROUND_PROTOTYPE.md。

## 1. 项目、Git 与协作约定

- 工程：E:\unity\CellDefense，Unity 2022.3.53f1c1，当前优先 PC 和 Unity 初级 / 客户端求职作品。
- 当前分支 codex/open-field-prototype，HEAD 01b43c5；本地跟踪状态显示比 origin/codex/open-field-prototype 超前 1 个提交，另有大量未提交 / 未跟踪代码、场景、动画、素材与文档。没有在本次 fetch、commit 或 push，因此跟踪状态不等于最新远端核验。不要沿用旧交接中的 main 推送结论。
- 保存用户现有修改；现有素材及 .meta 不覆盖或删除。生成 / 修改图片最终保存到 Assets/_Project/Art/Sprites/细胞绘制/，按用途分目录，不交付到桌面。
- 用户希望自己做 Inspector / Prefab / Animator / Play / Profiler 操作。此前明确要求暂不构建；未再次要求时不要默认跑 Player 构建或把测试当作下一步。
- 普通业务代码可由 AI 完成，但要讲清职责和验证；对象池、关键状态机等选为手写任务时先列实际变量、类型、用途、读写和初始化 / 重置时机，再由用户实现。已有 AI 代码不等于用户已独立掌握。
- 不把个人答题、评分或能力等级写入 Notion。用户最新希望自己写下一座塔，AI 应先指导接口和 Review，避免代写整套新塔。

## 2. 当前主线玩法与场景

- MainMenu.StartGame → OpenFieldPrototype → 胜利 Next → OpenFieldLevel02。两张图各三轮；旧 Prototype / Level02 保留为固定路线参考。
- Planning 阶段选格建塔，手动开始 Assault；出完且活跃敌人清空后结束一轮。核心归零失败优先，第三轮清场且存活胜利。
- GridModel 四邻接 BFS 从共同核心生成距离场；开战时为各入口创建共享 WaypointPath。塔占格、固定障碍不可走，建塔必须保持所有入口可达，进攻阶段不修改占用。
- 第一关 15×11，核心 (7,5)，入口左 / 右 / 上。第二关底部两入口 (2,0)、(12,0)，七个固定阻挡格。
- 当前三种可建塔：近卫采样、抗体标记、巨噬直接伤害 / 吞噬。近卫命中后经 BuildSlot 一跳传播抗原；B 塔记忆 8 秒、命中标记 4 秒；巨噬普通吞噬阈值 20、标记阈值 50、消化 3 秒。
- 胜负 UI 复用原 Prefab；规划工具栏仍是 IMGUI。菜单与数字键只支持前三种塔，追加第四个配置不会自动显示。

## 3. 本对话已经落盘的改动

### 密集敌群与调速

| 第二关轮次 | 数量 | 每组 | 组内间隔 | 组间间隔 |
|---|---:|---:|---:|---:|
| 1 | 30 | 6 | 0.12 秒 | 2.2 秒 |
| 2 | 48 | 6 | 0.10 秒 | 1.9 秒 |
| 3 | 72 | 6 | 0.08 秒 | 1.6 秒 |

- 总数 150 是累计生成量，未测同屏峰值。
- 第二关 initialATP=200、betweenRoundATP=60、initialCoreLife=20，enemySpeedMultiplier=0.75；敌人基础速度 2，有效速度每秒 1.5 世界单位。
- 第一关仍为 5 / 7 / 10 只，倍率 1、ATP 100 / 40、核心生命 10。
- WaveDefinition 增加 BurstSize / BurstInterval；组大小 1 保留原生成行为，旧 WaveController 不读取新分组字段。

### 第一轮性能代码

- OpenFieldPrototypeController 内按 Prefab 保存闲置敌人栈，首波预热最多 16，每种 Prefab 保留最多 128 个闲置实例。128 不限制活跃实例，池不足仍 Instantiate；超出闲置上限才 Destroy。
- 正常死亡 / 泄露及终局清场取消 Exited 订阅后回池；敌人 Initialize 重置生命和路径，OnEnable 重置免疫标记。
- EnemyController.SpawnVersion 每次 Initialize 递增；塔目标、待动画释放目标和 Projectile 都记录版本，防止旧引用攻击复用后的新敌人。
- 塔范围查询使用可复用 List<Collider2D> 的 Physics2D.OverlapCircle，容量不足时可扩容，不截断目标。保留中心距离精确过滤，修复碰撞体边缘与射程圈的差异。
- 每波复用 WaitForSeconds；Marker 为 CellDefense.Enemy.Spawn、CellDefense.Targeting.Query，另有原投射物 Spawn / Return Marker。没有 CPU、GC、GPU 或帧率提升实测。

### 攻击最前面的敌人

- 现有三个 TowerDefinition.targetingMode 均为 FarthestAlongPath（序列化值 1）。现在按剩余实际路径距离最短定义前排，每轮攻击开始前重新选择，避免一直追打旧目标。
- WaypointPath 缓存每点到终点的距离；PathFollower.RemainingDistance 是当前位置到下一点距离加其后缀距离；EnemyController.RemainingPathDistance 转发。多入口不同路线长度可直接比较。
- 前排距离并列时才比较 GetTargetPriority（抗体未标记 / 巨噬可吞噬），再按到塔距离破同分。
- 动画前摇期间保留已承诺目标；ReleaseAttack 验证目标活跃、可攻击与 SpawnVersion，失效则取消，不在该次事件中换目标。

## 4. 当前磁盘中的动画与打击反馈

- 近卫 Tower.prefab 的 Idle / Attack 和葡萄球菌移动 / 抵达核心攻击已接入；用户反馈动画感觉跑通。
- 近卫启用 releaseOnAnimationEvent，动画释放帧调用 ReleaseAttack；等待上限 0.6 秒。漏事件或晚于上限会取消本次攻击。
- 敌人抵达核心可触发 Attack 动画，CompleteCoreAttack 或 0.6 秒超时结算一次 Leaked；动作阶段 CanBeTargeted=false。
- 当前另有 CombatHitFeedback、SpriteHitFlash、素材安装器 CombatAnimationSampleInstaller 和显式验证器 CombatGameplayLevelVerifier。资料入口为 Docs/Art/AnimationIntegration/2026-10-12/gameplay/README.md。不要默认运行资源安装器以免覆盖用户接线。
- PPU 越大，Sprite 显示越小；调整原 PNG 导入比例并 Apply，动画各帧需统一尺寸、Pivot 和 PPU。保留新旧图集及 .meta。

## 5. 讨论过但尚未批准或实现的玩法

- 用户担心：塔阻挡格子导致敌人绕路，近战作用不足；普通塔如果是无风险墙壁，巨噬仅拦两只意义有限。
- 已讨论路边短程攻击、有限拦截、塔有血量、允许封路、敌人绕太远时拆塔，以及巨噬承伤 / 群攻 / 吞噬回血等方向。
- 这些都是候选设计。用户尚未确认实施哪个版本；当前塔无承伤与拦截状态，敌人不拆塔，仍必须留路。
- 下一次若继续此话题，先决定迷宫绕路与前后排防线的侧重，以及建筑被摧毁后是否释放格子 / 刷新在场敌人路径。不要把 AI 推荐直接落地成已确认策划。

## 6. 最新问题与用户当前学习目标

- IDE 报 CS1061：EnemyController 未包含 SpawnVersion。源码第 19 行实际已经定义，csproj 引用正确文件，已查项目脚本目录只有一个类定义，Unity 日志没有对应编译错误。推断为编辑器缓存 / 工作区 / 未保存内容不同步，尚未确认重载后的结果。
- 用户操作建议：F12 核对路径与属性，检查未保存内容，VS Code Developer: Reload Window；必要时 Unity External Tools → Regenerate project files，再从 Unity 打开脚本。不要为消除 IDE 旧诊断删除 SpawnVersion 保护。
- 用户希望理解当前塔逻辑并自行写下一座。Docs/tutorial.md 已展开 T01：TryBuildAt → Initialize → Update / FindTarget → 动画或立即出手 → Attack 策略 → 子弹 / 直接伤害 → Enemy.TakeDamage → 死亡退出 / 回池。
- 新塔继承 TowerAttackBehaviour，只实现一次攻击的效果。CanAttack 是额外就绪条件；damage 已含升级；GetTargetPriority 在前排模式只破同分。只改数值 / 外观通常只需新 Definition 与 Prefab。
- 每塔最多一个特殊策略。复制近卫再加策略时关闭 samplesAntigenOnHit；复制特殊塔时移除旧策略。
- ProjectilePool 使用场景中的固定模板，Definition.ProjectilePrefab 不自动切换实际子弹。直接 Destroy 敌人会跳过奖励 / 退出计数；伤害应走 EnemyController.TakeDamage。延迟或范围技能注意版本、失效目标、碰撞体去重及重置。

## 7. 验证证据与下一步

- 最新前排选敌代码完成静态检查，尚无该规则的完整 Play 回归；本次未运行 Unity、Player 构建或 Profiler。
- 工作区既有 level-validation.json 记录第二关动画战斗 passed=true、17 次发射、16 次命中、5 次核心攻击。报告生成于 2026-10-12 12:04，TowerController 最新修改在当天 17:36；报告不能覆盖最新选敌、完整三轮或性能。它是已有验证副本产物，不是本次新运行。
- 用户此前验证过胜负 UI，动画也反馈感觉跑通；不要扩展为所有最新场景按钮和边界均已验收。
- 优先核对 IDE 红线刷新 → 用户决定下一座塔机制 → AI 说明相关字段与接口 → 用户实现 → AI Review / 接线 → 用户 Play 验证。
- 密集敌群的后续优化先采 Profiler 基线，再决定锁敌分频、错峰、空间桶或移动缓存。Jobs / Burst / ECS 仍未接入，不能写成已取得优化效果。
- 文档已更新为策划 v0.10、架构 v0.5、性能记录 v1.3；Docs/README.md 为导航。Notion 项目总览、策划、架构、性能和学习路线同步本次快照，下方旧内容保留为历史。

Notion 同步已回读核对，原文保留：[项目总览](https://app.notion.com/p/3b8c4f9516208114a5bfc964cc2a5ed2)、[策划](https://app.notion.com/p/3b8c4f9516208182bdf1f2b106c7ba75)、[架构](https://app.notion.com/p/3b8c4f95162081d88119fc7a3b5da58f)、[性能](https://app.notion.com/p/3b8c4f95162081bb92f7f4c33714edae)、[学习路线](https://app.notion.com/p/3b8c4f9516208129b301f4d8d999106c)。

## 8. 新对话开场模板

请先阅读 E:/unity/CellDefense/Docs/CHAT_HANDOFF_2026-10-13.md、Docs/tutorial.md 和当前 Git 状态。当前是开放地图三轮核心防御，已有两张图、三种塔、第二关密集波次、敌人池、动画事件及剩余路径距离前排选敌。塔承伤、近战拦截、允许封路和拆塔仍是未确认方案，先讨论再实施。我想自行写下一座塔，请先按当前源码讲清相关字段、类型、职责和初始化 / 重置时机，给最小骨架，再让我写核心效果并帮我 Review。暂不构建，不把旧验证报告当作最新锁敌或性能验收，保留现有素材及 .meta。
