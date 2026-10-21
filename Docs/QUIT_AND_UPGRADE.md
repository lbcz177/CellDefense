# 主菜单退出与开放关卡单次升级

2026-10-19，Unity 2022.3.53f1c1。升级范围由用户明确选择：先复用现有单次伤害升级，不新增六塔专项升级或完整分支树。

## 退出游戏

主菜单背景上的“退出游戏”已接入透明点击区，和已有背景一起缩放。仅主菜单空闲时可触发，设置 / 选关打开、正在加载、控制器禁用时拒绝；重复请求不重复执行。

`MainMenuController.QuitGame` 先调用 PlayerPrefs.Save，再调用 Application.Quit。Editor 中输出“退出请求已触发；独立运行时关闭游戏窗口。”，保持编辑器运行；Windows Player 会退出。没有新增确认弹窗，Esc 仍用于关闭设置 / 选关。

## 单次升级

五关均可点击已有塔 → 查看“升级后伤害”和费用 → 升级。布置及战斗都可用，暂停与终局拒绝；升级后保持当前选择，按钮显示已满级。

| 范围 | 规则 |
|---|---|
| 可升级塔 | 近卫、巨噬、酸化、神经脉冲 |
| 抗体 / 纤维蛋白壁垒 | 保持 Lv.1，显示“暂未开放升级”，不收费 |
| 费用 | 建造时实际支付的价格，不跟随后续 Definition 改价 |
| 次数 | 每座塔 Lv.1 → Lv.2 一次 |
| 效果 | 伤害乘 1.5，复用原有 1.15 倍视觉缩放；酸区沿原有发射时倍率结算 |
| 保持项 | 生命及已受伤量、射程、攻击间隔、吞噬 / 标记规则、接敌、路径、抗原记忆和冷却不重置 |
| 出售 | 实付建造与升级费用合计的 50%，向下取整；25 建造 + 25 升级 → 25 退款 |

ATP 不足、无塔、塔已死、已满级、支援塔不适用、暂停或终局都不扣费。出售确认打开时，升级按钮禁用且回调拒绝。死亡仍无退款；升级不消耗或返还壁垒累计额度，不重新生成塔，不刷新寻路。

## 代码

- MainMenuController：退出入口、QuitRequested 防重复与动态入口清理。
- OpenFieldPrototypeController：GetUpgradeCost / CanUpgradeAt / TryUpgradeAt，校验、扣费、升级失败返还与面板刷新。
- TowerController：SupportsDamageUpgrade 与已有 TryUpgrade / CurrentDamage 规则。
- OpenFieldTowerMenu：费用、升级后伤害、ATP 不足 / 满级 / 支援塔状态；升级按钮与事件焦点清理。

无需改场景、Prefab 或美术；塔面板继续使用既有文字控件样式。旧场景升级流程保留。

## 验证

最新 EditMode 117/117、PlayMode 26/26，0 失败 / 跳过；五关升级、战斗中四种伤害塔、支援塔拒绝、资金边界、暂停 / 死亡回调、退款均覆盖。已查看升级前后实际 Unity 渲染。

另在隔离副本构建只含 MainMenu 的 Windows Mono 验证程序，无图形隐藏启动，调用真实 QuitGame，记录 Application.quitting 且进程以 0 正常退出。这是退出链路验证，不是完整五关 Player 发布或窗口视觉验收。

见 [测试记录](Testing/2026-10-19_QuitUpgrade.md)。此前“没有开放升级入口 / 退出未接通”的当前文档保存在 [开发前快照](History/2026-10-19_before_quit_upgrade/INDEX.md)。
