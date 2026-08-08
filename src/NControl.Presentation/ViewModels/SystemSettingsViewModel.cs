using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 系统设置页:分类标签 + 分组设置(产品文档 §6.3)。
/// 分类与《ZyperWin++ 当前功能统计表》对齐(7 个分类)。
/// 状态未知时用“选择应用”表达,不假装显示真实开关状态。
/// 高风险项目仅在其所属分类中显示,不混入“全部”视图,也不进任何预设。
/// </summary>
public partial class SystemSettingsViewModel : ObservableObject
{
    private readonly IFunctionCatalog _catalog;
    private readonly SelectionService _selection;
    private readonly NavigationService _nav;
    private readonly CompatibilityEngine _compat;

    public SystemSettingsViewModel(IFunctionCatalog catalog, SelectionService selection, NavigationService nav,
        CompatibilityEngine? compat = null)
    {
        _catalog = catalog;
        _selection = selection;
        _compat = compat ?? new CompatibilityEngine(new Infrastructure.WindowsEnvironmentProbe());

        Chips.Add(new ChipItem("全部", true));
        foreach (var category in catalog.ByModule(ModuleKind.Optimization).Select(f => f.Category).Distinct())
            Chips.Add(new ChipItem(category, false));

        _nav = nav;
        Rebuild();
    }

    public ObservableCollection<ChipItem> Chips { get; } = new();
    public ObservableCollection<SettingGroupViewModel> Groups { get; } = new();

    [ObservableProperty]
    private string selectedCategory = "全部";

    [RelayCommand]
    private void SelectChip(ChipItem chip)
    {
        SelectedCategory = chip.Title;
        foreach (var c in Chips) c.IsActive = c == chip;
        Rebuild();
    }

    /// <summary>
    /// 全部重新执行:把当前分类视图中的所有项(含已优化的)加入选择队列,底部确认后统一重新执行。
    /// "全部"视图=全部非高风险项;选中具体分类=该分类全部项(含高风险)。
    /// </summary>
    [RelayCommand]
    private void SelectAllForReapply()
    {
        var items = _catalog.ByModule(ModuleKind.Optimization)
            .Where(f => SelectedCategory == "全部"
                ? f.Risk != RiskLevel.HighRisk
                : f.Category == SelectedCategory)
            .ToArray();
        if (items.Length == 0)
        {
            ReapplyStatusText = "当前分类下没有可执行项";
            return;
        }
        _selection.AddRange(items);
        ReapplyStatusText = $"已加入 {items.Length} 项(含已优化的)到选择队列,底部确认后统一重新执行";
    }

    [ObservableProperty]
    private string reapplyStatusText = "";

    public void ApplyCategory(string category)
    {
        var chip = Chips.FirstOrDefault(c => c.Title == category);
        SelectedCategory = chip?.Title ?? "全部";
        foreach (var c in Chips) c.IsActive = c == chip || (chip is null && c.Title == "全部");
        Rebuild();
    }

    private void Rebuild()
    {
        Groups.Clear();

        var items = _catalog.ByModule(ModuleKind.Optimization)
            .Where(f => SelectedCategory == "全部"
                ? f.Risk != RiskLevel.HighRisk
                : f.Category == SelectedCategory)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToArray();

        foreach (var group in items.GroupBy(f => f.Category))
            Groups.Add(new SettingGroupViewModel(
                group.Key,
                group.Select(f => new SettingRowViewModel(f, _selection, _nav, _compat))));
    }
}
