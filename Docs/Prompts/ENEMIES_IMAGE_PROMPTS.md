# 《细胞防卫战》敌人与 Boss AI 图片提示词

> 独立类别：只包含敌人、病原群落和 Boss  
> 统一规范：[../ART_DIRECTION.md](../ART_DIRECTION.md)  
> 输出目标：四类核心敌人、群落状态、三关 Boss 与图标

## 0. 科学与玩法边界

- “葡萄球菌簇”和“破伤风梭菌鼓槌形”借鉴真实形态。
- “鞭毛双球菌”“变异超级真菌”“细菌生物膜核心”“病毒母体”均包含明显玩法改编。
- 病毒、细菌、真菌的真实尺度差异不用于游戏内尺寸；图鉴必须标注“为玩法可读性调整比例”。
- 不把疾病严重程度、传播方式或治疗方式从游戏伤害倍率中直接推导。

## 1. 统一敌人生产后缀

```text
stylized 2D biological field-guide game art for a top-down tower-defense game,
playful microscopic pathogen design, asymmetric organic silhouette,
rounded hand-drawn vector shapes with a few controlled jagged details,
flat cel shading with 2 to 3 value bands, dark plum outlines, subtle paper grain,
magenta and violet pathogen palette with restrained toxic-green ability accents,
consistent upper-left soft light, gentle educational tone, threatening but not horrifying,
single isolated subject, centered, fully inside canvas,
three-quarter top-down game sprite viewed from about 70 degrees,
transparent background, 12 percent empty margin, small soft contact shadow only,
no text, no icon frame, no environment, no extra unrelated characters
```

### 1.1 统一负面词

```text
photorealistic microscope image, medical photograph, gore, blood, open wound,
horror, realistic parasite, disgusting slime, pus, wet flesh, teeth, human eye,
3D render, glossy plastic, metal armor, machine, robot, weapon, zombie,
dramatic cinematic lighting, long shadow, depth of field, complex scenery,
busy particles, cute mascot with arms and legs, full humanoid face,
cropped subject, duplicate structures, text, letters, number, logo, watermark, border, UI
```

---

## 2. 四类核心敌人轮廓探索

```text
silhouette exploration sheet for four microscopic pathogen enemies in one cohesive 2D strategy-game style,
same three-quarter top-down angle and consistent scale guide:
1) a grape-like cluster of small cocci with an uneven cluster outline,
2) a fast paired oval bacterium with two long curved flagella and a forward-leaning body,
3) a heavy drumstick-shaped spore-forming rod bacterium with layered organic armor,
4) a large mutant fungus boss with a mushroom cap, branching hyphae and three spore sacs,
all silhouettes clearly different even in pure black,
magenta and violet pathogen faction, restrained toxic-green spores,
plain light gray background, no labels, no text, no UI, no environment, no gore
```

验收：普通、快速、重甲和 Boss 的体型/动势一眼可分；不要只靠“大小不同”。

---

## 3. 葡萄球菌群

### 3.1 设计锚点

- 不是一个有手脚的圆球，而是 5–9 个小球组成的单个游戏单位。
- 外轮廓像不规则葡萄串，中间有 1–2 个偏心暗核点用于朝向提示。
- 移动时小球轻微弹性重排，为群落聚集玩法提供视觉基础。

### 3.2 单位生产精灵提示词

```text
a small staphylococcus-inspired cocci cluster enemy for a cell-themed strategy game,
seven rounded bacterial spheres packed into one uneven grape-like unit,
slight differences in sphere size, compressed cluster silhouette with one small trailing sphere,
magenta membranes, violet shaded gaps, two tiny pale-green chemical dots inside separate spheres,
subtle mischievous direction through body tilt only, no human face, no arms or legs,
simple compact fodder-enemy silhouette readable at 48 pixels,
biologically inspired cluster rather than a single cartoon ball
```

追加第 1 节统一后缀和负面词。

### 3.3 群落聚集提示词

概念图用于设计 3 个单位如何组成群落，不作为单体精灵：

