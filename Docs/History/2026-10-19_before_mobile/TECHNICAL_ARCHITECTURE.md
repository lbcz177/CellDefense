# 《胞卫战》技术架构：当前实现

> 架构基线：v0.13 · 2026-10-19；配套 [策划 v0.19](GAME_DESIGN_FRAMEWORK.md)、[实现与验证状态](PROJECT_STATUS.md)。
> 当前已接入暂停 / 倍速逻辑与用户 v2 暂停美术，通过共享 Resources 引用八张 Sprite，未改场景、原图或 .meta。此前临时 UI 阶段见 [接图前快照](History/2026-10-18_before_pause_art/TECHNICAL_ARCHITECTURE.md)，v0.9 及更早原文仍保留。

## 1. 场景与配置边界

Unity 2022.3.53f1c1。当前五个 OpenField 场景共用开放地图编排，MainMenuController 创建五关选择 UI，StartGame 打开选关。旧 Prototype / Level02 使用 GameplayCompositionRoot、GameFlowController、BuildController、WaveController、HudController；新场景不并挂旧流程控制器。

场景保存地图尺寸、核心 / 入口 / 固定障碍、初始与轮间经济、生命、速度倍率、壁垒累计额度、退款比例及资源引用。TowerDefinition / EnemyDefinition / WaveDefinition / WaveSequenceDefinition 保存共享静态配置；当前生命、ATP、目标、冷却、抗原记忆和阶段保存在运行实例。三至五关共用 EnemyRoster03_WaveSequence，调整它会影响三关；第一、二关分别引用 EnemyRoster01 / 02。

## 2. 实际职责

| 模块 | 当前职责 |
|---|---|
| MainMenuController | 设置 / 退出透明入口、QuitRequested 防重复与 Save / Quit；独立 Canvas 复用 GameSettingsPanel；与选关互斥，底层门控、焦点恢复和动态 UI 清理 |
| OpenFieldPrototypeController | 组合根、输入、建造与出售事务、细胞注册、波次 / 结果、敌人池、动态路径、信号邻接；仍为集中编排 |
| OpenFieldGridModel | 规则类；occupied 建造占位、routeBlocked 壁垒阻挡、blocked 地形、BFS 距离场、入口 / 在场路径可达性、Revision |
| OpenFieldPresentation | 地图、调试格、虚影 / 射程、当前及候选路线 |
| OpenFieldBuildMenu / OpenFieldTowerMenu | 建造、属性 / 射程、升级费用 / 效果 / 资格、二次确认出售、遮挡与定位；交易规则由控制器执行 |
| OpenFieldBattleHud / HudSpriteNumber | 场景 HUD 预制体的数据绑定、安全区布局、仅数值变化时更新数字 |
| OpenFieldEntranceWaveButtons | 入口投影、可开波时显隐、同一开波意图 |
| OpenFieldPlaybackControls | 文字工具条 / v2 暂停独立图层、安全区、Sprite Swap、全屏射线遮挡；管理暂停菜单 / 设置页切换；CanvasScaler 更新后渲染前校正布局 |
| GameSettings / GameSettingsPanel | 首场景前读取共享偏好，AudioListener 总音量 / 静音、桌面 Player 显示模式与 PlayerPrefs 保存；五张声音 / 拉条图通过 Resources 引用，固定填充裁切、滑块内槽对齐；设置页不改变战斗时间 |
| TowerController / TowerAttackBehaviour | 索敌、冷却、动画释放；默认投射物或至多一个策略组件定义效果 |
| TowerCombatState / EnemyContactCombat | 细胞生命 / 容量；路径段内接敌、停留、伤害、溢出和释放 |
| EnemyController / Health / 状态组件 | 入场版本、伤害 / 护盾 / 护甲 / 标记、吞噬、互斥退出与状态重置 |
| BossPhaseController / 三类 BossAbilityBehaviour | 最低到达血量触发阶段、预告、有限召唤、支援、抗原变化 |
| WaypointPath / PathFollower | 路点距离缓存、移动、控制倍率、接敌门控、保持当前段的 Reroute |
| Projectile / ProjectilePool | 跟踪、命中回调、伤害 / 标记、版本检查与复用 |
| BuildSlot / AntigenSignalFeedback | 动态格子与塔引用、一跳抗原传递、可复用多条连线 |
| EconomyService / LifeService / TowerEconomyRules | 本局 ATP / 核心生命；新旧场景共享价格与实付退款计算 |
| PlanningConfigWindow / Validation | 编辑塔 / 敌人 / 波次 / 关卡；SerializedObject、Undo、定向保存与只读校验 |

