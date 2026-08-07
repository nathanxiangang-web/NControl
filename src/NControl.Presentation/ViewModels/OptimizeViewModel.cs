using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 一键优化页:方案卡片 + 自定义分类 + 我的方案(第二代 §5-§6)。
/// 支持:保存当前选择为方案 / 导出方案 / 从配置导入(预览确认后执行)。
/// 配置只引用已登记功能,不携带任意代码。
/// </summary>
public partial class OptimizeViewModel : ObservableObject
{
    private readonly IFunctionCatalog _catalog;
    private readonly SelectionService _selection;
    private readonly NavigationService _nav;
    private readonly IPlanService _plans;
    private readonly CompatibilityEngine _compat;

    public OptimizeViewModel(IFunctionCatalog catalog, SelectionService selection, NavigationService nav,
        IPlanService? plans = null, CompatibilityEngine? compat = null)
    {
        _catalog = catalog;
        _selection = selection;
        _nav = nav;
        _plans = plans ?? throw new ArgumentNullException(nameof(plans), "DI 应注入 IPlanService");
        compat ??= new CompatibilityEngine(new Infrastructure.WindowsEnvironmentProbe());
        _compat = compat;

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

        RefreshMyPlans();
    }

    public ObservableCollection<PresetCardViewModel> Presets { get; } = new();
    public ObservableCollection<CategoryCardViewModel> Categories { get; } = new();
    public ObservableCollection<PlanCardViewModel> MyPlans { get; } = new();

    [ObservableProperty]
    private string planStatusText = "";

    [ObservableProperty]
    private string planNameInput = "";

    private void RefreshMyPlans()
    {
        MyPlans.Clear();
        foreach (var plan in _plans.GetAll())
            MyPlans.Add(new PlanCardViewModel(plan, _catalog, _selection, _compat));
    }

    /// <summary>把当前已选择的功能保存为方案。</summary>
    [RelayCommand]
    private void SavePlan()
    {
        var name = PlanNameInput.Trim();
        if (string.IsNullOrEmpty(name))
        {
            PlanStatusText = "请先输入方案名称";
            return;
        }
        if (_selection.Count == 0)
        {
            PlanStatusText = "当前没有已选择的功能,无法保存";
            return;
        }

        var plan = new PlanConfig
        {
            Name = name,
            Description = "用户自建方案",
            Functions = _selection.Selected.Select(f => f.Id).Distinct().ToList()
        };
        _plans.Save(plan);
        PlanNameInput = "";
        PlanStatusText = $"已保存方案「{name}」({plan.Functions.Count} 项)";
        RefreshMyPlans();
    }

    /// <summary>导出当前选择为 JSON 配置文件。</summary>
    [RelayCommand]
    private void ExportPlan()
    {
        if (_selection.Count == 0)
        {
            PlanStatusText = "当前没有已选择的功能,无法导出";
            return;
        }
        var dialog = new SaveFileDialog
        {
            Title = "导出配置方案",
            Filter = "NControl 配置文件 (*.json)|*.json",
            FileName = $"ncontrol-plan-{DateTime.Now:yyyyMMdd-HHmm}.json"
        };
        if (dialog.ShowDialog() == true)
        {
            var plan = new PlanConfig
            {
                Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                Description = "从 NControl 导出的配置方案",
                Functions = _selection.Selected.Select(f => f.Id).Distinct().ToList()
            };
            _plans.Export(plan, dialog.FileName);
            PlanStatusText = $"已导出到 {dialog.FileName}";
        }
    }

    /// <summary>从 JSON 配置文件导入方案:解析 → 匹配 → 兼容性/风险检查 → 预览确认。</summary>
    [RelayCommand]
    private async Task ImportPlanAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入配置方案",
            Filter = "NControl 配置文件 (*.json)|*.json"
        };
        if (dialog.ShowDialog() != true) return;

        var result = _plans.Import(dialog.FileName, _catalog, _compat);
        if (!result.ParseOk)
        {
            PlanStatusText = $"导入失败:{result.ParseError}";
            return;
        }

        // 预览摘要
        var lines = new List<string>
        {
            $"配置「{result.Config!.Name}」解析成功",
            $"  可执行:{result.Ready.Count} 项"
        };
        if (result.Unsupported.Count > 0) lines.Add($"  不兼容(禁止执行):{result.Unsupported.Count} 项");
        if (result.HighRisk.Count > 0) lines.Add($"  高风险(不默认选择):{result.HighRisk.Count} 项");
        if (result.UnknownIds.Count > 0) lines.Add($"  未知功能(跳过):{result.UnknownIds.Count} 项");
        if (result.DuplicateCount > 0) lines.Add($"  重复项已去重:{result.DuplicateCount} 项");
        if (result.SchemaHint is not null) lines.Add($"  提示:{result.SchemaHint}");
        if (result.ParseError is not null) lines.Add($"  注意:{result.ParseError}");
        PlanStatusText = string.Join("\n", lines);

        // 只把可执行项加入选择(不兼容/高风险/未知不自动加入)
        var readyItems = result.Ready
            .Select(r => _catalog.Find(r.FunctionId))
            .Where(f => f is not null)
            .Cast<FunctionItem>()
            .ToArray();
        if (readyItems.Length == 0)
        {
            PlanStatusText += "\n没有可执行的项,未加入选择";
            return;
        }

        _selection.AddRange(readyItems);
        PlanStatusText += $"\n已加入 {readyItems.Length} 项到选择队列,请查看底部执行栏确认后执行";
        _nav.Navigate("optimize", null);
    }

    /// <summary>加载方案到选择队列。</summary>
    [RelayCommand]
    private void LoadPlan(PlanCardViewModel plan)
    {
        var items = plan.Config.Functions
            .Select(id => _catalog.Find(id))
            .Where(f => f is not null)
            .Cast<FunctionItem>()
            .ToArray();
        if (items.Length == 0)
        {
            PlanStatusText = $"方案「{plan.Config.Name}」没有可执行的功能项";
            return;
        }
        _selection.AddRange(items);
        PlanStatusText = $"已加载方案「{plan.Config.Name}」({items.Length} 项)到选择队列";
    }

    /// <summary>删除方案。</summary>
    [RelayCommand]
    private void DeletePlan(PlanCardViewModel plan)
    {
        _plans.Delete(plan.Config.Name);
        PlanStatusText = $"已删除方案「{plan.Config.Name}」";
        RefreshMyPlans();
    }
}

/// <summary>我的方案卡片。</summary>
public partial class PlanCardViewModel : ObservableObject
{
    public PlanCardViewModel(PlanConfig config, IFunctionCatalog catalog, SelectionService selection,
        CompatibilityEngine compat)
    {
        Config = config;
        var items = config.Functions
            .Select(id => catalog.Find(id))
            .Where(f => f is not null)
            .Cast<FunctionItem>()
            .ToArray();
        CountText = $"包含 {items.Length} 项";
        var unknown = config.Functions.Count - items.Length;
        if (unknown > 0) CountText += $"(其中 {unknown} 项已不在目录中)";

        var results = items.Select(compat.Evaluate).ToArray();
        UnsupportedCount = results.Count(r => r.Status == CompatibilityStatus.Unsupported);
        if (UnsupportedCount > 0)
            CompatText = $"⚠ {UnsupportedCount} 项在当前系统不兼容";
    }

    public PlanConfig Config { get; }
    public string Name => Config.Name;
    public string Description => Config.Description;
    public string CountText { get; }
    public string CompatText { get; } = "";
    public int UnsupportedCount { get; }
}
