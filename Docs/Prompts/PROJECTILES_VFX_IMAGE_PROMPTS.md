# 《细胞防卫战》子弹、命中与技能特效 AI 图片提示词

> 独立类别：只包含投射物、攻击反馈、状态覆盖和主动技能特效  
> 统一规范：[../ART_DIRECTION.md](../ART_DIRECTION.md)  
> 性能约束：[../PERFORMANCE_MANY_ENEMIES.md](../PERFORMANCE_MANY_ENEMIES.md)  
> 当前重点：为后续哨兵 `Projectile`/曳光表现预先定义统一的生物微脉冲外观；当前代码仍是空骨架

## 0. 使用原则

- 玩法伤害与视觉分离：高频哨兵攻击先立即结算，池化曳光只负责表现。
- AI 优先生成“单个组成元素”：光点、囊泡、裂纹、环、纤维和抗体；最终时序、缩放、旋转和颜色在 Unity 中组合。
- 不直接使用 AI 生成的一整张复杂 Sprite Sheet，除非人工校正每帧尺寸、Pivot、亮度和透明边。
- 特效缩小后必须先看起点、方向和命中点；漂亮但无法指示因果的特效不采用。
- 所有闪光遵守无障碍设置：可降低强度、关闭镜头震动，不使用持续高频全屏闪烁。

## 1. 统一 VFX 风格后缀

```text
stylized 2D biological game VFX element, clean hand-drawn vector shape,
flat cel-shaded color with a soft controlled inner glow, dark-plum edge where appropriate,
microscopic membrane and signal motif, readable silhouette, restrained particle count,
transparent background, centered, fully inside canvas, no contact shadow,
no text, no icon frame, no environment, no characters, no UI mockup
```

### 1.1 统一负面词

```text
photorealistic, gore, blood, pus, disgusting slime, realistic explosion,
fireball, bullet casing, metal ammunition, missile, rocket, laser gun,
3D render, lens flare, cinematic smoke, huge particle cloud, motion-blur background,
busy sparks, tiny noise, long cast shadow, square background,
text, letters, numbers, logo, watermark, border, UI screenshot
```

### 1.2 透明图通用规格

| 元素 | 源图建议 | 游戏内目标 | Pivot |
|---|---:|---:|---|
| 高频微脉冲/曳光 | 512×512 | 长 16–40 px | 中心或尾端 |
| 慢速囊泡弹 | 512×512 | 14–24 px | 中心 |
| 命中星芒/裂纹 | 512×512 | 20–48 px | 中心 |
| 地面酸化/纤维环 | 1024×1024 | 80–180 px | 中心 |
| 状态环/标记 | 512×512 | 24–64 px | 中心 |
| 全图技能纹理 | 2048×2048 | 按屏幕/世界组合 | 中心 |

---

## 2. 哨兵 Projectile 目标表现：抗原微脉冲

### 2.1 设计目标

后续哨兵攻击不画成实弹或激光。目标表现是一枚极短寿命的细胞膜微脉冲：前端采样、尾部留下青色曳光，命中后出现一枚黄绿色抗原碎片。权威伤害与视觉曳光是否使用同一 GameObject，等攻击原型和 Profiler 证据出现后再决定。

### 2.2 单体子弹提示词

```text
a single tiny biological micro-pulse projectile fired by a neutrophil sentinel,
short teardrop-shaped cyan-white membrane packet pointing to the right,
one bright immune-white leading tip, translucent cyan body,
two small lime antigen dots inside, one short tapered blue-cyan trail attached to the rear,
clean compact silhouette designed to remain readable at 16 to 24 pixels,
organic cell signal, not a physical bullet, no metal and no weapon casing
```

追加第 1 节风格后缀和负面词。

### 2.3 高频曳光提示词

```text
a single short neutrophil attack tracer for a 2D top-down game,
thin tapered cyan-white biological signal stroke with a rounded bright front and a soft broken tail,
slightly curved organic line, maximum three color bands, no particles around it,
designed for a 50 to 100 millisecond visual flash, horizontal orientation, transparent background,
not a laser beam, not electricity, not a bullet
```

### 2.4 命中提示词

```text
a compact neutrophil sampling impact mark,
one small cyan membrane ring opening outward with three lime antigen fragments at the center,
six rounded short rays, no sharp explosion spikes, transparent background,
readable at 24 pixels, biological sample collected rather than violent damage
```

### 2.5 已采样碎片提示词

