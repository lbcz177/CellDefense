# 酸化炮与神经塔：课程适用范围

> 2026-10-18 校正：两塔已接入第三至第五关六塔列表，酸区 / 破壳、连锁 / 停顿及连锁反馈已有自动测试。第一、二关为四塔。下方接入步骤、待验证和旧三关快照保留教学参考，不表示当前尚未实现。完整规则和已有证据见 [策划](GAME_DESIGN_FRAMEWORK.md)、[当前状态](PROJECT_STATUS.md)。

## 历史接线与知识材料

# 酸化炮与神经脉冲：基础逻辑和接线

2026-10-16：六塔基础机制已完成实战回归，神经塔补了可复用连锁线段显示。酸区叠加 / 清理、致命首击和复用目标保护已验证；当前总计 EditMode 78 项、PlayMode 9 项通过，详见 [六塔当前状态](TOWER_ABILITIES_STATUS.md)。下文早期“未执行测试”描述只代表当时状态。

2026-10-14 后续更新：已创建 AcidTower / NeuralPulseTower 的 Prefab 与 Definition，并将六种塔接到第三关；第三关独立敌人 Prefab 已加入减速和停顿组件。三关现在点击有效空格弹出选择框，第一、二关保留原三塔。下文的未接线状态是首轮逻辑交付记录，手动步骤用于检查和自定义，不要重复生成同名资产。详见 [三关试玩说明](OPEN_FIELD_THREE_ROUND_PROTOTYPE.md)，尚未运行 Unity。

2026-10-14，Unity 2022.3.53f1c1，分支 codex/open-field-prototype。

用户授权先实现剩余塔逻辑。此次已写入脚本、公共策略初始化和六个菜单入口；尚未创建塔 Prefab、TowerDefinition 或修改场景与敌人 Prefab。保留现有素材、配置和 .meta。Unity 操作、Play 和 Profiler 验证由用户完成；本次没有构建。

## 1. 当前实现范围

| 塔 | 脚本 | 已写入的基础机制 | 仍缺少的旧策划机制 |
| --- | --- | --- | --- |
| 溶酶体酸化炮 | AcidAttackBehaviour | 普通子弹直击；命中后在落点生成持续酸区，伤害走 EnemyController.TakeDamage | 生物膜 / 护盾破坏、群落、炎症和 LV2 / LV3 分支 |
| 神经脉冲刺突 | NeuralPulseAttackBehaviour | 从已承诺主目标开始，向邻近未命中敌人连锁，逐跳衰减，并施加短暂停顿 | 同群落优先传导、群落行为打断、炎症和 LV2 / LV3 分支 |

主目标仍由 TowerController 在攻击开始时按当前配置选择；使用 FarthestAlongPath 时优先剩余路程最短者。动画释放继续使用已承诺目标和 SpawnVersion。神经塔没有改成“能串最多敌人的主目标优先”，后续跳跃才查询邻近敌人。

本次不增加塔承伤、近战拦截、拆塔或封路规则。

## 2. 公共接入变化

- TowerAttackBehaviour 新增 Initialize(TowerDefinition, LayerMask)。TowerController 赋好配置和依赖后调用，特殊策略在这里校验参数并准备范围查询。校验异常可进入既有建造回滚路径。
- RequiresProjectile 默认 false；抗体和酸化策略返回 true，通用初始化要求其 Definition.ProjectilePrefab 非空。实际子弹仍来自场景固定 ProjectilePool，不按配置自动切换池模板。
- RequiresDamage 默认 true；抗体与减速策略返回 false，允许 Definition.Damage=0，CurrentDamage 始终为 0。输出策略仍要求正伤害。标记易伤在 EnemyController.TakeDamage 统一乘 1.15；酸区和连锁传入基础 / 升级伤害，不自行重复乘易伤。
- 禁用特殊策略组件后，控制器不再启动或释放该组件的攻击，也不回退成普通子弹塔。
- 新策略各自拥有 EnemyRangeQuery，复用结果集合、识别子对象碰撞体、过滤中心距离并按敌人去重。
- EnemyTargetSnapshot 保存敌人引用、SpawnVersion 和采样位置。结算前检查版本、活跃状态及可攻击状态，避免伤害落到复用后的新实例。
- 菜单最多显示六项，支持数字键 1–6；按钮、开战按钮和指针排除区域按对应布局换行。

## 3. 酸化炮

在新塔 Prefab 上挂唯一的 AcidAttackBehaviour，保留 TowerController，关闭 Samples Antigen On Hit，Enemy Layer Mask 选择 Enemy。