GridModel 虽不继承 MonoBehaviour，仍依赖 Unity Vector2Int。EnemyRegistry、SignalNetwork、空间桶、专用 OpenFieldLevelDefinition、集中 Tick 调度和 ECS 尚未实现。

## 3. 初始化与对象生命周期

Awake 验证三波序列、至少一种塔、恰好一种改路塔、子弹池、中文字体、按钮 / HUD 资源、额度和退款比例；选择相机并创建 GridModel，设置棋盘视口映射，初始化经济 / 生命 / 子弹池。随后创建 Presentation、建造 / 塔菜单、敌人状态及入口按钮，绑定场景 HUD；缺少 HUD 场景引用时可实例化默认 Prefab。UI Resources 提供按钮与 HUD 素材回退，运行时不依赖 AssetDatabase。

敌人池按 Prefab 保存闲置栈，首波总预热预算最多 16，每种 Prefab 最多保留 128 个闲置实例；不足时继续创建，128 不是活跃或总实例上限。控制器在生成前订阅 Exited / SummonRequested，入场递增 SpawnVersion，重置生命、路线、抗原、控制、防御、阶段与接敌状态。正常退出后移除活跃集合、取消订阅再回池；终局先取消订阅，直接回收剩余单位，无奖励。

塔锁敌、动画待释放目标、子弹和范围目标快照校验 SpawnVersion；同一 Unity 对象再次入场视为新一代，延迟引用不能直接沿用。召唤请求同时核对母体版本、活跃资格和当前有效路径，子单位使用 InitializeAt 继承中段进度。

OnDisable 隐藏菜单 / HUD / 入口及预览。场景重载恢复 timeScale；Presentation 和特殊效果组件各自释放创建的材质 / 网格 / 视觉对象。当前终局未统一集中释放所有在飞投射物；完整幂等清理仍是待整理职责，场景卸载不能代替生命周期契约。

## 4. 建造、出售与动态寻路

GridModel 从核心进行四邻接 BFS，均等代价格生成距离场；TryGetPath 每步取距离减 1 的邻格，顺序为上、右、下、左。一次 BFS 时间 / 辅助空间 O(宽×高)，路径为共享 WaypointPath，不是每敌人 A*。

普通细胞占 occupied 但不写 routeBlocked；壁垒检查额度并临时阻挡候选格，所有入口可达才允许提交。Assault 还检查敌人当前 / 下一格和全部在场下一节点。候选检查在 finally 恢复临时状态。控制器缓存候选壁垒格、网格 Revision 和在场下一节点集合；BFS 仍会分配数组 / Queue，不能宣称零分配或只在成功建造时运行。

建造链：CanBuild / 配置 / 格子 / ATP 检查 → TrySpend → 创建 BuildSlot / 塔 → Initialize / Occupy → Grid.TryOccupy → 注册 TowerCombatState / 邻接 → 成功壁垒消耗累计额度 → 必要时重建路线。异常回滚 ATP、占用、实例和已消费额度。

出售链：面板记录待售塔实例 → 确认再次核对当前选中实例 → 计算已付费用的退款 → 立即禁用并移除细胞 / 接敌 / 占位 / 邻接 → 壁垒重建路径 → 加退款。死亡复用同一清理入口但不退款。TowerEconomyRules 用 long 计算 (BuildCostPaid + UpgradeCostPaid) × percent / 100；五关配置 percent=50，壁垒累计额度不返还。

