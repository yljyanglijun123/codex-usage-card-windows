# Codex Usage Card for Windows

在 Codex Windows 客户端侧栏显示剩余使用额度。**非官方 Windows 移植测试版 · v0.10.1-windows.1 · MIT**。

基于 [Lorie7813/codex-usage-card](https://github.com/Lorie7813/codex-usage-card) v0.9.4 Beta，保留上游版权。本项目与 OpenAI 无官方关联。

## 下载与使用

前往 [Releases](https://github.com/yljyanglijun123/codex-usage-card-windows/releases/tag/v0.10.1-windows.1) 下载 `CodexUsageCard-Windows-0.10.1-preview.zip`。本版为 Preview，包含退出后重启的候选修复；实际托盘退出后的偶发首次点击问题尚待用户验收。

1. Windows 上已安装并登录 **Codex 商店版**，并有 **Node.js 24+**。安装器会尝试发现已有运行时；不会自动安装缺失依赖。
2. 保存工作，从右下角托盘**完全退出 Codex**，等待约 10 秒；升级前也请先完全退出。
3. 完整解压 ZIP，双击 `install.cmd`。
4. 从开始菜单的 **Codex** 入口启动。
5. 将这条正常 Codex 入口固定到任务栏，替换旧固定项。它首次激活商店应用时就携带额度连接参数，显示为同一个应用图标。

默认方案不注册登录启动监听器。主窗口关闭后，额度读取代理自动结束；如果 Codex 仍在托盘，轻量窗口监测进程继续等待，窗口恢复时重新连接额度卡。完全退出 Codex 后，窗口监测进程也会结束。

0.9.7 修复关窗留在托盘后再次点击任务栏可能卡住的问题：由 Codex 原生单实例机制唤醒窗口，不再强制显示隐藏的对话框或输入法辅助窗口；连续点击只执行一次启动交接。默认重装会停用本项目旧版实验启动监听任务。

0.10.0 使用 Windows 原生商店应用激活接口，保持应用身份并在首次激活时传递连接参数。任务栏启动器在启动后退出，独立 CodexUsageCardMonitor-v1.exe 负责额度生命周期。后台监听改为仅被动附加，已移除结束新进程后重开的逻辑。未修改的原商店入口若没有连接参数，会正常打开 Codex，监听器不会为了显示额度而重启它。详见 [首次启动修复](STARTUP-FIX.md)。

## 功能

- 保留上游额度圆环、浅深色主题、悬停提示、重置时间。
- Plus 在数据可用时显示 5 小时和每周额度，其他账号沿用上游规则；不保证所有账号都有相同数据。
- 每次启动重新查找当前 Codex、CLI 和 Node，适应升级导致的路径变化。
- **路径可自动恢复，不代表保证兼容未来的内部接口或侧栏结构。**

## 测试版限制

已在一台 Windows 电脑、Codex 商店版 26.1027.3384.0 和 Node 24 环境验证隔离安装、真实额度显示、正常关窗后代理退出。最新候选通过 8 项安装场景、6 轮受控进程退出后单次重开，以及 4 项窗口恢复回归；受控退出测试不是手工托盘退出的完整复现。早期首启测试存在时间比较错误，已修正，旧通过次数不作本版验收依据。详见 [启动验证记录](STARTUP-FIX.md)。

未在另一台干净 Windows、ARM64 或组织策略环境验证，未完成完整卸载测试。目前没有一键完整卸载工具。仍是 Preview，不承诺所有电脑下载即用。

`install-with-monitor.cmd` 可额外注册随登录运行的被动监听器，只连接已经开启本机调试端口的客户端。`disable-monitor.cmd` 可停用它。额度显示依赖已更新的正常 Codex 入口，不能再声称任何未修改的商店启动入口都能自动显示额度。旧版抢占式监听已停用。

## 本机连接与隐私

使用 `127.0.0.1:39222` 的 CDP 调试接口，其权限较高，回环地址不提供身份认证，请勿转发端口。插件通过本机 CLI 读取额度，不请求聊天内容、不扫描项目、不额外上传遥测。

程序未签名；不要关闭系统安全防护或绕过安全拦截。仓库及安装包不包含账号配置、日志、浏览器数据或凭据。

## 源码与验证

- 源码：`src/Launcher.cs`、`src/Resolver.cs`、`src/Broker.cs`、`agent.cjs`。
- 入口：`install.cmd`、`install.ps1`。`-CheckOnly` 可只做依赖预检，`-InstallRoot` 与 `-NoShortcuts` 可指定独立安装目录。
- 编译启动器需同时包含 Launcher.cs、Resolver.cs、TaskbarIdentity.cs 和 NativeActivation.cs，使用 .NET Framework C# 编译器，`/target:winexe`，引用 System.Management.dll、System.Web.Extensions.dll、System.Windows.Forms.dll。
- [安装验证](INSTALL-VERIFICATION.md)、[路径迁移验证](AUTO-UPDATE-PATHS.md)、[来源与修改](UPSTREAM.md)、[MIT 许可](LICENSE)。

停止使用时完全退出客户端，移除自己创建的快捷方式；实验监听器的停用方法见其单独说明。
