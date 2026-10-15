# 试玩关 Tilemap 方块样例 v1

仅供确认画风和方块拼接方式，当前包含 3 张完整的正方形地面图。每张 PNG 对应一个格子，地面和纹理已经在同一张图里，不需要叠加透明边缘。

| 文件 | 用途 |
| --- | --- |
| 01_普通组织地块.png | 组织地面 A |
| 02_细胞纹理地块.png | 组织地面 B，用于打散重复 |
| 03_纤维纹理地块.png | 带纤维的组织地面，装饰纹理 |

三种图案都只表示地表外观，纤维不表示阻挡物。依据当前 `Docs/OPEN_FIELD_THREE_ROUND_PROTOTYPE.md` 的开放地图方案制作，不预画敌人的固定行进路线。

## 在 Tilemap 中使用

- PNG 尺寸为 128×128，Sprite Mode 为 Single，PPU 为 128。
- 已配置 Point、关闭 Mip Maps、Compression None、Full Rect。
- 在 Grid Cell Size = (1, 1, 0)、Tile Transform Scale = (1, 1, 1) 下，每张图覆盖一个格子。
- 把这三张 PNG 拖入 Tile Palette，保存 Unity 提示创建的 Tile 资源，再用画笔绘制。
- 不需要 Sprite Editor 切割。此批没有修改场景或自动创建完整地图。

## 拼接预览与制作记录

`制作记录~/preview_3x3.png` 是把这三张实际 PNG 按 3×3 排列后放大三倍的检查图。
`制作记录~/preview_samples.png` 是三张样块横排预览；单张原尺寸仍为 128×128。
文件夹末尾的 `~` 用于让 Unity 忽略制作源文件。

采用内置 image_gen 绘图，再由 Aseprite 统一像素尺寸、16 色调色板和边缘底色。保留原始生成图、Aseprite 文件、完整提示词 `prompts.json` 和检查结果 `validation.json`。这批是样例，不是完整的自动地形拼接图集。

