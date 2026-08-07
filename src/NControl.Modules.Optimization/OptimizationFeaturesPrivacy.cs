using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 隐私设置:与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)逐项对齐。
/// 名称/说明/分类均采用文档原文;实现参考 ZyperWin++ 4.2 源码的注册表操作。
/// 注:文档中部分条目键值相同(如 广告标识符/定向广告、自动连接热点/Wi-Fi感知),按文档逐项保留。
/// </summary>
public static class OptimizationFeaturesPrivacy
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ===== 文档:隐私设置(34 项)=====

        // 1、禁用页面预测功能
        catalog.Register(F("privacy.disable-page-prediction", "禁用页面预测功能", "隐私设置",
            "建议开启：减少网页预加载和浏览数据发送", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'AllowPagePrediction' -Value 0 -Type DWord -Force"));

        // 2、禁用SMS路由器服务
        catalog.Register(F("privacy.disable-sms-router", "禁用SMS路由器服务", "隐私设置",
            "按需开启：不用短信或移动宽带设备可开", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service SmsRouter -Force -ErrorAction SilentlyContinue; Set-Service SmsRouter -StartupType Disabled"));

        // 3、禁用活动收集
        catalog.Register(F("privacy.disable-tailored-experiences", "禁用活动收集", "隐私设置",
            "建议开启：停止记录应用和文件活动", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy' -Name 'TailoredExperiencesWithDiagnosticDataEnabled' -Value 0 -Type DWord -Force"));

        // 4、禁用应用启动跟踪
        catalog.Register(F("privacy.disable-app-launch-tracking", "禁用应用启动跟踪", "隐私设置",
            "按需开启：增强隐私，开始推荐可能变差", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Start_TrackProgs' -Value 0 -Type DWord -Force"));

        // 5、禁用广告标识符
        catalog.Register(F("privacy.disable-ads-id", "禁用广告标识符", "隐私设置",
            "建议开启：减少应用间广告追踪", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo' -Name 'Enabled' -Value 0 -Type DWord -Force"));

        // 6、禁用应用访问文件系统
        catalog.Register(F("privacy.deny-file-system-access", "禁用应用访问文件系统", "隐私设置",
            "按需开启：商店应用可能无法访问文件", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\broadFileSystemAccess' -Name 'Value' -Value 'Deny' -Type String -Force"));

        // 7、禁用应用访问文档
        catalog.Register(F("privacy.deny-documents-access", "禁用应用访问文档", "隐私设置",
            "按需开启：商店应用可能无法读取文档", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\documentsLibrary' -Name 'Value' -Value 'Deny' -Type String -Force"));

        // 8、禁用应用访问日历
        catalog.Register(F("privacy.deny-calendar-access", "禁用应用访问日历", "隐私设置",
            "按需开启：日历同步和提醒可能失效", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appointments' -Name 'Value' -Value 'Deny' -Type String -Force"));

        // 9、禁用应用访问联系人
        catalog.Register(F("privacy.deny-contacts-access", "禁用应用访问联系人", "隐私设置",
            "按需开启：通讯应用无法读取联系人", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\contacts' -Name 'Value' -Value 'Deny' -Type String -Force"));

        // 10、禁用网站语言跟踪
        catalog.Register(F("privacy.disable-language-tracking", "禁用网站语言跟踪", "隐私设置",
            "建议开启：网站无法按系统语言定制内容", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\International\\User Profile' -Name 'HttpAcceptLanguageOptOut' -Value 1 -Type DWord -Force"));

        // 11、禁用Windows欢迎体验
        catalog.Register(F("privacy.disable-welcome-experience", "禁用Windows欢迎体验", "隐私设置",
            "建议开启：减少更新后的欢迎和推广页面", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'DisableFirstRunAnimate' -Value 1 -Type DWord -Force"));

        // 12、禁用反馈频率
        catalog.Register(F("privacy.disable-feedback-notifications", "禁用反馈频率", "隐私设置",
            "建议开启：减少系统反馈询问", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'DoNotShowFeedbackNotifications' -Value 1 -Type DWord -Force"));

        // 13、禁用诊断数据收集
        catalog.Register(F("privacy.disable-telemetry", "禁用诊断数据收集", "隐私设置",
            "建议开启：减少诊断上传，但排错信息变少", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force"));

        // 14、禁用写作习惯跟踪
        catalog.Register(F("privacy.disable-inking-personalization", "禁用写作习惯跟踪", "隐私设置",
            "建议开启：停止收集手写和输入习惯", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Input\\Settings' -Name 'Inking&TypingPersonalization' -Value 0 -Type DWord -Force"));

        // 15、禁用设置应用建议
        catalog.Register(F("privacy.disable-settings-suggestions", "禁用设置应用建议", "隐私设置",
            "建议开启：设置页面不再显示推荐内容", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SubscribedContent-338393Enabled' -Value 0 -Type DWord -Force"));

        // 16、禁用Bing搜索结果
        catalog.Register(F("privacy.disable-bing-search", "禁用Bing搜索结果", "隐私设置",
            "按需开启：系统搜索不再显示网络结果", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search' -Name 'BingSearchEnabled' -Value 0 -Type DWord -Force"));

        // 17、禁用通讯录收集
        catalog.Register(F("privacy.disable-contact-collection", "禁用通讯录收集", "隐私设置",
            "建议开启：阻止系统收集联系人信息", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\contacts' -Name 'Value' -Value 'Deny' -Type String -Force"));

        // 18、禁用键入文本收集
        catalog.Register(F("privacy.disable-text-collection", "禁用键入文本收集", "隐私设置",
            "建议开启：减少输入文本数据收集", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Input\\Settings' -Name 'Inking&TypingPersonalization' -Value 0 -Type DWord -Force"));

        // 19、禁用搜索历史
        catalog.Register(F("privacy.disable-search-history", "禁用搜索历史", "隐私设置",
            "按需开启：增强隐私，但无法查看历史搜索", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings' -Name 'IsDeviceSearchHistoryEnabled' -Value 0 -Type DWord -Force"));

        // 20、禁用赞助商应用安装
        catalog.Register(F("privacy.disable-third-party-suggestions", "禁用赞助商应用安装", "隐私设置",
            "建议开启：阻止赞助应用自动安装", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent' -Name 'DisableThirdPartySuggestions' -Value 1 -Type DWord -Force"));

        // 21、禁用自动连接热点
        catalog.Register(F("privacy.disable-hotspot-connect", "禁用自动连接热点", "隐私设置",
            "建议开启：避免自动连接陌生热点", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WcmSvc\\Local' -Name 'fBlockNonDomain' -Value 1 -Type DWord -Force"));

        // 22、禁用输入数据个性化
        catalog.Register(F("privacy.disable-input-personalization", "禁用输入数据个性化", "隐私设置",
            "建议开启：停止用输入数据个性化词库", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Personalization\\Settings' -Name 'RestrictImplicitTextCollection' -Value 1 -Type DWord -Force"));

        // 23、禁用键入见解
        catalog.Register(F("privacy.disable-typing-insights", "禁用键入见解", "隐私设置",
            "按需开启：不再显示打字统计和输入见解", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Input\\Settings' -Name 'EnableTypingInsights' -Value 0 -Type DWord -Force"));

        // 24、禁用预安装应用
        catalog.Register(F("privacy.disable-preinstalled-apps", "禁用预安装应用", "隐私设置",
            "按需开启：减少预装应用，部分功能可能缺失", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'DisablePreInstalledApps' -Value 1 -Type DWord -Force"));

        // 25、禁用.NET遥测
        catalog.Register(F("privacy.disable-net-telemetry", "禁用.NET遥测", "隐私设置",
            "建议开启：减少.NET使用数据上传", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'DisableNetFrameworkTelemetry' -Value 1 -Type DWord -Force"));

        // 26、禁用PowerShell遥测
        catalog.Register(F("privacy.disable-powershell-telemetry", "禁用PowerShell遥测", "隐私设置",
            "建议开启：减少PowerShell使用数据上传", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell' -Name 'EnablePowerShellTelemetry' -Value 0 -Type DWord -Force"));

        // 27、禁用遥测服务
        catalog.Register(F("advanced.disable-diagtrack", "禁用遥测服务", "隐私设置",
            "按需开启：减少遥测，但系统诊断能力下降", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service DiagTrack -StartupType Disabled"));

        // 28、禁用语音激活(Cortana)
        catalog.Register(F("privacy.disable-voice-activation", "禁用语音激活(Cortana)", "隐私设置",
            "建议开启：不用语音唤醒时可减少监听", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy' -Name 'LetAppsActivateWithVoice' -Value 2 -Type DWord -Force"));

        // 29、禁用位置服务
        catalog.Register(F("privacy.disable-location", "禁用位置服务", "隐私设置",
            "按需开启：地图天气等定位功能将受影响", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors' -Name 'DisableLocation' -Value 1 -Type DWord -Force"));

        // 30、启用剪切板历史记录
        catalog.Register(F("privacy.enable-clipboard-history", "启用剪切板历史记录", "隐私设置",
            "按需开启：复制更方便，但敏感内容会留存", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Clipboard' -Name 'EnableClipboardHistory' -Value 1 -Type DWord -Force"));

        // 31、禁用定向广告
        catalog.Register(F("privacy.disable-targeted-ads", "禁用定向广告", "隐私设置",
            "建议开启：减少按兴趣投放广告", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo' -Name 'Enabled' -Value 0 -Type DWord -Force"));

        // 32、禁用Wi-Fi感知
        catalog.Register(F("privacy.disable-wifi-sense", "禁用Wi-Fi感知", "隐私设置",
            "建议开启：避免自动共享或连接陌生Wi-Fi", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WcmSvc\\Local' -Name 'fBlockNonDomain' -Value 1 -Type DWord -Force"));

        // 33、禁用步骤记录器
        catalog.Register(F("privacy.disable-step-recorder", "禁用步骤记录器", "隐私设置",
            "按需开启：禁止录制操作，远程排错不便", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\ProblemReports' -Name 'DisableProblemReports' -Value 1 -Type DWord -Force"));

        // 34、禁用写入调试信息
        catalog.Register(F("privacy.disable-debug-print", "禁用写入调试信息", "隐私设置",
            "按需开启：减少磁盘占用，不利于蓝屏排错", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Debug Print Filter' -Name 'DEFAULT' -Value 0 -Type DWord -Force"));
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
        Source = "ZyperWin++ 适配 · 注册表/系统命令(2026-08-02 文档对齐)",
        Command = command
    };
}
