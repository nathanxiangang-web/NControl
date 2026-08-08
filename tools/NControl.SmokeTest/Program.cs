using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NControl.Core;
using NControl.Infrastructure;
using NControl.Modules.Applications;
using NControl.Modules.Cleanup;
using NControl.Modules.Optimization;
using NControl.Modules.Repair;
using NControl.Modules.Tools;

// ============================================================
// NControl 无头冒烟测试:验证 功能目录 → 执行中心 → SQLite 记录 完整管线
// 不修改真实系统状态(探针只做输出,不写注册表/服务)。
// ============================================================

var failures = 0;

void Check(bool condition, string name, string? detail = null)
{
    if (condition)
    {
        Console.WriteLine($"[PASS] {name}");
    }
    else
    {
        failures++;
        Console.WriteLine($"[FAIL] {name}{(detail is null ? "" : $" -> {detail}")}");
    }
}

// ---------- 1. 功能目录 ----------
var loggerFactory = LoggerFactory.Create(b => { });
var catalog = new FunctionCatalog(loggerFactory.CreateLogger<FunctionCatalog>());
IModuleRegistrar[] registrars =
{
    new OptimizationModuleRegistrar(),
    new ApplicationsModuleRegistrar(),
    new CleanupModuleRegistrar(),
    new RepairModuleRegistrar(),
    new ToolsModuleRegistrar()
};
foreach (var r in registrars) r.RegisterFeatures(catalog);
foreach (var r in registrars) r.RegisterPresets(catalog);

Console.WriteLine($"功能目录: {catalog.All.Count} 项, 方案: {catalog.Presets.Count} 个");
Check(catalog.All.Count >= 150, "功能目录不少于 150 项", $"实际 {catalog.All.Count}");
Check(catalog.Presets.Count >= 4, "方案不少于 4 个", $"实际 {catalog.Presets.Count}");

// 每个功能项字段完整
Check(catalog.All.All(f => !string.IsNullOrWhiteSpace(f.Id) && !string.IsNullOrWhiteSpace(f.Name)
                           && !string.IsNullOrWhiteSpace(f.Category) && !string.IsNullOrWhiteSpace(f.Description)
                           && !string.IsNullOrWhiteSpace(f.Source) && !string.IsNullOrWhiteSpace(f.Command)),
    "所有功能项字段完整(Id/名称/分类/说明/来源/命令)");

// 方案只引用存在的功能;高风险不进预设
var presetHighRisk = new List<string>();
foreach (var preset in catalog.Presets)
{
    foreach (var id in preset.FeatureIds)
    {
        var item = catalog.Find(id);
        Check(item is not null, $"方案 [{preset.Id}] 引用的功能存在: {id}");
        if (item is not null && item.Risk == RiskLevel.HighRisk)
            presetHighRisk.Add($"{preset.Id}:{id}");
    }
}
Check(presetHighRisk.Count == 0, "任何预设都不包含高风险功能", presetHighRisk.Count == 0 ? null : string.Join(",", presetHighRisk));

// 方案引用不重复注册
Check(catalog.All.Select(f => f.Id).Distinct().Count() == catalog.All.Count, "功能 Id 无重复");

// ---------- 2. 执行中心 ----------
var tempFolder = Path.Combine(Path.GetTempPath(), "NControlSmoke", Guid.NewGuid().ToString("N"));
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["NControl:DataFolder"] = tempFolder })
    .Build();
var paths = new AppPaths(config);
var store = new SqliteTaskRecordStore(paths, loggerFactory.CreateLogger<SqliteTaskRecordStore>());
var psProvider = new PowerShellExecutionProvider(loggerFactory.CreateLogger<PowerShellExecutionProvider>());
var cmdProvider = new CommandExecutionProvider(loggerFactory.CreateLogger<CommandExecutionProvider>());
var center = new ExecutionCenter(
    new IExecutionProvider[] { psProvider, cmdProvider },
    store,
    loggerFactory.CreateLogger<ExecutionCenter>());

var progressEvents = new List<TaskItemProgress>();
var progress = new Progress<TaskItemProgress>(p => progressEvents.Add(p));

var probeOk = new FunctionItem
{
    Id = "smoke.probe-ok",
    Name = "冒烟探针(成功)",
    Category = "测试",
    Module = ModuleKind.Tools,
    Description = "仅输出文本,不修改系统。",
    Risk = RiskLevel.Safe,
    RequiresAdmin = false,
    Restart = RestartRequirement.None,
    Source = "测试",
    Kind = ExecutionKind.PowerShell,
    Command = "Write-Output 'smoke-ok'; Write-Output ('2+3=' + (2+3)); exit 0"
};
var probeFail = new FunctionItem
{
    Id = "smoke.probe-fail",
    Name = "冒烟探针(失败)",
    Category = "测试",
    Module = ModuleKind.Tools,
    Description = "故意以非零退出码结束。",
    Risk = RiskLevel.Safe,
    RequiresAdmin = false,
    Restart = RestartRequirement.None,
    Source = "测试",
    Kind = ExecutionKind.PowerShell,
    Command = "Write-Output 'before-fail'; Write-Error 'boom'; exit 1"
};
var probeCmd = new FunctionItem
{
    Id = "smoke.probe-cmd",
    Name = "冒烟探针(命令)",
    Category = "测试",
    Module = ModuleKind.Tools,
    Description = "系统命令执行方式。",
    Risk = RiskLevel.Safe,
    RequiresAdmin = false,
    Restart = RestartRequirement.None,
    Source = "测试",
    Kind = ExecutionKind.Command,
    Command = "echo cmd-ok"
};

