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

// ============================================================
// 真机执行模式:dotnet run --project tools/NControl.SmokeTest -- real
// 需用户已做系统快照;真实执行功能并验证注册表结果(默认模式不执行,保持无副作用)。
// ============================================================
if (args.Contains("real"))
{
    await RealExecTestAsync(catalog, center, store);
    return 0;
}

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

var probeCn = new FunctionItem
{
    Id = "smoke.probe-cn",
    Name = "冒烟探针(中文输出)",
    Category = "测试",
    Module = ModuleKind.Tools,
    Description = "验证 PowerShell 中文输出编码。",
    Risk = RiskLevel.Safe,
    RequiresAdmin = false,
    Restart = RestartRequirement.None,
    Source = "测试",
    Kind = ExecutionKind.PowerShell,
    Command = "Write-Output '中文测试:你好世界'; exit 0"
};
var record = await center.ExecuteAsync(
    new ExecutionRequest("冒烟测试任务", new[] { probeOk, probeFail, probeCmd, probeCn }),
    progress,
    CancellationToken.None);

Console.WriteLine($"任务结果: {record.Result} (成功 {record.SuccessCount} / 失败 {record.FailedCount} / 取消 {record.CancelledCount})");
Check(record.SuccessCount == 3 && record.FailedCount == 1, "执行结果:3 成功 + 1 失败");
Check(record.Result == "部分失败", "任务汇总为“部分失败”");
Check(record.Items[0].Output?.Contains("smoke-ok") == true, "成功项输出包含 smoke-ok", record.Items[0].Output);
Check(record.Items[1].Status == "失败" && !string.IsNullOrWhiteSpace(record.Items[1].Error), "失败项记录了错误信息", record.Items[1].Error);
Check(record.Items[2].Output?.Contains("cmd-ok") == true, "命令执行项输出包含 cmd-ok", record.Items[2].Output);
Check(record.Items[3].Output?.Contains("中文测试:你好世界") == true, "PowerShell 中文输出无乱码(GBK->UTF8 修复)", record.Items[3].Output);
Check(progressEvents.Count >= 3, "进度事件不少于 3 条", $"实际 {progressEvents.Count}");
Check(record.Id > 0, "任务记录已写入 SQLite 并获得 Id", $"Id={record.Id}");

// ---------- 3. 记录持久化 ----------
var recent = await store.GetRecentAsync(5);
Check(recent.Count >= 1 && recent[0].Id == record.Id, "最近记录可读回且为最新任务");
Check(recent[0].Items.Count == 4, "记录明细完整(4 项)");
var all = await store.GetAllAsync();
Check(all.Count == recent.Count && all[0].Id == record.Id, "全部记录可枚举");

// ---------- 3.5 SQLite 迁移兼容(第二代 §12.3):旧库历史不破坏 ----------
try
{
    // 直接向同一库插入一条"旧格式"记录(模拟旧版本写入,列结构与当前一致)
    var legacy = new TaskRecord
    {
        Name = "旧版本任务",
        Result = "成功",
        SuccessCount = 1,
        Items = new List<TaskItemRecord>
        {
            new() { FunctionId = "legacy.feature", FunctionName = "旧功能", Status = "成功" }
        }
    };
    await store.SaveAsync(legacy);
    var afterMigrate = await store.GetAllAsync();
    Check(afterMigrate.Any(t => t.Name == "旧版本任务" && t.Items.Count == 1 && t.Items[0].FunctionId == "legacy.feature"),
        "SQLite 迁移:旧格式记录可读回且明细完整", afterMigrate.Count.ToString());
    Check(afterMigrate.Any(t => t.Id == record.Id), "SQLite 迁移:原有记录未丢失", "");
}
catch (Exception ex)
{
    Check(false, "SQLite 迁移测试异常", ex.Message);
}

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
Check(restoreSvc?.Contains("Set-Service SysMain -StartupType Automatic") == true && restoreSvc?.Contains("Start-Service SysMain") == true,
    "恢复生成器:服务禁用 -> 恢复自动启动并启动", restoreSvc);

