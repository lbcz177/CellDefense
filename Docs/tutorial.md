# T01 源码课程：当前接口补充

> 2026-10-18：保留下方课程原文。当前五关已支持战中建造、动态路径、承伤 / 容量和出售；不再是两关三塔或仅布置期建造。源码行为以 [当前架构](TECHNICAL_ARCHITECTURE.md)为准。

- TowerAttackBehaviour 已增加 CanTarget，在共享前排比较之前筛选有效候选；抗体跳过未知抗原，没有有效目标不消耗冷却。
- 每次攻击仍按剩余实际路程选前排，策略优先级仅破同分；动画保留承诺目标，释放超时 0.6 秒，SpawnVersion 校验仍必需。
- 路径现可在壁垒建造 / 出售后重建，PathFollower.Reroute 保留当前段与状态；不能再假设整轮路径永远不变。
- 第一、二关四塔，第三至第五关六塔；菜单按 towerDefinitions 创建选项。开放 BuildSlot 首次共享抗原时会创建连线反馈。
- 子弹池仍使用场景固定模板；EnemyController 核心动作超时为 0.75 秒。已有自动测试及其边界见 [当前状态](PROJECT_STATUS.md)。

## 原课程与历史实现快照

# 胞卫战源码学习：塔的攻击流程与扩展

源码快照：2026-10-12，基准提交 `01b43c5`，包含当前工作区未提交的选敌、动画、表现和对象池改动。引擎为 Unity 2022.3.53f1c1。

本次只覆盖开放场景的建塔、选敌、攻击和伤害链路，以及用户自行编写下一座塔的接入点。近战拦截、塔生命值和敌人拆塔仍是讨论中的玩法，当前源码没有实现这些规则。未执行构建或 Play 验证。

前置知识：MonoBehaviour、ScriptableObject、继承与 override、事件、Animator Trigger / Animation Event、对象池。

## T01：从建塔走到命中，再扩展一种攻击方式

状态：已展开。

读完应能回答：谁决定何时攻击、攻击谁、攻击怎样发生，以及如何增加一种攻击效果。

### 1. 先理解报错来自哪里

用户报告的编辑器错误为 CS1061：EnemyController 未包含 SpawnVersion。

核对发现：

- `Assets/_Project/Scripts/Enemies/EnemyController.cs` 中存在 `public uint SpawnVersion { get; private set; }`，Initialize 每次入场递增它。
- `Assembly-CSharp.csproj` 包含这份 EnemyController 源码。
- 当前项目脚本目录中只有一个 EnemyController 类定义。
- 已检查的 Unity Editor.log 没有对应的 TowerController 编译错误，并记录了进入 Play、加载开放场景。

这说明磁盘源码与代码编辑器的诊断不一致，可能是语言服务缓存、打开的工作区或未保存的编辑器内容不一致。尚未直接验证编辑器刷新后红线消失。SpawnVersion 是类成员，不需要添加 using 才能访问。

建议依次检查：在编辑器中对 EnemyController 按 F12，确认跳到上述文件且能看到属性；核对未保存内容后刷新编辑器窗口；必要时在 Unity 的 Edit → Preferences → External Tools 中点击 Regenerate project files，再从 Unity 双击脚本打开工程。[Microsoft 的 Unity 工具排错文档](https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/troubleshooting/troubleshooting-and-known-issues-visual-studio-tools-for-unity)说明了重新生成工程文件的入口。

代码编辑器使用工程文件进行代码分析；Unity 使用自己的脚本编译与程序集加载流程。因此编辑器红线与 Unity 的实际编译结果可能不一致。真正的脚本编译失败通常会阻止重新进入 Play。

### 2. 主链与阅读顺序

```text
玩家规划期点击格子
  → SelectBuildCell：有效空格打开 OpenFieldBuildMenu
  → 悬停预览，选择图标 / 价格选项
  → OpenFieldPrototypeController.TryBuildAt
  → Instantiate(TowerDefinition.Prefab)
  → TowerController.Initialize
  → TowerController.Update：冷却结束、特殊攻击允许
  → FindTarget：射程内剩余路线最短的可攻击敌人
  → 启动一次攻击
      ├─ 立即出手：Attack
      └─ 动画出手：Attack Trigger → 动画事件 ReleaseAttack → Attack
  → Attack 的效果分支
      ├─ 默认攻击：ProjectilePool.Get → Projectile.Initialize
      │             → 子弹飞行 → Projectile.Hit
      └─ 特殊攻击：TowerAttackBehaviour.Attack
  → EnemyController.TakeDamage → Health.TakeDamage
  → 死亡事件 → EnemyController.Exited
  → 开放场景结算奖励、移出活跃集合、回收敌人
```

两条出手分支和两条攻击效果分支分别是条件选择，不会一次攻击把所有分支都执行。

必读顺序：

1. `Assets/_Project/Scripts/Data/TowerDefinition.cs`：配置输入，包含价格、基础伤害、射程、攻击间隔、选敌模式和 Prefab。
2. `Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs` 的 TryBuildAt：读取配置、创建塔和 BuildSlot，调用 Initialize。失败时退回 ATP、销毁失败的实例。
3. `Assets/_Project/Scripts/Towers/TowerController.cs` 的 Initialize、Update、FindTarget、IsCandidateBetter：把配置变成一次次攻击请求。
4. 同文件的 ReleaseAttack、Attack：观察动画事件边界和攻击策略分支。
5. `Assets/_Project/Scripts/Towers/TowerAttackBehaviour.cs`：新增攻击方式的接口。
6. `Assets/_Project/Scripts/Combat/Projectile.cs` 的 Initialize、Update、Hit、ResetForPool，以及 `EnemyController.TakeDamage`：理解飞行、命中和回收。

选读：AntibodyAttackBehaviour、PhagocyteAttackBehaviour、BuildSlot.ShareAntigen、ProjectilePool。暂缓地图绘制和 BFS 内部实现；本课只使用路径剩余距离作为选敌输入。

### 3. 配置与控制器如何配合

TowerDefinition 是共享的 ScriptableObject 配置，不保存某座塔的运行状态。两座塔可以共用它，但应分别拥有目标、冷却和升级状态。

TowerController.Initialize 接收 definition、pool 和 slot，检查配置并缓存依赖。每座塔允许零个或一个 TowerAttackBehaviour，超过一个会抛异常。零个时使用默认子弹攻击；一个时调用它的 Attack。

普通塔 Update 使用 Time.deltaTime 推进攻击冷却。冷却未结束、等待动画出手、CanAttack 为 false 时，不启动新攻击。CurrentDamage 包含升级倍率；现有升级使基础伤害增加 50%。特殊攻击应该使用传入的 damage 参数，以继承这条升级规则。

### 4. 目前怎样选择“最前面的敌人”

FindTarget 用复用的 List<Collider2D> 接收 Physics2D.OverlapCircle 查询结果，再按敌人中心距离做精确射程过滤，排除不可攻击敌人。

现有三个 TowerDefinition 都使用 FarthestAlongPath。该枚举值现在表示优先选剩余实际路线最短的敌人，适用于多入口和不同长度的路径。每轮攻击准备时重新选取目标。距离近似相同时，才比较特殊攻击的 GetTargetPriority；再次并列时选离塔更近的敌人。

