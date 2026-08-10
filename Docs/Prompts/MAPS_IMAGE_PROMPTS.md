# 《细胞防卫战》地图 AI 图片提示词

> 独立类别：只包含地图、组织背景和关卡宣传图  
> 统一规范：[../ART_DIRECTION.md](../ART_DIRECTION.md)  
> 输出目标：三关地图概念、可用背景候选、关卡缩略图

## 0. 重要限制

AI 生成地图只负责美术方向与背景纹理，不能作为路线、碰撞、塔位和信号图的权威数据。模型容易生成断路、假分叉、透视错误和不等宽路径。正确工作流是：

1. 先在 Unity/绘图工具中画灰盒：路线中心线、入口、出口、Boss 区、塔位和 UI 安全区。
2. 把灰盒作为图像参考（工具支持时）生成美术概念。
3. 选中方向后生成“背景层候选”，人工修正路径边界。
4. 路线碰撞、Waypoint/PathGraph、BuildSlot、SignalNode 仍在 Unity 中独立配置。
5. 信号线、塔位状态、感染遮罩和路线预警优先用可控 Sprite/LineRenderer/VFX 覆盖，不永久画死在背景里。

## 1. 统一地图提示前缀

```text
orthographic top-down 2D biological tissue map for a horizontal tower-defense game,
living microscopic field-guide illustration, warm soft tissue environment,
large clean organic shapes, flat cel shading with 2 to 3 value bands,
dark plum contour accents, subtle paper grain, almost no cast shadow,
clear continuous pathogen route with consistent width and no perspective vanishing point,
quiet background values with high gameplay readability, non-gory and educational,
designed for a 16:9 landscape game screen, no characters and no UI
```

### 1.1 统一负面词

```text
photorealistic medical image, gore, blood pool, open flesh, organs, surgery, horror,
disgusting slime, pus, wet meat, 3D render, isometric perspective, side view,
landscape horizon, vanishing point, medieval road, stone path, grass, forest, city,
metal sci-fi corridor, circuit board, military base,
broken route, dead end, accidental extra branch, overlapping path, inconsistent path width,
tiny noisy cells, busy texture, high contrast decoration, heavy shadow, depth of field,
characters, enemies, towers, bullets, UI, text, letters, numbers, logo, watermark, border
```

## 2. 地图通用构图模板

```text
map composition follows this gameplay plan:
[ENTRANCE AND ROUTE DESCRIPTION],
[EXIT DESCRIPTION],
[BOSS OR MERGE AREA],
leave a calm top strip for HUD readability and a calm bottom strip for tower cards,
reserve [NUMBER] rounded tissue attachment clearings near but never overlapping the route,
include faint embedded biological signal fibers between those clearings as low-contrast decoration only,
keep every route segment visually continuous and at least one enemy-width away from the screen edge
```

注意：提示词中的“塔位数量”只帮助留白，最终准确位置必须由关卡灰盒决定。

---

## 3. 第一关：伤口表层

### 3.1 玩法构图

```text
入口：左侧偏上，破损表皮边缘
路线：单路线，柔和 S 形穿过画面中央，不能交叉
出口：右侧偏下，健康组织修复边界
塔位：8 个；其中 6 个普通信号节点、2 个近路吞噬位
信号图：入口 2 节点 -> 中段枢纽 -> 末端 3 节点的简单可读链
Boss 区：右侧中段有较宽椭圆空间
```

### 3.2 氛围概念图提示词

```text
wide concept art of a shallow skin-wound surface transformed into a friendly microscopic strategy battlefield,
orthographic top-down view, a single clean S-shaped invasion groove enters from the upper-left skin break
and crosses toward a calm repaired-tissue exit at the lower-right,
soft coral epithelial sheets overlap like large rounded paper layers,
gentle cream fibrin patches and sparse cyan immune-signal fibers weave beneath the surface,
eight quiet rounded attachment clearings sit around the route without looking like turret pads,
one wider oval encounter area near the right-center for a fungus boss,
hopeful healing atmosphere with localized danger, no visible blood, no characters,
soft coral, rose, cream and restrained cyan palette, clear center gameplay area, 16:9
```

追加第 1 节统一前缀和负面词。

### 3.3 生产背景提示词

```text
production-ready background layer for level one, shallow epithelial wound surface,
strict orthographic top-down 16:9 composition,
one uninterrupted soft-edged S-shaped tissue groove of uniform width from upper-left to lower-right,
large low-detail coral epithelial plates, cream repair fibers, a few broad cell-membrane shapes far from the route,
eight subtle low-contrast circular clearings left empty around the lane,
faint cyan signal fibers embedded in tissue but not glowing,
no baked path arrows, no tower rings, no status effects, no characters,
quiet values under the future HUD at top and tower bar at bottom,
clean layered 2D field-guide background suitable for adding gameplay overlays in Unity
```

### 3.4 路径蒙版参考提示词

