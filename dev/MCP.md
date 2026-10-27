# 《霓虹余烬 · 未删之城》v0.4 本地 MCP

游戏专用的标准 stdio MCP，使用 Node.js 内置模块，无 npm 依赖，不开放网络端口。服务支持构建本游戏、运行固定测试、渲染白名单场景和查询开发产物。玩家正常游玩不需要启动 MCP。

## 连接方式

在支持 MCP 的宿主中添加 stdio 服务，`command` 为本机 `node`，`args` 为 `dev/mcp-server.cjs` 的绝对路径。若 Node.js 未加入 PATH，可将 `command` 改为 `node.exe` 的绝对路径。下例中的路径需与实际解压目录一致：

```json
{
  "mcpServers": {
    "neon-ember-dev": {
      "command": "node",
      "args": ["C:/Users/Kayeung/Documents/Codex/2026-10-11/new-chat-3/outputs/NeonEmber/dev/mcp-server.cjs"]
    }
  }
}
```

服务使用 JSON-RPC 消息行，支持 `initialize`、`tools/list`、`tools/call` 与 `ping`。标准输出保留给协议消息。随附 `call.cjs` 可独立完成初始化握手和一次工具调用；交付文件本身不会替你修改其他软件的全局 MCP 配置。

## 六个工具

| 工具 | 参数 | 结果 |
| --- | --- | --- |
| `game_build` | `{}` | 编译 Windows x64 客户端，返回可执行文件路径、大小与编译输出 |
| `game_test` | `{}` | 运行战斗回归与带种子的长局模拟，返回 `reports/simulation.md` |
| `game_tactics_test` | `{}` | 验证预判瞄准、协同队伍、断链、体系机制与剧情解锁，返回 `reports/tactics.md` |
| `game_adaptive_test` | `{}` | 验证弧线观察、阶段配合、精英/Boss 反制及固定构筑路线，返回 `reports/adaptive.md` |
| `game_scene` | `{"scene":"tactics"}` | 在独立进程绘制真实客户端画面，返回 PNG 路径和 JSON 场景状态 |
| `game_status` | `{}` | 查询客户端构建是否存在及报告文件列表 |

服务不提供任意 shell、任意路径读写或其他软件的桌面控制。除了状态查询，各工具会更新本游戏目录内的构建或开发产物。

## 场景白名单

| 场景 | 用途 |
| --- | --- |
| `menu` | 开始菜单、三种起始核心与世界简介 |
| `play` | 常规战斗及 HUD |
| `level` | 升级选择 |
| `shop` | 修补匠「弥」的补给界面 |
| `boss` | 监察者战斗 |
| `pause` | 暂停与设置 |
| `help` | 操作与战斗指南 |
| `build` | 构筑面板 |
| `over` | 战斗结束与纪录 |
| `stress` | 固定实体数量的渲染压力夹具 |
| `tactics` | 指挥节点、护卫、交叉火力与截击关系 |
| `archive` | 已解锁的剧情档案 |
| `schools` | 三系投入、觉醒与混搭协同 |
| `upgrades` | 三个体系的高等级升级卡，检查说明与排版 |
| `transition` | 升级后进入补给的点击回归；断言同一点击不会购买商品 |
| `encounter` / `elite` | 四阶段编队与机制精英的画面检视 |
| `codex` | 暂停式敌人图鉴 |
| `boss_patterns` | 三个独立的真实 Boss 前摇快照，检视圆/线/环形状 |
| `runtime` | 2400 帧真实渲染与战斗模拟，自动选择升级和继续补给，定期恢复生命；只验证运行稳定性 |

这些是确定性的开发夹具，用于直接覆盖 UI 状态，不控制正在游玩的客户端，也不写入玩家的正式纪录或剧情进度。输出位于 `dev/reports/<scene>.png` 和对应 `.json`。

所有自动场景导出 1920×1080 图像，JSON 包含画布尺寸、精英数量、危险区和当前编队阶段。`stress` 采样 210 帧，报告平均值和 P95 渲染提交及呈现耗时，不包含完整逻辑模拟。其他场景的 `renderSamples` 为 0；不要将耗时字段解释为实测 0 毫秒。`runtime` 每帧执行 6 个逻辑步；其 `runtimeStepBatchMeanMs` 是整批平均核心耗时，不含渲染，也不是单步 FPS。

## 使用随附客户端

在游戏根目录执行：

```powershell
node dev/call.cjs game_build
node dev/call.cjs game_test
node dev/call.cjs game_tactics_test
node dev/call.cjs game_adaptive_test
node dev/call.cjs game_status
```

Windows PowerShell 中传递场景 JSON 时，可用停止解析标记保留引号：

```powershell
node --% dev/call.cjs game_scene "{\"scene\":\"tactics\"}"
node --% dev/call.cjs game_scene "{\"scene\":\"archive\"}"
node --% dev/call.cjs game_scene "{\"scene\":\"upgrades\"}"
```

在 MCP 宿主中则直接传递结构化参数，例如 `{"scene":"tactics"}`，无需处理 shell 引号。

构建前关闭正在运行的游戏，避免 Windows 锁定 exe。工具执行失败会返回 MCP `isError`，随附调用客户端也返回非零退出码。构建与测试使用 Windows 的 .NET Framework C# 编译器；正常游戏运行不需要开发接口。

## 报告与源码关系

最新战斗回归及长局诊断见 [simulation.md](reports/simulation.md)，协同 AI 和体系行为结果见 [tactics.md](reports/tactics.md)。以报告实际结果为准，不把截图夹具视为完整流程通过。

独立模拟需要共同编译 `src/Game.cs`、`src/Tactics.cs`、`src/World.cs`，再加入对应的 `dev/GameTests.cs` 或 `dev/TacticsTests.cs`。两套测试均可接收报告路径；`GameTests` 的第二参数 `quick` 可跳过长局诊断。通过上述 MCP 工具调用时已由服务完成这些编译和参数处理。
