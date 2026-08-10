# 《细胞防卫战》防御塔 AI 图片提示词

> 独立类别：只包含防御塔，不包含敌人、地图或投射物  
> 统一规范：[../ART_DIRECTION.md](../ART_DIRECTION.md)  
> 输出目标：六塔概念探索、透明背景游戏精灵、升级分支和图标

## 0. 使用方法

1. 先使用“轮廓探索提示词”生成 4–8 个方向，只选轮廓，不直接进游戏。
2. 再使用每座塔的“生产精灵提示词”，固定同一模型、版本、Seed、风格参考和色板。
3. LV2 保持主体结构，只增加一个明确部件；LV3 分支只改变约 25–35% 轮廓，玩家仍要认出基础塔。
4. 所有结果必须人工清理透明边缘、错误结构和颜色，并在 76–104 px 游戏尺寸测试。
5. 提示词中的生物结构是科学启发；具体攻击能力仍是玩法改编。

### 0.1 统一生产后缀

复制每座塔提示词时，把下面内容附在末尾：

```text
stylized 2D biological field-guide game art, playful microscopic world,
clean readable silhouette, rounded hand-drawn vector shapes, flat cel shading with 2 to 3 value bands,
dark plum outlines, subtle paper grain, consistent upper-left soft light,
cool cyan and immune white faction accents, gentle and educational, non-gory,
single isolated subject, centered, fully inside the canvas,
three-quarter top-down game sprite viewed from about 70 degrees,
transparent background, 12 percent empty margin, small soft contact shadow only,
no text, no icon frame, no environment, no extra characters
```

### 0.2 统一负面词

```text
photorealistic, medical photograph, gore, blood, open wound, horror, disgusting slime,
3D render, glossy plastic, metal turret, cannon, gun, military machine, medieval tower,
robot, vehicle, spaceship, human body, full humanoid character,
dramatic perspective, long cast shadow, depth of field, complex scenery,
busy particles, unreadable silhouette, cropped subject, duplicate organelles,
text, letters, numbers, logo, watermark, border, UI mockup
```

### 0.3 六塔 ID 对照

| 玩家可见名称 | 当前/旧设计标识 | 反应角色 |
|---|---|---|
| 中性粒细胞哨兵 | `SentryTCell` / SentryTower | 识别、采样、速射 |
| 纤维蛋白壁垒 | Bio Barrier Cell | 调节、隔离、减速 |
| 溶酶体酸化炮 | Acidic B Cell Launcher | 破膜、化学效应 |
| 神经脉冲刺突 | Neural Spiker | 群落中断、连锁效应 |
| 巨噬细胞清道夫 | Macrophage Devourer | 清除、回收、降炎症 |
| 抗体 B 细胞中继 | Viral Interceptor | 标记、记忆、信号中继 |

旧 ID 只用于和当前原型对照；最终 UI 与美术文件名采用玩家可见名称。不要通过手工修改 `.asset` 或 GUID 完成重命名。

---

## 1. 六塔共同轮廓探索

用于先检查六塔是否像同一阵营、同时足够不同：

```text
silhouette exploration sheet for six living immune defense organoids in one cohesive 2D game art style,
arranged in two rows of three with generous spacing, each shown at the same three-quarter top-down angle:
1) a compact neutrophil sentinel with a three-lobed nucleus and short sampling feelers,
2) a wide low fibrin barrier spreading a woven ring,
3) a pear-shaped lysosome acidifier with clustered membrane vesicles and one organic nozzle,
4) a tall branching neural pulse spire with dendrite-like arms,
5) a large irregular macrophage cleaner with wrapping pseudopods and a digestion chamber,
6) a symmetrical B-cell relay with Y-shaped antibody antennae and a golden memory ring,
all are living cellular structures, no mechanical weapons, clearly different black silhouettes,
minimal flat colors, cool cyan and white immune faction accents, dark plum outlines,
plain light gray background, clean concept sheet, no labels, no text, no UI, no environment
```

验收：把图片转成纯黑剪影后，六个对象仍可被正确命名；如果只能靠颜色区分，重新生成。

---

## 2. 中性粒细胞哨兵

### 2.1 设计锚点

- 体型：中等偏小、紧凑、略向入口方向探身。
- 必须结构：半透明膜、明显三叶/多叶细胞核、3–5 根短采样触须、背部样本囊。
- 配色：免疫白主体、青蓝膜边、信号黄绿样本点。
- 禁止：人形 T 细胞、步枪、炮管、机械眼、金属脚架。

### 2.2 生产精灵提示词

