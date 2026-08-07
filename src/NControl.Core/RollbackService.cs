namespace NControl.Core;

/// <summary>
/// 批次回滚分析结果(第二代 §8):从一次任务记录分析可恢复项。
/// </summary>
public sealed class RollbackAnalysis
{
    /// <summary>可恢复项(有可靠恢复命令)。</summary>
    public List<RollbackItem> Restorable { get; set; } = new();

    /// <summary>不可恢复项(无恢复命令/不支持)。</summary>
    public List<RollbackItem> NotSupported { get; set; } = new();

    /// <summary>原任务记录。</summary>
    public required TaskRecord SourceTask { get; init; }

    /// <summary>可恢复数量。</summary>
    public int RestorableCount => Restorable.Count;

    /// <summary>不可恢复数量。</summary>
    public int NotSupportedCount => NotSupported.Count;
}

/// <summary>回滚清单中的一项。</summary>
public sealed class RollbackItem
{
    public required string FunctionId { get; init; }
    public required string FunctionName { get; init; }
    public required bool Restorable { get; init; }
    public string? RestoreCommand { get; init; }
    public string? Note { get; init; }
}

/// <summary>单项回滚结果状态(第二代 §8)。</summary>
public enum RollbackItemStatus
{
    /// <summary>已恢复。</summary>
    Restored,

    /// <summary>恢复失败。</summary>
    Failed,

    /// <summary>跳过(用户取消/无权限)。</summary>
    Skipped,

    /// <summary>不支持恢复。</summary>
    NotSupported,

    /// <summary>状态已被用户再次修改,不盲目覆盖。</summary>
    StateChanged,

    /// <summary>恢复后需要重启。</summary>
    RequiresRestart
}

/// <summary>
/// 批次回滚服务:从历史任务记录创建恢复任务(第二代 §8)。
/// 原则:只恢复有可靠恢复信息的功能;恢复本身是新 Task;逆序恢复;写历史;失败不删原任务。
/// </summary>
public sealed class RollbackService
{
    private readonly IFunctionCatalog _catalog;
    private readonly IExecutionCenter _execution;
    private readonly ITaskRecordStore _store;

    public RollbackService(IFunctionCatalog catalog, IExecutionCenter execution, ITaskRecordStore store)
    {
        _catalog = catalog;
        _execution = execution;
        _store = store;
    }

    /// <summary>分析任务记录,生成回滚清单。</summary>
    public RollbackAnalysis Analyze(TaskRecord task)
    {
        var analysis = new RollbackAnalysis { SourceTask = task };

        foreach (var item in task.Items)
        {
            var function = _catalog.Find(item.FunctionId);
            string? restoreCommand = null;
            string? note = null;

            if (function is not null)
            {
                restoreCommand = function.RestoreCommand;
                if (string.IsNullOrWhiteSpace(restoreCommand))
                    restoreCommand = RestoreCommandBuilder.Build(function);
                if (string.IsNullOrWhiteSpace(restoreCommand))
                    note = "该功能没有可推导的恢复命令";
            }
            else
            {
                note = $"功能 {item.FunctionId} 已不在当前目录中(可能是旧版本功能)";
            }

            var rollbackItem = new RollbackItem
            {
                FunctionId = item.FunctionId,
                FunctionName = item.FunctionName,
                Restorable = !string.IsNullOrWhiteSpace(restoreCommand),
                RestoreCommand = restoreCommand,
                Note = note
            };

            if (rollbackItem.Restorable)
                analysis.Restorable.Add(rollbackItem);
            else
                analysis.NotSupported.Add(rollbackItem);
        }

        return analysis;
    }

    /// <summary>
    /// 执行批次回滚:生成恢复任务 → 逆序执行(原则上与执行顺序相反) → 写历史。
    /// 返回新任务的记录(已写入 SQLite)。
    /// </summary>
    public async Task<TaskRecord> RollbackAsync(TaskRecord sourceTask, IProgress<TaskItemProgress>? progress = null,
        CancellationToken ct = default)
    {
        var analysis = Analyze(sourceTask);
        if (analysis.RestorableCount == 0)
            throw new InvalidOperationException("没有可恢复的功能项");

        // 逆序:后执行的先恢复
        var items = analysis.Restorable
            .OrderByDescending(i => sourceTask.Items.FindIndex(t => t.FunctionId == i.FunctionId))
            .Select(i =>
            {
                var original = _catalog.Find(i.FunctionId);
                return new FunctionItem
                {
                    Id = i.FunctionId + ".restore",
                    Name = "恢复:" + i.FunctionName,
                    Category = "批次回滚",
                    Module = ModuleKind.Repair,
                    Description = $"撤销任务「{sourceTask.Name}」中的「{i.FunctionName}」,恢复系统默认状态。",
                    Risk = RiskLevel.Safe,
                    RequiresAdmin = original?.RequiresAdmin ?? false,
                    Restart = original?.Restart ?? RestartRequirement.None,
                    Source = "自研 · 批次回滚",
                    Kind = ExecutionKind.PowerShell,
                    Command = i.RestoreCommand,
                    TimeoutSeconds = original?.TimeoutSeconds ?? 180
                };
            })
            .ToArray();

        var request = new ExecutionRequest($"恢复任务:{sourceTask.Name}", items);
        var result = await _execution.ExecuteAsync(request, progress, ct);
        return result;
    }
}
