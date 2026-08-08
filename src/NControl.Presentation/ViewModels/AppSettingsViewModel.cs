using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Infrastructure;

namespace NControl.Presentation.ViewModels;

/// <summary>应用设置页:数据目录与基础信息。</summary>
public partial class AppSettingsViewModel : ObservableObject
{
    public AppSettingsViewModel(AppPaths paths)
    {
        DataFolder = paths.DataFolder;
        DatabasePath = paths.DatabasePath;
    }

    public string DataFolder { get; }
    public string DatabasePath { get; }
    public string Version => typeof(AppSettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "2.0.0";

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{DataFolder}\"") { UseShellExecute = true });
        }
        catch
        {
            // 打不开目录时静默失败
        }
    }
}
