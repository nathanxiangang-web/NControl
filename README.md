# NControl · Windows 控制中心

Windows 优化与系统管理工具（WPF / .NET 10）。第一代已完成：统一功能目录 + 执行中心 + 任务记录 + 状态检测，功能覆盖优化、应用、清理、修复、工具五大模块。

> 定位：**功能型测试版本**（产品文档《NControl_第一代产品开发文档_v0.2.md》）。强调统一入口和常用能力，不宣称覆盖所有电脑/系统版本，不保证所有优化都能提升性能。

---

## 快速开始

```powershell
# 构建
dotnet build NControl.slnx -c Debug

# 运行(App 默认以管理员身份运行)
dotnet run --project src/NControl.App

# 冒烟测试(225 项断言,无需管理员)
dotnet run --project tools/NControl.SmokeTest

# 功能统计(运行时权威数据:目录/分类/风险/预设引用)
dotnet run --project tools/NControl.FeatureStats

# 全量真机功能测试(逐项执行→备份→验证→恢复;需管理员)
dotnet run --project tools/NControl.FunctionTest
```

环境要求：.NET 10 SDK、Windows 10/11（中文系统）。PowerShell 5.1（内置，provider 固定绝对路径调用）。

---

## 项目结构

```
src/
  NControl.App/              启动入口:Host/DI/日志/模块注册/app.manifest(requireAdministrator)
  NControl.Core/             领域模型/功能目录/执行中心/恢复命令构建器/状态检测器/来源台账
  NControl.Infrastructure/   执行提供程序(PowerShell/cmd)/提权/SQLite 任务记录
  NControl.Modules.Optimization/  优化模块(154 项,7 分类,与 ZyperWin++ 文档 100% 对齐)
  NControl.Modules.Applications/  应用模块(10 项预装应用卸载)
  NControl.Modules.Cleanup/       清理模块(9 项)
  NControl.Modules.Repair/        修复模块(9 项)
  NControl.Modules.Tools/         工具模块(10 项)
  NControl.Presentation/     WPF 页面/ViewModel/主题/导航
tools/
  NControl.SmokeTest/        冒烟测试(225 断言)
  NControl.FeatureStats/     运行时功能统计 + 预设引用校验
  NControl.FunctionTest/     全量真机测试(逐项执行→备份→验证→恢复;需管理员)
  NControl.StartupVerify.ps1 启动渲染验证(句柄/标题/尺寸/像素)
  UiVerify/                  提权 UI 自动化验证(导航/点击/开关状态)
```

---

## 架构要点（后续开发必读）

### 数据流：功能目录 → 选择 → 执行中心 → 记录

```
IModuleRegistrar.RegisterFeatures() → IFunctionCatalog(全局功能目录)
用户选择(SelectionService) → IExecutionCenter(逐项执行)
  → IExecutionProvider(PowerShell/cmd, 提权自动处理)
  → 结果写入 SQLite 任务记录(%LocalAppData%\NControl\ncontrol.db)
```

- **新增功能**：在对应模块 Registrar 里 `catalog.Register(...)`，不需要改页面。页面只读目录。
- **新增预设**：`catalog.RegisterPreset(...)`。**高风险项(HighRisk)禁止进入任何预设**（冒烟测试有断言）。
- **执行命令**：`FunctionItem.Command` 是 PowerShell 脚本。provider 自动注入幂等保护（建键前 Test-Path、删除前 Test-Path）。
- **恢复命令**：`RestoreCommandBuilder.Build(item)` 从命令反推恢复（注册表→删值/服务→Manual/防火墙特例）。不可恢复的功能如实隐藏"恢复"按钮。
- **状态检测**：`StateDetector.Detect(item)` 从命令反推检测规则（注册表值/服务 StartType/电源方案/休眠），实时读系统状态，返回 已优化/未优化/不可检测。已优化项自动选中并显示绿色徽标。

### 风险等级（决定进不进预设）