```text
single lime antigen-sample status element made from three uneven soft membrane shards,
one short cyan dotted trail curving downward, bold dark-plum micro-outline,
flat 2D transparent game overlay, readable at 24 pixels, no text, no frame
```

---

## 3. 纤维蛋白壁垒特效

### 3.1 基础减速/群落隔离环

```text
a single top-down fibrin isolation field for a biological strategy game,
open circular ring woven from six broad blue-white organic fibers,
two deliberate gaps keep the center and route visible,
small calm cyan nodes at three intersections, very low-opacity inner fill,
clean transparent overlay, no ice crystals, no magic rune, no metal wire, no wall
```

### 3.2 群落信号切断

```text
a small gameplay VFX showing a pathogen quorum signal being separated,
one short broken violet dotted line crossed by a soft cyan-white fibrin strand,
two rounded endpoints, compact icon-like effect, transparent background,
no scissors, no red prohibition sign, no text
```

### 3.3 稳态降炎脉冲

```text
a calm biological homeostasis pulse,
one thin cyan-to-soft-green circular wave with three smooth inward membrane curves,
subtle and low intensity, large transparent center, no medical cross, no healing sparkles,
designed to expand and fade without covering enemies
```

---

## 4. 溶酶体酸化炮投射物与地面效果

### 4.1 慢速酸化囊泡弹

```text
a single membrane-bound lysosome capsule projectile,
round teal-lime vesicle with a thick cyan-white outer membrane and one warm-orange warning dot,
slightly squashed forward shape, two small internal bubbles, clean silhouette at 20 pixels,
contained biological acidification, no dripping slime, no glass vial, no grenade
```

### 4.2 囊泡飞行拖尾

```text
a short trail of three decreasing translucent cyan membrane bubbles,
largest bubble at the right and smallest at the left, gentle curved motion,
no smoke, no flame, no splashing liquid, transparent background
```

### 4.3 囊泡落地/命中

```text
a controlled lysosome capsule impact,
one teal membrane bubble opening into four rounded lobes,
small lime inner droplets remain close to the center, one short orange inflammation pulse at the edge,
no violent explosion, no slime splash, no gore, transparent background
```

### 4.4 酸化区域

```text
a top-down biological acidification zone for a 2D strategy game,
irregular but clean rounded patch formed by five overlapping translucent teal membrane pools,
several pale-lime vesicles and three cyan-reactive edge notches,
large transparent gaps so the route and units remain visible,
one restrained orange rim segment to indicate inflammation,
no toxic waste, no bubbling slime, no skull, no text
```

### 4.5 生物膜裂纹

```text
a single organic membrane fracture overlay,
two connected cyan-lit cracks with rounded ends and three tiny teal vesicles along the seam,
designed to sit over a smoky-violet biofilm shield,
transparent background, no broken glass, no rock texture, no explosion
```

---

## 5. 神经脉冲刺突特效

### 5.1 单段电弧

```text
a single short biological neural pulse arc,
clean angular but slightly soft blue-white line with three bends,
rounded synaptic bulbs at both endpoints and one cyan glow band around the center,
thin controlled silhouette, transparent background,
no lightning storm, no yellow electricity, no metal conductor, no background
```

### 5.2 群落连锁路径

不要生成一整条固定路线。使用上面的单段，在 Unity 中连接目标点并加入轻微随机折点。需要概念参考时：

```text
concept diagram of one blue-white neural pulse chaining through four marked pathogen positions,
four empty gold antibody rings connected by three short controlled organic electric arcs,
clear left-to-right order, generous spacing, plain transparent or neutral background,
no enemies, no text, no UI panel, no lightning cloud
```

### 5.3 信号封锁命中

```text
a compact neural interruption impact,
one blue-white synaptic flash shaped like two opposing rounded forks,
a violet quorum dotted line breaks at the center,
maximum eight broad rays, transparent background, readable at 32 pixels,
no stun stars, no comic text, no explosion smoke
```

### 5.4 电击无障碍替代

```text
a low-flash accessibility variant of a neural pulse impact,
two thick cyan arcs and one dark-blue expanding ring with no white center,
high shape readability and restrained brightness, transparent background,
no flickering particles, no lightning storm
```

---

## 6. 巨噬细胞清除与回收特效

### 6.1 伪足包裹弧

```text
a single soft macrophage pseudopod wrapping arc,
thick creamy-white organic crescent with a cyan membrane rim,
rounded ends curling inward around a large transparent center,
gentle engulfing motion shape, no mouth, no teeth, no tentacle horror, transparent background
```

