# 暂停与倍速图片按钮接入

日期：2026-10-20；Unity 2022.3.53f1c1。

五个 OpenField 关卡的右上文字工具条改用现有六张按钮图片：暂停、1×、2×各有常态与高亮图。按钮显示当前倍率，点击仍调用原 Pause / TogglePlaybackSpeed；暂停菜单、倍率恢复和战斗规则沿用现有实现。

## 修改

- OpenFieldUiResources 与 Resources/OpenFieldUiResources.asset 引用六张独立 Sprite，五关自动加载，无需逐场景拖图。
- OpenFieldPlaybackControls 使用 128×64 的 2:1 图片按钮、272×72 容器，保留安全区定位及 HUD 避让；移除旧文字与矩形底板。
- 常态 / 悬停 / 按下 / 选中 / 禁用通过 Sprite Swap 接线；倍率变化同步更新常态与高亮，指针不移动也会显示新倍率。
- 六张 PNG 的 SHA256 与接入前完全一致；六份 .meta 及 GUID 保留，仅将过滤调整为 Point、压缩调整为 None，保持无 Mip Maps。原导入设置备份于 Logs/PlaybackToolbarValidation。

## 实际验证

| 检查 | 结果 |
|---|---|
| 现有 PlayMode 完整回归 | 32/32，0 失败 / 跳过 |
| 现有五关按钮测试的额外截图运行 | 1/1，通过；不是新增用例，不与 32 相加 |
| 五关实际 Sprite 状态观察 | 20/20：1×高亮、切换后2×高亮、移开后2×常态、暂停高亮 |
| 主工程与隔离副本 C# 文件 | 96/96 SHA256 一致，截图用临时修改已恢复 |
| 现有横屏回归 | 覆盖 1280×720、1600×720、1920×1080；实际查看桌面比例与移动端暂停预览 |

测试在 Logs/CodexTradingTests 隔离副本运行，使用 D3D11 图形设备。额外截图运行仅给已有五关测试插入截图和 Sprite 名称记录，没有新增测试用例、修改玩法数据或将临时采集代码写入主工程。实际查看了 1280×720 / 1600×720 桌面预览、2×悬停预览与 1600×720 手机布局暂停预览。

预览保存在 Assets/_Project/Art/Sprites/细胞绘制/战斗UI/暂停与倍速按钮_v1/接入验证~/；该目录不作为运行时素材导入。截图来自 Unity 编辑器，不能代替 Android 真机验收。本次没有重打 APK、修改平台草稿、提审或发布。

原始结果：[PlayMode XML](2026-10-20_PlaybackToolbar_playmode.xml)、[额外截图运行 XML](2026-10-20_PlaybackToolbar_capture.xml)。日志、20 条 Sprite 状态、原图 / GUID 保留核对及源码哈希见 Logs/PlaybackToolbarValidation/。
