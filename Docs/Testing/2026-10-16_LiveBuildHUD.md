# 战斗中建塔与 HUD 预制体：测试记录

日期：2026-10-16。引擎：Unity 2022.3.53f1c1。

用户本次明确要求执行测试。为避免占用已打开的项目，将当前 Assets、Packages、ProjectSettings 和包缓存复制到忽略目录 `Temp/CodexLiveBuildTests`，在副本使用 `-batchmode -nographics -runTests` 分别运行 EditMode 和 PlayMode，没有构建 Player。测试完成后核对相关源码、Prefab 和测试与主工作区一致，源文件哈希保存在摘要中。

## 结果

| 测试集 | 通过 / 总数 | 主要覆盖 |
|---|---|---|
| OpenFieldLiveBuildHudTests | 22 / 22 | 阶段限制、ATP / 额度、普通塔与壁垒、敌人脚下保护、在场可达性、改路状态、数字更新与五关 Prefab 引用 |
| OpenFieldWaveFlowTests | 18 / 18 | 开波 / 提前开波、奖励去重、旧敌人、视口映射、默认按钮素材 |
| EnemyCombatTests | 21 / 21 | 既有敌人战斗与有限改路回归 |
| EconomyServiceTests | 5 / 5 | ATP 服务 |
| LifeServiceTests | 1 / 1 | 生命归零不重复结算 |
| OpenFieldLiveBuildSmokeTests（PlayMode） | 1 / 1 | 真实第一关开波后建普通塔和壁垒、敌人改路不瞬移且继续移动、召唤继承新路径、HUD 显示 45 ATP |

共 EditMode 67 项、PlayMode 1 项，全部通过，0 失败、0 跳过。另有 77 个源码的 Roslyn 静态诊断 0 错误，没有 Emit。

## 原始报告

- [EditMode NUnit XML](2026-10-16_LiveBuildHUD_editmode.xml)
- [PlayMode NUnit XML](2026-10-16_LiveBuildHUD_playmode.xml)
- [运行摘要与源文件 SHA256](2026-10-16_LiveBuildHUD_summary.json)

## 验证边界

自动化测试验证代码行为、资源导入及场景引用；没有验证实际画面的清晰度、大小是否符合个人偏好，也没有完成五关全流程通关或测量 Profiler 性能。用户可在 Unity 检查缩小后的 HUD 和战斗中建造交互，再作目视验收。
