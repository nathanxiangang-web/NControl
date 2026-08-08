using System.Security.Principal;

namespace NControl.Infrastructure;

/// <summary>权限辅助:判断当前进程是否以管理员身份运行。</summary>
public static class ElevationHelper
{
    public static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
