# 葡萄球菌群：移动循环预览 v1

保留现有洋红色菌球、紫色轮廓和绿色斑点，采用挤压、伸展、回弹的原地蠕动。沿道路前进的位移交给游戏中的路径移动逻辑。

## 文件

- `staph_move_preview.gif`：3 倍最近邻放大的循环预览，384×384。
- `staph_move_sheet.png`：Unity 使用的透明 PNG 图集，512×256。
- `staph_move.aseprite`：128×128 的可编辑 8 帧动画，含 Move 标签。
- `frames/`：逐帧 PNG，按 01～08 排序，每张 128×128。
- `animation.json`：帧坐标、时长、对齐位置和生成记录。`frame` 使用左上原点；`unityRect` 已转换为 Unity 左下原点。
- `source/generated-poses.png`、`source/prompt.txt`：内置图像生成工具绘制的姿势原图和提示词。原图不是最终切割图集。
- `build-animation.lua`、`verify-animation.lua`：Aseprite 整理与检查脚本。

## 图集规格

| 项目 | 数值 |
| --- | --- |
| 整图 | 512×256 像素 |
| 网格 | 4 列 × 2 行 |
| 单帧 | 128×128 像素 |
| 帧数 | 8 |
| 帧时长 | 100 ms |
| 播放速度 | 10 FPS |
| 循环长度 | 0.8 秒 |
| Offset / Padding | 全部 0 |
| 建议 Pivot | Custom：X=0.5，Y=0.125 |
| 建议 Pixels Per Unit | 128，可按游戏显示尺寸统一调整 |

播放顺序：

```text
01 → 02 → 03 → 04
05 → 06 → 07 → 08
```

按上述顺序连续播放，08 之后回到 01。各帧使用相同缩放倍率；以底部为锚点对齐，保留动作自身的挤压与伸展。透明边缘保留。

## Unity 2022.3 切割与循环

1. 把 `staph_move_sheet.png` 拖到 Project 窗口的素材文件夹。
2. Inspector 中设置 Texture Type 为 `Sprite (2D and UI)`，Sprite Mode 为 `Multiple`，Pixels Per Unit 为 `128`，Filter Mode 为 `Point (no filter)`，Compression 为 `None`，关闭 Generate Mip Maps，点击 Apply。
3. 打开 Sprite Editor → Slice，Type 选 `Grid By Cell Size`，Pixel Size 设为 `128 × 128`，Offset 和 Padding 全部为 `0`。Pivot 选 Custom，填 `X=0.5 / Y=0.125`。点击 Slice，再点 Apply。应得到 8 个等大的精灵。
4. 将 8 个精灵按上面的顺序命名为 `staph_move_01`～`staph_move_08`，选择后拖到场景中，保存生成的动画文件。也可以把它们依次放到现有 SpriteRenderer 的 Animation 窗口中。
5. 将动画的 Samples 设为 `10`，选择动画 `.anim` 文件，在 Inspector 勾选 `Loop Time`。循环时长应为 `0.8` 秒；时间线顺序应是 01～08。
6. 播放检查菌球蠕动，角色在地图上的位置继续由路径脚本控制。

已验证图集与 Aseprite 内 8 帧逐像素一致、每格透明边界、GIF 帧时长和无限循环。此版本供动作效果确认，尚未接入现有敌人预制体，也未在 Unity 内运行验证。

## 制作与参考

姿势由内置 image_gen 生成；本机 Aseprite 负责最近邻缩放、位置对齐、8 帧时间线及 PNG/GIF 导出。没有把受击或死亡姿势混入移动循环。

- [Aseprite Image API](https://www.aseprite.org/api/image)
- [Aseprite Frame API](https://www.aseprite.org/api/frame)
- [Unity Sprite Editor](https://docs.unity3d.com/2022.3/Documentation/Manual/SpriteEditor.html)
- [Unity Animation Clips](https://docs.unity3d.com/2022.3/Documentation/Manual/AnimationClips.html)
