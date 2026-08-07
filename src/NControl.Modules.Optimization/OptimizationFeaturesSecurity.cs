using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 安全设置:与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)逐项对齐。
/// 名称/说明/分类均采用文档原文;实现参考 ZyperWin++ 4.2 源码的注册表操作。
/// 安全设置整体按产品文档 §7.2 治理:全部标记 HighRisk,不进任何预设,需用户明确勾选。
/// </summary>
public static class OptimizationFeaturesSecurity
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ===== 文档:安全设置(10 项)=====

        // 1、将用户账号控制（UAC）调整为从不通知
        catalog.Register(F("advanced.disable-uac-notifications", "将用户账号控制（UAC）调整为从不通知", "安全设置",
            "按需开启：免去提权弹窗，需防误操作", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'PromptOnSecureDesktop' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'ConsentPromptBehaviorUser' -Value 3 -Type DWord -Force"));

        // 2、用于内置管理员账户的管理审批模式
        catalog.Register(F("advanced.admin-filter-token", "用于内置管理员账户的管理审批模式", "安全设置",
            "按需开启：提升安全，但会增加确认弹窗", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'FilterAdministratorToken' -Value 1 -Type DWord -Force"));

        // 3、以管理审批模式运行所有管理员
        catalog.Register(F("advanced.admin-enable-lua", "以管理审批模式运行所有管理员", "安全设置",
            "按需开启：提升安全，但会增加提权确认", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableLUA' -Value 1 -Type DWord -Force"));

        // 4、仅提升安全路径下的UIAccess程序
        catalog.Register(F("advanced.secure-uia-paths", "仅提升安全路径下的UIAccess程序", "安全设置",
            "按需开启：提升安全，部分工具可能受限", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableSecureUIAPaths' -Value 1 -Type DWord -Force"));

        // 5、允许UIAccess程序在非安全桌面上提升
        catalog.Register(F("advanced.uia-nonsecure-desktop", "允许UIAccess程序在非安全桌面上提升", "安全设置",
            "按需开启：兼容辅助工具，安全性略降", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableUIADesktopToggle' -Value 1 -Type DWord -Force"));

        // 6、关闭SmartScreen应用筛选器
        catalog.Register(F("advanced.disable-smartscreen", "关闭SmartScreen应用筛选器", "安全设置",
            "按需开启：减少误报拦截，下载需自行判断", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'SmartScreenEnabled' -Value 'off' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Policies\\Microsoft\\Windows Defender\\SmartScreen' -Name 'EnableSmartScreen' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Policies\\Microsoft\\MicrosoftEdge\\PhishingFilter' -Name 'EnabledV9' -Value 0 -Type DWord -Force"));

        // 7、关闭打开程序的安全警告
        catalog.Register(F("advanced.disable-open-security-warning", "关闭打开程序的安全警告", "安全设置",
            "按需开启：打开程序不再警告，需自行辨别", RiskLevel.HighRisk, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Associations' -Name 'ModRiskFileTypes' -Value '.bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Associations' -Name 'ModRiskFileTypes' -Value '.bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd' -Type String -Force"));

        // 8、关闭防火墙
        catalog.Register(F("advanced.disable-firewall", "关闭防火墙", "安全设置",
            "按需开启：有第三方防护或纯内网时可关", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force"));

        // 9、关闭内存完整
        catalog.Register(F("advanced.disable-memory-integrity", "关闭内存完整", "安全设置",
            "按需开启：兼容性和性能提高，防护下降", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity' -Name 'Enabled' -Value 0 -Type DWord -Force"));

        // 10、关闭虚拟化安全性
        catalog.Register(F("advanced.disable-vbs", "关闭虚拟化安全性", "安全设置",
            "按需开启：性能可能提高，隔离防护下降", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard' -Name 'EnableVirtualizationBasedSecurity' -Value 0 -Type DWord -Force"));

        // ===== 新增:安全中心/Defender 开关(参考用户提供的“开关安全中心”工具 + ZyperWin++ Defender.cs)=====
        // 调用随程序分发的 SuperUser 提权工具执行 DEFENDER.CMD,完整处理服务/进程/实时防护/计划任务/SmartScreen。
        // 前置检测:Defender 篡改保护(TamperProtection)开启时拦截一切服务键写入,需先引导用户手动关闭。
        // 禁用后建议安装火绒安全等第三方防护软件(执行完成汇总会提醒)。
        catalog.Register(F("advanced.disable-security-center", "禁用安全中心", "安全设置",
            "按需开启：以 TrustedInstaller 权限完整关闭 Microsoft Defender 与安全中心(服务/进程/实时防护/计划任务/SmartScreen)。\n\n⚠ 前置条件：需先在 Windows 安全中心→病毒和威胁防护→管理设置 中关闭“篡改保护”，否则会被 Defender 自我保护拦截。\n⚠ 安全提示：关闭后系统将失去 Defender 防护，建议安装火绒安全等第三方防护软件并开启实时防护。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "$tp = (Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows Defender\\Features' -ErrorAction SilentlyContinue).TamperProtection; if ($tp -eq 4) { Write-Error 'Defender 篡改保护(Tamper Protection)正在运行,拦截对安全中心的修改。请先到 Windows 安全中心 → 病毒和威胁防护 → 管理设置,关闭“篡改保护”后重试。'; exit 1 }; $d = [AppContext]::BaseDirectory; & \"$d\\Tools\\SecurityCenter\\SuperUser64.exe\" /w /c \"$d\\Tools\\SecurityCenter\\DEFENDER.CMD\" \"Disable\"; if ($LASTEXITCODE -ne 0) { Write-Error \"SuperUser 执行失败,退出码 $LASTEXITCODE\" }"));
        catalog.Register(F("advanced.enable-security-center", "启用安全中心", "安全设置",
            "按需开启：以 TrustedInstaller 权限恢复 Microsoft Defender 与安全中心至初始状态(服务/实时防护/计划任务/SmartScreen)\n\n⚠ 前置条件：需先在 Windows 安全中心关闭“篡改保护”，否则会被 Defender 自我保护拦截。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "$tp = (Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows Defender\\Features' -ErrorAction SilentlyContinue).TamperProtection; if ($tp -eq 4) { Write-Error 'Defender 篡改保护(Tamper Protection)正在运行,拦截对安全中心的修改。请先到 Windows 安全中心 → 病毒和威胁防护 → 管理设置,关闭“篡改保护”后重试。'; exit 1 }; $d = [AppContext]::BaseDirectory; & \"$d\\Tools\\SecurityCenter\\SuperUser64.exe\" /w /c \"$d\\Tools\\SecurityCenter\\DEFENDER.CMD\" \"Enable\"; if ($LASTEXITCODE -ne 0) { Write-Error \"SuperUser 执行失败,退出码 $LASTEXITCODE\" }"));
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
