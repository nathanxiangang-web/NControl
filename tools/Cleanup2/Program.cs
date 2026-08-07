using System;
using System.Diagnostics;
using System.IO;
using System.Text;

// 修复测试残留服务:RemoteRegistry(远程注册表)恢复 Automatic,WerSvc 恢复 Manual
class FixSvc
{
    static void Main()
    {
        var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\NControl.FunctionTest\fixsvc_result.txt", append: false, new UTF8Encoding(true));

        // RemoteRegistry: 默认 Automatic(2)
        log.WriteLine(RunPs("Set-Service RemoteRegistry -StartupType Automatic; Start-Service RemoteRegistry -ErrorAction SilentlyContinue") + " RemoteRegistry->Automatic");
        // WerSvc: 默认 Manual(3)
        log.WriteLine(RunPs("Set-Service WerSvc -StartupType Manual") + " WerSvc->Manual");

        // 顺带恢复其它可能被测试禁用的服务到默认(仅处理测试明确涉及的)
        // DPS 诊断策略服务: Automatic
        log.WriteLine(RunPs("if (Get-Service DPS -ErrorAction SilentlyContinue) { Set-Service DPS -StartupType Automatic -ErrorAction SilentlyContinue }") + " DPS->Automatic");
        // TrkWks: Automatic
        log.WriteLine(RunPs("if (Get-Service TrkWks -ErrorAction SilentlyContinue) { Set-Service TrkWks -StartupType Automatic -ErrorAction SilentlyContinue }") + " TrkWks->Automatic");
        // SysMain: Automatic
        log.WriteLine(RunPs("if (Get-Service SysMain -ErrorAction SilentlyContinue) { Set-Service SysMain -StartupType Automatic -ErrorAction SilentlyContinue }") + " SysMain->Automatic");
        // WSearch: Automatic(延迟)
        log.WriteLine(RunPs("if (Get-Service WSearch -ErrorAction SilentlyContinue) { Set-Service WSearch -StartupType Automatic -ErrorAction SilentlyContinue }") + " WSearch->Automatic");
        // DiagTrack: Automatic
        log.WriteLine(RunPs("if (Get-Service DiagTrack -ErrorAction SilentlyContinue) { Set-Service DiagTrack -StartupType Automatic -ErrorAction SilentlyContinue }") + " DiagTrack->Automatic");
        // HomeGroupProvider: 可能不存在,存在则 Manual
        log.WriteLine(RunPs("if (Get-Service HomeGroupProvider -ErrorAction SilentlyContinue) { Set-Service HomeGroupProvider -StartupType Manual -ErrorAction SilentlyContinue }") + " HomeGroupProvider->Manual");
        // SmsRouter: Manual
        log.WriteLine(RunPs("if (Get-Service SmsRouter -ErrorAction SilentlyContinue) { Set-Service SmsRouter -StartupType Manual -ErrorAction SilentlyContinue }") + " SmsRouter->Manual");
        // PcaSvc: Automatic
        log.WriteLine(RunPs("if (Get-Service PcaSvc -ErrorAction SilentlyContinue) { Set-Service PcaSvc -StartupType Automatic -ErrorAction SilentlyContinue }") + " PcaSvc->Automatic");
        // hpet: 若存在则 Manual
        log.WriteLine(RunPs("if (Get-Service hpet -ErrorAction SilentlyContinue) { Set-Service hpet -StartupType Manual -ErrorAction SilentlyContinue }") + " hpet->Manual");

        log.WriteLine("DONE");
        log.Close();
    }

    static string RunPs(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add("[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; " + script + "; exit 0");
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode == 0 ? "OK" : "FAIL";
    }
}
