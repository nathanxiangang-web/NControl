using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using NControl.Core;

namespace NControl.Infrastructure;

/// <summary>
/// PowerShell 执行提供程序。
/// 通过 powershell.exe 进程执行,支持输出实时回调。
/// 需要管理员权限的项:以提权进程执行,输出写入临时文件,完成后读取;用户取消 UAC 时返回失败。
/// </summary>
public sealed class PowerShellExecutionProvider : IExecutionProvider
{
    private static readonly System.Text.RegularExpressions.Regex RegWriteKeyPattern = new(
        @"Set-ItemProperty\s+-Path\s+'([^']+)'",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>删除键命令:Remove-Item -Path '...' [参数...]</summary>
    private static readonly System.Text.RegularExpressions.Regex RegRemoveKeyPattern = new(
        @"Remove-Item\s+-Path\s+'([^']+)'([^;]*)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>删除值命令:Remove-ItemProperty -Path '...' -Name '...' [参数...]</summary>
    private static readonly System.Text.RegularExpressions.Regex RegRemoveValuePattern = new(
        @"Remove-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'([^;]*)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>服务操作整句:Set-Service X ...; / Stop-Service X ...;(含参数,到分号为止)</summary>
    private static readonly System.Text.RegularExpressions.Regex ServiceOpPattern = new(
        @"(Set-Service|Stop-Service)\s+([A-Za-z0-9_]+)([^;]*)",
        System.Text.RegularExpressions.RegexOptions.Compiled);
    private readonly ILogger<PowerShellExecutionProvider> _logger;

    /// <summary>
    /// powershell.exe 绝对路径。管理员进程(提权后)的 PATH 可能不含
    /// System32\WindowsPowerShell\v1.0,相对名启动会报"系统找不到指定的文件"。
    /// </summary>
    private static readonly string PowerShellPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

    public PowerShellExecutionProvider(ILogger<PowerShellExecutionProvider> logger) => _logger = logger;

    public bool CanHandle(ExecutionKind kind) => kind == ExecutionKind.PowerShell;

    public async Task<ExecutionResult> ExecuteAsync(FunctionItem item, Action<string>? onOutput, CancellationToken ct)
    {
        var script = item.Command;
        if (string.IsNullOrWhiteSpace(script))
            return new ExecutionResult(false, -1, null, "功能项没有配置 PowerShell 脚本");

        // 中文系统下 powershell.exe 重定向输出默认 GBK(代码页 936);强制 UTF-8,避免中文乱码
        script = "[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; " + script;

        // 幂等注入(核心):
        // 1) 建键:Set-ItemProperty 目标键不存在时自动创建(if -not Test-Path 才建,不破坏已有键)
        // 2) 删键:Remove-Item 仅当目标键存在时执行(避免报错/退出码1)
        // 3) 删值:Remove-ItemProperty 仅当目标值存在时执行(幂等,目标不存在视为成功)
        // 全部包裹后,同一命令重复执行/恢复均无副作用,退出码恒为 0(成功路径)。
        script = RegWriteKeyPattern.Replace(script,
            m => $"if (-not (Test-Path '{m.Groups[1].Value}')) {{ New-Item -Path '{m.Groups[1].Value}' -Force | Out-Null }}; Set-ItemProperty -Path '{m.Groups[1].Value}'");
        script = RegRemoveKeyPattern.Replace(script,
            m => $"if (Test-Path '{m.Groups[1].Value}') {{ Remove-Item -Path '{m.Groups[1].Value}'{m.Groups[2].Value} }}");
        script = RegRemoveValuePattern.Replace(script,
            m => $"if (Get-ItemProperty -Path '{m.Groups[1].Value}' -Name '{m.Groups[2].Value}' -ErrorAction SilentlyContinue) {{ Remove-ItemProperty -Path '{m.Groups[1].Value}' -Name '{m.Groups[2].Value}'{m.Groups[3].Value} }}");

        // 4) 服务操作:目标服务不存在时静默跳过。若命令已自带 Get-Service 保护(如家庭组/HPET),跳过注入避免嵌套。
        if (!script.Contains("Get-Service", StringComparison.OrdinalIgnoreCase))
        {
            script = ServiceOpPattern.Replace(script,
                m => $"if (Get-Service {m.Groups[2].Value} -ErrorAction SilentlyContinue) {{ {m.Groups[1].Value} {m.Groups[2].Value}{m.Groups[3].Value} }}");
        }

        var needAdmin = item.RequiresAdmin && !ElevationHelper.IsElevated();

        // 控制台窗口模式:在独立 cmd 窗口实时显示进度(用于 DISM/SFC 等长耗时修复命令)
        if (item.UseConsoleWindow)
        {
            return LaunchConsoleWindow(script, item);
        }

        if (needAdmin)
            return await RunElevatedAsync(script, item.TimeoutSeconds, ct);

        return await RunDirectAsync(script, item.TimeoutSeconds, onOutput, ct);
    }

    /// <summary>
    /// 控制台窗口模式:启动独立控制台窗口实时显示命令进度(用于 DISM/SFC 等长耗时修复)。
    /// 窗口保留(-NoExit),用户可查看进度;完成后手动关闭。返回成功后写入任务记录。
    /// </summary>
    private ExecutionResult LaunchConsoleWindow(string script, FunctionItem item)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = true, // 使用 ShellExecute 以便窗口独立显示
                CreateNoWindow = false
            };
            // 在 cmd 中调用 powershell -NoExit 运行脚本,保留窗口显示进度
            psi.ArgumentList.Add("/k");
            psi.ArgumentList.Add($"powershell.exe -NoExit -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"");
            System.Diagnostics.Process.Start(psi);
            return new ExecutionResult(true, 0,
                $"已在控制台窗口启动「{item.Name}」,请查看窗口中的执行进度;完成后可关闭窗口。", null);
        }
        catch (Exception ex)
        {
            return new ExecutionResult(false, -1, null, $"启动控制台窗口失败:{ex.Message}");
        }
    }

    private async Task<ExecutionResult> RunDirectAsync(
        string script, int timeoutSeconds, Action<string>? onOutput, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = PowerShellPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);

        using var process = new Process { StartInfo = psi };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            output.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            error.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };

