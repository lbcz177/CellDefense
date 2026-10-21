# 退出与单次升级验证

2026-10-19；Unity 2022.3.53f1c1。用户选择先复用现有单次伤害升级，费用等于实付建造价；四种伤害塔开放，抗体 / 壁垒不收费升级。

| 检查 | 结果 |
|---|---|
| EditMode 全部回归 | 117/117 通过，0 失败 / 跳过；新增 8 项升级经济 / 状态用例 |
| PlayMode 全部回归 | 26/26 通过，0 失败 / 跳过；新增退出 1 项与升级 3 项 |
| 本轮 8 份脚本 SHA256 对隔离副本 | 8/8 一致 |
| Windows 主菜单退出验证程序 | BuildResult Succeeded，Application.quitting 触发，进程退出码 0 |
| 实际 Unity 图形渲染 | D3D11，已查看升级前 / 后两张 640×480 图 |

升级验证覆盖五关按钮、显示 Lv.2 / 满级、伤害 ×1.5、实付价格快照、余额恰好 / 差 1、重复升级无重复扣费、升级费参与 50% 退款、受伤后升级不回满血、网格 Revision 不变、战斗中四种伤害塔可升级且两种支援塔拒绝。另验证暂停、终局、出售确认、死亡及失效 UI 回调不误扣费；升级按钮点击清除 EventSystem 选择，避免 Space 再触发。

退出先单独验证主菜单 3 项回归，再运行最终全部回归。实际 EventSystem 射线命中背景上的退出区域；设置 / 选关打开或控制器禁用时不退出，空闲时仅记录一次请求。Editor 忽略 Application.Quit，不关闭编辑器。

为了验证真正退出，在 Logs/CodexTradingTests 隔离副本临时加入仅测试用的构建 / 启动脚本，生成仅含 MainMenu 的 Windows x64 Mono 程序。以隐藏、batchmode、nographics 启动，通过同一个 MainMenuController.QuitGame 触发，quitting 事件写出 QuitRequested=true，进程正常返回 0。临时脚本在最终回归前已移除，未进入主工程。验证程序不包含五关游玩范围，也不代表窗口视觉或全屏切换验收；未发布完整五关 Player。

所有回归运行于隔离副本，沿用相同核心依赖。EditMode 无图形批处理，PlayMode 隐藏 D3D11；截图等待 / 刷新 HUD 后导出，升级前 ATP 75、后 ATP 50，与交易结果一致。未改场景、Prefab、原图或已有 .meta。未执行 Profiler、所有窗口比例验收、完整五关通关和平衡测试。

- [EditMode XML](2026-10-19_QuitUpgrade_editmode.xml)
- [PlayMode XML](2026-10-19_QuitUpgrade_playmode.xml)
- [执行摘要、源码与验证程序哈希](2026-10-19_QuitUpgrade_summary.json)
- [Windows quitting 事件证据](2026-10-19_QuitUpgrade_player-quit.json)
- [升级前预览](../../Assets/_Project/Art/Sprites/细胞绘制/升级接入_2026-10-19/接入验证~/塔升级前_Unity预览_640x480.png)
- [升级后预览](../../Assets/_Project/Art/Sprites/细胞绘制/升级接入_2026-10-19/接入验证~/塔升级后_Unity预览_640x480.png)
- [开发前快照](../History/2026-10-19_before_quit_upgrade/INDEX.md)

此前各阶段的报告和截图按原日期保留，不将新结果补写进旧报告。