路径距离来自 EnemyController.RemainingPathDistance → PathFollower.RemainingDistance：当前位置到下一路点的距离，加上下一路点到终点的剩余距离。WaypointPath 预计算共享的剩余距离数组，Configure 会刷新它。现有路径在一轮进攻中保持静止；运行中移动路点后，需要重新配置才能刷新这份缓存。

这里的“最前面”由通往终点的剩余路程决定，和敌人的出生顺序无关。特殊塔也不能仅因一个后方敌人可吞噬或未标记，就越过更靠前的敌人。

### 5. 动画事件怎样决定出手

当前近卫 Tower.prefab 启用了 releaseOnAnimationEvent。开始攻击时，控制器记录 pendingAttackTarget 和 pendingAttackSpawnVersion，触发 Animator 的 Attack。

攻击动画在出手帧调用塔对象上的 public ReleaseAttack。它取出已记录的目标、先清除等待状态，再检查目标是否仍存活、启用并属于同一轮入场，最后调用 Attack。重复动画事件因等待状态已清除而不会重复出手。

需要注意：

- 开启事件出手时，动画必须带有 ReleaseAttack 事件，事件要能到达挂有 TowerController 的对象。
- 当前等待上限固定为 0.6 秒。超时会取消待出手攻击；超时后到达的事件不会造成伤害。
- 这次攻击使用攻击开始时承诺的目标。目标死亡或被复用时取消出手，不在该事件中重新选敌。
- ReleaseAttack 当前没有再次检查射程：目标在准备阶段位于射程内，前摇期间移出射程仍可能被出手。
- 未启用事件出手时，控制器立即调用 Attack，再播放动画。不要同时在另一段代码里再结算一次伤害。

### 6. 攻击方式在哪里扩展

| 实现 | 攻击结果 | 额外规则 |
| --- | --- | --- |
| TowerController 的默认分支 | 从子弹池取子弹，飞行到目标后命中 | 近卫可在命中时传播抗原记忆；支持炮口偏移和子弹外观 |
| AntibodyAttackBehaviour | 认识目标抗原时发射零伤害标记子弹，使其他塔伤害增加 15% | 标记目标需要启用的 EnemyImmuneState；记忆来自邻塔信号；2026-10-14 改为纯辅助 |
| PhagocyteAttackBehaviour | 直接对目标结算伤害；满足阈值时吞噬 | 未标记血量阈值 20、标记阈值 50；吞噬后消化 3 秒，通过 CanAttack 暂停出手 |

TowerAttackBehaviour 的三个扩展点：

- Attack：抽象方法，派生类必须实现，定义一次出手产生什么效果。
- CanAttack：默认 true；需要充能、消化或其他额外等待时覆盖。公共攻击间隔仍由 TowerController 管理。
- GetTargetPriority：默认 0；值较大的敌人有更高的并列优先级。当前前排模式会先比较剩余路程。

如果只改变伤害、射程、射速和外观，创建新的配置和 Prefab 即可。需要新的攻击机制时，再增加派生攻击组件。

### 7. 自己写下一座塔的最小步骤

先用一个最小攻击组件验证接入，再自行把 Attack 内部改成所需机制。下面是阅读示例，没有创建对应运行脚本或塔资源：

```csharp
public class MyTowerAttackBehaviour : TowerAttackBehaviour
{
    public override void Attack(
        EnemyController target, float damage, ProjectilePool projectilePool)
    {
        if (target == null || !target.isActiveAndEnabled || !target.CanBeTargeted)
            return;

        target.TakeDamage(damage);
    }
}
```

1. 新建派生脚本，在 Attack 中写一次攻击的效果。直接伤害和子弹命中伤害要选择清楚结算时点。
2. 复制合适的塔 Prefab，挂载 TowerController 和你的攻击组件。复制巨噬塔或抗体塔时，移除原来的攻击组件；复制近卫塔时，增加特殊攻击前把 samplesAntigenOnHit 关闭，否则 Initialize 会拒绝这种组合。
3. 创建新的 TowerDefinition，填写伤害、射程、间隔、价格和新 Prefab。输出塔伤害必须为正；RequiresDamage=false 的辅助策略允许伤害为 0。射程、间隔仍必须为正，Enemy Layer Mask 也必须正确。
4. 使用攻击动画时配置 Attack Trigger 和 ReleaseAttack 事件。没有出手事件的动画应使用立即出手方式。
5. 在开放场景的 towerDefinitions 数组接入。当前菜单只显示前三种塔，快捷键也只有 1、2、3；单纯把第四个配置追加到数组，不会自动出现第四个按钮。可以先替换一个现有槽位验证，之后再扩展菜单。

当前 ProjectilePool 用场景中的固定 prefab 创建子弹。TowerDefinition.ProjectilePrefab 的引用会被检查，但实际 pool.Get 不按它切换池。因此更换配置里的 ProjectilePrefab，不会自动改变实际生成的子弹。当前默认近卫分支可用 SetVisual 调整外观；新的子弹运动或爆炸机制需要另外设计池与子弹的配合。

### 8. 命中、状态与清理边界

伤害统一通过 EnemyController.TakeDamage 进入：它检查可攻击状态，触发受击表现，按当前标记状态乘一次 1.15 易伤，再交给 Health。攻击策略传入未计算易伤的伤害。敌人死亡后，Exited 通知场景结算奖励和回池。攻击组件直接 Destroy 敌人会跳过这条退出链，可能留下活跃计数，使波次无法结束。

SpawnVersion 用于识别对象池中同一实例的不同入场。敌人 A 回池后被当作敌人 B 使用时，C# 引用仍指向同一实例，但版本已增加。延迟命中、协程、待释放技能应同时保存目标引用和版本号，并在结算前重新检查。现有 TowerController 与 Projectile 已这样保护。

范围攻击查询时应复用结果集合。一个敌人有多个碰撞体时，要按 EnemyController 去重，避免同一次范围攻击重复伤害。临时集合、每次命中 Instantiate 和重复订阅事件的成本，会随敌群规模放大。新组件自己的临时状态也要定义禁用、销毁或将来池化时如何清理。

### 9. 具体例子与复述任务

假设塔基础伤害 25、间隔 1 秒，射程内两个敌人剩余路程为 3 和 5。塔选择剩余 3 的敌人；启用事件出手时先播放动画，出手帧调用 ReleaseAttack。默认攻击发射子弹，子弹命中后造成 25 伤害，再回池；已升级塔传入的伤害是 37.5。若出手前目标死亡或被回收复用，该次待出手攻击取消。

请自行复述：你准备写的新塔，哪里决定攻击频率、哪里选敌、哪里产生攻击效果、哪一步真正扣血？然后列出它需要新增的临时状态，以及状态的清理时机。

### 10. 手写新塔前：类型、字段和读写边界

补充核对：2026-10-13，当前分支 `codex/open-field-prototype`、HEAD `01b43c5`，包含工作区未提交修改。本节依据磁盘源码；最新锁敌尚待用户 Play 回归，性能尚无 Profiler 实测。新塔的核心机制待用户选择，下面只提供扩展契约和留空骨架。

