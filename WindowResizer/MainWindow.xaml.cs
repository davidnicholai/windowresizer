using System;
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
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out SystemRect rect);

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
        private bool _isToggled;
        private ScreenCalculator _screenCalculator;

        public MainWindow()
        {
            InitializeComponent();
            _screenCalculator = new ScreenCalculator();
            _proc = HookCallback;

            taskbarNotification = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon();

            OnKeyPressed += foo_OnKeyPressed;
            HookKeyboard();
        }

        #region Methods related to the software

        private void foo_OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.KeyPressed == Key.F7)
            {
                MoveToCenter();
            }
            else if (e.KeyPressed == Key.F8)
            {
                MoveToCenterWithGaps();
            }
        }

        private void MoveToCenterWithGaps()
        {
            var screenWidth = (int)SystemParameters.WorkArea.Width;
            var screenHeight = (int)SystemParameters.WorkArea.Height;

            if (double.TryParse(PercentageBox.Text, out double result))
            {
                SystemRect rect = _screenCalculator.ChangeWindowSize(screenWidth, screenHeight, result, result);

                var windowWidth = _screenCalculator.ComputeForWindowLength(rect.Right, rect.Left);
                var windowHeight = _screenCalculator.ComputeForWindowLength(rect.Bottom, rect.Top);

                int x = _screenCalculator.ComputeForX(screenWidth, rect);
                int y = _screenCalculator.ComputeForY(screenHeight, rect);

                SetWindowPos(GetForegroundWindow(), (IntPtr)SpecialWindowHandles.HWND_TOP, x, y, windowWidth, windowHeight, SetWindowPosFlags.SWP_SHOWWINDOW);
            }
        }

        /// <summary>
        ///     The computation should be:
        ///     ((screenSizeX - windowSizeX) / 2)
        ///     ((screenSizeY - windowSizeY) / 2)
        /// </summary>
        private void MoveToCenter()
        {
            IntPtr active = GetForegroundWindow();

            var screenWidth = (int)SystemParameters.WorkArea.Width;
            var screenHeight = (int)SystemParameters.WorkArea.Height;

            GetWindowRect(active, out SystemRect rect);

            var windowWidth = _screenCalculator.ComputeForWindowLength(rect.Right, rect.Left);
            var windowHeight = _screenCalculator.ComputeForWindowLength(rect.Bottom, rect.Top);

            int x = _screenCalculator.ComputeForX(screenWidth, rect);
            int y = _screenCalculator.ComputeForY(screenHeight, rect);
            
            SetWindowPos(active, (IntPtr)SpecialWindowHandles.HWND_TOP, x, y, windowWidth, windowHeight, SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        private int computeToCurrentMonitor(double dblCurrentScreenWidth, int windowFromOuter, int windowFromInner)
        {
            int currentScreenWidth = (int)dblCurrentScreenWidth;

            // This means that the window is at the primary monitor.
            if (windowFromOuter < currentScreenWidth)
            {
                return (currentScreenWidth - (windowFromOuter - windowFromInner)) / 2;
            }

            return ((currentScreenWidth - (windowFromOuter - windowFromInner)) / 2) + currentScreenWidth;
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
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
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

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            _isToggled = !_isToggled;

            if (_isToggled)
            {
                HookKeyboard();
            }
            else
            {
                UnHookKeyboard();
            }

            System.Diagnostics.Debug.WriteLine(_isToggled);
        }

        #endregion

        #region Hide to System Tray

        private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskbarNotification;

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
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
