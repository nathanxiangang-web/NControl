using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 优化模块:系统设置页与一键优化方案来源。
/// 功能目录与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)完全一致:
/// 外观/资源管理器 29 + 性能优化设置 46 + 安全设置 10 + Edge优化设置 12 + 系统设置 15 + 更新设置 8 + 隐私设置 34。
/// (自研扩展已移除;Win停止更新5000天自系统设置移入更新设置)
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
                "taskbar.hide-search",
                "start.disable-recommendations", "privacy.disable-ads-id",
                "explorer.show-extensions",
                "explorer.notepad-wrap", "explorer.quick-access-no-recent",
                "explorer.titlebar-full-path", "privacy.enable-clipboard-history"
            }
        });

                catalog.RegisterPreset(new Preset
        {
            Id = "preset.recommended",
            Name = "推荐优化",
            Description = "按 ZyperWin++ 推荐配置(2026-08-06)对齐:涵盖资源管理器、性能、隐私、更新、Edge 与常用系统设置;高风险项(关闭防火墙/UAC/SmartScreen/系统还原/停止更新等)不进预设。",
            Risk = RiskLevel.Caution,
            TargetGroup = "适合大多数人",
            FeatureIds = new[]
            {
                "update.no-notepad-banner",
                "update.block-feature-updates",
                "update.exclude-mrt",
                "update.disable-driver-updates",
                "performance.disable-animations",
                "explorer.no-shortcut-suffix",
                "explorer.open-this-pc",
                "explorer.notepad-wrap",
                "explorer.notepad-statusbar",
                "explorer.disable-broken-shortcut-tracking",
                "explorer.quick-access-no-frequent",
                "explorer.quick-access-no-recent",
                "explorer.enable-transparency",
                "explorer.foreground-responsiveness",
                "ime.default-english",
                "explorer.show-extensions",
                "taskbar.hide-taskview",
                "taskbar.hide-search",
                "explorer.hide-spotlight-icon",
                "explorer.no-simple-net-id-list",
                "explorer.separate-process",
                "explorer.desktop-this-pc",
                "explorer.desktop-recycle-bin",
                "explorer.shell-restart-on-crash",
                "explorer.unload-unused-dlls",
                "system.vhd-no-expand-on-mount",
                "system.fonts-hide-by-language",
                "system.disable-auto-debug",
                "system.disable-hibernation",
                "system.autochk-timeout-5",
                "system.disable-device-restore-point",
                "system.fonts-install-as-link",
                "explorer.classic-context-menu",
                "perf.no-store-openwith",
                "perf.no-start-suggestions",
                "perf.disable-tips",
                "perf.disable-pca",
                "perf.disable-cortana",
                "perf.disable-ads-id",
                "perf.disable-store-promo",
                "perf.disable-auto-debug",
                "gaming.disable-game-dvr",
                "perf.disable-remote-assistance",
                "perf.disable-prefetch",
                "perf.disable-reserved-storage",
                "perf.disable-low-disk-warning",
                "perf.disable-error-reporting-service",
                "perf.disable-hpet",
                "perf.disable-homegroup",
                "perf.disable-ceip",
                "perf.disable-remote-registry",
                "perf.disable-diagnostics-service",
                "perf.disable-ntfs-link-tracking",
                "advanced.disable-wsearch",
                "perf.disable-paging-executive",
                "perf.disable-error-report",
                "perf.disable-store-auto-update",
                "perf.disable-silent-installs",
                "perf.disable-auto-maintenance",
                "perf.gpu-hw-scheduling",
                "perf.large-system-cache",
                "performance.high-performance-plan",
                "perf.enable-device-setup",
                "perf.disable-search-suggestions",
                "perf.shorter-service-timeout",
                "perf.hide-recommended-sites",
                "start.disable-recommendations",
                "perf.cpu-priority-optimize",
                "perf.svchost-split-threshold",
                "perf.disable-search-web",
                "perf.io-page-lock-limit",
                "privacy.disable-net-telemetry",
                "privacy.disable-step-recorder",
                "privacy.disable-targeted-ads",
                "privacy.disable-feedback-notifications",
                "privacy.disable-ads-id",
                "privacy.disable-tailored-experiences",
                "privacy.disable-typing-insights",
                "privacy.disable-text-collection",
                "privacy.disable-settings-suggestions",
                "privacy.disable-input-personalization",
                "privacy.disable-search-history",
                "privacy.disable-contact-collection",
                "privacy.disable-language-tracking",
                "privacy.disable-location",
                "privacy.disable-debug-print",
                "privacy.disable-inking-personalization",
                "advanced.disable-diagtrack",
                "privacy.disable-page-prediction",
                "privacy.deny-contacts-access",
                "privacy.deny-calendar-access",
                "privacy.deny-documents-access",
                "privacy.deny-file-system-access",
                "privacy.disable-voice-activation",
                "privacy.disable-preinstalled-apps",
                "privacy.disable-third-party-suggestions",
                "privacy.disable-telemetry",
                "privacy.disable-hotspot-connect",
                "privacy.disable-bing-search",
                "privacy.disable-powershell-telemetry",
                "privacy.disable-sms-router",
                "privacy.disable-wifi-sense",
                "privacy.disable-welcome-experience",
                "privacy.enable-clipboard-history",
                "edge.no-diagnostic-data",
                "edge.hide-first-run",
                "edge.hide-top-sites",
                "edge.suppress-unsupported-os-warning",
                "edge.disable-performance-detector",
                "edge.disable-personalized-ads",
                "edge.disable-startup-boost",
                "edge.hide-news-feed",
                "edge.hide-sidebar",
                "edge.block-bing-ads",
                "edge.no-background-apps"
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
                "taskbar.hide-search",
                "start.disable-recommendations", "privacy.disable-ads-id",
                "explorer.show-extensions",
                "taskbar.hide-taskview",
                "explorer.open-this-pc",
                "privacy.disable-settings-suggestions", "privacy.disable-tailored-experiences",
                "privacy.disable-welcome-experience", "gaming.disable-game-dvr",
                "explorer.titlebar-full-path", "privacy.enable-clipboard-history",
                // 谨慎追加
                "explorer.classic-context-menu", "performance.disable-animations",
                "performance.high-performance-plan", "update.disable-driver-updates",
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
