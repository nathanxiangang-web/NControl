using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 高级(高风险)功能:仅出现在“高级”分类,不进任何预设(产品文档 §7.2 高风险功能治理)。
/// 说明必须具体,不使用“可能存在风险”式模糊表述。
/// 注:ZyperWin++ 的“关闭 Windows Defender”经评估不引入(见来源台账)。
/// </summary>
public static class OptimizationFeaturesSecurity
{
    public static void Register(IFunctionCatalog catalog)
    {
        catalog.Register(F("advanced.disable-uac-notifications", "UAC 调整为从不通知", "高级",
            "关闭用户账户控制的全部提示,程序可直接以管理员权限运行。会显著降低系统对恶意提权的防护,请仅在完全可控的环境使用。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'PromptOnSecureDesktop' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'ConsentPromptBehaviorUser' -Value 3 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-smartscreen", "关闭 SmartScreen 应用筛选器", "高级",
            "关闭 SmartScreen 对未知应用的云查杀提示;下载未知程序时不再获得安全警告,风险自担。",
            RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'SmartScreenEnabled' -Value 'off' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Policies\\Microsoft\\Windows Defender\\SmartScreen' -Name 'EnableSmartScreen' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Policies\\Microsoft\\MicrosoftEdge\\PhishingFilter' -Name 'EnabledV9' -Value 0 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-open-security-warning", "关闭打开程序的安全警告", "高级",
            "打开可执行/脚本等高风险文件类型时不再显示安全警告;恶意文件被直接执行的风险显著上升。",
            RiskLevel.HighRisk, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Associations' -Name 'ModRiskFileTypes' -Value '.bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Associations' -Name 'ModRiskFileTypes' -Value '.bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd' -Type String -Force"));
        catalog.Register(F("advanced.disable-firewall", "关闭 Windows 防火墙", "高级",
            "关闭标准/公用/域三套防火墙配置;电脑将直接暴露在未受保护的网络环境中,请仅在隔离网络使用。",
            RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-memory-integrity", "关闭内存完整性", "高级",
            "关闭基于虚拟化的代码完整性(HVCI/内核隔离);内核级恶意代码防护减弱,部分不兼容驱动可运行。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity' -Name 'Enabled' -Value 0 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-vbs", "关闭虚拟化安全性", "高级",
            "关闭基于虚拟化的安全(VBS);影响内存完整性、凭据保护等能力,部分场景性能可能提升。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard' -Name 'EnableVirtualizationBasedSecurity' -Value 0 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-system-restore", "关闭系统还原", "高级",
            "关闭系统还原并清除已有还原点;系统故障时将无法通过还原点恢复。",
            RiskLevel.HighRisk, true, RestartRequirement.None,
            "Disable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore' -Name 'DisableSR' -Value 1 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-windows-update-checks", "从不检查系统更新", "高级",
            "系统不再自动检查任何更新;将长期缺少安全补丁,仅建议在离线或隔离环境使用。",
            RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update' -Name 'AUOptions' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update' -Name 'CachedAUOptions' -Value 1 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-tsx", "关闭 TSX 漏洞补丁", "高级",
            "禁用 Intel TSX 指令集相关漏洞补丁,部分应用性能可能提升;降低对 TSX 侧信道攻击的防护。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel' -Name 'DisableTsx' -Value 1 -Type DWord -Force"));
        catalog.Register(F("advanced.disable-insecure-download-warnings", "关闭不安全下载警告", "高级",
            "浏览器下载非 HTTPS 或来源不明文件时不再显示警告;降低对不安全下载的提醒。",
            RiskLevel.HighRisk, true, RestartRequirement.None,
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
        Source = "ZyperWin++ 适配 · 注册表/系统命令",
        Command = command
    };
}
