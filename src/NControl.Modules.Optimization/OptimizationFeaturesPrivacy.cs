using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 隐私与广告功能(来自 ZyperWin++ 功能池适配,2026-08-02 数据)。
/// 每项只解决一个清晰问题;高风险项不在本文件注册。
/// </summary>
public static class OptimizationFeaturesPrivacy
{
    public static void Register(IFunctionCatalog catalog)
    {
        catalog.Register(F("privacy.disable-page-prediction", "禁用页面预测", "隐私与广告",
            "关闭资源管理器对常用文件夹的页面预读预测。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'AllowPagePrediction' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-sms-router", "禁用 SMS 路由器服务", "隐私与广告",
            "停止 SMS 路由器服务(SmsRouter),阻止短信相关数据转发。", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service SmsRouter -Force -ErrorAction SilentlyContinue; Set-Service SmsRouter -StartupType Disabled"));
        catalog.Register(F("privacy.deny-file-system-access", "禁止应用访问文件系统", "隐私与广告",
            "默认禁止应用访问整个文件系统(需在设置中为个别应用单独授权)。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\broadFileSystemAccess' -Name 'Value' -Value 'Deny' -Type String -Force"));
        catalog.Register(F("privacy.deny-documents-access", "禁止应用访问文档", "隐私与广告",
            "默认禁止应用访问文档库。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\documentsLibrary' -Name 'Value' -Value 'Deny' -Type String -Force"));
        catalog.Register(F("privacy.deny-calendar-access", "禁止应用访问日历", "隐私与广告",
            "默认禁止应用访问日历数据。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appointments' -Name 'Value' -Value 'Deny' -Type String -Force"));
        catalog.Register(F("privacy.deny-contacts-access", "禁止应用访问联系人", "隐私与广告",
            "默认禁止应用访问联系人数据。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\contacts' -Name 'Value' -Value 'Deny' -Type String -Force"));
        catalog.Register(F("privacy.disable-language-tracking", "禁用网站语言跟踪", "隐私与广告",
            "浏览器不再根据系统语言偏好跟踪网站语言。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\International\\User Profile' -Name 'HttpAcceptLanguageOptOut' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-feedback-notifications", "禁用反馈通知", "隐私与广告",
            "系统不再弹出“提供反馈”类通知。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'DoNotShowFeedbackNotifications' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-telemetry", "禁用诊断数据收集", "隐私与广告",
            "将诊断数据收集级别设为最低(仅必需数据);部分系统功能依赖诊断数据。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-inking-personalization", "禁用写作习惯跟踪", "隐私与广告",
            "输入法不再学习与个性化你的书写/输入习惯。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Input\\Settings' -Name 'Inking&TypingPersonalization' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-bing-search", "禁用 Bing 搜索结果", "隐私与广告",
            "系统搜索框不再返回必应搜索结果。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search' -Name 'BingSearchEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-search-history", "禁用搜索历史", "隐私与广告",
            "关闭设备上的搜索历史记录。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings' -Name 'IsDeviceSearchHistoryEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-third-party-suggestions", "禁用赞助商应用安装", "隐私与广告",
            "系统不再推荐和安装第三方赞助应用。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent' -Name 'DisableThirdPartySuggestions' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-hotspot-connect", "禁用自动连接热点与感知", "隐私与广告",
            "关闭自动连接开放热点与 Wi-Fi 感知功能。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WcmSvc\\Local' -Name 'fBlockNonDomain' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-input-personalization", "禁用输入数据个性化", "隐私与广告",
            "关闭基于输入内容的个性化建议。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Personalization\\Settings' -Name 'RestrictImplicitTextCollection' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-typing-insights", "禁用键入见解", "隐私与广告",
            "关闭输入法基于键入内容的见解与建议。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Input\\Settings' -Name 'EnableTypingInsights' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-preinstalled-apps", "禁用预安装应用", "隐私与广告",
            "禁止系统预装与重新安装推广应用。", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'DisablePreInstalledApps' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-net-telemetry", "禁用 .NET 遥测", "隐私与广告",
            "关闭 .NET Framework 的遥测数据上报。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'DisableNetFrameworkTelemetry' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-powershell-telemetry", "禁用 PowerShell 遥测", "隐私与广告",
            "关闭 PowerShell 的遥测数据上报。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell' -Name 'EnablePowerShellTelemetry' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-voice-activation", "禁用语音激活", "隐私与广告",
            "应用无法通过语音词随时激活(如 Cortana 唤醒)。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy' -Name 'LetAppsActivateWithVoice' -Value 2 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-location", "禁用位置服务", "隐私与广告",
            "关闭系统位置服务;地图、天气、查找设备等依赖位置的功能将不可用。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors' -Name 'DisableLocation' -Value 1 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-step-recorder", "禁用步骤记录器", "隐私与广告",
            "关闭问题步骤记录器与问题报告数据收集。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\ProblemReports' -Name 'DisableProblemReports' -Value 1 -Type DWord -Force"));
    }

    private static FunctionItem F(
        string id, string name, string category, string description, RiskLevel risk,
        bool admin, RestartRequirement restart, string command) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Module = ModuleKind.Optimization,
        Description = description,
        Risk = risk,
        RequiresAdmin = admin,
        Restart = restart,
        Source = "ZyperWin++ 适配 · 注册表/系统命令",
        Command = command
    };
}
