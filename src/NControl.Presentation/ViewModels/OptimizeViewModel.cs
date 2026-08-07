using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>一键优化页:方案卡片 + 自定义分类(产品文档 §6.2)。深度优化不作为默认选项。</summary>
public partial class OptimizeViewModel : ObservableObject
{
    public OptimizeViewModel(IFunctionCatalog catalog, SelectionService selection, NavigationService nav,
        CompatibilityEngine? compat = null)
    {
        compat ??= new CompatibilityEngine(new Infrastructure.WindowsEnvironmentProbe());
        foreach (var preset in catalog.Presets.Where(p => !p.Id.StartsWith("preset.apps.")))
            Presets.Add(new PresetCardViewModel(preset, catalog, selection, compat));

        foreach (var group in catalog.ByModule(ModuleKind.Optimization)
                     .GroupBy(f => f.Category))
        {
            var category = group.Key;
            var count = group.Count();
            Categories.Add(new CategoryCardViewModel(
                category, count,
                new RelayCommand(() => nav.Navigate("settings", category))));
        }
    }

    public ObservableCollection<PresetCardViewModel> Presets { get; } = new();
    public ObservableCollection<CategoryCardViewModel> Categories { get; } = new();
}
