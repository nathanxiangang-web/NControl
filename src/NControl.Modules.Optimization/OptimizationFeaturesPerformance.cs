using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 性能优化设置/系统设置/更新设置/Edge优化设置:与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)逐项对齐。
/// 名称/说明/分类均采用文档原文;实现参考 ZyperWin++ 4.2 源码(RegWrite/SetServiceStart/ExplorerNotify Cmd)。
/// 高风险项(幽灵熔断/Exploit Protection/TSX/从不检查更新/关闭系统还原/停止更新5000天/不安全下载警告)标记 HighRisk,不进预设。
/// </summary>
public static class OptimizationFeaturesPerformance
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ================= 文档:性能优化设置(46 项)=================

        // 1、优化进程数量
        catalog.Register(F("perf.svchost-split-threshold", "优化进程数量", "性能优化设置",
            "按需开启：减少进程，但故障影响范围变大", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control' -Name 'SvcHostSplitThresholdInKB' -Value 4294967295 -Type DWord -Force"));

        // 2、不允许在「开始」菜单显示建议
        catalog.Register(F("perf.no-start-suggestions", "不允许在「开始」菜单显示建议", "性能优化设置",
            "建议开启：开始菜单不再显示微软建议", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SubscribedContent-338388Enabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SubscribedContent-338389Enabled' -Value 1 -Type DWord -Force"));

        // 3、不要在应用商店中查找关联应用
        catalog.Register(F("perf.no-store-openwith", "不要在应用商店中查找关联应用", "性能优化设置",
            "建议开启：未知文件不再跳转商店找应用", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer' -Name 'NoUseStoreOpenWith' -Value 1 -Type DWord -Force"));

        // 4、关闭商店应用推广
        catalog.Register(F("perf.disable-store-promo", "关闭商店应用推广", "性能优化设置",
            "建议开启：减少商店广告和推荐内容", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'PreInstalledAppsEnabled' -Value 0 -Type DWord -Force"));

        // 5、禁止应用商店自动下载和安装更新
        catalog.Register(F("perf.disable-store-auto-update", "禁止应用商店自动下载和安装更新", "性能优化设置",
            "按需开启：商店应用需手动更新维护", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsStore' -Name 'AutoDownload' -Value 2 -Type DWord -Force"));

        // 6、关闭锁屏时的Windows聚焦推广
        catalog.Register(F("perf.disable-lock-spotlight", "关闭锁屏时的Windows聚焦推广", "性能优化设置",
            "按需开启：锁屏不再自动显示推广图片", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'RotatingLockScreenEnable' -Value 0 -Type DWord -Force"));

        // 7、关闭“使用Windows时获取技巧和建议”
        catalog.Register(F("perf.disable-tips", "关闭“使用Windows时获取技巧和建议”", "性能优化设置",
            "建议开启：关闭系统技巧和建议弹窗", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SoftLandingEnabled' -Value 0 -Type DWord -Force"));

        // 8、禁止自动安装推荐的应用程序
        catalog.Register(F("perf.disable-silent-installs", "禁止自动安装推荐的应用程序", "性能优化设置",
            "建议开启：避免自动安装推荐应用", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SilentInstalledAppsEnabled' -Value 0 -Type DWord -Force"));

        // 9、关闭游戏录制工具
        catalog.Register(F("gaming.disable-game-dvr", "关闭游戏录制工具", "性能优化设置",
            "按需开启：不用录屏和Xbox功能时可开", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -Force"));

        // 10、关闭多嘴的小娜
        catalog.Register(F("perf.disable-cortana", "关闭多嘴的小娜", "性能优化设置",
            "建议开启：不用小娜时减少后台占用", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCortana' -Value 0 -Type DWord -Force"));

        // 11、“运行”对话框不要显示历史记录
        catalog.Register(F("perf.run-dialog-no-history", "“运行”对话框不要显示历史记录", "性能优化设置",
            "按需开启：运行框不保存历史命令", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Start_TrackProgs' -Value 0 -Type DWord -Force"));

        // 12、隐藏「开始」菜单中的“推荐”
        catalog.Register(F("start.disable-recommendations", "隐藏「开始」菜单中的“推荐”", "性能优化设置",
            "建议开启：减少开始菜单推荐内容", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\Explorer' -Name 'HideRecommendedSection' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer' -Name 'HideRecommendedSection' -Value 1 -Type DWord -Force"));

        // 13、隐藏「开始」菜单历史记录中推荐的网站
        catalog.Register(F("perf.hide-recommended-sites", "隐藏「开始」菜单历史记录中推荐的网站", "性能优化设置",
            "建议开启：开始菜单不再推荐历史网站", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\Explorer' -Name 'HideRecommendedPersonalizedSites' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer' -Name 'HideRecommendedPersonalizedSites' -Value 1 -Type DWord -Force"));

        // 14、加快关机速度
        catalog.Register(F("perf.fast-shutdown", "加快关机速度", "性能优化设置",
            "按需开启：关机更快，程序可能被强制关闭", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'AutoEndTasks' -Value 1 -Type String -Force; Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'HungAppTimeout' -Value 3000 -Type String -Force; Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'WaitToKillAppTimeout' -Value 1000 -Type String -Force"));

        // 15、缩短关闭服务等待时间
        catalog.Register(F("perf.shorter-service-timeout", "缩短关闭服务等待时间", "性能优化设置",
            "按需开启：关机更快，数据可能未写完", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control' -Name 'WaitToKillServiceTimeout' -Value 1000 -Type String -Force"));

        // 16、关闭远程协助
        catalog.Register(F("perf.disable-remote-assistance", "关闭远程协助", "性能优化设置",
            "建议开启：不用远程协助时更安全", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance' -Name 'fAllowToGetHelp' -Value 0 -Type DWord -Force"));

        // 17、禁用远程修改注册表
        catalog.Register(F("perf.disable-remote-registry", "禁用远程修改注册表", "性能优化设置",
            "建议开启：阻止远程修改注册表，更安全", RiskLevel.Recommended, true, RestartRequirement.None,
            "Stop-Service RemoteRegistry -Force -ErrorAction SilentlyContinue; Set-Service RemoteRegistry -StartupType Disabled"));

        // 18、禁用诊断服务
        catalog.Register(F("perf.disable-diagnostics-service", "禁用诊断服务", "性能优化设置",
            "不建议开启：影响系统诊断和故障排查", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service DPS -Force -ErrorAction SilentlyContinue; Set-Service DPS -StartupType Disabled"));

        // 19、禁用SysMain
        catalog.Register(F("advanced.disable-sysmain", "禁用SysMain", "性能优化设置",
            "按需开启：磁盘占用下降，程序启动可能变慢", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service SysMain -Force -ErrorAction SilentlyContinue; Set-Service SysMain -StartupType Disabled"));

        // 20、禁用Windows Search
        catalog.Register(F("advanced.disable-wsearch", "禁用Windows Search", "性能优化设置",
            "不建议开启：文件和开始菜单搜索可能失效", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service WSearch -Force -ErrorAction SilentlyContinue; Set-Service WSearch -StartupType Disabled"));

        // 21、禁用错误报告
        catalog.Register(F("perf.disable-error-reporting-service", "禁用错误报告", "性能优化设置",
            "按需开启：减少上报，但不利于故障排查", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service WerSvc -Force -ErrorAction SilentlyContinue; Set-Service WerSvc -StartupType Disabled"));

        // 22、禁用家庭组(服务不存在时静默跳过,Win11 已移除 HomeGroupProvider)
        catalog.Register(F("perf.disable-homegroup", "禁用家庭组", "性能优化设置",
            "建议开启：旧版家庭组已淘汰，通常无影响", RiskLevel.Recommended, true, RestartRequirement.None,
            "if (Get-Service HomeGroupProvider -ErrorAction SilentlyContinue) { Stop-Service HomeGroupProvider -Force -ErrorAction SilentlyContinue; Set-Service HomeGroupProvider -StartupType Disabled -ErrorAction SilentlyContinue }"));

        // 23、禁用客户体验改善计划
        catalog.Register(F("perf.disable-ceip", "禁用客户体验改善计划", "性能优化设置",
            "建议开启：减少微软体验数据上传", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows' -Name 'CEIPEnable' -Value 0 -Type DWord -Force"));

        // 24、禁用NTFS链接跟踪服务
        catalog.Register(F("perf.disable-ntfs-link-tracking", "禁用NTFS链接跟踪服务", "性能优化设置",
            "按需开启：移动链接文件可能失效", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service TrkWks -Force -ErrorAction SilentlyContinue; Set-Service TrkWks -StartupType Disabled"));

        // 25、禁止自动维护计划
        catalog.Register(F("perf.disable-auto-maintenance", "禁止自动维护计划", "性能优化设置",
            "不建议开启：会停止磁盘清理和系统维护", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\ScheduledDiagnostics' -Name 'EnabledExecution' -Value 0 -Type DWord -Force"));

        // 26、启用大系统缓存以提高性能
        catalog.Register(F("perf.large-system-cache", "启用大系统缓存以提高性能", "性能优化设置",
            "不建议开启：桌面电脑可能占用更多内存", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'LargeSystemCache' -Value 1 -Type DWord -Force"));

        // 27、禁止系统内核与驱动程序分页到硬盘
        catalog.Register(F("perf.disable-paging-executive", "禁止系统内核与驱动程序分页到硬盘", "性能优化设置",
            "按需开启：内存充足可减少内核换页", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'DisablePagingExecutive' -Value 1 -Type DWord -Force"));

        // 28、增加文件管理系统缓存以提高性能
        catalog.Register(F("perf.io-page-lock-limit", "增加文件管理系统缓存以提高性能", "性能优化设置",
            "按需开启：提高文件缓存，但更占内存", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'IoPageLockLimit' -Value 10000000 -Type DWord -Force"));

        // 29、启用高性能电源计划
        catalog.Register(F("performance.high-performance-plan", "启用高性能电源计划", "性能优化设置",
            "按需开启：性能更高，但更耗电发热", RiskLevel.Caution, false, RestartRequirement.None,
            "& 'C:\\WINDOWS\\System32\\powercfg.exe' /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"));

        // 30、禁用处理器的幽灵和熔断补丁以提高性能
        catalog.Register(F("advanced.disable-meltdown-mitigations", "禁用处理器的幽灵和熔断补丁以提高性能", "性能优化设置",
            "不建议开启：关闭漏洞防护会降低安全性", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'FeatureSettingsOverride' -Value 3 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'FeatureSettingsOverrideMask' -Value 3 -Type DWord -Force"));

        // 31、禁用保留的存储
        catalog.Register(F("perf.disable-reserved-storage", "禁用保留的存储", "性能优化设置",
            "不建议开启：可能导致系统更新空间不足", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ReserveManager' -Name 'MiscPolicyInfo' -Value 2 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ReserveManager' -Name 'PassedPolicy' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ReserveManager' -Name 'ShippedWithReserves' -Value 0 -Type DWord -Force"));

        // 32、优化处理器性能
        catalog.Register(F("perf.cpu-priority-optimize", "优化处理器性能", "性能优化设置",
            "按需开启：性能更高，但更耗电发热", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'Win32PrioritySeparation' -Value 38 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'IRQ8Priority' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'IRQ16Priority' -Value 2 -Type DWord -Force"));

        // 33、加快预读能力改善速度
        catalog.Register(F("perf.disable-prefetch", "加快预读能力改善速度", "性能优化设置",
            "按需开启：可能加快启动，也会增加读写", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters' -Name 'EnablePrefetcher' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters' -Name 'EnableSuperfetch' -Value 0 -Type DWord -Force"));

        // 34、禁止系统自动生成错误报告
        catalog.Register(F("perf.disable-error-report", "禁止系统自动生成错误报告", "性能优化设置",
            "建议开启：减少数据上报，基本没有用", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\PCHealth\\ErrorReporting' -Name 'DoReport' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\PCHealth\\ErrorReporting' -Name 'ShowUI' -Value 0 -Type DWord -Force"));

        // 35、禁用高精度事件定时器（HPET）(服务不存在时静默跳过)
        catalog.Register(F("perf.disable-hpet", "禁用高精度事件定时器（HPET）", "性能优化设置",
            "不建议开启：收益不稳，可能引发计时异常", RiskLevel.Caution, true, RestartRequirement.None,
            "if (Get-Service hpet -ErrorAction SilentlyContinue) { Stop-Service hpet -Force -ErrorAction SilentlyContinue; Set-Service hpet -StartupType Disabled -ErrorAction SilentlyContinue }"));

        // 36、关闭系统自动调试功能，提高系统运行速度
        catalog.Register(F("perf.disable-auto-debug", "关闭系统自动调试功能，提高系统运行速度", "性能优化设置",
            "按需开启：减少调试开销，不利于排错", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug' -Name 'Auto' -Value 0 -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug' -Name 'Auto' -Value 0 -Type String -Force"));

        // 37、关闭程序兼容性助手
        catalog.Register(F("perf.disable-pca", "关闭程序兼容性助手", "性能优化设置",
            "按需开启：旧软件兼容提示不再出现", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\ControlSet001\\services\\PcaSvc' -Name 'Start' -Value 4 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\services\\PcaSvc' -Name 'Start' -Value 4 -Type DWord -Force; Stop-Service PcaSvc -Force -ErrorAction SilentlyContinue"));

        // 38、启用自动完成设备设置
        catalog.Register(F("perf.enable-device-setup", "启用自动完成设备设置", "性能优化设置",
            "建议开启：自动完成更新和设备配置", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'DisableAutomaticRestartSignOn' -Value 0 -Type DWord -Force"));

        // 39、关闭Exploit Protection（乱序内存）
        catalog.Register(F("perf.disable-exploit-protection", "关闭Exploit Protection（乱序内存）", "性能优化设置",
            "不建议开启：关闭漏洞防护会降低安全性", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\System\\ControlSet001\\Control\\Session Manager\\kernel' -Name 'MitigationOptions' -Value ([byte[]](0x22,0x22,0x22,0x00,0x00,0x02,0x00,0x00,0x00,0x02,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force"));

        // 40、优化Windows Search和小娜的设置
        catalog.Register(F("perf.disable-search-web", "优化Windows Search和小娜的设置", "性能优化设置",
            "按需开启：减少联网搜索，结果更单一", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'BingSearchEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'DisableWebSearch' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'ConnectedSearchUseWeb' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'ConnectedSearchUseWebOverMeteredConnections' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCloudSearch' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCortanaAboveLock' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AlwaysUseAutoLangDetection' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowIndexingEncryptedStoresOrItems' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCortana' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowSearchToUseLocation' -Value 0 -Type DWord -Force"));

        // 41、关闭广告 ID
        catalog.Register(F("perf.disable-ads-id", "关闭广告 ID", "性能优化设置",
            "建议开启：减少个性化广告追踪", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo' -Name 'Enabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo' -Name 'DisabledByGroupPolicy' -Value 1 -Type DWord -Force"));

        // 42、禁用磁盘空间不足警告
        catalog.Register(F("perf.disable-low-disk-warning", "禁用磁盘空间不足警告", "性能优化设置",
            "不建议开启：磁盘将满不提醒，易出故障", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoLowDiskSpaceChecks' -Value 1 -Type DWord -Force"));

        // 43、去除搜索页面信息流和热搜
        catalog.Register(F("perf.disable-search-suggestions", "去除搜索页面信息流和热搜", "性能优化设置",
            "建议开启：搜索界面更清爽，减少干扰", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\explorer' -Name 'DisableSearchBoxSuggestions' -Value 1 -Type DWord -Force"));

        // 44、关闭TSX漏洞补丁
        catalog.Register(F("advanced.disable-tsx", "关闭TSX漏洞补丁", "性能优化设置",
            "不建议开启：关闭漏洞补丁会降低安全性", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel' -Name 'DisableTsx' -Value 1 -Type DWord -Force"));

        // 45、开启GPU硬件加速
        catalog.Register(F("perf.gpu-hw-scheduling", "开启GPU硬件加速", "性能优化设置",
            "按需开启：部分电脑更流畅，不兼容可关", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' -Name 'HwSchMode' -Value 2 -Type DWord -Force"));

        // 46、32GB以上关闭内存压缩
        catalog.Register(F("perf.disable-memory-compression", "32GB以上关闭内存压缩", "性能优化设置",
            "按需开启：省 CPU 资源和减少低微卡顿", RiskLevel.Caution, true, RestartRequirement.None,
            "Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue"));

        // ===== 第一代自研扩展(不在 ZyperWin 文档中,保留)=====
        catalog.Register(F("performance.ultimate-performance-plan", "启用卓越性能电源计划", "性能优化设置",
            "启用并切换到卓越性能计划;效果依赖硬件与系统版本,验证不足(自研扩展)。", RiskLevel.Experimental, true, RestartRequirement.None,
            "& 'C:\\WINDOWS\\System32\\powercfg.exe' -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-Null; & 'C:\\WINDOWS\\System32\\powercfg.exe' /setactive e9a42b02-d5df-448d-aa00-03f14749eb61"));
        catalog.Register(F("performance.disable-transparency", "关闭窗口透明效果", "性能优化设置",
            "关闭任务栏和部分界面的透明效果,界面更直接(自研扩展)。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize' -Name 'EnableTransparency' -Value 0 -Type DWord -Force"));
        catalog.Register(F("gaming.disable-game-bar", "禁用游戏栏", "性能优化设置",
            "禁用 Win+G 游戏栏入口(不影响游戏本身运行)(自研扩展)。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameBar' -Name 'UseNexusForGameBarEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("gaming.disable-game-mode", "关闭游戏模式", "性能优化设置",
            "关闭游戏模式;可能影响部分游戏的资源调度,仅建议特定场景使用(自研扩展)。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value 0 -Type DWord -Force"));

        // ================= 文档:系统设置(16 项)=================

        // 1、关闭休眠
        catalog.Register(F("system.disable-hibernation", "关闭休眠", "系统设置",
            "按需开启：释放磁盘空间，但无法休眠", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "powercfg /h off; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power' -Name 'HibernateEnabled' -Value 0 -Type DWord -Force"));

        // 2、弹出USB磁盘后彻底断开其电源
        catalog.Register(F("system.usb-eject-power-off", "弹出USB磁盘后彻底断开其电源", "系统设置",
            "按需开启：弹出后断电，再用需重新插拔", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\services\\usbhub\\hubg' -Name 'DisableOnSoftRemove' -Value 1 -Type DWord -Force"));

        // 3、不要将VHD动态文件扩展到最大以节省磁盘空间
        catalog.Register(F("system.vhd-no-expand-on-mount", "不要将VHD动态文件扩展到最大以节省磁盘空间", "系统设置",
            "建议开启：节省VHD空间，性能可能略低", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\services\\FsDepends\\Parameters' -Name 'VirtualDiskExpandOnMount' -Value 4 -Type DWord -Force"));

        // 4、蓝屏时自动重启
        catalog.Register(F("system.auto-restart-on-bsod", "蓝屏时自动重启", "系统设置",
            "按需开启：自动恢复，但不便查看蓝屏错误", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl' -Name 'AutoReboot' -Value 1 -Type DWord -Force"));

        // 5、关闭系统自动调试功能
        catalog.Register(F("system.disable-auto-debug", "关闭系统自动调试功能", "系统设置",
            "按需开启：减少调试开销，不利于故障定位", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug' -Name 'Auto' -Value 0 -Type String -Force"));

        // 6、将磁盘错误检查的等待时间缩短到五秒
        catalog.Register(F("system.autochk-timeout-5", "将磁盘错误检查的等待时间缩短到五秒", "系统设置",
            "建议开启：缩短开机磁盘检查等待", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager' -Name 'AutoChkTimeOut' -Value 5 -Type DWord -Force"));

        // 7、设备安装禁止创建系统还原点
        catalog.Register(F("system.disable-device-restore-point", "设备安装禁止创建系统还原点", "系统设置",
            "不建议开启：驱动出错时少一个恢复点", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeviceInstall\\Settings' -Name 'DisableSystemRestore' -Value 1 -Type DWord -Force"));

        // 8、MSI类软件安装禁止创建系统还原点
        catalog.Register(F("system.disable-msi-restore-point", "MSI类软件安装禁止创建系统还原点", "系统设置",
            "不建议开启：软件出错时少一个恢复点", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Installer' -Name 'LimitSystemRestoreCheckpointing' -Value 1 -Type DWord -Force"));

        // 9、关闭系统还原
        catalog.Register(F("advanced.disable-system-restore", "关闭系统还原", "系统设置",
            "不建议开启：系统故障后无法用还原点恢复", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Disable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore' -Name 'DisableSR' -Value 1 -Type DWord -Force"));

        // 10、根据语言设置隐藏字体
        catalog.Register(F("system.fonts-hide-by-language", "根据语言设置隐藏字体", "系统设置",
            "按需开启：少见语言字体将被隐藏", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Font Management' -Name 'Auto Activation Mode' -Value 1 -Type DWord -Force"));

        // 11、允许字体作为快捷方式安装
        catalog.Register(F("system.fonts-install-as-link", "允许字体作为快捷方式安装", "系统设置",
            "按需开启：节省空间，但源字体不能删除", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Font Management' -Name 'InstallAsLink' -Value 1 -Type DWord -Force"));

        // 12、崩溃时不写入调试信息
        catalog.Register(F("system.no-crash-dump", "崩溃时不写入调试信息", "系统设置",
            "按需开启：减少崩溃文件，不利于排错", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl' -Name 'CrashDumpEnabled' -Value 0 -Type DWord -Force"));

        // 13、禁用账户登录日志报告
        catalog.Register(F("system.disable-boot-log-report", "禁用账户登录日志报告", "系统设置",
            "按需开启：减少登录日志，不利于审计排错", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon' -Name 'ReportBootOk' -Value 0 -Type String -Force"));

        // 14、禁用WfpDiag.ETL日志
        catalog.Register(F("system.disable-wfpdiag-log", "禁用WfpDiag.ETL日志", "系统设置",
            "按需开启：减少网络日志，不利于网络排错", RiskLevel.Caution, true, RestartRequirement.None,
            "NETSH WFP SET OPTIONS NETEVENTS=OFF; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\BFE\\Parameters\\Policy\\Options' -Name 'CollectNetEvents' -Value 0 -Type DWord -Force"));

        // 15、Win11右键菜单恢复为Win10样式
        catalog.Register(F("explorer.classic-context-menu", "Win11右键菜单恢复为Win10样式", "系统设置",
            "按需开启：恢复旧右键菜单，操作更直接", RiskLevel.Safe, false, RestartRequirement.ExplorerRestart,
            "New-Item -Path 'HKCU:\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32' -Name '(Default)' -Value '' -Type String -Force; Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue"));

        // 16、Win停止更新5000天
        catalog.Register(F("system.pause-updates-5000d", "Win停止更新5000天", "系统设置",
            "按需开启：延迟系统更新10年", RiskLevel.HighRisk, true, RestartRequirement.None,
            "$s=[DateTime]::Now; $e=$s.AddDays(5000); Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'FlightSettingsMaxPauseDays' -Value '7152' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseFeatureUpdatesStartTime' -Value $s.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseFeatureUpdatesEndTime' -Value $e.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseQualityUpdatesStartTime' -Value $s.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseQualityUpdatesEndTime' -Value $e.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseUpdatesStartTime' -Value $s.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseUpdatesExpiryTime' -Value $e.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force"));

        // ================= 文档:更新设置(7 项)=================

        // 1、自动安装无需重启的更新
        catalog.Register(F("update.auto-install-minor-updates", "自动安装无需重启的更新", "更新设置",
            "建议开启：及时安装更新，通常无需重启", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'AutoInstallMinorUpdates' -Value 1 -Type DWord -Force"));

        // 2、更新挂起时若有用户登录则不自动重启计算机
        catalog.Register(F("update.no-auto-reboot-with-users", "更新挂起时若有用户登录则不自动重启计算机", "更新设置",
            "建议开启：有人使用时避免强制重启", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -Value 1 -Type DWord -Force"));

        // 3、Windows更新不包括驱动程序
        catalog.Register(F("update.disable-driver-updates", "Windows更新不包括驱动程序", "更新设置",
            "按需开启：避免驱动翻车，但需手动更新", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'ExcludeWUDriversInQualityUpdate' -Value 1 -Type DWord -Force"));

        // 4、禁止Win10/11进行大版本更新
        catalog.Register(F("update.block-feature-updates", "禁止Win10/11进行大版本更新", "更新设置",
            "不建议长期开启：会错过新功能和安全支持", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control' -Name 'PortableOperatingSystem' -Value 1 -Type DWord -Force; Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'TargetReleaseVersion' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'TargetReleaseVersionInfo' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'ProductVersion' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DisableOSUpgrade' -ErrorAction SilentlyContinue"));

        // 5、Windows更新不包括恶意软件删除工具
        catalog.Register(F("update.exclude-mrt", "Windows更新不包括恶意软件删除工具", "更新设置",
            "不建议开启：会跳过微软恶意软件清理", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\MRT' -Name 'DontOfferThroughWUAU' -Value 1 -Type DWord -Force"));

        // 6、从不检查系统更新
        catalog.Register(F("advanced.disable-windows-update-checks", "从不检查系统更新", "更新设置",
            "不建议开启：长期不更新会积累安全风险", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update' -Name 'AUOptions' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update' -Name 'CachedAUOptions' -Value 1 -Type DWord -Force"));

        // 7、不要显示“新版本记事本已可用”提示
        catalog.Register(F("update.no-notepad-banner", "不要显示“新版本记事本已可用”提示", "更新设置",
            "建议开启：去除新版记事本升级提示", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'ShowStoreBanner' -Value 0 -Type DWord -Force"));

        // ===== 第一代自研扩展(不在 ZyperWin 文档中,保留)=====
        catalog.Register(F("update.disable-delivery-optimization", "关闭传递优化", "更新设置",
            "停止从其他电脑下载更新和向其他电脑上传更新(自研扩展)。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config' -Name 'DODownloadMode' -Value 0 -Type DWord -Force"));
        catalog.Register(F("update.set-active-hours", "设置主动时间段(9:00-18:00)", "更新设置",
            "系统在该时间段内不自动重启以安装更新(自研扩展)。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Update\\Settings' -Name 'ActiveHoursStart' -Value 540 -Type DWord -Force; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Update\\Settings' -Name 'ActiveHoursEnd' -Value 1080 -Type DWord -Force"));
        catalog.Register(F("update.pause-feature-updates-7d", "暂停功能更新 7 天", "更新设置",
            "将功能更新暂停 7 天,期间不会安装新的功能更新(自研扩展)。", RiskLevel.Caution, true, RestartRequirement.None,
            "$s=[DateTime]::Now; $e=$s.AddDays(7); Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseFeatureUpdatesStartTime' -Value $s.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'PauseFeatureUpdatesEndTime' -Value $e.ToString('yyyy-MM-ddTHH:mm:ssZ') -Type String -Force"));

        // ================= 文档:Edge优化设置(12 项)=================

        // 1、不要显示“首次运行”欢迎页面
        catalog.Register(F("edge.hide-first-run", "不要显示“首次运行”欢迎页面", "Edge优化设置",
            "建议开启：跳过欢迎页，首次启动更直接", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge' -Name 'PreventFirstRunPage' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'HideFirstRunExperience' -Value 1 -Type DWord -Force"));

        // 2、Edge浏览器关闭后禁止继续运行后台应用
        catalog.Register(F("edge.no-background-apps", "Edge浏览器关闭后禁止继续运行后台应用", "Edge优化设置",
            "建议开启：关闭Edge后不再占后台资源", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'BackgroundModeEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'BackgroundModeEnabled' -Value 0 -Type DWord -Force"));

        // 3、禁用启动增强
        catalog.Register(F("edge.disable-startup-boost", "禁用启动增强", "Edge优化设置",
            "按需开启：减少后台占用，冷启动稍慢", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'StartupBoostEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'StartupBoostEnabled' -Value 0 -Type DWord -Force"));

        // 4、阻止必应搜索结果中的所有广告
        catalog.Register(F("edge.block-bing-ads", "阻止必应搜索结果中的所有广告", "Edge优化设置",
            "建议开启：减少必应广告，页面更清爽", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'BingAdsSuppression' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'BingAdsSuppression' -Value 1 -Type DWord -Force"));

        // 5、从新标签页中隐藏默认的热门站点
        catalog.Register(F("edge.hide-top-sites", "从新标签页中隐藏默认的热门站点", "Edge优化设置",
            "按需开启：新标签页不显示热门站点", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'NewTabPageHideDefaultTopSites' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'NewTabPageHideDefaultTopSites' -Value 1 -Type DWord -Force"));

        // 6、隐藏Edge浏览器边栏
        catalog.Register(F("edge.hide-sidebar", "隐藏Edge浏览器边栏", "Edge优化设置",
            "按需开启：不用侧栏时界面更简洁", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge\\Recommended' -Name 'HubsSidebarEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge\\Recommended' -Name 'HubsSidebarEnabled' -Value 0 -Type DWord -Force"));

        // 7、关闭Edge浏览器停止支持旧系统的通知
        catalog.Register(F("edge.suppress-unsupported-os-warning", "关闭Edge浏览器停止支持旧系统的通知", "Edge优化设置",
            "不建议开启：可能错过系统兼容提醒", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'SuppressUnsupportedOSWarning' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'SuppressUnsupportedOSWarning' -Value 1 -Type DWord -Force"));

        // 8、不要发送任何诊断数据
        catalog.Register(F("edge.no-diagnostic-data", "不要发送任何诊断数据", "Edge优化设置",
            "建议开启：减少浏览器诊断数据上传", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'DiagnosticData' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'DiagnosticData' -Value 0 -Type DWord -Force"));

        // 9、禁用标签页性能检测器
        catalog.Register(F("edge.disable-performance-detector", "禁用标签页性能检测器", "Edge优化设置",
            "按需开启：难以及时发现耗资源标签页", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'PerformanceDetectorEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'PerformanceDetectorEnabled' -Value 0 -Type DWord -Force"));

        // 10、禁用新选项卡页面上的微软资讯内容
        catalog.Register(F("edge.hide-news-feed", "禁用新选项卡页面上的微软资讯内容", "Edge优化设置",
            "建议开启：新标签页不再显示微软资讯", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'NewTabPageContentEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'NewTabPageContentEnabled' -Value 0 -Type DWord -Force"));

        // 11、禁用个性化广告和体验
        catalog.Register(F("edge.disable-personalized-ads", "禁用个性化广告和体验", "Edge优化设置",
            "建议开启：减少个性化内容和广告追踪", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'PersonalizationReportingEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'PersonalizationReportingEnabled' -Value 0 -Type DWord -Force"));

        // 12、禁用不安全的下载警告
        catalog.Register(F("advanced.disable-insecure-download-warnings", "禁用不安全的下载警告", "Edge优化设置",
            "按需开启：下载不再拦截，需自行辨别", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'ShowDownloadsInsecureWarningsEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'ShowDownloadsInsecureWarningsEnabled' -Value 0 -Type DWord -Force"));
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
