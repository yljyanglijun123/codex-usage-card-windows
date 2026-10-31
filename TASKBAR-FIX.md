# 任务栏双图标修复：0.9.9-windows.1

2026-10-31，本机 Codex 商店版 26.1027.3384.0。

## 原因与实现

后台宿主用 EXE 启动客户端后，实测进程没有应用 AUMID，主窗口的 System.AppUserModel.ID 也为空，无法按商店应用的身份与原来的固定图标分组。仅把快捷方式改为 explorer.exe shell:AppsFolder 启动不能修复运行窗口缺少分组身份的问题。

src/TaskbarIdentity.cs 使用 Windows 窗口属性存储，为已核验的 Codex 主进程中的可见 Electron 窗口设置原应用分组 ID：OpenAI.Codex_2p2nqsd0c76g0!App。宿主会在窗口恢复时再次检查。Windows 的窗口级 AppUserModelID 用于控制任务栏分组，见 [Microsoft 文档](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetpropertystoreforwindow)。

测试配置使用 CodexUsageCard.IsolatedTests 分组身份，避免与用户主实例混在同一任务栏分组。

## 本机验证

- 直接读取当前运行窗口，确认 System.AppUserModel.ID 已等于商店应用 ID。
- 用户实际确认任务栏已经只剩一个 Codex 图标。
- 更换插件宿主时保持原 Codex 主 PID；当前额度卡仍为单个卡片、双项真实额度有效。
- 独立测试通过 4 项恢复场景：专用入口恢复、原可执行文件入口恢复、连续点击 5 次、快速关窗重开。每轮均额外校验分组 ID 正确、可见主窗口仅一个、窗口消息响应、真实额度有效。结束后测试宿主和测试主实例均已退出。

## 旧入口清理

本机已移除旧版 Codex Usage Card 专用快捷方式和不再使用的旧启动器、旧监听器、脚本。保留 v5 后台监听器、v5 宿主、agent.cjs、配置与许可。清理前保留了工作目录内的本地恢复备份；项目源码及历史安装包不属于已安装的旧入口，没有删除。

从安装包选择 install-with-monitor.cmd 时，不再创建专用启动快捷方式或复制独立启动入口到安装目录。老版本升级者应取消固定旧版专用入口，沿用商店应用原图标。

这次修复仍为本机验证，未进行跨机器测试或实际注销登录测试；GitHub 公开版本尚未同步更新。
