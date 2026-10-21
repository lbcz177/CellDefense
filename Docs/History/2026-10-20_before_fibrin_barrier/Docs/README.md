# 《胞卫战》文档导航

> 当前本地上架候选为 Android 0.1.2 / Code 3，57.01 MiB，安装名“胞卫战”、游戏图标、非 Development；已有30秒运行视频、3张截图和宣传图。TapTap草稿已按用户确认完成上传、绑定与填写，资料体检无缺失；待用户检查，未提审或发布。见 [检查页](Publishing/Review/2026-10-20/index.html)和[证据](Testing/2026-10-20_StorePreparation.md)。

> Android 0.1.1 修复包已生成：修复手机地图外的旧 UI 残影，约 69.81 MiB，支持覆盖 0.1.0。最新 EditMode 127/127、PlayMode 32/32；签名与完整性校验通过，等待用户在原手机上复测。见 [修复说明](MOBILE_UI_RESIDUE_FIX.md)和[验证记录](Testing/2026-10-19_MobileUiResidue.md)。

> 整理日期：2026-10-19；Unity 2022.3.53f1c1。
> 当前范围：五关各三轮、有限壁垒改路、细胞承伤 / 有限拦截、六塔、混合敌群与 ATP 购售。

先读 [当前实现与验证状态](PROJECT_STATUS.md)，再读 [策划 v0.21](GAME_DESIGN_FRAMEWORK.md) 和 [技术架构 v0.15](TECHNICAL_ARCHITECTURE.md)。五关核心 / 入口、数量和共享配置以状态页、场景和引用资产核对；旧日期交接不代表当前进度。

| 目的 | 文档 |
|---|---|
| Android 横屏、触控 / 安全区 / 后台暂停、APK 与学习路径 | [手机适配说明](MOBILE_ANDROID.md) |
| 战斗地图 / 细胞 / 射程圈拉伸修复 | [等比显示说明](VIEWPORT_ASPECT.md) |
| 最新测试 | [手机残影修复 127/32](Testing/2026-10-19_MobileUiResidue.md) |
| 手机地图外残留上一页 UI | [清屏修复说明](MOBILE_UI_RESIDUE_FIX.md) |
| TapTap 发布协作 / 先确认再修改平台内容 | [发布流程](Publishing/TAPTAP_WORKFLOW.md)、[0.1.2 检查页](Publishing/Review/2026-10-20/index.html) |
| Android APK 输出 / 重打包 / 安装试玩 | [打包说明](ANDROID_APK_BUILD.md)、[首次构建证据](Testing/2026-10-19_AndroidApk.md) |
| 当前状态、五关参数、已有测试和未覆盖项 | [PROJECT_STATUS](PROJECT_STATUS.md) |
| 当前规则与候选范围 | [策划](GAME_DESIGN_FRAMEWORK.md) |
| 真实职责、路径、生命周期和技术债 | [架构](TECHNICAL_ARCHITECTURE.md) |
| 启动、建造、开波、出售与手动验收 | [试玩说明](OPEN_FIELD_THREE_ROUND_PROTOTYPE.md) |
| 暂停 / 继续、1x / 2x 与 v2 图片界面 | [代码与操作](PAUSE_PLAYBACK.md) |
| 音量 / 静音 / 桌面全屏 / 默认值、本地保存、声音拉条和字图 | [设置说明](SETTINGS.md)、[字图接入测试](Testing/2026-10-19_SettingsText.md) |
| 五关地图、选关、坐标和共享波次 | [地图接入](LevelPlanning/FIRST_FIVE_MAPS_IMPLEMENTATION_2026-10-15.md) |
| 战中建造 / 动态改路 | [实现](LIVE_BUILD_IMPLEMENTATION.md) |
| 购买 / 确认出售与退款 | [购售实现](OPEN_FIELD_TRADING.md) |
| 主菜单退出与四种伤害塔单次升级 | [操作与规则](QUIT_AND_UPGRADE.md)、[验证](Testing/2026-10-19_QuitUpgrade.md) |
| 入口开波 / 提前开波 / 等比显示 | [开波说明](ENTRANCE_WAVE_BUTTONS.md) |
| HUD 素材接入 | [图片 HUD](BATTLE_HUD_ART_IMPLEMENTATION.md) |
| 六塔基础能力 / 反馈 | [塔能力](TOWER_ABILITIES_STATUS.md) |
| 壁垒 / 承伤 / 容量的实现过程 | [有限改路记录](LIMITED_ROUTE_DEFENSE_IMPLEMENTATION.md) |
| 敌人规则及实现过程 | [设计](ENEMY_DESIGN.md)、[实现](ENEMY_IMPLEMENTATION.md) |
| 策划配置工具与源码学习 | [工具](PLANNING_CONFIG_TOOL.md)、[课程](PlanningConfigTool/tutorial.md) |
| 塔攻击源码与旧接口演进 | [T01](tutorial.md)、[减速塔](SLOW_TOWER_SETUP.md)、[酸化 / 神经](REMAINING_TOWERS_SETUP.md) |
| 性能当前措施与候选测量目标 | [性能](PERFORMANCE_MANY_ENEMIES.md) |
| 求职学习方法 | [学习路线](UNITY_JOB_LEARNING_ROADMAP.md) |
| 热更新学习大纲，尚未实现 | [课程大纲](HotUpdate/tutorial.md) |
| 美术和素材提示词参考 | [美术规范](ART_DIRECTION.md)、[塔](Prompts/TOWERS_IMAGE_PROMPTS.md)、[敌人](Prompts/ENEMIES_IMAGE_PROMPTS.md)、[地图](Prompts/MAPS_IMAGE_PROMPTS.md)、[特效](Prompts/PROJECTILES_VFX_IMAGE_PROMPTS.md) |
| 手机接入阶段测试 | [手机适配 126/30](Testing/2026-10-19_MobileAndroid.md)，另横屏 1/1 |
| 历史阶段测试 | [退出与升级 117/26](Testing/2026-10-19_QuitUpgrade.md)；此前 [主菜单设置](Testing/2026-10-19_MainMenuSettings.md)、[字图](Testing/2026-10-19_SettingsText.md)和更早报告保留 |
| 本次文档整理与 Notion 同步 | [同步记录](NOTION_SYNC_2026-10-18.md) |

