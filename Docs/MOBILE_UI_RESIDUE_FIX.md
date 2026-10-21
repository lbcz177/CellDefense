# 手机战斗视口外 UI 残影修复

日期：2026-10-19；针对用户在 Android 0.1.0 实机截图中反馈的问题。

## 原因与改动

手机为 HUD 和刘海安全区缩小了战斗相机的 Camera.rect。该相机的纯色清屏只覆盖自己的视口，整块屏幕并没有每帧重绘；前一页面的按钮、标题等像素可能留在地图外。因此即使旧场景对象已销毁，画面仍可能像界面交错。

OpenFieldPrototypeController.ConfigureViewportBackground 在需要局部视口时创建全屏背景相机，挂在战斗相机下，随场景销毁。它先于战斗相机渲染，使用纯色清屏和 cullingMask=0，不绘制塔、敌人或 UI。随后正常绘制等比地图和 Overlay UI；暂停时仍持续清屏。安全区变化时同步配置，桌面完整视口时关闭这台额外相机。

代码：Assets/_Project/Scripts/OpenField/OpenFieldPrototypeController.cs。原地图投影、触控坐标和玩法规则不变。

Unity 参考：[多相机与视口](https://docs.unity3d.com/2022.3/Documentation/Manual/MultipleCameras.html)、[清屏配置](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Camera-clearFlags.html)。

## 回归证据

EditMode 127/127、PlayMode 32/32 通过。新增图形测试在真实 Game 视图中先用低优先级相机写入洋红色作为旧页面残留，再读回世界相机 HDR 合成完成后的屏幕像素，验证顶部、底部和两侧都被正确清理，地图仍可见。

覆盖五关、1280×720 和 2048×928 两种横屏尺寸，以及三种安全区状态（含暂停时变化），共 30 组场景状态；回到主菜单后不残留背景相机。仅在隔离副本关闭清屏调用时，测试按预期在地图上方失败，恢复后全量通过。

测试没有使用 targetTexture 来代替屏幕视口，因为 Unity 将相机渲染到纹理时会忽略 rect，可能漏掉这类错误。参考：[targetTexture](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Camera-targetTexture.html)。

截图在 Assets/_Project/Art/Sprites/细胞绘制/手机UI残影修复_2026-10-19/接入验证~/，拍摄于 Overlay UI 之前，不属于手机截图。完整结果见 [验证与修复包报告](Testing/2026-10-19_MobileUiResidue.md)。

## 手机复测

修复包版本为 0.1.1 / versionCode 2，保留原包名与签名，供覆盖安装。实际构建和校验信息以验证报告为准。用户已提供旧包实机问题截图；本地修复和编辑器 GPU 回归不能代替同一手机上的复测。

建议在手机中进入第二关，再检查暂停 / 设置 / 返回主菜单后重进关卡，以及切后台再返回时，顶部和边缘有无旧画面。

修复前的文档见 [历史快照](History/2026-10-19_before_mobile_ui_residue/INDEX.md)，原 0.1.0 APK 另存为 Builds/Android/CellDefense-0.1.0.apk。
