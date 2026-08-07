using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>首页快速开始卡片。</summary>
public sealed class QuickCardViewModel
{
    public QuickCardViewModel(string title, string description, string linkText, string glyph, IRelayCommand command)
    {
        Title = title;
        Description = description;
        LinkText = linkText;
        Glyph = glyph;
        Command = command;
    }

    public string Title { get; }
    public string Description { get; }
    public string LinkText { get; }
    public string Glyph { get; }
    public IRelayCommand Command { get; }
}

/// <summary>首页常用工具卡片。</summary>
public sealed class ToolCardViewModel
{
    public ToolCardViewModel(string title, string subtitle, string glyph, IRelayCommand command)
    {
        Title = title;
        Subtitle = subtitle;
        Glyph = glyph;
        Command = command;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string Glyph { get; }
    public IRelayCommand Command { get; }
}

/// <summary>首页最近执行行。</summary>
public sealed class ActivityRowViewModel
{
    public ActivityRowViewModel(TaskRecord record)
    {
        Name = record.Name;
        var first = record.Items.FirstOrDefault();
        Sub = first?.FunctionName ?? "批量任务";
        TimeText = record.FinishedAt?.ToString("MM-dd HH:mm") ?? record.StartedAt.ToString("MM-dd HH:mm");
        PillText = record.Result;
        StatusGlyph = record.Result switch
        {
            "成功" => "✓",
            "已取消" => "–",
            _ => "!"
        };
        (PillBrush, PillForeground, DotBrush, DotForeground) = record.Result switch
        {
            "成功" => ((Brush)Brushes.Transparent, Brushes.Transparent, (Brush)new SolidColorBrush(Color.FromRgb(0xE9, 0xF8, 0xF0)), (Brush)new SolidColorBrush(Color.FromRgb(0x2F, 0xA3, 0x6B))),
            "已取消" => ((Brush)Brushes.Transparent, Brushes.Transparent, (Brush)new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF6)), (Brush)new SolidColorBrush(Color.FromRgb(0x66, 0x70, 0x85))),
            _ => ((Brush)new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xDF)), (Brush)new SolidColorBrush(Color.FromRgb(0xD9, 0x89, 0x16)), (Brush)new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xDF)), (Brush)new SolidColorBrush(Color.FromRgb(0xD9, 0x89, 0x16)))
        };
    }

    public string Name { get; }
    public string Sub { get; }
    public string TimeText { get; }
    public string PillText { get; }
    public string StatusGlyph { get; }
    public Brush PillBrush { get; }
    public Brush PillForeground { get; }
    public Brush DotBrush { get; }
    public Brush DotForeground { get; }
}

/// <summary>自定义选择分类卡(跳转系统设置并过滤)。</summary>
public sealed class CategoryCardViewModel
{
    public CategoryCardViewModel(string title, int count, IRelayCommand command)
    {
        Title = title;
        CountText = $"{count} 个项目";
        Command = command;
    }

    public string Title { get; }
    public string CountText { get; }
    public IRelayCommand Command { get; }
}

/// <summary>方案内功能行(勾选状态先于“选择方案”生效)。</summary>
public partial class PresetFeatureRowViewModel : ObservableObject
{
    private readonly SelectionService _selection;

    public PresetFeatureRowViewModel(FunctionItem item, SelectionService selection)
    {
        Item = item;
        _selection = selection;
    }

    public FunctionItem Item { get; }
    public string Name => Item.Name;
    public RiskLevel Risk => Item.Risk;

    [ObservableProperty]
    private bool isSelected = true;

    [RelayCommand]
    private void Toggle()
    {
        IsSelected = !IsSelected;
        if (IsSelected) _selection.Add(Item);
        else _selection.Remove(Item);
    }
}

/// <summary>方案卡。</summary>
public partial class PresetCardViewModel : ObservableObject
{
    private readonly SelectionService _selection;

