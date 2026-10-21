# 项目内 TapTap CLI

已安装官方包 @taptap/cli 2026.10.19；Node 24.21.0。依赖在 node_modules 内，不提交；package.json 和 package-lock.json 保留版本。未修改全局 npm registry / prefix，未执行 CLI 自更新或全局 Skill 安装。

入口：./node_modules/.bin/taptap-cli.cmd。只读本地检查：./Check-LocalReady.ps1。

重装可在项目根目录执行 npm ci --prefix Tools/TapTapCLI --ignore-scripts --no-audit --no-fund --registry=https://registry.npmjs.org/；请沿用项目 Logs/TapTapCLI/npm-cache 缓存以减少下载。此次下载曾遇到官方 CDN 停滞，镜像包经过官方 SHA512 完整性值核验后才进入缓存和安装。

流程与用户确认约定见 ../../Docs/Publishing/TAPTAP_WORKFLOW.md。用户计划明天登录，本轮不发起认证或平台写入。不要运行无人值守的资料修改、上传、审核或发布脚本。
