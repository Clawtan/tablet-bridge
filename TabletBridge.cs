using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TabletBridge
{
    public class MessageWindow : NativeWindow
    {
        private Action onHotkey;
        private const int WM_HOTKEY = 0x0312;

        public MessageWindow(Action onHotkeyAction)
        {
            this.onHotkey = onHotkeyAction;
            this.CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                if (onHotkey != null) onHotkey();
            }
            base.WndProc(ref m);
        }
    }

    public class TabletBridgeTray
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private const int HOTKEY_ID = 9001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_Q = 0x51;

        private const byte VK_RCONTROL = 0xA3;
        private const byte VK_P = 0x50;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const int MK_LBUTTON = 0x0001;
        private const int SW_SHOW = 5;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Process scrcpyProcess;
        private IntPtr lastPcWindow = IntPtr.Zero;
        private bool isTabletActive = false;
        private MessageWindow msgWin;
        private string scrcpyDir;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            new TabletBridgeTray();
            Application.Run();
        }

        public TabletBridgeTray()
        {
            scrcpyDir = AppDomain.CurrentDomain.BaseDirectory;
            msgWin = new MessageWindow(ToggleControl);

            // Register Hotkey: Ctrl + Q
            RegisterHotKey(msgWin.Handle, HOTKEY_ID, MOD_CONTROL | MOD_NOREPEAT, VK_Q);

            // Tray Menu
            trayMenu = new ContextMenuStrip();
            ToolStripMenuItem titleItem = new ToolStripMenuItem("Tablet Bridge (Active)");
            titleItem.Enabled = false;
            trayMenu.Items.Add(titleItem);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Switch Device (Ctrl + Q)", null, (s, e) => ToggleControl());
            trayMenu.Items.Add("Restart Scrcpy", null, (s, e) => StartScrcpy());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => ExitApp());

            // Custom Flat Aesthetic Tablet Icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Tablet Bridge\nHotkey: Ctrl + Q (Auto Sleep/Wake)";
            trayIcon.Icon = GenerateTabletIcon();
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ToggleControl();

            StartScrcpy();
        }

        private static Icon GenerateTabletIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Body / Frame (White outline, transparent background)
                Rectangle bodyRect = new Rectangle(6, 2, 20, 28);
                using (GraphicsPath path = RoundedRect(bodyRect, 4))
                {
                    using (Pen whitePen = new Pen(Color.White, 1.8f))
                    {
                        g.DrawPath(whitePen, path);
                    }
                }

                // Screen (Subtle frosted glass accent with thin white border)
                Rectangle screenRect = new Rectangle(9, 6, 14, 17);
                using (GraphicsPath screenPath = RoundedRect(screenRect, 2))
                {
                    using (SolidBrush screenBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    {
                        g.FillPath(screenBrush, screenPath);
                    }
                    using (Pen screenPen = new Pen(Color.FromArgb(140, 255, 255, 255), 1.0f))
                    {
                        g.DrawPath(screenPen, screenPath);
                    }
                }

                // Camera Dot at Top
                using (SolidBrush dotBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(dotBrush, 15f, 3.5f, 2f, 2f);
                }

                // Bottom Home Bar
                using (Pen homePen = new Pen(Color.White, 1.6f))
                {
                    homePen.StartCap = LineCap.Round;
                    homePen.EndCap = LineCap.Round;
                    g.DrawLine(homePen, 13, 26, 19, 26);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void StartScrcpy()
        {
            KillExistingScrcpy();

            string scrcpyPath = Path.Combine(scrcpyDir, "scrcpy.exe");
            if (!File.Exists(scrcpyPath))
            {
                MessageBox.Show("scrcpy.exe tidak ditemukan di: " + scrcpyDir, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = scrcpyPath;
            psi.Arguments = "--no-video --no-audio --keyboard=uhid --mouse=uhid --shortcut-mod=rctrl --window-borderless --window-title=Tablet_Bridge_OTG --window-width=2 --window-height=2 --window-x=0 --window-y=0";
            psi.WorkingDirectory = scrcpyDir;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            try
            {
                scrcpyProcess = Process.Start(psi);

                // Strip taskbar icon
                ThreadPool.QueueUserWorkItem(state =>
                {
                    for (int i = 0; i < 30; i++)
                    {
                        Thread.Sleep(200);
                        IntPtr hwnd = GetScrcpyHwnd();
                        if (hwnd != IntPtr.Zero)
                        {
                            StripTaskbarIcon(hwnd);
                            break;
                        }
                    }
                });

                trayIcon.ShowBalloonTip(1500, "Tablet Bridge Active", "Ctrl + Q: Toggle Device\nAuto Sleep/Wake aktif!", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menjalankan scrcpy: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StripTaskbarIcon(IntPtr hwnd)
        {
            try
            {
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                exStyle &= ~WS_EX_APPWINDOW;
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            }
            catch { }
        }

        private IntPtr GetScrcpyHwnd()
        {
            IntPtr hwnd = FindWindow(null, "Tablet_Bridge_OTG");
            if (hwnd != IntPtr.Zero) return hwnd;

            if (scrcpyProcess != null && !scrcpyProcess.HasExited)
            {
                scrcpyProcess.Refresh();
                if (scrcpyProcess.MainWindowHandle != IntPtr.Zero)
                    return scrcpyProcess.MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        private void RunAdbAsync(string arguments)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Path.Combine(scrcpyDir, "adb.exe");
                    psi.Arguments = arguments;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(1000);
                    }
                }
                catch { }
            });
        }

        private void ToggleControl()
        {
            IntPtr scrcpyHwnd = GetScrcpyHwnd();
            if (scrcpyHwnd == IntPtr.Zero)
            {
                StartScrcpy();
                Thread.Sleep(400);
                scrcpyHwnd = GetScrcpyHwnd();
                if (scrcpyHwnd == IntPtr.Zero) return;
            }

            IntPtr fg = GetForegroundWindow();

            if (!isTabletActive || fg != scrcpyHwnd)
            {
                // Switching TO Tablet: Wake up tablet screen
                RunAdbAsync("shell input keyevent 224");

                if (fg != scrcpyHwnd)
                {
                    lastPcWindow = fg;
                }

                StripTaskbarIcon(scrcpyHwnd);
                ShowWindow(scrcpyHwnd, SW_SHOW);
                SetForegroundWindow(scrcpyHwnd);

                // Click to grab mouse
                IntPtr lparam = (IntPtr)((1 << 16) | 1);
                PostMessage(scrcpyHwnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lparam);
                PostMessage(scrcpyHwnd, WM_LBUTTONUP, IntPtr.Zero, lparam);

                isTabletActive = true;
            }
            else
            {
                // Switching back TO PC: Sleep tablet screen & restore PC focus
                RunAdbAsync("shell input keyevent 223");

                isTabletActive = false;
                if (lastPcWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(lastPcWindow);
                }
            }
        }

        private void KillExistingScrcpy()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("scrcpy"))
                {
                    p.Kill();
                }
            }
            catch { }
        }

        private void ExitApp()
        {
            UnregisterHotKey(msgWin.Handle, HOTKEY_ID);
            msgWin.DestroyHandle();

            trayIcon.Visible = false;
            trayIcon.Dispose();

            KillExistingScrcpy();
            Application.Exit();
        }
    }
}
