using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 外观/资源管理器功能:与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)逐项对齐。
/// 名称/说明/分类均采用文档原文;实现参考 ZyperWin++ 4.2 源码的注册表操作。
/// </summary>
public static class OptimizationFeaturesExplorer
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ===== 文档:外观/资源管理器(29 项)=====

        // 1、隐藏任务栏搜索框
        catalog.Register(F("taskbar.hide-search", "隐藏任务栏搜索框", "外观/资源管理器",
            "建议开启：任务栏更简洁，仍可正常搜索", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search' -Name 'SearchboxTaskbarMode' -Value 0 -Type DWord -Force"));

        // 2、隐藏“任务视图”按钮
        catalog.Register(F("taskbar.hide-taskview", "隐藏“任务视图”按钮", "外观/资源管理器",
            "按需开启：隐藏任务视图，不影响窗口切换", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'ShowTaskViewButton' -Value 0 -Type DWord -Force"));

        // 3、始终在任务栏显示所有图标和通知
        catalog.Register(F("taskbar.show-all-tray-icons", "始终在任务栏显示所有图标和通知", "外观/资源管理器",
            "按需开启：图标全部显示，任务栏可能拥挤", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'EnableAutoTray' -Value 0 -Type DWord -Force"));

        // 4、任务栏窗口被占满时合并
        catalog.Register(F("taskbar.merge-buttons-always", "任务栏窗口被占满时合并", "外观/资源管理器",
            "建议开启：空间不足才合并，兼顾查看与整洁", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarGlomLevel' -Value 1 -Type DWord -Force"));

        // 5、提高前台程序的显示速度
        catalog.Register(F("explorer.foreground-responsiveness", "提高前台程序的显示速度", "外观/资源管理器",
            "建议开启：前台响应更快，后台任务略受影响", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'ForegroundLockTimeout' -Value 0 -Type DWord -Force"));

        // 6、不要显示窗口出现和消失动画
        catalog.Register(F("performance.disable-animations", "不要显示窗口出现和消失动画", "外观/资源管理器",
            "建议开启：关闭窗口动画，操作更利落", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop\\WindowMetrics' -Name 'MinAnimate' -Value 0 -Type String -Force"));

        // 7、使「开始」菜单、任务栏、操作中心透明
        catalog.Register(F("explorer.enable-transparency", "使「开始」菜单、任务栏、操作中心透明", "外观/资源管理器",
            "按需开启：仅增加透明效果，可能略耗显卡", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize' -Name 'EnableTransparency' -Value 1 -Type DWord -Force"));

        // 8、打开资源管理器时显示此电脑
        catalog.Register(F("explorer.open-this-pc", "打开资源管理器时显示此电脑", "外观/资源管理器",
            "建议开启：打开即见磁盘和常用位置", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'LaunchTo' -Value 1 -Type DWord -Force"));

        // 9、总是从内存中卸载无用的DLL
        catalog.Register(F("explorer.unload-unused-dlls", "总是从内存中卸载无用的DLL", "外观/资源管理器",
            "不建议开启：释放有限，程序重开可能变慢", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'AlwaysUnloadDll' -Value 1 -Type DWord -Force"));

        // 10、记事本启用自动换行
        catalog.Register(F("explorer.notepad-wrap", "记事本启用自动换行", "外观/资源管理器",
            "建议开启：长文本自动换行，阅读更方便", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'fWrap' -Value 1 -Type DWord -Force"));

        // 11、记事本始终显示状态栏
        catalog.Register(F("explorer.notepad-statusbar", "记事本始终显示状态栏", "外观/资源管理器",
            "建议开启：显示行列和缩放信息", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'StatusBar' -Value 1 -Type DWord -Force"));

        // 12、禁止跟踪损坏的快捷方式
        catalog.Register(F("explorer.disable-broken-shortcut-tracking", "禁止跟踪损坏的快捷方式", "外观/资源管理器",
            "建议开启：避免自动寻找失效快捷方式", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoResolveTrack' -Value 1 -Type DWord -Force"));

        // 13、优化Windows文件列表刷新策略
        catalog.Register(F("explorer.no-simple-net-id-list", "优化Windows文件列表刷新策略", "外观/资源管理器",
            "建议开启：文件变化显示更及时", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoSimpleNetIDList' -Value 1 -Type DWord -Force"));

        // 14、显示已知文件类型的扩展名
        catalog.Register(F("explorer.show-extensions", "显示已知文件类型的扩展名", "外观/资源管理器",
            "建议开启：识别文件类型，防止文件伪装", RiskLevel.Recommended, false, RestartRequirement.ExplorerRestart,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'HideFileExt' -Value 0 -Type DWord -Force"));

        // 15、不要保留“最近打开的文件”历史记录
        catalog.Register(F("explorer.no-recent-docs-history", "不要保留“最近打开的文件”历史记录", "外观/资源管理器",
            "按需开启：保护隐私，但失去最近文件", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoRecentDocsHistory' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoRecentDocsHistory' -Value 1 -Type DWord -Force"));

        // 16、退出时清除“最近打开的文件”历史记录
        catalog.Register(F("explorer.clear-recent-on-exit", "退出时清除“最近打开的文件”历史记录", "外观/资源管理器",
            "按需开启：每次退出清空最近文件记录", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'ClearRecentDocsOnExit' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'ClearRecentDocsOnExit' -Value 1 -Type DWord -Force"));

        // 17、创建快捷方式时不添加“快捷方式”字样
        catalog.Register(F("explorer.no-shortcut-suffix", "创建快捷方式时不添加“快捷方式”字样", "外观/资源管理器",
            "建议开启：快捷方式名称更简洁", RiskLevel.Recommended, false, RestartRequirement.ExplorerRestart,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'Link' -Value ([byte[]](0,0,0,0)) -Type Binary -Force"));

        // 18、禁止自动播放
        catalog.Register(F("explorer.disable-autoplay", "禁止自动播放", "外观/资源管理器",
            "建议开启：阻止U盘和光盘内容自动运行", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers' -Name 'DisableAutoplay' -Value 1 -Type DWord -Force"));

        // 19、在单独的进程中打开文件夹窗口
        catalog.Register(F("explorer.separate-process", "在单独的进程中打开文件夹窗口", "外观/资源管理器",
            "按需开启：文件夹更稳定，但略增内存占用", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'SeparateProcess' -Value 1 -Type DWord -Force"));

        // 20、标题栏显示完整路径
        catalog.Register(F("explorer.titlebar-full-path", "标题栏显示完整路径", "外观/资源管理器",
            "按需开启：标题栏显示完整文件路径", RiskLevel.Safe, false, RestartRequirement.ExplorerRestart,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CabinetState' -Name 'FullPath' -Value 1 -Type DWord -Force"));

        // 21、快速访问不显示常用文件夹
        catalog.Register(F("explorer.quick-access-no-frequent", "快速访问不显示常用文件夹", "外观/资源管理器",
            "按需开启：不再展示常用文件夹", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'ShowFrequent' -Value 0 -Type DWord -Force"));

        // 22、快速访问不显示最近使用的文件
        catalog.Register(F("explorer.quick-access-no-recent", "快速访问不显示最近使用的文件", "外观/资源管理器",
            "按需开启：不再展示最近使用的文件", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'ShowRecent' -Value 0 -Type DWord -Force"));

        // 23、资源管理器崩溃时自动重启
        catalog.Register(F("explorer.shell-restart-on-crash", "资源管理器崩溃时自动重启", "外观/资源管理器",
            "建议开启：资源管理器崩溃后自动恢复", RiskLevel.Recommended, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon' -Name 'AutoRestartShell' -Value 1 -Type DWord -Force"));

        // 24、桌面显示此电脑
        catalog.Register(F("explorer.desktop-this-pc", "桌面显示此电脑", "外观/资源管理器",
            "按需开启：桌面增加“此电脑”图标", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel' -Name '{20D04FE0-3AEA-1069-A2D8-08002B30309D}' -Value 0 -Type DWord -Force"));

        // 25、桌面显示回收站
        catalog.Register(F("explorer.desktop-recycle-bin", "桌面显示回收站", "外观/资源管理器",
            "建议开启：方便进入回收站和恢复文件", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel' -Name '{645FF040-5081-101B-9F08-00AA002F954E}' -Value 0 -Type DWord -Force"));

        // 26、隐藏桌面上的“了解此图片”图标
        catalog.Register(F("explorer.hide-spotlight-icon", "隐藏桌面上的“了解此图片”图标", "外观/资源管理器",
            "按需开启：壁纸桌面更干净", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel' -Name '{2cc5ca98-6485-489a-920e-b3e88a6ccce3}' -Value 1 -Type DWord -Force; Remove-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\{2cc5ca98-6485-489a-920e-b3e88a6ccce3}' -Recurse -Force -ErrorAction SilentlyContinue"));

        // 27、微软拼音默认为英文输入
        catalog.Register(F("ime.default-english", "微软拼音默认为英文输入", "外观/资源管理器",
            "按需开启：默认英文，中文输入需切换", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\InputMethod\\Settings\\CHS' -Name 'Default Mode' -Value 1 -Type DWord -Force"));

        // 28、关闭微软拼音云计算
        catalog.Register(F("ime.disable-cloud-suggestion", "关闭微软拼音云计算", "外观/资源管理器",
            "按需开启：减少联网联想，候选词可能变少", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\InputMethod\\Settings\\CHS' -Name 'Enable Cloud Candidate' -Value 0 -Type DWord -Force"));

        // 29、去除本地磁盘重复显示
        catalog.Register(F("explorer.remove-duplicate-drives", "去除本地磁盘重复显示", "外观/资源管理器",
            "建议开启：消除导航栏中磁盘重复显示", RiskLevel.Caution, true, RestartRequirement.ExplorerRestart,
            "Remove-Item -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}' -Recurse -Force -ErrorAction SilentlyContinue"));
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
