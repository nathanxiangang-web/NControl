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
    [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--apps")
        {
            RunAppsVerify();
            return;
        }
        if (args.Length > 0 && args[0] == "--gen2")
        {
            RunGen2Verify();
            return;
        }
        if (args.Length > 0 && args[0] == "--components")
        {
            RunComponentsVerify();
            return;
        }
        if (args.Length > 0 && args[0] == "--softinstall")
        {
            RunSoftInstallVerify();
            return;
        }
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

    static void RunAppsVerify()
    {
        var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\apps_uiverify.txt", append: false, new UTF8Encoding(true));
        var L = new Action<string>(s => { Console.WriteLine(s); log.WriteLine(s); log.Flush(); });

        var procs = Process.GetProcessesByName("NControl");
        Process? target = procs.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        if (target is null) { L("FAIL 无主窗口进程"); log.Close(); return; }
        L($"使用 PID {target.Id}");

        var root = AutomationElement.FromHandle(target.MainWindowHandle);
        var nav = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "应用管理"));
        if (nav is null) { L("FAIL 未找到'应用管理'导航"); log.Close(); return; }
        var rc = nav.Current.BoundingRectangle;
        SetForegroundWindow(target.MainWindowHandle);
        System.Threading.Thread.Sleep(300);
        SetCursorPos((int)(rc.X + rc.Width / 2), (int)(rc.Y + rc.Height / 2));
        System.Threading.Thread.Sleep(200);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
        L($"已点击'应用管理'导航({rc.X},{rc.Y})");
        System.Threading.Thread.Sleep(1500);

        var title = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "预装应用"));
        L($"页面'预装应用'文字: {(title is null ? "未找到" : "找到")}");
        var scanBtn = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "重新扫描"));
        L($"'重新扫描'按钮: {(scanBtn is null ? "未找到" : "找到")}");
        var clipchamp = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Clipchamp"));
        L($"'Clipchamp'行: {(clipchamp is null ? "未找到" : "找到")}");
        var storeNote = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "卸载为单向操作,需要时可从 Microsoft Store 重新安装。"));
        L($"卸载说明文案: {(storeNote is null ? "未找到" : "找到")}");
        if (storeNote is null)
        {
            // 长文本可能被 TextWrapping 拆分,用前缀匹配
            var all1 = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            string? note = null;
            foreach (AutomationElement e in all1)
            {
                var n = e.Current.Name;
                if (n is not null && n.Contains("Microsoft Store", StringComparison.OrdinalIgnoreCase)) { note = n; break; }
            }
            L($"卸载说明(模糊匹配): {(note is null ? "仍未找到" : note)}");
        }
        if (scanBtn is not null)
        {
            var sr = scanBtn.Current.BoundingRectangle;
            SetCursorPos((int)(sr.X + sr.Width / 2), (int)(sr.Y + sr.Height / 2));
            System.Threading.Thread.Sleep(200);
            mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            L($"已点击'重新扫描'按钮({sr.X},{sr.Y})");
            System.Threading.Thread.Sleep(6000);
            // 扫描完成文案是完整句子,用前缀匹配
            var all2 = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            string? scanText = null;
            foreach (AutomationElement e in all2)
            {
                var n = e.Current.Name;
                if (n is not null && n.StartsWith("扫描完成", StringComparison.Ordinal)) { scanText = n; break; }
            }
            L($"扫描结果文案: {(scanText is null ? "未找到'扫描完成'" : $"找到: {scanText}")}");
        }
        log.Close();
    }

    static void RunGen2Verify()
    {
        var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\gen2_uiverify.txt", append: false, new UTF8Encoding(true));
        var L = new Action<string>(s => { Console.WriteLine(s); log.WriteLine(s); log.Flush(); });
        var procs = Process.GetProcessesByName("NControl");
        Process? target = procs.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        if (target is null) { L("FAIL 无主窗口进程"); log.Close(); return; }
        var root = AutomationElement.FromHandle(target.MainWindowHandle);

        // 1. 一键优化页:我的方案区块
        Click(root, target, "一键优化", L);
        System.Threading.Thread.Sleep(1200);
        var planTitle = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "我的方案"));
        L($"一键优化页'我的方案'区块: {(planTitle is null ? "未找到" : "找到")}");
        var saveBtn = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "保存为我的方案"));
        L($"'保存为我的方案'按钮: {(saveBtn is null ? "未找到" : "找到")}");
        var exportBtn = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "导出方案"));
        L($"'导出方案'按钮: {(exportBtn is null ? "未找到" : "找到")}");
        var importBtn = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "从配置导入"));
        L($"'从配置导入'按钮: {(importBtn is null ? "未找到" : "找到")}");

        // 2. 清理维护页:扫描区块
        Click(root, target, "清理维护", L);
        System.Threading.Thread.Sleep(1200);
        var scanTitle = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "扫描可清理内容"));
        L($"清理页'扫描可清理内容'区块: {(scanTitle is null ? "未找到" : "找到")}");
        var startScan = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "开始扫描"));
        L($"'开始扫描'按钮: {(startScan is null ? "未找到" : "找到")}");
        if (startScan is not null)
        {
            var sr = startScan.Current.BoundingRectangle;
            SetForegroundWindow(target.MainWindowHandle);
            System.Threading.Thread.Sleep(200);
            SetCursorPos((int)(sr.X + sr.Width / 2), (int)(sr.Y + sr.Height / 2));
            System.Threading.Thread.Sleep(150);
            mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            L("已点击'开始扫描'");
            System.Threading.Thread.Sleep(25000);
            // 扫描完成文案前缀匹配
            var all3 = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            string? scanDone = null;
            foreach (AutomationElement e in all3)
            {
                var n = e.Current.Name;
                if (n is not null && n.StartsWith("扫描完成", StringComparison.Ordinal)) { scanDone = n; break; }
            }
            L($"扫描结果: {(scanDone is null ? "未找到'扫描完成'" : scanDone)}");
        }
        log.Close();
    }

    static void Click(AutomationElement root, Process target, string name, Action<string> L)
    {
        var el = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
        if (el is null) { L($"FAIL 未找到元素 '{name}'"); return; }
        var r = el.Current.BoundingRectangle;
        SetForegroundWindow(target.MainWindowHandle);
        System.Threading.Thread.Sleep(200);
        SetCursorPos((int)(r.X + r.Width / 2), (int)(r.Y + r.Height / 2));
        System.Threading.Thread.Sleep(150);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
        L($"已点击 '{name}'");
    }

    static void RunComponentsVerify()
    {
        var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\components_uiverify.txt", append: false, new UTF8Encoding(true));
        var L = new Action<string>(s => { Console.WriteLine(s); log.WriteLine(s); log.Flush(); });
        var procs = Process.GetProcessesByName("NControl");
        Process? target = procs.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        if (target is null) { L("FAIL 无主窗口进程"); log.Close(); return; }
        var root = AutomationElement.FromHandle(target.MainWindowHandle);

        // 1. 进入应用管理
        Click(root, target, "应用管理", L);
        System.Threading.Thread.Sleep(1200);
        // 2. 点击 Windows 组件 Tab(RadioButton)
        var tab = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Windows 组件"));
        if (tab is null) { L("FAIL 未找到 Windows 组件 Tab"); log.Close(); return; }
        var tr = tab.Current.BoundingRectangle;
        SetForegroundWindow(target.MainWindowHandle);
        System.Threading.Thread.Sleep(200);
        SetCursorPos((int)(tr.X + tr.Width / 2), (int)(tr.Y + tr.Height / 2));
        System.Threading.Thread.Sleep(150);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
        L("已点击 Windows 组件 Tab");
        System.Threading.Thread.Sleep(1200);

        // 3. 检查 6 个组件行
        foreach (var name in new[] { "任务栏搜索框", "任务视图按钮", "小组件按钮" })
        {
            var el = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
            L($"组件行 [{name}]: {(el is null ? "未找到" : "找到")}");
        }
        // 4. 检查状态文本
        var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        foreach (AutomationElement e in all)
        {
            var n = e.Current.Name;
            if (n is not null && (n.StartsWith("当前:开") || n.StartsWith("当前:关") || n.StartsWith("未配置")))
            {
                L($"状态: {n}");
            }
        }
        log.Close();
    }

    static void RunSoftInstallVerify()
    {
        var log = new StreamWriter(@"C:\Users\test\.openclaw\workspace\tools\softinstall_uiverify.txt", append: false, new UTF8Encoding(true));
        var L = new Action<string>(s => { Console.WriteLine(s); log.WriteLine(s); log.Flush(); });
        var procs = Process.GetProcessesByName("NControl");
        Process? target = procs.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        if (target is null) { L("FAIL 无主窗口进程"); log.Close(); return; }
        var root = AutomationElement.FromHandle(target.MainWindowHandle);
        SetForegroundWindow(target.MainWindowHandle);
        System.Threading.Thread.Sleep(300);

        // 键盘激活导航
        KeyActivate(root, "应用管理", L);
        System.Threading.Thread.Sleep(1200);
        KeyActivate(root, "软件安装", L);
        System.Threading.Thread.Sleep(1200);

        // 检查新条目
        foreach (var name in new[] { "StartAllBack", "GeekUninstaller" })
        {
            var el = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
            L($"条目 [{name}]: {(el is null ? "未找到" : "找到")}");
        }
        // 安装按钮
        var btns = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "安装"));
        L($"'安装'按钮数: {btns.Count}");
        log.Close();
    }

    static void KeyActivate(AutomationElement root, string name, Action<string> L)
    {
        var txt = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
        if (txt is null) { L($"FAIL '{name}' 未找到"); return; }
        AutomationElement? el = txt;
        while (el is not null && el.Current.ControlType != ControlType.RadioButton && el.Current.ControlType != ControlType.Button)
            el = TreeWalker.ControlViewWalker.GetParent(el);
        if (el is null) { L($"FAIL '{name}' 无激活元素"); return; }
        el.SetFocus();
        System.Threading.Thread.Sleep(300);
        keybd_event(0x20, 0, 0, UIntPtr.Zero);
        keybd_event(0x20, 0, 2, UIntPtr.Zero);
        L($"已键盘激活 '{name}'");
    }
}
