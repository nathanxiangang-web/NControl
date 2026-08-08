using NControl.Core;

namespace NControl.Modules.Applications;

/// <summary>
/// 应用模块:预装应用、已安装软件、Windows 组件和软件入口(产品文档 §3.3)。
/// 第一代覆盖预装应用(Appx);默认不选择 Microsoft Store、关键运行库和系统依赖。
/// </summary>
public sealed class ApplicationsModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "应用模块";

    public void RegisterFeatures(IFunctionCatalog catalog)
    {
        catalog.Register(F("apps.clipchamp", "Clipchamp", "预装应用",
            "Microsoft 的视频编辑器。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.Clipchamp",
            "Get-AppxPackage -Name 'Microsoft.Clipchamp' | Remove-AppxPackage"));
        catalog.Register(F("apps.bing-news", "Microsoft News(资讯)", "预装应用",
            "新闻资讯应用。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.BingNews",
            "Get-AppxPackage -Name 'Microsoft.BingNews' | Remove-AppxPackage"));
        catalog.Register(F("apps.bing-weather", "天气", "预装应用",
            "天气应用。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.BingWeather",
            "Get-AppxPackage -Name 'Microsoft.BingWeather' | Remove-AppxPackage"));
        catalog.Register(F("apps.gethelp", "获取帮助", "预装应用",
            "Microsoft 帮助应用。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.GetHelp",
            "Get-AppxPackage -Name 'Microsoft.GetHelp' | Remove-AppxPackage"));
        catalog.Register(F("apps.feedback-hub", "反馈中心", "预装应用",
            "向 Microsoft 提交反馈的应用。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.WindowsFeedbackHub",
            "Get-AppxPackage -Name 'Microsoft.WindowsFeedbackHub' | Remove-AppxPackage"));
        catalog.Register(F("apps.solitaire", "纸牌游戏合集", "预装应用",
            "Microsoft Solitaire Collection 游戏。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.MicrosoftSolitaireCollection",
            "Get-AppxPackage -Name 'Microsoft.MicrosoftSolitaireCollection' | Remove-AppxPackage"));
        catalog.Register(F("apps.maps", "Windows 地图", "预装应用",
            "Windows 地图应用。属于常见预装应用,可按需删除。",
            RiskLevel.Recommended, false, RestartRequirement.None, "Microsoft.WindowsMaps",
            "Get-AppxPackage -Name 'Microsoft.WindowsMaps' | Remove-AppxPackage"));

        catalog.Register(F("apps.xbox-gamebar", "Xbox Game Bar", "预装应用",
            "游戏录制与快捷入口。游戏用户可能用到,默认不选。",
            RiskLevel.Caution, false, RestartRequirement.None, "Microsoft.XboxGamingOverlay",
            "Get-AppxPackage -Name 'Microsoft.XboxGamingOverlay' | Remove-AppxPackage"));
        catalog.Register(F("apps.todo", "Microsoft To Do", "预装应用",
            "任务清单应用。部分用户日常使用,默认不选。",
            RiskLevel.Caution, false, RestartRequirement.None, "Microsoft.Todo",
            "Get-AppxPackage -Name 'Microsoft.Todo' | Remove-AppxPackage"));
        catalog.Register(F("apps.copilot", "Copilot", "预装应用",
            "系统集成的 Copilot 应用。如不使用可按需删除。",
            RiskLevel.Caution, false, RestartRequirement.None, "Microsoft.Copilot",
            "Get-AppxPackage -Name 'Microsoft.Copilot' | Remove-AppxPackage"));
    }

    public void RegisterPresets(IFunctionCatalog catalog)
    {
        catalog.RegisterPreset(new Preset
        {
            Id = "preset.apps.recommended",
            Name = "删除常见预装应用",
            Description = "Clipchamp、资讯、天气、地图、反馈中心等常见可选应用;不包含商店与关键组件。",
            Risk = RiskLevel.Recommended,
            TargetGroup = "推荐清理",
            FeatureIds = new[]
            {
                "apps.clipchamp", "apps.bing-news", "apps.bing-weather", "apps.gethelp",
                "apps.feedback-hub", "apps.solitaire", "apps.maps"
            }
        });
    }

    private static FunctionItem F(
        string id, string name, string category, string description, RiskLevel risk,
        bool admin, RestartRequirement restart, string packageName, string command) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Module = ModuleKind.Applications,
        Description = description,
        Risk = risk,
        RequiresAdmin = admin,
        Restart = restart,
        Source = "系统命令(Get-AppxPackage / Remove-AppxPackage)",
        Command = command,
        Extra = packageName
    };
}
