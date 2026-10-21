# Android 横屏适配验证

日期：2026-10-19；Unity 2022.3.53f1c1。源码、规则和阅读路径见 [手机适配说明](../MOBILE_ANDROID.md)。

## 结果

- EditMode：126/126 通过，0 失败 / 跳过。
- PlayMode：30/30 通过，0 失败 / 跳过；包含原有 26 项、新增手机场景 3 项和横屏分辨率 1 项。
- 横屏覆盖实际 Game 视图 1280×720、1600×720、1920×1080；模拟安全区缩进，检查菜单、暂停和设置位置，导出 Unity 渲染。
- 仅在隔离工程 `Logs/CodexTradingTests` 执行，不关闭或重启用户主工程 Unity。
- 用户决定暂不安装 Android 组件。没有 APK、Android IL2CPP / Gradle 构建、模拟器或真机结果，没有手机性能 / 60 FPS 实测。

## 覆盖范围

`MobileInputTests` 新增 9 项：抬起才确认、防重复、UI 起点 / 终点、超阈值移动后返回、多指抑制与清空、手指编号、系统取消，以及布置 / 战斗后台暂停不自动恢复。

`MobilePlayModeTests` 新增 3 项：主菜单入口实际射线命中、隐藏桌面全屏设置、选关与返回；五个场景点选和购买、塔详情 / 出售对话框 / 胜利结算按钮 / 下一关显示、越安全区点选拒绝；战斗切后台、轮次保留、2x 手动恢复。

`ZzMobileLandscapePlayModeTests` 在实际横屏 Game 视图中检查手机主菜单、设置和暂停位置，导出三种尺寸。该 Editor 测试通过反射使用 Unity 2022.3 内部 Game view API；不进入 Player。它改变 Game 视图，因此在默认整套测试中最后运行，也可用 `-testCategory MobileLandscape` 单独验证。

## 修复与验证过程

额外结算检查发现旧 Prefab 的按钮 localScale 为 6，直接包装原 800×600 参考区域仍会越界。手机分支将按钮恢复为 1 倍、512×96 并按有效按钮数排列；保留原事件，桌面分支不变。补充断言验证了五关重试 / 返回与末关隐藏下一关。

横屏测试最初与普通小窗口设置测试共用 fixture，改变批处理 Game 视图后导致旧测试尺寸断言失败。随后拆出最后执行的 fixture，并重新运行无筛选的全部 PlayMode，30/30 通过。

一次回归启动遇到前一次 EditMode 虽已生成通过结果但进程尚未退出，Unity 报工程被占用。只确认并终止了本任务拥有、项目路径和结果均匹配的完成进程，再重启回归。未处理用户主工程进程。

## 证据

- [EditMode XML](2026-10-19_MobileAndroid_editmode.xml)
- [PlayMode XML](2026-10-19_MobileAndroid_playmode.xml)
- [独立横屏 XML](2026-10-19_MobileAndroid_landscape.xml)
- [源码 SHA256 与摘要](2026-10-19_MobileAndroid_summary.json)
- 预览位置：`Assets/_Project/Art/Sprites/细胞绘制/移动端适配_2026-10-19/接入验证~/`，仅新增导出的验证图，不覆盖既有素材。

以前 117/26 的退出与升级记录及 [适配前文档快照](../History/2026-10-19_before_mobile/INDEX.md)保留。这里的编辑器通过不能推断手机安装、来电、切后台系统行为、旋转安全区变化、音频焦点、发热和性能均已验收。