```text
a living neutrophil sentinel organoid designed as a defensive tower for a cell-themed strategy game,
compact round immune cell body leaning slightly forward, a clearly visible three-lobed deep-blue nucleus,
three to five short soft receptor feelers scanning the path, tiny lime antigen sample vesicles held near its back,
a small directional membrane pore for firing harmless-looking cyan micro-pulses, not a gun,
alert but friendly personality expressed only through posture, no human face,
immune-white body, cool cyan membrane rim, blue nucleus, lime sample accents,
strong compact silhouette readable at 80 pixels, living organic anatomy rather than machinery
```

追加 0.1 的统一生产后缀和 0.2 负面词。

### 2.3 LV2 追加句：趋化强化

```text
keep the exact same base body and silhouette identity,
add one brighter chemotaxis ring around the lower membrane and two neatly organized sample vesicles,
slightly more focused forward posture, only 20 percent more visual complexity
```

### 2.4 LV3-A：NET 捕获

```text
upgrade variant focused on extracellular trapping:
the short receptor feelers unfold into a controlled fan of delicate cyan-white biological fibers,
small lime sample sparks travel along the fibers, wider silhouette but still compact,
protective net imagery, not spider web horror, no extra weapon
```

### 2.5 LV3-B：精准趋化

```text
upgrade variant focused on precision sampling:
keep most feelers folded, extend one elegant long receptor cone with a bright lime sensor tip,
enlarge and clarify the segmented nucleus, narrower forward-pointing silhouette,
calm specialist posture, no scope, no rifle, no mechanical sniper references
```

### 2.6 塔卡图标提示词

```text
simple emblem of a three-lobed immune-cell nucleus surrounded by three short receptor feelers,
white and cyan with one lime antigen spark, bold dark-plum outline,
flat 2D vector game icon, centered on transparent background, readable at 32 pixels,
no text, no square frame, no gradient, no shadow
```

---

## 3. 纤维蛋白壁垒

### 3.1 设计锚点

- 体型：六塔中最宽、最低，像稳固的组织结点。
- 必须结构：血小板启发的中心体、向外编织的纤维蛋白环、两侧锚点。
- 配色：免疫白/浅青主体，纤维偏蓝白，稳态微光为柔和青绿。
- 禁止：砖墙、盾牌骑士、冰塔、铁丝网、石头城堡。

### 3.2 生产精灵提示词

```text
a living fibrin barrier organoid for a cell-themed tower-defense game,
wide low platelet-inspired central body anchored to tissue by two soft membrane pads,
an elegant woven ring of thick biological fibrin fibers spreading horizontally around it,
the center gently pulls and organizes the fibers like a living loom,
stable protective silhouette, broad base and low height, no vertical cannon,
immune white and pale cyan body, blue-white fibers, tiny calm green homeostasis glow,
clearly organic and soft, readable as slowing and isolating rather than attacking
```

### 3.3 LV2 追加句：纤维致密

```text
keep the same low wide base, add a second partially interwoven fiber layer and brighter anchor pads,
do not increase height, preserve open gaps so the sprite does not become a solid blob
```

### 3.4 LV3-A：隔离纤维

```text
upgrade variant focused on separating pathogen groups:
the fibrin ring opens into two curved crescent barriers with a clear channel between them,
cyan severed-signal symbols appear as small broken dotted lines near the crescents,
strong horizontal silhouette, no medieval barricade, no ice
```

### 3.5 LV3-B：稳态凝固

```text
upgrade variant focused on calming tissue:
the outer fibers curl back into a balanced circular nest around the platelet core,
three gentle green-cyan breathing nodes are embedded in the ring,
more symmetrical and tranquil silhouette, restrained light, no healing cross symbol
```

### 3.6 塔卡图标提示词

```text
simple emblem of three interwoven fibrin strands forming an open protective ring,
white and cyan with a tiny calm green node, bold dark-plum outline,
flat 2D vector icon, transparent background, readable at 32 pixels,
no text, no wall, no shield, no frame
```

---

## 4. 溶酶体酸化炮

### 4.1 设计锚点

- 体型：梨形/囊形，底部稳定，顶部一个有机囊泡喷口。
- 必须结构：多个大小不同的溶酶体囊泡、膜融合通道、警示橙小节点。
- 配色：青白外膜、青绿色囊泡、少量炎症橙；不要整体毒绿。
- 禁止：化学枪、金属炮、玻璃烧瓶、写实腐蚀、黏液怪。

### 4.2 生产精灵提示词

```text
a living lysosome acidifier organoid used as a biological defense tower,
stable pear-shaped immune cell body with several clearly separated acidic membrane vesicles visible inside,
one short organic exocytosis funnel at the top, formed from folded cell membrane rather than a cannon barrel,
vesicles are teal-lime with tiny warm-orange warning dots, surrounded by an immune-white and cyan membrane,
slightly asymmetric silhouette suggesting stored pressure, but still friendly and controlled,
designed to launch membrane-bound acidifying capsules that break pathogen biofilm,
non-gory, no liquid splashing around the body, no laboratory equipment
```

