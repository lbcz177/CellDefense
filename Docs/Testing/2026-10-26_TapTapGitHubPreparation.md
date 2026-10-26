# 0.1.5 发布准备与 GitHub 同步验证

日期：2026-10-26。工作区E:/unity/CellDefense，Unity2022.3.53f1c1。用户要求更新TapTap草稿并同步GitHub，默认保留录屏与截图。

## 平台与历史

通过当前CLI契约只读核对：上线版本V-20261019、新草稿V-20261025，草稿主包体0.1.4。资料分析blockers/warnings/suggestions为空。原文案及素材保留快照于Logs/TapTapRelease/2026-10-26。未将此前的审核拒绝状态当作当前状态。

## 代码与回归

- EditMode 140/140通过。
- 首轮PlayMode 43/45：两个已有测试发现默认视口下Score Maximum分成两行。原失败XML、日志和代码副本保留。
- 最小修正：上限区域从64拓宽至88、中心x从25移至30，去掉“/ 10000”的多余空格。未改评分或存档规则。
- 修正后完整PlayMode45/45通过，0失败/跳过；包含五关流程、分数/星级、最高分、重试、下一关与三种桌面/移动视口。未新增与实现重复的测试。
- 隔离工程432份源码/场景/配置/插件/引用文件与主工程快照一致；测试公司CellDefenseValidation、产品ScoreFlowTests。构建前恢复主工程设置，不操作用户Unity或真实成绩。
- 首轮通过的EditMode未在仅排版修正后重复；最终PlayMode编译并验证修正后的全部场景逻辑。

## 隐私与政策

- 应用仍采用原生PrivacyActivity先同意后启动Unity，游戏Activity不导出、无自动启动provider/service/receiver；最终APK审计已通过：唯一原生授权启动入口、游戏Activity不导出、无自动启动组件、权限列表为空。
- 复用真实ConsentStore执行12项JVM检查全部通过，含政策变化再次同意、拒绝、撤回及存储失败边界。
- 本地政策增加最高通关分、星级/通关展示用途及清除应用数据删除成绩说明；不增加联网同步。
- 现有政策网页的手机390px及电脑1280px实际浏览器检查通过，无横向溢出，八节完整、日期和新记录/删除说明存在；实际截图已查看。
- 用户确认后，原政策网页已公开更新，匿名HTTP200、完整正文SHA256与APK内文本一致；Site版本2。
- 真机安装、运行与平台隐私复检仍未执行。

## GitHub

直接连接github.com超时；读取系统已有代理后只读查询和fetch成功，未修改全局代理或Git配置。远程分支是本地祖先，本地此前有两条未推送提交；本次将正常快进推送，不强推。
源码、原生插件、资源、政策与文档进入提交；未发现凭据特征、非空敏感JSON键或超过90MiB的新增文件。源版本提交已完成并推送，远程分支SHA与本地一致。

本轮原始证据：Logs/StoreBuild/2026-10-26、Logs/PolicyPreview20261026、Logs/GitHubSync20261026。


## 最终APK

0.1.5 / Code 6，59,617,474字节（56.86MiB），SHA256 4801149e9f07451b5f0bd6ba863ab7f8442dca438a806ddd4ea431e0a446c2ab。Unity构建成功并正常以0退出，242项ZIP条目CRC通过；双架构IL2CPP/Unity ELF、同签名、非Development验证通过。实际资源内政策与源码/本地网页文本完全一致，原生launcher字节码不引用Unity类。

当前政策源SHA256 fa9eecc29eee6dadfd0876d90230561798668d56f166e1f805b75c2b83adab4e。原政策网页发布、APK上传和草稿保存均已按本次明确确认完成。


## 已验证 GitHub 同步

仓库lbcz177/CellDefense，分支codex/open-field-prototype，源版本提交ccefa587cced455f656f9e985a89f521e2430314。git push返回0，后续ls-remote得到完全相同SHA。未强推，未创建GitHub Release；APK按既有规则不入库。站点四个实际文件以100644普通文件提交，未将独立Sites仓库作为缺少.gitmodules的gitlink提交，嵌套Git历史保留。后续并发出现的章节策划文档未纳入本次已冻结的发布版本。

## 已确认执行与读回（2026-10-26）

用户回复“确认，更新草稿和隐私政策页”。上传APK ID1071004，平台解析0.1.5 / Code6、包名、59,617,474字节及MD5均与本地一致。主槽绑定经新鲜expected防漂移校验后写入，读回指向1071004。18个可见资料字段仅apk_whatsnew发生变化，截图、录屏、宣传图及其他资料保持原值。V-20261025仍draft，V-20261019仍online；未建立审核快照、未预检、未提审、未发布游戏。资料分析三项列表均为空。

网站复用原project_id及public访问范围，推送源提交d541c3bb06fa3fe959c7c41aee52c1140f4bd19d后保存Site版本2并部署成功，公开原文与包内政策SHA256一致。完整原响应保留在Logs/TapTapRelease/2026-10-26/apply-approved和Logs/PrivacySite20261026；可提交的摘要见Docs/Publishing/Review/2026-10-26/execution-verification.json及Docs/Privacy/site-live-verification-2026-10-26.json。