    public PresetCardViewModel(Preset preset, IFunctionCatalog catalog, SelectionService selection,
        CompatibilityEngine? compat = null)
    {
        Preset = preset;
        _selection = selection;
        Name = preset.Name;
        Description = preset.Description;
        TargetGroup = preset.TargetGroup;
        (PillBrush, PillForeground) = preset.Risk switch
        {
            RiskLevel.Safe => ((Brush)new SolidColorBrush(Color.FromRgb(0xE9, 0xF8, 0xF0)), (Brush)new SolidColorBrush(Color.FromRgb(0x2F, 0xA3, 0x6B))),
            RiskLevel.Caution => ((Brush)new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xDF)), (Brush)new SolidColorBrush(Color.FromRgb(0xD9, 0x89, 0x16))),
            _ => ((Brush)new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xFF)), (Brush)new SolidColorBrush(Color.FromRgb(0x5B, 0x5C, 0xE2)))
        };

        var features = preset.FeatureIds
            .Select(id => catalog.Find(id))
            .Where(f => f is not null)
            .Cast<FunctionItem>()
            .ToArray();
        Features = new System.Collections.ObjectModel.ObservableCollection<PresetFeatureRowViewModel>(
            features.Select(f => new PresetFeatureRowViewModel(f, selection)));
        CountText = $"包含 {Features.Count} 个项目";
        HasMissing = preset.FeatureIds.Count != Features.Count;

        // 兼容性统计(第二代 §4):预设中不兼容/待验证/未知项数量
        if (compat is not null && features.Length > 0)
        {
            var results = features.Select(compat.Evaluate).ToArray();
            UnsupportedCount = results.Count(r => r.Status == CompatibilityStatus.Unsupported);
            NeedsVerificationCount = results.Count(r => r.Status == CompatibilityStatus.NeedsVerification);
            UnknownCount = results.Count(r => r.Status == CompatibilityStatus.Unknown);
            if (UnsupportedCount > 0 || UnknownCount > 0 || NeedsVerificationCount > 0)
            {
                var parts = new List<string>();
                if (UnsupportedCount > 0) parts.Add($"{UnsupportedCount} 项不兼容");
                if (UnknownCount > 0) parts.Add($"{UnknownCount} 项兼容性未知");
                if (NeedsVerificationCount > 0) parts.Add($"{NeedsVerificationCount} 项待验证");
                CompatText = "⚠ " + string.Join(" · ", parts);
            }
        }
    }

    public Preset Preset { get; }
    public string Name { get; }
    public string Description { get; }
    public string TargetGroup { get; }
    public string CountText { get; }
    public bool HasMissing { get; }

    /// <summary>兼容性统计文本(第二代 §4):不兼容/未知/待验证数量。</summary>
    public string CompatText { get; } = "";

    /// <summary>不兼容项数量。</summary>
    public int UnsupportedCount { get; }

    /// <summary>待验证项数量。</summary>
    public int NeedsVerificationCount { get; }

    /// <summary>兼容性未知项数量。</summary>
    public int UnknownCount { get; }
    public Brush PillBrush { get; }
    public Brush PillForeground { get; }
    public System.Collections.ObjectModel.ObservableCollection<PresetFeatureRowViewModel> Features { get; }

    [ObservableProperty]
    private bool isExpanded;

    [RelayCommand]
    private void ToggleDetails() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private void SelectPlan()
    {
        foreach (var f in Features.Where(f => f.IsSelected))
            _selection.Add(f.Item);
    }
}

/// <summary>设置/清理页行(开关 = “选择应用”,非真实状态;支持单项恢复)。</summary>
public partial class SettingRowViewModel : ObservableObject
{
    private readonly SelectionService _selection;
    private readonly NavigationService _nav;

