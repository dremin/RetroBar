using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ManagedShell.AppBar;
using Microsoft.Win32;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for JapaneseIme.xaml
    /// </summary>
    public partial class JapaneseIme : UserControl
    {
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        // Window
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

        // IME
        [DllImport("imm32.dll")]
        static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        const int WM_IME_CONTROL = 0x0283;

        const uint IME_CMODE_ALPHANUMERIC = 0x0000;
        const uint IME_CMODE_NATIVE = 0x0001;
        const uint IME_CMODE_CHINESE = IME_CMODE_NATIVE;
        const uint IME_CMODE_HANGUL = IME_CMODE_NATIVE;
        const uint IME_CMODE_JAPANESE = IME_CMODE_NATIVE;
        const uint IME_CMODE_KATAKANA = 0x0002;  // only effect under IME_CMODE_NATIVE
        const uint IME_CMODE_LANGUAGE = 0x0003;
        const uint IME_CMODE_FULLSHAPE = 0x0008;
        const uint IME_CMODE_ROMAN = 0x0010;
        const uint IME_CMODE_CHARCODE = 0x0020;
        const uint IME_CMODE_HANJACONVERT = 0x0040;

        const uint IMC_GETCONVERSIONMODE = 0x0001;
        const uint IMC_SETCONVERSIONMODE = 0x0002;
        const uint IMC_GETSENTENCEMODE = 0x0003;
        const uint IMC_SETSENTENCEMODE = 0x0004;
        const uint IMC_GETOPENSTATUS = 0x0005;
        const uint IMC_SETOPENSTATUS = 0x0006;

        // Regstory
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegOpenKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(UIntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegQueryValueEx(UIntPtr hKey, string lpValueName, IntPtr lpReserved, out uint lpType, out uint lpData, ref uint lpcbData);

        const int ERROR_SUCCESS = 0;

        const uint  REG_DWORD = 4; // 32-bit number
        const uint  REG_SIZE_DWORD = 4; // 32-bit number

        // SendInput
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            // MOUSEINPUT and HARDWAREINPUT are unused,
            // but removing them will cause them to not work.
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        const uint INPUT_KEYBOARD = 1;

        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint KEYEVENTF_KEYUP = 0x0002;

        const ushort VK_SHIFT = 0x10;
        const ushort VK_CONTROL = 0x11;
        const ushort VK_F10 = 0x79;
        const ushort VK_OEM_COPY = 0xF2;

        // KeyboardLayout
        [DllImport("user32.dll")]
        static extern uint GetKeyboardLayout(uint idThread);

        private IntPtr hWndCurFg;
        private bool NewImeEnabled;
        private bool CurInputRoma;
        private uint CurInputMode;
        private uint CurImmConversion;

        private readonly DispatcherTimer ImeCheckTimer = new DispatcherTimer(DispatcherPriority.Background);

        private bool _isLoaded;

        public static readonly DependencyProperty JapaneseImeEnabledProperty = DependencyProperty.Register(nameof(JapaneseImeEnabled), typeof(bool), typeof(JapaneseIme));

        public bool JapaneseImeEnabled
        {
            get => (bool)GetValue(JapaneseImeEnabledProperty);
            set => SetValue(JapaneseImeEnabledProperty, value);
        }

        public static readonly DependencyProperty JapaneseImeTipProperty = DependencyProperty.Register(nameof(JapaneseImeTip), typeof(string), typeof(JapaneseIme));

        public string JapaneseImeTip
        {
            get => (string)GetValue(JapaneseImeTipProperty);
            set => SetValue(JapaneseImeTipProperty, value);
        }

        public static readonly DependencyProperty JapaneseImeStatusProperty = DependencyProperty.Register(nameof(JapaneseImeStatus), typeof(string), typeof(JapaneseIme));

        public string JapaneseImeStatus
        {
            get => (string)GetValue(JapaneseImeStatusProperty);
            set => SetValue(JapaneseImeStatusProperty, value);
        }

        public JapaneseIme()
        {
            InitializeComponent();
            DataContext = this;

            ImeCheckTimer.Interval = TimeSpan.FromMilliseconds(200);
            ImeCheckTimer.Tick += ImeChk_Event;
            ImeCheckTimer.Start();
        }

        private void Initialize()
        {
            hWndCurFg = (IntPtr)0;
            NewImeEnabled = false;
            CurInputRoma = false;
            CurInputMode = 0;
            CurImmConversion = 0;

            JapaneseImeTip = (string)this.FindResource("ime_input_tip_disabled");
            JapaneseImeStatus = @"";

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            SizeUpdate();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Instance.ImeShow)
                || e.PropertyName == nameof(Settings.Instance.Edge)
                || e.PropertyName == nameof(Settings.Instance.RowCount)
                || e.PropertyName == nameof(Settings.Instance.TaskbarWidth))
            {
                SizeUpdate();
            }
        }

        private void SizeUpdate()
        {
            double size = System.Windows.Application.Current.FindResource("TaskbarHeight") as double? ?? 0;
            size *= Settings.Instance.TaskbarScale;

            if(Settings.Instance.ImeShow == ImeShowOption.ShowJapaneseIME)
            {
                this.Visibility = Visibility.Visible;

                if(Settings.Instance.Edge == AppBarEdge.Top || Settings.Instance.Edge == AppBarEdge.Bottom)
                {
                    this.Width = size;
                }
                else if(Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
                {
                    this.Height = size;
                }

                ImeCheckTimer.IsEnabled = true;
            }
            else
            {
                this.Visibility = Visibility.Hidden;

                if(Settings.Instance.Edge == AppBarEdge.Top || Settings.Instance.Edge == AppBarEdge.Bottom)
                {
                    this.Width = 0;
                }
                else if(Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
                {
                    this.Height = 0;
                }

                ImeCheckTimer.IsEnabled = false;
            }
        }

        private void JapaneseIme_full_shape_hiragana_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;
            uint ImmNewConversion;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            ImmNewConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
            ImmNewConversion |= IME_CMODE_FULLSHAPE;
            ImmNewConversion |= IME_CMODE_NATIVE;
            ImmNewConversion &= ~IME_CMODE_KATAKANA;
            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImmNewConversion);
        }
        
        private void JapaneseIme_full_shape_katakana_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;
            uint ImmNewConversion;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            ImmNewConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
            ImmNewConversion |= IME_CMODE_FULLSHAPE;
            ImmNewConversion |= IME_CMODE_NATIVE;
            ImmNewConversion |= IME_CMODE_KATAKANA;
            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImmNewConversion);
        }
 
        private void JapaneseIme_full_shape_alphanumeric_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;
            uint ImmNewConversion;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            ImmNewConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
            ImmNewConversion |= IME_CMODE_FULLSHAPE;
            ImmNewConversion &= ~IME_CMODE_NATIVE;
            ImmNewConversion &= ~IME_CMODE_KATAKANA;
            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImmNewConversion);
        }
 
        private void JapaneseIme_kana_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;
            uint ImmNewConversion;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            ImmNewConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
            ImmNewConversion &= ~IME_CMODE_FULLSHAPE;
            ImmNewConversion |= IME_CMODE_NATIVE;
            ImmNewConversion |= IME_CMODE_KATAKANA;
            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImmNewConversion);
        }

        private void JapaneseIme_alphanumeric_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;
            uint ImmNewConversion;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            ImmNewConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
            ImmNewConversion &= ~IME_CMODE_FULLSHAPE;
            ImmNewConversion &= ~IME_CMODE_NATIVE;
            ImmNewConversion &= ~IME_CMODE_KATAKANA;
            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImmNewConversion);
        }
 
        private void JapaneseIme_direct_OnClick(object sender, RoutedEventArgs e)
        {
            ImmClose();
        }

        private void JapaneseIme_open_imepad_OnClick(object sender, RoutedEventArgs e)
        {
			if(ImmOpenGetWindow() == (IntPtr)0)
                return;

            ExecImePad("");
        }
 
        private void JapaneseIme_open_add_word_dictionary_OnClick(object sender, RoutedEventArgs e)
        {
            ExecImeDictionaryTool("-w");
        }
 
        private void JapaneseIme_open_dictionary_tool_OnClick(object sender, RoutedEventArgs e)
        {
            ExecImeDictionaryTool("-t");
        }
 
        private void JapaneseIme_open_properties_OnClick(object sender, RoutedEventArgs e)
        {
            ExecImeProperties("");
        }
 
        private void JapaneseIme_input_key_roma_OnClick(object sender, RoutedEventArgs e)
        {
			if(ImmOpenGetWindow() == (IntPtr)0)
                return;

            if(CurInputRoma)
                return;     // no need to execute

            ToggleKanaMode();	// change "kana" to "roma"
        }

        private void JapaneseIme_input_key_kana_OnClick(object sender, RoutedEventArgs e)
        {
			if(ImmOpenGetWindow() == (IntPtr)0)
                return;

            if(!CurInputRoma)
                return;     // no need to execute

            ToggleKanaMode();	// change "roma" to "kana"
        }

        private void JapaneseIme_conversion_general_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETSENTENCEMODE, (IntPtr)0x08);
        }

        private void JapaneseIme_conversion_none_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;

			if((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
				return;

            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETSENTENCEMODE, (IntPtr)0x00);
        }

        private IntPtr ImmOpenGetWindow()
        {
            IntPtr hImeWnd;
            int OpenSts;

			SetForeOtherTopWindow();

			if((hImeWnd = ImmGetDefaultIMEWnd(hWndCurFg)) == (IntPtr)0)
				return (IntPtr)0;
        
            OpenSts = SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);

            if(OpenSts == 0)
            {
                SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETOPENSTATUS, (IntPtr)1);
                OpenSts = SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);

                if(OpenSts == 0)
                    return (IntPtr)0;
            }

            return hImeWnd;
        }

        private void ImmClose()
        {
            IntPtr hImeWnd;
            int OpenSts;

			SetForeOtherTopWindow();

			if((hImeWnd = ImmGetDefaultIMEWnd(hWndCurFg)) == (IntPtr)0)
				return;
        
            OpenSts = SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);

            if(OpenSts != 0)
            {
                SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETOPENSTATUS, (IntPtr)0);
            }

            return;
        }

        // When changing the IME state from the RetroBar,
        // focus should be returned to the window the user was previously using.
        private void SetForeOtherTopWindow()
        {
            UpdateOtherTopWindow();

            if(hWndCurFg == GetForegroundWindow())
                return;     // no need to execute

            if(!IsWindow(hWndCurFg))
                return;     // invalid Window

            if(!IsWindowVisible(hWndCurFg))
                return;     // hidden Window

            SetForegroundWindow(hWndCurFg);
            Thread.Sleep(200);  // 100ms = OK / 50ms = NG
            return;
        }

        // IME state is different for each window.
        // Clicking will give focus to RetroBar.
        // Need to get the previous window.
        private void UpdateOtherTopWindow()
        {
            IntPtr hWkTop;
            uint WkThId, WkProcId;
            uint WkLayout;

            if((hWkTop = GetForegroundWindow()) == (IntPtr)0)
                return; // error, not update.

            if((WkThId = GetWindowThreadProcessId(hWkTop, out WkProcId)) == 0)
                return; // error, not update.
                    
            if(WkProcId == GetCurrentProcessId())
                return;	// foreground is current process. not update.

            // Reference win32api.
            //     MAKELANGID(LANG_JAPANESE, SUBLANG_JAPANESE_JAPAN) == 0x411
            WkLayout = GetKeyboardLayout(WkThId);
            NewImeEnabled = (WkLayout & 0xFFFF) == 0x411;

            hWndCurFg = hWkTop; // update
            return;
        }

        private bool ExecImeDictionaryTool(string ExecOpt)
        {
            const string Dict32Name = @"C:\Windows\SysWOW64\IME\IMEJP\imjpdct.exe";
            const string Dict64Name = @"C:\Windows\System32\IME\IMEJP\imjpdct.exe";

            SetForeOtherTopWindow();

            return PrgExec(Dict32Name, Dict64Name, ExecOpt);
        }

        private bool ExecImeProperties(string ExecOpt)
        {
            const string Prop32Name = @"C:\Windows\SysWOW64\IME\IMEJP\imjpset.exe";
            const string Prop64Name = @"C:\Windows\System32\IME\IMEJP\imjpset.exe";

            SetForeOtherTopWindow();

            return PrgExec(Prop32Name, Prop64Name, ExecOpt);
        }

        // Ideally, call it via an API,
        // but this is a suboptimal solution.
        private bool ExecImePad(string ExecOpt)
        {
            bool RetSts = true;

            INPUT[] InBuf = new INPUT[6];
            uint RetCnt;

            try
            {
                // [Ctrl] + [F10]
                InBuf[0].type = INPUT_KEYBOARD;
                InBuf[0].u.ki.wVk = VK_CONTROL;
                InBuf[0].u.ki.dwFlags = KEYEVENTF_EXTENDEDKEY;

                    InBuf[1].type = INPUT_KEYBOARD;
                    InBuf[1].u.ki.wVk = VK_F10;
                    InBuf[1].u.ki.dwFlags = 0;

                    InBuf[2].type = INPUT_KEYBOARD;
                    InBuf[2].u.ki.wVk = VK_F10;
                    InBuf[2].u.ki.dwFlags = KEYEVENTF_KEYUP;

                InBuf[3].type = INPUT_KEYBOARD;
                InBuf[3].u.ki.wVk = VK_CONTROL;
                InBuf[3].u.ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

                // [P]
                InBuf[4].type = INPUT_KEYBOARD;
                InBuf[4].u.ki.wVk = 'P';
                InBuf[4].u.ki.dwFlags = 0;

                InBuf[5].type = INPUT_KEYBOARD;
                InBuf[5].u.ki.wVk = 'P';
                InBuf[5].u.ki.dwFlags = KEYEVENTF_KEYUP;

                RetCnt = SendInput(6, InBuf, Marshal.SizeOf(typeof(INPUT)));

                if(RetCnt != 6)
                    throw new System.Exception();
            }

            catch
            {
                RetSts = false;
            }

            return RetSts;
        }

        private bool PrgExec(string Exec32bit, string Exec64bit, string ExecOpt)
        {
            bool RetSts = true;
            bool Wow64Sts;
            ProcessStartInfo ProcStInf = new();
            Process Proc;

            if(!IsWow64Process(GetCurrentProcess(), out Wow64Sts))
                return false;

            if(Wow64Sts)
                ProcStInf.FileName = Exec32bit;     // 32bit
            else
                ProcStInf.FileName = Exec64bit;     // 64bit

            ProcStInf.Arguments = ExecOpt;

            try
            {
                Proc = Process.Start(ProcStInf);
            }

            catch
            {
                RetSts = false;
            }

            return RetSts;
        }

        // Determining whether input is kana or romaji
        // does not work via the API and requires reading the registry.
        private bool GetRegKanaMd()
        {
            bool RetMode;

            string RegKeyStrKanaMd = @"Software\AppDataLow\Software\Microsoft\IME\15.0\IMEJP\MSIME";
            int ChkWk;

            UIntPtr hKey;

            uint GetType, GetData;
            uint GetLen = REG_SIZE_DWORD;

            if((ChkWk = RegOpenKey((UIntPtr)RegistryHive.CurrentUser , RegKeyStrKanaMd, out hKey)) != ERROR_SUCCESS)
                return false;   // If unknown, treat as "roma"

            ChkWk = RegQueryValueEx(hKey, "kanaMd", (IntPtr)0, out GetType, out GetData, ref GetLen);
            
            RegCloseKey(hKey);

            if(ChkWk == ERROR_SUCCESS && GetType == REG_DWORD)
            {
                if(GetData == 1)
                    RetMode = true;		// 1: kana
                else
                    RetMode = false;	// 2: roma
            }
            else
            {
                RetMode = true;		// If unknown, treat as "roma"
            }

            return RetMode;
        }

        // Switching between Kana input and Romaji input
        // affects all windows simultaneously.
        private bool ToggleKanaMode()
        {
            bool RetSts = true;

            INPUT[] InBuf = new INPUT[6];
            uint RetCnt;

            try
            {
                // [Ctrl] + [Shift] + [kana]
                InBuf[0].type = INPUT_KEYBOARD;
                InBuf[0].u.ki.wVk = VK_CONTROL;
                InBuf[0].u.ki.dwFlags = KEYEVENTF_EXTENDEDKEY;

                    InBuf[1].type = INPUT_KEYBOARD;
                    InBuf[1].u.ki.wVk = VK_SHIFT;
                    InBuf[1].u.ki.dwFlags = 0;

                        // https://atmarkit.itmedia.co.jp/bbs/phpBB/viewtopic.php?topic=42587&forum=7
                        //	VK_OEM_COPY = 0xF2,		// kana key button.

                        InBuf[2].type = INPUT_KEYBOARD;
                        InBuf[2].u.ki.wVk = VK_OEM_COPY;		// kana key button.
                        InBuf[2].u.ki.dwFlags = KEYEVENTF_EXTENDEDKEY;

                        InBuf[3].type = INPUT_KEYBOARD;
                        InBuf[3].u.ki.wVk = VK_OEM_COPY;		// kana key button.
                        InBuf[3].u.ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

                    InBuf[4].type = INPUT_KEYBOARD;
                    InBuf[4].u.ki.wVk = VK_SHIFT;
                    InBuf[4].u.ki.dwFlags = KEYEVENTF_KEYUP;

                InBuf[5].type = INPUT_KEYBOARD;
                InBuf[5].u.ki.wVk = VK_CONTROL;
                InBuf[5].u.ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

                RetCnt = SendInput(6, InBuf, Marshal.SizeOf(typeof(INPUT)));

                if(RetCnt != 6)
                    throw new System.Exception();
            }

            catch
            {
                RetSts = false;
            }

            return RetSts;
        }

        private void ImeChk_Event(object sender, EventArgs args)
        {
            string NewImmTip;
            IntPtr hImeWnd;
            uint NewInputMode;
            int ImeOpenStatus;
            uint NewInputFlag;
            uint NewImmConversion;
            bool NewInputRoma;
            string NewImeStatus;
            bool NeedDspUpdate;

            const int MODE_FULL_SHAPE_HIRAGANA = 40000;
            const int MODE_FULL_SHAPE_KATAKANA = 40001;
            const int MODE_FULL_SHAPE_ALPHANUMERIC = 40002;
            const int MODE_KANA = 40003;
            const int MODE_ALPHANUMERIC = 40004;
            const int MODE_DIRECT = 40005;

            MenuItem Item_FullShapeHiragana = this.FindName("JapaneseIme_full_shape_hiragana") as MenuItem;
            MenuItem Item_FullShapeKatakana = this.FindName("JapaneseIme_full_shape_katakana") as MenuItem;
            MenuItem Item_FullShapeAlphanumeric = this.FindName("JapaneseIme_full_shape_alphanumeric") as MenuItem;
            MenuItem Item_Kana = this.FindName("JapaneseIme_kana") as MenuItem;
            MenuItem Item_Alphanumeric = this.FindName("JapaneseIme_alphanumeric") as MenuItem;
            MenuItem Item_Direct = this.FindName("JapaneseIme_direct") as MenuItem;

            MenuItem Item_InputRoma = this.FindName("JapaneseIme_input_key_roma") as MenuItem;
            MenuItem Item_InputKana = this.FindName("JapaneseIme_input_key_kana") as MenuItem;

            MenuItem Item_ConversionGeneral = this.FindName("JapaneseIme_conversion_general") as MenuItem;
            MenuItem Item_ConversionNone = this.FindName("JapaneseIme_conversion_none") as MenuItem;

            ((DispatcherTimer)sender).Stop();

            UpdateOtherTopWindow();

            NeedDspUpdate = false;
            NewImeStatus = @"✖";
            NewImmTip = (string)this.FindResource("ime_input_tip_disabled");

            if((hImeWnd = ImmGetDefaultIMEWnd(hWndCurFg)) == (IntPtr)0)
                NewImeEnabled = false;  // IME window not found

            if(NewImeEnabled)
            {
                NewImmTip = (string)this.FindResource("ime_input_tip_enabled");

                NewInputMode = 0;

                ImeOpenStatus = SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);
            
                if(ImeOpenStatus != 0)
                {
                    // conversion enabled
                    NewInputFlag = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
                    NewImmConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETSENTENCEMODE, (IntPtr)0);

                    if(GetRegKanaMd())
                    {
                        // Kana Input
                        NewInputRoma = false;
                    }
                    else
                    {
                        // Romanized Input
                        NewInputRoma = true;
                    }

                    if((NewInputFlag & IME_CMODE_FULLSHAPE) != 0)
                    {
                        // full shape
                        if((NewInputFlag & IME_CMODE_NATIVE) != 0)
                        {
                            // full shape / japanese
                            if((NewInputFlag & IME_CMODE_KATAKANA) != 0)
                            {
                                // full shape / katakana
                                NewInputMode = MODE_FULL_SHAPE_KATAKANA;
                                NewImeStatus = @"カ";
                            }
                            else
                            {
                                // full shape / hiragana
                                NewInputMode = MODE_FULL_SHAPE_HIRAGANA;
                                NewImeStatus = @"あ";
                            }
                        }
                        else
                        {
                            // full shape / alphanumeric
                            NewInputMode = MODE_FULL_SHAPE_ALPHANUMERIC;
                            NewImeStatus = @"Ａ";
                        }
                    }
                    else
                    {
                        // nornal shape
                        if((NewInputFlag & IME_CMODE_KATAKANA) != 0)
                        {
                            // nornal shape / katakana
                            NewInputMode = MODE_KANA;
                            // If type it directly, the editor will become confused because it contains special characters.
                            NewImeStatus = "\u033A\uFF76\u0000";
                        }
                        else
                        {
                            // nornal shape / alphanumeric
                            NewInputMode = MODE_ALPHANUMERIC;
                            // If type it directly, the editor will become confused because it contains special characters.
                            NewImeStatus = "\u033A\u0041\u0000";
                        }
                    }
                }
                else
                {
                    // conversion disabled
                    NewInputRoma = false;
                    NewImmConversion = 0;
                    NewInputMode = MODE_DIRECT;
                    NewImeStatus = "A";
                }
            
                if(CurInputRoma != NewInputRoma)
                {
                    CurInputRoma = NewInputRoma;
                    NeedDspUpdate = true;
                }

                if(CurInputMode != NewInputMode)
                {
                    CurInputMode = NewInputMode;
                    NeedDspUpdate = true;
                }

                if(CurImmConversion != NewImmConversion)
                {
                    CurImmConversion = NewImmConversion;
                    NeedDspUpdate = true;
                }
            }

            if(JapaneseImeEnabled != NewImeEnabled)
            {
                JapaneseImeEnabled = NewImeEnabled;
                NeedDspUpdate = true;
            }

            if(JapaneseImeTip != NewImmTip)
            {
                JapaneseImeTip = NewImmTip;
                NeedDspUpdate = true;
            }

            if(JapaneseImeStatus != NewImeStatus)
            {
                JapaneseImeStatus = NewImeStatus;
                NeedDspUpdate = true;
            }

            if(NeedDspUpdate)
            {
                Item_FullShapeHiragana.IsChecked = CurInputMode == MODE_FULL_SHAPE_HIRAGANA;
                Item_FullShapeKatakana.IsChecked = CurInputMode == MODE_FULL_SHAPE_KATAKANA;
                Item_FullShapeAlphanumeric.IsChecked = CurInputMode == MODE_FULL_SHAPE_ALPHANUMERIC;
                Item_Kana.IsChecked = CurInputMode == MODE_KANA;
                Item_Alphanumeric.IsChecked = CurInputMode == MODE_ALPHANUMERIC;
                Item_Direct.IsChecked = CurInputMode == MODE_DIRECT;

                if(CurInputMode != MODE_DIRECT)
                {
                    Item_InputRoma.IsChecked = CurInputRoma;
                    Item_InputKana.IsChecked = !CurInputRoma;
                    Item_ConversionGeneral.IsChecked = CurImmConversion != 0;
                    Item_ConversionNone.IsChecked = CurImmConversion == 0;
                }
                else
                {
                    Item_InputRoma.IsChecked = false;
                    Item_InputKana.IsChecked = false;
                    Item_ConversionGeneral.IsChecked = false;
                    Item_ConversionNone.IsChecked = false;
                }
            }

            ((DispatcherTimer)sender).Start();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                Initialize();

                _isLoaded = true;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            _isLoaded = false;
        }
  };
}