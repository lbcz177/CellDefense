# 胞卫战：开放地图核心防御

Unity 2D 免疫主题塔防学习项目，使用 Unity 2022.3.53f1c1，当前优先 Windows PC。当前策划 v0.16（2026-10-18）：五关各三轮，有限壁垒改路、普通细胞自由布置 / 承伤、六塔协作、ATP 购售与暂停 / 倍速。

## 当前实现

- 主菜单五关选择，顺序挑战 OpenFieldPrototype → OpenFieldLevel02 → OpenFieldLevel03 → OpenFieldLevel04 → OpenFieldLevel05。
- 15×11 格，核心与入口因关卡不同；仅纤维蛋白壁垒阻挡路线，每关累计 6 座，每座 30 ATP。
- Planning / Assault 均可建造和出售；壁垒动态更新入口 / 在场路径，入口和在场敌人始终可达。
- 普通细胞 100 生命 / 容量 1，巨噬 250 / 2；超额敌人通行。近卫采样 → 抗体标记 → 增伤 / 巨噬吞噬，一跳共享。
- 壁垒减速、酸区破壳、神经连锁 / 停顿，八种单位含三名 Boss 和有限召唤。
- UGUI HUD、选塔、属性 / 二次确认出售、入口开波和胜负。实付退款 50% 向下取整，壁垒额度不返还。
- 本波出怪结束后可带残敌提前开下一波，轮间 ATP 一次发放；末波清场且核心存活胜利，归零失败优先。
- 暂停 / 继续与 1x / 2x；暂停时禁止建造 / 出售 / 开波，HUD 保留，继续恢复原倍率。当前使用临时文字按钮，用户稍后提供美术，见 [代码与操作](Docs/PAUSE_PLAYBACK.md)。

## 运行与阅读

用对应 Unity 版本打开工程，进入 Assets/_Project/Scenes/MainMenu.unity 后 Play，点击开始游戏并选择关卡；也可直接打开开放场景。点击空位再选塔，悬停预览；点击已有塔查看 / 出售；入口按钮或 Space 开波，右键 / Esc 取消选择。

完整操作见 [试玩说明](Docs/OPEN_FIELD_THREE_ROUND_PROTOTYPE.md)。先看 [当前状态](Docs/PROJECT_STATUS.md)、[策划](Docs/GAME_DESIGN_FRAMEWORK.md)、[架构](Docs/TECHNICAL_ARCHITECTURE.md)和[文档导航](Docs/README.md)。旧 Prototype / Level02 继续保留固定路线参考。

## 验证边界与协作

最新 2026-10-18 独立副本自动测试 EditMode 103/103、PlayMode 18/18，通过报告与六个源码 / 测试哈希见 [测试记录](Docs/Testing/2026-10-18_Playback.md)。完整五关通关、最终美术 / 平衡和当前版本 Player 发布仍待验证；未构建 Player 或执行 Profiler，没有性能收益数据。

项目服务 Unity 初级 / 客户端求职。AI 协助普通业务与文档，开发者理解调用链、独立修改核心规则、调试与验证。约定的手写练习先说明实际变量与生命周期。不把 AI 产出当作个人已掌握，也不把候选系统或性能目标写成成果。

旧文档原文见 [历史快照](Docs/History/2026-10-18_before_sync/README.md)。图片交付到 Assets/_Project/Art/Sprites/细胞绘制/，按用途分目录，保留已有素材及 .meta。