    public SettingRowViewModel(FunctionItem item, SelectionService selection, NavigationService nav,
        CompatibilityEngine? compat = null)
    {
        Item = item;
        _selection = selection;
        _nav = nav;
        var hints = new List<string>();
        if (item.RequiresAdmin) hints.Add("需要管理员权限");
        if (item.Restart == RestartRequirement.ExplorerRestart) hints.Add("需重启资源管理器");
        if (item.Restart == RestartRequirement.Reboot) hints.Add("需重启系统");
        HintText = hints.Count > 0 ? string.Join(" · ", hints) : "普通权限";
        IsHighRisk = item.Risk == RiskLevel.HighRisk;
        RestoreCommandText = RestoreCommandBuilder.Build(item);
        CanRestore = !string.IsNullOrWhiteSpace(RestoreCommandText);

        // 兼容性判定(第二代 §4):不兼容项显示原因
        if (compat is not null)
        {
            var cr = compat.Evaluate(item);
            CompatStatus = cr.Status;
            CompatText = cr.Status switch
            {
                CompatibilityStatus.Supported => "",
                CompatibilityStatus.Unsupported => "不兼容",
                CompatibilityStatus.Unknown => "兼容性未知",
                CompatibilityStatus.NeedsVerification => "待验证",
                _ => ""
            };
            CompatReason = cr.Reason;
            IsUnsupported = cr.Status == CompatibilityStatus.Unsupported;
        }

        // 状态检测:已优化的项自动处于选中状态,并显示“已优化”标识
        var detected = StateDetector.Detect(item);
        if (detected == true)
        {
            IsOptimized = true;
            OptimizedText = "已优化";
            if (!_selection.IsSelected(item))
                _selection.Add(item);
        }
        else if (detected == false)
        {
            IsOptimized = false;
        }
        else
        {
            OptimizedText = "状态未知";
        }
    }

    /// <summary>兼容性状态(第二代 §4)。</summary>
    public CompatibilityStatus CompatStatus { get; }

    /// <summary>兼容性徽标文本(Supported 显示为空)。</summary>
    public string CompatText { get; } = "";

    /// <summary>兼容性原因说明。</summary>
    public string? CompatReason { get; }

    /// <summary>是否不兼容(禁止执行)。</summary>
    public bool IsUnsupported { get; }

    public FunctionItem Item { get; }
    public string Name => Item.Name;
    public string Description => Item.Description;
    public RiskLevel Risk => Item.Risk;
    public string HintText { get; }
    public bool IsHighRisk { get; }
    public string? RestoreCommandText { get; }
    public bool CanRestore { get; }

    /// <summary>当前系统是否已处于优化后状态(状态检测)。</summary>
    public bool IsOptimized { get; }

    /// <summary>状态标识文本:已优化 / 状态未知(空=未优化)。</summary>
    public string OptimizedText { get; } = "";

    public bool IsSelected
    {
        get => _selection.IsSelected(Item);
        set
        {
            if (value) _selection.Add(Item);
            else _selection.Remove(Item);
            OnPropertyChanged();
        }
    }

    /// <summary>撤销该项优化,恢复系统默认值;恢复操作同样写入任务记录。</summary>
    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (string.IsNullOrWhiteSpace(RestoreCommandText)) return;
        var restoreItem = new FunctionItem
        {
            Id = Item.Id + ".restore",
            Name = "恢复:" + Item.Name,
            Category = Item.Category,
            Module = Item.Module,
            Description = "撤销该项优化,删除优化写入的配置并恢复系统默认值。",
            Risk = RiskLevel.Safe,
            RequiresAdmin = Item.RequiresAdmin,
            Restart = Item.Restart,
            Source = "自研 · 恢复命令",
            Kind = ExecutionKind.PowerShell,
            Command = RestoreCommandText,
            TimeoutSeconds = Item.TimeoutSeconds
        };
        await _nav.RunSingleAsync(restoreItem);
    }
}

/// <summary>设置/清理页分组。</summary>
public sealed class SettingGroupViewModel
{
    public SettingGroupViewModel(string title, IEnumerable<SettingRowViewModel> rows)
    {
        Title = title;
        Rows = new System.Collections.ObjectModel.ObservableCollection<SettingRowViewModel>(rows);
    }

    public string Title { get; }
    public System.Collections.ObjectModel.ObservableCollection<SettingRowViewModel> Rows { get; }
}

