# 《胞卫战》热更新学习路线

状态：课程大纲；热更新功能尚未实现。记录日期：2026-10-17。

源码依据：Unity 2022.3.53f1c1，分支 `codex/open-field-prototype`，HEAD `da0c60e`，并包含当前工作区未提交的六塔、战中建造、HUD 和购售改动。保留原 `Docs/tutorial.md` 的塔攻击课程；其中历史玩法说明不代表当前完整实现。

学习目标：以同一个塔为练习对象，依次完成数值配置更新、资源更新和 C# 逻辑更新。先理解链路，再逐项实现。本次只记录路线，未安装包、修改玩法、运行 Unity 或构建。

前置基础：C# 类与接口、JSON、协程或异步操作、文件读写、HTTP、MonoBehaviour 生命周期、ScriptableObject。程序集阶段还需学习 asmdef、AOT、IL2CPP、反射和代码裁剪。

## 先区分三种更新

| 种类 | 本项目练习 | 能力边界 |
| --- | --- | --- |
| 配置更新 | 近卫伤害 25 → 30 | 已有程序读取新数据；无法仅靠数据增加任意新算法 |
| 资源更新 | 替换一个塔的选择图标 | 下载并加载新资源；AssetBundle 自身不包含可执行 C# 代码 |
| 代码更新 | 改变一种攻击效果的算法 | 需要预先集成支持加载热更新程序集的运行时及稳定接口 |

拟采用 JSON → Addressables → HybridCLR 的学习顺序。Addressables 负责资源；HybridCLR 负责 IL2CPP 下的 C# 热更新。接入时再锁定与当前编辑器匹配的具体包版本，不能直接安装任意最新版。

## H01：一次建塔如何把配置变成攻击伤害

状态：大纲；现有链路，可直接阅读。

读完能回答：修改伤害应进入哪个配置层，为什么不能把每座塔的状态存到共享资产上？

主链：玩家选塔 → `OpenFieldPrototypeController.TryBuildAt` → 实例化塔 → `TowerController.Initialize` → `CurrentDamage` → `Attack` → 默认子弹或特殊攻击效果。

必读顺序（以下路径均相对工程根目录）：

1. `Assets/_Project/Scripts/Data/TowerDefinition.cs`：输入 Inspector 配置，输出 Prefab、Damage、AttackInterval 等只读属性，供建造和初始化读取。
2. `Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs` 的 `TryBuildAt`：输入格子与已选定义，检查建造条件、扣费并初始化实例；成功返回 true，失败按现有路径清理。
3. `Assets/_Project/Scripts/Towers/TowerController.cs` 的 `Initialize`、`CurrentDamage`、`Attack`：输入共享定义和实例依赖，初始化冷却与目标；目前 `CurrentDamage` 直接读取 `Definition.Damage` 并应用升级倍率，再交给攻击分支。
4. `Assets/_Project/Scripts/Towers/TowerAttackBehaviour.cs` 的 `Initialize`、`CanTarget`、`Attack`：输入已确认目标与伤害，输出具体技能效果；不负责下载或版本检查。

选读验证：`Assets/_Project/Tests/PlayMode/TowerAbilityPlayModeTests.cs`，观察现有攻击效果如何断言。暂缓资源下载和 DLL；分别归 H03、H04。

## H02：外部 JSON 怎样改变下一局近卫伤害

状态：大纲；配置加载链路待实现。第一项动手任务。

读完能回答：从读文件到伤害生效经过哪些步骤，坏配置如何回退？

拟实现主链：进入游戏前 → 加载内置默认值 → 读取缓存 JSON → 解析并完整校验 → 生成本次会话的有效配置 → 创建塔时传入运行时数值 → 下一局攻击使用新伤害。无缓存、解析失败或数值非法则使用默认配置。

阅读及实施顺序：

1. 阅读 H01 的 `TowerDefinition` 与 `TowerController.CurrentDamage`：确定默认值来源和伤害消费位置。
2. 新增配置 DTO 和加载器（尚无对应源码）：先只处理稳定塔 ID、版本、伤害。禁止用显示名或列表位置充当长期 ID；校验重复 ID、NaN、Infinity 和越界数值。
3. 新增运行时配置快照（尚无对应源码）：把默认值与有效覆盖合并；避免直接修改磁盘 ScriptableObject 或让战中共享资产变化影响所有实例。
4. 调整初始化与伤害读取入口（待实现）：创建塔前取得快照；实例初始化读取它，重新开局重新装载。首版不做战中立即变更，价格和退款也不在本实验范围内。

拟验证：默认 25；有效 JSON 改为 30 后重新开始一局；损坏 JSON、未知 ID、负数和缺文件均按约定回退；磁盘 TowerDefinition 保持原值。外部文件解析练习通过后，再加入 HTTP 下载、超时与缓存。

