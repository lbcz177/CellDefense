# 《细胞防卫战》Unity 求职知识与学习路线

> 文档状态：动态能力档案 v1.1  
> 基线日期：2026-08-30  
> 当前项目：Unity 2022.3.53f1c1  
> 原则：掌握等级只按代码、调试记录和口头解释更新，不按“看过教程”更新

## 0. 结论

你不需要现在独立设计整个项目架构。AI 先提供模块边界、数据流和迁移脚手架；你通过亲自实现关键模块、亲自定位 Bug、亲自解释取舍，逐步取得架构设计能力。

这个项目要证明的不是“用过最多 Unity 包”，而是：

1. 能用 C# 和 Unity 生命周期完成一个可发布的 2D 游戏。
2. 能把 ScriptableObject 配置、运行时状态、UI 和表现分开。
3. 能在固定塔位、波次、状态和大量敌人场景中做清楚的模块设计。
4. 能用 Profiler 在真机找到 CPU、GPU、内存或 GC 瓶颈，并展示优化前后证据。
5. 能调试序列化、Prefab 引用、事件生命周期、池化复用和同帧边界问题。
6. 能使用 Git 安全迭代、阅读陌生代码、写技术文档，并在面试中讲清自己的贡献。

自动化测试不只是测试岗位的工作。开发者不需要学习完整 QA 流程，但应该给“纯规则、高风险、容易回归”的代码写少量测试。它能迫使架构可测试，也能在重构时保护最重要的规则。

---

## 1. 2026 年岗位样本告诉我们的事情

这不是全球职位统计，只是 2026-08-10 仍可访问的一组职位样本，用来校准学习优先级：

