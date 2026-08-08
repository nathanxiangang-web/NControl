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

if (args.Contains("--security-center-preflight") || args.Contains("--execute-security-center"))
{
    var execute = args.Contains("--execute-security-center");
    if (execute && !args.Contains("--confirm-irrevocable"))
    {
        Console.Error.WriteLine("拒绝执行:缺少 --confirm-irrevocable 双重确认参数。");
        Environment.ExitCode = 64;
        return;
    }

    Environment.ExitCode = await SecurityCenterProbeAsync(execute);
    return;
}

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

if (args.Contains("--detect"))
{
    // 状态检测验证
    var dSw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "detect_result.txt"), append: false, new UTF8Encoding(true));
    var dlog = new Action<string>(s => { Console.WriteLine(s); dSw.WriteLine(s); dSw.Flush(); });
    var dCat = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
    new OptimizationModuleRegistrar().RegisterFeatures(dCat);

    dlog("=== 已优化状态检测(当前系统) ===");
    foreach (var id in new[] { "taskbar.hide-search", "explorer.remove-duplicate-drives" })
    {
        var item = dCat.Find(id);
        if (item is null) continue;
        var s = StateDetector.Detect(item);
        dlog($"{item.Name}: {(s == true ? "已优化✅" : s == false ? "未优化" : "不可检测")}");
    }
    dlog("=== 覆盖率统计 ===");
    var dAll = dCat.ByModule(ModuleKind.Optimization).ToArray();
    var detectable = dAll.Count(f => StateDetector.Detect(f) is not null);
    var optimized = dAll.Count(f => StateDetector.Detect(f) == true);
    dlog($"可检测: {detectable}/{dAll.Length}  当前已优化: {optimized}");
    dlog("=== 已优化项明细 ===");
    foreach (var f in dAll.Where(f => StateDetector.Detect(f) == true))
        dlog($"  [{f.Category}] {f.Name}");
    dSw.Close();
    return;
}