只作为人工描线参考，不直接作为碰撞：

```text
minimal top-down route mask on pure white background,
one solid black S-shaped path with perfectly continuous smooth edges and uniform width,
entry at upper-left edge, exit at lower-right edge, one wider oval boss area near right-center,
no texture, no gray, no shadow, no labels, no arrows, no extra branch, 16:9
```

### 3.5 关卡选择卡提示词

```text
small landscape level-card illustration of a healing epithelial wound surface,
one coral S-shaped tissue groove, cream fibrin patch, one subtle cyan signal chain,
warm hopeful palette, bold simple shapes readable at 240 by 135 pixels,
no enemies, no towers, no text, no UI frame, no gore
```

---

## 4. 第二关：组织深层

### 4.1 玩法构图

```text
入口 A：左上胶原裂隙
入口 B：左下淋巴/组织间隙
路线：两条独立弧线在画面中右部汇合，不交叉、不产生假支路
出口：右侧中央深层组织核心
塔位：10–12 个；包含信号枢纽位和 2 个感染塔位
信号图：两条采样链在中央枢纽合并，再通往末端清除区
Boss 区：汇合点后方的大型圆角空间
```

### 4.2 氛围概念图提示词

```text
wide concept art of a deep connective-tissue battlefield seen from strict top-down,
two pathogen routes enter separately from upper-left and lower-left collagen gaps,
curve through layered rose and burgundy tissue, then merge clearly at a central-right biological junction
before reaching one exit on the right edge,
large ribbon-like collagen fibers cross only in the background and never block the routes,
a glowing but infected violet signal hub sits between several pale attachment clearings,
two attachment clearings are partially covered by smoky pathogen matrix,
one broad biofilm-boss encounter area after the merge,
deeper and tenser than level one but still clean, non-gory and readable,
rose, burgundy, cream, cyan and restrained violet palette, 16:9, no characters
```

### 4.3 生产背景提示词

```text
production-ready top-down background for a deep connective-tissue level,
two continuous uniform-width organic lanes entering at upper-left and lower-left,
the lanes remain clearly separate and merge exactly once at the central-right,
one continuous final lane reaches the middle-right exit,
large soft collagen ribbons and rounded extracellular-matrix fields in muted rose and burgundy,
ten to twelve empty attachment clearings with two subtly discolored infected clearings,
one quiet central signal-hub clearing with faint cyan fibers leading to both route regions,
no baked glow, no shield, no characters, no tower icons, no arrows, no text,
low-detail safe bands behind top HUD and bottom tower bar, orthographic 16:9
```

### 4.4 双路线蒙版参考提示词

```text
minimal route mask on pure white background, strict 16:9 top-down plan,
two solid black smooth paths of identical width start at upper-left and lower-left,
curve without crossing and merge exactly once near the right-center,
one black path continues to the middle-right exit,
one wider rounded boss pocket immediately after the merge,
no texture, no gray, no shadow, no labels, no arrows, no extra branches
```

### 4.5 感染覆盖层提示词

生成透明覆盖候选，不能画进背景：

```text
single isolated organic infection patch for a 2D top-down tissue game,
asymmetric smoky-violet extracellular matrix with five broad lobes and two magenta pathogen nodules,
soft feathered but clean outer edge, two cyan-reactive fracture seams,
transparent background, flat cel shading, no route, no tower, no text, no gore
```

### 4.6 关卡选择卡提示词

```text
small landscape level-card illustration of deep connective tissue,
two rose lanes merging around one violet infected signal hub,
three broad cream collagen ribbons and restrained cyan fibers,
clear bold shapes readable at 240 by 135 pixels,
no enemies, no towers, no text, no UI frame, no gore
```

---

## 5. 第三关：血管防线

### 5.1 玩法构图

```text
入口：左侧主血流入口
路线：主路在中央分成上/下两支，靠近右侧重新汇合
事件：Boss 可切换一个血管阀门，让部分群落改变分支
出口：右侧健康微循环
塔位：12 个；含两个信号阀节点、两个高活性位和末端清除区
信号图：入口识别 -> 中央双分支 -> 两个阀门 -> 末端中继
Boss 区：中央分叉前后均留空间，阶段变化预警清楚
```

### 5.2 氛围概念图提示词

```text
wide concept art of a stylized blood-vessel defense network from strict orthographic top-down,
a broad dark-rose vascular channel enters from the left, forks cleanly into upper and lower curved branches at center,
then rejoins once near the right before reaching a healthy microcirculation exit,
vessel walls are soft layered membranes rather than a pool of blood,
two clear valve-like tissue folds sit at the branch entrances,
cyan-white immune signal fibers run along the vessel wall and split with the route,
twelve pale attachment niches are integrated into the wall, two with warm-orange active-tissue accents,
deep plum surroundings, rose vessel interior, cyan signals and sparse gold highlights,
strategic, urgent, clean and non-gory, no red blood cells filling the gameplay lane, 16:9
```

