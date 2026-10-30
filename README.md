# Codex Usage Card for Windows

在 Codex Windows 客户端侧栏显示剩余使用额度。**非官方 Windows 移植测试版 · v0.9.6-windows.1 · MIT**。

基于 [Lorie7813/codex-usage-card](https://github.com/Lorie7813/codex-usage-card) v0.9.4 Beta，保留上游版权。本项目与 OpenAI 无官方关联。

## 下载与使用

前往 [Releases](https://github.com/yljyanglijun123/codex-usage-card-windows/releases) 下载 Windows 安装 ZIP，而非 GitHub 自动生成的 Source code 归档。

1. Windows 上已安装并登录 **Codex 商店版**，并有 **Node.js 24+**。安装器会尝试发现已有运行时；不会自动安装缺失依赖。
2. 完整解压 ZIP，双击 `install.cmd`。
3. 保存工作，从客户端菜单完全退出 Codex，再用生成的 **Codex Usage Card** 快捷方式启动。
4. 可将这个新快捷方式固定到任务栏作为日常入口。

默认方案不需要常驻启动监听器。主窗口关闭后，额度代理自动结束；Codex 自身是否后台驻留由客户端决定。

## 功能

- 保留上游额度圆环、浅深色主题、悬停提示、重置时间。
- Plus 在数据可用时显示 5 小时和每周额度，其他账号沿用上游规则；不保证所有账号都有相同数据。
- 每次启动重新查找当前 Codex、CLI 和 Node，适应升级导致的路径变化。
- **路径可自动恢复，不代表保证兼容未来的内部接口或侧栏结构。**

## 测试版限制

已在一台 Windows 电脑、Codex 商店版 26.1027.3384.0 和 Node 24 环境验证隔离安装、真实额度显示、正常关窗后代理退出。另有 8 项安装场景和 9 项路径解析测试。

未在另一台干净 Windows、ARM64 或组织策略环境验证，未完成完整卸载测试。目前没有一键完整卸载工具。仍是 Preview，不承诺所有电脑下载即用。

原商店图标自动接管是**非默认实验功能**，用户日常任务栏验收尚未通过。它需要常驻监听器，可能在启动初期重开客户端。首次使用请采用新快捷方式。详情见 [ORIGINAL-ICON.md](ORIGINAL-ICON.md)。

## 本机连接与隐私

使用 `127.0.0.1:39222` 的 CDP 调试接口，其权限较高，回环地址不提供身份认证，请勿转发端口。插件通过本机 CLI 读取额度，不请求聊天内容、不扫描项目、不额外上传遥测。

程序未签名；不要关闭系统安全防护或绕过安全拦截。仓库及安装包不包含账号配置、日志、浏览器数据或凭据。

## 源码与验证

- 源码：`src/Launcher.cs`、`src/Resolver.cs`、`src/Broker.cs`、`agent.cjs`。
- 入口：`install.cmd`、`install.ps1`。`-CheckOnly` 可只做依赖预检，`-InstallRoot` 与 `-NoShortcuts` 可指定独立安装目录。
- 编译启动器需同时包含 Launcher.cs 和 Resolver.cs，使用 .NET Framework C# 编译器，`/target:winexe`，引用 System.Management.dll、System.Web.Extensions.dll、System.Windows.Forms.dll。
- [安装验证](INSTALL-VERIFICATION.md)、[路径迁移验证](AUTO-UPDATE-PATHS.md)、[来源与修改](UPSTREAM.md)、[MIT 许可](LICENSE)。

停止使用时完全退出客户端，移除自己创建的快捷方式；实验监听器的停用方法见其单独说明。