/// <summary>应用管理页行。</summary>
public partial class AppRowViewModel : ObservableObject
{
    public AppRowViewModel(FunctionItem item, SelectionService selection)
    {
        Item = item;
        _selection = selection;
        PackageName = item.Extra ?? "";
        IconLetter = (item.Name.Length > 0 ? item.Name[..1] : "?").ToUpperInvariant();
        KindText = item.Risk switch
        {
            RiskLevel.Recommended => "推荐删除",
            RiskLevel.Caution => "按需保留",
            _ => "建议保留"
        };
        KindBrush = item.Risk switch
        {
            RiskLevel.Recommended => (Brush)new SolidColorBrush(Color.FromRgb(0x2F, 0xA3, 0x6B)),
            RiskLevel.Caution => (Brush)new SolidColorBrush(Color.FromRgb(0x66, 0x70, 0x85)),
            _ => (Brush)new SolidColorBrush(Color.FromRgb(0x5B, 0x5C, 0xE2))
        };
    }

    private readonly SelectionService _selection;

    public FunctionItem Item { get; }
    public string PackageName { get; }
    public string IconLetter { get; }
    public string KindText { get; }
    public Brush KindBrush { get; }

    [ObservableProperty]
    private string installedText = "待扫描";

    [ObservableProperty]
    private bool isInstalled;

    public bool IsSelected
    {
        get => _selection.IsSelected(Item);
        set
        {
            if (value) _selection.Add(Item);
            else _selection.Remove(Item);
            OnPropertyChanged();
        }
    }

    public void ApplyScanResult(bool installed, bool residual = false)
    {
        IsInstalled = installed;
        InstalledText = installed ? "已安装" : residual ? "残留(未彻底卸载)" : "未安装";
    }
}

/// <summary>修复页卡片。</summary>
public partial class RepairCardViewModel : ObservableObject
{
    private readonly NavigationService _nav;

    public RepairCardViewModel(FunctionItem item, string glyph, NavigationService nav)
    {
        Item = item;
        Glyph = glyph;
        _nav = nav;
        Title = item.Name;
        Description = item.Description;
        MetaText = item.RequiresAdmin ? "需要管理员权限" : "普通权限";
    }

    public FunctionItem Item { get; }
    public string Glyph { get; }
    public string Title { get; }
    public string Description { get; }
    public string MetaText { get; }

    [RelayCommand]
    private async Task RunAsync()
    {
        await _nav.RunSingleAsync(Item);
    }
}

/// <summary>任务记录卡。</summary>
public partial class RecordCardViewModel : ObservableObject
{
    private readonly RollbackService? _rollback;
    private readonly NavigationService? _nav;

