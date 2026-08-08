using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 应用管理页:预装应用/已安装软件/Windows 组件/软件安装(产品文档 §6.4)。
/// 默认不选择 Microsoft Store、关键运行库和系统依赖;卸载前显示实际选中列表。
/// </summary>
public partial class AppsViewModel : ObservableObject
{
    private readonly IFunctionCatalog _catalog;
    private readonly IExecutionProvider _powerShell;

    public AppsViewModel(
        IFunctionCatalog catalog,
        SelectionService selection,
        IEnumerable<IExecutionProvider> providers)
    {
        _catalog = catalog;
        _powerShell = providers.First(p => p.CanHandle(ExecutionKind.PowerShell));

        foreach (var item in catalog.ByModule(ModuleKind.Applications))
            Rows.Add(new AppRowViewModel(item, selection));

        var preset = catalog.Presets.FirstOrDefault(p => p.Id == "preset.apps.recommended");
        if (preset is not null)
            BloatPreset = new PresetCardViewModel(preset, catalog, selection);
    }

    public ObservableCollection<AppRowViewModel> Rows { get; } = new();
    public PresetCardViewModel? BloatPreset { get; }

    [ObservableProperty]
    private int activeTab;

    [ObservableProperty]
    private string scanStateText = "尚未扫描当前系统";

    [ObservableProperty]
    private bool isScanning;

    [RelayCommand]
    private void SelectTab(string index)
    {
        if (int.TryParse(index, out var i) && i is >= 0 and <= 3)
            ActiveTab = i;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;
        ScanStateText = "正在扫描当前系统的预装应用…";

        try
        {
            var probe = new FunctionItem
            {
                Id = "probe.appx-list",
                Name = "扫描预装应用",
                Category = "预装应用",
                Module = ModuleKind.Applications,
                Description = "",
                Risk = RiskLevel.Safe,
                RequiresAdmin = false,
                Restart = RestartRequirement.None,
                Source = "系统命令",
                Kind = ExecutionKind.PowerShell,
                Command = "Get-AppxPackage | ForEach-Object { $_.Name }",
                TimeoutSeconds = 120
            };

            var result = await _powerShell.ExecuteAsync(probe, null, CancellationToken.None);
            var installed = new HashSet<string>(
                (result.Output ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);

            int found = 0;
            foreach (var row in Rows)
            {
                var isInstalled = installed.Contains(row.PackageName);
                row.ApplyScanResult(isInstalled);
                if (isInstalled) found++;
            }
            ScanStateText = $"扫描完成:检测到 {found} 个已安装,其余未安装或已被移除";
        }
        catch (Exception ex)
        {
            ScanStateText = $"扫描失败:{ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
