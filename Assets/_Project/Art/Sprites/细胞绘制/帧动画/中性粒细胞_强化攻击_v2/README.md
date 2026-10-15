# 中性粒细胞：强化攻击预览 v2

- `preview.gif`：384×384、无限循环、每轮 1 秒。深色背景仅用于预览；GIF 无声音。
- `neutrophil_attack_sheet.png`：512×256 透明底，4 列×2 行，单帧 128×128，按从左到右、从上到下排列。
- `frames/`：8 张独立透明 PNG。
- `neutrophil_attack.aseprite`：可编辑动画及每帧时间。
- `animation.json`：帧序、时长、Unity 矩形、释放帧信息。

动作：待机 → 收缩蓄力 → 最大压缩 → 喷射 → 后坐 → 回弹 → 恢复 → 待机。

帧时长依次为 160、90、60、50、70、90、130、350 毫秒。第 4 帧为释放帧。首尾采用同一张待机姿势，预览中保留等待时间以便观察每次攻击。

Unity 导入设置：Sprite Multiple、8 个 128×128 切片、中心 Pivot、128 PPU、Point、关闭纹理压缩和 Mip Maps。独立帧使用 Sprite Single。实际接入时，攻击应按一次攻击播放一次，弹体应在第 4 帧同步生成；普通等间隔 AnimationClip 不会自动采用 JSON/GIF 中的非等时帧时长。

本版作为美术预览新增，未替换现有塔资源、Animator 或战斗代码。喷口闪光包含在第 4 帧中；不包含独立飞行弹体、敌人受击动画或声音。

制作：内置 image_gen 重绘关键帧，Aseprite 最近邻整理、对齐、设置帧时长及导出。原始关键帧、完整提示词、制作脚本和验证结果位于 `Docs/Art/AnimationSamples/NeutrophilAttack-v2/`。
