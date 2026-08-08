using Microsoft.Extensions.Configuration;

namespace NControl.Infrastructure;

/// <summary>应用数据路径。默认 %LocalAppData%\NControl,可在 appsettings.json 的 NControl:DataFolder 覆盖。</summary>
public sealed class AppPaths
{
    public string DataFolder { get; }
    public string DatabasePath { get; }

    public AppPaths(IConfiguration configuration)
    {
        var configured = configuration["NControl:DataFolder"];
        DataFolder = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NControl")
            : Path.GetFullPath(configured);

        Directory.CreateDirectory(DataFolder);
        DatabasePath = Path.Combine(DataFolder, "ncontrol.db");
    }
}
