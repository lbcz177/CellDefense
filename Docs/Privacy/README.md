# Android 隐私整改（2026-10-22）

> 2026-10-26：0.1.5包内政策补充最高通关分/星级与删除说明，原网页已按用户确认公开更新，Site版本2；匿名下载的政策SHA256与0.1.5包内文本完全相同。TapTap草稿已绑定0.1.5 / Code6，未提交新审核或发布游戏。

以下0.1.4部分为2026-10-22整改历史；当前结果见[0.1.5上传记录](../Publishing/0.1.5_UPLOAD.md)。

本轮对应用户提供的 Unity 提前获取信息整改要求及 TapTap 拒审意见。已完成本地整改、正式候选包构建和独立政策网页发布；用户确认后，0.1.4 已上传并绑定 TapTap 主包，独立链接已存入隐私资质草稿。未提审或上线。

## 已实现

- 首次启动的唯一桌面入口为原生 `PrivacyActivity`，继承 Android `Activity`；不引用 Unity SDK 类，不使用 WebView，不发网络请求。
- 原生弹窗有明确的“同意”“拒绝”和“查看完整隐私政策”。只有明确同意且同步保存成功才进入游戏；拒绝、返回不会创建 UnityPlayer。
- 内部 `ConsentUnityActivity` 在创建 UnityPlayer 之前再次核验政策摘要，防止旧任务恢复绕过授权。未初始化时的生命周期与输入回调都处理空播放器。
- Unity 导出会重写游戏 Activity 的 exported 属性，因此 `AndroidPrivacyGradleGuard` 在导出后恢复 `exported=false`，并阻止默认 UnityPlayerActivity 混入。
- 记录完整政策的 SHA-256 与同意时间；政策变化后重新征求同意。写入失败时也回滚 SharedPreferences 内存中的同意状态。关闭备份并明确排除授权记录迁移。
- 主菜单左下角有原生“隐私政策”入口，可再次离线阅读和“撤回同意并退出”。撤回清除同意记录并结束游戏进程。
- 关闭旧版 Unity Analytics 及其自动初始化；离线包移除 INTERNET、ACCESS_NETWORK_STATE 权限。
- 包内政策明确列出设备传感器信息、Android ID、基础运行环境信息的读取方式、时机与用途；说明本地保存、删除、撤回、政策更新和未成年人事项。

## 政策与包体

- [独立公开政策网页](https://cell-defense-privacy.jazzy-flint-2968.chatgpt.site/)；[离线审阅副本](privacy-policy-draft.html)。原始政策位于 `Assets/Plugins/Android/CellPrivacy.androidlib/res/raw/cell_privacy_policy.txt`，原生页面直接读取，断网可用。网页及下载文本已验证无需登录即可访问，文本与包内政策一致。
- 用户已确认运营者使用 **ichen**，公开隐私咨询渠道使用 **QQ 2558057120**；正式政策已补齐。未要求用户必须提供邮箱。
- 正式候选包为 **0.1.4 / Code 5**，文件名 `Builds/Android/CellDefense-0.1.4.apk`，59,571,802 字节；SHA-256：`8a5cd5b70a7c3411383b20c2d4b2ec785dc8796679c8c97ce287fde516e906fb`，与已有 0.1.3 签名一致。
- 先前 `CellDefense-0.1.4-privacy-validation.apk` 为未补联系方式的历史验证包，予以保留，不作为上传候选。同版本正式候选用于替代它，尚未上传。
- 构建入口临时设置版本号后恢复项目设置。本地候选清单为 `Docs/Publishing/0.1.4_manifest.json`；TapTap 已绑定 0.1.4 / Code 5，当前审核结果仍为此前的拒绝，新包尚未提审。
- 独立网页已公开，部署记录为 `site-deployment.json`；用户明确确认后已保存政策链接至隐私资质草稿、上传并绑定新包。现有截图、录屏及更新日志均保留，见[上传记录](../Publishing/0.1.4_UPLOAD.md)。资质状态已核对为已存草稿，CLI 未提供政策链接值的读回接口。

## 验证

`python Tools/Privacy/verify_privacy.py`：使用生产 `ConsentStore.java` 运行 12 项 JVM 逻辑检查，Android 存储 API 使用测试替身；覆盖首次启动、明确同意、政策变更、撤回、保存失败、内存回滚和缺失政策。另检查源清单与统计配置。

`python Tools/Privacy/verify_apk.py <APK路径>`：使用 Android SDK 分析最终 APK，核对唯一启动入口、内部游戏入口不可外部启动、默认 Application、无自动初始化 Provider / Service / Receiver、无联网及敏感权限、政策资源与原文一致、双架构引擎库、DEX 引用和签名与旧包一致。AAPT 会改写 ZIP 内资源文件名，校验通过资源表名称和原始内容完成。

正式候选构建入口：Unity 菜单“胞卫战 / Android / 构建隐私整改候选 APK（0.1.4）”。如已存在同名候选包，该入口拒绝覆盖，先另存旧包后再构建。原包和 `.meta` 不删除。

日志与原始结果位于 `Logs/PrivacyFix20261022/`。这些检查不等于 Android 真机或平台隐私检测通过。

## 待真机验证

当前 adb 没有连接设备，尚未安装测试。

1. 首次安装启动，Unity 闪屏和游戏界面应在“同意”之后才出现；不点击时保持原生页面。
2. 拒绝、返回键、切后台后重新进入，均不得将未同意状态视为已授权；拒绝后重新打开仍询问。
3. 同意后正常进入主菜单、游玩五关，验证横屏、触控、后台暂停与音量。
4. 主菜单打开政策，验证阅读、返回游戏、撤回并退出；撤回后再次启动重新询问。
5. 覆盖原 0.1.3，首次打开应重新征求同意；之后政策文本发生变化也应重新询问。
6. 在不同横屏尺寸、字体缩放和 Android 版本上检查弹窗正文滚动及两个授权按钮始终可见。
7. 重新进行隐私调用检测，核验同意前无 Android ID、传感器读取及引擎初始化；保留实际检测结果后再判断整改是否被平台接受。

## 参考

- [Unity 2022.3 Android 自定义 Activity](https://docs.unity3d.com/2022.3/Documentation/Manual/android-custom-activity.html)。生命周期转发也核对了项目所用 Unity 安装目录中的 `UnityPlayerActivity.java`，避免调用其会构造播放器的 `onCreate`。
- [Android Activity 生命周期](https://developer.android.com/guide/components/activities/activity-lifecycle)。
- [Unity 游戏玩家与应用用户隐私 FAQ](https://unity.com/legal/game-player-and-app-user-privacy-faq) 与[隐私政策](https://unity.com/legal/game-player-and-app-user-privacy-policy)，用于核对引擎信息类别说明；不把所有 Unity 服务的数据处理都视作本游戏已启用。
- 用户指定的 [CSDN 方案](https://blog.csdn.net/Czhenya/article/details/131555391) 本轮多次读取超时；实现采用其在用户截图中说明的“原生授权入口先于引擎启动”方向，具体接口按官方文档与当前引擎源文件核对。
