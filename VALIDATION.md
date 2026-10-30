# 验证记录

2026-10-30，Windows，Codex 商店版 26.1027.3384.0，Node 24.22.1。

1. 原生启动器启动独立测试配置的真实 Codex。
2. 通过本机 app-server 获取真实账号额度。
3. 读取真实渲染器状态：`placed: true`、`mode: dual`、`badgeCount: 1`、两项 percent 均为有效数值。
4. 正常关闭测试主窗口，记录：`windowClosed: true`、`agentExited: true`。本次 Codex 自身后台进程没有退出，记录 `appExited: false`。
5. 再查询本轮对应的原生启动器和额度代理 PID，均不存在。
6. 上游额度和合并逻辑相关的 9 项既有测试继续通过；这不替代第 1—5 项真实测试。

未验证：用户原任务栏固定入口替换后的启动，以及其他 Windows / Codex / Node 版本。

GitHub 发布条件尚未满足，没有对外发布。