        if (!process.Start())
            return new ExecutionResult(false, -1, null, "无法启动 powershell.exe");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeoutSeconds > 0) cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Kill(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            _logger.LogWarning("执行超时,已终止进程: {Script}", Short(script));
            return new ExecutionResult(false, -1, output.ToString(), $"执行超过 {timeoutSeconds} 秒,已终止");
        }

        var err = error.ToString().Trim();
        return new ExecutionResult(process.ExitCode == 0, process.ExitCode, output.ToString(), err.Length > 0 ? err : null);
    }

    private async Task<ExecutionResult> RunElevatedAsync(string script, int timeoutSeconds, CancellationToken ct)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NControl");
        Directory.CreateDirectory(tempDir);
        var ps1 = Path.Combine(tempDir, $"nctl_{Guid.NewGuid():N}.ps1");
        var outFile = Path.Combine(tempDir, $"nctl_{Guid.NewGuid():N}.out");

        try
        {
            // UTF-8 BOM 写入:PowerShell 5.1 对无 BOM 文件按 ANSI 解码,中文脚本会乱码
            await File.WriteAllTextAsync(ps1, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);

            // 提权进程:输出重定向到文件,父进程等待并读取;退出码经 Start-Process -PassThru 获取
            var wrapper = $"& {{ & '{ps1}' *> '{outFile}'; exit $LASTEXITCODE }}";
            var psi = new ProcessStartInfo
            {
                FileName = PowerShellPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add($"Start-Process -FilePath '{PowerShellPath}' -Verb RunAs -Wait -PassThru -ArgumentList @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-Command', '{EscapeSingle(wrapper)}') | ForEach-Object {{ exit $_.ExitCode }}");

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
                return new ExecutionResult(false, -1, null, "无法启动提权进程");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (timeoutSeconds > 0) cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds + 60));

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Kill(process);
                throw;
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                return new ExecutionResult(false, -1, null, $"执行超过 {timeoutSeconds} 秒,已终止");
            }

            var output = File.Exists(outFile) ? await File.ReadAllTextAsync(outFile, Encoding.UTF8, ct) : "";
            return new ExecutionResult(process.ExitCode == 0, process.ExitCode, output, null);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ExecutionResult(false, -1, null, "权限提升已取消或失败(UAC)");
        }
        finally
        {
            TryDelete(ps1);
            TryDelete(outFile);
        }
    }

    private static string EscapeSingle(string s) => s.Replace("'", "''");
    private static string Short(string s) => s.Length <= 80 ? s : s[..80] + "…";

    private static void Kill(Process p)
    {
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
