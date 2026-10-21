# 左上角图片状态栏

2026-10-16：第一至第五关统一使用新绘制的 ATP、生命、波次与三栏外框，替换原来的英文 IMGUI 状态栏。

后续按用户要求改为真正的场景预制体：`Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab`，五关都放置了关联实例，并在控制器的 `battleHud` 字段绑定。下文布局以本次更新为准。

## 素材与布局

- 原图位于 `Assets/_Project/Art/Sprites/细胞绘制/战斗UI/` 下的 `ATP像素数值_v1`、`生命波次与HUD框_v1`，所有 PNG 与 `.meta` 保留。
- 使用三栏空框和独立标签、图标、0–9 数字及斜杠；`HUD组合预览.png` 仅供参考，不作为运行界面。
- 按 1280×208 原稿坐标组合，外框缩放从 0.5 改成 0.4，显示宽 512、高 83.2 个 UI 单位，比此前缩小 20%；整体含额度提示占位为 512×112，距屏幕安全区域左上角各 12 个 UI 单位。Canvas 参考分辨率 1280×720，宽高匹配为 0.5；较小安全区域会整体等比缩小。外框使用 Simple。
- ATP、核心生命、当前波 / 总波数读取控制器实际状态。数值改变时更新预建的数字 Image，多位数超出栏位会缩小字符，不截断数值。
- 壁垒剩余额度提示也属于此预制体，在外框下方；额度改变时更新文字。选塔框与入口按钮根据 HUD 的实际屏幕矩形避让，HUD 内点击不会落到建造格。
- 布置 / 进攻期间显示，胜负时隐藏图片状态栏，使用各关已有的胜负面板。

## 代码

| 文件 | 职责 |
|---|---|
| `Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab` | 可在编辑器中编辑的 Canvas、外框、图标、四组数字与额度提示 |
| `Assets/_Project/Scripts/UI/OpenFieldBattleHud.cs` | 绑定预制体字段、安全区域定位、数据刷新与输入区域，不创建图形对象 |
| `Assets/_Project/Scripts/UI/HudSpriteNumber.cs` | 使用预制体中的十个数字槽更新数值，支持归零和位数增减 |
| `Assets/_Project/Scripts/UI/OpenFieldUiResources.cs` | 默认 UI 资源类型与完整性校验 |
| `Assets/_Project/Resources/OpenFieldUiResources.asset` | 引用原始 Sprite 与默认 HUD Prefab，兼容尚未刷新场景引用的编辑器状态 |
| `Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs` | 创建 HUD、提供实时数据、移除旧 OnGUI、终局隐藏 |
| `Assets/_Project/Scripts/UI/EnemyStatusHud.cs` | 敌人 / 塔血条及敌人介绍，不再创建壁垒额度文字 |
| `Assets/_Project/Scripts/UI/OpenFieldBuildMenu.cs` / `OpenFieldEntranceWaveButtons.cs` | 避让 HUD |

## 核对与用户验证

已完成 77 个源码的 Roslyn 静态诊断，0 错误，没有 Emit；核对默认 Sprite 引用、数字顺序及新增脚本 GUID，HUD 图片 / `.meta` 保留。五关场景引用、Prefab 字段、数字位数增减及实时数据绑定均已通过 Unity EditMode 测试；真实关卡开波 / 战中建塔 / 改路 / 召唤及 HUD 已通过 PlayMode 冒烟。总计 EditMode 67/67、PlayMode 1/1，通过结果见 [测试记录](Testing/2026-10-16_LiveBuildHUD.md)。使用独立工程副本执行，没有构建或 Profiler 实测；目视验证仍需用户完成。

用户在 Unity 导入后验证：

1. 五关左上角出现新框与图标，原英文状态栏消失，数字与关卡初始 ATP、生命和 1/3 一致。
2. 建塔扣费、击杀奖励、轮间奖励、敌人泄露及提前开波后，各项数值正确更新；波次不超过 3/3。
3. 点击外框不建塔；靠近左上角选格和入口开波时，选择框 / 按钮避让 HUD，仍可点击。
4. 切换窗口比例，检查框内图标、数字比例和安全区域边距；胜负、重试、下一关后正确隐藏 / 重建。