RebuildAssaultRoutes 先准备所有入口替换路径及按在场下一节点共享的剩余路径，再调用 PathFollower.Reroute，最后清理旧路径。替换路径以重复下一节点保留当前路段，不重新 Initialize，因此不改变生命、控制、SpawnVersion 或 BeforeMove 接敌门控。当前没有拆墙、追击或拥挤避让。

## 5. 波次与结算

Phase 为 Planning / Assault / Victory / Defeat。CanBuild 在前两种状态且未暂停时成立，包含建造、升级与出售。CanStartWave 同样要求未暂停，且处于 Planning，或处于 Assault 且本波计划出完、仍有下一波。

新增 IsPaused 与 PlaybackSpeed（1 / 2），不向 Phase 增加暂停值；HasActiveRun 表示 Planning / Assault，CanBuild / CanStartWave 另要求未暂停。Pause 将 timeScale 置零并清空选择，Resume 恢复原倍率，同帧屏蔽地图输入；HUD 按 HasActiveRun 保持可见。Awake、终局和场景切换入口 / OnDisable 重置暂停和倍率。assaultCheck 保持原 Phase 条件，暂停不释放接敌；PathFollower 在 deltaTime=0 时不执行 BeforeMove，避免新接敌。UI 自动创建并接线，八张 v2 素材由共享 Resources 引用，状态逻辑与表现分开，接口见 [暂停与倍速](PAUSE_PLAYBACK.md)。

正常清场：出完且 activeEnemies 为空 → 最后一波则胜利，否则 AdvanceRound 发一次轮间 ATP → Planning。提前开波：保留旧敌人和路径 → AdvanceRound 发一次 ATP → 清零 allEnemiesSpawned → 开下一波，Phase 继续 Assault。最终清场包含全部召唤物。核心归零检查先于完成检查。

WaveDefinition 支持混合 groups，每组可配敌人、count、burstSize 和 afterDelay；组内 / 组间 WaitForSeconds 按波次复用，组切换与 Boss 预告可单独等待。旧 WaveController 仍是固定路线顺序生成，不自动获得新分组规则。

敌人 hasResolved 保证一次退出，控制器 HashSet.Remove 再防重复结算。Killed 发 EnemyDefinition.KillReward；Leaked 扣 LeakDamage，非固定 1；Cleared 不发奖励或扣生命。核心动作期间不可锁敌，CompleteCoreAttack 动画事件或 0.75 秒超时触发一次泄露；塔释放等待上限为 0.6 秒，两者不同。

## 6. 攻击、伤害和协作

每轮攻击开始前查询 Physics2D.OverlapCircle，复用 List（初始容量 64，允许扩容），按敌人中心精确检查射程。策略 CanTarget 先排除无效候选，再比较剩余实际路程最短；GetTargetPriority 仅处理并列，最终按到塔距离。

CanAttack 是额外就绪条件，没有目标不消耗新冷却。默认塔立即出手或记录 pendingAttackTarget / SpawnVersion 进入动画，ReleaseAttack 校验活跃、可攻击和版本，再执行 Attack。释放不重新检查射程，也不换承诺目标；策略仍可拒绝失效资格。每塔最多一个特殊策略，不能同时开启近卫采样开关。

伤害统一进入 EnemyController.TakeDamage：酸伤先破壳 → 护甲倍率与标记倍率 → 护盾吸收 → 剩余扣 Health → Died → Exited → 场景奖励 / 回池。TryEngulf 使用专用资格及 Health 死亡链，不直接 Destroy。标记子弹允许零伤害并快照发射抗原，命中时核对，防病毒变异后误标记。

近卫命中将当前抗原传给半径 3 格的一跳抗体邻居；每次建造 / 移除重建邻接，复杂度 O(塔数²)。BuildSlot 首次实际传递时懒创建 AntigenSignalFeedback，多条连线复用。抗体记忆 8 秒、标记 4 秒、易伤 15%；状态禁用 / 重入清除。酸区禁用后清理，延迟命中用施法版本防重新生成旧区；神经先快照完整连锁，防致命首击后对象复用误伤。

