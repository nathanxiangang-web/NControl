using Microsoft.Win32;
using NControl.Core;

namespace NControl.Infrastructure;

/// <summary>
/// 环境探测:从注册表与系统 API 读取当前 Windows 环境信息(第二代 §4)。
/// 全部为本地只读操作,不联网、不收集个人标识信息(符合 §16 隐私原则)。
/// </summary>
public sealed class WindowsEnvironmentProbe : IEnvironmentProbe
{
    public WindowsEnvironmentInfo GetEnvironment()
    {
        int major = 10, minor = 0, build = 0, ubr = 0;
        string arch = "x64", productType = "Unknown";

        try
        {
            // 版本信息(Windows NT CurrentVersion)
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key is not null)
            {
                major = Convert.ToInt32(key.GetValue("CurrentMajorVersionNumber") ?? 10);
                minor = Convert.ToInt32(key.GetValue("CurrentMinorVersionNumber") ?? 0);
                build = Convert.ToInt32(key.GetValue("CurrentBuildNumber") ?? 0);
                ubr = Convert.ToInt32(key.GetValue("UBR") ?? 0);
            }

            // 架构(64 位视图下读取)
            using var envKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            var procArch = envKey?.GetValue("PROCESSOR_ARCHITECTURE") as string;
            arch = procArch switch
            {
                "AMD64" => "x64",
                "ARM64" => "arm64",
                "x86" => "x86",
                _ => procArch ?? "x64"
            };

            // 产品类型(专业版/家庭版/企业版)
            using var policyKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var edition = policyKey?.GetValue("EditionID") as string;
            if (!string.IsNullOrEmpty(edition))
                productType = MapEdition(edition);
        }
        catch
        {
            // 保守默认:Windows 10 x64,构建 0 → 上层按 Unknown 处理
        }

        return new WindowsEnvironmentInfo
        {
            MajorVersion = major,
            MinorVersion = minor,
            BuildNumber = build,
            Ubr = ubr,
            Architecture = arch,
            ProductType = productType
        };
    }

    private static string MapEdition(string editionId) => editionId.ToUpperInvariant() switch
    {
        "PROFESSIONAL" or "PROFESSIONALN" or "PROFESSIONALEDITION" => "Professional",
        "PROFESSIONALWORKSTATION" or "PROFESSIONALWORKSTATIONN" => "ProWorkstation",
        "HOME" or "HOMEN" or "CORE" or "CORN" or "COREONLY" or "COREONLYN" => "Home",
        "ENTERPRISE" or "ENTERPRISEN" or "ENTERPRISEG" or "ENTERPRISEGN" or "ENTERPRISEEVALUATION" => "Enterprise",
        "EDUCATION" or "EDUCATIONN" => "Education",
        "SERVERSTANDARD" or "SERVERSTANDARDCORE" or "SERVERDATACENTER" or "SERVERDATACENTERCORE" => "Server",
        "CLOUD" or "CLOUDN" or "CLOUDSOLUTION" or "CLOUDSOLUTIONN" => "Cloud",
        _ => editionId
    };
}
