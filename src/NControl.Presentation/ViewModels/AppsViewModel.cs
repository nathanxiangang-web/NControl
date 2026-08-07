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
        IEnumerable<IExecutionProvider> providers,
        CompatibilityEngine? compat = null)
    {
        _catalog = catalog;
        _powerShell = providers.First(p => p.CanHandle(ExecutionKind.PowerShell));
        _compat = compat ?? new CompatibilityEngine(new Infrastructure.WindowsEnvironmentProbe());

        foreach (var item in catalog.ByModule(ModuleKind.Applications).Where(f => f.Category == "预装应用"))
            Rows.Add(new AppRowViewModel(item, selection));

        var preset = catalog.Presets.FirstOrDefault(p => p.Id == "preset.apps.recommended");
        if (preset is not null)
            BloatPreset = new PresetCardViewModel(preset, catalog, selection);
    }

    private readonly CompatibilityEngine _compat;

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
            // 扫描所有用户注册(提权下可查 AllUsers):区分 已安装/仅残留/未安装
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
                Command = "$cur = Get-AppxPackage | ForEach-Object { $_.Name }; $all = Get-AppxPackage -AllUsers | ForEach-Object { $_.Name }; @{Current=($cur | Sort-Object -Unique); All=($all | Sort-Object -Unique)} | ConvertTo-Json -Compress",
                TimeoutSeconds = 120
            };

            var result = await _powerShell.ExecuteAsync(probe, null, CancellationToken.None);
            var output = (result.Output ?? "").Trim();
            var jsonStart = output.IndexOf('{');
            var json = jsonStart >= 0 ? output[jsonStart..] : "";

            var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(json))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                foreach (var n in root.GetProperty("Current").EnumerateArray()) current.Add(n.GetString() ?? "");
                foreach (var n in root.GetProperty("All").EnumerateArray()) allUsers.Add(n.GetString() ?? "");
            }

            int found = 0, residual = 0;
            foreach (var row in Rows)
            {
                var isInstalled = current.Contains(row.PackageName);
                var isResidual = !isInstalled && allUsers.Contains(row.PackageName);
                row.ApplyScanResult(isInstalled, isResidual);
                if (isInstalled) found++;
                if (isResidual) residual++;
            }
            ScanStateText = residual > 0
                ? $"扫描完成:检测到 {found} 个已安装,{residual} 个存在残留(未彻底卸载),其余未安装"
                : $"扫描完成:检测到 {found} 个已安装,其余未安装或已被移除";
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