暂缓升级全表和波次热配置；先让一座塔的一个数值走通。

## H03：从本地替换图标走到远程资源更新

状态：大纲；当前未安装 Addressables，远程加载待实现。

读完能回答：为什么改 Assets 中的图片不会自动更新到已安装的游戏？

拟实现主链：启动更新入口 → 检查远程目录和依赖 → 下载所需 Bundle → 按地址加载图标 → 显示在选塔按钮 → 使用结束释放句柄；网络失败按明确的缓存或内置回退策略进入游戏。

阅读及实施顺序：

1. `Assets/_Project/Scripts/UI/OpenFieldUiResources.cs` 的 `LoadDefaults`：输入固定 Resources 名称，输出随基包打入的默认配置；当前不是远程加载。
2. `Assets/_Project/Scripts/Data/TowerDefinition.cs` 的 `BuildIcon`，及 `Assets/_Project/Scripts/UI/OpenFieldBuildMenu.cs`：定位配置图标到按钮的消费位置。
3. 新增独立资源加载入口（待实现）：先用一个地址加载一个实验图标，再接到 UI；原图及 .meta 保留。
4. 配置远程 Catalog、构建平台对应的资源包并保存发布内容状态（待实施）：首次版本随基包准备，后续只发布资源更新。发布时资源先就位，再使新目录可见。

拟验证：第一次联网取得新版图标；再次启动能使用缓存；断网、缺资源、下载中断有清晰结果；反复开关选塔框没有句柄泄漏。此阶段需要资源构建，完整验证还需 Player 基包，届时由用户执行 Unity 操作。

暂缓更新整个地图和全部 Prefab，避免一开始迁移大量直接引用。DLL 加载归 H04。

## H04：让一个攻击效果来自 HotUpdate.dll

状态：大纲；HybridCLR、程序集拆分及加载入口均未实现。

读完能回答：资源管理器下载 DLL 后，谁执行它，哪些接口仍必须留在基包？

拟实现主链：基包启动 → 确认资源及代码版本兼容 → 下载并校验更新 → 按 HybridCLR 要求准备 AOT 元数据 → 加载热更新程序集及依赖 → 调用约定入口 → 注册攻击逻辑 → 加载依赖这些类型的资源 → 进入游戏。

阅读及实施顺序：

1. `Assets/_Project/Scripts/Towers/TowerAttackBehaviour.cs`：分析现有扩展点，以及它依赖的 TowerDefinition、EnemyController、ProjectilePool 类型。
2. 盘点 asmdef 及共享类型（待重构）：当前大部分玩法在 Assembly-CSharp；单独新增 asmdef 不能直接引用预定义 Assembly-CSharp 中的类型。需设计基包契约程序集与热更新程序集，禁止基包反向静态依赖热更新实现，避免循环引用。
3. 固定接口与一个最小入口后，再安装、初始化匹配版本的 HybridCLR（待实施）；先跑独立 Hello 验证，再接一个攻击效果。不要一开始迁移所有脚本。
4. 构建支持热更新的 IL2CPP 基包（待实施），部署 v1；仅修改并发布兼容的 v2 热更产物，重启游戏后验证新效果，基包不重新安装。

拟验证：目标平台 IL2CPP Player 能执行新版逻辑、没有使用旧 DLL，断网能使用上次完整版本，资源引用的脚本类型能正确解析。Editor Play 成功不足以证明这一步通过。代码裁剪、泛型 AOT 和元数据需要按所选 HybridCLR 版本检查。

此处“更新”首版规定为下一次启动加载；不承诺在同一进程内替换已经加载的程序集。Unity 引擎、原生插件或不兼容的基包接口变更，仍可能要求发布新基包。

## 最终链路与范围

拟实现：启动 → 检查版本和基包兼容性 → 下载 → 校验 → 激活一组完整版本 → 读取配置 / 加载资源及代码 → 打开选关 → 正常游玩。

后续再练：发布清单、临时文件与原子切换、失败重试、缓存配额、回滚、平台差异。摘要或哈希用于完整性检查，不等于可信来源认证；生产发布还需保护更新来源。线上商店分发要求不在本课程当前范围。

若某文件或原理看不懂，请继续追问 AI；本路线先提供学习顺序，不替代每课的具体实现讲解。

## 官方参考

- [Unity Addressables 内容更新](https://docs.unity.cn/Packages/com.unity.addressables@1.22/manual/content-update-builds-overview.html)：资源目录、增量更新、内容状态文件和代码更新边界。1.22 文档用于学习概念，不代表已经为项目选定包版本。
- [HybridCLR 快速上手](https://www.hybridclr.cn/docs/beginner/quickstart)：2022.3 支持范围、IL2CPP、程序集配置和动态加载。当前页面为 9.x，接入前需核对具体编辑器发行版本和依赖。
