# 结算后血条残留修复验证

胜利和失败结算原先只调用 `EnemyStatusHud.ClearHints()`，受伤的防御细胞仍会在 `LateUpdate` 中显示血条。

`OpenFieldPrototypeController.FinishRun` 现在停用 `EnemyStatusHud`：已有的 `OnDisable` 会立即隐藏敌人、防御细胞血条、护盾和提示，后续帧也不再更新这些状态图像。重开和下一关通过场景加载创建新的 HUD。

使用 Unity 2022.3.53f1c1，在 `Logs/CodexTradingTests` 隔离项目运行 PlayMode 验证：

- 修复前，新回归测试失败：胜利结算当帧仍有 4 个可见状态图像。
- 修复后，`OpenFieldPlaybackPlayModeTests` 共 7 项全部通过。
- 新测试覆盖五个关卡各自的胜利与失败；先实际建塔、受伤并生成受伤敌人的血条，再从暂停进入结算，确认当帧及后续帧没有可见状态图像；重开后受伤血条恢复显示。
- 同组已有测试覆盖暂停、倍速、设置、下一关及控制器停用行为。

日志与原始结果：`Logs/ResultHealthBars20261022/before.log`、`before.xml`、`after.log`、`after.xml`。

验证时主项目与隔离项目的控制器 SHA-256 一致：`324765A90DC11414C4B5F6F7380F8F42792F2EF186A69DF62FA2CF17F72B87FF`。
