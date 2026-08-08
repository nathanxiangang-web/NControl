using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 清理维护页:按来源分组(产品文档 §6.5)。
/// 第一代不显示虚构的空间数字,提供固定清理动作;明确区分可安全清理与谨慎项。
/// </summary>
public partial class CleanupViewModel : ObservableObject
{
    public CleanupViewModel(IFunctionCatalog catalog, SelectionService selection)
    {
        foreach (var group in catalog.ByModule(ModuleKind.Cleanup).GroupBy(f => f.Category))
            Groups.Add(new SettingGroupViewModel(
                group.Key,
                group.Select(f => new SettingRowViewModel(f, selection))));
    }

    public ObservableCollection<SettingGroupViewModel> Groups { get; } = new();

    /// <summary>静态说明卡:诚实表达,不编造大小。</summary>
    public IReadOnlyList<CleanupSummaryCard> SummaryCards { get; } = new[]
    {
        new CleanupSummaryCard("系统临时文件", "用户/系统临时目录与预读取文件;清理后可释放磁盘空间。"),
        new CleanupSummaryCard("更新残留", "已下载但不再需要的更新文件;不影响已安装更新。"),
        new CleanupSummaryCard("应用缓存", "缩略图、着色器与浏览器缓存;会在需要时重新生成。"),
        new CleanupSummaryCard("日志与回收站", "错误报告与回收站内容;清理后可能影响近期故障排错。")
    };
}

public sealed record CleanupSummaryCard(string Title, string Description);