| 设置 | 起始值 / 建议 | 用途 |
| --- | --- | --- |
| Definition.Damage | 可先设 15，来自旧策划直击基线 | 只在子弹命中时扣除一次；实际数值由用户试玩调整 |
| Definition.AttackRange | 正数，由用户设置 | 主目标射程；不是酸区半径 |
| Definition.AttackInterval | 正数，由用户设置 | 常规发射间隔 |
| Definition.ProjectilePrefab | 已有合法子弹引用 | 必须配置；实际发射仍使用场景子弹池 |
| Acid Radius | 1.25 | 落点酸区的世界单位半径，初始试用值 |
| Acid Duration | 3 | 酸区持续游戏秒数，旧策划 LV1 基线 |
| Damage Per Second | 10 | 单个酸区每秒伤害，旧策划 LV1 基线 |
| Tick Interval | 0.25 | 查询与结算间隔，必须为正且不大于酸区时长 |

一次完整流程：控制器释放 → 从池取子弹 → 子弹校验目标版本并飞行 → 命中回调先登记酸区 → 子弹造成直击伤害 → 子弹回池 → 酸区周期查询和扣血 → 到期移除。

子弹命中回调发生在直击扣血之前，因此击杀主目标的直击仍可留下酸区。子弹飞行期间目标死亡、抵达核心或被复用时，沿用已有子弹取消规则，不在空中或新目标位置补酸区。酸区中心固定在命中落点，不跟随原目标，也不把持续伤害状态挂在敌人身上。

酸区伤害按自上次结算起经过的游戏时间计算，最后一次截断到酸区结束时间。查询采用当前位置采样，不重建两次查询之间的真实进出轨迹；最后一笔伤害可能延迟到下一次结算，低帧率和边缘效果需用户观察。同一酸区中的同个敌人多碰撞体只算一次；不同酸区允许叠加，按同次采样累计伤害再结算。这是此次首版采用的叠加规则，平衡尚待试玩。

发射时快照升级倍率：直击使用传入的 damage，酸区每秒伤害乘同一升级倍率。普通 +50% 伤害升级下，初始 15 / 10 变为 22.5 / 15，不重复乘升级。

关闭酸化策略会清除其全部酸区并使旧飞行子弹的酸区回调失效；已发射子弹仍按现有 Projectile 逻辑造成直击。

2026-10-14 后续已补酸区显示：子弹实际命中后，在固定落点显示半透明绿色酸液圆与边界。几何覆盖和伤害查询使用同一份命中时的半径快照；默认半径 1.25、持续 3 秒，透明度轻微脉动，最后 0.35 秒内淡出，到期隐藏。显示时间每帧更新，原伤害仍按 0.25 秒采样，末笔累计伤害可能在下一次采样补算。

AcidFieldVisual 仅负责显示，既不查询敌人，也不扣血。每座酸化塔预热两个显示实例，命中从闲置栈取出；到期或策略禁用隐藏并归还。实例不足时按需扩展，所有实例共用该塔的一份圆形 Mesh 和 Material。显示对象放在单位缩放的独立场景根上，避免酸化塔的小 Sprite 缩放压缩范围；使用 BackGround 排序层，盖在地图上、位于敌人与塔下方。塔销毁时清理显示根、Mesh 和 Material。

AcidTower.prefab 已保存 Acid Color / Boundary Color，不需要额外挂接特效 Prefab，也没有生成或修改图片；这是代码绘制的基础范围反馈，没有酸液贴图、气泡粒子或专用子弹外观。用户需确认绿色区域可见、与伤害范围对齐、固定落点不跟随目标、暂停冻结、到期和禁用消失、连续攻击 / 重开无残留。本次未执行 Unity、Play 或构建。

## 4. 神经脉冲刺突

在新塔 Prefab 上挂唯一的 NeuralPulseAttackBehaviour，保留 TowerController，关闭 Samples Antigen On Hit，Enemy Layer Mask 选择 Enemy。它直接结算连锁，不取子弹。

| 设置 | 起始值 / 建议 | 用途 |
| --- | --- | --- |
| Definition.Damage | 可先设 45，来自旧策划基线 | 第一目标的基础伤害 |
| Definition.AttackRange / AttackInterval | 正数，由用户设置 | 主目标射程与攻击开始间隔 |
| Max Targets | 3 | 一次攻击最多命中几个目标，包含第一个；初始试用值 |
| Chain Radius | 2.5 | 从上一目标采样位置寻找下一跳的半径，初始试用值 |
| Damage Retention | 0.8 | 每跳保留前一跳 80% 伤害，即衰减 20% |
| Stun Duration | 0.2 | 短暂停顿游戏秒数；填 0 可关闭停顿，仅保留伤害 |