### 4.3 LV2 追加句：囊泡储备

```text
keep the same pear-shaped body, show three larger organized vesicles and a stronger membrane fusion seam,
slightly brighter funnel rim, no extra nozzle and no increase in overall footprint
```

### 4.4 LV3-A：破膜酸化

```text
upgrade variant focused on breaking protective membranes:
the upper funnel becomes a short flower-like membrane aperture with four thick lobes,
acidic vesicles display a crisp cracked-shell motif, teal and lime with restrained orange warning accents,
broader upper silhouette, purposeful but not violent, no metal cannon
```

### 4.5 LV3-B：定向囊泡

```text
upgrade variant focused on precision and lower collateral inflammation:
the body becomes more symmetric, vesicles align in one clear vertical channel,
the funnel narrows into a soft membrane nozzle with a cyan targeting receptor beside it,
less orange, more clean cyan-white, compact precise silhouette, no gun or syringe
```

### 4.6 塔卡图标提示词

```text
simple emblem of three membrane vesicles moving toward a four-lobed exocytosis opening,
teal, lime and immune white with a tiny orange warning point, dark-plum outline,
flat 2D vector icon, transparent background, readable at 32 pixels,
no flask, no skull, no text, no frame
```

---

## 5. 神经脉冲刺突

### 5.1 设计锚点

- 体型：六塔中最高、最窄，形成明显竖向轮廓。
- 必须结构：树突状分叉、蓝白发光核心、底部与组织信号线相连。
- 配色：深蓝/免疫青/白，少量炎症橙只在过载状态出现。
- 禁止：特斯拉线圈、金属避雷针、魔法雷塔、机械天线。

### 5.2 生产精灵提示词

```text
a living neural pulse spire grown from tissue for a cell-themed strategy game,
tall slender organic core with several elegant dendrite-like branches,
a visible blue-white synaptic nucleus inside a translucent cyan membrane,
branches end in rounded receptor bulbs rather than sharp metal points,
two roots connect naturally to biological signal fibers at the base,
upright branching silhouette clearly different from all round immune cells,
tiny restrained electric arcs contained between branch tips, calm before firing,
organic nervous-tissue inspiration, no machine, no magic tower
```

### 5.3 LV2 追加句：传导增幅

```text
preserve the main trunk, add two secondary dendrite branches and one brighter synaptic ring,
keep all branch endings rounded and keep the silhouette clean at small scale
```

### 5.4 LV3-A：信号封锁

```text
upgrade variant focused on interruption:
branches spread into a wider fork with three blunt receptor bulbs forming a clear stop-like gap,
a short broken violet pathogen signal line is suspended between them,
blue-white controlled pulse, no stun stars, no metal fork
```

### 5.5 LV3-B：同步电弧

```text
upgrade variant focused on chaining through a marked cohort:
branches curl into a graceful spiral crown around the central synaptic core,
three small gold antibody sparks align with the blue-white arc path,
taller energetic silhouette, restrained electricity, no lightning storm
```

### 5.6 塔卡图标提示词

```text
simple emblem of a branching organic dendrite with three rounded tips and one blue-white pulse core,
cyan and deep blue with bold dark-plum outline,
flat 2D vector icon on transparent background, readable at 32 pixels,
no metal antenna, no text, no frame
```

---

## 6. 巨噬细胞清道夫

### 6.1 设计锚点

- 体型：最大、最柔软、略不规则，贴近路线。
- 必须结构：包裹式伪足、明显吞噬腔、回收囊泡和偏心核。
- 配色：乳白/青蓝主体，消化腔为深蓝紫，回收生物酶为金绿小滴。
- 禁止：大嘴怪、牙齿、舌头、食人花、血盆大口。

### 6.2 生产精灵提示词

```text
a large living macrophage cleaner organoid for a microscopic defense game,
broad irregular immune cell body resting close to the tissue path,
two soft wrapping pseudopods form an open protective cradle around a visible internal digestion chamber,
one off-center deep-blue nucleus and several small recycling vesicles,
the opening is a membrane pocket with no teeth, tongue, or monster mouth,
strong wide asymmetrical silhouette suggesting engulfing and cleanup,
creamy immune-white body, cyan membrane edges, deep blue-violet chamber,
two tiny gold-lime enzyme droplets stored safely near the rear,
gentle but powerful, non-gory, not a creature with a face
```

### 6.3 LV2 追加句：伪足加速

```text
keep the same broad body, extend the two pseudopods slightly forward and clarify three recycling vesicles,
make the digestion chamber more readable without turning it into a mouth
```