    public RecordCardViewModel(TaskRecord record, RollbackService? rollback = null, NavigationService? nav = null)
    {
        Record = record;
        _rollback = rollback;
        _nav = nav;
        TimeText = record.FinishedAt?.ToString("yyyy-MM-dd HH:mm") ?? record.StartedAt.ToString("yyyy-MM-dd HH:mm");
        SummaryText = $"成功 {record.SuccessCount} · 失败 {record.FailedCount} · 取消 {record.CancelledCount}";
        PillBrush = record.Result switch
        {
            "成功" => (Brush)new SolidColorBrush(Color.FromRgb(0xE9, 0xF8, 0xF0)),
            "已取消" => (Brush)new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF6)),
            _ => (Brush)new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xDF))
        };
        PillForeground = record.Result switch
        {
            "成功" => (Brush)new SolidColorBrush(Color.FromRgb(0x2F, 0xA3, 0x6B)),
            "已取消" => (Brush)new SolidColorBrush(Color.FromRgb(0x66, 0x70, 0x85)),
            _ => (Brush)new SolidColorBrush(Color.FromRgb(0xD9, 0x89, 0x16))
        };
        Items = new System.Collections.ObjectModel.ObservableCollection<RecordItemViewModel>(
            record.Items.Select(i => new RecordItemViewModel(i)));

        // 批次回滚分析(第二代 §8):统计可恢复项
        if (_rollback is not null)
        {
            var analysis = _rollback.Analyze(record);
            RestorableCount = analysis.RestorableCount;
            NotRestorableCount = analysis.NotSupportedCount;
            HasRestorable = RestorableCount > 0;
            RollbackSummary = RestorableCount > 0
                ? $"可恢复 {RestorableCount} 项 · 不可恢复 {NotRestorableCount} 项"
                : $"不可恢复 {NotRestorableCount} 项";
        }
    }

    public TaskRecord Record { get; }
    public string Name => Record.Name;
    public string Result => Record.Result;
    public string TimeText { get; }
    public string SummaryText { get; }
    public Brush PillBrush { get; }
    public Brush PillForeground { get; }
    public bool RequiresRestart => Record.RequiresRestart;
    public System.Collections.ObjectModel.ObservableCollection<RecordItemViewModel> Items { get; }

    /// <summary>可恢复项数量(第二代 §8)。</summary>
    public int RestorableCount { get; }

    /// <summary>不可恢复项数量。</summary>
    public int NotRestorableCount { get; }

    /// <summary>是否存在可恢复项。</summary>
    public bool HasRestorable { get; }

    /// <summary>回滚摘要文本。</summary>
    public string RollbackSummary { get; } = "";

    /// <summary>回滚执行中。</summary>
    [ObservableProperty]
    private bool isRollingBack;

    /// <summary>回滚结果文本。</summary>
    [ObservableProperty]
    private string rollbackResultText = "";

    [ObservableProperty]
    private bool isExpanded;

    [RelayCommand]
    private void ToggleDetails() => IsExpanded = !IsExpanded;

    /// <summary>恢复本次任务(批次回滚,第二代 §8):逆序恢复可恢复项,写新任务记录。</summary>
    [RelayCommand]
    private async Task RollbackAsync()
    {
        if (_rollback is null || _nav is null || IsRollingBack) return;
        IsRollingBack = true;
        RollbackResultText = "正在分析并执行回滚…";
        try
        {
            var record = await _rollback.RollbackAsync(Record);
            RollbackResultText = $"回滚完成:成功 {record.SuccessCount} · 失败 {record.FailedCount} · 取消 {record.CancelledCount}";
        }
        catch (Exception ex)
        {
            RollbackResultText = $"回滚失败:{ex.Message}";
        }
        finally
        {
            IsRollingBack = false;
        }
    }
}

/// <summary>任务记录单项。</summary>
public sealed class RecordItemViewModel
{
    public RecordItemViewModel(TaskItemRecord item)
    {
        StatusGlyph = item.Status switch
        {
            "成功" => "✓",
            "失败" => "✗",
            "已取消" => "–",
            "执行中" => "●",
            _ => "○"
        };
        StatusBrush = item.Status switch
        {
            "成功" => (Brush)new SolidColorBrush(Color.FromRgb(0x2F, 0xA3, 0x6B)),
            "失败" => (Brush)new SolidColorBrush(Color.FromRgb(0xD6, 0x4E, 0x4E)),
            "已取消" => (Brush)new SolidColorBrush(Color.FromRgb(0x92, 0x9B, 0xAD)),
            _ => (Brush)new SolidColorBrush(Color.FromRgb(0xA2, 0xA9, 0xB7))
        };
        Name = item.FunctionName;
        Status = item.Status;
        Detail = string.IsNullOrWhiteSpace(item.Error)
            ? (string.IsNullOrWhiteSpace(item.Output) ? "" : Truncate(item.Output, 400))
            : Truncate(item.Error!, 400);
        ExitText = item.ExitCode != 0 ? $"退出码 {item.ExitCode}" : "";
    }

    public string StatusGlyph { get; }
    public Brush StatusBrush { get; }
    public string Name { get; }
    public string Status { get; }
    public string Detail { get; }
    public string ExitText { get; }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";
}

/// <summary>来源台账行(关于页)。</summary>
public sealed class SourceRowViewModel
{
    public SourceRowViewModel(SourceRecord record)
    {
        Record = record;
    }

    public SourceRecord Record { get; }
    public string Project => Record.Project;
    public string License => Record.License;
    public string Functions => Record.Functions;
    public string LocalChanges => Record.LocalChanges;
    public string VerifiedOn => Record.VerifiedOn;
}