`TowerDefinition : ScriptableObject` 是多座塔可以共享的配置资产；`TowerController : MonoBehaviour` 保存单座塔的运行状态并调度攻击；`TowerAttackBehaviour : MonoBehaviour` 是抽象基类，你的组件继承它，负责一次出手的效果。这里的“接口”指可调用的约定，源码并没有声明名为 TowerAttackBehaviour 的 C# interface。

配置通过 Inspector 写入，运行时用公开属性读取。`[SerializeField] private` 让 Unity 保存字段，同时限制其他 C# 类直接写入；`public ... { get; private set; }` 允许其他类读取，只允许所属类赋值；`=>` 属性在被读取时计算结果。`float` 用于秒数、距离和伤害；`int` 用于价格、数量和优先级；`uint` 是非负的整数版本号；组件引用可以失效，使用前需要校验。

TowerDefinition 的字段（左侧为序列化字段 / 对外属性）：

| 字段 / 属性 | 类型 | 职责与读取方 | 设置 / 重置时机 |
| --- | --- | --- | --- |
| displayName / DisplayName | string | 建造菜单名称 | 制作配置时填写；不作战斗重置 |
| prefab / Prefab | TowerController | 建造时实例化的塔模板，其引用指向 Prefab 上的组件 | 制作配置时连接新 Prefab |
| buildIcon / BuildIcon | Sprite | 建造预览 / 图标输入 | 制作配置时填写 |
| placementKind / PlacementKind | BuildPlacementKind 枚举 | 通用建造系统使用的放置类别；开放场景 TryBuildAt 主要检查 grid.CheckPlacement | 制作配置时填写；不是新技能的运行状态 |
| projectilePrefab / ProjectilePrefab | Projectile | 默认塔及 RequiresProjectile=true 的策略（抗体、酸化）初始化时要求非空；场景子弹池实际使用自身 prefab | 配置时填写；这个字段不会自动切换子弹池模板 |
| buildCost / BuildCost | int | ATP 价格；开放场景要求 > 0 | 配置时填写；建造读取并记录已支付价格 |
| attackRange / AttackRange | float | 世界单位射程；TowerController 查询并按中心距离过滤敌人 | 配置时填写，Initialize 要求 > 0 |
| damage / Damage | float | 输出塔基础伤害；CurrentDamage 据此计算升级伤害；辅助塔忽略 | 输出塔 Initialize 要求 > 0；RequiresDamage=false 允许 0，CurrentDamage=0；不把单塔升级写回这里 |
| attackInterval / AttackInterval | float | 两次攻击开始之间的公共冷却 | 配置时填写，Initialize 要求 > 0 |
| targetingMode / TargetingMode | TargetingMode 枚举 | FarthestAlongPath=1：剩余路程最短优先；NearestToTower=0：优先级比较后按离塔距离选择 | 配置时填写；前三座塔使用前排模式 |

TowerController 中与你的新组件有关的字段与属性：

| 字段 / 属性 | 类型 | 谁读写、用于什么 | 初始化 / 清理时机 |
| --- | --- | --- | --- |
| Definition | TowerDefinition | Initialize 写；控制器和需要配置的新组件读 | Initialize 接收配置；Awake / OnEnable 时可能仍为 null |
| IsUpgraded / CurrentDamage | bool / float | 控制器更新升级状态；CurrentDamage 每次读取计算，升级后为基础值的 1.5 倍 | Initialize 将 IsUpgraded 设为 false；TryUpgrade 设为 true；Attack 接收到计算后的 damage |
| BuildCostPaid / UpgradeCostPaid | int / int | 单塔的已支付价格，供经济操作读取 | Initialize 记录建造价并清零升级价；TryUpgrade 记录升级价 |
| enemyLayerMask / targetFilter | LayerMask / ContactFilter2D | Inspector 配置敌人层；Initialize 创建查询过滤器 | 每次 Initialize 配置过滤器；LayerMask 必须非零 |
| attackCooldown | float | 控制器每帧减去 deltaTime；攻击开始即写入 AttackInterval | Initialize 设为 0；技能不再实现同一份普通冷却 |
| currentTarget / currentTargetSpawnVersion | EnemyController / uint | 控制器保存本轮选敌及其入场版本 | Initialize 清空；每次攻击开始重新选敌并记录版本；失效时清空 |
| targetCandidates | readonly List<Collider2D> | 控制器复用物理查询结果，初始容量 64，可扩容 | 字段创建时分配，查询反复使用；readonly 限制换掉引用，仍可改列表内容 |
| projectilePool / occupiedSlot | ProjectilePool / BuildSlot | Initialize 缓存场景子弹池和占用格；Attack 将池作为参数传给策略 | Initialize 接线；策略没有直接访问这些 private 字段的权限 |
| attackBehaviour | TowerAttackBehaviour | GetComponents 查找同对象上的特殊攻击策略 | Initialize 要求最多一个；无策略用默认子弹，有策略只调用策略 Attack |
| animator / bodyRenderer | Animator / SpriteRenderer | 控制器缓存动画和主体显示组件 | Initialize 用 GetComponent 获取 |
| releaseOnAnimationEvent | bool | Inspector 决定是否等待动画出手事件；还需有效 Animator 与 Controller | 配置 Prefab；等待方式在每次攻击开始判断 |
| awaitingAttackRelease / attackReleaseTimeout | bool / float | 控制器标记等待动画释放，并处理 0.6 秒超时 | 攻击开始写入；ReleaseAttack、超时、Initialize、OnDisable 清空 |
| pendingAttackTarget / pendingAttackSpawnVersion | EnemyController / uint | 控制器保存前摇时承诺的目标身份 | 攻击开始写入；取消或释放时清空；ReleaseAttack 校验后才调用策略 |
| muzzleLocalOffset / projectileVisual / projectileVisualSize | Vector2 / Sprite / float | 默认子弹分支的炮口和显示配置 | Prefab 设置；特殊策略分支会提前 return，不自动使用这些设置 |
| aimVisualAtTarget | bool | 控制器在攻击开始时根据目标方向翻转塔身 | Prefab 设置；特殊策略也经过这段塔身朝向逻辑 |
| samplesAntigenOnHit / antigenMemoryDuration | bool / float | 默认近卫子弹命中回调传播抗原 | Prefab 设置；使用特殊策略必须关闭采样；采样时记忆时长须 > 0 |

新组件不需要复制这些 private 字段。控制器已经决定“何时攻击”和“主目标是谁”；新组件只保存自己的机制需要的状态。

### 11. 三个扩展点与敌人 / 子弹接口

基类声明见 `Assets/_Project/Scripts/Towers/TowerAttackBehaviour.cs`：

```csharp
public virtual bool CanAttack => true;
public virtual int GetTargetPriority(EnemyController target) => 0;
public abstract void Attack(
    EnemyController target, float damage, ProjectilePool projectilePool);
```

`abstract` 要求子类实现；`virtual` 已有默认行为，仅需要时 override。三个扩展点的调用时机不同：

