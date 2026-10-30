# 来源与修改

- 原仓库：https://github.com/Lorie7813/codex-usage-card
- 发布：https://github.com/Lorie7813/codex-usage-card/releases/tag/v0.9.4-beta.1
- 输入文件：CodexUsageCard-macOS-0.9.4.zip
- 已核对 SHA256：e41cbb00fd01b785eb0cac73a946115a35dd2d9a2410c196ec7287ce4b1737f8
- 原版权与 MIT 条款：见 LICENSE，完整保留。

保留原包中的 agent.cjs 和启动保护控制器。agent.cjs 修改包括：限制 CDP WebSocket 必须匹配配置端口、导出 RendererInjector 供卸载使用、支持父进程 IPC 正常关闭、修复主题切换时提示文字的颜色继承。额度读取、账号规则、合并与过期处理和圆环主体沿用上游实现。

新增 Windows 平台适配、管理器、CMD 入口、Node 发现脚本及测试。没有打包 macOS 的安装器、原生二进制、更新器或其他无关兼容模块。本版本不自动更新。

此交付物是第三方 Windows 移植测试版，未向原作者仓库提交或发布。