```text
three staphylococcus-inspired cocci cluster units gathering into one pathogen cohort,
short broken violet quorum-signal lines between clusters, one shared three-segment signal meter represented only by shapes,
units remain visually separate and do not merge into an unreadable blob,
top-down gameplay readability study on a plain pale-coral tissue patch,
flat 2D field-guide style, no text, no UI frame, no immune cells
```

### 3.4 生物膜软适应提示词

```text
the same cocci cluster enemy with a thin shared biofilm adaptation,
one translucent smoky-violet membrane wraps around the entire cluster,
the membrane has two clear cyan-reactive fracture seams and does not hide individual spheres,
silhouette remains compact, shield is organic and soft, no metal armor, no soap bubble rainbow
```

### 3.5 图标提示词

```text
simple game icon of five uneven magenta cocci arranged like a grape cluster,
violet gaps and bold dark-plum outline, transparent background, readable at 32 pixels,
flat 2D vector, no face, no text, no frame
```

---

## 4. 鞭毛双球菌

### 4.1 设计锚点

- 两个连接的椭圆菌体形成前后方向。
- 两条主鞭毛形成向后流动的 S 曲线；避免细碎毛发。
- 主色更亮、更高对比，轮廓前倾，体现高速而不是靠运动模糊。

### 4.2 单位生产精灵提示词

```text
a fast flagellated diplococcus-inspired enemy for a microscopic strategy game,
two connected streamlined oval bacterial bodies leaning forward as one unit,
two long clean S-curved flagella trailing behind and one short sensory filament,
bright magenta front body, violet rear body, thin toxic-green energy seam between the pair,
strong directional arrow-like silhouette created by anatomy, no actual arrow symbol,
agile and slippery but non-humanoid, readable at 48 pixels,
no motion blur, no speed lines in the base sprite
```

### 4.3 集群冲刺状态提示词

```text
the same paired flagellated bacterium in a synchronized swarm-sprint state,
flagella extend into two parallel clean curves, body compresses slightly forward,
one short violet quorum pulse behind the unit and a small green-white leading edge,
keep the exact anatomy and colors, no flame exhaust, no vehicle, no motion-blur background
```

### 4.4 被凝血隔离状态提示词

```text
the same fast diplococcus unit gently caught by two blue-white fibrin arcs,
flagella remain visible but lose their synchronized curve,
one broken violet signal line below it, readable gameplay status, no ice, no cage, no chains
```

### 4.5 图标提示词

```text
simple game icon of two connected magenta-violet ovals with two bold curved flagella,
strong forward direction, dark-plum outline, transparent background,
flat 2D vector, readable at 32 pixels, no text, no speed-line frame
```

---

## 5. 破伤风梭菌启发的重甲菌

### 5.1 设计锚点

- 主轮廓为粗长杆，末端有显著圆形芽孢，形成鼓槌/球拍形。
- “重甲”来自厚细胞壁层片和芽孢壳，不是钢板铠甲。
- 移动缓慢、重心低，受击时层片裂开。

### 5.2 单位生产精灵提示词

```text
a heavy spore-forming rod bacterium inspired by Clostridium tetani morphology,
thick elongated magenta-violet rod body ending in one large rounded terminal spore,
three overlapping organic cell-wall lamellae wrap around the body like natural layered armor,
the terminal spore has a muted pale-green core under a violet shell,
low heavy drumstick-shaped silhouette, slightly bent as if pushing forward through tissue,
no metal plates, no helmet, no medieval armor, no weapon,
designed as a slow high-armor enemy readable at 64 pixels
```

### 5.3 破膜状态提示词

```text
the same armored spore-forming rod after biological membrane disruption,
two outer wall lamellae show clean teal-reactive cracks and peel slightly away,
inner magenta body remains intact, terminal spore remains recognizable,
restrained membrane vesicles around the cracks, no gore, no acid slime, no explosion
```

### 5.4 吞噬阈值状态提示词

```text
the same heavy bacterium with its outer layers weakened and compressed inward,
one clear cyan circular cleanup marker underneath and a small gold antibody tag above,
do not include the macrophage itself, no text, no death effect
```

