using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using NControl.Core;

namespace NControl.Infrastructure;

/// <summary>
/// 系统命令执行提供程序:经 cmd.exe /c 执行(用于系统自带命令行工具)。
/// 输出以 UTF-8 重新编码,避免中文乱码。
/// </summary>
public sealed class CommandExecutionProvider : IExecutionProvider
{
    private readonly ILogger<CommandExecutionProvider> _logger;

    public CommandExecutionProvider(ILogger<CommandExecutionProvider> logger) => _logger = logger;

    public bool CanHandle(ExecutionKind kind) => kind == ExecutionKind.Command;

    public async Task<ExecutionResult> ExecuteAsync(FunctionItem item, Action<string>? onOutput, CancellationToken ct)
    {
        var command = item.Command;
        if (string.IsNullOrWhiteSpace(command))
            return new ExecutionResult(false, -1, null, "功能项没有配置系统命令");

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add($"chcp 65001 >nul & {command}");

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
            return new ExecutionResult(false, -1, null, "无法启动 cmd.exe");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (item.TimeoutSeconds > 0) cts.CancelAfter(TimeSpan.FromSeconds(item.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new ExecutionResult(false, -1, output.ToString(), $"执行超过 {item.TimeoutSeconds} 秒,已终止");
        }

        var err = error.ToString().Trim();
        return new ExecutionResult(process.ExitCode == 0, process.ExitCode, output.ToString(), err.Length > 0 ? err : null);
    }
}
