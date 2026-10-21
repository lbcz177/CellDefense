# 胞卫战：开放地图核心防御

> 当前本地上架候选：Android 0.1.2 / Code 3，57.01 MiB，安装名“胞卫战”、游戏图标和非Development构建。宣传图、3张截图与30秒运行演示已备好；TapTap草稿已保存并读回验证，待用户检查，未提审或发布。见 [检查页](Docs/Publishing/Review/2026-10-20/index.html)。

Unity 2D 免疫主题塔防学习项目，使用 Unity 2022.3.53f1c1，桌面与 Android 横屏共用玩法。当前策划 v0.21（2026-10-19）：五关各三轮、有限改路 / 拦截、六塔协作、购售 / 单次升级、暂停 / 倍速、共享设置与主菜单退出。

## 当前实现

- 主菜单五关选择，顺序挑战 OpenFieldPrototype → OpenFieldLevel02 → OpenFieldLevel03 → OpenFieldLevel04 → OpenFieldLevel05。
- 15×11 格，核心与入口因关卡不同；仅纤维蛋白壁垒阻挡路线，每关累计 6 座，每座 30 ATP。
- Planning / Assault 均可建造和出售；壁垒动态更新入口 / 在场路径，入口和在场敌人始终可达。
- 普通细胞 100 生命 / 容量 1，巨噬 250 / 2；超额敌人通行。近卫采样 → 抗体标记 → 增伤 / 巨噬吞噬，一跳共享。
- 壁垒减速、酸区破壳、神经连锁 / 停顿，八种单位含三名 Boss 和有限召唤。
- UGUI HUD、选塔、属性 / 二次确认出售、入口开波和胜负。实付退款 50% 向下取整，壁垒额度不返还。
- 本波出怪结束后可带残敌提前开下一波，轮间 ATP 一次发放；末波清场且核心存活胜利，归零失败优先。
- 暂停 / 继续与 1x / 2x；暂停时禁止建造 / 出售 / 开波，HUD 保留，继续恢复原倍率。已接入用户 v2 暂停面板、独立标题与按钮换图；设置提供总音量、静音、桌面全屏 / 窗口与恢复默认，自动保存。声音 / 静音图标与拉条已接入，可点击喇叭切换静音。见 [暂停操作](Docs/PAUSE_PLAYBACK.md)、[设置](Docs/SETTINGS.md)。

Android 横屏适配代码已接入；可用 Unity 菜单“胞卫战 → Android → 手机布局预览（运行前切换）”在电脑中检查布局。Android 构建组件已安装，当前提供 0.1.1 修复包；用户已反馈旧包手机残影，修复包待复测。见 [适配与源码学习](Docs/MOBILE_ANDROID.md)。

## 运行与阅读

用对应 Unity 版本打开工程，进入 Assets/_Project/Scenes/MainMenu.unity 后 Play，点击开始游戏并选择关卡；也可直接打开开放场景。点击空位再选塔，悬停预览；点击已有塔查看 / 出售；入口按钮或 Space 开波，右键 / Esc 取消选择。

完整操作见 [试玩说明](Docs/OPEN_FIELD_THREE_ROUND_PROTOTYPE.md)。先看 [当前状态](Docs/PROJECT_STATUS.md)、[策划](Docs/GAME_DESIGN_FRAMEWORK.md)、[架构](Docs/TECHNICAL_ARCHITECTURE.md)和[文档导航](Docs/README.md)。旧 Prototype / Level02 继续保留固定路线参考。

## 验证边界与协作

主菜单设置与退出均已接通；五关近卫 / 巨噬 / 酸化 / 神经脉冲可升一次，按实付造价扣费，伤害 +50%，升级费计入出售退款。抗体 / 壁垒暂不升级。见 [操作说明](Docs/QUIT_AND_UPGRADE.md)。退出与升级阶段 EditMode 117/117、PlayMode 26/26；仅主菜单的 Windows 验证程序已实测退出，见 [报告](Docs/Testing/2026-10-19_QuitUpgrade.md)。全屏切换、五关完整通关、窗口比例 / 平衡及完整五关 Player 发布仍待验证，未执行 Profiler。

项目服务 Unity 初级 / 客户端求职。AI 协助普通业务与文档，开发者理解调用链、独立修改核心规则、调试与验证。约定的手写练习先说明实际变量与生命周期。不把 AI 产出当作个人已掌握，也不把候选系统或性能目标写成成果。

旧文档原文见 [历史快照](Docs/History/2026-10-18_before_sync/README.md)。图片交付到 Assets/_Project/Art/Sprites/细胞绘制/，按用途分目录，保留已有素材及 .meta。

手机接入阶段验证：EditMode 126/126、PlayMode 30/30；另独立横屏 1/1。见 [报告](Docs/Testing/2026-10-19_MobileAndroid.md)。

已修复桌面固定 15:11 导致的细胞 / 射程拉伸，按实际视口等比完整显示。该阶段 EditMode 127/127、PlayMode 31/31，见 [报告](Docs/Testing/2026-10-19_ViewportAspect.md)。

Android 0.1.1 试玩修复包已生成：Builds/Android/CellDefense-0.1.1.apk（CellDefense.apk 同步更新），约 70 MB；修复地图外旧 UI 残影，支持覆盖安装。最新 EditMode 127/127、PlayMode 32/32，签名 / 完整性验证通过；待用户手机复测。见 [清屏修复](Docs/MOBILE_UI_RESIDUE_FIX.md)和[打包说明](Docs/ANDROID_APK_BUILD.md)。
