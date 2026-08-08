namespace NControl.Core;

/// <summary>
/// 功能项:产品最小业务单元(产品文档 §4.1)。
/// 用户看到的每一个开关、按钮或可执行操作,本质上都是一个功能项。
/// </summary>
public sealed class FunctionItem
{
    /// <summary>唯一标识,长期识别该功能,不随显示名称变化。</summary>
    public required string Id { get; init; }

    /// <summary>用户能理解的动作名称。</summary>
    public required string Name { get; init; }

    /// <summary>分组,如“任务栏与开始菜单”。</summary>
    public required string Category { get; init; }

    /// <summary>所属业务模块。</summary>
    public required ModuleKind Module { get; init; }

    /// <summary>解释作用、适用场景和可能影响。</summary>
    public required string Description { get; init; }

    /// <summary>风险等级,决定颜色、提示、确认方式和是否进入预设。</summary>
    public required RiskLevel Risk { get; init; }

    /// <summary>执行是否需要管理员权限。</summary>
    public required bool RequiresAdmin { get; init; }

    /// <summary>重启要求。</summary>
    public required RestartRequirement Restart { get; init; }

    /// <summary>实现来源(自研 / 系统命令 / 开源适配)。</summary>
    public required string Source { get; init; }

    /// <summary>执行方式。</summary>
    public ExecutionKind Kind { get; init; } = ExecutionKind.PowerShell;

    /// <summary>PowerShell 脚本或系统命令。</summary>
    public string? Command { get; init; }

    /// <summary>
    /// 恢复命令(可选)。为空时由 <see cref="RestoreCommandBuilder"/> 从 Command 自动推导。
    /// 对应产品文档 §4.1“恢复方式”预留字段的第一代实现。
    /// </summary>
    public string? RestoreCommand { get; init; }

    /// <summary>超时秒数。</summary>
    public int TimeoutSeconds { get; init; } = 180;

    /// <summary>即时工具:点击即运行,不参与批量选择流程(产品文档 §5.3)。</summary>
    public bool IsTool { get; init; }

    /// <summary>附加信息(如 Appx 包名)。</summary>
    public string? Extra { get; init; }

    /// <summary>预留:第一代后补的状态检测。</summary>
    public bool Detectable { get; init; }

    /// <summary>预留:第一代后补的恢复/回滚。</summary>
    public bool Recoverable { get; init; }
}

/// <summary>方案:若干功能项的组合,只引用现有功能,不复制执行逻辑(产品文档 §4.2)。</summary>
public sealed class Preset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required RiskLevel Risk { get; init; }
    /// <summary>适用人群说明,如“推荐新用户”“适合大多数人”“谨慎使用”。</summary>
    public required string TargetGroup { get; init; }
    /// <summary>引用的功能项 Id 列表。</summary>
    public required IReadOnlyList<string> FeatureIds { get; init; }
}

/// <summary>任务记录:每次点击执行的统一载体(产品文档 §4.3)。</summary>
public sealed class TaskRecord
{
    public long Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public required string Name { get; set; }
    public string Result { get; set; } = "";
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int CancelledCount { get; set; }
    public bool RequiresRestart { get; set; }
    public required List<TaskItemRecord> Items { get; set; } = new();
}

/// <summary>任务单项结果。</summary>
public sealed class TaskItemRecord
{
    public required string FunctionId { get; set; }
    public required string FunctionName { get; set; }
    public string Status { get; set; } = "等待中";
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int ExitCode { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

/// <summary>一次执行的请求。</summary>
public sealed record ExecutionRequest(string TaskName, IReadOnlyList<FunctionItem> Items);

/// <summary>执行进度事件。</summary>
public sealed record TaskItemProgress(FunctionItem Item, TaskItemStatus Status, string? Detail, int Index, int Total);

/// <summary>单项执行结果。</summary>
public sealed record ExecutionResult(bool Success, int ExitCode, string? Output, string? Error);