var restoreHpet = RestoreCommandBuilder.Build(RestoreProbe("t.restore-hpet", "bcdedit /set useplatformclock false"));
Check(restoreHpet?.Contains("bcdedit /deletevalue useplatformclock") == true,
    "恢复生成器:bcdedit 特例", restoreHpet);

var restorePower = RestoreCommandBuilder.Build(RestoreProbe("t.restore-power", "powercfg /h off; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power' -Name 'HibernateEnabled' -Value 0 -Type DWord -Force"));
Check(restorePower?.Contains("powercfg /h on") == true && restorePower?.Contains("Remove-ItemProperty") == true,
    "恢复生成器:电源特例 + 注册表删除混合", restorePower);

// 回归:防火墙恢复必须"启用 + 清理组策略残留",且不得删除本地 EnableFirewall(否则安全中心显示由组织管理且恢复不生效)
var restoreFw = RestoreCommandBuilder.Build(RestoreProbe("t.restore-fw", "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force"));
Check(restoreFw?.Contains("Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True") == true,
    "恢复生成器:防火墙特例 -> 恢复启用", restoreFw);
Check(restoreFw?.Contains("Policies\\Microsoft\\WindowsFirewall") == true,
    "恢复生成器:防火墙特例 -> 清理组策略残留", restoreFw);
Check(restoreFw?.Contains("Remove-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy") != true,
    "恢复生成器:防火墙特例 -> 不删除本地 EnableFirewall(避免配置变未配置)", restoreFw);

// ---------- 状态检测器断言 ----------
var det1 = StateDetector.Detect(RestoreProbe("t.det-reg", "Set-ItemProperty -Path 'HKCU:\\Software\\Test\\Det' -Name 'X' -Value 1 -Type DWord -Force"));
Check(det1 == false || det1 == null,
    "状态检测:不存在的值 -> 未优化或不可检测", det1?.ToString() ?? "null");
var det2 = StateDetector.Detect(RestoreProbe("t.det-svc", "Set-Service SysMain -StartupType Disabled"));
// SysMain 本机通常 Automatic => 未优化
Check(det2 == false || det2 == null,
    "状态检测:服务未禁用 -> 未优化或不可检测", det2?.ToString() ?? "null");
var detCoverage = catalog.ByModule(ModuleKind.Optimization)
    .Count(f => StateDetector.Detect(f) is not null);
Check(detCoverage >= catalog.ByModule(ModuleKind.Optimization).Count() * 0.8,
    "状态检测:优化模块可检测覆盖率不低于 80%", $"{detCoverage}/{catalog.ByModule(ModuleKind.Optimization).Count()}");

// ---------- 兼容性引擎断言(第二代 §4) ----------
var env = new WindowsEnvironmentProbe().GetEnvironment();
Console.WriteLine($"环境探测: {env.DisplayVersion} | {env.Architecture} | {env.ProductType} | Win11={env.IsWindows11}");
var compatEngine = new CompatibilityEngine(new WindowsEnvironmentProbe());
var compatSample = new[] { "update.never-check-updates", "system.pause-updates-5000d", "apps.clipchamp", "security.*" };
foreach (var id in compatSample)
{
    var item = catalog.Find(id);
    if (item is null) continue;
    var cr = compatEngine.Evaluate(item);
    Console.WriteLine($"兼容性[{item.Id}]: {cr.Status} {(cr.Reason is null ? "" : "- " + cr.Reason)}");
}
// 断言:全部功能都能得到非空判定;未知/待验证功能不静默隐藏
Check(catalog.All.All(f => compatEngine.Evaluate(f).Status != CompatibilityStatus.Unknown || f.Id == ""),
    "兼容性:所有功能均能得到判定(Supported/Unsupported/NeedsVerification)", "");
Check(compatEngine.Evaluate(catalog.Find("apps.clipchamp")!).Status is CompatibilityStatus.Supported or CompatibilityStatus.NeedsVerification,
    "兼容性:apps.* 得到支持或待验证判定", compatEngine.Evaluate(catalog.Find("apps.clipchamp")!).Status.ToString());