| 等级 | 含义 | 预设 |
|---|---|---|
| Safe | 界面/非关键偏好 | ✅ 可进 |
| Recommended | 明确收益 | ✅ 可进 |
| Caution | 可能影响部分功能 | ⚠️ 可进,需说明 |
| HighRisk | 降低安全性/更新/稳定性 | ❌ 仅高级区域,单独确认,禁入预设 |
| Experimental | 效果依赖版本 | 默认隐藏/测试标识 |

### 提权与执行

- App 以管理员运行（`requireAdministrator`）。提权进程 PATH 不含 System32\WindowsPowerShell\v1.0 → provider 用绝对路径。
- 测试工具提权执行：`Start-Process powershell -Verb RunAs -File` 在本机**不稳定**（偶发退出码 1 无日志）；可靠方式是 **base64 编码脚本 + `-Command "iex ..."`**。

### 已知环境限制

- `TaskbarDa`（隐藏小组件按钮）：HKCU\...\Explorer\Advanced 键 ACL 特殊，Set-ItemProperty 报"未经授权"——环境限制，如实报失败。
- 全量测试可能污染系统（曾致 RemoteRegistry 禁用、Wallpaper 删除、DelegateFolders 键删除）——测试工具已加"恢复后验证"，新增 Remove-Item 类功能需补显式 RestoreCommand。

---

## 当前功能规模

- **功能目录 192 项**：优化 154（外观29/性能46/安全10/Edge12/系统15/更新8/隐私34）+ 应用 10 + 清理 9 + 修复 9 + 工具 10
- **预设 4 个**：轻度优化(8) / 推荐优化(115,按 ZyperWin++ 推荐配置对齐,含 Caution 级,不含 HighRisk) / 深度优化(62) / 删除常见预装应用(7)
- **验证基线**：冒烟 249 PASS / 0 FAIL；全量真机 154 PASS / 0 FAIL（优化模块）；状态检测覆盖率 147/154（95.5%）

## 第二代能力（已完成）

- **兼容性判断**：CompatibilityEngine 按功能 ID 规则判定 Supported/Unsupported/Unknown/NeedsVerification；环境探测（Windows 版本/构建号/架构/产品类型）；UI 行级徽标 + 预设卡警告统计
- **配置系统**：PlanService 保存/导出/导入 JSON 配置（只引用 FunctionId,不携带代码）；导入流程：解析→Schema 检查→ID 匹配去重→兼容性→风险；导入不自动执行,预览确认后进统一执行中心
- **批次回滚**：RollbackService 从历史任务分析可恢复项,逆序恢复,写新任务；UI：任务记录页"恢复本次任务"
- **清理扫描**：CleanupScanner 统计文件数+大小（真实数据）；UI：清理维护页扫描区块（开始扫描/进度/结果列表/总可清理空间）；实测可清理 1.31 GB/56138 文件

---

## 文档

- `PROJECT_TASKS.md` — 开发任务清单与轮次记录（第一~十三轮）
- `NControl_第一代产品开发文档_v0.2.md` — 产品文档（完成定义 §9.3、迭代路线 §10、验收标准 §12）
- `src/NControl.Core/Sources.cs` — 开源来源台账（ZyperWin++ 4.2 等）

## 开发约定

- 中文回复/注释/UI 文案。
- 提交粒度：一轮功能一个提交，commit message 说明改动+验证结果。
- 改 UI 后跑 `NControl.StartupVerify.ps1` 确认渲染；改目录/预设后跑 `NControl.FeatureStats` + `NControl.SmokeTest`。
- 不要删除来源、许可证和功能说明信息；不要自行增加一级导航或改变模块名称（文档 §11.2）。

## 第二代规划（文档 §10）

状态感知（兼容性提示）→ 恢复与配置（批次回滚/配置导入导出）→ 更新治理 → 专业能力。所有能力围绕现有底座生长，不改变产品结构。

> ✅ 第二代已完成：兼容性判断、配置保存/导入/导出、批次回滚、清理扫描与空间统计（见 PROJECT_TASKS_GEN2.md）