### 5.5 图标提示词

```text
simple game icon of a thick violet rod with one large round terminal spore and three layered wall bands,
magenta inner body, dark-plum outline, transparent background,
flat 2D vector, readable at 32 pixels, no armor plate, no text, no frame
```

---

## 6. 第一关 Boss：变异超级真菌

### 6.1 设计锚点

- 大伞盖不是普通蘑菇，底部连接分叉菌丝，三枚孢子囊对应阶段。
- Boss 轮廓保持“伞盖 + 菌丝 + 孢子囊”，每阶段只改变囊体和裂缝。
- 分裂小怪和塔瘫痪分别用孢子播散、菌丝信号干扰表现。

### 6.2 基础 Boss 生产精灵提示词

```text
a large mutant fungus boss for a playful microscopic tower-defense game,
broad asymmetrical violet mushroom cap with a deep-magenta underside,
three distinct pale-green spore sacs embedded along one side of the cap,
multiple thick branching hyphae spread from a compact central body and curl forward along tissue,
one cracked bioluminescent core visible beneath the cap,
ominous but not horrific, no eyes, mouth, teeth, or humanoid limbs,
strong boss silhouette readable at 150 pixels, clear empty spaces between major hyphae,
designed for three damage phases while preserving the same core identity
```

### 6.3 阶段一：休眠侵入

```text
phase one of the same mutant fungus boss,
all three spore sacs closed, hyphae gathered close to the body,
core glow dim, cap mostly intact, restrained violet and magenta palette,
calm looming posture, exact same camera and proportions as later phases
```

### 6.4 阶段二：孢子播散

```text
phase two of the exact same fungus boss,
one spore sac split into three large readable floating spores near the cap,
hyphae extend wider and one lime-violet signal pulse travels toward nearby tissue nodes,
the cap gains one clear fracture, no cloud of tiny particles, no scene background
```

### 6.5 阶段三：菌丝干扰

```text
phase three of the exact same fungus boss,
two spore sacs collapsed, final sac glowing intensely, hyphae form three deliberate hooked branches,
blue-cyan signal lines near the roots appear disrupted by short violet static shapes,
cap partially folded but main silhouette remains recognizable,
urgent, no gore, no lightning storm, no immune towers in the image
```

### 6.6 Boss 图标提示词

```text
simple boss icon of an asymmetrical violet fungal cap with three pale-green spore sacs and two branching hyphae,
magenta underside, bold dark-plum outline, transparent background,
flat 2D vector, readable at 48 pixels, no skull, no face, no text, no frame
```

---

## 7. 第二关 Boss：细菌生物膜核心

### 7.1 设计锚点

- 多种球/杆菌被一层巨大共享基质包裹，中间有周期脉动核心。
- Boss 本体是“群落结构”，不是一只巨大细菌。
- 护盾、恢复、感染塔位来自基质纤维，不用魔法光环。

### 7.2 生产精灵提示词

```text
a bacterial biofilm-core boss formed by a whole pathogen community,
an asymmetric oval colony containing several visible magenta cocci clusters and short violet rod bacteria,
all embedded inside one thick translucent smoky-violet extracellular matrix,
a pulsing pale-green quorum core sits off-center, connected by four broad organic matrix strands,
outer membrane has clear layered edges and several cyan-reactive fracture seams,
large low wide silhouette, visually a colony rather than one monster,
threatening support boss, no face, no metal shield, no soap-bubble rainbow, no gore
```

### 7.3 护盾脉冲状态

```text
the exact same biofilm-core boss sending one support pulse,
outer matrix expands into a single clean violet ring with four thick connection arcs,
inner bacterial members remain readable, green core brightens,
no tiny particle cloud, no magic rune, no background enemies
```

### 7.4 破膜状态

```text
the exact same biofilm-core boss after membrane disruption,
two large cyan-lit cracks open through the outer matrix, one connection strand severed,
green core exposed but still intact, colony members do not spill out,
no gore, no shattered glass, no explosion
```

---