### 6.4 LV3-A：消化回收

```text
upgrade variant focused on enzyme recycling:
add a neat chain of three gold-lime recycling vesicles flowing from the digestion chamber to a rear storage lobe,
pseudopods remain soft and open, brighter calm cyan membrane, no coins and no money symbol
```

### 6.5 LV3-B：巨物裂解

```text
upgrade variant focused on handling large targets:
the two pseudopods become thicker and form a larger incomplete ring,
the internal chamber shows a restrained segmented blue pattern that suggests controlled breakdown,
heavier silhouette but still cellular and soft, no claws, teeth, blades, or gore
```

### 6.6 塔卡图标提示词

```text
simple emblem of two curved pseudopods wrapping around one small membrane vesicle,
immune white and cyan with a deep-blue center, bold dark-plum outline,
flat 2D vector game icon, transparent background, readable at 32 pixels,
no mouth, no teeth, no text, no frame
```

---

## 7. 抗体 B 细胞中继

### 7.1 设计锚点

- 体型：最对称、最精致，圆形主体加清晰 Y 形结构。
- 必须结构：Y 形抗体天线、金色记忆核、与信号节点连接的中继环。
- 配色：免疫白/青蓝，金色是主要区别色；不能整体金黄。
- 禁止：卫星天线、雷达、狙击镜、十字架、字母 Y 文本。

### 7.2 生产精灵提示词

```text
a living antibody B-cell relay organoid for a cell-themed tower-defense game,
balanced round immune cell with a translucent cyan-white membrane,
three elegant Y-shaped antibody receptors growing organically from the upper membrane,
a warm golden memory nucleus at the center surrounded by a thin cyan signal relay ring,
two short membrane roots connect to the map's biological signal fibers,
highly symmetrical calm silhouette, designed to receive antigen samples and mark matching pathogens,
immune white and cyan body, deep blue shadow, restrained gold antibody accents,
no mechanical radar, no weapon, no human face
```

### 7.3 LV2 追加句：亲和成熟

```text
keep the same round body and three Y-shaped receptors,
make the central golden memory nucleus clearer and add one thin secondary cyan relay orbit,
only slight extra detail, preserve the clean symmetrical silhouette
```

### 7.4 LV3-A：克隆记忆

```text
upgrade variant focused on memory propagation:
the golden nucleus divides visually into three linked warm-gold lobes inside one membrane,
small Y-shaped receptors repeat in a controlled halo, no more than six,
soft outward memory pulse indicated by one broken gold ring, not an explosion
```

### 7.5 LV3-B：连续中继

```text
upgrade variant focused on rapid relay:
the cyan signal ring becomes two offset organic arcs with small gold antibody sparks traveling between them,
the three main Y-shaped receptors point toward different directions,
dynamic but still balanced silhouette, no satellite dish, no sci-fi antenna
```

### 7.6 塔卡图标提示词

```text
simple emblem of one bold Y-shaped antibody above a small golden memory nucleus inside a cyan ring,
immune white, cyan and warm gold with dark-plum outline,
flat 2D vector icon, transparent background, readable at 32 pixels,
no letters, no medical cross, no text, no frame
```

---

## 8. 同一塔的动画关键姿势提示模板

AI 只提供姿势参考，不直接输出最终 Sprite Sheet：

```text
four separate key poses of [TOWER NAME] on one clean concept sheet:
idle breathing, sensing or aiming anticipation, biological attack release, brief recovery,
exact same body proportions, colors, organelles and camera angle in every pose,
each pose isolated with equal spacing, no overlap, plain light gray background,
2D field-guide game art, flat cel shading, no text, no frame, not a final sprite sheet
```

若四姿势的细胞核数量、触须、颜色或体积发生漂移，只把它当构图灵感，不能直接切图使用。

---

## 9. 塔类最终验收表

- [ ] 六塔黑色剪影可区分。
- [ ] 所有塔使用相同视角、轮廓宽度、光源和接触影。
- [ ] 没有任何塔像金属炮台、枪械或传统魔法塔。
- [ ] LV3 分支仍能认出基础塔，且分支差异能在 96 px 看见。
- [ ] 识别/调节/破膜/中断/清除/记忆职责能从结构推测。
- [ ] 中性粒细胞、纤维蛋白、溶酶体、巨噬细胞和抗体结构没有明显 AI 错误。
- [ ] 玩家可见名称与总策划一致，旧 ID 仅作为迁移备注。
- [ ] 原图、提示词、生成工具、日期与许可记录完整。

---

## 10. 变更日志

| 日期 | 版本 | 变更 |
|---|---|---|
| 2026-08-10 | 1.0 | 为六塔建立独立生产精灵、升级分支、图标与姿势提示词，并保留当前代码 ID 对照 |

