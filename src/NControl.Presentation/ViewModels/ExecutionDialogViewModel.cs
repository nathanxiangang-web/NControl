using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 执行弹窗:确认清单 → 统一进度面板(逐项成功/失败/跳过)→ 汇总结果。
/// 所有执行都经过执行中心;高风险项在确认阶段展示警示。
/// </summary>
public partial class ExecutionDialogViewModel : ObservableObject
{
    public enum DialogPhase
    {
        Confirm,
        Running,
        Done
    }

    private readonly IExecutionCenter _center;
    private IReadOnlyList<FunctionItem> _pendingItems = Array.Empty<FunctionItem>();
    private string _pendingName = "";
    private CancellationTokenSource? _cts;

    public ExecutionDialogViewModel(IExecutionCenter center)
    {
        _center = center;
    }

    public ObservableCollection<ExecRowViewModel> Rows { get; } = new();

    public event Action<TaskRecord>? TaskCompleted;

    [ObservableProperty]
    private bool isOpen;

    [ObservableProperty]
    private DialogPhase currentPhase;

    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string progressText = "";

    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string logText = "";

    [ObservableProperty]
    private string summaryText = "";

    [ObservableProperty]
    private bool hasHighRisk;

    [ObservableProperty]
    private bool isRunning;

    public async Task RunAsync(IReadOnlyList<FunctionItem> items, string taskName, bool showConfirm)
    {
        _pendingItems = items;
        _pendingName = taskName;
        Title = taskName;
        HasHighRisk = items.Any(f => f.Risk == RiskLevel.HighRisk);

        Rows.Clear();
        foreach (var item in items)
            Rows.Add(new ExecRowViewModel(item));

        LogText = "";
        SummaryText = "";
        ProgressPercent = 0;
        ProgressText = $"共 {items.Count} 项,准备执行";
        CurrentPhase = showConfirm ? DialogPhase.Confirm : DialogPhase.Running;
        IsOpen = true;

        if (!showConfirm)
            await StartExecutionAsync();
    }

    [RelayCommand]
    private async Task StartAsync() => await StartExecutionAsync();

    private async Task StartExecutionAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        CurrentPhase = DialogPhase.Running;
        _cts = new CancellationTokenSource();
        LogText = "";
        SummaryText = "";

        var progress = new Progress<TaskItemProgress>(OnProgress);
        try
        {
            var record = await _center.ExecuteAsync(
                new ExecutionRequest(_pendingName, _pendingItems),
                progress,
                _cts.Token);

            ProgressPercent = 100;
            ProgressText = $"完成:{record.SuccessCount} 成功 / {record.FailedCount} 失败 / {record.CancelledCount} 取消";
            SummaryText = $"成功 {record.SuccessCount} 项 · 失败 {record.FailedCount} 项 · 取消 {record.CancelledCount} 项";
            if (record.RequiresRestart)
                SummaryText += "\n⚠ 部分项目需要重启资源管理器或系统才能完全生效。";
            // 安全规范:执行了关闭安全防护类功能后,提醒用户安装替代安全软件(如火绒)
            var securityDisables = _pendingItems.Where(i => IsSecurityDisableItem(i)).Select(i => i.Name).Distinct().ToArray();
            if (securityDisables.Length > 0)
                SummaryText += "\n\n⚠ 已关闭: " + string.Join("、", securityDisables) +
                    "\n系统将不再主动提醒安全状态。建议安装火绒安全等第三方防护软件,并在安装完成后开启实时防护。";
            CurrentPhase = DialogPhase.Done;
            TaskCompleted?.Invoke(record);
        }
        catch (Exception ex)
        {
            SummaryText = $"执行异常:{ex.Message}";
            CurrentPhase = DialogPhase.Done;
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    [RelayCommand]
    private void Close()
    {
        if (IsRunning) return;
        IsOpen = false;
        Rows.Clear();
        LogText = "";
        SummaryText = "";
    }

    private void OnProgress(TaskItemProgress p)
    {
        if (p.Index < 0 || p.Index >= Rows.Count) return;
        var row = Rows[p.Index];
        row.ApplyStatus(p.Status);

        if (!string.IsNullOrWhiteSpace(p.Detail))
        {
            row.AppendOutput(p.Detail!);
            LogText += p.Detail + "\n";
        }

        ProgressPercent = p.Total == 0 ? 0 : Math.Min(100, (p.Index + 1.0) / p.Total * 100);
        ProgressText = $"当前进度 {p.Index + 1} / {p.Total}";
    }

    /// <summary>识别“关闭安全防护”类功能:执行成功后提醒用户安装替代安全软件(安全规范)。</summary>
    private static bool IsSecurityDisableItem(FunctionItem item)
    {
        var id = item.Id;
        if (id.StartsWith("advanced.disable-", StringComparison.OrdinalIgnoreCase)) return true;
        if (id.Contains("firewall", StringComparison.OrdinalIgnoreCase)
            || id.Contains("security-center", StringComparison.OrdinalIgnoreCase)
            || id.Contains("smartscreen", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}

/// <summary>执行弹窗中的单项行。</summary>
public partial class ExecRowViewModel : ObservableObject
{
    public ExecRowViewModel(FunctionItem item)
    {
        Item = item;
    }

    public FunctionItem Item { get; }
    public string Name => Item.Name;

    [ObservableProperty]
    private string statusGlyph = "○";

    [ObservableProperty]
    private Brush statusBrush = new SolidColorBrush(Color.FromRgb(0xA2, 0xA9, 0xB7));

    [ObservableProperty]
    private string statusText = "等待中";

    [ObservableProperty]
    private string outputText = "";

    public void ApplyStatus(TaskItemStatus status)
    {
        switch (status)
        {
            case TaskItemStatus.Running:
                StatusGlyph = "●";
                StatusText = "执行中";
                StatusBrush = new SolidColorBrush(Color.FromRgb(0x5B, 0x5C, 0xE2));
                break;
            case TaskItemStatus.Success:
                StatusGlyph = "✓";
                StatusText = "成功";
                StatusBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0xA3, 0x6B));
                break;
            case TaskItemStatus.Failed:
                StatusGlyph = "✗";
                StatusText = "失败";
                StatusBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x4E, 0x4E));
                break;
            case TaskItemStatus.Cancelled:
                StatusGlyph = "–";
                StatusText = "已取消";
                StatusBrush = new SolidColorBrush(Color.FromRgb(0x92, 0x9B, 0xAD));
                break;
        }
    }

    public void AppendOutput(string line) => OutputText += line + "\n";
}