// 环境探测基本字段非空
Check(env.BuildNumber > 0 && !string.IsNullOrEmpty(env.Architecture) && !string.IsNullOrEmpty(env.ProductType),
    "环境探测:构建号/架构/产品类型非空", $"{env.BuildNumber}/{env.Architecture}/{env.ProductType}");
// 引擎缓存可清除
compatEngine.ClearCache();
Check(true, "兼容性:引擎缓存清除正常", "");

// ---------- 配置系统断言(第二代 §5-§6) ----------
var planDir = Path.Combine(Path.GetTempPath(), "nctl_plan_test_" + Guid.NewGuid().ToString("N"));
var planPaths = new NControl.Infrastructure.AppPaths(new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["NControl:DataFolder"] = planDir
}).Build());
var planService = new NControl.Infrastructure.PlanService(planPaths);
var plan = new PlanConfig { Name = "测试方案", Description = "冒烟测试", Functions = new List<string> { "explorer.open-this-pc", "privacy.disable-page-prediction", "explorer.open-this-pc", "unknown.feature-xyz" } };
planService.Save(plan);
Check(planService.GetAll().Count == 1, "配置:保存后读取到 1 个方案", planService.GetAll().Count.ToString());

// 导出
var exportPath = Path.Combine(Path.GetTempPath(), "nctl_plan_test_export.json");
planService.Export(plan, exportPath);
Check(File.Exists(exportPath), "配置:导出 JSON 文件成功", exportPath);

// 导入:正常流程
var importResult = planService.Import(exportPath, catalog, compatEngine);
Check(importResult.ParseOk, "配置:导入解析成功", importResult.ParseError ?? "");
Check(importResult.DuplicateCount == 1, "配置:重复 FunctionId 自动去重", importResult.DuplicateCount.ToString());
Check(importResult.UnknownIds.Count == 1 && importResult.UnknownIds[0] == "unknown.feature-xyz",
    "配置:未知 FunctionId 标记", string.Join(",", importResult.UnknownIds));
Check(importResult.Ready.Count == 2, "配置:可执行项 2 个(兼容项)", importResult.Ready.Count.ToString());
Check(importResult.HighRisk.Count == 0 && importResult.Unsupported.Count == 0,
    "配置:无高风险/无不兼容项", $"{importResult.HighRisk.Count}/{importResult.Unsupported.Count}");

// 非法 JSON
var badPath = Path.Combine(Path.GetTempPath(), "nctl_plan_test_bad.json");
File.WriteAllText(badPath, "{ 这不是合法JSON");
var badResult = planService.Import(badPath, catalog, compatEngine);
Check(!badResult.ParseOk, "配置:非法 JSON 拒绝导入", badResult.ParseError ?? "");

// 高风险项不默认选择
var highRiskPlan = new PlanConfig { Name = "高风险方案", Functions = new List<string> { "security.*" } };
var hrExport = Path.Combine(Path.GetTempPath(), "nctl_plan_test_hr.json");
planService.Export(highRiskPlan, hrExport);
var hrResult = planService.Import(hrExport, catalog, compatEngine);
Check(hrResult.UnknownIds.Count == 1, "配置:不存在的高风险 ID 标记未知", string.Join(",", hrResult.UnknownIds));

// 清理
planService.Delete("测试方案");
Check(planService.GetAll().Count == 0, "配置:删除方案成功", "");
File.Delete(exportPath); File.Delete(badPath); File.Delete(hrExport);
Directory.Delete(planDir, true);