| 扩展点 | 返回值 / 输入 | 调用时机与责任 |
| --- | --- | --- |
| RequiresDamage | bool，默认 true | 初始化校验及 CurrentDamage 读取；抗体、减速返回 false，允许零伤害配置且实际伤害固定为 0 |
| CanAttack | bool | 普通冷却结束且未等待动画释放时读取；false 阻止开始新攻击。应只返回就绪状态，不在 getter 中结算伤害。ReleaseAttack 不会再次检查它 |
| GetTargetPriority | target 为候选 EnemyController；返回 int，越大越优先 | 选敌比较时读取，可能一次查询调用多次。前排模式仅在剩余路程近似相同时使用；应只读状态 |
| Attack | target、damage、projectilePool；返回 void | 立即出手或有效的 ReleaseAttack 之后调用一次。可直接结算效果或发射子弹；void 不会告诉控制器是否命中，也不会退回已经消耗的冷却 |

Attack 参数的含义：`target` 是本次承诺的主目标；`damage` 是释放时计算的当前伤害，已包含升级，优先使用它而不是硬编码或再次乘 1.5；`projectilePool` 是场景中已初始化的共享子弹池，直接效果可以不用它。控制器目前即使是直接伤害塔，也要求 Initialize 传入非空池和 BuildSlot。

| 可调用接口 | 类型 / 签名 | 用途与生命周期 |
| --- | --- | --- |
| target.isActiveAndEnabled | bool | 组件及所属对象是否启用，结算前检查 |
| target.CanBeTargeted | bool | 已退出、死亡或正在攻击核心时为 false；它自身没有包含 active / enabled 检查，需要结合上一项 |
| target.SpawnVersion | uint，只读 | EnemyController.Initialize 每次入场递增；延迟攻击保存当时版本，结算时比较；新塔不要改写或重置它 |
| target.RemainingPathDistance | float，只读 | 控制器的前排选敌输入；新效果通常不必重复选主目标 |
| target.Definition | EnemyDefinition，只读引用 | 读取抗原、可否吞噬等敌人配置；Initialize 设置 |
| target.TakeDamage(amount) | void，amount 为正 float | 受击反馈 → 当前标记易伤（×1.15）→ Health 扣血 → 死亡退出事件 → 场景奖励 / 活跃计数 / 回池；不要用 Destroy 替代 |
| target.GetComponent<Health>().CurrentHealth | float，只读 | 机制确实需要生命阈值时读取；组件可能缺失，先检查；正常伤害仍走 EnemyController |
| EnemyImmuneState.IsMarked / ApplyMark(duration) | bool / void(float) | 读取标记或应用正时长标记；重复应用延长到期时间；OnEnable / OnDisable / EnemyController.Initialize 清理标记及易伤 |
| EnemyImmuneState.DamageTakenMultiplier | float，只读 | 标记中为 1.15，否则为 1；由敌人统一扣血入口读取，攻击策略不重复读取并乘算 |
| projectilePool.Get(position) | Projectile，position 为 Vector3 | 获取并激活子弹；之后由攻击代码调用 Initialize |
| projectile.Initialize(target, damage) | void | 初始化一次普通飞行攻击，内部记录目标 SpawnVersion |
| projectile.InitializeMark(target, markDuration) | void | 初始化零伤害纯标记子弹，内部记录目标 SpawnVersion；命中只标记和回池，不触发扣血与受击反馈 |
| projectile.Initialize(target, damage, markDuration, onHit) | void；float / Action<EnemyController> | 可附正时长标记和命中回调；命中顺序为回调 → 标记 → 伤害 → 回池。若回调再次伤害主目标，会与子弹伤害叠加 |
| projectile.ResetForPool() | void | 池 Release 时清理目标、版本、伤害、标记、回调及外观；普通攻击代码交给子弹自身完成回收 |

当前控制器已经保护从选敌到动画释放之间的目标身份，现有 Projectile 也保护飞行期间的身份。如果你在 Attack 内新增协程、延迟爆炸或持续伤害，这段新增延迟仍需自己保存并核对引用 + SpawnVersion + 活跃状态 + CanBeTargeted。

### 12. 初始化顺序与自己新增状态的重置时机

当前开放场景的建塔顺序是：

```text
TryBuildAt 检查规划期、格子和 ATP
  → 创建 BuildSlot
  → Instantiate 塔 Prefab
      → 激活实例的 Awake / OnEnable
  → TowerController.Initialize(definition, projectilePool, slot)
  → slot.Occupy(tower)
  → grid.TryOccupy(cell) → 保存格子与邻塔信号
```

Awake 可缓存同对象上的 TowerController、SpriteRenderer 等组件，但此时不能假定 Definition 已赋值。OnEnable 可以准备该组件自己的状态。2026-10-14 实现剩余两塔时，基类新增 `Initialize(TowerDefinition, LayerMask)`，TowerController.Initialize 在赋好依赖后显式调用；需要初始化的策略应 override 这个签名。普通自定义同名方法仍不会自动执行，ResetState 也需要所属生命周期显式调用。

| 时机 | 当前已实现行为 | 新组件需要负责的部分 |
| --- | --- | --- |
| 字段创建 / Prefab 反序列化 | 配置恢复；运行字段取得默认值；集合字段分配 | 仅创建确实需要的状态和容器 |
| Awake | 各组件缓存自身依赖 | 缓存组件，不依赖尚未完成的塔配置注入 |
| OnEnable | 敌人订阅事件；EnemyImmuneState 重置标记 | 若机制规定启用即重新就绪，在此清零自己的忙碌状态、结束时间和待结算记录 |
| TowerController.Initialize | 配置检查、依赖注入、清零公共冷却 / 目标 / 升级、取消待动画攻击，再调用策略 Initialize | 特殊状态由策略的 Initialize override 自行重置；默认实现为空；当前塔每次建造是新实例，未使用塔对象池 |
| Attack | 控制器只调用一次效果入口 | 写入本次技能需要的状态；记录延迟目标身份；使用传入伤害 |
| OnDisable | TowerController 取消等待动画释放；抗体策略清空记忆；巨噬策略恢复反馈颜色 | 清理自己的延迟任务 / 待结算记录、事件订阅、集合和临时显示；若使用协程应明确取消，不依赖所有禁用方式都自动停止协程 |
| 轮次结束 → Planning | 场景销毁本轮路线，保留已建塔；没有再次调用塔 Initialize，也没有禁用全部塔 | 不会自动清零你的特殊状态；Time.time 在规划期仍继续，基于它的消化 / 充能可以继续到期。只在进攻期计时的机制需要另外接入阶段规则 |

不要把现有组件的所有 OnDisable 都理解为“完整战斗状态归零”。例如巨噬当前仅 ResetFeedback，没有清零 digestionUntil；如果同一实例短暂禁用再启用，原消化结束时间仍保留。这是当前实现事实，尚未扩展为塔池化重置协议。

若新机制确实需要，可新增下列字段；它们只是设计候选，不是当前公共接口已有字段，也不要求全部加入：

