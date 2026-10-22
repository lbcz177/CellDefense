# 2026-10-22 TapTap 0.1.3 更新准备与执行

目标厂商338749、游戏986196。执行前为V-20261019未发布草稿、主包体0.1.2；用户确认后已更新为0.1.3 / Code 4，仍为未发布草稿。

## 已完成

- 当前代码回归：EditMode 131/131、PlayMode 41/41，0失败/跳过；在Logs/CodexTradingTests隔离工程运行，未操作主工程Unity。
- 405份玩法源码、场景、配置与资源引用在隔离工程匹配；394份当前主工程关键文件仍与构建快照一致。资产全量4150份文件摘要保存在Logs/StoreBuild/2026-10-22/source-hashes.json。
- Unity构建成功并正常以0退出；包装脚本因默认编码解析中文productName而误报失败。原错误已保留，使用UTF-8解析原JSON后核实成功，未重打包。
- APK：0.1.3 / Code 4，60,499,274字节，非Development；234项ZIP CRC完整，ARMv7/ARM64原生库ELF正确；安装名胞卫战，包名com.celldefense.game。
- v1/v2签名验证通过，证书SHA256与历史原包一致；未在Android设备安装运行。
- 新素材：三张1920×1080原始截图、20秒1080p/30fps H.264连续战斗视频；来源Unity Editor PlayMode手机布局，正常ATP，无额外字幕、音轨或合成。原录制900帧保留，导出前600帧用于展示。
- 图标与宣传图沿用；简介只修正壁垒旧改路描述，更新日志列出现有实现，不包含规划新塔。

## 依据与限制

current_fact：素材规格取本次assets-upload动态字段；首页推荐现值GAMEPLAY_DEMO，保留用户选择。版本历史只有draft，无拒审记录。

test_evidence：包体校验、签名、源文件比较及测试XML见Logs/StoreBuild/2026-10-22；录屏与原图证据见Logs/StoreCapture/2026-10-22。

official_rule：CLI附带v4审核规范2.5、2.6要求真实截图和核心玩法演示，来源[TapTap游戏审核规范细则](https://developer.taptap.cn/docs/store/release/publish/agree/)；精确尺寸与时长按实时字段判断。

agent_assessment：已查看三张截图和录制首、中、末帧，选择前20秒连续战斗供用户人工复核。语义与实机验收不等于自动校验或平台审核通过。

[检查页](../Publishing/Review/2026-10-22/index.html) · [变更清单](../Publishing/Review/2026-10-22/change-plan.json)。尚未执行本次平台写入、审核快照、审核预检、提审或发布。

最终准备检查：5项文件上传预演、2项文案变更预演均通过；检查页全部13个本地素材/文件引用有效。CLI本地准备核对通过，实际平台上传与保存仍等待用户对本次内容确认。


## 用户确认后的实际执行

用户明确确认“确认，按检查页更新草稿”，随后指定后续默认不更新录屏/截图，本次按原范围执行。

- 写前重读七个模块、版本与包体槽位；五个文件SHA256均与已确认检查页一致。
- 三张图片、20秒视频和APK通过官方CLI端到端上传，复用预演幂等键并逐项留存结果。
- 图片完整info与视频真实videoId用于回填，截图组与视频引用读回一致。
- 当前查询中0.1.3 APK为ready，main槽位可用；使用原样expected绑定并读回，平台APK版本、包名、大小、MD5与本地一致。
- 简介、日志两项使用原预演payload/key与最新expected保存，读回一致。
- 七模块仅发生已确认的四项字段变化；原宣传图、图标、首页实机视频选项及其他已读资料保持原值。资料体检blockers/warnings/suggestions均为空；版本仍draft。
- 包体库配置为warning，未给出原因；需在后台配置页人工核对，不能从该字段推断具体场景。
- 未准备审核快照、未审核预检、未提审、未改变上线安排、未发布；真机仍待用户验收。

[写后状态](../Publishing/Review/2026-10-22/after-platform.json)、[交接](../Publishing/Review/2026-10-22/HANDOFF.md)；原始证据在Logs/TapTapRelease/2026-10-22/apply-approved。
