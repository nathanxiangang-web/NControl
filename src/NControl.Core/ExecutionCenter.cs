using Microsoft.Extensions.Logging;

namespace NControl.Core;

/// <summary>
/// 执行中心:统一接收任务、调用实现、收集结果和生成记录。
/// 流程:创建任务 → 逐项执行 → 汇总结果 → 写入记录(产品文档 §5.1)。
/// 单项失败默认继续执行后续项目;用户停止则标记后续为已取消。
/// </summary>
public sealed class ExecutionCenter : IExecutionCenter
{
    private readonly IEnumerable<IExecutionProvider> _providers;
    private readonly ITaskRecordStore _store;
    private readonly ILogger<ExecutionCenter> _logger;

    public ExecutionCenter(
        IEnumerable<IExecutionProvider> providers,
        ITaskRecordStore store,
        ILogger<ExecutionCenter> logger)
    {
        _providers = providers;
        _store = store;
        _logger = logger;
    }

    public async Task<TaskRecord> ExecuteAsync(
        ExecutionRequest request,
        IProgress<TaskItemProgress>? progress,
        CancellationToken ct)
    {
        var record = new TaskRecord
        {
            Name = request.TaskName,
            StartedAt = DateTime.Now,
            Items = new List<TaskItemRecord>()
        };

        bool cancelled = false;
        int total = request.Items.Count;

        for (int i = 0; i < total; i++)
        {
            var item = request.Items[i];
            var itemRecord = new TaskItemRecord
            {
                FunctionId = item.Id,
                FunctionName = item.Name,
                Status = "等待中",
                StartedAt = DateTime.Now
            };
            record.Items.Add(itemRecord);

            if (cancelled)
            {
                itemRecord.Status = "已取消";
                record.CancelledCount++;
                progress?.Report(new TaskItemProgress(item, TaskItemStatus.Cancelled, null, i, total));
                continue;
            }

            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                itemRecord.Status = "已取消";
                record.CancelledCount++;
                progress?.Report(new TaskItemProgress(item, TaskItemStatus.Cancelled, null, i, total));
                continue;
            }

            itemRecord.Status = "执行中";
            progress?.Report(new TaskItemProgress(item, TaskItemStatus.Running, null, i, total));

            var provider = _providers.FirstOrDefault(p => p.CanHandle(item.Kind));
            if (provider is null)
            {
                itemRecord.Status = "失败";
                itemRecord.Error = "没有可用的执行提供程序";
                itemRecord.FinishedAt = DateTime.Now;
                record.FailedCount++;
                progress?.Report(new TaskItemProgress(item, TaskItemStatus.Failed, itemRecord.Error, i, total));
                continue;
            }

            try
            {
                var result = await provider.ExecuteAsync(
                    item,
                    line => progress?.Report(new TaskItemProgress(item, TaskItemStatus.Running, line, i, total)),
                    ct);

                itemRecord.ExitCode = result.ExitCode;
                itemRecord.Output = result.Output;
                itemRecord.Error = result.Error;
                itemRecord.FinishedAt = DateTime.Now;

                if (result.Success)
                {
                    itemRecord.Status = "成功";
                    record.SuccessCount++;
                }
                else
                {
                    itemRecord.Status = "失败";
                    record.FailedCount++;
                    if (string.IsNullOrWhiteSpace(itemRecord.Error))
                        itemRecord.Error = $"退出代码 {result.ExitCode}";
                }

                if (item.Restart != RestartRequirement.None)
                    record.RequiresRestart = true;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                itemRecord.Status = "已取消";
                itemRecord.Error = "已被用户停止";
                itemRecord.FinishedAt = DateTime.Now;
                record.CancelledCount++;
            }
            catch (Exception ex)
            {
                itemRecord.Status = "失败";
                itemRecord.Error = ex.Message;
                itemRecord.FinishedAt = DateTime.Now;
                record.FailedCount++;
                _logger.LogError(ex, "执行功能项失败: {Id}", item.Id);
            }

            progress?.Report(new TaskItemProgress(item, StatusFrom(itemRecord.Status), itemRecord.Error, i, total));
        }

        record.FinishedAt = DateTime.Now;
        record.Result = Summarize(record);
        await _store.SaveAsync(record);
        _logger.LogInformation("任务完成: {Name} -> {Result} (成功 {Ok} / 失败 {Fail} / 取消 {Cancel})",
            record.Name, record.Result, record.SuccessCount, record.FailedCount, record.CancelledCount);
        return record;
    }

    private static TaskItemStatus StatusFrom(string status) => status switch
    {
        "成功" => TaskItemStatus.Success,
        "失败" => TaskItemStatus.Failed,
        "已取消" => TaskItemStatus.Cancelled,
        _ => TaskItemStatus.Pending
    };

    private static string Summarize(TaskRecord record)
    {
        if (record.CancelledCount > 0 && record.SuccessCount == 0 && record.FailedCount == 0)
            return "已取消";
        if (record.FailedCount > 0)
            return "部分失败";
        if (record.SuccessCount > 0)
            return "成功";
        return "无结果";
    }
}
