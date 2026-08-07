using Microsoft.Win32;

namespace NControl.Core;

/// <summary>
/// 兼容性规则引擎(第二代 §4):按 FunctionId 判定功能在当前环境的兼容性。
/// 规则来源优先级(§13):Microsoft 官方文档 > 系统实际行为 > 真机测试 > 成熟开源实现。
/// 无法判断时返回 Unknown/NeedsVerification,不自行猜测。
/// </summary>
public sealed class CompatibilityEngine
{
    private readonly IEnvironmentProbe _probe;
    private readonly List<CompatibilityRule> _rules = new();
    private readonly Dictionary<string, CompatibilityResult> _cache = new();

    public CompatibilityEngine(IEnvironmentProbe probe)
    {
        _probe = probe;
        RegisterDefaultRules();
    }

    /// <summary>判定功能兼容性。结果按 FunctionId 缓存(单次会话内环境不变)。</summary>
    public CompatibilityResult Evaluate(FunctionItem item)
    {
        if (item is null || string.IsNullOrEmpty(item.Id)) return CompatibilityResult.Unknown("功能标识缺失");
        if (_cache.TryGetValue(item.Id, out var cached)) return cached;

        // 1. 精确规则
        var rule = _rules.FirstOrDefault(r => r.Pattern == item.Id);
        // 2. 前缀规则(模块级,如 "update." / "advanced.")
        rule ??= _rules.FirstOrDefault(r => r.Pattern.EndsWith(".*") && item.Id.StartsWith(r.Pattern[..^1], StringComparison.OrdinalIgnoreCase));
        // 3. 分类规则
        rule ??= _rules.FirstOrDefault(r => r.Pattern.StartsWith("category:") && r.Pattern[9..] == item.Category);

        var env = _probe.GetEnvironment();
        var result = rule is null
            ? CompatibilityResult.NeedsVerification("该功能尚未配置兼容性规则,理论可用但未在当前版本验证")
            : EvaluateRule(rule, item, env);

        _cache[item.Id] = result;
        return result;
    }

    public void ClearCache() => _cache.Clear();

    /// <summary>注册自定义规则(供测试与后续扩展)。</summary>
    public void RegisterRule(CompatibilityRule rule) => _rules.Add(rule);

    private CompatibilityResult EvaluateRule(CompatibilityRule rule, FunctionItem item, WindowsEnvironmentInfo env)
    {
        // 1. 最低构建号
        if (rule.MinBuild > 0 && env.BuildNumber > 0 && env.BuildNumber < rule.MinBuild)
            return CompatibilityResult.NotSupported(
                $"需要 Windows 构建号 ≥ {rule.MinBuild},当前为 {env.BuildNumber}({env.DisplayVersion})");

        // 2. 系统类型限制
        if (rule.RequiresWindows11 && !env.IsWindows11)
            return CompatibilityResult.NotSupported("该功能仅适用于 Windows 11");
        if (rule.RequiresWindows10 && !env.IsWindows10 && !env.IsWindows11)
            return CompatibilityResult.NotSupported("该功能仅适用于 Windows 10/11");

        // 3. 产品类型白名单
        if (rule.AllowedProductTypes.Count > 0 && !rule.AllowedProductTypes.Contains(env.ProductType, StringComparer.OrdinalIgnoreCase))
            return CompatibilityResult.NotSupported(
                $"该功能不支持当前系统版本({env.ProductType}),仅支持 {string.Join("/", rule.AllowedProductTypes)}");

        // 4. 依赖组件检查(任一缺失 → Unsupported)
        foreach (var dep in rule.Dependencies)
        {
            var depResult = CheckDependency(dep);
            if (depResult is not null)
                return depResult;
        }

        // 5. 已验证构建范围
        if (rule.MaxVerifiedBuild > 0 && env.BuildNumber > rule.MaxVerifiedBuild)
            return CompatibilityResult.NeedsVerification(
                $"当前构建号 {env.BuildNumber} 高于已验证版本 {rule.MaxVerifiedBuild},理论可用但建议谨慎");

        // 6. 高风险/实验性 → NeedsVerification(提示谨慎,不禁止)
        if (item.Risk is RiskLevel.HighRisk or RiskLevel.Experimental)
            return CompatibilityResult.NeedsVerification("高风险/实验性功能,执行前请确认影响");

        return CompatibilityResult.Ok(rule.MaxVerifiedBuild > 0 ? rule.MaxVerifiedBuild.ToString() : null);
    }

    /// <summary>检查依赖。返回 null 表示满足,返回结果表示不满足。</summary>
    private CompatibilityResult? CheckDependency(CompatibilityDependency dep)
    {
        try
        {
            switch (dep.Kind)
            {
                case DependencyKind.RegistryKey:
                    {
                        var (hive, path) = SplitRegPath(dep.Target);
                        if (hive is null) return null;
                        using var key = hive.OpenSubKey(path);
                        return key is null
                            ? CompatibilityResult.NotSupported($"依赖的系统组件不存在:{dep.DisplayName ?? dep.Target}")
                            : null;
                    }
                case DependencyKind.RegistryValue:
                    {
                        var (hive, path) = SplitRegPath(dep.Target);
                        if (hive is null) return null;
                        using var key = hive.OpenSubKey(path);
                        var value = key?.GetValue(dep.ValueName ?? "");
                        return value is null
                            ? CompatibilityResult.NotSupported($"依赖的系统设置不存在:{dep.DisplayName ?? dep.Target}\\{dep.ValueName}")
                            : null;
                    }
                case DependencyKind.Service:
                    {
                        // 服务存在性:读注册表 Services 键(无需提权)
                        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{dep.Target}");
                        return key is null
                            ? CompatibilityResult.NotSupported($"依赖的系统服务不存在:{dep.DisplayName ?? dep.Target}")
                            : null;
                    }
                default:
                    return null;
            }
        }
        catch
        {
            return null; // 探测失败不武断判定
        }
    }

