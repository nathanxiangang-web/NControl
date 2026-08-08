using System.Text.RegularExpressions;

namespace NControl.Core;

/// <summary>
/// 恢复命令构建器:为功能项生成“撤销优化”的 PowerShell 命令(产品文档 §10.1 回滚方向)。
/// 规则:
/// 1. 优先使用功能项显式配置的 RestoreCommand;
/// 2. 注册表写入(Set-ItemProperty)→ 反向删除对应值(删除后恢复系统默认);
/// 3. 服务禁用(Set-Service/Stop-Service)→ 恢复为手动启动(Manual),不擅自设为自动;
/// 4. 特殊命令(电源/内存压缩/防火墙等)→ 内置恢复表。
/// 无法推导时返回 null,UI 隐藏恢复入口。
/// </summary>
public static class RestoreCommandBuilder
{
    private static readonly Regex RegWritePattern = new(
        @"Set-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ServicePattern = new(
        @"(?:Set-Service|Stop-Service)\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? Build(FunctionItem item)
    {
        if (item is null) return null;
        if (!string.IsNullOrWhiteSpace(item.RestoreCommand))
            return item.RestoreCommand;

        var cmd = item.Command;
        if (string.IsNullOrWhiteSpace(cmd)) return null;

        var parts = new List<string>();

        // ---- 特殊命令恢复表 ----
        if (cmd.Contains("bcdedit /set useplatformclock", StringComparison.OrdinalIgnoreCase))
            parts.Add("bcdedit /deletevalue useplatformclock");
        if (cmd.Contains("powercfg /h off", StringComparison.OrdinalIgnoreCase))
            parts.Add("powercfg /h on");
        if (cmd.Contains("Disable-MMAgent", StringComparison.OrdinalIgnoreCase))
            parts.Add("Enable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue");
        if (cmd.Contains("Set-NetFirewallProfile", StringComparison.OrdinalIgnoreCase))
            parts.Add("Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True");
        if (cmd.Contains("Disable-ComputerRestore", StringComparison.OrdinalIgnoreCase))
            parts.Add("Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue");

        // ---- 服务类:恢复为手动启动 ----
        var serviceNames = ServicePattern.Matches(cmd)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToArray();
        if (serviceNames.Length > 0)
            parts.Add(string.Join("; ", serviceNames.Select(n => $"Set-Service {n} -StartupType Manual -ErrorAction SilentlyContinue")));

        // ---- 注册表类:反向删除写入的值 ----
        var pairs = RegWritePattern.Matches(cmd)
            .Select(m => (Path: m.Groups[1].Value, Name: m.Groups[2].Value))
            .Distinct()
            .ToArray();
        if (pairs.Length > 0)
            parts.Add(string.Join("; ", pairs.Select(p => $"Remove-ItemProperty -Path '{p.Path}' -Name '{p.Name}' -ErrorAction SilentlyContinue")));

        return parts.Count > 0 ? string.Join("; ", parts) : null;
    }
}
