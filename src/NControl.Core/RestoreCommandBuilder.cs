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
        if (cmd.Contains("Disable-ComputerRestore", StringComparison.OrdinalIgnoreCase))
            parts.Add("Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue");

        // 防火墙:恢复为启用,并清理组策略残留(否则安全中心显示"由你的组织管理"且恢复不生效)
        var isFirewall = cmd.Contains("Set-NetFirewallProfile", StringComparison.OrdinalIgnoreCase);
        if (isFirewall)
        {
            parts.Add("Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True -ErrorAction SilentlyContinue");
            // 清理优化工具曾写入的策略残留:值删除后,若 Profile 子键为空则一并删除,避免安全中心横幅残留
            parts.Add(
                "Remove-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\DomainProfile' -Recurse -Force -ErrorAction SilentlyContinue; " +
                "Remove-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\PrivateProfile' -Recurse -Force -ErrorAction SilentlyContinue; " +
                "Remove-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\PublicProfile' -Recurse -Force -ErrorAction SilentlyContinue; " +
                "if ((Test-Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall') -and -not (Get-ChildItem 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall' -ErrorAction SilentlyContinue)) { Remove-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall' -Force -ErrorAction SilentlyContinue }");
        }

        // ---- 服务类:恢复为手动启动 ----
        var serviceNames = ServicePattern.Matches(cmd)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToArray();
        if (serviceNames.Length > 0)
        {
            // 已知默认 Automatic 的服务:恢复为 Automatic(否则远程注册表等重启后不自动启动)
            var autoDefaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "RemoteRegistry", "DPS", "TrkWks", "SysMain", "WSearch", "DiagTrack", "PcaSvc", "HomeGroupProvider"
            };
            parts.Add(string.Join("; ", serviceNames.Select(n =>
                $"Set-Service {n} -StartupType {(autoDefaults.Contains(n) ? "Automatic" : "Manual")} -ErrorAction SilentlyContinue; " +
                $"Start-Service {n} -ErrorAction SilentlyContinue")));
        }

        // ---- 注册表类:反向删除写入的值 ----
        var pairs = RegWritePattern.Matches(cmd)
            .Select(m => (Path: m.Groups[1].Value, Name: m.Groups[2].Value))
            .Distinct()
            .ToArray();

        // 防火墙:Set-NetFirewallProfile 已负责写回 EnableFirewall=1,跳过本地 FirewallPolicy 值的删除,
        // 避免“启用后又删除”导致配置变为未配置(安全中心显示由组织管理)。
        if (isFirewall)
            pairs = pairs
                .Where(p => !(p.Path.Contains("FirewallPolicy", StringComparison.OrdinalIgnoreCase)
                              && p.Name.Equals("EnableFirewall", StringComparison.OrdinalIgnoreCase)))
                .ToArray();

        if (pairs.Length > 0)
            parts.Add(string.Join("; ", pairs.Select(p => $"Remove-ItemProperty -Path '{p.Path}' -Name '{p.Name}' -ErrorAction SilentlyContinue")));

        return parts.Count > 0 ? string.Join("; ", parts) : null;
    }
}
