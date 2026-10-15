# 前五关地图讨论图：提示词记录

日期：2026-10-15。使用内置 imagegen 工具；参考图为当前三张开放地图背景。最终 PNG 已复制到项目指定美术目录，保留生成目录原图与既有素材。

## 初始生成

```text
Use case: stylized-concept. Asset type: ONE five-panel level layout discussion sheet for the existing Unity game 胞卫战, not five finished production maps.
Input images 1, 2 and 3 are STYLE REFERENCES ONLY: existing game backgrounds. Faithfully match their exact 2D orthographic overhead, softly pixel-textured hand-painted organic tissue style, warm coral / salmon / peach tissue floor, cream layered membrane rims, round pink tissue cells, magenta wound-mouth entrances, and the same small teal-outlined cluster of three pale cells as the protected core. Keep the low-contrast floor textures and readable large rounded organic obstacle silhouettes. No perspective, no change of art direction.
Create a high-resolution wide planning sheet: five similarly sized LANDSCAPE map panels, three on the top row and two on the bottom row, with a small sixth legend panel. Pale cream gutters and dark warm-brown readable Chinese titles outside each map. Exact top title: "胞卫战 · 前五关地图布局讨论稿". Small subtitle: "沿用当前画风 · 空间结构提案".
Each map must remain an OPEN ORGANIC TISSUE ARENA. NO painted roads, NO fixed tower sockets, NO arrows, NO grid, NO towers, NO enemies, NO characters, NO interface. Ordinary floor areas are all visually open for free placement. Obstacles are isolated raised pink tissue islands with unmistakable cream membrane borders, never decorative structures that could be confused with units. Outer rim remains intact except at the EXACT listed entrance openings; no extra dark holes or entry mouths elsewhere. The core must be fully visible and approachable with ample deployment space, never enclosed by obstacles.
Panel 1 title "01 守住伤口": EXACTLY ONE entrance in the left edge at mid-height. Core right of center at roughly 72% width, 50% height. One small tissue island at roughly 40% width, 50% height, leaving generous upper and lower alternatives around it. Very simple and spacious, no maze.
Panel 2 title "02 集中清除": EXACTLY TWO entrances on the left edge, at 25% and 75% height. Core at 75% width, 50% height. Two tissue islands at 43% width, one at 22% height and one at 78% height. A broad open common fighting space between the islands and core, so the player can bring both streams into one area while retaining alternative routes around the islands.
Panel 3 title "03 认出敌人": EXACTLY TWO entrances on the left edge, at 30% and 70% height. Core at 75% width, 50% height. One compact island at 35% width and 50% height, then a generous unobstructed central-right support area between 48% and 68% width. Enough space for scouts on the two approaches and a shared support position between them. No added structures or node sockets: convey this only with open space.
Panel 4 title "04 两侧告急": EXACTLY TWO entrances: left edge at 25% height, right edge at 75% height. Core in exact center. Two vertical short tissue islands at 30% and 70% width, centered vertically, each with open upper and lower ends. Visually separate left and right approach areas, preserve the open central space. Match the third style-reference map's vertical organic islands but use the specified offset entry positions.
Panel 5 title "05 感染加剧": EXACTLY THREE entrances: left edge at 20% and 80% height, right edge at 50% height. Core exact center. Three small asymmetric tissue islands: one left of center at 30% width / 38% height; one upper-right at 70% width / 25% height; one lower-right at 70% width / 76% height. Three open approaches, broad spaces for grouping enemies; no cramped tunnel network and no impassable ring around the core.
Sixth small legend panel title "共同约定" with three small illustrated samples matching the maps, labelled exactly "伤口入口", "保护核心", "固定组织障碍", and one short line "开放地面可自由布置". Bottom footer exact "布局草案 v0.1 · 2026-10-15 · 尚未接入关卡".
Do not redesign the existing core, do not introduce buildings, roads, sci-fi circuit boards, veins as roads, stone cliffs, green grass, realistic gore, cinematic lighting, or dense decorations. This is a coherent map design comparison sheet in the exact existing art style.
```

## 第一次局部修正

```text
Edit this existing five-panel map planning sheet with exactly ONE localized correction. In the bottom-middle map panel labelled "05 感染加剧", there are currently only two entrances (upper left and middle right). Add the missing THIRD entrance: a magenta wound-mouth opening through the LEFT border of that same bottom-middle map, at approximately 80% down that panel's height. It must resemble the other existing entry mouths with cream membrane lips and a dark magenta interior, fully and clearly opening into the arena. Its location is around global image x=625 and y=725 on the displayed 1774x887-scale sheet; use the panel identity and relative position as authoritative if actual image size differs.
Preserve EVERYTHING else exactly: the other four map panels, panel 5's existing upper-left and right entrances, all cores, obstacles, coral tissue floor, pixel-textured hand-painted style, panel borders, arrangement, Chinese titles, footer, and legend. Do not add paths, arrows, grids, units or tower sockets. Do not add any other entrances. The finished panel 5 must have exactly two left-edge mouths (upper and lower) and one right-edge mouth.
```

## 最终局部修正

```text
Precise localized edit of Image 1 (the current planning sheet). Image 2 is only a detail reference for the missing entrance. Preserve all of Image 1 except a tiny patch on the UPPER-LEFT EDGE of its bottom-middle map labelled "05 感染加剧".
Current Image 1's panel 05 has a LOWER-left entrance and a right-middle entrance, but is missing its UPPER-left entrance. Restore the UPPER-left entrance that can be seen in Image 2's panel 05. Add a small magenta wound opening at 20% down the LEFT border of panel 05, approximately global x=625, y=530 on the 1774x887 preview. The opening is below the top-left corner, separate from that corner.
CRITICAL: Keep the existing LOWER-left magenta opening around y=725 EXACTLY intact. Keep the RIGHT-middle magenta opening around x=1220,y=623 intact. The result in panel 05 must show THREE distinct magenta mouths: one upper left, one lower left, one middle right. Do not move or substitute one opening for another. Do not close any existing opening. Do not alter any other pixels beyond the restored upper-left mouth and its cream membrane lips. Preserve all 5 maps, Chinese labels, layout, cores, terrain islands, low-contrast floor texture, legend and footer. Exact same art style and overall composition.
```

