using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowResizerForms.Hotkeys;

namespace WindowResizerForms
{
    public partial class Form1 : Form
    {
        private ScreenCalculator _screenCalculator;

        public Form1()
        {
            InitializeComponent();
            _screenCalculator = new ScreenCalculator();

            RegisterHotkeys();
        }

        public Rectangle GetMonitorNumber(Rectangle rectangle)
        {
            return Screen.GetWorkingArea(rectangle);
        }

        public Rectangle ToRectangle(SystemRect systemRect)
        {
            return new Rectangle
            {
                Width = systemRect.Right - systemRect.Left,
                Height = systemRect.Bottom - systemRect.Top,
                X = systemRect.Left,
                Y = systemRect.Top,
                Location = new Point(systemRect.Left, systemRect.Top),
                Size = new Size(systemRect.Right - systemRect.Left, systemRect.Bottom - systemRect.Top)
            };
        }

        /// <summary>
        ///     The computation should be:
        ///     ((screenSizeX - windowSizeX) / 2)
        ///     ((screenSizeY - windowSizeY) / 2)
        /// </summary>
        private void MoveToCenter()
        {
            IntPtr activeWindow = GetForegroundWindow();
            GetWindowRect(activeWindow, out SystemRect windowRect);

            var workingArea = GetMonitorNumber(ToRectangle(windowRect));
            int screenWidth = workingArea.Width;
            int screenHeight = workingArea.Height;

            var windowWidth = _screenCalculator.ComputeForWindowLength(windowRect.Right, windowRect.Left);
            var windowHeight = _screenCalculator.ComputeForWindowLength(windowRect.Bottom, windowRect.Top);

            int x = _screenCalculator.ComputeForX(screenWidth, windowRect, workingArea.X);
            int y = _screenCalculator.ComputeForY(screenHeight, windowRect, workingArea.Y);

            System.Diagnostics.Debug.WriteLine($"x: {x} / y: {y} / windowWidth: {windowWidth} / windowHeight: {windowHeight}");

            SetWindowPos(activeWindow, (IntPtr)SpecialWindowHandles.HWND_TOP, x, y, windowWidth, windowHeight, SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        private void MoveToCenterWithGaps()
        {
            IntPtr activeWindow = GetForegroundWindow();
            GetWindowRect(activeWindow, out SystemRect windowRect);

            var workingArea = GetMonitorNumber(ToRectangle(windowRect));
            int screenWidth = workingArea.Width;
            int screenHeight = workingArea.Height;

            string percentageText = "0.95";

            if (double.TryParse(percentageText, out double result))
            {
                SystemRect rect = _screenCalculator.ChangeWindowSize(screenWidth, screenHeight, result, result);

                var windowWidth = _screenCalculator.ComputeForWindowLength(rect.Right, rect.Left);
                var windowHeight = _screenCalculator.ComputeForWindowLength(rect.Bottom, rect.Top);

                int x = _screenCalculator.ComputeForX(screenWidth, rect, workingArea.X);
                int y = _screenCalculator.ComputeForY(screenHeight, rect, workingArea.Y);

                System.Diagnostics.Debug.WriteLine($"x: {x} / y: {y} / windowWidth: {windowWidth} / windowHeight: {windowHeight}");

                SetWindowPos(GetForegroundWindow(), (IntPtr)SpecialWindowHandles.HWND_TOP, x, y, windowWidth, windowHeight, SetWindowPosFlags.SWP_SHOWWINDOW);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                /* Note that the three lines below are not needed if you only want to register one hotkey.
                 * The below lines are useful in case you want to register multiple keys, which you can use a switch with the id as argument, or if you want to know which key/modifier was pressed for some particular reason. */

                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.

                HandleHotkey(modifier, key, id);
            }
        }

        private void HandleHotkey(KeyModifier keyModifier, Keys key, int id)
        {
            System.Diagnostics.Debug.WriteLine($"{keyModifier} {key.ToString()} {id}");
            if (keyModifier == KeyModifier.Alt && key == Keys.C)
            {
                MoveToCenter();
            }
            else if (keyModifier == KeyModifier.Alt && key == Keys.V)
            {
                UnregisterHotkeys();
            }
            else if (keyModifier == KeyModifier.Alt && key == Keys.G)
            {
                MoveToCenterWithGaps();
            }
            else if (keyModifier == KeyModifier.Alt && key == Keys.M)
            {
                MinimizeToSystemTray();
            }
        }

        private void RegisterHotkeys()
        {
            RegisterHotKey(Handle, 0, (int)KeyModifier.Alt, Keys.C.GetHashCode());
            RegisterHotKey(Handle, 1, (int)KeyModifier.Alt, Keys.V.GetHashCode());
            RegisterHotKey(Handle, 2, (int)KeyModifier.Alt, Keys.G.GetHashCode());
            RegisterHotKey(Handle, 3, (int)KeyModifier.Alt, Keys.M.GetHashCode());
        }

        private void UnregisterHotkeys()
        {
            UnregisterHotKey(Handle, 0);
            UnregisterHotKey(Handle, 1);
            UnregisterHotKey(Handle, 2);
            UnregisterHotKey(Handle, 3);
        }

        private void MinimizeToSystemTray()
        {
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowResizerForms.NOTES.ICO");
            NotifyIcon notifyIcon = new NotifyIcon(components)
            {
                Icon = new Icon(resource),
                BalloonTipText = "I'll be here for a while. You can double click me in the taskbar if you miss me.",
                BalloonTipTitle = "Minimized to System Tray",
                Text = "Window Resizer",
                Visible = true
            };

            notifyIcon.ShowBalloonTip(5000);
            Hide();

            notifyIcon.DoubleClick += new System.EventHandler(NotifyIcon_DoubleClick);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
            Show();
            TopMost = true;
        }

        #region PInvoke Methods

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        ///     SetWindowsPos(IntPtr handle, IntPtr handleWindowInsertAfter (optional), int x, int y, int width, int height, uint uFlags)
        ///     For uFlags, you can just use 0x0004 for SWP_NOZORDER
        ///     http://www.pinvoke.net/default.aspx/user32.SetWindowPos
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out SystemRect rect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion
    }
}
