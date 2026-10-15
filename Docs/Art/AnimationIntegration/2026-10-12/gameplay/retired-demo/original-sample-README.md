# 动画接入样例：中性粒细胞与葡萄球菌群

打开 `Assets/_Project/Scenes/AnimationSample_Staph.unity` 并按 Play，即可循环查看移动、塔攻击、中弹闪白和敌人冲撞核心。这个演示使用试玩关相同的 Enemy/Tower Prefab 和攻击代码。

## 行为

- 葡萄球菌群移动：使用新版八帧爬行动画，0.8 秒循环。
- 葡萄球菌群攻击：抵达路径终点后停止移动，播放自身冲撞动画；接触姿势显示后，在 0.16 秒处结算一次入侵并回收。抵达终点即提交入侵，不再作为塔的目标。
- 中性粒细胞攻击：压缩蓄力、喷射和回弹，共约 0.44 秒；在 0.11 秒的释放帧通过 Animation Event 发射，弹体从喷口附近生成。
- 弹体：中性粒细胞使用小型青色颗粒，并朝飞行方向转动。对象池归还时恢复默认外观，避免其他塔继承这个样例的外观。
- 受击：敌人闪白约 65 毫秒。通过 SpriteRenderer 的材质参数实现，不改变碰撞体、路径位置或标记颜色。
- 暂停：移动、动画、伤害和反馈计时都使用游戏时间。
- 复用：敌人重新生成时重置到 Move，清除冲撞状态和受击闪白；等待发射的塔检查目标的 SpawnVersion，避免攻击回收后重新使用的对象。

生命值、伤害、攻速、射程、收益数据未调整。原有动画和图片保留；新控制器绑定到对应 Prefab，两个 Prefab 的原 GUID 文件保持不变。

## 文件

- `Assets/_Project/Art/Animations/CombatSample_v1/`：四个 AnimationClip、两个 AnimatorController 和受击材质。
- `Assets/_Project/Art/Shaders/SpriteHitFlash.shader`：受击闪白着色器。
- `Assets/_Project/Art/Sprites/细胞绘制/帧动画/运行样例_v1/`：颗粒素材副本和真实运行预览 GIF/PNG。
- `Assets/_Project/Scenes/AnimationSample_Staph.unity`：循环演示场景。
- `Assets/_Project/Editor/CombatAnimationSampleInstaller.cs`：编辑器接入工具，菜单为 Cell Defense → Animation Sample → Install or Update Sample。
- `Assets/_Project/Editor/CombatAnimationSampleSmokeTest.cs`：批处理运行验证入口。

运行预览由 Unity 的真实播放循环和摄像机录制，Aseprite 仅合成为 GIF。目标环是演示中的核心位置标记。

## 验证

使用本机 Unity 2022.3.53f1c1 完成脚本及着色器编译，并实际进入 Play Mode。

- 确认移动动画循环、攻击动画单次播放、切换无混合延迟。
- 确认敌人攻击片段为 Staph_Attack，未误绑塔的动画。
- 确认释放帧之前没有弹体，释放事件之后产生正确的小型颗粒。
- 实际观察到三次命中反馈、两次核心冲撞和多次敌人复用。
- 确认暂停期间移动、动画和伤害停止。

结果保存在 `asset-validation.json`、`runtime-validation.json`；原始运行截图位于 `runtime-frames/`。此验证覆盖样例交互，未运行完整关卡胜负流程。

本次改动前的脚本与 Prefab 副本保存在本目录的 `backup/`。EnemyController 的旧 GBK 编码在保留原文本的前提下转为 UTF-8。
