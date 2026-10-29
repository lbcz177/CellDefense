# 《胞卫战》文档导航

> 2026-10-28：第一章五关改为逐关通关解锁，无星级门槛，成绩持久保留；第二、三章仍仅预览。完整逻辑 141/141、运行 53/53 通过，见[顺序通关规则](SEQUENTIAL_UNLOCK.md)。仅本地源码，未重新打包。

> 2026-10-28：三章选择与序号详情界面已接入；第一章五关正常游玩，第二、三章共八图仅预览，保留原最高分与星级。见[章节流程](CHAPTER_SELECTION.md)。本轮未重新打包或上传。

> 2026-10-26发布准备：当前候选0.1.5 / Code 6（56.86MiB），含五关评分、星级、本地最高分及结算修正；140项EditMode、修正后45项PlayMode和12项授权逻辑检查通过，实际APK隐私审计通过。TapTap目标为新草稿V-20261025，仍绑定0.1.4；截图录屏保留，更新待确认。


> 2026-10-25：五关按击杀与核心生命比例评分，满分 10000，7000 二星 / 9000 三星；结算及选关显示成绩，最高通关分本地保存，重试不降分。五关继续自由选择。完整 EditMode **140/140**、PlayMode **45/45** 通过。本次仅本地源码，未重打 APK 或修改 TapTap。见[评分规则与流程](LEVEL_SCORING.md)及[验证报告](Testing/2026-10-25_LevelScoring.md)。

