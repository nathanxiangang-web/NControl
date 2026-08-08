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
    private readonly ILogger<PowerShellExecutionProvider> _logger;

    public PowerShellExecutionProvider(ILogger<PowerShellExecutionProvider> logger) => _logger = logger;

    public bool CanHandle(ExecutionKind kind) => kind == ExecutionKind.PowerShell;

    public async Task<ExecutionResult> ExecuteAsync(FunctionItem item, Action<string>? onOutput, CancellationToken ct)
    {
        var script = item.Command;
        if (string.IsNullOrWhiteSpace(script))
            return new ExecutionResult(false, -1, null, "功能项没有配置 PowerShell 脚本");

        var needAdmin = item.RequiresAdmin && !ElevationHelper.IsElevated();

        if (needAdmin)
            return await RunElevatedAsync(script, item.TimeoutSeconds, ct);

        return await RunDirectAsync(script, item.TimeoutSeconds, onOutput, ct);
    }

    private async Task<ExecutionResult> RunDirectAsync(
        string script, int timeoutSeconds, Action<string>? onOutput, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
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
            await File.WriteAllTextAsync(ps1, script, Encoding.UTF8, ct);

            // 提权进程:输出重定向到文件,父进程等待并读取;退出码经 Start-Process -PassThru 获取
            var wrapper = $"& {{ & '{ps1}' *> '{outFile}'; exit $LASTEXITCODE }}";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add($"Start-Process -FilePath 'powershell.exe' -Verb RunAs -Wait -PassThru -ArgumentList @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-Command', '{EscapeSingle(wrapper)}') | ForEach-Object {{ exit $_.ExitCode }}");

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