### 5.3 生产背景提示词

```text
production-ready top-down background for a branching vascular level,
one continuous uniform-width main vessel enters from the middle-left,
forks into exactly two clean curved branches near center-left,
both branches remain separate and rejoin exactly once near the right-center,
one final channel reaches the middle-right exit,
soft layered vessel walls in muted rose, burgundy and deep plum,
two visible but inactive membrane-valve folds at the fork,
twelve empty wall attachment niches and faint non-glowing cyan signal fibers,
no red blood-cell crowd, no characters, no arrows, no tower rings, no UI, no text,
calm top and bottom safe bands, strict orthographic 16:9, low-noise gameplay background
```

### 5.4 分叉路线蒙版参考提示词

```text
minimal route mask on pure white background, strict 16:9 top-down plan,
one solid black path enters from middle-left and forks exactly once into upper and lower branches,
both branches have equal readable width and rejoin exactly once near right-center,
one black path continues to middle-right exit,
no crossings, no third branch, no dead end, no texture, no gray, no labels, no arrows
```

### 5.5 血管阀门覆盖层提示词

```text
single isolated living vessel-valve overlay for a 2D top-down biological strategy game,
two soft crescent membrane flaps facing each other with a clear central passage,
deep rose and cyan edge accents, one small signal node on each flap,
flat cel shading, dark-plum outline, transparent background,
no metal gate, no heart, no blood, no path background, no text
```

### 5.6 关卡选择卡提示词

```text
small landscape level-card illustration of a branching living vessel,
one rose channel splitting into two around a deep-plum center and rejoining,
two cyan membrane valves and one gold signal pulse,
bold clean shapes readable at 240 by 135 pixels,
no enemies, no towers, no text, no UI frame, no gore
```

---

## 6. 信号图覆盖层的生成与实现

背景中只保留很淡的组织纤维。游戏内可交互信号图建议由 Unity 规则化绘制，AI 只提供材质/端点概念：

### 6.1 信号节点底图提示词

```text
single isolated biological signal-node socket for a 2D top-down tissue strategy game,
rounded organic attachment clearing with one central immune-white membrane pad,
four subtle cyan receptor notches around the edge, soft coral tissue rim,
flat cel shading, dark-plum detail outline, transparent background,
empty and ready for a living tower, no machine, no icon, no text, no glow beam
```

### 6.2 信号纤维纹理提示词

```text
single isolated curved biological signal fiber segment,
soft cyan-white membrane strand with a darker blue inner channel and tiny spaced lime receptor dots,
clean consistent width, gentle S curve, transparent background,
2D flat game overlay, no branching, no arrow, no electricity, no text
```

实际连接由 LineRenderer、SpriteShape 或分段 Sprite 根据 `SignalGraphDefinition` 生成；激活/阻断/样本传递再通过颜色、虚实和流动 UV 控制。

---

## 7. 宣传主视觉提示词

宣传图可以使用更自由构图，但不能拿来当地图背景：

```text
cinematic but still flat 2D illustrated key art for a game called Cell Defense: Wound Invasion,
a living coral tissue battlefield viewed from an elevated top-down angle,
six distinct immune organoids connected by glowing cyan signal fibers form a coordinated reaction chain,
magenta pathogen cohorts gather into a translucent violet biofilm along an S-shaped tissue route,
one neutrophil sentinel samples the front, a B-cell relay creates golden antibody marks,
a macrophage cleans the rear while orange inflammation builds at the edge,
clear visual story of recognition, signaling, effect and cleanup,
playful biological field-guide style, non-gory, no military towers,
leave clean negative space in upper-left for a title to be added later,
16:9, no generated text, no logo, no watermark
```

---

## 8. 地图验收表

- [ ] 路线连续、等宽，入口/出口/分叉/汇合数量与灰盒一致。
- [ ] 正式碰撞和 PathGraph 不从 AI 图片自动猜测。
- [ ] 塔位有足够留白，不与路线、HUD 和 Safe Area 冲突。
- [ ] 信号图在背景中不抢眼，激活覆盖层一眼可见。
- [ ] 第一关单路、第二关双入口汇合、第三关分叉再汇合的轮廓明显不同。
- [ ] 三关仍共享色板、轮廓、光源和组织材质。
- [ ] 地图没有写实伤口、血池、器官或医疗摄影感。
- [ ] 48 px 敌人放入背景后仍清晰，不被同尺寸装饰淹没。
- [ ] 背景、覆盖层和玩法节点分层保存。
- [ ] 原图、灰盒、提示词、工具、日期和许可记录完整。

---

## 9. 变更日志

| 日期 | 版本 | 变更 |
|---|---|---|
| 2026-08-10 | 1.0 | 为三关建立概念、生产背景、路线蒙版、覆盖层、缩略图和宣传图提示词，并明确 AI 地图不承担玩法数据 |