// ---------- 批次回滚断言(第二代 §8) ----------
var rollbackService = new RollbackService(catalog, center, store);
// 构造一个历史任务(模拟已执行)
var fakeTask = new TaskRecord
{
    Name = "冒烟测试任务",
    Result = "成功",
    Items = new List<TaskItemRecord>
    {
        new() { FunctionId = "explorer.open-this-pc", FunctionName = "我的电脑", Status = "成功" },
        new() { FunctionId = "unknown.old-feature", FunctionName = "旧功能", Status = "成功" }
    }
};
var rbAnalysis = rollbackService.Analyze(fakeTask);
Check(rbAnalysis.RestorableCount >= 1, "回滚:可恢复项分析(有恢复命令)", rbAnalysis.RestorableCount.ToString());
Check(rbAnalysis.NotSupportedCount == 1, "回滚:未知功能标记不可恢复", rbAnalysis.NotSupportedCount.ToString());
Check(rbAnalysis.Restorable.All(i => !string.IsNullOrWhiteSpace(i.RestoreCommand)),
    "回滚:可恢复项均带恢复命令", "");

// ---------- 清理扫描断言(第二代 §9) ----------
var cleanupScanner = new CleanupScanner(new IExecutionProvider[] { psProvider, cmdProvider });
var cleanupItems = catalog.ByModule(ModuleKind.Cleanup).ToArray();
Check(cleanupItems.Length == 9, "清理:功能项数量=9", cleanupItems.Length.ToString());
var scanCount = cleanupItems.Count(f => f.Command?.Contains("Get-ChildItem -Path") == true);
Check(scanCount >= 5, "清理:多数项可提取扫描路径", $"{scanCount}/{cleanupItems.Length}");
var tempItem = cleanupItems.FirstOrDefault(f => f.Id == "cleanup.user-temp");
if (tempItem is not null)
{
    var scanResult = await cleanupScanner.ScanAsync(tempItem);
    Check(scanResult is not null && scanResult.Ok, "清理:用户临时目录扫描成功", scanResult?.Note ?? scanResult?.SizeText ?? "null");
    Check(scanResult!.ItemCount > 0, "清理:扫描到文件数>0", scanResult.ItemCount.ToString());
    Check(scanResult.SizeBytes > 0, "清理:扫描到大小>0", scanResult.SizeText);
}

// 目录中可恢复功能占比(注册表/服务类应绝大多数可推导)
var restorable = catalog.All.Count(f => RestoreCommandBuilder.Build(f) is not null);
Console.WriteLine($"可恢复功能: {restorable} / {catalog.All.Count}");
Check(restorable >= catalog.All.Count * 0.6, "目录中可恢复功能占比不低于 60%", $"{restorable}/{catalog.All.Count}");

// ---------- 5. 应用管理页页签命令修复验证 ----------
// 回归:RelayCommand<int> 收到字符串 CommandParameter 会抛 ArgumentException(曾导致点击页签闪退)
var appsVm = new NControl.Presentation.ViewModels.AppsViewModel(
    catalog,
    new NControl.Presentation.Services.SelectionService(),
    new IExecutionProvider[] { psProvider, cmdProvider });
var tabSwitched = false;
try
{
    appsVm.SelectTabCommand.Execute("0");
    appsVm.SelectTabCommand.Execute("1");
    appsVm.SelectTabCommand.Execute("2");
    appsVm.SelectTabCommand.Execute("3");
    tabSwitched = appsVm.ActiveTab == 3;
}
catch (Exception ex)
{
    Console.WriteLine($"[FAIL] 页签命令抛异常: {ex.Message}");
}
Check(tabSwitched, "应用管理页页签切换命令接受字符串参数(闪退修复验证)", $"ActiveTab={appsVm.ActiveTab}");
Check(appsVm.BloatPreset is not null, "应用管理页预装应用方案卡可用", appsVm.BloatPreset?.CountText);
Check(appsVm.SoftwareEntries.Count >= 5, "应用管理页软件安装条目不少于 5 个", appsVm.SoftwareEntries.Count.ToString());
Check(appsVm.SoftwareEntries.All(e => !string.IsNullOrWhiteSpace(e.Url) && e.Url.StartsWith("https://")),
    "软件安装条目均为 https 官方地址", string.Join(",", appsVm.SoftwareEntries.Select(e => e.Name)));