- `float effectDuration`：Inspector 中的机制时长，配置值不在每次攻击后清零。
- `float busyUntil`：本塔额外忙碌的结束时间；Attack 写入，CanAttack 只读取，重置策略由你定义。
- `EnemyController delayedTarget` 和 `uint delayedSpawnVersion`：新增延迟结算时成对记录，结算或取消时清空；多个并行目标需要多条记录，不能只用一对字段覆盖。
- `List<Collider2D>` 和 `HashSet<EnemyController>`：范围攻击才需要，复用查询结果并在每次效果前清空去重集合；一个敌人多个碰撞体只结算一次。

### 13. 留给你实现核心效果的最小骨架

建议文件位置：`Assets/_Project/Scripts/Towers/MyTowerAttackBehaviour.cs`。以下骨架仅存放在教程，尚未创建运行脚本、Definition 或 Prefab；TODO 留给你填写。类名只是占位，你命名后文件名也保持一致。

```csharp
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerController))]
public class MyTowerAttackBehaviour : TowerAttackBehaviour
{
    public override void Attack(
        EnemyController target,
        float damage,
        ProjectilePool projectilePool)
    {
        if (target == null || !target.isActiveAndEnabled || !target.CanBeTargeted)
        {
            return;
        }

        // TODO：你在这里实现一次出手的核心效果。
        // damage 已包含升级；新增延迟需记录并校验 SpawnVersion。
    }
}
```

这个骨架的 CanAttack 默认 true，GetTargetPriority 默认 0，无自己的运行状态，因此没有需要额外实现的 Awake / OnEnable / OnDisable。TODO 留空时，控制器仍会选敌并消耗一次攻击冷却，但不会伤害敌人。DisallowMultipleComponent 只约束同种组件重复添加；接入时仍须保证塔上最多一个 TowerAttackBehaviour 派生组件。

机制明确后，再列它真正需要的字段与重置规则，然后由你补效果。写完的 Review 会沿这条链检查：合法目标 → 一次出手 → 命中 / 状态结算 → 目标失效或复用 → 技能取消与清理。接入时核对单一策略、关闭近卫采样、配置数值和动画；菜单已在后续实现扩至六项及数字键 1–6。Unity Play 验证由用户完成。

作为开始实现前的复述，请用一句话描述新塔效果，并指出真正扣血是在 Attack 当下、子弹命中，还是你新增的延迟结算时刻。

### 14. 对照策划：哪些塔还没做

用户随后希望从策划已有塔型中选择。当前 `GAME_DESIGN_FRAMEWORK.md` 第 8 节只确定“开发者自行编写下一座塔”，没有指定第四座名称或机制。旧六塔表位于 `GAME_DESIGN_FRAMEWORK_PRE_OPEN_FIELD.md` 第 6.2 节；它是候选内容来源，旧价格和升级规则不是当前开放地图数值。

| 旧策划塔型 | 当前源码落地 | 尚缺内容 |
| --- | --- | --- |
| 中性粒细胞哨兵 | 近卫塔的投射物伤害与命中采样 | NET 捕获、精准趋化等旧升级分支 |
| 抗体 B 细胞中继 | 抗体攻击组件、邻塔记忆、命中标记 | 护盾剥离、克隆记忆、连续中继等 |
| 巨噬细胞清道夫 | 直接伤害、条件吞噬、消化 | 生物酶回收、巨物裂解等；承伤 / 拦截仍是另行讨论的候选 |
| 纤维蛋白壁垒 | 尚无对应攻击策略、Definition 或塔 Prefab | 范围减速；完整旧案还含群落感应抑制和炎症调节 |
| 溶酶体酸化炮 | 尚无对应攻击策略、Definition 或塔 Prefab | 直击与持续酸化；完整旧案还含生物膜破坏和吞噬协作 |
| 神经脉冲刺突 | 尚无对应攻击策略、Definition 或塔 Prefab | 连锁伤害、每跳衰减与短暂停顿；完整旧案还含同群落传导和打断 |

若按旧六塔表的排列，第一个未实现条目是纤维蛋白壁垒。这个排列并没有在当前策划中成为实施顺序。已导入这几类角色的图片不代表机制已实现；现有三塔也不等于旧 LV1 / LV2 / LV3 全套均已完成。

从当前代码出发，推荐把神经脉冲刺突的“连锁伤害 + 逐跳衰减”作为下一次手写任务：它能继续使用控制器承诺的主目标，以及 EnemyController.TakeDamage，不必先建立减速、持续伤害、群落或炎症系统。此推荐尚不代表用户已选择；短暂停顿、同群落传导、打断等应另列边界，不宣称基础连锁已覆盖完整旧策划。

纤维蛋白壁垒需要先定义敌人减速接口及来源重叠 / 到期恢复规则。PathFollower.SetSpeedMultiplier 当前保存关卡速度倍率（第二关 0.75），不能直接拿这个字段反复写入减速并在离圈后恢复为 1，否则会丢失关卡调速。持续光环还需要考虑“没有攻击主目标时仍能作用”，不能只依赖一次 Attack。

溶酶体酸化炮需要先定义持续伤害的归属、跳伤计时、重复命中叠加 / 刷新规则，以及对象池复用时取消旧效果的接口。完整酸池和单目标持续伤害是不同机制，不能在用户尚未选定时混为一套。

### 15. 已选择的手写任务：纤维蛋白壁垒范围减速

用户于 2026-10-13 选择先做减速。下面保留当时的手写规则与骨架作为学习材料；2026-10-14 用户先完成敌人状态和塔扫描初稿，随后授权 AI 实现剩余代码，最新交付见第 16 节。群落感应与炎症属于旧案的其他机制，本节只定义范围减速。

2026-10-14 手写阶段记录：用户创建了 EnemySlowState.cs 与独立的 SlowEntry.cs（public struct），完成来源刷新、有效记录取最强减速、删除过期来源和启停重置。实际入口为 `ApplySlow(float fraction, int sourceID, float duration)`；AI 后续接线沿用此参数顺序，并将 ResetState 公开用于入场初始化。下方 TODO 骨架保留作阅读示例，实际脚本已经完成，不应再复制空骨架覆盖实现。未运行 Unity、构建或 Play 验证。

建议先使用 40% 减速、TowerDefinition.AttackRange 为范围（可先设 2.5）、扫描间隔 0.1 秒、刷新有效期 0.25 秒。前三项职责分别是减速强度、覆盖圈、查询频率；有效期使离圈或来源塔停止工作后最多 0.25 秒再恢复（帧更新还会有一帧误差），这是首版的明确残留规则，不是精确离圈立即恢复。扫描和到期都使用游戏时间，暂停时停止推进；最终数值由用户试玩调整。

速度公式：`实际速度 = moveSpeed × speedMultiplier × slowState.SpeedMultiplier`。现有 speedMultiplier 保留关卡倍率，新增 SlowState.SpeedMultiplier 只表达减速。第二关减速 40% 时：`2 × 0.75 × (1 - 0.4) = 0.9` 世界单位 / 秒；减速到期后恢复 1.5，不恢复成 2。

每座塔以 sourceId 独立保存一条效果，相同塔反复扫描只更新自己的记录。多个来源取当前仍有效的最大 slowFraction；两座 40% 塔仍是 40%。40% 和 55% 同时有效时取 55%，55% 到期而 40% 仍有效时降回 40%，全部到期时减速倍率恢复 1。不能只存“最强强度 + 一个结束时间”，否则弱来源的刷新可能延长已经离圈的强来源效果。

