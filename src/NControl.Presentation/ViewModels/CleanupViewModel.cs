using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 清理维护页:按来源分组 + 扫描统计(第二代 §9)。
/// 只展示真实扫描得到的文件数、大小与分类汇总,不显示虚构空间数字。
/// </summary>
public partial class CleanupViewModel : ObservableObject
{
    private readonly CompatibilityEngine _compat;
    private readonly ICleanupScanner _scanner;
    private readonly IFunctionCatalog _catalog;

    public CleanupViewModel(IFunctionCatalog catalog, SelectionService selection, NavigationService nav,
        ICleanupScanner? scanner = null, CompatibilityEngine? compat = null)
    {
        _catalog = catalog;
        _compat = compat ?? new CompatibilityEngine(new Infrastructure.WindowsEnvironmentProbe());
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner), "DI 应注入 ICleanupScanner");
        foreach (var group in catalog.ByModule(ModuleKind.Cleanup).GroupBy(f => f.Category))
            Groups.Add(new SettingGroupViewModel(
                group.Key,
                group.Select(f => new SettingRowViewModel(f, selection, nav, _compat))));
    }

    public ObservableCollection<SettingGroupViewModel> Groups { get; } = new();

    /// <summary>扫描结果(按清理项)。</summary>
    public ObservableCollection<CleanupScanRowViewModel> ScanRows { get; } = new();

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private string scanProgressText = "尚未扫描";

    [ObservableProperty]
    private string scanSummaryText = "";

    /// <summary>扫描所有可扫描的清理项。</summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;
        ScanProgressText = "正在扫描…";
        ScanRows.Clear();
        ScanSummaryText = "";

        try
        {
            var items = _catalog.ByModule(ModuleKind.Cleanup).ToArray();
            var progress = new Progress<string>(t => ScanProgressText = t);
            var results = await _scanner.ScanManyAsync(items, progress);

            foreach (var r in results)
                ScanRows.Add(new CleanupScanRowViewModel(r));

            long totalBytes = results.Where(r => r.Ok).Sum(r => r.SizeBytes);
            long totalCount = results.Where(r => r.Ok).Sum(r => r.ItemCount);
            ScanSummaryText = results.Count == 0
                ? "没有可扫描的清理项"
                : $"扫描完成:共 {results.Count} 项,可清理 {CleanupScanItem.FormatSize(totalBytes)}({totalCount} 个文件/条目)";
        }
        catch (Exception ex)
        {
            ScanSummaryText = $"扫描失败:{ex.Message}";
        }
        finally
        {
            IsScanning = false;
            ScanProgressText = IsScanning ? ScanProgressText : ScanProgressText;
        }
    }

    /// <summary>静态说明卡。</summary>
    public IReadOnlyList<CleanupSummaryCard> SummaryCards { get; } = new[]
    {
        new CleanupSummaryCard("系统临时文件", "用户/系统临时目录与预读取文件;清理后可释放磁盘空间。"),
        new CleanupSummaryCard("更新残留", "已下载但不再需要的更新文件;不影响已安装更新。"),
        new CleanupSummaryCard("应用缓存", "缩略图、着色器与浏览器缓存;会在需要时重新生成。"),
        new CleanupSummaryCard("日志与回收站", "错误报告与回收站内容;清理后可能影响近期故障排错。")
    };
}

public sealed record CleanupSummaryCard(string Title, string Description);

/// <summary>扫描结果行。</summary>
public sealed class CleanupScanRowViewModel
{
    public CleanupScanRowViewModel(CleanupScanItem item)
    {
        Name = item.Name;
        Category = item.Category;
        SizeText = item.SizeText;
        CountText = item.Note is not null
            ? item.Note
            : $"{item.ItemCount} 个文件/条目";
        IsOk = item.Ok;
    }

    public string Name { get; }
    public string Category { get; }
    public string SizeText { get; }
    public string CountText { get; }
    public bool IsOk { get; }
}