## 文档和历史规则

当前实现、目标设计和验证证据分别记录；看到“尚未运行”必须结合日期理解。退出与升级阶段 EditMode 117/117、PlayMode 26/26，升级前后画面已查看；Windows 主菜单验证程序已构建并实测退出，不代表完整五关发布。系统全屏、五关通关、全窗口比例 / 平衡及 Profiler 收益仍待验证。

历史原文完整保存在 [整理前快照](History/2026-10-18_before_sync/README.md)，[10月13日交接](CHAT_HANDOFF_2026-10-13.md)保留为历史。旧三关地图 / 入口、全塔挡路、仅布置期建造、无出售、临时 IMGUI 等描述均已被当前正文取代。专项历史和课程开头注明适用范围，原有代码与知识材料保留。

素材保存在 Assets/_Project/Art/Sprites/细胞绘制/，保留原图和 .meta；课程和美术早期参考不自动增加当前功能范围。个人答题、评分和推测掌握等级不写入 Notion。

手机适配阶段回归：EditMode **126/126**、PlayMode **30/30**，0 失败 / 跳过；另独立横屏验证 1/1。覆盖三种实际横屏尺寸和五关触控 / 结算。见 [验证报告](Testing/2026-10-19_MobileAndroid.md)。Android 首个 APK 已生成，尚未真机验收。

投影修复阶段回归：EditMode **127/127**、PlayMode **31/31**，0 失败 / 跳过。世界图形按实际视口等比显示，覆盖圆形、165 格点选和暂停时窗口变化。见 [验证报告](Testing/2026-10-19_ViewportAspect.md)和[修复说明](VIEWPORT_ASPECT.md)。Android 组件已安装且首个 APK 已生成，尚未真机验收。
