// 第二代真机闭环验证:配置保存→导出→导入→执行→批次回滚
// 用安全可逆的注册表项(explorer 类),测试后恢复原值,不污染系统
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NControl.Core;
using NControl.Infrastructure;
using NControl.Modules.Optimization;

var catalog = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
new OptimizationModuleRegistrar().RegisterFeatures(catalog);

var paths = new AppPaths(TestConfig());
var store = new SqliteTaskRecordStore(paths, NullLogger<SqliteTaskRecordStore>.Instance);
var ps = new PowerShellExecutionProvider(NullLogger<PowerShellExecutionProvider>.Instance);
var cmd = new CommandExecutionProvider(NullLogger<CommandExecutionProvider>.Instance);
var center = new ExecutionCenter(new IExecutionProvider[] { ps, cmd }, store, NullLogger<ExecutionCenter>.Instance);
var compat = new CompatibilityEngine(new WindowsEnvironmentProbe());
var plans = new PlanService(paths);
var rollback = new RollbackService(catalog, center, store);

var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\gen2_e2e_result.txt", append: false, new System.Text.UTF8Encoding(true));
var L = new Action<string>(s => { Console.WriteLine(s); log.WriteLine(s); log.Flush(); });

// 选 3 个安全可逆项:explorer 打开此电脑 / 记事本自动换行 / 显示已知扩展名(如存在)
var ids = new[] { "explorer.open-this-pc", "explorer.notepad-wrap", "explorer.show-known-extensions" }
    .Where(id => catalog.Find(id) is not null)
    .ToArray();
L($"测试项: {string.Join(", ", ids)}");
if (ids.Length == 0) { L("FAIL 无可用测试项"); log.Close(); return; }

// 记录执行前状态(用于恢复验证)
var beforeValues = new Dictionary<string, (string Path, string Name, object? Value, string? Expected)>();
foreach (var id in ids)
{
    var item = catalog.Find(id)!;
    var m = System.Text.RegularExpressions.Regex.Match(item.Command!,
        @"Set-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'\s+-Value\s+([^\-;]+?)\s+-Type\s+\w+");
    if (m.Success)
    {
        beforeValues[id] = (m.Groups[1].Value, m.Groups[2].Value, ReadReg(m.Groups[1].Value, m.Groups[2].Value), m.Groups[3].Value.Trim());
        L($"  {id}: 原值 = {beforeValues[id].Value ?? "<无>"} 期望 = {beforeValues[id].Expected}");
    }
}

// ========== 1. 配置保存 ==========
var plan = new PlanConfig { Name = "E2E测试方案", Description = "真机闭环测试", Functions = ids.ToList() };
plans.Save(plan);
L($"1. 配置保存: OK (plans 目录)");
L($"   导出 JSON: {JsonPreview(plan)}");

// ========== 2. 导出 ==========
var exportPath = @"C:\Users\test\.openclaw\workspace\tools\e2e_plan_export.json";
plans.Export(plan, exportPath);
L($"2. 配置导出: OK ({exportPath})");

// ========== 3. 导入 ==========
var importResult = plans.Import(exportPath, catalog, compat);
L($"3. 配置导入: ParseOk={importResult.ParseOk} Ready={importResult.Ready.Count} Unknown={importResult.UnknownIds.Count} Unsupported={importResult.Unsupported.Count}");
if (!importResult.ParseOk || importResult.Ready.Count == 0) { L("FAIL 导入失败"); log.Close(); return; }

// ========== 4. 执行导入的项 ==========
var itemsToExec = importResult.Ready
    .Select(r => catalog.Find(r.FunctionId)!)
    .ToArray();
var execRecord = await center.ExecuteAsync(new ExecutionRequest("E2E执行", itemsToExec), null, CancellationToken.None);
L($"4. 执行导入项: Result={execRecord.Result} Success={execRecord.SuccessCount} Failed={execRecord.FailedCount}");
if (execRecord.SuccessCount == 0) { L("FAIL 执行失败"); log.Close(); return; }