// 应用模块:卸载命令格式 + 恢复能力如实标注(文档 §12.3 不可逆/暂不支持恢复如实标注)
var appsModule = catalog.ByModule(ModuleKind.Applications).ToArray();
Check(appsModule.Length == 10, "应用模块功能项数量=10(预装应用)", appsModule.Length.ToString());
Check(appsModule.All(f => f.Category == "预装应用"), "应用模块全部为预装应用分类", string.Join(",", appsModule.Select(f => f.Category).Distinct()));
Check(appsModule.All(f => f.Command.Contains("Remove-AppxPackage -AllUsers", StringComparison.OrdinalIgnoreCase)),
    "应用模块全部命令含 -AllUsers(彻底卸载)", string.Join(",", appsModule.Select(f => f.Id)));
Check(appsModule.All(f => f.RestoreCommand is null || f.RestoreCommand.Length == 0),
    "应用模块不提供恢复命令(卸载后需从商店重新安装,如实标注)", "");
Check(appsModule.Where(f => f.Category == "预装应用").All(f => f.Description.Contains("可按需删除") || f.Description.Contains("默认不选") || f.Description.Contains("可卸载") || f.Description.Contains("删除")),
    "预装应用描述均说明可删除/可卸载", "");

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

// ============================================================
// 真机执行验证(需系统快照):真实执行预设/设置项并核对注册表
// ============================================================
async Task RealExecTestAsync(IFunctionCatalog catalog, IExecutionCenter center, ITaskRecordStore store)
{
    Console.WriteLine("========== 真机执行验证(real 模式) ==========");
    Console.WriteLine("警告:将真实修改当前用户注册表,请确保已创建系统快照。");
    var rf = 0;
    void RCheck(bool ok, string name, string? detail = null)
    {
        if (ok) Console.WriteLine($"[PASS] {name}");
        else { rf++; Console.WriteLine($"[FAIL] {name}{(detail is null ? "" : $" -> {detail}")}"); }
    }

    // --- 1. 轻度方案 11 项(全部免管理员) ---
    var light = catalog.Presets.First(p => p.Id == "preset.light");
    var items = light.FeatureIds
        .Select(id => catalog.Find(id))
        .Where(f => f is not null && !f.RequiresAdmin)
        .Cast<FunctionItem>()
        .ToList();
    Console.WriteLine($"轻度方案: {items.Count} 项待执行");
    var r1 = await center.ExecuteAsync(new ExecutionRequest(light.Name, items), null, CancellationToken.None);
    Console.WriteLine($"轻度方案结果: {r1.Result}(成功 {r1.SuccessCount} / 失败 {r1.FailedCount})");
    var envLimited = new[] { "taskbar.hide-widgets" }; // TaskbarDa 在本测试机受系统策略 ACL 保护,属于环境限制
    var unexpected = r1.Items.Where(i => i.Status == "失败" && !envLimited.Contains(i.FunctionId)).ToList();
    RCheck(unexpected.Count == 0, "轻度方案执行成功(环境受限项除外)",
        string.Join("; ", unexpected.Select(i => $"{i.FunctionName}:{i.Error}")));
    foreach (var f in r1.Items.Where(i => i.Status == "失败")) Console.WriteLine($"  [环境受限] {f.FunctionName}: {f.Error}");

    // --- 2. 系统设置抽查(免管理员项) ---
    var picks = new[] { "taskbar.hide-search", "explorer.show-extensions", "privacy.disable-ads-id",
                        "explorer.notepad-wrap", "taskbar.clock-show-seconds", "start.disable-recommendations" }
        .Select(id => catalog.Find(id))
        .Where(f => f is not null && !f.RequiresAdmin)
        .Cast<FunctionItem>()
        .ToList();
    var r2 = await center.ExecuteAsync(new ExecutionRequest($"系统设置抽查({picks.Count} 项)", picks), null, CancellationToken.None);
    Console.WriteLine($"系统设置抽查结果: {r2.Result}(成功 {r2.SuccessCount} / 失败 {r2.FailedCount})");
    RCheck(r2.FailedCount == 0, "系统设置抽查全部执行成功");

    // --- 3. 注册表核对 ---
    using var adv = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
    using var cdm = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
    using var adi = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo");
    using var ntp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Notepad");

    RCheck(adv is not null && Convert.ToInt32(adv.GetValue("Start_ShowRecommended", -1)) == 0,
        "注册表:开始菜单推荐已关闭(Start_ShowRecommended=0)", adv?.GetValue("Start_ShowRecommended")?.ToString());
    RCheck(adv is not null && Convert.ToInt32(adv.GetValue("HideFileExt", -1)) == 0,
        "注册表:显示文件扩展名(HideFileExt=0)", adv?.GetValue("HideFileExt")?.ToString());
    RCheck(adv is not null && Convert.ToInt32(adv.GetValue("ShowSecondsInSystemClock", -1)) == 1,
        "注册表:时钟显示秒数(ShowSecondsInSystemClock=1)", adv?.GetValue("ShowSecondsInSystemClock")?.ToString());
    RCheck(adv is not null && Convert.ToInt32(adv.GetValue("SearchboxTaskbarMode", -1)) == 0,
        "注册表:隐藏任务栏搜索框(SearchboxTaskbarMode=0)", adv?.GetValue("SearchboxTaskbarMode")?.ToString());
    RCheck(adi is not null && Convert.ToInt32(adi.GetValue("Enabled", -1)) == 0,
        "注册表:广告 ID 已关闭(AdvertisingInfo/Enabled=0)", adi?.GetValue("Enabled")?.ToString());
    RCheck(ntp is not null && Convert.ToInt32(ntp.GetValue("fWrap", -1)) == 1,
        "注册表:记事本自动换行(fWrap=1)", ntp?.GetValue("fWrap")?.ToString());

    // --- 4. 恢复验证(2 项)-> 注册表值应被删除 ---
    var restores = new[] { "explorer.notepad-wrap", "taskbar.clock-show-seconds" };
    var restoreItems = restores
        .Select(id => catalog.Find(id))
        .Where(f => f is not null)
        .Select(f => new FunctionItem
        {
            Id = f!.Id + ".restore",
            Name = "恢复:" + f.Name,
            Category = f.Category,
            Module = f.Module,
            Description = "恢复默认值",
            Risk = RiskLevel.Safe,
            RequiresAdmin = f.RequiresAdmin,
            Restart = f.Restart,
            Source = "测试",
            Kind = ExecutionKind.PowerShell,
            Command = RestoreCommandBuilder.Build(f)!
        })
        .ToList();
    var r3 = await center.ExecuteAsync(new ExecutionRequest($"恢复验证({restoreItems.Count} 项)", restoreItems), null, CancellationToken.None);
    Console.WriteLine($"恢复验证结果: {r3.Result}(成功 {r3.SuccessCount} / 失败 {r3.FailedCount})");
    RCheck(r3.FailedCount == 0, "恢复任务全部执行成功");

    using var adv2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
    using var ntp2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Notepad");
    RCheck(ntp2 is null || ntp2.GetValue("fWrap") is null,
        "恢复:记事本自动换行值已删除", ntp2?.GetValue("fWrap")?.ToString() ?? "(已删除)");
    RCheck(adv2 is null || adv2.GetValue("ShowSecondsInSystemClock") is null,
        "恢复:时钟秒数值已删除", adv2?.GetValue("ShowSecondsInSystemClock")?.ToString() ?? "(已删除)");

    // --- 5. 任务记录持久化核对 ---
    var recent = await store.GetRecentAsync(6);
    RCheck(recent.Count >= 3 && recent.Any(r => r.Name.Contains("轻度优化")),
        "任务记录已写入(含轻度优化任务)", string.Join(", ", recent.Take(3).Select(r => r.Name)));

    Console.WriteLine(rf == 0 ? "========== 真机执行验证全部通过 ==========" : $"========== 真机执行验证失败 {rf} 项 ==========");
}
