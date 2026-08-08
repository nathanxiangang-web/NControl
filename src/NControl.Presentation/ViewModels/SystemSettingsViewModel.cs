using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 系统设置页:分类标签 + 分组设置(产品文档 §6.3)。
/// 第一代状态未知,用“选择应用”表达,不假装显示真实开关状态。
/// 高风险项目仅出现在“高级”分类。
/// </summary>
public partial class SystemSettingsViewModel : ObservableObject
{
    private readonly IFunctionCatalog _catalog;
    private readonly SelectionService _selection;
    private readonly NavigationService _nav;

    public SystemSettingsViewModel(IFunctionCatalog catalog, SelectionService selection, NavigationService nav)
    {
        _catalog = catalog;
        _selection = selection;

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
            .Where(f => SelectedCategory == "全部" || f.Category == SelectedCategory)
            .Where(f => SelectedCategory == "高级" || f.Risk != RiskLevel.HighRisk)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToArray();

        foreach (var group in items.GroupBy(f => f.Category))
            Groups.Add(new SettingGroupViewModel(
                group.Key,
                group.Select(f => new SettingRowViewModel(f, _selection, _nav))));
    }
}