- Battle Creek Games 的 Unity 工程职位同时强调 Unity/C#、Prefab/Scene/资产基础、可维护代码、Code Review、调试以及移动端 FPS/内存优化：[Senior Software Engineer, Unity](https://jobs.ashbyhq.com/battle-creek-games/33e45f61-d35b-4ed7-b6b9-8e85235b11ab/)。
- Amanotes 的移动 Unity 职位要求能阅读和改进已有项目，处理 CPU/GPU/内存/加载/渲染问题，并了解 Sprite、动画、材质、Prefab、音频与 VFX：[Senior Game Developer - Unity](https://jobs.lever.co/amanotes/47294f4a-d875-4701-8c2a-879f8a51550a)。
- Voodoo 的职位把稳健可维护的 Unity/C#、游戏手感、UI、动画、粒子、移动端性能、崩溃/ANR 与 CI/CD 列在同一工作范围：[Senior Game Developer - Helix Jump](https://jobs.ashbyhq.com/voodoo/4be9f0d2-9ee4-4f31-acf8-1cbdfd3e688c/)。
- Azra Games 的性能岗位进一步强调 Unity Profiler、Xcode Instruments、Android Profiler、CPU/GPU/内存、Shader、Addressables、资产预算和内容工具：[Performance Engineer](https://builtin.com/job/performance-engineer/6652020)。这是进阶方向，不应被误当成初级岗位每项都必须精通。
- Argentics 的 Unity 招聘页把 Unity/C#、OOP/设计模式、2D/3D、移动平台、性能、技术文档和 Git 放在一起：[Careers](https://www.argentics.io/careers)。
- Joyteractive 的 Lead 职位包含 Unity 6/URP、架构、Memory Profiler、Frame Debugger、Addressables、AssetBundles、SDK、客户端/服务端和 CI/CD：[Lead Unity Developer](https://jobs.ashbyhq.com/joyteractive/8bd82f98-bf31-4dca-b90f-eb67cac8e5a8)。这些更适合作为长期拓展，而非当前三关 Demo 的前置条件。

从这些样本可以合理推断：对求职最有用的组合是“扎实 Unity/C# + 完整功能交付 + 调试和性能证据 + 可维护代码”，而不是只做算法题或只堆设计模式。

---

## 2. 引擎版本策略

截至 2026-08-10，Unity 官方页面显示 Unity 6.3 LTS 为当前 LTS，支持到 2027 年 12 月；Unity 6.0 LTS 支持到 2026 年 10 月：[Unity 6 发布页](https://unity.com/releases/unity-6)、[Unity 6 支持策略](https://unity.com/releases/unity-6/support)。

本项目不立即升级：

1. 先在现有 2022.3.53f1c1 完成第一关纵向切片，避免把学习目标变成版本迁移排错。
2. 第一关稳定后复制项目或建立独立分支，做一次 Unity 6.3 LTS 升级演练。
3. 记录 Package、API、输入、渲染、序列化和构建差异。
4. 升级版稳定且收益明确，再决定主线是否迁移。

这样既能展示维护稳定版本的判断，也能获得当前 LTS 迁移经验。不要用原项目唯一副本直接升级。

---

## 3. 掌握等级定义

| 等级 | 标准 |
|---:|---|
| 0 | 未接触，无法说明用途 |
| 1 | 听过/看过，能跟随步骤完成，但不会独立排错 |
| 2 | 能在小功能中使用，遇到边界或生命周期问题需要提示 |
| 3 | 能独立设计和调试项目级功能，并解释常见取舍 |
| 4 | 能评审他人方案、做性能/架构权衡，并用证据教学或复盘 |

升级掌握等级需要至少两项证据：

- 独立完成代表功能。
- 定位并修复一个非语法 Bug。
- 写出或画出数据流。
- 给规则写测试并解释边界。
- 在 Profiler 中找到并验证瓶颈。
- 不看 AI 回答 3 个追问。

---

## 4. 当前能力档案

这张表根据目前自述建立，是初始假设，不是考试结果。

| 知识 | 当前 | 三关目标 | 本项目证据 | 默认责任 |
|---|---:|---:|---|---|
| C# 基础、类、继承 | 2–3 | 3 | 塔/敌人职责，普通 C# 规则类 | 【你主导】 |
| 接口与组合 | 1–2 | 3 | `ITargetingStrategy`、攻击行为与池契约 | 【你主导】 |
| 委托与事件 | 1–2 | 3 | 经济/生命/波次事件，正确订阅与取消 | 【你主导】 |
| 泛型 | 1–2 | 2–3 | 有边界的对象池或结果类型 | 【你主导】 |
| 协程 | 1–2 | 3 | 波次流程、取消、暂停与对象禁用 | 【你主导】 |
| Unity 生命周期 | 1–2 | 3 | Awake/OnEnable/Start/OnDisable 的初始化证据 | 【你主导】 |
| ScriptableObject/序列化 | 1–2 | 3 | 重建 TowerData、OnValidate、Definition/State 分离 | 【你主导】 |
| Prefab/Variant | 2 | 3 | 六塔、敌人和 VFX 的稳定公共结构 | 【协作完成】 |
| UGUI/RectTransform | 1–2 | 3 | 横屏 HUD、Safe Area、Anchor/Pivot 自适应 | 【你主导首套】 |
| 输入 | 1 | 2–3 | 鼠标/触控统一意图、UI 遮挡、取消操作 | 【协作完成】 |
| 2D 物理/射线 | 1–2 | 2–3 | 塔位点击、LayerMask、NonAlloc 查询取舍 | 【你主导】 |
| Animator/Timeline/VFX | 1–2 | 2–3 | 塔攻击、受击、Boss 阶段演出 | 【协作完成】 |
| 架构与设计模式 | 1 | 3 | State/Factory/Strategy/Observer 的真实使用与反例 | 【你主导实现】 |
| Debug | 1–2 | 3 | 可复现步骤、调用链、断点、日志、根因记录 | 【你主导】 |
| Profiler/优化 | 1 | 3 | 大量敌人前后对比和真机数据 | 【你主导】 |
| 自动化测试 | 0–1 | 2 | 10–20 个核心规则测试 | 【你主导关键用例】 |
| Git | 1–2 | 2–3 | 分支、提交、冲突、回退策略、LFS 概念 | 【AI 操作 + 你理解】 |
| Android/iOS 构建 | 0–1 | 2 | 至少 Android 真机；iOS 完成流程文档/条件允许实机 | 【协作完成】 |
| 技术文档/面试表达 | 1–2 | 3 | README、架构图、性能报告、Bug 复盘 | 【你主导讲解】 |

每个里程碑结束后，只改“当前”列和证据链接。未实际实现的设计文档不能单独把等级提升到 3。

---

## 5. 必须优先掌握的 P0 知识

### 5.1 Unity 对象与生命周期

你需要能回答：

- `Awake`、`OnEnable`、`Start` 的顺序和职责如何划分？
- 池化对象为什么可能多次触发 OnEnable/OnDisable，但通常只 Awake 一次？
- 事件订阅放错生命周期为什么会重复回调或内存泄漏？
- 被禁用 GameObject、被销毁 Unity Object 和普通 C# `null` 有何调试差异？
- 协程依附哪个对象？禁用/销毁/暂停分别会怎样？

项目证据：敌人进出池、波次重开、切场景和暂停后均无重复奖励或残留订阅。

### 5.2 序列化、ScriptableObject 与 Prefab

新工程将在 M5 第一次正式定义 `TowerLevelStats` 和 `TowerDefinition`，届时用干净资产完成序列化实验：

- 哪些字段 Unity 会序列化，`[System.Serializable]` 的作用是什么？
- `struct` 和 `class` 在 Inspector、复制语义和运行时修改上有何区别？
- 为什么静态 Definition 不能保存本局生命/等级？
- 为什么不能手工新建 `.asset` 文本或复制 GUID？
- 如何用 `CreateAssetMenu`、`OnValidate` 和自定义检查减少错误？
- Prefab、Prefab Variant、Scene 实例和 ScriptableObject 引用分别适合什么？

项目证据：从 Unity 菜单新建第一份哨兵塔 Definition，费用只有一个来源，Inspector 修改、保存、重启后仍正确；不再以旧工程异常资产的迁移作为学习任务。

### 5.3 数据流与可维护 C#

亲自实现：

- `BuildController.TryBuild` 的验证、扣款、生成、提交/回滚顺序。
- `EnemyExitResolver` 的死亡/泄露/清场互斥。
- `GameStateMachine` 和允许操作表。
- 路径进度锁敌策略。
- 伤害与反应结算的普通 C# 逻辑。

需要理解的模式不是背定义：

| 模式/原则 | 项目中的真实问题 | 过度使用警告 |
|---|---|---|
| State | 游戏流程和 Boss 阶段 | 不要每个 bool 都做状态类 |
| Strategy | 不同锁敌/攻击选择 | 只有一个实现时先不用接口套娃 |
| Factory | 定义到 Prefab/运行时状态的创建 | 工厂不能偷偷成为全局 Service Locator |
| Observer/Event | 经济、生命、波次通知 UI | 不做不可追踪的万能 EventBus |
| Object Pool | 高频敌人、VFX、投射物复用 | 回池契约和容量比“有池”更重要 |
| Composition Root | 显式连接本局依赖 | 当前规模不需要先装第三方 DI |

### 5.4 Debug 方法

每次 Bug 记录六项：

```text
现象：玩家看到了什么
最小复现：从空场景/新局开始的步骤
预期与实际：差异是什么
第一条错误证据：Console、断点、Inspector 或 Profiler
根因：哪条不变量被破坏
修复与防回归：改了什么，怎样证明没有副作用
```

优先工具顺序：Console 堆栈 -> Inspector 运行时状态 -> 条件断点/调用栈 -> 最小日志 -> Frame Debugger/Profiler/Memory Profiler。不要一开始到处打印日志或整段重写。

### 5.5 性能分析

按 [PERFORMANCE_MANY_ENEMIES.md](PERFORMANCE_MANY_ENEMIES.md) 完成：压力场景、目标设备采样、Marker、瓶颈定位、一次结构性优化和前后对比。官方也把设备上分析、移动端构建、Profiler 与 Addressables 放入当前移动游戏学习资源：[Ship your first mobile game](https://learn.unity.com/collection/ship-your-first-mobile-game)。

---

## 6. P1：完整游戏交付知识

### 6.1 UI 与 RectTransform 细节

你提到不知道 Pivot 等细参数的实际用途，第一套 HUD 应由你亲自完成：

| 概念 | 实际用途 | 本项目练习 |
|---|---|---|
| Anchor | 子物体跟随父矩形哪一部分/范围 | 资源栏固定左上，波次栏固定右上，底部塔栏横向拉伸 |
| Pivot | 自身缩放、旋转和定位的参考点 | 塔详情从点击侧展开；血条缩放不漂移 |
| Anchored Position | 相对 Anchor 的局部偏移 | 不用固定屏幕坐标摆所有按钮 |
| Size Delta | 相对拉伸 Anchor 后的尺寸差 | 横向拉伸面板保留固定边距 |
| Canvas Scaler | 参考分辨率到当前屏幕的缩放 | 1920×1080 横屏到不同手机比例 |
| Safe Area | 避开刘海、圆角与系统区域 | 手机顶部资源栏和暂停按钮 |
| Layout Group | 按内容自动排版 | 塔卡、状态图标和结算统计 |
| Canvas 拆分 | 限制频繁变化导致的整体重建 | HUD、弹窗、世界血条分层 |

第一套完成后，AI 可以协助把同样规则机械复制到其他面板；你负责验收 Anchor、Pivot、导航、遮挡和分辨率。

### 6.2 输入与平台适配

- 第一阶段保留简单鼠标/触控适配层。
- 核心循环稳定后迁移 Input System，把点击、取消、暂停、镜头拖动/缩放表示成游戏意图。
- UI 上方点击不能穿透到塔位；触摸需要区分点击、拖动和双指缩放。
- PC 支持快捷键和 Hover；手机不能依赖 Hover。
- 输入禁用由 GameState 决定，不由各按钮自行猜测。

当前 Unity 6 的 Input System 手册入口可作为后续迁移参考：[Input System package](https://docs.unity3d.com/ja/current/Manual/com.unity.inputsystem.html)。

### 6.3 动画、VFX 与游戏手感

你不需要成为技术美术，但需要能完成：

- Animator 参数、Transition、Exit Time、Animation Event 风险。
- Tween/代码插值与 Animator 的适用边界。
- Sorting Layer、Sprite Atlas、共享材质和 Overdraw 基础。
- 命中停顿、镜头轻震、轮廓闪白、音效与数字反馈的优先级。
- Boss 阶段转换用状态机负责规则，Timeline 只负责编排演出。

### 6.4 存档、本地化与构建

- JSON DTO、版本号、默认值、迁移、损坏恢复和安全写入。
- 不直接序列化 MonoBehaviour/ScriptableObject 运行时引用。
- 中文先完成，第三关阶段接入 Localization 表；代码不硬编码玩家文本。
- Windows 构建、Android IL2CPP 真机构建、分辨率/帧率/权限检查。
- iOS 若当前无 Mac，至少理解 Xcode 导出、签名和真机分析流程，不伪造“已发布 iOS”。

---

## 7. P2：按岗位方向选择的扩展

| 方向 | 可选知识 | 何时加入 |
|---|---|---|
| 移动客户端 | Addressables、SDK 封装、Android/iOS 生命周期、崩溃/ANR、热更新概念 | 三关稳定并有真实资源压力后 |
| 游戏玩法 | 更复杂 AI、寻路、关卡编辑器、行为树、战斗数值工具 | 有新玩法问题后 |
| 性能/引擎 | Jobs、Burst、DOTS、Memory Profiler、GPU 工具、Shader/HLSL | Profiler 已证明瓶颈后 |
| 图形/TA | URP、Shader Graph、渲染管线、内容校验、资源预算 | 正式美术进入后 |
| 网络/LiveOps | REST、WebSocket、账号、云存档、Remote Config、Analytics、CI/CD | 作为独立第四阶段，不阻塞 Demo |
| 工具 | EditorWindow、PropertyDrawer、AssetPostprocessor、自动校验 | 重复内容制作已经真实出现后 |

Lua/热更新、客户端服务端、广告/IAP、多人联网都很常见，但对当前个人三关塔防不是 P0。面试时宁可把核心系统讲深，也不要声称“接入过”却解释不了生命周期和失败处理。

---

## 8. 自动化测试应该学多少

### 8.1 你需要学的部分

目标不是替代 QA，而是保护开发者最容易改坏的规则。建议三关 Demo 总计先写 10–20 个高价值测试：

| 模块 | 代表用例 |
|---|---|
| Economy | 余额不足不扣款；一次购买只扣一次；出售按累计投入 |
| Damage | 护甲/易伤结算顺序；最低伤害；Boss 不被吞噬 |
| EnemyExit | 死亡与泄露同帧只处理一次；清场不发奖励 |
| GameState | Paused/Victory/Defeat 禁止建造与技能；结算只一次 |
| Wave | 最后生成和最后死亡不同顺序都能完成；提前开波奖励一次 |
| Status | 同类减速取强并刷新；控制抗性；回池后无残留 |
| Signal | 小图 BFS 结果；感染节点阻断；样本过期 |
| Adaptation | 相同快照/种子得到相同软适应；每波最多一个 |
| Save | 旧版本迁移；缺字段默认；损坏文件恢复 |

普通 C# 规则优先用 EditMode 测试。必须依赖 GameObject 生命周期、场景或协程时才用少量 PlayMode 测试。UI 像素、游戏手感和完整关卡仍主要依靠人工试玩。

### 8.2 AI 可以做什么

你先写规则、不变量和第一个代表测试；AI 可以扩展等价边界数据、生成重复用例和整理报告。测试失败时，你必须先阅读失败信息、定位根因，再让 AI Review。

---

## 9. Git 学习边界

AI 可以帮助执行机械命令，但你需要理解这些面试与安全概念：

- 工作区、暂存区、提交、分支和远端分别是什么。
- 为什么一个提交只做一类可描述的修改。
- Unity `.meta` 与 GUID 为什么必须随资源一起提交。
- 哪些目录不提交：Library、Temp、Logs、Obj、Build 等。
- Merge 与 Rebase 的差异；冲突时怎样确认两边意图。
- Revert 与 Reset 的区别，为什么共享历史优先使用 Revert。
- Git LFS 适合哪些大二进制资产，为什么不能把所有文件都放 LFS。
- 升级 Unity、批量重导入和移动大量资源前为何先建立可恢复点。

简历价值来自清晰历史、可复现版本和你能解释风险，不来自背大量命令参数。

---

## 10. 分阶段学习与作品集交付

### 阶段 A：建立首个纵向切片基线

当前进度：M1 固定路线、M2 敌人生命与退出互斥、M3 固定塔位一塔建造均已完成并通过 Play Mode 验证；下一步为 M4 ATP 与最小战斗闭环。

你主导：固定路径、敌人生命与退出原因、固定塔位、单一建塔入口、一座塔攻击、ATP 与胜负闭环。

交付物：

- 路径移动与单次终点事件的实现说明。
- 建造数据流图。
- 一个“同帧死亡/泄露”Bug 复盘。
- 5 个核心规则测试。

### 阶段 B：第一关纵向切片

你主导：GameState、Economy/Life、波次、锁敌、伤害、首条信号链。

协作：HUD、塔详情、动画/VFX 第一套规范。

交付物：

- 6–8 分钟完整关卡。
- 反应链与炎症教学录像。
- 技术架构图和关键类说明。
- Windows 可执行包。

### 阶段 C：第二关系统组合

你主导：多入口路径图、群落系统、状态叠加、动态软适应、存档版本化。

交付物：

- 两个入口与群落 Boss。
- 存档迁移测试。
- 大量敌人 Profiler 基线和第一次结构优化。
- Android 真机包。

### 阶段 D：第三关完成度

你主导：Boss 状态机、LV3 分支规则、性能结论和核心测试。

协作：Timeline、本地化、完整美术替换、移动端 UI 复制与验收。

交付物：

- 三关完整 Demo。
- 性能前后对比报告。
- 中英文 README、60–90 秒视频和 5–8 张截图。
- 一页“我的贡献/AI 协助/我能解释什么”。

### 阶段 E：独立扩展

从 Unity 6.3 LTS 迁移、Addressables、编辑器工具、Jobs/Burst 或网络中只选一个，先写问题、方案和验收，再由你提出初始架构。AI 从“给方案”退到“评审方案”。

---

## 11. 功能到面试证据的映射

| 项目功能 | 面试可讲知识 | 必须保留的证据 |
|---|---|---|
| 首个 `TowerDefinition` | Unity 序列化、SO、Definition/State、OnValidate | Inspector 配置、保存重启验证、非法数据检查 |
| 单一建造入口 | 事务、职责、依赖、错误结果 | Build 流程图和失败用例 |
| 信号网络 | 图、BFS、数据驱动地图、可视化 | 小图测试与 Inspector 配置 |
| 群落系统 | 聚合、分频、复杂度、玩法性能共同设计 | 朴素/优化复杂度和 Profiler |
| 炎症稳态 | 事件、状态区间、数值可观测性 | 来源统计和试玩调整记录 |
| 对象池 | 生命周期、Reset Contract、容量 | 峰值和回落数据、残留 Bug |
| 波次系统 | 协程、取消、完成条件、数据化 | 边界测试与多入口配置 |
| UI 自适应 | Anchor/Pivot/Layout/Safe Area | PC/手机截图和层级说明 |
| 存档 | DTO、版本迁移、容错、安全写入 | 旧档/损坏档测试 |
| 性能 | CPU/GPU/GC/内存、目标设备 | 前后相同场景对比 |
| Git | 原子提交、分支、冲突、Unity meta | 清楚的提交历史和一次冲突复盘 |

### 推荐的项目介绍句式

```text
我用 Unity 和 C# 独立完成了一款以免疫反应链为核心的 2D 塔防 Demo。
它不是让每座塔独立扫描并攻击，而是把固定塔位设计成信号图，病原体以群落聚合，
攻击、状态和视觉结算分离。我在 Android/PC 的可复现压力场景上用 Profiler 定位锁敌和分配问题，
通过注册表、分频调度和有上限的对象池完成优化，并保留了前后对比数据。
```

其中任何一句尚未真正完成或测量时，都不能提前放进简历。

---

## 12. 每个知识点的复盘模板

```markdown
### 知识点：
- 当前等级：
- 我亲自实现了什么：
- AI 帮助了什么：
- 我遇到的真实 Bug：
- 根因：
- 我用什么证据验证：
- 如果重做会改变什么：
- 面试官可能追问：
- 下一等级还缺什么：
```

---

## 13. 变更日志

| 日期 | 版本 | 变更 |
|---|---|---|
| 2026-08-10 | 1.0 | 建立当前岗位样本、能力档案、P0/P1/P2 路线、测试边界、AI 分工和作品集证据映射 |
| 2026-08-30 | 1.1 | 旧资产迁移任务退役；阶段 A 改为新工程纵向切片，并把序列化证据改为首次创建 Definition |
| 2026-08-30 | 1.2 | 记录 M2/M3 完成证据：互斥退出、事件生命周期、显式依赖、ScriptableObject/Prefab 与事务式建造 |

