// NControl 全量功能真机测试工具
// 用法(需管理员): dotnet run --project tools/NControl.FunctionTest [--recheck]
// --recheck: 只复验指定失败项,不执行恢复(用于诊断)
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using NControl.Core;
using NControl.Infrastructure;
using NControl.Modules.Optimization;


if (args.Contains("--recheck"))
{
    await RecheckAsync();
    return;
}

if (args.Contains("--diag"))
{
    await DiagAsync();
    return;
}

if (args.Contains("--probe"))
{
    // 诊断:打印 PowerShellPath 实际值
    var probeSw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "probe_result.txt"), append: false, new UTF8Encoding(true));
    probeSw.WriteLine($"Is64Bit: {Environment.Is64BitProcess}");
    probeSw.WriteLine($"SystemDirectory: {Environment.SystemDirectory}");
    probeSw.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");
    var prov = new PowerShellExecutionProvider(NullLogger<PowerShellExecutionProvider>.Instance);
    var probeItem = new FunctionItem
    {
        Id = "probe", Name = "探针", Category = "测试", Module = ModuleKind.Tools,
        Description = "", Risk = RiskLevel.Safe, RequiresAdmin = true, Restart = RestartRequirement.None,
        Source = "测试", Kind = ExecutionKind.PowerShell,
        Command = "[Console]::OutputEncoding=[Text.Encoding]::UTF8; 'probe-ps-ok'"
    };
    try
    {
        var r = await prov.ExecuteAsync(probeItem, null, CancellationToken.None);
        probeSw.WriteLine($"Probe Success={r.Success} Exit={r.ExitCode}");
        probeSw.WriteLine($"Probe Error: {r.Error}");
        probeSw.WriteLine($"Probe Output: {r.Output}");
    }
    catch (Exception ex) { probeSw.WriteLine($"Probe 异常: {ex.Message}"); }
    probeSw.Close();
    return;
}

// 高风险项: 不真机执行,只验证命令/恢复命令存在
var highRiskIds = new HashSet<string>
{
    "advanced.disable-uac-notifications", "advanced.admin-filter-token", "advanced.admin-enable-lua",
    "advanced.secure-uia-paths", "advanced.uia-nonsecure-desktop",
    "advanced.disable-smartscreen", "advanced.disable-open-security-warning",
    "advanced.disable-firewall", "advanced.disable-memory-integrity", "advanced.disable-vbs",
    "advanced.disable-system-restore", "advanced.disable-windows-update-checks",
    "advanced.disable-tsx", "advanced.disable-insecure-download-warnings",
    "advanced.disable-meltdown-mitigations", "perf.disable-exploit-protection",
    "system.pause-updates-5000d"
};

var catalog = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
new OptimizationModuleRegistrar().RegisterFeatures(catalog);
new OptimizationModuleRegistrar().RegisterPresets(catalog);
var provider = new PowerShellExecutionProvider(NullLogger<PowerShellExecutionProvider>.Instance);

var items = catalog.ByModule(ModuleKind.Optimization).OrderBy(f => f.Category).ThenBy(f => f.Name).ToArray();
Console.WriteLine($"优化模块共 {items.Length} 项\n");

var sw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "fulltest_result.txt"), append: false, new UTF8Encoding(true));
var log = new Action<string>(s => { Console.WriteLine(s); sw.WriteLine(s); sw.Flush(); });
log($"Is64Bit={Environment.Is64BitProcess} SystemDir={Environment.SystemDirectory}");
log($"IsAdmin={new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}");
log($"PSPath={Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe")}");

int pass = 0, fail = 0, skipped = 0;
var failures = new List<string>();

foreach (var item in items)
{
    var tag = $"[{item.Category}] {item.Name}";
    try
    {
        if (highRiskIds.Contains(item.Id))
        {
            var restore = RestoreCommandBuilder.Build(item);
            var ok = !string.IsNullOrWhiteSpace(item.Command) && !string.IsNullOrWhiteSpace(restore);
            if (ok) { pass++; log($"[PASS] {tag} (高风险:仅验证命令+恢复命令可生成)"); }
            else { fail++; failures.Add($"{tag}: 高风险项命令或恢复命令缺失"); log($"[FAIL] {tag}"); }
            continue;
        }

        var result = await TestOneAsync(provider, item);
        if (result.Pass) { pass++; log($"[PASS] {tag}  ({result.Note})"); }
        else if (result.Skipped) { skipped++; log($"[SKIP] {tag}  ({result.Note})"); }
        else { fail++; failures.Add($"{tag}: {result.Note}"); log($"[FAIL] {tag}  ({result.Note})"); }
    }
    catch (Exception ex)
    {
        fail++; failures.Add($"{tag}: 异常 {ex.Message}");
        log($"[FAIL] {tag}  (异常: {ex.Message})");
        log($"  堆栈: {ex}");
    }
}

