namespace NControl.Core;

/// <summary>执行提供程序:承载某类执行方式的具体实现(产品文档 §3.1 功能实现层)。</summary>
public interface IExecutionProvider
{
    bool CanHandle(ExecutionKind kind);

    /// <summary>
    /// 执行一个功能项。
    /// </summary>
    /// <param name="item">功能项。</param>
    /// <param name="onOutput">输出流回调(用于实时日志)。</param>
    /// <param name="ct">取消令牌。</param>
    Task<ExecutionResult> ExecuteAsync(FunctionItem item, Action<string>? onOutput, CancellationToken ct);
}
