using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
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

    /// <summary>软件安装页签:常用软件入口(官网 / 本地静默安装 / 便携复制)。</summary>
    public IReadOnlyList<SoftwareEntry> SoftwareEntries { get; } = new[]
    {
        new SoftwareEntry("Chrome", "Google 浏览器,官方下载页", "https://www.google.com/chrome/", InstallKind.OpenUrl),
        new SoftwareEntry("Firefox", "Mozilla 浏览器,官方下载页", "https://www.mozilla.org/firefox/", InstallKind.OpenUrl),
        new SoftwareEntry("7-Zip", "开源压缩软件,官方下载页", "https://www.7-zip.org/", InstallKind.OpenUrl),
        new SoftwareEntry("VLC", "开源播放器,官方下载页", "https://www.videolan.org/vlc/", InstallKind.OpenUrl),
        new SoftwareEntry("VS Code", "微软开源代码编辑器,官方下载页", "https://code.visualstudio.com/", InstallKind.OpenUrl),
        new SoftwareEntry("Everything", "本地文件搜索工具,官方下载页", "https://www.voidtools.com/", InstallKind.OpenUrl),
        new SoftwareEntry("PowerToys", "微软系统工具集,官方下载页", "https://learn.microsoft.com/windows/powertoys/", InstallKind.OpenUrl),
        new SoftwareEntry("PotPlayer", "多媒体播放器,官方下载页", "https://potplayer.daum.net/", InstallKind.OpenUrl),

        // 本地安装:StartAllBack(安装包放 %LocalAppData%\NControl\installers\,点击弹出官方向导手动安装)
        new SoftwareEntry("StartAllBack", "Win11 开始菜单/任务栏增强,本地安装包(点击后手动完成安装)", "startallback.com", InstallKind.LaunchInstaller,
            InstallerFile: "StartAllBack_setup.exe"),
        // 本地安装:GeekUninstaller(便携免安装单 exe,优先 D 盘,不可写自动回退 C 盘 + 桌面快捷方式)
        new SoftwareEntry("GeekUninstaller", "轻量卸载工具,便携免安装,优先安装到 D 盘(不可写自动回退 C 盘)并创建桌面快捷方式", "geekuninstaller.com", InstallKind.PortableExtract,
            InstallerFile: "geek.exe", TargetDir: @"D:\Program Files\GeekUninstaller", FallbackDir: @"C:\Program Files\GeekUninstaller", ExeName: "geek.exe")
    };

    /// <summary>安装状态文本。</summary>
    [ObservableProperty]
    private string installStatusText = "";

    /// <summary>安装进行中。</summary>
    [ObservableProperty]
    private bool isInstalling;

    /// <summary>执行软件安装/打开官网。</summary>
    [RelayCommand]
    private async Task InstallSoftwareAsync(SoftwareEntry entry)
    {
        if (entry is null || IsInstalling) return;
        switch (entry.Kind)
        {
            case InstallKind.OpenUrl:
                OpenUrl(entry.Url);
                break;
            case InstallKind.SilentInstaller:
                await SilentInstallAsync(entry);
                break;
            case InstallKind.PortableExtract:
                await PortableInstallAsync(entry);
                break;
            case InstallKind.LaunchInstaller:
                LaunchInstaller(entry);
                break;
        }
    }

    /// <summary>浏览器打开官网。</summary>
    private void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }

    /// <summary>启动本地安装包,弹出官方安装向导由用户手动完成(不静默,避免破解注入风险)。</summary>
    private void LaunchInstaller(SoftwareEntry entry)
    {
        var installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NControl", "installers");
        var installerPath = Path.Combine(installDir, entry.InstallerFile ?? "");
        if (!File.Exists(installerPath))
        {
            InstallStatusText = $"未找到安装包 {entry.InstallerFile},请先将其放入 {installDir}";
            return;
        }
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
            InstallStatusText = $"已启动 {entry.Name} 安装向导,请按提示完成安装";
        }
        catch (Exception ex)
        {
            InstallStatusText = $"启动安装包失败:{ex.Message}";
        }
    }

    /// <summary>静默安装:在后台运行安装器(安装包位于 %LocalAppData%\NControl\installers\)。</summary>
    private async Task SilentInstallAsync(SoftwareEntry entry)
    {
        var installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NControl", "installers");
        var installerPath = Path.Combine(installDir, entry.InstallerFile ?? "");
        if (!File.Exists(installerPath))
        {
            InstallStatusText = $"未找到安装包 {entry.InstallerFile},请先将其放入 {installDir}";
            return;
        }

        IsInstalling = true;
        InstallStatusText = $"正在静默安装 {entry.Name}…";
        string? tempDir = null;
        try
        {
            int exitCode;
            if (entry.ExtractFirst)
            {
                // 7z SFX 安装包:先解压,再运行内部 exe(Repack 版 SFX 外壳不响应静默参数)
                var sevenZip = Path.Combine(installDir, "7zr.exe");
                if (!File.Exists(sevenZip))
                {
                    InstallStatusText = $"缺少解压工具 7zr.exe,请放入 {installDir}";
                    return;
                }
                tempDir = Path.Combine(Path.GetTempPath(), "nctl_install_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                var extractPsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sevenZip,
                    Arguments = $"x \"{installerPath}\" -o\"{tempDir}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var extractProc = System.Diagnostics.Process.Start(extractPsi)!;
                await extractProc.WaitForExitAsync();
                if (extractProc.ExitCode != 0)
                {
                    InstallStatusText = $"{entry.Name} 解压失败(7z 退出码 {extractProc.ExitCode})";
                    return;
                }

                // 定位内部 exe(可能在子目录,递归搜索)
                var innerExe = entry.InnerExe ?? "";
                var innerPath = Path.Combine(tempDir, innerExe);
                if (!File.Exists(innerPath))
                {
                    innerPath = Directory.GetFiles(tempDir, innerExe, SearchOption.AllDirectories).FirstOrDefault() ?? innerPath;
                }
                if (!File.Exists(innerPath))
                {
                    InstallStatusText = $"{entry.Name} 内部安装程序 {innerExe} 未找到";
                    return;
                }

                var innerPsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = innerPath,
                    Arguments = entry.InnerArgs ?? "",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var innerProc = System.Diagnostics.Process.Start(innerPsi)!;
                await innerProc.WaitForExitAsync();
                exitCode = innerProc.ExitCode;
            }
            else
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                // 静默参数(如 /S)
                if (!string.IsNullOrEmpty(entry.SilentArgs)) psi.Arguments = entry.SilentArgs;
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc is null) { InstallStatusText = $"{entry.Name} 启动失败"; return; }
                await proc.WaitForExitAsync();
                exitCode = proc.ExitCode;
            }
            InstallStatusText = $"{entry.Name} 安装完成(退出码 {exitCode})";
        }
        catch (Exception ex)
        {
            InstallStatusText = $"{entry.Name} 安装失败:{ex.Message}";
        }
        finally
        {
            // 清理临时解压目录
            if (tempDir is not null)
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
            IsInstalling = false;
        }
    }

    /// <summary>便携安装:解压 zip 到目标目录 + 创建桌面快捷方式。</summary>
    private async Task PortableInstallAsync(SoftwareEntry entry)
    {
        var installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NControl", "installers");
        var zipPath = Path.Combine(installDir, entry.InstallerFile ?? "");
        if (!File.Exists(zipPath))
        {
            InstallStatusText = $"未找到安装包 {entry.InstallerFile},请先将其放入 {installDir}";
            return;
        }

        IsInstalling = true;
        InstallStatusText = $"正在安装 {entry.Name}…";
        try
        {
            // 1. 目标目录(防呆:优先指定盘,不可写自动回退备用目录)
            var targetDir = ResolveTargetDir(entry.TargetDir, entry.FallbackDir, entry.Name);
            Directory.CreateDirectory(targetDir);
            InstallStatusText = $"正在安装 {entry.Name} 到 {targetDir}…";

            // 2. 安装源:zip 解压 / 单 exe 直接复制
            var isZip = (entry.InstallerFile ?? "").EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            if (isZip)
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);
            }
            else
            {
                var destPath = Path.Combine(targetDir, Path.GetFileName(entry.InstallerFile!));
                File.Copy(zipPath, destPath, overwrite: true);
            }

            // 3. 定位 exe
            var exePath = Path.Combine(targetDir, entry.ExeName ?? "geek.exe");
            if (!File.Exists(exePath))
            {
                // 可能 zip 里有子目录,搜索
                var found = Directory.GetFiles(targetDir, entry.ExeName ?? "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                exePath = found ?? exePath;
            }

            // 4. 桌面快捷方式
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var lnkPath = Path.Combine(desktop, entry.Name + ".lnk");
            CreateShortcut(lnkPath, exePath);

            InstallStatusText = $"{entry.Name} 安装完成:{exePath} 已创建桌面快捷方式";
        }
        catch (Exception ex)
        {
            InstallStatusText = $"{entry.Name} 安装失败:{ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    /// <summary>
    /// 防呆解析安装目标目录:优先首选目录(如 D 盘),所在盘不可写时回退备用目录(如 C 盘),
    /// 再失败回退用户 LocalAppData\Programs。
    /// </summary>
    private static string ResolveTargetDir(string? preferred, string? fallback, string appName)
    {
        var candidates = new List<string?>();
        if (!string.IsNullOrWhiteSpace(preferred)) candidates.Add(preferred);
        if (!string.IsNullOrWhiteSpace(fallback)) candidates.Add(fallback);
        candidates.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", appName));

        foreach (var dir in candidates.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            if (IsDirWritable(dir!)) return dir!;
        }
        // 全部不可写:返回首选(让调用方报错)
        return preferred ?? fallback ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", appName);
    }

    /// <summary>检测目录是否可写:盘为固定磁盘且可创建/写入测试文件。</summary>
    private static bool IsDirWritable(string dir)
    {
        try
        {
            // 盘类型检查:固定磁盘(3)才考虑;光盘/网络盘/可移动盘跳过
            var root = Path.GetPathRoot(dir);
            if (!string.IsNullOrEmpty(root) && root.Length >= 2 && root[1] == ':')
            {
                var drive = System.IO.DriveInfo.GetDrives()
                    .FirstOrDefault(d => string.Equals(d.RootDirectory.FullName, root, StringComparison.OrdinalIgnoreCase));
                if (drive is not null && drive.DriveType != System.IO.DriveType.Fixed)
                    return false;
            }

            Directory.CreateDirectory(dir);
            var probe = Path.Combine(dir, $".nctl_write_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "test");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>创建 .lnk 快捷方式(COM WScript.Shell)。</summary>
    private static void CreateShortcut(string lnkPath, string targetPath)
    {
        if (!File.Exists(targetPath)) return;
        var shell = new COMObj();
        shell.CreateShortcut(lnkPath, targetPath);
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

/// <summary>安装类型:官网入口 / 本地静默安装 / 便携解压复制 / 本地安装包手动安装。</summary>
public enum InstallKind
{
    /// <summary>浏览器打开官网下载页。</summary>
    OpenUrl,

    /// <summary>本地安装器静默安装(后台运行,带静默参数)。</summary>
    SilentInstaller,

    /// <summary>便携版:解压 zip 到目标目录 + 创建桌面快捷方式。</summary>
    PortableExtract,

    /// <summary>本地安装包:弹出官方安装向导,由用户手动完成(不静默)。</summary>
    LaunchInstaller
}

/// <summary>软件安装页条目:软件名 + 说明 + 入口(官网地址/本地安装包)。</summary>
public sealed record SoftwareEntry(
    string Name,
    string Description,
    string Url,
    InstallKind Kind = InstallKind.OpenUrl,
    string? InstallerFile = null,
    string? SilentArgs = null,
    string? TargetDir = null,
    string? FallbackDir = null,
    string? ExeName = null,
    bool ExtractFirst = false,
    string? InnerExe = null,
    string? InnerArgs = null)
{
    /// <summary>按钮文本:官网入口显示"打开官网",本地安装显示"安装"。</summary>
    public string ButtonText => Kind == InstallKind.OpenUrl ? "打开官网" : "安装";
}

/// <summary>COM 辅助:通过 WScript.Shell 创建 .lnk 快捷方式。</summary>
internal sealed class COMObj
{
    public void CreateShortcut(string lnkPath, string targetPath)
    {
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
        dynamic sc = shell.CreateShortcut(lnkPath);
        sc.TargetPath = targetPath;
        sc.WorkingDirectory = Path.GetDirectoryName(targetPath);
        sc.Save();
    }
}