### 6.2 清除收缩

```text
a non-gory cellular cleanup effect,
three concentric irregular membrane rings shrinking toward a small deep-blue digestion vesicle,
outer ring magenta-violet fading into immune cyan and white toward the center,
few large shapes, no fragments flying outward, no blood, no explosion
```

### 6.3 生物酶回收滴

```text
a single recycled biological enzyme droplet for a game resource pickup,
small warm-gold and lime membrane vesicle with one cyan-white highlight and dark-plum outline,
teardrop shape pointing upward, transparent background, readable at 16 pixels,
no coin, no dollar symbol, no bottle, no text
```

### 6.4 免疫记忆脉冲

```text
a short immune-memory propagation pulse,
one warm-gold broken ring carrying three tiny Y-shaped antibody motifs,
large transparent center, thin cyan outer echo, restrained glow,
designed to expand once and fade, no magic rune, no explosion, no text
```

---

## 7. 抗体 B 细胞标记与中继

### 7.1 抗体标记环

```text
a single antibody target marker for a 2D cell-defense game,
open warm-gold circular ring made of four bold Y-shaped antibody motifs,
large transparent center so the enemy remains visible,
one short cyan dotted connection at the lower edge, dark-plum micro-outline,
flat game overlay, readable at 32 pixels, no crosshair, no scope, no text
```

### 7.2 样本传递光点

```text
a single antigen-sample signal packet traveling through a biological network,
small lime membrane shard at the front followed by three evenly spaced cyan-white dots,
gentle curved horizontal path, transparent background,
no arrow, no laser, no text, no long trail
```

### 7.3 连续中继

```text
a compact relay-transfer effect between two antibody markers,
two small open gold Y-shaped arcs linked by one clean cyan dotted curve,
one lime sample spark halfway along the curve,
transparent background, no radio waves, no satellite icon, no text
```

### 7.4 护盾/加速剥离

```text
a biological buff-removal impact,
one warm-gold Y-shaped antibody opens a smoky-violet membrane ring at a single seam,
two small violet pieces curl inward and fade, cyan center remains clear,
no shattered glass, no explosion, no prohibited-sign icon, transparent background
```

---

## 8. 通用命中反馈

### 8.1 物理/微脉冲命中

```text
small soft six-ray cyan-white membrane starburst with rounded tips,
one dark-blue center dot, transparent background, no sharp weapon spark, no smoke
```

### 8.2 化学持续伤害 Tick

```text
small teal membrane bubble compressing around one lime dot,
one tiny orange edge notch, transparent background, no splash, no poison skull
```

### 8.3 控制/打断

```text
one blue rounded pulse ring with a clean break at the top and two short opposing arcs,
transparent background, no stun stars, no text
```

### 8.4 护甲受击但未破

```text
small muted violet organic wall ripple with three layered curved bands,
one cyan impact point but no crack, transparent background, no metal shield
```

### 8.5 破膜成功

```text
small smoky-violet membrane oval split by one bright cyan rounded seam,
two teal vesicles at the opening, transparent background, no broken glass
```

---

## 9. 群落行为 VFX

### 9.1 群落感应连线

```text
single short pathogen quorum-signal connection,
broken violet organic line with one pale-green pulse bead and rounded endpoints,
consistent thin width, transparent background, no electricity, no arrow, no text
```

### 9.2 三段群落量表元素

```text
one open three-segment arc meter made of broad violet membrane pieces,
segments separated by clear gaps, transparent center, dark-plum outline,
no numbers, no letters, no frame, no full UI panel
```

### 9.3 生物膜共享边缘

```text
a large top-down translucent biofilm boundary overlay,
asymmetric smoky-violet membrane loop with three broad thickness variations,
two subtle pale-green matrix nodes and two cyan-reactive fracture seams,
very transparent center, no bubble rainbow, no units, no text
```

### 9.4 孢子播散

```text
three large readable pale-green fungal spores leaving one cracked violet membrane sac,
short curved travel paths, no cloud of tiny dust, transparent background,
non-gory, no mushroom body, no text
```

---

## 10. 炎症与组织稳态 VFX

### 10.1 有效应答区间

```text
a restrained inflammation-response ring,
one thin warm-orange organic circle with four soft outward membrane pulses and a calm cyan inner echo,
large transparent center, balanced and controlled, no fire, no warning triangle, no text
```

### 10.2 过度炎症警告

