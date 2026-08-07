// 验证系统设置页:导航 + 截屏 + 检查"已优化"元素
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

class UiVerify
{
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    static void Main()
    {
        var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\NControl.FunctionTest\uiverify.txt", append: false, new UTF8Encoding(true));
        var L = new Action<string>(s => { Console.WriteLine(s); log.WriteLine(s); log.Flush(); });

        var procs = Process.GetProcessesByName("NControl");
        L($"NControl 进程数: {procs.Length}");
        Process? target = null;
        foreach (var p in procs)
        {
            if (p.MainWindowHandle != IntPtr.Zero) { target = p; break; }
        }
        if (target is null) { L("FAIL 无主窗口进程"); log.Close(); return; }
        L($"使用 PID {target.Id} 窗口 {target.MainWindowHandle}");

        // 用 UIA 找左侧导航"系统设置"按钮并点击
        var root = AutomationElement.FromHandle(target.MainWindowHandle);
        L($"Root: {root.Current.Name} | {root.Current.ControlType.ProgrammaticName}");
        // 先打印所有元素名(诊断)
        var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        L($"元素总数: {all.Count}");
        int printed = 0;
        foreach (AutomationElement e in all)
        {
            var nm = e.Current.Name;
            if (!string.IsNullOrEmpty(nm) && printed < 40)
            {
                L($"元素: [{nm}] ({e.Current.ControlType.ProgrammaticName})");
                printed++;
            }
        }
        // 导航:任何类型控件名为'系统设置'
        var nav = root.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, "系统设置"));
        if (nav is null)
        {
            L("FAIL 未找到导航'系统设置'");
            log.Close();
            return;
        }
        // Text 元素不支持 Invoke,改用鼠标点击其屏幕坐标
        var rect = nav.Current.BoundingRectangle;
        L($"'系统设置'位置: {rect.X},{rect.Y} 尺寸 {rect.Width}x{rect.Height}");
        SetForegroundWindow(target.MainWindowHandle);
        System.Threading.Thread.Sleep(400);
        int cx = (int)(rect.X + rect.Width / 2), cy = (int)(rect.Y + rect.Height / 2);
        SetCursorPos(cx, cy);
        System.Threading.Thread.Sleep(200);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); // left down
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); // left up
        L("已鼠标点击'系统设置'导航");
        System.Threading.Thread.Sleep(1500);

        // 第二步:点击分类 Chip '外观/资源管理器'(本机有已优化项'关闭微软拼音云计算')
        var chip = root.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, "外观/资源管理器"));
        if (chip is not null)
        {
            var rc = chip.Current.BoundingRectangle;
            SetCursorPos((int)(rc.X + rc.Width / 2), (int)(rc.Y + rc.Height / 2));
            System.Threading.Thread.Sleep(200);
            mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            L($"已点击分类 '外观/资源管理器'({rc.X},{rc.Y})");
            System.Threading.Thread.Sleep(1200);
        }
        else
        {
            L("未找到分类 Chip '外观/资源管理器'");
        }

        // 截屏
        SetForegroundWindow(target.MainWindowHandle);
        System.Threading.Thread.Sleep(500);
        GetWindowRect(target.MainWindowHandle, out var r);
        int w = r.R - r.L, h = r.B - r.T;
        using var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(r.L, r.T, 0, 0, bmp.Size);
        var path = @"C:\Users\test\.openclaw\workspace\ncontrol_settings_detect.png";
        bmp.Save(path, ImageFormat.Png);
        L($"截图已保存: {path} ({w}x{h})");

        // 像素扫描:找绿色"已优化"徽标(SuccessSoftBrush #E9F8F0 背景 / 绿字 #2FA36B)
        int greenish = 0, darkgreen = 0, total = 0;
        for (int y = 0; y < h; y += 3)
            for (int x = 0; x < w; x += 3)
            {
                var c = bmp.GetPixel(x, y);
                total++;
                if (c.G > 200 && c.R > 180 && c.B < 220 && (c.G - c.R) > 30 && (c.G - c.B) > 30) greenish++;
                if (c.G > 100 && c.G < 200 && c.R < 100 && c.B < 130) darkgreen++;
            }
        L($"绿色像素(浅): {greenish}/{total}  深绿(文字): {darkgreen}");

        // 检查页面上是否有'已优化'/'状态未知'文字元素(UIA)
        var optText = root.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, "已优化"));
        L($"页面'已优化'文字元素: {(optText is null ? "未找到" : "找到")}");
        if (optText is not null)
        {
            var r2 = optText.Current.BoundingRectangle;
            L($"'已优化'位置: {r2.X},{r2.Y} 尺寸 {r2.Width}x{r2.Height}");
        }
        var unknownText = root.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, "状态未知"));
        L($"页面'状态未知'文字元素: {(unknownText is null ? "未找到" : "找到")}");
        if (unknownText is not null)
        {
            var r3 = unknownText.Current.BoundingRectangle;
            L($"'状态未知'位置: {r3.X},{r3.Y} 尺寸 {r3.Width}x{r3.Height}");
        }

        // 核心验证:已优化的项(关闭微软拼音云计算)的开关是否自动选中
        var optRow = root.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, "关闭微软拼音云计算"));
        if (optRow is null)
        {
            L("FAIL 未找到'关闭微软拼音云计算'行");
        }
        else
        {
            // 向上找容器,再找其中的 ToggleButton/CheckBox
            var rowParent = TreeWalker.RawViewWalker.GetParent(optRow);
            while (rowParent is not null && !(rowParent.Current.ControlType == ControlType.DataItem || rowParent.Current.ControlType == ControlType.ListItem))
                rowParent = TreeWalker.RawViewWalker.GetParent(rowParent);
            if (rowParent is null) { L("FAIL 未找到行容器"); }
            else
            {
                var toggles = rowParent.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox));
                L($"行内 CheckBox 数: {toggles.Count}");
                foreach (AutomationElement t in toggles)
                {
                    var ts = t.GetCurrentPropertyValue(TogglePattern.ToggleStateProperty)?.ToString();
                    L($"开关状态: {ts}");
                }
                if (toggles.Count == 0)
                {
                    // 也可能是 ToggleButton
                    var tgls = rowParent.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                    foreach (AutomationElement t in tgls)
                    {
                        var ts = t.GetCurrentPropertyValue(TogglePattern.ToggleStateProperty)?.ToString();
                        L($"按钮开关状态: [{t.Current.Name}] {ts}");
                    }
                }
            }
        }
        log.Close();
    }
}