var record = await center.ExecuteAsync(
    new ExecutionRequest("冒烟测试任务", new[] { probeOk, probeFail, probeCmd }),
    progress,
    CancellationToken.None);

Console.WriteLine($"任务结果: {record.Result} (成功 {record.SuccessCount} / 失败 {record.FailedCount} / 取消 {record.CancelledCount})");
Check(record.SuccessCount == 2 && record.FailedCount == 1, "执行结果:2 成功 + 1 失败");
Check(record.Result == "部分失败", "任务汇总为“部分失败”");
Check(record.Items[0].Output?.Contains("smoke-ok") == true, "成功项输出包含 smoke-ok", record.Items[0].Output);
Check(record.Items[1].Status == "失败" && !string.IsNullOrWhiteSpace(record.Items[1].Error), "失败项记录了错误信息", record.Items[1].Error);
Check(record.Items[2].Output?.Contains("cmd-ok") == true, "命令执行项输出包含 cmd-ok", record.Items[2].Output);
Check(progressEvents.Count >= 3, "进度事件不少于 3 条", $"实际 {progressEvents.Count}");
Check(record.Id > 0, "任务记录已写入 SQLite 并获得 Id", $"Id={record.Id}");

// ---------- 3. 记录持久化 ----------
var recent = await store.GetRecentAsync(5);
Check(recent.Count >= 1 && recent[0].Id == record.Id, "最近记录可读回且为最新任务");
Check(recent[0].Items.Count == 3, "记录明细完整(3 项)");
var all = await store.GetAllAsync();
Check(all.Count == recent.Count && all[0].Id == record.Id, "全部记录可枚举");

// 高级分类治理:该分类下不允许安全/推荐级功能(文档 §6.3 高级区域;谨慎级服务项可在此,但不进预设)
var advancedNonHighRisk = catalog.ByModule(ModuleKind.Optimization)
    .Where(f => f.Category == "高级" && f.Risk is RiskLevel.Safe or RiskLevel.Recommended)
    .Select(f => $"{f.Id}:{f.Risk}")
    .ToArray();
Check(advancedNonHighRisk.Length == 0, "高级分类不含安全/推荐级功能", string.Join(",", advancedNonHighRisk));

// ---------- 4. 恢复命令生成器 ----------
static FunctionItem RestoreProbe(string id, string command) => new()
{
    Id = id,
    Name = "恢复探针",
    Category = "测试",
    Module = ModuleKind.Tools,
    Description = "",
    Risk = RiskLevel.Safe,
    RequiresAdmin = false,
    Restart = RestartRequirement.None,
    Source = "测试",
    Kind = ExecutionKind.PowerShell,
    Command = command
};

var restoreReg = RestoreCommandBuilder.Build(RestoreProbe("t.restore", "Set-ItemProperty -Path 'HKCU:\\Software\\Test' -Name 'Foo' -Value 1 -Type DWord -Force"));
Check(restoreReg?.Contains("Remove-ItemProperty -Path 'HKCU:\\Software\\Test' -Name 'Foo'") == true,
    "恢复生成器:注册表写入 -> 反向删除", restoreReg);

var restoreSvc = RestoreCommandBuilder.Build(RestoreProbe("t.restore-svc", "Stop-Service SysMain -Force -ErrorAction SilentlyContinue; Set-Service SysMain -StartupType Disabled"));
Check(restoreSvc?.Contains("Set-Service SysMain -StartupType Manual") == true,
    "恢复生成器:服务禁用 -> 恢复手动启动", restoreSvc);

var restoreHpet = RestoreCommandBuilder.Build(RestoreProbe("t.restore-hpet", "bcdedit /set useplatformclock false"));
Check(restoreHpet?.Contains("bcdedit /deletevalue useplatformclock") == true,
    "恢复生成器:bcdedit 特例", restoreHpet);

var restorePower = RestoreCommandBuilder.Build(RestoreProbe("t.restore-power", "powercfg /h off; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power' -Name 'HibernateEnabled' -Value 0 -Type DWord -Force"));
Check(restorePower?.Contains("powercfg /h on") == true && restorePower?.Contains("Remove-ItemProperty") == true,
    "恢复生成器:电源特例 + 注册表删除混合", restorePower);

// 目录中可恢复功能占比(注册表/服务类应绝大多数可推导)
var restorable = catalog.All.Count(f => RestoreCommandBuilder.Build(f) is not null);
Console.WriteLine($"可恢复功能: {restorable} / {catalog.All.Count}");
Check(restorable >= catalog.All.Count * 0.6, "目录中可恢复功能占比不低于 60%", $"{restorable}/{catalog.All.Count}");

// 搜索
Check(catalog.Search("SysMain").Any(f => f.Id == "advanced.disable-sysmain"), "搜索可命中功能");
Check(catalog.Search("不存在的关键词xyz").Count == 0, "搜索无结果时返回空");

try
{
    Directory.Delete(tempFolder, true);
}
catch { }

Console.WriteLine(failures == 0
    ? "========== 冒烟测试全部通过 =========="
    : $"========== 冒烟测试失败 {failures} 项 ==========");
return failures == 0 ? 0 : 1;