ProjectilePool 仍由 Inspector 指定统一模板，TowerDefinition.ProjectilePrefab 不自动选择池；视觉可覆盖。共享 TowerController 的旧单次升级逻辑不代表五关已有升级 UI。

## 7. 工具、表现与后续职责

策划配置工具已存在，无须再规划“首版不做编辑器”。它编辑共享 Definition 与场景字段，Play 模式禁用修改，Undo 与保存分开；SaveAssetIfDirty / SaveScene 定向保存。特殊效果细节仍由 Prefab 组件配置。第四、五关共享波次需先复制再独立调节。

当前 UGUI HUD、建造菜单、塔菜单与入口按钮已拆出表现类；OpenFieldPrototypeController 仍集中承担输入 / 建造 / 回合 / 生成 / 路线。后续可按真实改动拆分这些规则，统一清理、场景配置与结果 UI 引用。拟议类名不能当作已有架构。

技术债包括集中编排、候选 BFS 分配、O(塔数²) 邻接、固定池模板、结果按钮路径耦合、未统一的终局投射物清理和图片 / 网格独立来源。没有 Profiler 前后数据，当前没有空间桶、分频调度、Jobs / Burst / ECS、Addressables 或热更新。只在需求和测量支持时引入。

## 8. 验证与决策记录

最新测试：[退出与升级 117/26](Testing/2026-10-19_QuitUpgrade.md)，涵盖新旧回归与 Windows 退出链路。单独构建仅含主菜单的验证 Player，非完整五关发布；未执行 Profiler，桌面系统全屏切换、全部窗口比例、五关试玩和平衡仍待验证。旧阶段测试按原结果保留。

- OF-01 保留：等代价格、多入口共同终点使用 BFS 距离场与共享路点。
- OF-03 已被替代：原“仅布置期修改占用”是历史决策；当前允许 Assault 建造 / 出售并重建剩余路线。
- OF-04 保留并补齐：BuildSlot 复用格子引用与一跳信号，已增加连线反馈。
- OF-06 保留：旧场景独立编排，不与开放流程同时结算。
- OF-07 保留：敌人 / 子弹闲置池，容量语义和代际校验明确；收益待测。
- 2026-10-15：占位 / 通行分离、细胞承伤与有限拦截。
- 2026-10-16：UGUI HUD、提前开波、战中改路及六塔反馈。
- 2026-10-17：购售共用实付退款规则。
- 2026-10-18：v0.9 文档归并，历史原文保留。
- 2026-10-18 后续：v0.10，暂停与倍率独立于波次状态，临时 UI / 输入保护 / 生命周期重置，实际测试通过。
- 2026-10-18 美术接入：通过共享 Resources 组装八张 v2 图片，设置未实现故置灰；原暂停 / 倍速接口保持，图形回归和渲染检查通过。
- 2026-10-19：v0.11，新增共享 GameSettings 与独立设置页，启动时应用偏好，修改后保存；Esc 优先关闭设置，恢复不改变倍率。修正初次显示时 CanvasScaler 更新先后导致的小面板问题。
- 2026-10-19 随后：Resources 引用五张声音 / 拉条图片，416 像素内槽裁切固定填充，滑块沿同范围移动，喇叭作为静音 Toggle；五关统一接入，未改写原图或 .meta。
- 2026-10-19 设置字图：Resources 引用九张当前使用的字图，设置页移除 TMP 标签，百分比以三个可复用数字 Image 和独立百分号右对齐组合；字图不拦截射线，按钮动作保持。见 [最新测试](Testing/2026-10-19_SettingsText.md)。
- 2026-10-19 主菜单设置：v0.12，MainMenuController 复用 GameSettingsPanel，独立根 Canvas 避开主菜单 CanvasGroup 门控；与选关互斥，禁用关闭 / 销毁清理，偏好沿用同一 GameSettings 实例。
- 2026-10-19 退出与升级：v0.13，开放控制器加入 GetUpgradeCost / CanUpgradeAt / TryUpgradeAt，复用 TowerController 单次伤害升级和实付退款；不重新 Initialize。主菜单 QuitGame 保存后调用 Application.Quit，Editor 只记录请求。