if (args.Contains("--cleanup")){
    // 清理全量测试残留:删除测试写入的键值,恢复系统默认(之前恢复逻辑静默失败留下的)
    var cleanupSw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "cleanup_result.txt"), append: false, new UTF8Encoding(true));
    var clog = new Action<string>(s => { Console.WriteLine(s); cleanupSw.WriteLine(s); cleanupSw.Flush(); });
    var cleanItems = new (string Path, string Name)[]
    {
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AutoInstallMinorUpdates"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\MRT", "DontOfferThroughWUAU"),
        (@"HKLM:\SYSTEM\CurrentControlSet\Control", "PortableOperatingSystem"),
        (@"HKCU:\Software\Microsoft\Notepad", "ShowStoreBanner"),
        (@"HKCU:\Control Panel\Desktop", "AutoEndTasks"),
        (@"HKCU:\Control Panel\Desktop", "HungAppTimeout"),
        (@"HKCU:\Control Panel\Desktop", "WaitToKillAppTimeout"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DisableNetFrameworkTelemetry"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "BingSearchEnabled"),
        (@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "DisablePreInstalledApps"),
        (@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer", "AllowPagePrediction"),
        (@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray"),
        (@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowFrequent"),
        (@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowRecent"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableThirdPartySuggestions"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice"),
        (@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableAutomaticRestartSignOn"),
        (@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\WcmSvc\Local", "fBlockNonDomain")
    };
    foreach (var (path, name) in cleanItems)
    {
        var script = $"Remove-ItemProperty -Path '{path}' -Name '{name}' -ErrorAction SilentlyContinue; exit 0";
        var ok = await RunPsSimpleAsync(script);
        clog($"{(ok ? "OK" : "FAIL")} {path}\\{name}");
    }
    clog("DONE");
    cleanupSw.Close();
    return;
}

// 高风险项: 不真机执行,只验证命令/恢复命令存在
var highRiskIds = new HashSet<string>
{
    "advanced.disable-uac-notifications", "advanced.admin-filter-token", "advanced.admin-enable-lua",
    "advanced.secure-uia-paths", "advanced.uia-nonsecure-desktop",
    "advanced.disable-smartscreen", "advanced.disable-open-security-warning",
    "advanced.disable-firewall", "advanced.disable-memory-integrity", "advanced.disable-vbs",
    "advanced.disable-security-center",
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
            var intentionallyIrreversible = item.Id == "advanced.disable-security-center";
            var ok = !string.IsNullOrWhiteSpace(item.Command)
                     && (intentionallyIrreversible ? string.IsNullOrWhiteSpace(restore) : !string.IsNullOrWhiteSpace(restore));
            if (ok) { pass++; log($"[PASS] {tag} (高风险:只做静态验证{(intentionallyIrreversible ? ",已标记不可恢复" : "+恢复命令")})"); }
            else { fail++; failures.Add($"{tag}: 高风险项命令/恢复策略与预期不符"); log($"[FAIL] {tag}"); }
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
    var svcBackup = new List<(string Name, string? StartType, bool Existed, bool WasRunning)>();
    foreach (var (name, _) in svcOps.Distinct())
    {
        var svc = GetService(name);
        svcBackup.Add((name, svc.StartType, svc.Exists, svc.WasRunning));
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

    // 6) 恢复:优先用 RestoreCommandBuilder 生成的命令(与 App 恢复按钮一致),再按备份写回
    var restoreNotes = new List<string>();
    var restoreCmd = RestoreCommandBuilder.Build(item);
    if (!string.IsNullOrWhiteSpace(restoreCmd))
    {
        try
        {
            var rr = await provider.ExecuteAsync(
                new FunctionItem
                {
                    Id = item.Id + ".restore", Name = "恢复:" + item.Name, Category = item.Category,
                    Module = ModuleKind.Tools, Description = "", Risk = RiskLevel.Safe,
                    RequiresAdmin = item.RequiresAdmin, Restart = RestartRequirement.None,
                    Source = "测试", Kind = ExecutionKind.PowerShell, Command = restoreCmd
                }, null, CancellationToken.None);
            restoreNotes.Add(rr.Success ? "恢复✓" : $"恢复✗({rr.Error ?? "?"})");
        }
        catch (Exception ex) { restoreNotes.Add($"恢复✗({ex.Message})"); }
    }
    else
    {
        // 无恢复命令时按备份写回(仅注册表写入点)
        foreach (var (path, name, existed, value, type) in backup)
        {
            try
            {
                if (existed)
                {
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
    }

    // 7) 恢复后验证:值不应再处于"优化写入后的状态"(允许:已删除=恢复默认,或写回原值)
    var restoreOk = true;
    foreach (var (path, name, existed, value, _) in backup)
    {
        var after = GetRegValue(path, name);
        if (existed)
        {
            // 备份时值存在:恢复后应等于原值,或被删除(删除=恢复系统默认,同样可接受)
            if (after.Exists && after.Value?.ToString() != value?.ToString())
            { restoreOk = false; restoreNotes.Add($"{name}未复原({after.Value})"); }
        }
        else
        {
            // 备份时值不存在:恢复后应不存在
            if (after.Exists) { restoreOk = false; restoreNotes.Add($"{name}仍残留"); }
        }
    }
    foreach (var (name, startType, existed, wasRunning) in svcBackup)
    {
        try
        {
            if (existed && !string.IsNullOrEmpty(startType))
            {
                await RunPsAsync($"Set-Service {name} -StartupType {startType}");
                if (wasRunning)
                    await RunPsAsync($"Start-Service {name} -ErrorAction SilentlyContinue");
            }
            restoreNotes.Add($"svc:{name}✓");
        }
        catch { restoreNotes.Add($"svc:{name}✗"); }
    }

    var restoreSummary = string.Join(",", restoreNotes);
    if (!restoreOk)
        return (false, false, $"写入验证OK但恢复未复原: {restoreSummary}");
    return (true, false, $"写入验证OK,恢复OK({restoreSummary})");
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

static (string? StartType, bool Exists, bool WasRunning) GetService(string name)
{
    var script = $"try {{ $s=Get-Service {name} -ErrorAction Stop; [Console]::WriteLine($s.StartType + '|' + $s.Status) }} catch {{ [Console]::WriteLine('MISSING') }}";
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
    if (outp == "MISSING") return (null, false, false);
    var parts = outp.Split('|');
    return (parts.Length > 0 ? parts[0] : null, true, parts.Length > 1 && parts[1] == "Running");
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

    foreach (var id in new[] { "privacy.disable-sms-router", "perf.disable-remote-registry", "perf.disable-homegroup" })
    {
        var item = catalog.Find(id)!;
        log($"=== {item.Name} ===");
        // 打印注入后的最终脚本
        var script = "[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; " + item.Command!;
        var reWrite = new System.Text.RegularExpressions.Regex(@"Set-ItemProperty\s+-Path\s+'([^']+)'");
        script = reWrite.Replace(script, m => $"if (-not (Test-Path '{m.Groups[1].Value}')) {{ New-Item -Path '{m.Groups[1].Value}' -Force | Out-Null }}; Set-ItemProperty -Path '{m.Groups[1].Value}'");
        var reSvc = new System.Text.RegularExpressions.Regex(@"(Set-Service|Stop-Service)\s+([A-Za-z0-9_]+)([^;]*)");
        script = reSvc.Replace(script, m => $"if (Get-Service {m.Groups[2].Value} -ErrorAction SilentlyContinue) {{ {m.Groups[1].Value} {m.Groups[2].Value}{m.Groups[3].Value} }}");
        log("注入后: " + script);
        log("");
    }
    sw.Close();
}

// ---------- 安全中心专用调试入口:默认只读,破坏性执行需要双重参数 ----------
static async Task<int> SecurityCenterProbeAsync(bool execute)
{
    var fileName = execute ? "security_center_execute.txt" : "security_center_preflight.txt";
    using var sw = new StreamWriter(Path.Combine(AppContext.BaseDirectory, fileName), append: false, new UTF8Encoding(true));
    var log = new Action<string>(s => { Console.WriteLine(s); sw.WriteLine(s); sw.Flush(); });
    var catalog = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
    new OptimizationModuleRegistrar().RegisterFeatures(catalog);
    var item = catalog.Find("advanced.disable-security-center");
    if (item is null)
    {
        log("FAIL: 功能 advanced.disable-security-center 未注册。");
        return 2;
    }

    var isAdmin = new System.Security.Principal.WindowsPrincipal(
        System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(
        System.Security.Principal.WindowsBuiltInRole.Administrator);
    log($"Mode={(execute ? "EXECUTE" : "PREFLIGHT")}");
    log($"BaseDirectory={AppContext.BaseDirectory}");
    log($"IsAdmin={isAdmin}");
    log($"Feature={item.Id}|Risk={item.Risk}|Restart={item.Restart}|HasRestore={!string.IsNullOrWhiteSpace(item.RestoreCommand)}");

    var payloadRoot = Path.Combine(AppContext.BaseDirectory, "Tools", "SecurityCenter");
    var required = new[]
    {
        "SuperUser32.exe", "SuperUser64.exe", "KILLSECURITYCENTER.CMD", "DEFENDER.CMD",
        "WINDOWS DEFENDER CACHE MAINTENANCE.XML", "WINDOWS DEFENDER CLEANUP.XML",
        "WINDOWS DEFENDER SCHEDULED SCAN.XML", "WINDOWS DEFENDER VERIFICATION.XML"
    };
    foreach (var name in required)
    {
        var path = Path.Combine(payloadRoot, name);
        log($"Payload={name}|Exists={File.Exists(path)}" +
            (File.Exists(path) ? $"|SHA256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)))}" : ""));
    }

    var provider = new PowerShellExecutionProvider(NullLogger<PowerShellExecutionProvider>.Instance);
    var stateProbe = new FunctionItem
    {
        Id = "security-center.state-probe", Name = "安全中心状态探针", Category = "测试", Module = ModuleKind.Tools,
        Description = "只读检查服务、驱动和篡改保护状态。", Risk = RiskLevel.Safe, RequiresAdmin = false,
        Restart = RestartRequirement.None, Source = "测试", Kind = ExecutionKind.PowerShell,
        Command = "Write-Output ('NCONTROL_APP_BASE=' + $env:NCONTROL_APP_BASE); " +
                  "$names=@('WinDefend','wscsvc','WdNisSvc','SecurityHealthService','WdFilter','WdBoot','wdnisdrv'); " +
                  "foreach($n in $names){$svc=Get-Service -Name $n -ErrorAction SilentlyContinue; " +
                  "$start=(Get-ItemProperty -LiteralPath ('HKLM:\\SYSTEM\\CurrentControlSet\\Services\\'+$n) -Name Start -ErrorAction SilentlyContinue).Start; " +
                  "Write-Output ($n+'|Status='+$(if($svc){$svc.Status}else{'Missing'})+'|Start='+$start)}; " +
                  "$tamper=(Get-ItemProperty -LiteralPath 'HKLM:\\SOFTWARE\\Microsoft\\Windows Defender\\Features' -Name TamperProtection -ErrorAction SilentlyContinue).TamperProtection; " +
                  "Write-Output ('TamperProtectionRegistry='+$tamper); " +
                  "try{$mp=Get-MpComputerStatus -ErrorAction Stop; Write-Output ('IsTamperProtected='+$mp.IsTamperProtected); Write-Output ('AntivirusEnabled='+$mp.AntivirusEnabled); Write-Output ('RealTimeProtectionEnabled='+$mp.RealTimeProtectionEnabled)}catch{Write-Output ('GetMpStatusError='+$_.Exception.Message)}; exit 0"
    };

    var before = await provider.ExecuteAsync(stateProbe, line => log("STATE> " + line), CancellationToken.None);
    log($"PreflightSuccess={before.Success}|Exit={before.ExitCode}|Error={before.Error}");
    if (!before.Success || before.Output?.Contains("NCONTROL_APP_BASE=" + AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase) != true)
    {
        log("FAIL: 只读前置检查失败,或子 PowerShell 未获得真实程序目录。");
        return 3;
    }

    if (!execute)
    {
        log("PASS: 只读前置检查完成,未修改系统。");
        return 0;
    }

    if (!isAdmin)
    {
        log("FAIL: 破坏性执行需要管理员权限。");
        return 5;
    }

    log("EXECUTE: 开始调用产品中的不可恢复功能。");
    var result = await provider.ExecuteAsync(item, line => log("RUN> " + line), CancellationToken.None);
    log($"ExecuteSuccess={result.Success}|Exit={result.ExitCode}|Error={result.Error}");
    if (!string.IsNullOrWhiteSpace(result.Output)) log("Output=" + result.Output.Trim());
    return result.Success ? 0 : 10;
}



// ---------- 简单 PowerShell 执行(清理用) ----------
static async Task<bool> RunPsSimpleAsync(string script)
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
    psi.ArgumentList.Add("-NoProfile");
    psi.ArgumentList.Add("-NonInteractive");
    psi.ArgumentList.Add("-Command");
    psi.ArgumentList.Add("[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; " + script);
    using var p = Process.Start(psi)!;
    await p.WaitForExitAsync();
    return p.ExitCode == 0;
}
