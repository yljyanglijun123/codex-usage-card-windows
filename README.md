# Codex Usage Card for Windows

非官方 Codex Windows 剩余额度侧栏插件，基于 [Lorie7813/codex-usage-card](https://github.com/Lorie7813/codex-usage-card) v0.9.4 Beta 移植，保留 MIT 许可，与 OpenAI 无官方关联。

## 版本与使用

v0.9.6-windows.1 Preview。支持在数据可用时显示 5 小时、每周剩余额度及重置时间。每次启动会重新查找当前 Codex、CLI 和 Node 的路径。

需要 Windows、已安装并登录的 Codex 商店版，以及 Node.js 24+。完整解压 Windows 安装包，双击 install.cmd，再使用生成的 Codex Usage Card 快捷方式启动。可以将这个新快捷方式固定到任务栏。

默认方案不需要常驻启动监听器；主窗口关闭后额度代理结束。原商店图标自动接管是非默认实验功能，日常任务栏验收尚未通过，不建议首次使用者启用。

## 验证范围

已在一台 Windows 电脑的独立真实 Codex 实例验证安装、真实额度显示和关窗后代理退出；另有 8 项安装场景测试与 9 项路径解析测试。没有在另一台干净 Windows、ARM64 或组织策略环境中验证，未完成完整卸载流程测试。仍是测试版，不保证所有电脑下载即用，也不保证兼容未来内部接口。

## 安全与隐私

使用 127.0.0.1:39222 的本机 CDP 调试接口，权限较高，请勿转发端口。通过本机 CLI 读取额度，不请求聊天内容、不扫描项目、不额外上传遥测。程序未签名，不要关闭系统安全防护或绕过安全拦截。

## 下载

请前往 [Releases](https://github.com/yljyanglijun123/codex-usage-card-windows/releases) 查看安装包。请下载 Windows 安装 ZIP，而非自动生成的 Source code 归档。

源码与安装包上传进行中，未出现发布附件前请勿将仓库视为已完成发布。
