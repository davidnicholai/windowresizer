using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using WindowResizer.Properties;

namespace WindowResizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region PInvoke Declarations

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId();

        /// <summary>
        /// SetWindowsPos(IntPtr handle, IntPtr handleWindowInsertAfter (optional), int x, int y, int width, int height, uint uFlags)
        /// For uFlags, you can just use 0x0004 for SWP_NOZORDER
        /// http://www.pinvoke.net/default.aspx/user32.SetWindowPos
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out SystemRect rect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public event EventHandler<KeyPressedEventArgs> OnKeyPressed;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();
            _proc = HookCallback;

            taskbarNotification = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon();

            OnKeyPressed += foo_OnKeyPressed;
            HookKeyboard();
        }

        #region Methods related to the software

        private void foo_OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.KeyPressed == Key.C)
            {
                MoveToCenter();
            }
        }

        private void MoveToCenter()
        {
            IntPtr active = GetForegroundWindow();
            System.Diagnostics.Debug.WriteLine(active);

            var screenWidth = SystemParameters.WorkArea.Width;
            var screenHeight = SystemParameters.WorkArea.Height;

            var foobar = GetWindowRect(active, out SystemRect rect);

            var windowWidth = rect.Right - rect.Left;
            var windowHeight = rect.Bottom - rect.Top;

            int x = computeToCurrentMonitor(screenWidth, rect.Right, rect.Left);
            int y = computeToCurrentMonitor(screenHeight, rect.Bottom, rect.Top); 
            
            bool bar = SetWindowPos(active, (IntPtr)SpecialWindowHandles.HWND_TOP, x, y, windowWidth, windowHeight, SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        private int computeToCurrentMonitor(double dblCurrentScreenWidth, int windowFromRight, int windowFromLeft)
        {
            int currentScreenWidth = (int)dblCurrentScreenWidth;

            // This means that the window is at the primary monitor.ccccc
            if (windowFromRight < currentScreenWidth)
            {
                return (windowFromRight - windowFromLeft) / 2;
            }

            return ((windowFromRight - windowFromLeft) / 2) + currentScreenWidth;
        }

        private int computeToCurrentMonitor2(double dblCurrentScreenWidth, int windowFromOuter, int windowFromInner)
        {
            int currentScreenWidth = (int)dblCurrentScreenWidth;

            // This means that the window is at the primary monitor.ccccc
            if (windowFromOuter < currentScreenWidth)
            {
                return (windowFromOuter - windowFromInner) / 2;
            }

            return ((windowFromOuter - windowFromInner) / 2) + currentScreenWidth;
        }

        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);
        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                OnKeyPressed?.Invoke(this, new KeyPressedEventArgs(KeyInterop.KeyFromVirtualKey(vkCode)));
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #endregion

        #region Hide to System Tray

        private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskbarNotification;

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            taskbarNotification = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)FindResource("MyNotifyIcon");

            taskbarNotification.DoubleClickCommand = new MyCommand(() =>
            {
                Show();
                Activate();
                Topmost = true;
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
            });
        }

        #endregion
    }
}