> 2026-10-22：Android 接入引擎启动前的原生隐私授权、离线政策及撤回入口，关闭自动统计并移除联网权限。0.1.4 / Code 5 正式候选已构建并通过包体检查；已补齐运营者 ichen、QQ 2558057120 并发布[独立政策网页](https://cell-defense-privacy.jazzy-flint-2968.chatgpt.site/)。用户已确认，TapTap 主包已切换为 0.1.4，独立链接已保存到隐私资质草稿；现有素材及更新日志保留，未提审或上线。真机及平台隐私复检待完成，见[上传记录](Publishing/0.1.4_UPLOAD.md)。见[整改与验证说明](Privacy/README.md)。

> 2026-10-21：第一关接入可跳过的三轮新手引导，包含末轮 Boss 增援提示；其他四关不启用。见 [操作与源码](FIRST_LEVEL_TUTORIAL.md)。

> 2026-10-20 壁垒最终回归：EditMode **128/128**、PlayMode **33/33**，0 失败 / 跳过；八敌共同攻墙、自然破裂 / 出售放行、暂停保持和超过六次建造均通过。在隔离副本验证，未重打 APK。见 [壁垒验证](Testing/2026-10-20_FibrinBarrier.md)。

> 2026-10-20 本地源码更新：壁垒改为可破坏的原路拦截物，500 生命、30 ATP，拦截全部经过敌人；取消累计建造额度和改路拒建。旧改路代码保留但不启用、界面不展示。本次不重打 APK 或修改 TapTap；既有 0.1.2 包和远程草稿不包含此变更。见 [壁垒说明](FIBRIN_BARRIER.md)。

> 当前本地上架候选为 Android 0.1.2 / Code 3，57.01 MiB，安装名“胞卫战”、游戏图标、非 Development；已有30秒运行视频、3张截图和宣传图。TapTap草稿已按用户确认完成上传、绑定与填写，资料体检无缺失；待用户检查，未提审或发布。见 [检查页](Publishing/Review/2026-10-20/index.html)和[证据](Testing/2026-10-20_StorePreparation.md)。

> Android 0.1.1 修复包已生成：修复手机地图外的旧 UI 残影，约 69.81 MiB，支持覆盖 0.1.0。该阶段 EditMode 127/127、PlayMode 32/32；签名与完整性校验通过，等待用户在原手机上复测。见 [修复说明](MOBILE_UI_RESIDUE_FIX.md)和[验证记录](Testing/2026-10-19_MobileUiResidue.md)。

> 整理日期：2026-10-19；Unity 2022.3.53f1c1。
> 当前范围：五关各三轮、承伤壁垒全部拦截、普通细胞有限接敌、六塔、混合敌群与 ATP 购售。

先读 [当前实现与验证状态](PROJECT_STATUS.md)，再读 [策划 v0.32](GAME_DESIGN_FRAMEWORK.md) 和 [技术架构](TECHNICAL_ARCHITECTURE.md)。当前主线十三关，第二、三章运行规则以 [第二章](CHAPTER_TWO.md)及[第三章](CHAPTER_THREE.md)为准；组织重塑与协同仍是[待实施的重排方案](LevelPlanning/CHAPTER_THREE_FLOW_2026-10-29.md)。本轮[候选库 v0.1](LevelPlanning/UNIT_CANDIDATE_POOL_2026-10-29.md)收录五种防御方向与六种怪物，制作时间、解锁关卡、数值均待定，不改当前关卡或流程方案。旧日期交接不代表当前进度。新增[首次介绍卡与图鉴收录规划](LevelPlanning/UNLOCK_CARDS_AND_CODEX_2026-10-29.md)，在现有图鉴基础上区分人体作用与游戏功能；弹卡、收录状态及相关暂停流程尚未实现。

| 目的 | 文档 |
|---|---|
| 首次解锁介绍、人体作用 / 游戏功能与图鉴收录（规划） | [介绍卡与图鉴设计](LevelPlanning/UNLOCK_CARDS_AND_CODEX_2026-10-29.md) |
| 防御机制与怪物种类候选，加入时间待定 | [候选库 v0.1](LevelPlanning/UNIT_CANDIDATE_POOL_2026-10-29.md) |
| 第三章四关、局部炎症、辅助T与Treg | [第三章首版可玩](CHAPTER_THREE.md) |
| 第二章四关、修补／补体、精英、波次与跨章解锁 | [第二章首版可玩](CHAPTER_TWO.md) |
| 胜利 / 失败面板过大、超出屏幕 | [屏幕适配](RESULT_UI_LAYOUT.md) |
| Android 横屏、触控 / 安全区 / 后台暂停、APK 与学习路径 | [手机适配说明](MOBILE_ANDROID.md) |
| 战斗地图 / 细胞 / 射程圈拉伸修复 | [等比显示说明](VIEWPORT_ASPECT.md) |
| 最新测试 | [关卡评分 140/45](Testing/2026-10-25_LevelScoring.md) |
| 手机地图外残留上一页 UI | [清屏修复说明](MOBILE_UI_RESIDUE_FIX.md) |
| TapTap 发布协作 / 先确认再修改平台内容 | [发布流程](Publishing/TAPTAP_WORKFLOW.md)、[0.1.2 检查页](Publishing/Review/2026-10-20/index.html) |
| Android APK 输出 / 重打包 / 安装试玩 | [打包说明](ANDROID_APK_BUILD.md)、[首次构建证据](Testing/2026-10-19_AndroidApk.md) |
| 当前状态、五关参数、已有测试和未覆盖项 | [PROJECT_STATUS](PROJECT_STATUS.md) |
| 当前规则与候选范围 | [策划](GAME_DESIGN_FRAMEWORK.md) |
| 后续章节、塔解锁、逐波敌人、地图与炎症 | [章节讨论 v0.4](LevelPlanning/CHAPTER_FLOW_DISCUSSION_2026-10-26.md) |
| 第三章四关逐波、地图、塔配合及第四章衔接 | [最新流程稿](LevelPlanning/CHAPTER_THREE_FLOW_2026-10-29.md) |
| 新 Boss 真实原型、局部溶纤、阶段与塔配合（未实现） | [化脓性链球菌·溶纤先导](LevelPlanning/BOSS_STREPTOCOCCUS_DISCUSSION_2026-10-29.md) |
| 真实职责、路径、生命周期和技术债 | [架构](TECHNICAL_ARCHITECTURE.md) |
| 启动、建造、开波、出售与手动验收 | [试玩说明](OPEN_FIELD_THREE_ROUND_PROTOTYPE.md) |
| 暂停 / 继续、1x / 2x 与 v2 图片界面 | [代码与操作](PAUSE_PLAYBACK.md) |
| 音量 / 静音 / 桌面全屏 / 默认值、本地保存、声音拉条和字图 | [设置说明](SETTINGS.md)、[字图接入测试](Testing/2026-10-19_SettingsText.md) |
| 五关地图、选关、坐标和共享波次 | [地图接入](LevelPlanning/FIRST_FIVE_MAPS_IMPLEMENTATION_2026-10-15.md) |
| 当前承伤壁垒 / 旧改路保留范围 | [壁垒说明](FIBRIN_BARRIER.md) |
| 战中建造的历史改路实现 | [历史实现](LIVE_BUILD_IMPLEMENTATION.md) |
| 购买 / 确认出售与退款 | [购售实现](OPEN_FIELD_TRADING.md) |
| 主菜单退出与四种伤害塔单次升级 | [操作与规则](QUIT_AND_UPGRADE.md)、[验证](Testing/2026-10-19_QuitUpgrade.md) |
| 第一波手动、出兵后自动倒计时 / 提前开波 / 等比显示 | [自动开波](AUTO_WAVES.md)、[入口说明](ENTRANCE_WAVE_BUTTONS.md) |
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
