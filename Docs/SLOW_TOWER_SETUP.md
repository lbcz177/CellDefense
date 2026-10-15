# 纤维蛋白壁垒：减速塔接线

2026-10-14 后续更新：第三关已接线 FibrinTower.prefab / FibrinTowerDefinition.asset，第三关独立敌人已有 EnemySlowState；第一、二关仍使用原三塔。三关现在点击有效空格后弹出选择框。下文手动接线步骤用于检查或自定义配置，无需重复创建第三关资产；运行效果仍待用户验证。详见 [三关试玩说明](OPEN_FIELD_THREE_ROUND_PROTOTYPE.md)。

2026-10-14。Unity 2022.3.53f1c1，分支 codex/open-field-prototype。

用户先实现敌人减速状态与塔扫描初稿，随后授权 AI 完成剩余代码。当前完成的是脚本与菜单入口；本次没有运行 Unity、Play、构建或 Profiler，没有修改 Prefab、Scene、配置资产、图片及现有 .meta。下方接线和验证由用户在 Unity 完成。

## 1. 已完成的代码

- FibrinSlowAura 继承 TowerAttackBehaviour，通过 CanAttack=false 关闭普通射击，由自己的 Update 扫描所有圈内敌人。
- 默认每 0.1 秒扫描一次，刷新 40% 减速，有效期 0.25 秒。查询复用可扩容 List，识别子对象碰撞体，按敌人去重，并检查活跃状态、可攻击状态和敌人中心距离。
- EnemySlowState 每个来源独立计时，取仍有效来源中最强的减速；相同来源刷新自己的记录，不相乘。启用、禁用及 EnemyController.Initialize 都会清空旧效果。
- PathFollower 读取可选的 EnemySlowState，速度为基础速度 × 关卡倍率 × 减速倍率。没有或禁用该组件的旧敌人仍按原速度移动。
- 开放场景支持第四个按钮和数字键 4；后续酸化 / 神经逻辑已将入口扩到六项。按钮与开战按钮按窗口宽度换行，鼠标排除范围同步调整。
- 扫描间隔字段从 scanlnterval 改为 scanInterval，并用 FormerlySerializedAs 保留原 Inspector 数值。

## 2. 敌人 Prefab

1. 打开实际由 EnemyDefinition 引用的敌人 Prefab；当前普通敌人为 Assets/_Project/Prefabs/Enemies/Enemy.prefab。
2. 在同时挂有 EnemyController 和 PathFollower 的对象上添加 EnemySlowState，并保持启用。
3. 确认用于查询的 Collider2D 位于 Enemy 层。当前项目 Enemy 层为第 3 层；子对象碰撞体也要检查自己的 Layer。
4. 保存 Prefab，不删除已有组件、素材或 .meta。

移动与入场重置的代码已经接好，无需修改 EnemyDefinition 或关卡速度。缺少 EnemySlowState 时，光环会报告一次配置错误并跳过该敌人，不会自动在运行时添加组件。

## 3. 新塔 Prefab

1. 复制合适的现有塔 Prefab，另存为 FibrinTower.prefab 等新名字，保留原 Prefab。
2. 保留 TowerController、显示组件；移除复制带来的 AntibodyAttackBehaviour 或 PhagocyteAttackBehaviour，添加唯一的 FibrinSlowAura。
3. TowerController：Enemy Layer Mask 选择 Enemy，关闭 Samples Antigen On Hit。纯光环不依赖 ReleaseAttack 动画事件；可关闭 Release On Animation Event。
4. FibrinSlowAura 按下表配置。Enemy Layer Mask 是光环自身的查询配置；可选 Enemy，留空时在策略 Initialize 中继承 TowerController 的敌人层。
5. Sprite 可使用现有纤维蛋白壁垒素材。保存 Prefab。