职责链如下：

```text
FibrinSlowAura.Update：每隔 0.1 秒查询覆盖圈
  → 活跃 / 可作用目标过滤、中心距离过滤、按 EnemyController 去重
  → EnemySlowState.ApplySlow(减速比例, 来源编号, 有效期)
EnemySlowState.Update：清理各来源的过期记录，计算最强有效减速
  → SpeedMultiplier
PathFollower.Move：基础速度 × 关卡倍率 × 减速倍率
敌人回池 / 再次 Initialize：清空来源记录，恢复减速倍率 1
```

这里补充第 6 / 7 节通用攻击扩展说明的边界：持续范围光环需要独立 Update 扫描，单次 Attack 入口不能覆盖“没有主目标时仍作用”的需求。建议 FibrinSlowAura 继承 TowerAttackBehaviour，CanAttack 固定返回 false，作为该塔的唯一特殊策略阻止控制器启动普通射击；扫描仍由 FibrinSlowAura.Update 执行。此处不改已有三塔的锁敌规则。

#### 15.1 敌人状态组件的字段与接口

建议脚本：`Assets/_Project/Scripts/Enemies/EnemySlowState.cs`。运行状态属于敌人实例，不能写入共享 EnemyDefinition。

| 名称 | 类型 | 职责 / 读写方 | 初始化与重置 |
| --- | --- | --- | --- |
| SlowEntry | private struct | 每一个来源的记录；值类型便于字典保存 | ApplySlow 构造或更新一条记录 |
| SlowEntry.Fraction | float | 减速比例，0.4 表示减速 40%，不是剩余速度 40%；允许 0 至小于 1 | ApplySlow 写；重算倍率时读 |
| SlowEntry.ExpiresAt | float | 该来源的到期游戏时间 | ApplySlow 写为 Time.time + duration；到期清理时读 |
| slowsBySource | Dictionary<int, SlowEntry> | 来源编号 → 当前效果；同来源更新，不累积条数 | 字段创建时分配；ResetState 清空 |
| expiredSources | List<int> | 暂存到期的来源编号，避免遍历字典时直接删除 | 字段创建时分配；每次清理前 Clear；ResetState 清空 |
| SpeedMultiplier | float，public get / private set | 只允许状态组件写；PathFollower 读取；无效果时 1，40% 减速时 0.6 | 初始 1；ApplySlow / Update 重算；ResetState 恢复 1 |
| ApplySlow(fraction, sourceID, duration) | void(float, int, float) | 校验输入，只更新指定来源，并刷新当前最强减速结果 | 由塔在每次扫描发现敌人时调用 |
| ResetState() | void | 清空所有来源及辅助列表，恢复倍率 | OnEnable、OnDisable，以及接入时的 EnemyController.Initialize 调用 |

sourceId 使用来源塔组件的 GetInstanceID()；它是塔实例身份。EnemyController.SpawnVersion 是敌人的入场身份，两者用途不同。首版同步查询后立即调用 ApplySlow，不保存敌人给未来结算；若以后增加待处理目标或延迟动作，仍须保存并校验敌人的 SpawnVersion。查询和去重集合只作当次扫描临时容器，不跨扫描作为目标承诺。

最小骨架（已有清理代码；来源更新、过期移除和取最强效果留给用户）：

```csharp
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PathFollower))]
public class EnemySlowState : MonoBehaviour
{
    private struct SlowEntry
    {
        public float Fraction;
        public float ExpiresAt;
    }

    private readonly Dictionary<int, SlowEntry> slowsBySource =
        new Dictionary<int, SlowEntry>();
    private readonly List<int> expiredSources = new List<int>();

    public float SpeedMultiplier { get; private set; } = 1f;

    private void OnEnable() => ResetState();
    private void OnDisable() => ResetState();

    public void ResetState()
    {
        slowsBySource.Clear();
        expiredSources.Clear();
        SpeedMultiplier = 1f;
    }

    public void ApplySlow(float fraction, int sourceID, float duration)
    {
        // TODO：校验 0 <= fraction < 1，duration > 0。
        // TODO：更新这个来源的 Fraction 与 ExpiresAt。
        // TODO：调用 RecalculateMultiplier，立即更新当前减速结果。
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        // TODO：expiredSources.Clear()，收集过期来源，遍历结束后删除。
        // TODO：调用 RecalculateMultiplier。
    }

    private void RecalculateMultiplier()
    {
        // TODO：只读取仍有效的记录，求最大 Fraction。
        // TODO：将 SpeedMultiplier 写为 1 - 最大 Fraction。
        //       没有有效记录时结果为 1。
    }
}
```

先写这个组件，再写塔的范围查询。骨架的 TODO 留空时它不会产生减速。RecalculateMultiplier 即使在尚未删除过期记录时调用，也必须排除到期记录，避免 ApplySlow 在本帧 Update 之前被调用时恢复错误强度。

#### 15.2 范围光环组件的字段与骨架

建议脚本：`Assets/_Project/Scripts/Towers/FibrinSlowAura.cs`。

| 名称 | 类型 | 职责 / 读写方 | 初始化与重置 |
| --- | --- | --- | --- |
| slowFraction | float，SerializeField | Inspector 配置强度，默认 0.4 | Prefab 配置；扫描读取，不清零 |
| scanInterval | float，SerializeField | 两次扫描间隔，默认 0.1 秒 | Prefab 配置，必须 > 0 |
| refreshDuration | float，SerializeField | 每次刷新后的有效期，默认 0.25 秒 | Prefab 配置；应大于 scanInterval，否则可能周期性断档；低帧率仍需观察 |
| enemyLayerMask | LayerMask，SerializeField | 光环自己的物理查询层；控制器同名字段为 private，不能直接读取 | Prefab 配置且非零；Awake 设置查询过滤器 |
| tower | TowerController | 读取 Definition.AttackRange 和控制器是否启用 | Awake 缓存；直到 Update 才使用非空 Definition |
| targetFilter | ContactFilter2D | 物理查询过滤器 | Awake 配置层；若扫描触发器，显式设 useTriggers=true |
| candidates | List<Collider2D> | 复用查询输出；初始容量 64，可扩容 | 字段创建时分配；每次扫描前 Clear，禁用时 Clear |
| visited | HashSet<EnemyController> | 同次扫描按敌人组件去重 | 字段创建时分配；每次扫描前 Clear，禁用时 Clear |
| scanCooldown | float | 光环自己的查询计时，与普通攻击间隔不同 | OnEnable / OnDisable 设为 0；Update 减少，扫描后赋 scanInterval |
| sourceId | int | 本塔减速来源编号 | Awake 记录 GetInstanceID()；敌人回池不会改变它 |

