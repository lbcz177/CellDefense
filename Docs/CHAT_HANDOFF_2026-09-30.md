# CellDefense 对话交接（2026-09-30）

## 项目与协作约定

- 工程：`E:\unity\CellDefense`，Unity `2022.3.53f1c1`；目标是完成可演示的细胞塔防作品，为 Unity 初级／客户端岗位求职做准备。
- 普通业务代码和接线可由 AI 完成，但需说明职责、数据流与验收；Unity Inspector、Prefab、场景和 Profiler 操作由开发者亲手完成。
- 对象池、关键状态机、确有用途的设计模式等面试重点可由开发者手写；动手前 AI 先按实际代码列变量名、类型、用途、读写与初始化／重置时机，不直接给完整核心算法。
- 当前优先推进游戏构筑，不把新增测试作为默认下一步；不要把开发者个人回答或评分写入 Notion。

## 本轮完成的功能与状态

1. 已有 `93f05dc feat: deploy phagocytes on road sites with attack feedback`：塔位区分 `TowerSite` 与 `RoadSite`，巨噬细胞可在路面部署；普通攻击闪蓝、实际吞噬闪橙。颜色按本次攻击是否触发吞噬决定，不是直接按目标是否有标记决定。
2. 新增抗原采样的一跳信号链代码：近卫塔的投射物命中时采样目标 `AntigenId`；其所在 `BuildSlot` 只向 `signalNeighbors` 中直接相连的塔位传递；相邻 B 塔按抗原类别缓存 8 秒。B 塔在缓存有效时优先选择未标记的同类目标，投射物命中后施加 4 秒标记；未掌握抗原时仍可普通攻击。重复采样刷新缓存。投射物回池时清空命中回调。
3. `Level02` 已保存一条有向连接：`TowerPlace/P002 → TowerPlace/P001`。按当前配置，需在 P002 建近卫塔、P001 建 B 塔才能传递；反过来不行。`Prototype` 尚未配置该连接，因此那里不能沿用原先 B 塔预知 A 类的效果。
4. 新建 `MainMenu` 场景和 `MainMenuController.StartGame()`，按钮以透明 UI 热区覆盖背景中画好的“开始游戏”；`SceneManager.LoadScene("Prototype")` 进入第一关。构建场景顺序为 `MainMenu → Prototype → Level02`，`MainMenu` 的 Canvas Scaler 为 `Scale With Screen Size`、参考分辨率 `1920×1080`。背景上其余三个按钮目前只是画面，没有功能。
5. 新增美术素材包，并由开发者开始替换 Prefab 图片。当前磁盘状态：`Enemy.prefab` 使用 `025_葡萄球菌群_基础`；`Tower.prefab` 和 `InterceptorTower.prefab` **都使用 `004_中性粒细胞哨兵_图标`**，B 塔尚未换成抗体 B 细胞图；`Tower 1.prefab` 使用 `020_巨噬细胞清道夫_图标`；`Projectile.prefab` 仍是原占位图。当前引用的三张角色图片 PPU 已设为 `1000`，主菜单背景 PPU 为 `100`。

## 关键数据流

`近卫塔投射物命中 → TowerController 读取 EnemyDefinition.AntigenId → BuildSlot.ShareAntigen 一跳传递 → AntibodyAttackBehaviour.RememberAntigen 缓存 8 秒 → B 塔发射时快照是否可标记 → Projectile 实际命中时 ApplyMark(4 秒) → EnemyImmuneState 更新敌人颜色 → 巨噬细胞按剩余生命与标记状态判断是否吞噬。`

当前吞噬门槛：普通目标剩余生命 `≤20`；已标记目标 `≤50`；吞噬后消化 3 秒。标记颜色目前只是敌人本体偏黄，视觉反馈不够醒目。`090_抗体标记环`是候选状态特效，不是投射物。

## 验证边界与待办

- C# 命令行编译曾通过（0 错误、0 警告）；新增信号链和主菜单在本次提交前会再编译。不能仅凭编译宣称 Unity Play Mode 或 Windows 构建已验收。
- 用户已保存 `Level02` 信号连接和 `MainMenu` 接线；尚未获得本轮完整 Play Mode 证据，需验证 P002 近卫塔、P001 B 塔确实能让后续同抗原敌人被标记；在错误塔位、缓存过期时只普通攻击。
- 先在 Unity Prefab 模式把 `InterceptorTower.prefab` 的 `Sprite Renderer → Sprite` 改为 `抗体B细胞中继_基础`（或对应 B 细胞图标），并确认 `Color` 为白色；当前两个塔外观相同是已知配置问题。
- 敌人现在只有一个通用 `Enemy.prefab`／`EnemyDefinition`，不能把 `029_鞭毛双球菌_基础`、`033_重甲芽孢杆菌_基础` 当成已经实现的新敌人。新敌人类型需另建 Prefab、Definition 和波次配置。
- `Projectile.prefab` 是近卫塔和 B 塔共用对象池模板；若把 `070_单体子弹` 换进去，两座塔都会使用它。巨噬细胞当前直接造成伤害，不发射此投射物。
- 当前美术包的关键姿势是概念参考，尚非已对齐可直接播放的动画帧；不要把 `02_塔图标` 与基础塔立绘、`03` 类状态图与基础敌人图混用。
- 下一步建议先完成上述 Prefab 纠错与两关 Play Mode／主菜单入口验证，再考虑更醒目的标记环、主菜单返回／退出闭环；不默认进入新测试模块。

## Git 与素材范围

- 上次已提交版本：`93f05dc`。本轮新代码、`Level02`、`MainMenu`、Prefab 替换、Build Settings 和本交接文档将同批提交；新哈希以 Git 日志为准。
- 开发者决定：本次只提交**当前场景／Prefab 实际引用**的图片及其 `.meta`，不把未使用的完整美术包（约 175 张 PNG、128 MB，另有大量清单与提示词）全部纳入 Git。未提交素材仍保留在本地工作区，不删除。
- 待替换的抗体 B 细胞基础图当前也属于未提交素材；若在别的机器检出本次提交，需先从本地素材包补入该图，之后再由 Unity 编辑器替换并提交。
- Notion 项目总览：<https://app.notion.com/p/3b8c4f9516208114a5bfc964cc2a5ed2>；策划：<https://app.notion.com/p/3b8c4f9516208182bdf1f2b106c7ba75>；学习路线：<https://app.notion.com/p/3b8c4f9516208129b301f4d8d999106c>。

## 新对话可以这样开头

请先阅读 `Docs/CHAT_HANDOFF_2026-09-30.md` 和当前 Git 状态，沿用其中的学习分工。优先核对 Unity Prefab 中 B 塔误用哨兵图标的问题，并根据我接下来的要求推进；不要把未使用的完整美术包自动提交，也不要记录我的个人回答或评分到 Notion。
