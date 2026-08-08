using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 任务栏与开始菜单、资源管理器功能(来自 ZyperWin++ 功能池适配,2026-08-02 数据)。
/// 按《产品开发文档 v0.2》§8 规则:重写名称/说明/风险分级;仅参考注册表操作,不复制其页面与流程。
/// </summary>
public static class OptimizationFeaturesExplorer
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ===== 任务栏与开始菜单 =====
        catalog.Register(F("taskbar.show-all-tray-icons", "始终显示所有托盘图标", "任务栏与开始菜单",
            "任务栏通知区域始终显示所有图标,不折叠隐藏。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'EnableAutoTray' -Value 0 -Type DWord -Force"));
        catalog.Register(F("taskbar.merge-buttons-always", "任务栏按钮始终合并", "任务栏与开始菜单",
            "任务栏窗口按钮在占满时始终合并为同一组,与 Windows 10 行为一致。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarGlomLevel' -Value 1 -Type DWord -Force"));
        catalog.Register(F("taskbar.clock-show-seconds", "任务栏时钟显示秒数", "任务栏与开始菜单",
            "任务栏右下角时钟显示秒;需要较新的 Windows 11 版本。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'ShowSecondsInSystemClock' -Value 1 -Type DWord -Force"));

        // ===== 资源管理器 =====
        catalog.Register(F("explorer.foreground-responsiveness", "加快前台窗口响应", "资源管理器",
            "缩短系统判定窗口被占用而限制其响应的时长,前台切换更跟手。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'ForegroundLockTimeout' -Value 0 -Type DWord -Force"));
        catalog.Register(F("explorer.unload-unused-dlls", "及时卸载无用的 DLL", "资源管理器",
            "允许系统卸载不再使用的动态库,减少内存占用;效果依赖实际负载。", RiskLevel.Caution, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'AlwaysUnloadDll' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.notepad-wrap", "记事本启用自动换行", "资源管理器",
            "记事本默认启用自动换行,长文本不再横向滚动。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'fWrap' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.notepad-statusbar", "记事本显示状态栏", "资源管理器",
            "记事本底部显示行号与列号等状态信息。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'StatusBar' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.disable-broken-shortcut-tracking", "禁止跟踪损坏的快捷方式", "资源管理器",
            "系统不再反复解析失效的快捷方式,减少资源管理器卡顿。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoResolveTrack' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.no-recent-docs-history", "不保留最近打开文件记录", "资源管理器",
            "不记录最近打开过的文档与程序历史。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoRecentDocsHistory' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'NoRecentDocsHistory' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.clear-recent-on-exit", "退出时清除最近打开记录", "资源管理器",
            "每次退出系统时清除最近打开的文件与程序记录。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'ClearRecentDocsOnExit' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer' -Name 'ClearRecentDocsOnExit' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.no-shortcut-suffix", "创建快捷方式不带“快捷方式”字样", "资源管理器",
            "新建快捷方式时文件名不再追加“快捷方式”后缀。", RiskLevel.Safe, false, RestartRequirement.ExplorerRestart,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'Link' -Value ([byte[]](0,0,0,0)) -Type Binary -Force"));
        catalog.Register(F("explorer.disable-autoplay", "禁止自动播放", "资源管理器",
            "插入光盘或移动存储时不再自动运行其中的程序,降低恶意内容风险。", RiskLevel.Recommended, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers' -Name 'DisableAutoplay' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.quick-access-no-frequent", "快速访问不显示常用文件夹", "资源管理器",
            "快速访问中隐藏自动统计的常用文件夹。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'ShowFrequent' -Value 0 -Type DWord -Force"));
        catalog.Register(F("explorer.quick-access-no-recent", "快速访问不显示最近使用的文件", "资源管理器",
            "快速访问中隐藏最近使用的文件列表。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'ShowRecent' -Value 0 -Type DWord -Force"));
        catalog.Register(F("explorer.shell-restart-on-crash", "资源管理器崩溃时自动重启", "资源管理器",
            "资源管理器异常退出后自动重新启动,减少桌面丢失图标与任务栏的情况。", RiskLevel.Safe, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon' -Name 'AutoRestartShell' -Value 1 -Type DWord -Force"));
        catalog.Register(F("explorer.desktop-this-pc", "桌面显示“此电脑”图标", "资源管理器",
            "在桌面上显示“此电脑”图标,便于快速访问磁盘与设备。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel' -Name '{20D04FE0-3AEA-1069-A2D8-08002B30309D}' -Value 0 -Type DWord -Force"));
        catalog.Register(F("explorer.desktop-recycle-bin", "桌面显示“回收站”图标", "资源管理器",
            "在桌面上显示“回收站”图标。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel' -Name '{645FF040-5081-101B-9F08-00AA002F954E}' -Value 0 -Type DWord -Force"));
        catalog.Register(F("explorer.hide-spotlight-icon", "隐藏桌面“了解此图片”图标", "资源管理器",
            "隐藏桌面壁纸右下角的“了解此图片”提示图标。", RiskLevel.Safe, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel' -Name '{2cc5ca98-6485-489a-920e-b3e88a6ccce3}' -Value 1 -Type DWord -Force; Remove-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\{2cc5ca98-6485-489a-920e-b3e88a6ccce3}' -Recurse -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("explorer.remove-duplicate-drives", "去除“本地磁盘”重复显示", "系统设置",
            "移除资源管理器中重复显示的“本地磁盘”条目;仅当出现重复显示时使用。", RiskLevel.Caution, true, RestartRequirement.ExplorerRestart,
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
        Source = "ZyperWin++ 适配 · 注册表/系统命令",
        Command = command
    };
}