```csharp
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerController))]
public class FibrinSlowAura : TowerAttackBehaviour
{
    [SerializeField, Range(0f, 0.95f)] private float slowFraction = 0.4f;
    [SerializeField, Min(0.01f)] private float scanInterval = 0.1f;
    [SerializeField, Min(0.01f)] private float refreshDuration = 0.25f;
    [SerializeField] private LayerMask enemyLayerMask;

    private TowerController tower;
    private ContactFilter2D targetFilter;
    private readonly List<Collider2D> candidates = new List<Collider2D>(64);
    private readonly HashSet<EnemyController> visited = new HashSet<EnemyController>();
    private float scanCooldown;
    private int sourceId;

    public override bool CanAttack => false;

    private void Awake()
    {
        tower = GetComponent<TowerController>();
        sourceId = GetInstanceID();
        targetFilter.SetLayerMask(enemyLayerMask);
        targetFilter.useTriggers = true;
    }

    private void OnEnable() => scanCooldown = 0f;

    private void OnDisable()
    {
        scanCooldown = 0f;
        candidates.Clear();
        visited.Clear();
        // 敌人端的效果按 refreshDuration 到期，不在这里强行恢复全体速度。
    }

    private void Update()
    {
        if (tower == null || !tower.isActiveAndEnabled ||
            tower.Definition == null || Time.deltaTime <= 0f) return;

        // TODO：推进扫描冷却，到期时调用 RefreshAura。
    }

    private void RefreshAura()
    {
        // TODO：校验层、scanInterval、refreshDuration 和 slowFraction。
        // TODO：清空临时集合；用 Physics2D.OverlapCircle 查询 AttackRange。
        // TODO：GetComponentInParent<EnemyController>，过滤失效目标与中心距离。
        // TODO：visited.Add(enemy) 成功后，取得 EnemySlowState。
        // TODO：调用 state.ApplySlow(slowFraction, sourceId, refreshDuration)。
        // 缺少状态组件应报告 Prefab 接线问题，避免悄悄当作已经减速。
    }

    public override void Attack(
        EnemyController target, float damage, ProjectilePool projectilePool)
    {
        // 光环由自己的 Update 刷新；CanAttack=false 阻止普通攻击启动。
    }
}
```

SerializeField / Min / Range 用于 Inspector 配置约束，运行逻辑仍应处理不合法配置。单次扫描中先按目标存活状态与中心距离过滤，再按 EnemyController 去重；不能只按碰撞体去重。Physics2D.OverlapCircle 的 List 重载在现有 TowerController 已使用，可参考该实现。

#### 15.3 Review 后接入的具体位置

以下保留最初的接线契约；移动和初始化代码及第四个菜单现在已经接入，剩余 Unity 资源操作见第 16 节：

1. 敌人 Prefab 挂 EnemySlowState，保留现有组件和 .meta。
2. PathFollower 增加可选的 EnemySlowState 缓存引用，在新增 Awake 中 GetComponent；Move 读取其 SpeedMultiplier。没有该组件时减速倍率用 1，兼容旧 Prefab。速度计算保留现有 moveSpeed 与 speedMultiplier，再乘新增减速倍率。
3. EnemyController 缓存可选的 EnemySlowState，在 Initialize 中显式 ResetState。OnEnable / OnDisable 清理保障普通池化，Initialize 清理覆盖不经过禁用的重新初始化。ResetState 不改变 PathFollower 的关卡 speedMultiplier。
4. 复制合适的塔 Prefab，移除旧特殊策略，挂唯一 FibrinSlowAura；TowerController 的 samplesAntigenOnHit 关闭。两个组件的 Enemy Layer Mask 都要配置：目前控制器初始化仍校验其自己的层。
5. 创建新的 TowerDefinition，配置价格、范围和新 Prefab。减速策略 RequiresDamage=false，Damage 可填 0；AttackInterval 仍需 > 0，先用合法占位值，不把它当成扫描频率。CanAttack=false 不会绕过 Initialize 校验。升级不会自动增强减速。全局初始化仍需有效 ProjectilePool 与 BuildSlot。
6. 扩展第四个菜单和快捷键，后由用户做 Unity 操作与 Play 验证。

用户验证时重点观察：第二关 40% 减速为 0.9；离圈 / 塔禁用后按有效期恢复 1.5；同强度不叠乘；强效果退出后弱效果仍生效；死亡回池再出生不保留旧减速；多碰撞体不重复应用；暂停恢复不计入暂停时长。光环不保存延迟敌人引用，也不更改 Stop / Begin 的核心抵达状态。

当前下一步：由用户在 Unity 完成敌人组件、新塔 Prefab / Definition 与两张地图第四项的接线，再做 Play 验证，具体见 [减速塔接线](SLOW_TOWER_SETUP.md)。

### 16. 授权 AI 完成后的减速代码交付（2026-10-14）

用户已明确授权 AI 完成剩余实现。此次完成 FibrinSlowAura 的扫描计时、配置校验、范围查询、中心距离过滤、敌人去重和效果刷新；保留用户的 EnemySlowState 来源字典与最强效果规则，简化重复赋值、公开 ResetState、补有限数值校验，并避免没有过期来源时重复重算。原 scanlnterval 字段改为 scanInterval，使用 FormerlySerializedAs 保留现有序列化数值，现有脚本 .meta 保留。

PathFollower.Move 已乘入可选的减速倍率；EnemyController.Initialize 在每次入场重置旧效果。没有减速组件的旧敌人仍用原速度，第二关 0.75 的关卡倍率独立保留。FibrinSlowAura.CanAttack=false 使该塔不执行普通射击，光环 Update 独立查询圈内所有有效敌人。

OpenFieldPrototypeController 已增加数字键 4 和第四个菜单按钮，按钮按窗口宽度换行，开战按钮与地图指针排除区域使用对应布局。现有三塔配置和资产未被替换。

当前完成状态是代码接入，尚未创建 / 修改塔 Prefab、Definition、场景引用或敌人 Prefab，也没有运行 Unity、构建、Play 或 Profiler。用户需按 [减速塔接线](SLOW_TOWER_SETUP.md) 完成资源操作。价格、平衡和运行效果待试玩，静态检查不能代替运行验收。

### 17. 剩余两塔的基础代码交付（2026-10-14）

用户继续授权先实现剩余塔逻辑。已加入 AcidAttackBehaviour：发射已有子弹，命中后登记固定落点酸区，按时间进行范围伤害；不同酸区允许叠加。NeuralPulseAttackBehaviour：保留控制器的主目标，先快照整条邻近连锁，再按版本校验结算伤害，默认三目标、20% 跳跃衰减，并调用 EnemyStunState 短暂停顿。

EnemyStunState 使用停顿结束时间与恢复窗口，不通过 PathFollower.Stop / Begin 重启路径。PathFollower.Update 在停顿中跳过移动，EnemyController.Initialize 清理复用前的停顿；无该组件的旧敌人移动不变。伤害统一走 EnemyController.TakeDamage。

策略基类新增 Initialize 与 RequiresProjectile，TowerController 显式初始化策略并尊重其启用状态。抗体和酸化声明需要子弹配置；减速塔的过滤器改在策略 Initialize 中准备，留空层时继承控制器层。范围查询复用集合并去重，酸区伤害批次和神经链保存 SpawnVersion。菜单与数字键扩为 1–6。

本次是当前战斗模型上的基础效果，不包括旧案的生物膜、群落传导 / 打断、炎症或六塔 LV2 / LV3 全套分支，也没有创建 VFX 或改动图片。Prefab、配置、场景引用和 Play / Profiler 验证仍由用户完成；未构建。具体参数、叠加规则、生命周期与接线见 [剩余塔逻辑与接线](REMAINING_TOWERS_SETUP.md)。