| FibrinSlowAura 字段 | 初始值 | 含义 |
| --- | --- | --- |
| Slow Fraction | 0.4 | 减速 40%，剩余速度倍率 0.6 |
| Scan Interval | 0.1 | 游戏时间中的查询间隔，秒 |
| Refresh Duration | 0.25 | 最近一次刷新后有效多久，秒；必须大于查询间隔 |
| Enemy Layer Mask | Enemy | 参与查询的敌人碰撞体层 |

光环配置现在由 TowerController.Initialize 调用策略 Initialize 时校验。参数不合法时进入既有建造回滚，修改后重新进入 Play。Inspector 的范围 / 最小值属性不能代替运行校验。

## 4. 新塔配置与两张地图

1. 创建新的 TowerDefinition 资产，放在 Assets/_Project/Data/Towers/，不要覆盖已有配置。
2. Display Name 填“纤维蛋白壁垒”；Prefab 指向新塔；Build Icon 使用已有图标或 Sprite；Attack Range 可先填 2.5。
3. Build Cost 填你选择的正整数。旧六塔策划的价格不是当前开放地图平衡结果，不在此次代码中锁定。
4. 减速策略 RequiresDamage=false，可将 Damage 填 0，已有正值也会被忽略；CurrentDamage 始终为 0。Attack Interval 仍需为正，可填 1 作兼容配置，它不决定扫描频率。普通伤害升级也不会自动增强减速。
5. Projectile Prefab 可沿用已有引用；光环不会取出或发射子弹。场景中已有 ProjectilePool 仍保留，通用建造初始化需要它。
6. 在 OpenFieldPrototype 和 OpenFieldLevel02 的 OpenFieldPrototypeController 上，把 Tower Definitions 扩为 4 个，将新配置放入索引 3（第四项）。保留前三项顺序，保存两张场景。

第四项接上后，规划期可用按钮或数字键 4 选择。塔仍占格并遵守所有入口可达的规则。

## 5. 用户 Play 验证

以下是待验证的预期，尚未记录为运行通过：

| 操作 / 场景 | 预期 |
| --- | --- |
| 第二关敌人进入 40% 光环 | 速度由 1.5 降到 0.9 世界单位 / 秒 |
| 离圈或关闭光环 | 最后一次刷新后约 0.25 秒恢复 1.5，可能相差一帧 |
| 两座 40% 光环重叠 | 仍是 40%，不变成连续相乘 |
| 55% 与 40% 重叠，离开 55% 范围 | 强来源到期后切回仍被刷新的 40%；随后全部到期恢复 |
| 减速中的敌人死亡并回池再出生 | 新入场没有旧减速，SpawnVersion 原有保护保留 |
| 圈内超过 10 个碰撞体 | 不受旧数组长度 10 的限制；同敌人多碰撞体只刷新一次 |
| 敵人仅碰撞体边缘入圈、中心在外 | 不施加效果，与射程预览的中心距离约定一致 |
| 塔建造完成 | 只产生光环，不额外射击、不触发普通攻击动画 |
| 数字键 4 / 第四按钮 / 开战按钮 | 可选择新塔；窗口变窄时按钮换行；点击工具栏不在地图建塔 |
| 暂停后恢复 | 扫描和效果有效期随游戏时间暂停 |

当前新场景没有暂停按钮；最后一项仅在你使用已有调试方式设置 Time.timeScale 时检查。

## 6. 当前边界

- 减速塔本身没有实现群落感应、炎症、定身、塔承伤、拦截或敌人拆塔；后续神经塔的短暂停顿单独使用 EnemyStunState，见 [剩余塔逻辑与接线](REMAINING_TOWERS_SETUP.md)。
- 离圈采用短时到期恢复，没有使用触发器离圈时立即移除来源的规则。
- 查询间隔和有效期是初始参数；低帧率下是否出现减速断档、经济和关卡难度仍需用户试玩。
- 静态检查和差异检查不等于 Unity 编译、Play 回归或性能测量；不宣称 CPU / GC 或帧率已有提升。
