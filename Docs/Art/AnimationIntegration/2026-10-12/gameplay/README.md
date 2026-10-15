# 现有关卡打击感修改（2026-10-12）

修改直接保存在原有的 OpenFieldLevel02、OpenFieldPrototype、Prototype 和 Level02 场景，以及它们使用的 Tower / Enemy Prefab 中。菜单入口沿用原场景。本轮没有新建游戏场景；之前的演示场景已移出 Assets，存入 retired-demo。

这次先完整接入中性粒细胞塔与葡萄球菌群这一组单位：塔攻击蓄力、释放、回弹，释放帧触发实际弹体，按敌人所在方向翻转；敌人循环移动，到核心后播放攻击并结算一次伤害。命中增加短暂闪白、白色/青色像素粒子和短音效，核心受击使用珊瑚色粒子。暂停时动画、粒子和音效一起暂停。地图、波次、攻击力、攻速、造价和碰撞尺寸沿用原配置。

新版本的三个图集保存在 Assets/_Project/Art/Sprites/细胞绘制/帧动画/关卡动画_v1，采用 128×128 格子、8 帧、Point 过滤、PPU 96，扩大单位显示尺寸。旧图集及其 meta 保留；原有场景和 Prefab 的 meta 完全保留。

## 验证

Unity 2022.3.53f1c1 在验证副本中直接打开原有 OpenFieldLevel02 场景，通过正常建塔和开始进攻接口运行，没有测试专用地图。运行结果见 level-validation.json：17 次发射、16 次命中、5 次核心攻击，闪白、粒子、移动帧和核心攻击均通过检查。验证过的场景、Prefab 和动画资源已复制回主项目并校验文件哈希。

第二关_实际战斗.gif 是上述真实关卡战斗的相机录制，裁掉画面外围留白；GIF 不包含声音和屏幕覆盖 UI。保存在 Assets/_Project/Art/Sprites/细胞绘制/帧动画/关卡打击感预览_v1。

用户原 Unity 编辑器处于系统锁屏状态，本轮没有刷新或操作其 Play Mode。回到 Unity 等待导入编译；如未自动导入，使用 Assets → Refresh，然后在原关卡直接 Play，建造中性粒细胞塔并开始进攻。运行时也会为已打开的原关卡补上反馈组件，无需切换到演示场景。

## 可回退文件

before-gameplay-update 保留本轮改动前的四个场景、两个 Prefab 及其 meta；上一轮脚本和资源备份位于上级 backup 目录。运行检查入口为 CombatGameplayLevelVerifier.Run；它仅在显式调用时启动，不会自动运行。资源安装菜单为 Cell Defense → Apply Combat Feedback to Existing Levels。