### 18. 抗体标记改为纯辅助（2026-10-14）

用户确认增加 15% 易伤，并要求标记塔单独不能输出。AntibodyAttackBehaviour.RequiresDamage=false，不读取传入 damage；没有对应抗原记忆时不发射，有记忆时使用 Projectile.InitializeMark。现有 InterceptorTowerDefinition 的 Damage 已由 40 改为 0，价格、射程、间隔和资源引用保留。普通伤害子弹仍要求 damage > 0，纯标记子弹命中时不调用 TakeDamage，避免 Health 的零伤害异常和虚假受击反馈。

EnemyImmuneState.MarkDamageBonusPercent 固定为 15；DamageTakenMultiplier 在有效标记期间返回 1.15，其余时间返回 1。EnemyController 缓存该组件，在 TakeDamage 中统一乘算一次，所以近卫 25 变为 28.75，酸区和神经连锁也能受益。重复标记只延长结束时间，易伤不叠加；到期立即按游戏时间失效，禁用与每次 EnemyController.Initialize 都清除状态。旧标记子弹继续校验目标引用、活跃状态和 SpawnVersion，不得标记复用后的新敌人。

近卫的 8 秒抗原记忆、标记的 4 秒持续时间、巨噬 20 / 50 的吞噬门槛仍按现有组件配置。纯辅助策略的 CurrentDamage 始终为 0，已有普通伤害升级不会增强易伤或标记时长。前排选敌与承诺目标规则保留；未知抗原的前排敌人不会被跳过，未发射的尝试仍消耗冷却。

待用户在 Unity 中验证：

1. 单独部署标记塔：可以完成初始化，无零伤害报错，敌人生命不下降，也不获得标记。
2. 近卫与标记塔在 3 格信号半径内：近卫命中后传抗原，标记子弹命中只出现标记，不减少生命；后续近卫每次基础 25 伤害实际扣 28.75。
3. 多个标记塔命中同一敌人，后续近卫扣血仍为 28.75；停止刷新后等待 4 秒，普通伤害恢复 25。
4. 标记中敌人被击杀并回池后，同实例重新出生无标记和易伤；旧子弹不能标记新出生实例。
5. 巨噬仍能吞噬不高于 50 血的标记目标；酸区和连锁按各敌人结算时的标记状态增伤，未标记敌人保持原伤害。

以上为静态核对和待验步骤；本次没有执行 Unity、Play、构建或 Profiler。

### 19. 第三关与三关空位选塔（2026-10-14）

主线新增 OpenFieldLevel03，从第二关胜利 Next 进入。第三关引用已有左右入口 / 双竖短墙地图，加入六塔 Definition；三种新塔的独立 Prefab 各挂唯一策略，第三关独立敌人副本加入减速与停顿状态。第一、二关保留原三种塔。

OpenFieldPrototypeController.selectedBuildCell（Vector2Int?）保存用户点击的有效空格，hoveredBuildIndex（int）只决定该格子的预览。OpenFieldBuildMenu 创建 UGUI 图标 / 价格卡片，使用传入的可用、选择、悬停和取消回调；它不持有 EconomyService，也不自己改变地图。选择回调经 TryBuildSelectedChoice → TryBuildAt 走原扣费 / 初始化 / 占用事务，成功后关闭选择；直接 TryBuildAt 接口保留用于既有代码调用。

空格选择在右键、Esc、取消、成功建造、开战、终局和 OnDisable 清除；字体来自三场景的 buildMenuFont。地图输入同时检查菜单矩形、工具栏及 EventSystem，避免按钮点击穿透。空格悬停可达结果缓存到移格或占用变化，实际打开和提交仍重新检查，未测性能收益。接线参数与用户 Play 步骤见 [三关试玩说明](OPEN_FIELD_THREE_ROUND_PROTOTYPE.md)。

### 20. 酸区范围显示（2026-10-14）

用户指出酸化炮命中后看不到范围。现已补 AcidFieldVisual：只画半透明酸液圆和边界，不负责伤害。AcidField 同时保存 Center、Radius、ExpiresAt 和 Visual；查询和显示使用同一半径快照。CreateAcidField 从塔持有的闲置栈取得显示对象，每帧按游戏时间更新透明度，到期隐藏；ResolveFields 移除记录时归还显示实例，CancelEffects 归还仍有效的实例。默认预热两个，不足时扩展。

塔持有单位圆 Mesh、共享 Material 和单位缩放的独立场景根；显示层位于地图之上、敌人 / 塔之下，不受塔的 Sprite 缩放影响。OnDisable 清场；OnDestroy 释放场景根与图形资源。没有新增图片文件，原素材及 .meta 保留。可见性、范围对齐、暂停、连续命中和场景重开仍需用户 Play 验证；没有执行 Unity 或构建。

### 21. 八种敌人代码接入（2026-10-14）

后续 v0.14（2026-10-15）已接入有限壁垒、细胞承伤和容量接敌。调用链为 TryBuildAt → TowerCombatState.Initialize → PathFollower.BeforeMove → EnemyContactCombat.TryMove / Tick → TowerCombatState.TakeDamage → 立即禁用细胞 → HandleDefenderDied 释放 BuildSlot / 建造格 / 信号邻接。壁垒额度只在成功建造后消耗，不随奖励或换轮增加；普通细胞格仍可寻路。敌人退出 / 禁用 / 重新初始化释放旧接敌关系，SpawnVersion 防旧版本释放新名额。具体字段、参数和用户测试步骤见 [v0.14 接入说明](LIMITED_ROUTE_DEFENSE_IMPLEMENTATION.md)。原敌人说明保留如下。

用户已授权实现敌人设计。EnemyController.Initialize 现在设置定义速度、运行时抗原和防御 / 控制，再初始化 EnemyBehaviour 派生组件，最后允许被攻击；同一入场递增 SpawnVersion。EnemyDamageContext 把酸性直击和跳伤交给同一入口，先破壳、再减伤 / 一次易伤 / 吸盾 / 扣生命。TryEngulf 是无盾且符合门槛时的单独处决，保留原奖励和退出链，不用普通受减伤的伤害模拟吞噬。

BossPhaseController 订阅生命变化只记录最低生命比例，Tick 使用游戏时间按顺序执行预告和阶段，治疗不会让已跨过的阶段丢失。三种能力各自负责有限召唤 / 支援 / 抗原变异；延迟执行前检查母体存活与版本。EnemySpawnRequest 保存当前路径、坐标和下一路点，关卡注册并 InitializeAt 子单位，包含在清场计数中；不同混合组共享入口轮转索引。

八种独立资源和九轮混合波次已接入三关，旧资源保留。EnemyVisualState 管状态图和能力脉冲，EnemyStatusHud 管复用血条 / 盾条、阶段和种类提示。新增 14 个 EditMode 边界测试，静态语法 / 类型与资源引用核对通过，尚未运行 Unity 或测试 Runner；细节见 [敌人接入与验证](ENEMY_IMPLEMENTATION.md)。
