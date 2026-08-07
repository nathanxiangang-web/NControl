namespace NControl.Core;

/// <summary>
/// 兼容性状态:当前 Windows 环境对某功能的适用性判断(第二代 §4)。
/// </summary>
public enum CompatibilityStatus
{
    /// <summary>已确认当前环境可用。</summary>
    Supported,

    /// <summary>已确认当前环境不适用(禁止执行并说明原因)。</summary>
    Unsupported,

    /// <summary>暂无足够依据判断(明确显示未知,不静默隐藏)。</summary>
    Unknown,

    /// <summary>理论可用,但当前 Windows 版本尚未完成验证(提示谨慎)。</summary>
    NeedsVerification
}

/// <summary>
/// 功能兼容性判定结果。
/// </summary>
public sealed record CompatibilityResult(
    CompatibilityStatus Status,
    string? Reason,
    string? VerifiedBuild)
{
    public static CompatibilityResult Ok(string? verifiedBuild = null) =>
        new(CompatibilityStatus.Supported, null, verifiedBuild);

    public static CompatibilityResult NotSupported(string reason) =>
        new(CompatibilityStatus.Unsupported, reason, null);

    public static CompatibilityResult Unknown(string? reason = null) =>
        new(CompatibilityStatus.Unknown, reason, null);

    public static CompatibilityResult NeedsVerification(string? reason = null) =>
        new(CompatibilityStatus.NeedsVerification, reason, null);
}

/// <summary>
/// 当前 Windows 环境信息(版本/构建号/架构/产品类型)。
/// </summary>
public sealed record WindowsEnvironmentInfo
{
    /// <summary>主版本,如 10。</summary>
    public required int MajorVersion { get; init; }

    /// <summary>次版本,如 0。</summary>
    public required int MinorVersion { get; init; }

    /// <summary>构建号,如 26100。</summary>
    public required int BuildNumber { get; init; }

    /// <summary>UCRT 修订版本(可选)。</summary>
    public int Ubr { get; init; }

    /// <summary>系统架构:x64 / arm64 / x86。</summary>
    public required string Architecture { get; init; }

    /// <summary>产品类型名,如 Professional / Home / Enterprise。</summary>
    public required string ProductType { get; init; }

    /// <summary>是否为 Windows 11(构建号 >= 22000)。</summary>
    public bool IsWindows11 => MajorVersion >= 10 && BuildNumber >= 22000;

    /// <summary>是否为 Windows 10(构建号 10240..21999)。</summary>
    public bool IsWindows10 => MajorVersion >= 10 && BuildNumber >= 10240 && BuildNumber < 22000;

    /// <summary>版本显示文本,如 "Windows 11 23H2 (Build 22631)"。</summary>
    public string DisplayVersion => $"{(IsWindows11 ? "Windows 11" : IsWindows10 ? "Windows 10" : $"Windows {MajorVersion}.{MinorVersion}")} (Build {BuildNumber}{(Ubr > 0 ? $".{Ubr}" : "")})";
}

/// <summary>
/// 环境探测服务:读取当前 Windows 环境信息(第二代 §4)。
/// 实现注:注册表读取使用 64 位视图;探测为本地操作,不联网。
/// </summary>
public interface IEnvironmentProbe
{
    /// <summary>读取当前环境信息。失败时返回基于保守默认值的环境信息。</summary>
    WindowsEnvironmentInfo GetEnvironment();
}
