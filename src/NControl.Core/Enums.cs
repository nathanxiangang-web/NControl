namespace NControl.Core;

/// <summary>风险等级(产品文档 §7)。</summary>
public enum RiskLevel
{
    /// <summary>安全:一般仅影响界面和非关键偏好。</summary>
    Safe,

    /// <summary>推荐:有明确收益,影响范围可理解。</summary>
    Recommended,

    /// <summary>谨慎:可能影响部分功能、性能、耗电或维护。</summary>
    Caution,

    /// <summary>高风险:可能降低安全性、更新能力或稳定性。</summary>
    HighRisk,

    /// <summary>实验性:效果依赖系统版本或验证不足。</summary>
    Experimental
}

/// <summary>重启要求(产品文档 §4.1)。</summary>
public enum RestartRequirement
{
    /// <summary>无需重启。</summary>
    None,

    /// <summary>需要重启资源管理器。</summary>
    ExplorerRestart,

    /// <summary>需要重启系统。</summary>
    Reboot
}

/// <summary>执行方式(技术基线:C# 原生 + PowerShell + 系统命令)。</summary>
public enum ExecutionKind
{
    /// <summary>PowerShell 脚本。</summary>
    PowerShell,

    /// <summary>系统命令(经 cmd.exe)。</summary>
    Command
}

/// <summary>任务单项状态。</summary>
public enum TaskItemStatus
{
    Pending,
    Running,
    Success,
    Failed,
    Cancelled
}

/// <summary>业务模块(产品文档 §3.3)。</summary>
public enum ModuleKind
{
    Optimization,
    Applications,
    Cleanup,
    Repair,
    Tools
}
