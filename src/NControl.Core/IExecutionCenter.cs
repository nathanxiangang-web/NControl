namespace NControl.Core;

/// <summary>
/// 执行中心:统一接收任务、调用实现、收集结果和生成记录(产品文档 §3.1)。
/// 所有可执行功能统一经过执行中心,不允许页面绕过。
/// </summary>
public interface IExecutionCenter
{
    /// <summary>
    /// 执行一批功能项,逐项反馈进度,完成后写入任务记录。
    /// </summary>
    Task<TaskRecord> ExecuteAsync(ExecutionRequest request, IProgress<TaskItemProgress>? progress, CancellationToken ct);
}
