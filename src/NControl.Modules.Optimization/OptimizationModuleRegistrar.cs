using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 优化模块:系统设置页与一键优化方案来源。
/// 功能目录与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)对齐:
/// 外观/资源管理器 29 + 性能优化设置 46 + 安全设置 10 + Edge优化设置 12 + 系统设置 16 + 更新设置 7 + 隐私设置 34。
/// 第一代自研扩展(任务栏补充/游戏栏/传递优化等)保留在对应分类,标记为扩展。
/// </summary>
public sealed class OptimizationModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "优化模块";

    public void RegisterFeatures(IFunctionCatalog catalog)
    {
        // 功能目录:与 ZyperWin++ 文档对齐(2026-08-02 数据)
        OptimizationFeaturesExplorer.Register(catalog);
        OptimizationFeaturesPerformance.Register(catalog);
        OptimizationFeaturesPrivacy.Register(catalog);
        OptimizationFeaturesSecurity.Register(catalog);
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
                // ZyperWin++ 文档对齐补充(仅安全/推荐级)
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
                // ZyperWin++ 文档对齐补充(谨慎级)
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
                // ZyperWin++ 文档对齐补充(谨慎级,第二批)
                "perf.svchost-split-threshold", "perf.large-system-cache",
                "perf.disable-paging-executive", "perf.io-page-lock-limit", "perf.cpu-priority-optimize"
            }
        });
    }
}
