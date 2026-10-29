# 0.1.6 隐私链接反馈处理准备

2026-10-29。App986196，V-20261028现为审核失败；版本审核原文与用户提供的一致：“隐私政策链接请勿为TapTap站内页面，请更换为独立网页链接。”

拟保存原有独立网页 https://cell-defense-privacy.jazzy-flint-2968.chatgpt.site/ ，并明确设置“不使用通用隐私政策”。主页和纯文本匿名HTTP200、不跳转TapTap；纯文本与当前包内原文逐字节一致。无需重新上传APK或发布政策网站。

当前CLI隐私资质只返回uploaded与空submitted_fields，没有返回实际链接或通用政策开关，无法据此断言当前后台具体填了哪个地址，也不能认定拒审必然由通用政策开关导致。前次保存记录不能替代本次实际字段核对。

用户回复“确认，修正隐私链接”后，已用相同预览内容与幂等键执行保存。接口返回completed、privacy-compliance；读回隐私资质为uploaded（已存草稿）。仅保存隐私资质草稿，没有重新提交审核、公开上线、改包体或素材。原状态与审核反馈保留于Logs/TapTapRelease/2026-10-29/privacy-link-feedback。

19项商店可见字段、主包槽位和版本状态均与保存前一致。当前V-20261028仍为rejected（原拒审状态）；修改草稿不代表审核通过。接口仍未返回链接和通用政策开关，不能把保存回执及uploaded状态表述为字段值已读回。

[执行结果](privacy-link-fix-result.json)。修改前准备材料保留于Docs/History/2026-10-29_before_privacy_link_apply。
