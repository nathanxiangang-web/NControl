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
    private string highRiskWarningText = "本任务包含高风险项目(可能降低安全性、更新能力或稳定性)。确认后仍将执行,请谨慎操作。";

    [ObservableProperty]
    private bool isRunning;

    public async Task RunAsync(IReadOnlyList<FunctionItem> items, string taskName, bool showConfirm)
    {
        _pendingItems = items;
        _pendingName = taskName;
        Title = taskName;
        HasHighRisk = items.Any(f => f.Risk == RiskLevel.HighRisk);
        HighRiskWarningText = items.Any(f => f.Id.Equals("advanced.disable-security-center", StringComparison.OrdinalIgnoreCase))
            ? "将以 TrustedInstaller 权限自动关闭篡改保护,并彻底禁用 Windows 安全中心、Defender、SmartScreen 和部分 AMSI 检查。此操作没有恢复按钮,任务记录也无法回滚;确认前请先创建完整系统镜像。"
            : "本任务包含高风险项目(可能降低安全性、更新能力或稳定性)。确认后仍将执行,请谨慎操作。";

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
            var succeededIds = record.Items
                .Where(i => i.Status == "成功")
                .Select(i => i.FunctionId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (succeededIds.Contains("advanced.disable-security-center"))
                SummaryText += "\n\n⛔ Windows 安全中心/Defender 已彻底禁用。NControl 不提供恢复或任务回滚;请重启系统完成操作。";

            // 只汇总真正执行成功的防护组件变更;已失败的项目不误报为已关闭。
            var securityDisables = _pendingItems
                .Where(i => succeededIds.Contains(i.Id) && IsSecurityDisableItem(i))
                .Select(i => i.Name)
                .Distinct()
                .ToArray();
            if (securityDisables.Length > 0)
                SummaryText += "\n\n⚠ 已关闭: " + string.Join("、", securityDisables) +
                    "\n部分系统防护已降低。如非信任的内网或兼容性场景,请尽快使用“恢复”或任务回滚重新启用。";
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
        return id.Contains("firewall", StringComparison.OrdinalIgnoreCase)
               || id.Contains("smartscreen", StringComparison.OrdinalIgnoreCase)
               || id.Contains("memory-integrity", StringComparison.OrdinalIgnoreCase)
               || id.Contains("disable-vbs", StringComparison.OrdinalIgnoreCase)
               || id.Contains("exploit-protection", StringComparison.OrdinalIgnoreCase)
               || id.Contains("meltdown-mitigations", StringComparison.OrdinalIgnoreCase);
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