先在出手瞬间建立整条链，每次选上一目标附近最近、尚未加入链的有效敌人；距离近似相同用实例编号决定顺序。下一跳允许超出塔的主目标射程，但不能超出上一跳的 Chain Radius。同一次攻击不重复命中同一敌人。

建立完毕后再逐个结算，避免第一个目标死亡 / 回池改变后续查找的起点。每个目标都有入场版本快照，失效时跳过，不重新补选。连锁在一次出手中同步完成，目前没有逐段延迟、电弧 LineRenderer 或传导动画。

基础伤害 45、保留率 0.8 时，三目标依次为 45、36、28.8；传入伤害已包含升级，逐跳只做衰减。

上述数字针对未标记目标。各目标独立按结算时的标记状态享受 15% 易伤；三目标全被标记时为 51.75、41.4、33.12。单个基础每秒伤害 10 的酸区，对持续处于标记中的目标结算为每秒 11.5。

## 5. 敌人的短暂停顿

给要受停顿的敌人 Prefab 在 EnemyController / PathFollower 所在对象上添加并启用 EnemyStunState。

| EnemyStunState 设置 | 初始值 | 用途 |
| --- | --- | --- |
| Recovery Duration | 0.5 | 停顿结束后不能再次停顿的恢复窗口 |
| Stun Immune | false | 需要完全免疫停顿的敌人可手动开启 |

TryStun 在停顿中及恢复窗口内拒绝再次施加，不延长现有结束时间。默认一次停顿 0.2 秒，然后恢复 0.5 秒，避免多个神经塔通过每帧刷新无限延长同一次停顿。

PathFollower 只临时跳过移动，没有调用 Stop / Begin，不重置路点、不改变关卡倍率或减速倍率。停顿结束继续沿原路径移动。EnemyController.Initialize、EnemyStunState.OnEnable / OnDisable 清空停顿和恢复窗口；暂停时随 Time.time 暂停。

缺少或禁用 EnemyStunState 时，神经塔仍造成连锁伤害，并报告一次停顿接线错误。当前不修改敌人抵达核心攻击动作，不把停顿施加给 CanBeTargeted=false 的敌人。

## 6. 两张地图的资源接线

1. 在 Unity 复制两个合适的塔 Prefab，分别挂 AcidAttackBehaviour 和 NeuralPulseAttackBehaviour，移除复制带来的旧特殊策略。每塔最多一个 TowerAttackBehaviour。
2. 用已有酸化炮 / 神经刺突 Sprite 和图标；本次没有生成或改动图片。
3. 在 Assets/_Project/Data/Towers/ 创建两个独立 TowerDefinition。伤害、射程、间隔、价格填合法正值，保留现有配置；此处的 15 / 45 只是旧策划起点，未做经济平衡。
4. 酸化配置填写 ProjectilePrefab，两个 Prefab 的 TowerController.Enemy Layer Mask 选择 Enemy。
5. 敌人 Prefab 添加 EnemyStunState，保留上次的 EnemySlowState。
6. 将 OpenFieldPrototype 与 OpenFieldLevel02 的 Tower Definitions 扩为六项；保留前三项，第四项为减速塔，第五项酸化炮，第六项神经塔。
7. 若启用 Release On Animation Event，配置有效的 Attack 动画及 ReleaseAttack 事件，并遵守当前 0.6 秒释放等待上限；没有事件的动画使用立即释放。

单纯追加数组不创建 Prefab 或配置。脚本已经提供菜单 5 / 6 与数字键入口，接线后才会实际显示可建塔。

## 7. 待用户验证

- 普通发射只直击一次，命中后生成酸区；目标在飞行时死亡 / 复用不生成旧酸区。
- 酸区离开原目标后仍留在落点，其他敌人可受到范围伤害；到期移除；同敌人多碰撞体去重。
- 多个酸区叠加与升级后的直击 / 每秒伤害符合上述规则。
- 三目标连锁对应伤害 45 / 36 / 28.8，邻居不足时提前结束；首目标死亡不阻止已确定的后续链。
- 被停顿敌人恢复原路径移动；恢复窗口内重复攻击不延长停顿；死亡复用不带旧状态。
- 在结算回调中死亡、清场或禁用塔时，版本校验和效果清理不误伤复用实例，也不发生集合遍历修改异常。
- 六个按钮、数字键、窗口变窄时换行和工具栏点击排除正常；现有三塔采样 / 标记 / 吞噬及前排锁敌仍需回归。
- 未取得新的 Play、Unity 编译或 Profiler 证据，不能把静态差异检查当作运行或性能验收。
