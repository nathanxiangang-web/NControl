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

    /// <summary>软件安装页签:常用软件官网入口(点击打开浏览器)。</summary>
    public IReadOnlyList<SoftwareEntry> SoftwareEntries { get; } = new[]
    {
        new SoftwareEntry("Chrome", "Google 浏览器,官方下载页", "https://www.google.com/chrome/"),
        new SoftwareEntry("Firefox", "Mozilla 浏览器,官方下载页", "https://www.mozilla.org/firefox/"),
        new SoftwareEntry("7-Zip", "开源压缩软件,官方下载页", "https://www.7-zip.org/"),
        new SoftwareEntry("VLC", "开源播放器,官方下载页", "https://www.videolan.org/vlc/"),
        new SoftwareEntry("VS Code", "微软开源代码编辑器,官方下载页", "https://code.visualstudio.com/"),
        new SoftwareEntry("Everything", "本地文件搜索工具,官方下载页", "https://www.voidtools.com/"),
        new SoftwareEntry("PowerToys", "微软系统工具集,官方下载页", "https://learn.microsoft.com/windows/powertoys/"),
        new SoftwareEntry("PotPlayer", "多媒体播放器,官方下载页", "https://potplayer.daum.net/")
    };

    /// <summary>打开软件官网(浏览器)。</summary>
    [RelayCommand]
    private void OpenSoftware(SoftwareEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Url)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = entry.Url,
                UseShellExecute = true
            });
        }
        catch
        {
            // 打开失败静默(浏览器不可用时)
        }
    }

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

/// <summary>软件安装页签条目:软件名 + 说明 + 官方下载地址。</summary>
public sealed record SoftwareEntry(string Name, string Description, string Url);
