# 返回与恢复默认按钮框验证

2026-10-19，Unity 2022.3.53f1c1。四张普通 / 蓝色按钮框已通过 Resources 接入，字图保持独立居中，按钮使用 Sprite Swap 和原有动作。

- PlayMode 全部回归 **20/20** 通过，0 失败 / 跳过；在原用例中验证暂停时悬停切蓝、离开恢复、恢复默认保存与返回保持暂停。
- 本轮 4 份源码 / 配置 / 测试 SHA256 与隔离副本一致；15 份按钮框原图 / 预览 / 源文件 / 说明 / .meta 哈希不变。
- 已查看普通、恢复默认悬停、返回悬停三张 640×480 Unity 渲染，文字居中、两个按钮完整显示，点击区域不重叠。

运行于 Logs/CodexTradingTests 隔离副本，隐藏窗口使用实际 Direct3D 11。本轮为 UI 表现替换，未重跑 EditMode；上轮 109/109 属于设置字图阶段，不能当作本轮新运行。未构建 Player / 执行 Profiler，也不代表所有窗口比例验收。

- [PlayMode XML](2026-10-19_SettingsButtonFrames_playmode.xml)
- [摘要与哈希](2026-10-19_SettingsButtonFrames_summary.json)
- [普通状态](../../Assets/_Project/Art/Sprites/细胞绘制/设置界面_按钮空框_v1/接入验证~/设置按钮框_普通_Unity预览_640x480.png)
- [恢复默认选中](../../Assets/_Project/Art/Sprites/细胞绘制/设置界面_按钮空框_v1/接入验证~/设置按钮框_恢复默认选中_Unity预览_640x480.png)
- [返回选中](../../Assets/_Project/Art/Sprites/细胞绘制/设置界面_按钮空框_v1/接入验证~/设置按钮框_返回选中_Unity预览_640x480.png)

接入前两份当前文档保存在 Docs/History/2026-10-19_before_settings_frames/，原设置字图报告与截图保留。