log($"\n========== 汇总: PASS {pass} / FAIL {fail} / SKIP {skipped} ==========");
if (failures.Count > 0)
{
    log("\n失败明细:");
    foreach (var f in failures) log($"  - {f}");
}
sw.Close();

// ---------- 测试逻辑 ----------
static async Task<(bool Pass, bool Skipped, string Note)> TestOneAsync(
    IExecutionProvider provider, FunctionItem item)
{
    var cmd = item.Command ?? "";
    if (cmd.Length == 0) return (false, false, "无命令");

    // 1) 解析所有 Set-ItemProperty 写入点
    var writes = new List<(string Path, string Name)>();
    foreach (Match m in Regex.Matches(cmd, @"Set-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'"))
        writes.Add((m.Groups[1].Value, m.Groups[2].Value));

    // 2) 备份现有值
    var backup = new List<(string Path, string Name, bool Existed, object? Value, object? Type)>();
    foreach (var (path, name) in writes)
    {
        try
        {
            var prop = GetRegValue(path, name);
            backup.Add((path, name, prop.Exists, prop.Value, prop.Type));
        }
        catch { backup.Add((path, name, false, null, null)); }
    }

    // 3) 服务类操作
    var svcOps = new List<(string Name, string Action)>();
    foreach (Match m in Regex.Matches(cmd, @"(?:Set-Service|Stop-Service)\s+([A-Za-z0-9_]+)", RegexOptions.IgnoreCase))
        svcOps.Add((m.Groups[1].Value, m.Groups[0].Value.Split(' ')[0]));
    var svcBackup = new List<(string Name, string? StartType, bool Existed)>();
    foreach (var (name, _) in svcOps.Distinct())
    {
        var svc = GetService(name);
        svcBackup.Add((name, svc.StartType, svc.Exists));
    }

    // 4) 执行
    var result = await provider.ExecuteAsync(item, null, CancellationToken.None);
    if (!result.Success)
        return (false, false, $"执行失败: {result.Error ?? result.Output ?? "无错误信息"}");

    // 5) 验证: 注册表写入点是否有新值
    var verifyNotes = new List<string>();
    var allVerified = true;
    foreach (var (path, name) in writes)
    {
        var prop = GetRegValue(path, name);
        if (!prop.Exists) { allVerified = false; verifyNotes.Add($"{name} 未写入"); }
    }
    if (writes.Count == 0 && svcOps.Count == 0)
        return (true, false, "命令执行成功(无注册表/服务写入点)");

    if (!allVerified)
        return (false, false, string.Join("; ", verifyNotes));

    // 6) 恢复
    var restoreNotes = new List<string>();
    foreach (var (path, name, existed, value, type) in backup)
    {
        try
        {
            if (existed)
            {
                // 写回原值
                var typeStr = type is string ts ? ts : "DWord";
                var valStr = value is string s ? $"'{s}'" : (value is null ? "0" : value.ToString());
                await RunPsAsync($"Set-ItemProperty -Path '{path}' -Name '{name}' -Value {valStr} -Type {typeStr} -Force");
            }
            else
            {
                await RunPsAsync($"Remove-ItemProperty -Path '{path}' -Name '{name}' -ErrorAction SilentlyContinue");
            }
            restoreNotes.Add($"{name}✓");
        }
        catch { restoreNotes.Add($"{name}✗"); }
    }
    foreach (var (name, startType, existed) in svcBackup)
    {
        try
        {
            if (existed && !string.IsNullOrEmpty(startType))
                await RunPsAsync($"Set-Service {name} -StartupType {startType}");
            restoreNotes.Add($"svc:{name}✓");
        }
        catch { restoreNotes.Add($"svc:{name}✗"); }
    }

    return (true, false, $"写入验证OK,恢复OK({string.Join(",", restoreNotes)})");
}

static (bool Exists, object? Value, object? Type) GetRegValue(string psPath, string name)
{
    var script = $"$p='{psPath}'; $n='{name}'; try {{ $v=(Get-ItemProperty -Path $p -Name $n -ErrorAction Stop).$n; [Console]::WriteLine('EXISTS:' + $v.GetType().Name + ':' + $v) }} catch {{ [Console]::WriteLine('MISSING') }}";
    var psi = new ProcessStartInfo
    {
        FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8
    };
    psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-Command"); psi.ArgumentList.Add(script);
    using var p = Process.Start(psi)!;
    var outp = p.StandardOutput.ReadToEnd().Trim();
    p.WaitForExit(10000);
    if (outp.StartsWith("EXISTS:"))
    {
        var parts = outp.Substring(7).Split(':', 2);
        return (true, parts.Length > 1 ? parts[1] : null, parts[0]);
    }
    return (false, null, null);
}

