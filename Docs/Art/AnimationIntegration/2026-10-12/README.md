# 原关卡动画与打击感接入

当前交付已改为直接修改原有游戏关卡，详见 [gameplay/README.md](gameplay/README.md)。运行 OpenFieldLevel02 或从主菜单进入原有关卡即可体验中性粒细胞塔与葡萄球菌群的强化动画、攻击释放事件、命中闪白、像素粒子和音效。

原有场景、Prefab 和它们的 meta 保留。新图集位于 Assets/_Project/Art/Sprites/细胞绘制/帧动画/关卡动画_v1。实际第二关录像位于该帧动画目录的关卡打击感预览_v1/第二关_实际战斗.gif。

最新的实际关卡运行验证结果位于 gameplay/level-validation.json。旧版 asset-validation.json、runtime-validation.json 及运行样例_v1 录像仅记录之前的独立样例验证；独立样例场景和相关脚本已移到 gameplay/retired-demo，不再留在 Unity Assets 中。其原说明存入 retired-demo/original-sample-README.md。

改动前文件保留在 backup 和 gameplay/before-gameplay-update 中。编辑器资源安装入口为 Cell Defense → Apply Combat Feedback to Existing Levels，只更新现有游戏关卡。