```text
a high-inflammation biological warning overlay element,
two incomplete orange-red pulsing membrane arcs with three uneven pressure bulges,
one dark-plum break between arcs, large transparent center,
urgent shape without fire or blood, no text, no full-screen filter
```

### 10.3 细胞因子风暴（玩法改编）

```text
a controlled top-down cytokine-storm gameplay overlay,
several broad orange-red membrane waves spiraling gently around a large transparent center,
three bright but not white pulse nodes and a dark-plum outer warning rim,
low particle count, designed for optional reduced-flash mode,
no fire tornado, no blood, no lightning, no text
```

实现时使用 UI/Shader 控制透明度和脉动，不能在背景图片里永久烘焙。

---

## 11. 三个主动技能

### 11.1 发热反应

概念提示词：

```text
top-down biological fever-response skill VFX,
one wide warm-orange tissue pulse moves outward as three soft concentric membrane waves,
small pathogen silhouettes would be readable beneath the mostly transparent effect,
subtle cyan-white immune sparks appear at the inner ring,
controlled systemic response, no flames, no fireball, no full white flash, transparent background
```

### 11.2 抗体轰炸

```text
top-down targeted antibody-barrage skill VFX,
six large warm-gold Y-shaped antibody motifs descend in a clean spiral toward one cyan target ring,
one compact gold-white membrane impact at the center,
few large readable shapes, no missiles, no bombs, no crosshair, no text, transparent background
```

### 11.3 凝血封锁

```text
top-down temporary coagulation-blockade skill overlay,
two wide blue-white fibrin crescents grow from opposite sides of an invisible path and nearly meet,
thick woven organic fibers with rounded ends, clear passable gap remains,
three calm cyan anchor nodes, no stone wall, no ice, no metal barricade, transparent background
```

---

## 12. 生成动画参考的模板

### 12.1 径向效果四姿势

```text
four separate key stages of the exact same [VFX NAME] on one clean sheet:
small anticipation, clear activation, widest readable peak, almost-faded recovery,
identical center and color palette, no overlap, equal square cells, transparent or plain dark-neutral background,
flat 2D biological game VFX, no text, no frame, not a finished sprite sheet
```

### 12.2 投射物三姿势

```text
three separate key stages of the exact same [PROJECTILE NAME]:
contained projectile, impact compression, clean biological release,
same membrane structure and colors, equal spacing, horizontal orientation,
transparent or plain neutral background, no text, no environment
```

最终动画应尽量用单图的缩放、旋转、Trail、Particle System 或 Shader 完成，减少帧图数量与 AI 结构漂移。

---

## 13. 性能与可读性预算

| 类型 | 同屏软上限思路 | 达到上限后的降级 |
|---|---|---|
| 哨兵曳光 | 高频但寿命极短 | 每 N 发显示一次，伤害照常结算 |
| 慢速囊泡弹 | 与真实飞行攻击一一对应 | 简化拖尾，不能丢权威投射物 |
| 命中粒子 | 按屏幕区域/重要度预算 | 合并连续命中、保留 Boss/技能 |
| 伤害数字 | 按目标 0.2 秒合并 | 显示累计值 |
| 群落连线 | 每群落少量骨架线 | 只显示外圈/群落量表 |
| 地面区域 | 保留玩法边界 | 降低内部粒子和动画频率 |
| 全图技能 | 一次一个主层 | 关闭次级粒子与震动 |

特效池达到硬上限时只降低表现密度，不能改变伤害、状态、标记、群落或炎症结果。

---

## 14. 最终验收表

- [ ] 哨兵攻击资产包含“微脉冲、曳光、命中、样本碎片”四个可分层元素。
- [ ] 所有投射物看起来是生物结构，不是军事子弹、魔法或科幻武器。
- [ ] 六塔的攻击色与塔自身色板一致，但敌我轮廓不混淆。
- [ ] 高频效果在 16–32 px 可读，且没有过多细粒子。
- [ ] 已采样、已标记、群落感应、生物膜和炎症的视觉编码互不冲突。
- [ ] 背景、单位、路线和 UI 在最混乱场景仍可辨。
- [ ] Reduced Flash/关闭震动模式仍能用形状理解命中与技能。
- [ ] 池上限与视觉降级不会改变玩法结算。
- [ ] 原图、提示词、工具、日期和许可记录完整。

---

## 15. 变更日志

| 日期 | 版本 | 变更 |
|---|---|---|
| 2026-08-10 | 1.0 | 为哨兵攻击、六塔攻击、群落、炎症和三个主动技能建立独立提示词与性能边界 |