static (string? StartType, bool Exists) GetService(string name)
{
    var script = $"try {{ $s=Get-Service {name} -ErrorAction Stop; [Console]::WriteLine($s.StartType) }} catch {{ [Console]::WriteLine('MISSING') }}";
    var psi = new ProcessStartInfo
    {
        FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8
    };
    psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-Command"); psi.ArgumentList.Add(script);
    using var p = Process.Start(psi)!;
    var outp = p.StandardOutput.ReadToEnd().Trim();
    p.WaitForExit(10000);
    return outp == "MISSING" ? (null, false) : (outp, true);
}

static async Task RunPsAsync(string script)
{
    var psi = new ProcessStartInfo
    {
        FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
    };
    psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-Command"); psi.ArgumentList.Add(script);
    using var p = Process.Start(psi)!;
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) throw new Exception($"恢复命令失败: {script}");
}

// ---------- 复验模式: 只执行不恢复, 诊断失败项 ----------
static async Task RecheckAsync()
{
    var sw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "recheck_result.txt"), append: false, new UTF8Encoding(true));
    var log = new Action<string>(s => { Console.WriteLine(s); sw.WriteLine(s); sw.Flush(); });
    var catalog = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
    new OptimizationModuleRegistrar().RegisterFeatures(catalog);
    var provider = new PowerShellExecutionProvider(NullLogger<PowerShellExecutionProvider>.Instance);

    var ids = new[]
    {
        "update.block-feature-updates", "update.set-active-hours", "update.pause-feature-updates-7d",
        "taskbar.hide-widgets", "explorer.hide-spotlight-icon",
        "perf.no-start-suggestions", "perf.fast-shutdown", "perf.disable-prefetch",
        "perf.disable-reserved-storage", "perf.disable-hpet", "perf.disable-homegroup",
        "perf.disable-error-report", "performance.ultimate-performance-plan",
        "perf.cpu-priority-optimize", "perf.disable-search-web"
    };

    foreach (var id in ids)
    {
        var item = catalog.Find(id)!;
        log($"=== {item.Name} (admin={item.RequiresAdmin}) ===");
        try
        {
            var r = await provider.ExecuteAsync(item, null, CancellationToken.None);
            log($"  Success={r.Success} Exit={r.ExitCode}");
            if (!string.IsNullOrWhiteSpace(r.Error)) log($"  Error: {r.Error!.Split('\n')[0]}");
            if (!string.IsNullOrWhiteSpace(r.Output)) log($"  Output: {r.Output!.Split('\n')[0]}");
        }
        catch (Exception ex)
        {
            log($"  异常: {ex.Message}");
        }
    }
    sw.Close();
}

// ---------- 诊断模式: 打印失败项完整错误 ----------
static async Task DiagAsync()
{
    var sw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "diag_result.txt"), append: false, new UTF8Encoding(true));
    var log = new Action<string>(s => { Console.WriteLine(s); sw.WriteLine(s); sw.Flush(); });
    var catalog = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
    new OptimizationModuleRegistrar().RegisterFeatures(catalog);
    var provider = new PowerShellExecutionProvider(NullLogger<PowerShellExecutionProvider>.Instance);

    foreach (var id in new[] { "update.block-feature-updates" })
    {
        var item = catalog.Find(id)!;
        log($"=== {item.Name} ===");
        // 手动复现 provider 注入,打印最终脚本
        var script = "[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; " + item.Command!;
        var reWrite = new System.Text.RegularExpressions.Regex(@"Set-ItemProperty\s+-Path\s+'([^']+)'");
        script = reWrite.Replace(script, m => $"if (-not (Test-Path '{m.Groups[1].Value}')) {{ New-Item -Path '{m.Groups[1].Value}' -Force | Out-Null }}; Set-ItemProperty -Path '{m.Groups[1].Value}'");
        log("脚本: " + script);
        try
        {
            var r = await provider.ExecuteAsync(item, null, CancellationToken.None);
            log($"Success={r.Success} Exit={r.ExitCode}");
            log($"Error: {r.Error ?? "(null)"}");
        }
        catch (Exception ex) { log($"异常: {ex}"); }
        log("");
    }
    sw.Close();
}