## 8. 第三关 Boss：病毒母体（玩法尺度）

### 8.1 设计锚点

- 使用抽象包膜病毒结构：圆形包膜、表面蛋白、内部螺旋核酸。
- 三阶段分别强调复制、变异和路线信号劫持。
- 不使用现实中特定人类病毒的可识别品牌式图像，避免错误暗示。

### 8.2 生产精灵提示词

```text
a fictional enveloped virus-matrix boss enlarged for gameplay readability,
large asymmetrical violet-magenta membrane sphere with a partially open cutaway showing one coiled luminous core,
surface carries several uneven protein clusters shaped like soft forks and short spikes, not a perfect coronavirus,
three orbiting membrane vesicles represent replication stages,
one side extends a ribbon-like signal-hijack appendage toward tissue pathways,
elegant alien biological silhouette, no face, no machine, no horror, no photorealism,
clearly labeled by design as fictional gameplay scale, cohesive 2D field-guide style
```

### 8.3 阶段变体追加句

复制阶段：

```text
phase focused on replication: three orbiting vesicles become clear and evenly spaced,
core remains contained, appendage folded, silhouette calm and round
```

变异阶段：

```text
phase focused on mutation: membrane becomes slightly asymmetric,
two surface-protein clusters change shape and the inner coil changes from one loop to a double helix-like abstract ribbon,
preserve the same colors and total size, no random extra limbs
```

劫持阶段：

```text
phase focused on route-signal hijacking: one ribbon appendage unfolds into a fork matching two path directions,
short broken cyan signal lines appear at the tips, core glows warm magenta,
no map background, no towers, no lightning
```

---

## 9. 状态叠加图标提示词

每个图标单独生成，不让模型在一张图中写标签：

| 状态 | Prompt 核心 |
|---|---|
| 已采样 | `one lime antigen fragment made of three uneven membrane shards, dotted cyan trail` |
| 已标记 | `one bold warm-gold Y-shaped antibody ring, small open center` |
| 群落感应 | `three violet pathogen dots connected by two short broken lines, three-segment arc above` |
| 生物膜 | `one smoky-violet organic membrane oval with two cyan fracture seams` |
| 集群冲刺 | `paired magenta oval bodies and two synchronized S-curved flagella` |
| 孢子播散 | `three pale-green fungal spores leaving one cracked violet sac` |

统一后缀：

```text
single flat 2D vector game-status icon, bold dark-plum outline,
transparent background, centered, readable at 24 pixels,
no text, no number, no square frame, no shadow, no gradient
```

---

## 10. 动画关键姿势模板

```text
four key poses of the exact same [ENEMY NAME] on one clean concept sheet:
idle locomotion contact, stretched movement pose, clear hit-reaction compression, non-gory cleanup or dissolution,
identical anatomy, color, organelle count, camera angle and scale in every pose,
equal spacing, plain light-gray background, 2D flat field-guide game art,
no text, no frame, no environment, not a final sprite sheet
```

群落单位再增加一组“分散 -> 感应上升 -> 群落行为 -> 被隔离”整体姿势参考，但最终群落连线和量表优先由 Unity 规则化生成。

---

## 11. 敌人类最终验收表

- [ ] 四类核心敌人在黑色剪影和 48–64 px 下可区分。
- [ ] 敌人保持洋红/紫阵营基线，能力色不篡夺主体。
- [ ] 群落状态是一组共享视觉，不在每个敌人上叠大量图标。
- [ ] 生物膜、冲刺、孢子和适应均有明确预告/破坏状态。
- [ ] Boss 阶段变化保留主轮廓，玩家不会误认为换了一只怪。
- [ ] 没有写实病理、血腥、恐怖或错误文字。
- [ ] 图鉴标出形态启发、比例调整和玩法改编。
- [ ] 原图、提示词、工具、日期和许可记录完整。

---

## 12. 变更日志

| 日期 | 版本 | 变更 |
|---|---|---|
| 2026-08-10 | 1.0 | 为四类核心敌人、群落状态和三关 Boss 建立独立提示词与科学边界 |

