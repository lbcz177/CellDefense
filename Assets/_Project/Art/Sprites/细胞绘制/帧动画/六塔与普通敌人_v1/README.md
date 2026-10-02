# 胞卫战 · 六塔与普通敌人动画 v1

共 12 套动画、96 帧。包含六座防御塔的攻击动作，以及三种普通敌人的移动和攻击动作。葡萄球菌群移动沿用已经确认的版本，其余 11 套为本批新绘制。

双击同目录的 **动画预览.html**，即可离线查看、暂停、逐帧播放、切换底色或筛选单位。也可以直接打开每个文件夹中的 preview.gif。

| 文件夹 | 单位 | 动作 |
| --- | --- | --- |
| 01_neutrophil_attack | 中性粒细胞哨兵 | 攻击：膜孔微脉冲 |
| 02_fibrin_attack | 纤维蛋白壁垒 | 攻击：纤维收紧与隔离脉冲 |
| 03_lysosome_attack | 溶酶体酸化炮 | 攻击：囊泡蓄积、出口释放 |
| 04_neural_attack | 神经脉冲刺突 | 攻击：脉冲释放 |
| 05_phagocyte_attack | 巨噬细胞清道夫 | 攻击：伪足合拢吞噬 |
| 06_antibody_attack | 抗体 B 细胞中继 | 攻击：抗体节点发光与释放 |
| 07_staph_move | 葡萄球菌群 | 移动：菌群挤压、弹性蠕动 |
| 08_staph_attack | 葡萄球菌群 | 攻击：蓄力、前冲、复位 |
| 09_diplococcus_move | 鞭毛双球菌 | 移动：鞭毛摆动 |
| 10_diplococcus_attack | 鞭毛双球菌 | 攻击：收尾蓄力、冲刺、复位 |
| 11_bacillus_move | 重甲芽孢杆菌 | 移动：节段蠕动 |
| 12_bacillus_attack | 重甲芽孢杆菌 | 攻击：收缩、顶撞、复位 |

## 每套文件

- **animation_sheet.png**：供 Unity 切割的透明精灵图，512 × 256，4 列 × 2 行。
- **frames/animation_01.png 至 animation_08.png**：独立透明帧，每张 128 × 128。
- **animation.aseprite**：可直接在本机 Aseprite 中逐帧编辑的源文件，含 Move 或 Attack 标签。
- **preview.gif**：3 倍整数放大到 384 × 384 的循环预览。
- **animation.json**：帧坐标、枢轴、时长、原图裁剪信息。
- **validation.json**：帧数量、透明边界、精灵图与源文件一致性等检查记录。
- **source/generated-poses.png 与 source/prompt.txt**：生成原图及实际提示词。原图不是最终规整切图，请使用 animation_sheet.png 导入游戏。

## Unity 切割与播放

1. 导入 **animation_sheet.png**。Texture Type 选择 **Sprite (2D and UI)**，Sprite Mode 选择 **Multiple**。
2. Filter Mode 选择 **Point (no filter)**，Compression 选择 **None**，关闭 Generate Mip Maps。Pixels Per Unit 可先设为 **128**，再按场景比例统一调整。
3. 打开 Sprite Editor，Slice 选择 **Grid By Cell Size**，Pixel Size 为 **128 × 128**，Offset 和 Padding 均为 **0**。
4. Pivot 选择 **Custom**，X 为 **0.5**、Y 为 **0.125**，对应每帧底部对齐点。切割后 Apply。
5. 按从左到右、从上到下的顺序放置 8 帧；第一行 01—04，第二行 05—08。也可直接用 frames 中已编号的 PNG 创建动画，避免自动命名顺序混淆。
6. Animation Clip 的 Samples 设为 **10**，每帧 **0.1 秒**，整段 **0.8 秒**。移动可勾选 Loop Time；攻击可勾选以预览循环，实装时按实际攻击冷却触发动作。

每套动画内部使用统一缩放与底部对齐，保留动作的压缩、伸展和回弹。葡萄球菌群、重甲芽孢杆菌的攻击尺寸已向对应移动动作校准。当前包含单一朝向，不含八方向转向帧；不同动作仍应结合实际模型尺寸和游戏镜头做最终视觉确认。

攻击预览的 0.8 秒不是战斗数值或攻击间隔。快速连射单位应按实际节奏截取或调整短促攻击段。独立弹体、命中反馈、伤害判定和动画事件应在 Unity 中另行接入。

## 制作与验证

姿态通过内置 image_gen 按已有单位参考图生成，再由本机 Aseprite 整理为 128 像素帧，使用最近邻缩放、固定切片网格、统一时长并导出。完整提示词见 **提示词汇总.txt**；单项参数见 jobs 和各动画的 source 目录。

检查包括：8 帧数量、100 ms 帧时长、GIF 无限循环、透明边界、独立帧与精灵图逐像素一致、Aseprite 文件重新打开验证。PNG 保留完整透明信息；GIF 为便于查看的预览格式。

这批文件是动画美术预览与导入素材，尚未接入 Unity 的 Prefab、Animator 或战斗逻辑，也未完成 Unity 内实际战斗播放验证。

参数参考：[Unity Sprite Editor](https://docs.unity3d.com/2022.3/Documentation/Manual/SpriteEditor.html)、[Unity Animation Clips](https://docs.unity3d.com/2022.3/Documentation/Manual/AnimationClips.html)、[Aseprite CLI](https://www.aseprite.org/docs/cli/)。