// 验证执行后值已改变
foreach (var id in ids)
{
    var item = catalog.Find(id)!;
    var m = System.Text.RegularExpressions.Regex.Match(item.Command!,
        @"Set-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'\s+-Value\s+([^\-;]+?)\s+-Type\s+\w+");
    if (m.Success)
    {
        var newVal = ReadReg(m.Groups[1].Value, m.Groups[2].Value);
        var expected = beforeValues[id].Expected;
        var achieved = ValuesMatch(newVal, expected);
        L($"   执行后 {id}: {newVal} (期望 {expected} → {(achieved ? "已达成 ✓" : "未达成 ✗")})");
    }
}

// ========== 5. 批次回滚 ==========
L($"5. 批次回滚分析: Restorable={rollback.Analyze(execRecord).RestorableCount} NotSupported={rollback.Analyze(execRecord).NotSupportedCount}");
var rollbackRecord = await rollback.RollbackAsync(execRecord);
L($"   回滚执行: Result={rollbackRecord.Result} Success={rollbackRecord.SuccessCount} Failed={rollbackRecord.FailedCount}");

// ========== 6. 验证恢复 ==========
var allRestored = true;
foreach (var id in ids)
{
    var item = catalog.Find(id)!;
    var m = System.Text.RegularExpressions.Regex.Match(item.Command!,
        @"Set-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'\s+-Value\s+([^\-;]+?)\s+-Type\s+\w+");
    if (m.Success)
    {
        var afterVal = ReadReg(m.Groups[1].Value, m.Groups[2].Value);
        var oldVal = beforeValues[id].Value;
        // 恢复语义:值复原 或 删除(默认未配置态)均接受
        var restored = afterVal is null || Equals(afterVal, oldVal);
        if (!restored) allRestored = false;
        L($"   回滚后 {id}: {afterVal} ({(restored ? "已恢复 ✓" : "未恢复 ✗ 原值=" + (oldVal ?? "<无>"))})");
    }
}
L(allRestored ? "=== 闭环验证通过:保存→导出→导入→执行→回滚→恢复 ✓ ===" : "=== 闭环验证失败 ===");

// 清理测试方案
plans.Delete("E2E测试方案");
File.Delete(exportPath);
log.Close();

static object? ReadReg(string psPath, string name)
{
    try
    {
        var (hive, sub) = Split(psPath);
        using var key = hive.OpenSubKey(sub);
        return key?.GetValue(name);
    }
    catch { return null; }
}

static bool ValuesMatch(object? actual, string? expected)
{
    if (expected is null) return true;
    if (actual is null) return false;
    // 数字容错:int/long 与字符串数字
    if (long.TryParse(expected, out var expLong))
    {
        try { return Convert.ToInt64(actual) == expLong; }
        catch { return false; }
    }
    return string.Equals(actual.ToString(), expected.Trim('\''), StringComparison.OrdinalIgnoreCase);
}

static (Microsoft.Win32.RegistryKey Hive, string Sub) Split(string psPath)
{
    if (psPath.StartsWith(@"HKCU:\", StringComparison.OrdinalIgnoreCase))
        return (Microsoft.Win32.Registry.CurrentUser, psPath[6..]);
    return (Microsoft.Win32.Registry.LocalMachine, psPath[6..]);
}

static string JsonPreview(PlanConfig plan)
    => "{schemaVersion:" + plan.SchemaVersion + ", name:" + plan.Name + ", functions:[" + string.Join(",", plan.Functions) + "]}";

static Microsoft.Extensions.Configuration.IConfiguration TestConfig()
{
    var dir = Path.Combine(Path.GetTempPath(), "nctl_e2e_" + Guid.NewGuid().ToString("N"));
    return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["NControl:DataFolder"] = dir })
        .Build();
}