    private static (RegistryKey? Hive, string Path) SplitRegPath(string target)
    {
        if (target.StartsWith(@"HKLM:\", StringComparison.OrdinalIgnoreCase))
            return (Registry.LocalMachine, target[6..]);
        if (target.StartsWith(@"HKCU:\", StringComparison.OrdinalIgnoreCase))
            return (Registry.CurrentUser, target[6..]);
        return (null, "");
    }

    /// <summary>默认规则:第一批重点功能(更新/系统组件/Appx/服务/Defender/高风险)。</summary>
    private void RegisterDefaultRules()
    {
        // ---- 更新设置:Windows 10/11 通用,构建号门槛 ----
        _rules.Add(new CompatibilityRule("update.*", "Windows 更新设置", 10240));
        _rules.Add(new CompatibilityRule("system.pause-updates-5000d", "暂停更新 5000 天", 17763, 0) { Note = "通过策略键写入,Win10 1809+ 可用" });

        // ---- 系统设置:构建门槛 ----
        _rules.Add(new CompatibilityRule("system.*", "系统设置", 10240));

        // ---- Appx 相关(应用模块) ----
        _rules.Add(new CompatibilityRule("apps.*", "预装应用卸载", 10240));

        // ---- 高风险/安全类:需验证 ----
        _rules.Add(new CompatibilityRule("security.*", "安全设置(高风险)", 10240) { Note = "修改安全关键项,兼容性默认 NeedsVerification" });
        _rules.Add(new CompatibilityRule("advanced.*", "高级设置", 10240) { Note = "高级功能,兼容性默认 NeedsVerification" });

        // ---- 清理/修复/工具:通用 ----
        _rules.Add(new CompatibilityRule("cleanup.*", "清理维护", 10240));
        _rules.Add(new CompatibilityRule("repair.*", "系统修复", 10240));
        _rules.Add(new CompatibilityRule("tools.*", "实用工具", 10240));
    }
}

/// <summary>兼容性规则:按功能 ID 模式匹配。</summary>
public sealed class CompatibilityRule
{
    public CompatibilityRule(string pattern, string? note = null, int minBuild = 0, int maxVerifiedBuild = 0,
        bool requiresWindows11 = false, bool requiresWindows10 = false,
        IReadOnlyList<string>? allowedProductTypes = null,
        IReadOnlyList<CompatibilityDependency>? dependencies = null)
    {
        Pattern = pattern;
        Note = note;
        MinBuild = minBuild;
        MaxVerifiedBuild = maxVerifiedBuild;
        RequiresWindows11 = requiresWindows11;
        RequiresWindows10 = requiresWindows10;
        AllowedProductTypes = allowedProductTypes ?? Array.Empty<string>();
        Dependencies = dependencies ?? Array.Empty<CompatibilityDependency>();
    }

    /// <summary>匹配模式:精确 ID 或前缀(如 "update.*")或 "category:分类名"。</summary>
    public string Pattern { get; init; }

    /// <summary>规则说明(用于诊断)。</summary>
    public string? Note { get; init; }

    /// <summary>最低 Windows 构建号(0=不限)。</summary>
    public int MinBuild { get; init; }

    /// <summary>最高已验证构建号(超过则 NeedsVerification)。</summary>
    public int MaxVerifiedBuild { get; init; }

    /// <summary>仅 Windows 11。</summary>
    public bool RequiresWindows11 { get; init; }

    /// <summary>仅 Windows 10/11。</summary>
    public bool RequiresWindows10 { get; init; }

    /// <summary>支持的产品类型白名单(空=不限)。</summary>
    public IReadOnlyList<string> AllowedProductTypes { get; init; } = Array.Empty<string>();

    /// <summary>依赖组件列表(任一缺失 → Unsupported)。</summary>
    public IReadOnlyList<CompatibilityDependency> Dependencies { get; init; } = Array.Empty<CompatibilityDependency>();
}

/// <summary>兼容性依赖组件。</summary>
public sealed class CompatibilityDependency
{
    public CompatibilityDependency(DependencyKind kind, string target, string? valueName = null, string? displayName = null)
    {
        Kind = kind;
        Target = target;
        ValueName = valueName;
        DisplayName = displayName;
    }

    public DependencyKind Kind { get; init; }

    /// <summary>注册表路径(如 HKLM:\...)/服务名/组件名。</summary>
    public string Target { get; init; }

    /// <summary>值名(仅 RegistryValue)。</summary>
    public string? ValueName { get; init; }

    /// <summary>展示名(如 "远程注册表服务")。</summary>
    public string? DisplayName { get; init; }
}

public enum DependencyKind
{
    RegistryKey,
    RegistryValue,
    Service
}
