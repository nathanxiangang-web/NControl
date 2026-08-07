using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 优化模块:系统体验、隐私、任务栏、资源管理器、更新、性能和游戏(产品文档 §3.3)。
/// 对应一级导航“系统设置”页面与“一键优化”方案来源。
/// </summary>
public sealed class OptimizationModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "优化模块";

    public void RegisterFeatures(IFunctionCatalog catalog)
    {
        // 基础功能(第一代核心)
        RegisterBaseFeatures(catalog);
        // ZyperWin++ 功能池适配(2026-08-02 数据)
        OptimizationFeaturesExplorer.Register(catalog);
        OptimizationFeaturesPerformance.Register(catalog);
        OptimizationFeaturesPrivacy.Register(catalog);
        OptimizationFeaturesSecurity.Register(catalog);
    }

    private void RegisterBaseFeatures(IFunctionCatalog catalog)
    {
        // ---------- 任务栏与开始菜单 ----------
        catalog.Register(F("taskbar.hide-search", "隐藏任务栏搜索框", "任务栏与开始菜单",
            "隐藏任务栏上的搜索框,保留搜索功能本身。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'SearchboxTaskbarMode' -Value 0 -Type DWord -Force"));
        catalog.Register(F("taskbar.hide-widgets", "隐藏小组件按钮", "任务栏与开始菜单",
            "隐藏任务栏上的天气/小组件入口。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarDa' -Value 0 -Type DWord -Force"));
        catalog.Register(F("taskbar.hide-chat", "隐藏聊天(Teams)按钮", "任务栏与开始菜单",
            "隐藏任务栏上的聊天/Teams 入口。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarMn' -Value 0 -Type DWord -Force"));
        catalog.Register(F("taskbar.hide-taskview", "隐藏任务视图按钮", "任务栏与开始菜单",
            "隐藏任务栏上的任务视图按钮(多桌面入口仍可通过 Win+Tab 使用)。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'ShowTaskViewButton' -Value 0 -Type DWord -Force"));
        catalog.Register(F("taskbar.hide-copilot", "隐藏 Copilot 按钮", "任务栏与开始菜单",
            "隐藏任务栏上的 Copilot 入口按钮。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'ShowCopilotButton' -Value 0 -Type DWord -Force"));
        catalog.Register(F("taskbar.align-left", "任务栏图标左对齐", "任务栏与开始菜单",
            "将任务栏图标从居中改为左对齐,接近 Windows 10 习惯。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarAl' -Value 0 -Type DWord -Force"));
        catalog.Register(F("start.disable-recommendations", "关闭开始菜单推荐", "任务栏与开始菜单",
            "减少开始菜单中的推荐应用、文件和推广内容。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Start_ShowRecommended' -Value 0 -Type DWord -Force"));

        // ---------- 资源管理器 ----------
        catalog.Register(F("explorer.open-this-pc", "打开资源管理器显示“此电脑”", "资源管理器",
            "打开文件管理器后直接显示磁盘、设备和常用位置。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'LaunchTo' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.show-extensions", "显示已知文件类型的扩展名", "资源管理器",
            "显示 .txt、.exe、.jpg 等完整文件扩展名。", RiskLevel.Recommended, false, RestartRequirement.ExplorerRestart,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'HideFileExt' -Value 0 -Type DWord -Force"));
        catalog.Register(F("explorer.show-hidden", "显示隐藏文件", "资源管理器",
            "在资源管理器中显示隐藏文件和文件夹。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Hidden' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.classic-context-menu", "恢复 Windows 10 经典右键菜单", "资源管理器",
            "取消 Windows 11 的折叠式右键菜单,操作更直接。", RiskLevel.Recommended, false, RestartRequirement.ExplorerRestart,
            "New-Item -Path 'HKCU:\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32' -Name '(Default)' -Value '' -Type String -Force; Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("explorer.disable-recent-files", "关闭快速访问中的最近文件", "资源管理器",
            "快速访问不再显示最近打开的文件列表。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Start_TrackDocs' -Value 0 -Type DWord -Force"));

        // ---------- 隐私与广告 ----------
        catalog.Register(F("privacy.disable-ads-id", "关闭个性化广告 ID", "隐私与广告",
            "停止使用广告 ID 提供个性化广告。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo' -Name 'Enabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-tailored-experiences", "关闭定向体验", "隐私与广告",
            "关闭基于诊断数据的个性化提示与建议。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy' -Name 'TailoredExperiencesWithDiagnosticDataEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-lock-screen-tips", "关闭锁屏提示与推广", "隐私与广告",
            "锁屏界面不再显示提示、广告和推广内容。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SubscribedContent-338387Enabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-settings-suggestions", "关闭设置页建议", "隐私与广告",
            "Windows 设置应用不再显示推广性建议条目。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SubscribedContent-310093Enabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("privacy.disable-welcome-experience", "关闭欢迎体验", "隐私与广告",
            "关闭更新后展示的欢迎与新功能提示内容。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SubscribedContent-310094Enabled' -Value 0 -Type DWord -Force"));

        // ---------- Windows 更新 ----------
        catalog.Register(F("update.disable-driver-updates", "禁止通过更新自动安装驱动", "Windows 更新",
            "Windows 更新不再自动下载和安装驱动,驱动需手动更新。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching' -Name 'SearchOrderConfig' -Value 0 -Type DWord -Force"));
        catalog.Register(F("update.disable-delivery-optimization", "关闭传递优化", "Windows 更新",
            "停止从其他电脑下载更新和向其他电脑上传更新。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config' -Name 'DODownloadMode' -Value 0 -Type DWord -Force"));
        catalog.Register(F("update.set-active-hours", "设置主动时间段(9:00-18:00)", "Windows 更新",
            "系统在该时间段内不自动重启以安装更新。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Update\\Settings' -Name 'ActiveHoursStart' -Value 540 -Type DWord -Force; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Update\\Settings' -Name 'ActiveHoursEnd' -Value 1080 -Type DWord -Force"));
        catalog.Register(F("update.pause-feature-updates-7d", "暂停功能更新 7 天", "Windows 更新",
            "将功能更新暂停 7 天,期间不会安装新的功能更新。", RiskLevel.Caution, true, RestartRequirement.None,
            "$s=[DateTime]::Now; $e=$s.AddDays(7); Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseFeatureUpdatesStartTime' -Value $s.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseFeatureUpdatesEndTime' -Value $e.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force"));

        // ---------- 性能与电源 ----------
        catalog.Register(F("performance.high-performance-plan", "切换到高性能电源计划", "性能与电源",
            "切换到高性能电源计划,可能增加耗电和发热。", RiskLevel.Caution, false, RestartRequirement.None,
            "powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"));
        catalog.Register(F("performance.ultimate-performance-plan", "启用卓越性能电源计划", "性能与电源",
            "启用并切换到卓越性能计划;效果依赖硬件与系统版本,验证不足。", RiskLevel.Experimental, true, RestartRequirement.None,
            "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-Null; powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61"));
        catalog.Register(F("performance.disable-transparency", "关闭窗口透明效果", "性能与电源",
            "关闭任务栏和部分界面的透明效果,界面更直接。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize' -Name 'EnableTransparency' -Value 0 -Type DWord -Force"));
        catalog.Register(F("performance.disable-animations", "关闭窗口动画", "性能与电源",
            "关闭窗口动画,使界面操作更直接。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop\\WindowMetrics' -Name 'MinAnimate' -Value 0 -Type String -Force"));

        // ---------- 游戏 ----------
        catalog.Register(F("gaming.disable-game-dvr", "关闭后台录制(Game DVR)", "游戏",
            "关闭游戏时后台录制与截图功能。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("gaming.disable-game-bar", "禁用游戏栏", "游戏",
            "禁用 Win+G 游戏栏入口(不影响游戏本身运行)。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameBar' -Name 'UseNexusForGameBarEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("gaming.disable-game-mode", "关闭游戏模式", "游戏",
            "关闭游戏模式;可能影响部分游戏的资源调度,仅建议特定场景使用。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value 0 -Type DWord -Force"));

        // ---------- 高级(不进任何预设) ----------
        catalog.Register(F("advanced.disable-sysmain", "禁用 SysMain 服务", "高级",
            "停止 SysMain;部分设备上的应用预加载可能变慢。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service SysMain -Force -ErrorAction SilentlyContinue; Set-Service SysMain -StartupType Disabled"));
        catalog.Register(F("advanced.disable-diagtrack", "禁用诊断跟踪服务", "高级",
            "停止 DiagTrack(连接的用户体验和遥测);部分系统功能可能受影响。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service DiagTrack -StartupType Disabled"));
        catalog.Register(F("advanced.disable-wsearch", "禁用 Windows Search 服务", "高级",
            "停止 Windows Search;开始菜单和资源管理器的搜索将不可用。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service WSearch -Force -ErrorAction SilentlyContinue; Set-Service WSearch -StartupType Disabled"));
        catalog.Register(F("advanced.disable-meltdown-mitigations", "关闭处理器漏洞缓解措施", "高级",
            "可能提升少量性能,但会降低系统安全性;不会包含在任何推荐方案中。", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'FeatureSettingsOverride' -Value 3 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'FeatureSettingsOverrideMask' -Value 3 -Type DWord -Force"));
    }

    public void RegisterPresets(IFunctionCatalog catalog)
    {
        catalog.RegisterPreset(new Preset
        {
            Id = "preset.light",
            Name = "轻度优化",
            Description = "只调整广告、推荐内容、任务栏和常用界面设置,不禁用服务,不修改安全功能。",
            Risk = RiskLevel.Safe,
            TargetGroup = "推荐新用户",
            FeatureIds = new[]
            {
                "taskbar.hide-search", "taskbar.hide-widgets", "taskbar.hide-copilot",
                "start.disable-recommendations", "privacy.disable-ads-id",
                "privacy.disable-lock-screen-tips", "explorer.show-extensions",
                "performance.disable-transparency",
                "explorer.notepad-wrap", "taskbar.clock-show-seconds", "explorer.quick-access-no-recent",
                "explorer.titlebar-full-path", "privacy.enable-clipboard-history"
            }
        });

        catalog.RegisterPreset(new Preset
        {
            Id = "preset.recommended",
            Name = "推荐优化",
            Description = "包含隐私、广告、资源管理器、任务栏和部分后台功能优化。",
            Risk = RiskLevel.Recommended,
            TargetGroup = "适合大多数人",
            FeatureIds = new[]
            {
                // 轻度
                "taskbar.hide-search", "taskbar.hide-widgets", "taskbar.hide-copilot",
                "start.disable-recommendations", "privacy.disable-ads-id",
                "privacy.disable-lock-screen-tips", "explorer.show-extensions",
                "performance.disable-transparency",
                "explorer.titlebar-full-path", "privacy.enable-clipboard-history",
                // 追加
                "taskbar.hide-chat", "taskbar.hide-taskview", "taskbar.align-left",
                "explorer.open-this-pc", "explorer.show-hidden",
                "privacy.disable-settings-suggestions", "privacy.disable-tailored-experiences",
                "privacy.disable-welcome-experience", "gaming.disable-game-dvr", "gaming.disable-game-bar",
                // ZyperWin++ 适配补充(仅安全/推荐级)
                "taskbar.show-all-tray-icons", "taskbar.merge-buttons-always",
                "explorer.foreground-responsiveness", "explorer.disable-autoplay", "explorer.no-recent-docs-history",
                "perf.disable-lock-spotlight", "perf.disable-tips", "perf.disable-silent-installs",
                "perf.disable-remote-assistance", "perf.disable-search-web",
                "privacy.disable-bing-search", "privacy.disable-search-history",
                "privacy.disable-preinstalled-apps", "privacy.disable-inking-personalization",
                "edge.no-background-apps", "edge.hide-news-feed", "edge.block-bing-ads"
            }
        });

        catalog.RegisterPreset(new Preset
        {
            Id = "preset.deep",
            Name = "深度优化",
            Description = "包含服务、更新、电源和高级性能设置,部分功能可能影响系统行为;默认不全选高风险项。",
            Risk = RiskLevel.Caution,
            TargetGroup = "谨慎使用",
            FeatureIds = new[]
            {
                // 推荐
                "taskbar.hide-search", "taskbar.hide-widgets", "taskbar.hide-copilot",
                "start.disable-recommendations", "privacy.disable-ads-id",
                "privacy.disable-lock-screen-tips", "explorer.show-extensions",
                "performance.disable-transparency",
                "taskbar.hide-chat", "taskbar.hide-taskview", "taskbar.align-left",
                "explorer.open-this-pc", "explorer.show-hidden",
                "privacy.disable-settings-suggestions", "privacy.disable-tailored-experiences",
                "privacy.disable-welcome-experience", "gaming.disable-game-dvr", "gaming.disable-game-bar",
                "explorer.titlebar-full-path", "privacy.enable-clipboard-history",
                // 谨慎追加
                "explorer.classic-context-menu", "performance.disable-animations",
                "performance.high-performance-plan", "update.disable-driver-updates",
                "update.disable-delivery-optimization", "update.set-active-hours",
                "update.pause-feature-updates-7d", "gaming.disable-game-mode",
                "advanced.disable-sysmain", "advanced.disable-diagtrack", "advanced.disable-wsearch",
                // 常用清理与预装应用(跨模块引用)
                "cleanup.user-temp", "cleanup.thumbnails", "cleanup.recycle-bin",
                "apps.clipchamp", "apps.bing-news", "apps.bing-weather", "apps.solitaire",
                // ZyperWin++ 适配补充(谨慎级)
                "explorer.unload-unused-dlls", "explorer.remove-duplicate-drives",
                "perf.disable-store-auto-update", "perf.disable-cortana", "perf.fast-shutdown",
                "perf.shorter-service-timeout", "perf.disable-diagnostics-service",
                "perf.disable-error-reporting-service", "perf.disable-ntfs-link-tracking",
                "perf.disable-auto-maintenance", "perf.disable-low-disk-warning",
                "perf.disable-pca", "perf.disable-reserved-storage",
                "system.disable-hibernation", "system.disable-device-restore-point",
                "system.disable-msi-restore-point", "system.no-crash-dump",
                "update.block-feature-updates",
                "edge.hide-first-run", "edge.disable-startup-boost", "edge.hide-top-sites",
                "edge.hide-sidebar", "edge.no-diagnostic-data", "edge.disable-personalized-ads",
                "privacy.disable-telemetry", "privacy.disable-location",
                "privacy.disable-sms-router", "privacy.deny-file-system-access",
                // ZyperWin++ 适配补充(谨慎级,第二批对齐)
                "perf.svchost-split-threshold", "perf.large-system-cache",
                "perf.disable-paging-executive", "perf.io-page-lock-limit", "perf.cpu-priority-optimize"
            }
        });
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
        Source = "自研 · Windows 注册表/系统命令适配",
        Command = command
    };
}
