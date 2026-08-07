using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 性能与电源、系统设置、Windows 更新、Edge 浏览器功能(来自 ZyperWin++ 功能池适配,2026-08-02 数据)。
/// 高风险项(从不检查更新)不在此注册,见 OptimizationFeaturesSecurity。
/// </summary>
public static class OptimizationFeaturesPerformance
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ===== 性能与电源 =====
        catalog.Register(F("perf.no-store-openwith", "不在应用商店查找关联应用", "性能与电源",
            "打开未知文件类型时,不再自动跳转到应用商店搜索应用。", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer' -Name 'NoUseStoreOpenWith' -Value 1 -Type DWord -Force"));
        catalog.Register(F("perf.disable-store-promo", "关闭商店应用推广", "性能与电源",
            "关闭应用商店对预装应用的推广展示。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'PreInstalledAppsEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-store-auto-update", "禁止应用商店自动下载更新", "性能与电源",
            "应用商店不再自动下载和安装应用更新,需手动更新。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsStore' -Name 'AutoDownload' -Value 2 -Type DWord -Force"));
        catalog.Register(F("perf.disable-lock-spotlight", "关闭锁屏聚焦推广", "性能与电源",
            "锁屏不再展示 Windows 聚焦的推广与提示内容。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'RotatingLockScreenEnable' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-tips", "关闭使用技巧与建议", "性能与电源",
            "系统不再推送“使用 Windows 时获取技巧和建议”类通知。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SoftLandingEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-silent-installs", "禁止自动安装推荐应用", "性能与电源",
            "系统不会静默安装推荐的应用。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SilentInstalledAppsEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-cortana", "关闭 Cortana", "性能与电源",
            "关闭小娜语音助手入口(不影响系统搜索)。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCortana' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.run-dialog-no-history", "运行对话框不显示历史", "性能与电源",
            "Win+R 运行对话框不再显示历史输入记录。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Start_TrackProgs' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.hide-recommended-sites", "隐藏开始菜单推荐网站", "性能与电源",
            "开始菜单历史记录中不再显示推荐的网站。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\Explorer' -Name 'HideRecommendedPersonalizedSites' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer' -Name 'HideRecommendedPersonalizedSites' -Value 1 -Type DWord -Force"));
        catalog.Register(F("perf.fast-shutdown", "加快关机速度", "性能与电源",
            "缩短系统等待应用程序结束的时间;未保存的数据可能丢失,请谨慎。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'AutoEndTasks' -Value 1 -Type String -Force; Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'HungAppTimeout' -Value 3000 -Type String -Force; Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'WaitToKillAppTimeout' -Value 1000 -Type String -Force"));
        catalog.Register(F("perf.shorter-service-timeout", "缩短关闭服务等待时间", "性能与电源",
            "关机时服务停止等待时间从默认值缩短,加快关机;部分服务可能被强制结束。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control' -Name 'WaitToKillServiceTimeout' -Value 1000 -Type String -Force"));
        catalog.Register(F("perf.disable-remote-assistance", "关闭远程协助", "性能与电源",
            "关闭远程协助入口,减少被远程控制的风险。", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance' -Name 'fAllowToGetHelp' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-remote-registry", "禁用远程注册表服务", "性能与电源",
            "停止远程注册表服务,降低远程修改注册表的攻击面。", RiskLevel.Recommended, true, RestartRequirement.None,
            "Stop-Service RemoteRegistry -Force -ErrorAction SilentlyContinue; Set-Service RemoteRegistry -StartupType Disabled"));
        catalog.Register(F("perf.disable-diagnostics-service", "禁用诊断策略服务", "性能与电源",
            "停止诊断策略服务(DPS);部分系统诊断与问题报告功能将不可用。", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service DPS -Force -ErrorAction SilentlyContinue; Set-Service DPS -StartupType Disabled"));
        catalog.Register(F("perf.disable-error-reporting-service", "禁用错误报告服务", "性能与电源",
            "停止错误报告服务(WerSvc);系统不再自动收集错误报告,可能影响故障排查。", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service WerSvc -Force -ErrorAction SilentlyContinue; Set-Service WerSvc -StartupType Disabled"));
        catalog.Register(F("perf.disable-homegroup", "禁用家庭组服务", "性能与电源",
            "停止家庭组服务(HomeGroupProvider);家庭组功能已逐步被 Windows 移除。", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service HomeGroupProvider -Force -ErrorAction SilentlyContinue; Set-Service HomeGroupProvider -StartupType Disabled"));
        catalog.Register(F("perf.disable-ceip", "关闭客户体验改善计划", "性能与电源",
            "停止向微软发送客户体验改善计划数据。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows' -Name 'CEIPEnable' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-ntfs-link-tracking", "禁用 NTFS 链接跟踪服务", "性能与电源",
            "停止 NTFS 链接跟踪服务(TrkWks);开始菜单/快捷方式对移动文件的自动追踪失效。", RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service TrkWks -Force -ErrorAction SilentlyContinue; Set-Service TrkWks -StartupType Disabled"));
        catalog.Register(F("perf.disable-auto-maintenance", "禁止自动维护计划", "性能与电源",
            "关闭系统自动维护计划(诊断与维护任务);系统可能无法自动完成例行维护。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\ScheduledDiagnostics' -Name 'EnabledExecution' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-low-disk-warning", "禁用磁盘空间不足警告", "性能与电源",
            "不再提示磁盘空间不足;磁盘写满时可能无法及时察觉,请谨慎使用。", RiskLevel.Caution, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoLowDiskSpaceChecks' -Value 1 -Type DWord -Force"));
        catalog.Register(F("perf.disable-search-suggestions", "去除搜索页信息流和热搜", "性能与电源",
            "搜索界面不再显示信息流与热搜内容。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\explorer' -Name 'DisableSearchBoxSuggestions' -Value 1 -Type DWord -Force"));
        catalog.Register(F("perf.gpu-hw-scheduling", "开启 GPU 硬件加速调度", "性能与电源",
            "启用 GPU 硬件加速计划调度;仅在显卡与驱动支持时有效,效果因环境而异。", RiskLevel.Experimental, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' -Name 'HwSchMode' -Value 2 -Type DWord -Force"));
        catalog.Register(F("perf.disable-memory-compression", "关闭内存压缩", "性能与电源",
            "关闭内存压缩;建议物理内存 32GB 以上时使用,否则可能增加页面文件使用。", RiskLevel.Experimental, true, RestartRequirement.None,
            "Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue"));
        catalog.Register(F("perf.disable-hpet", "禁用高精度事件定时器", "性能与电源",
            "关闭高精度事件定时器(HPET),改用系统默认时钟;部分应用依赖 HPET,效果因硬件而异。", RiskLevel.Experimental, true, RestartRequirement.Reboot,
            "bcdedit /set useplatformclock false"));
        catalog.Register(F("perf.disable-reserved-storage", "禁用保留存储", "性能与电源",
            "释放系统为更新预留的磁盘空间(约数 GB);后续更新可能因空间不足失败。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ReserveManager' -Name 'MiscPolicyInfo' -Value 2 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ReserveManager' -Name 'PassedPolicy' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ReserveManager' -Name 'ShippedWithReserves' -Value 0 -Type DWord -Force"));
        catalog.Register(F("perf.disable-auto-debug", "关闭系统自动调试", "性能与电源",
            "程序崩溃时不再自动启动调试器,减少卡顿。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug' -Name 'Auto' -Value 0 -Type String -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug' -Name 'Auto' -Value 0 -Type String -Force"));
        catalog.Register(F("perf.disable-pca", "关闭程序兼容性助手", "性能与电源",
            "停止程序兼容性助手服务(PcaSvc);部分旧程序可能不再获得兼容性提示。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\services\\PcaSvc' -Name 'Start' -Value 4 -Type DWord -Force; Stop-Service PcaSvc -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("perf.disable-search-web", "关闭搜索的必应与网络结果", "性能与电源",
            "系统搜索不再请求必应和网络内容,仅搜索本地;可减少联网与延迟。", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'BingSearchEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'DisableWebSearch' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'ConnectedSearchUseWeb' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'ConnectedSearchUseWebOverMeteredConnections' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCloudSearch' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCortanaAboveLock' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AlwaysUseAutoLangDetection' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowIndexingEncryptedStoresOrItems' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowCortana' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search' -Name 'AllowSearchToUseLocation' -Value 0 -Type DWord -Force"));

        // ===== ZyperWin++ 性能补充(2026-08-02 数据)=====
        catalog.Register(F("perf.svchost-split-threshold", "优化进程数量", "性能与电源",
            "提高服务进程合并阈值,让更多系统服务共用进程,减少进程数量与内存占用。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control' -Name 'SvcHostSplitThresholdInKB' -Value 4294967295 -Type DWord -Force"));
        catalog.Register(F("perf.large-system-cache", "启用大系统缓存", "性能与电源",
            "让系统使用更多内存作为文件缓存;内存较小的机器可能挤压应用可用内存。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'LargeSystemCache' -Value 1 -Type DWord -Force"));
        catalog.Register(F("perf.disable-paging-executive", "内核与驱动常驻内存", "性能与电源",
            "禁止系统内核与驱动程序分页到硬盘,减少磁盘 I/O;内存不足时可能导致系统不稳定。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'DisablePagingExecutive' -Value 1 -Type DWord -Force"));
        catalog.Register(F("perf.io-page-lock-limit", "增加文件系统缓存上限", "性能与电源",
            "提高文件系统可锁定的 IO 页数上限,大文件读写更流畅;会占用更多内存。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'IoPageLockLimit' -Value 10000000 -Type DWord -Force"));
        catalog.Register(F("perf.cpu-priority-optimize", "优化处理器优先级", "性能与电源",
            "调整前台进程优先级与关键设备中断优先级,提升前台响应;部分硬件组合下可能异常。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'Win32PrioritySeparation' -Value 38 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'IRQ8Priority' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'IRQ16Priority' -Value 2 -Type DWord -Force"));
        catalog.Register(F("perf.disable-prefetch", "关闭预读与超级预读", "性能与电源",
            "关闭 Prefetch/Superfetch 预读,减少启动与后台磁盘预读开销;机械硬盘上可能变慢。", RiskLevel.Experimental, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters' -Name 'EnablePrefetcher' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters' -Name 'EnableSuperfetch' -Value 0 -Type DWord -Force"));

        // ===== 系统设置 =====
        catalog.Register(F("system.disable-hibernation", "关闭休眠", "系统设置",
            "关闭休眠与快速启动(删除休眠文件,释放与内存等大的磁盘空间);合盖/睡眠行为不变。", RiskLevel.Caution, true, RestartRequirement.Reboot,
            "powercfg /h off; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power' -Name 'HibernateEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("system.usb-eject-power-off", "弹出 USB 后彻底断电", "系统设置",
            "安全弹出 U 盘/移动硬盘后彻底断开其电源,减少数据残留风险。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\services\\usbhub\\hubg' -Name 'DisableOnSoftRemove' -Value 1 -Type DWord -Force"));
        catalog.Register(F("system.autochk-timeout-5", "磁盘检查等待缩短至 5 秒", "系统设置",
            "开机时磁盘错误检查的等待时间缩短为 5 秒,加快启动。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager' -Name 'AutoChkTimeOut' -Value 5 -Type DWord -Force"));
        catalog.Register(F("system.disable-device-restore-point", "设备安装不创建还原点", "系统设置",
            "安装设备驱动时不再自动创建系统还原点。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeviceInstall\\Settings' -Name 'DisableSystemRestore' -Value 1 -Type DWord -Force"));
        catalog.Register(F("system.disable-msi-restore-point", "MSI 安装不创建还原点", "系统设置",
            "使用 Windows Installer 安装软件时不再创建系统还原点。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Installer' -Name 'LimitSystemRestoreCheckpointing' -Value 1 -Type DWord -Force"));
        catalog.Register(F("system.fonts-hide-by-language", "仅显示当前语言字体", "系统设置",
            "字体列表中只显示与当前语言相关的字体,减少干扰。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Font Management' -Name 'Auto Activation Mode' -Value 1 -Type DWord -Force"));
        catalog.Register(F("system.fonts-install-as-link", "允许字体快捷方式安装", "系统设置",
            "字体可以快捷方式方式安装,不复制字体文件,节省空间。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Font Management' -Name 'InstallAsLink' -Value 1 -Type DWord -Force"));
        catalog.Register(F("system.no-crash-dump", "崩溃时不写入调试信息", "系统设置",
            "系统崩溃(蓝屏)时不生成内存转储文件;将无法用于事后分析故障。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl' -Name 'CrashDumpEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("system.disable-wfpdiag-log", "禁用网络诊断日志", "系统设置",
            "停止收集 WfpDiag 网络诊断日志,减少磁盘写入。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\BFE\\Parameters\\Policy\\Options' -Name 'CollectNetEvents' -Value 0 -Type DWord -Force"));
        catalog.Register(F("system.vhd-no-expand-on-mount", "VHD 动态磁盘按需扩展", "系统设置",
            "挂载 VHD/VHDX 动态磁盘时不再一次性扩展到最大容量,改为按需增长,节省磁盘空间。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\services\\FsDepends\\Parameters' -Name 'VirtualDiskExpandOnMount' -Value 4 -Type DWord -Force"));
        catalog.Register(F("system.auto-restart-on-bsod", "蓝屏时自动重启", "系统设置",
            "系统蓝屏(崩溃)后自动重启,便于无人值守场景快速恢复;如需排查蓝屏原因建议关闭。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl' -Name 'AutoReboot' -Value 1 -Type DWord -Force"));
        catalog.Register(F("system.disable-boot-log-report", "关闭登录成功引导报告", "系统设置",
            "系统启动后不再写入\"上次登录成功\"引导日志,减少日志写入。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon' -Name 'ReportBootOk' -Value 0 -Type DWord -Force"));
        catalog.Register(F("ime.default-english", "微软拼音默认英文输入", "系统设置",
            "微软拼音输入法打开时默认为英文模式。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\InputMethod\\Settings\\CHS' -Name 'Default Mode' -Value 1 -Type DWord -Force"));
        catalog.Register(F("ime.disable-cloud-suggestion", "关闭微软拼音云计算", "系统设置",
            "输入法不再使用云端候选词,减少联网;本地词库仍可用。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\InputMethod\\Settings\\CHS' -Name 'Enable Cloud Candidate' -Value 0 -Type DWord -Force"));

        // ===== Windows 更新 =====
        catalog.Register(F("update.no-auto-reboot-with-users", "有用户登录时不自动重启", "Windows 更新",
            "更新挂起时,若有用户已登录则不自动重启电脑,改为等待用户确认。", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -Value 1 -Type DWord -Force"));
        catalog.Register(F("update.block-feature-updates", "禁止大版本更新", "Windows 更新",
            "将 Windows 停留在当前大版本(如 23H2),不接收功能更新;安全更新仍正常。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'TargetReleaseVersion' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'TargetReleaseVersionInfo' -Value 'Windows 11' -Type String -Force"));
        catalog.Register(F("update.no-notepad-banner", "不显示记事本新版提示", "Windows 更新",
            "记事本不再提示“新版本可用”的商店横幅。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'ShowStoreBanner' -Value 0 -Type DWord -Force"));

        // ===== Edge 浏览器 =====
        catalog.Register(F("edge.hide-first-run", "不显示首次运行欢迎页", "Edge 浏览器",
            "Edge 首次启动不展示欢迎引导页。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'HideFirstRunExperience' -Value 1 -Type DWord -Force"));
        catalog.Register(F("edge.no-background-apps", "关闭后不后台运行", "Edge 浏览器",
            "关闭所有 Edge 窗口后不再保留后台进程。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'BackgroundModeEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'BackgroundModeEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("edge.disable-startup-boost", "禁用启动增强", "Edge 浏览器",
            "关闭 Edge 启动增强,减少开机与后台占用。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'StartupBoostEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'StartupBoostEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("edge.block-bing-ads", "阻止必应搜索结果广告", "Edge 浏览器",
            "在必应搜索中屏蔽推广广告结果。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'BingAdsSuppression' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'BingAdsSuppression' -Value 1 -Type DWord -Force"));
        catalog.Register(F("edge.hide-top-sites", "新标签页隐藏热门站点", "Edge 浏览器",
            "新标签页不再展示默认的热门站点。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'NewTabPageHideDefaultTopSites' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'NewTabPageHideDefaultTopSites' -Value 1 -Type DWord -Force"));
        catalog.Register(F("edge.hide-sidebar", "隐藏 Edge 边栏", "Edge 浏览器",
            "隐藏 Edge 浏览器右侧的边栏入口。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge\\Recommended' -Name 'HubsSidebarEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge\\Recommended' -Name 'HubsSidebarEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("edge.no-diagnostic-data", "不发送诊断数据", "Edge 浏览器",
            "Edge 不再发送浏览器诊断数据。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'DiagnosticData' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'DiagnosticData' -Value 0 -Type DWord -Force"));
        catalog.Register(F("edge.hide-news-feed", "禁用新标签页资讯内容", "Edge 浏览器",
            "新标签页不再展示微软资讯信息流。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'NewTabPageContentEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'NewTabPageContentEnabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("edge.disable-personalized-ads", "禁用个性化广告", "Edge 浏览器",
            "关闭 Edge 的个性化广告与体验数据上报。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Edge' -Name 'PersonalizationReportingEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'PersonalizationReportingEnabled' -Value 0 -Type DWord -Force"));
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
