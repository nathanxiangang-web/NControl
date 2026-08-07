using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NControl.Core;

/// <summary>
/// 状态检测器:从功能项命令解析出检测规则,判断系统当前是否已处于优化后状态。
/// 对应产品文档 §4.1 预留的“状态检测”字段(第一代后补实现)。
/// 返回 null 表示无法检测(命令格式复杂或非注册表/服务类)。
/// 注:注册表读取使用 64 位视图,与 App 执行(64 位进程)一致。
/// </summary>
public static class StateDetector
{
    // Set-ItemProperty -Path 'X' -Name 'Y' -Value V -Type T
    private static readonly Regex RegWritePattern = new(
        @"Set-ItemProperty\s+-Path\s+'([^']+)'\s+-Name\s+'([^']+)'\s+-Value\s+([^\-;]+?)\s+-Type\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Set-Service X -StartupType Y / Stop-Service X
    private static readonly Regex ServiceSetPattern = new(
        @"Set-Service\s+([A-Za-z0-9_]+)\s+-StartupType\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 特殊命令
    private static readonly Regex PowerCfgActivePattern = new(
        @"powercfg(?:\.exe)?\s+(?:-|/)setactive\s+([0-9a-f\-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HibernateOffPattern = new(
        @"powercfg(?:\.exe)?\s+(?:-|/)h\s+off",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 检测功能项当前是否已处于优化后状态。
    /// </summary>
    /// <returns>true=已优化, false=未优化, null=无法检测。</returns>
    public static bool? Detect(FunctionItem item)
    {
        var cmd = item?.Command;
        if (string.IsNullOrWhiteSpace(cmd)) return null;

        var results = new List<bool?>();

        // ---- 注册表类 ----
        foreach (Match m in RegWritePattern.Matches(cmd))
        {
            results.Add(CheckRegValue(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value.Trim(), m.Groups[4].Value));
        }

        // ---- 服务类 ----
        foreach (Match m in ServiceSetPattern.Matches(cmd))
        {
            results.Add(CheckServiceStartType(m.Groups[1].Value, m.Groups[2].Value));
        }

        // ---- 特殊命令 ----
        var pc = PowerCfgActivePattern.Match(cmd);
        if (pc.Success)
            results.Add(CheckActivePowerScheme(pc.Groups[1].Value));
        if (HibernateOffPattern.IsMatch(cmd))
            results.Add(!CheckHibernateEnabled());

        if (results.Count == 0) return null;

        // 任一项未优化 => 未优化;全部已优化 => 已优化
        if (results.Any(r => r == false)) return false;
        if (results.All(r => r == true)) return true;
        return null;
    }

    private static bool? CheckRegValue(string psPath, string name, string expectedValue, string type)
    {
        try
        {
            var (hive, subKey) = SplitPath(psPath);
            if (hive is null) return null;
            using var key = OpenHive(hive.Value).OpenSubKey(subKey, writable: false);
            if (key is null) return null;
            var actual = key.GetValue(name);
            if (actual is null) return false;

            var expected = ParseValue(expectedValue, type);
            return ValuesEqual(actual, expected);
        }
        catch
        {
            return null;
        }
    }

    private static bool? CheckServiceStartType(string serviceName, string expectedStartType)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: false);
            if (key is null) return null;
            var start = key.GetValue("Start");
            if (start is null) return null;
            var expected = expectedStartType.ToLowerInvariant() switch
            {
                "disabled" => 4,
                "automatic" => 2,
                "manual" => 3,
                _ => (int?)null
            };
            if (expected is null) return null;
            return Convert.ToInt32(start) == expected;
        }
        catch
        {
            return null;
        }
    }

    private static bool? CheckActivePowerScheme(string expectedGuid)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes", writable: false);
            if (key is null) return null;
            var active = key.GetValue("ActivePowerScheme") as string;
            return string.Equals(active, expectedGuid, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static bool? CheckHibernateEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Power", writable: false);
            if (key is null) return null;
            var h = key.GetValue("HibernateEnabled");
            return h is null || Convert.ToInt32(h) == 0;
        }
        catch
        {
            return null;
        }
    }

    // ---------- 工具 ----------

    private static (RegistryHive? Hive, string SubKey) SplitPath(string psPath)
    {
        if (psPath.StartsWith(@"HKCU:\", StringComparison.OrdinalIgnoreCase))
            return (RegistryHive.CurrentUser, psPath[6..]);
        if (psPath.StartsWith(@"HKLM:\", StringComparison.OrdinalIgnoreCase))
            return (RegistryHive.LocalMachine, psPath[6..]);
        return (null, "");
    }

    private static RegistryKey OpenHive(RegistryHive hive) => hive switch
    {
        RegistryHive.CurrentUser => Registry.CurrentUser,
        RegistryHive.LocalMachine => Registry.LocalMachine,
        _ => throw new NotSupportedException()
    };

    private static object? ParseValue(string value, string type)
    {
        if (type.Equals("DWord", StringComparison.OrdinalIgnoreCase))
            return long.TryParse(value, out var l) ? l : (object?)null;
        if (type.Equals("String", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("REG_SZ", StringComparison.OrdinalIgnoreCase))
            return value.Trim('\'');
        if (type.Equals("Binary", StringComparison.OrdinalIgnoreCase))
        {
            // [byte[]](0,0,0,0) 或 0,0,0,0
            var m = Regex.Match(value, @"\(byte\[\]\)\(([^)]*)\)");
            var inner = m.Success ? m.Groups[1].Value : value.Trim('(', ')');
            var parts = inner.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(p => p.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToByte(p, 16)
                    : Convert.ToByte(p))
                .ToArray();
            return parts;
        }
        return null;
    }

    private static bool ValuesEqual(object? actual, object? expected)
    {
        if (expected is null) return false;
        if (actual is byte[] ab && expected is byte[] eb)
            return ab.SequenceEqual(eb);
        try
        {
            return Convert.ToInt64(actual) == Convert.ToInt64(expected);
        }
        catch
        {
            return string.Equals(actual?.ToString(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
