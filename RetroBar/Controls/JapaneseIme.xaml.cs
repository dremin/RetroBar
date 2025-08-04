using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ManagedShell.AppBar;
using static ManagedShell.Interop.NativeMethods;
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
        static extern IntPtr GetCurrentProcess();

        // IME
        [DllImport("imm32.dll")]
        static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        const int WM_IME_CONTROL = 0x0283;

        const uint IME_CMODE_NATIVE = 0x0001;
        const uint IME_CMODE_KATAKANA = 0x0002;  // only effect under IME_CMODE_NATIVE
        const uint IME_CMODE_FULLSHAPE = 0x0008;
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

        const uint REG_DWORD = 4; // 32-bit number
        const uint REG_SIZE_DWORD = 4; // 32-bit number

        // SendInput
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        const ushort VK_SHIFT = 0x10;
        const ushort VK_CONTROL = 0x11;
        const ushort VK_F10 = 0x79;
        const ushort VK_OEM_COPY = 0xF2;

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

            ImeChk();
            ImeCheckTimer.Start();
        }

        private void JapaneseIme_OnClick(object sender, RoutedEventArgs e)
        {
            ImmSetOpen(ImmOpenStatus.ImmToggle);
        }

        private void JapaneseIme_full_shape_hiragana_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;
            uint ImmNewConversion;

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
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

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
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

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
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

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
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

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
                return;

            ImmNewConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
            ImmNewConversion &= ~IME_CMODE_FULLSHAPE;
            ImmNewConversion &= ~IME_CMODE_NATIVE;
            ImmNewConversion &= ~IME_CMODE_KATAKANA;
            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImmNewConversion);
        }

        private void JapaneseIme_direct_OnClick(object sender, RoutedEventArgs e)
        {
            ImmSetOpen(ImmOpenStatus.ImmClose);
        }

        private void JapaneseIme_open_imepad_OnClick(object sender, RoutedEventArgs e)
        {
            if (ImmOpenGetWindow() == (IntPtr)0)
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
            if (ImmOpenGetWindow() == (IntPtr)0)
                return;

            if (CurInputRoma)
                return;     // no need to execute

            ToggleKanaMode();	// change "kana" to "roma"
        }

        private void JapaneseIme_input_key_kana_OnClick(object sender, RoutedEventArgs e)
        {
            if (ImmOpenGetWindow() == (IntPtr)0)
                return;

            if (!CurInputRoma)
                return;     // no need to execute

            ToggleKanaMode();	// change "roma" to "kana"
        }

        private void JapaneseIme_conversion_general_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
                return;

            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETSENTENCEMODE, (IntPtr)0x08);
        }

        private void JapaneseIme_conversion_none_OnClick(object sender, RoutedEventArgs e)
        {
            IntPtr hImeWnd;

            if ((hImeWnd = ImmOpenGetWindow()) == (IntPtr)0)
                return;

            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETSENTENCEMODE, (IntPtr)0x00);
        }

        private IntPtr ImmOpenGetWindow()
        {
            IntPtr hImeWnd;
            int OpenSts;

            hImeWnd = ImmSetOpen(ImmOpenStatus.ImmOpen);

            OpenSts = (int)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);

            if (OpenSts == 0)
                return (IntPtr)0;

            return hImeWnd;
        }

        private enum ImmOpenStatus
        {
            ImmClose,
            ImmOpen,
            ImmToggle
        }

        private IntPtr ImmSetOpen(ImmOpenStatus SetStatus)
        {
            IntPtr hImeWnd;
            int OpenCurSts;
            int OpenNewSts;

            SetForeOtherTopWindow();

            if ((hImeWnd = ImmGetDefaultIMEWnd(hWndCurFg)) == (IntPtr)0)
                return (IntPtr)0;

            OpenCurSts = (int)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);

            switch (SetStatus)
            {
                case ImmOpenStatus.ImmOpen:
                    OpenNewSts = 1;
                    break;

                case ImmOpenStatus.ImmClose:
                    OpenNewSts = 0;
                    break;

                case ImmOpenStatus.ImmToggle:
                    OpenNewSts = OpenCurSts == 0 ? 1 : 0;
                    break;

                default:
                    OpenNewSts = OpenCurSts;
                    break;
            }

            SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETOPENSTATUS, (IntPtr)OpenNewSts);

            return hImeWnd;
        }

        // When changing the IME state from the RetroBar,
        // focus should be returned to the window the user was previously using.
        private void SetForeOtherTopWindow()
        {
            UpdateOtherTopWindow();

            if (hWndCurFg == GetForegroundWindow())
                return;     // no need to execute

            if (!IsWindow(hWndCurFg))
                return;     // invalid Window

            if (!IsWindowVisible(hWndCurFg))
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
            int WkLayout;

            if ((hWkTop = GetForegroundWindow()) == (IntPtr)0)
                return; // error, not update.

            if ((WkThId = GetWindowThreadProcessId(hWkTop, out WkProcId)) == 0)
                return; // error, not update.

            if (WkProcId == GetCurrentProcessId())
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
            string WinDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string Dict32Name = WinDirPath + @"\SysWOW64\IME\IMEJP\imjpdct.exe";
            string Dict64Name = WinDirPath + @"\System32\IME\IMEJP\imjpdct.exe";

            SetForeOtherTopWindow();

            return PrgExec(Dict32Name, Dict64Name, ExecOpt);
        }

        private bool ExecImeProperties(string ExecOpt)
        {
            string WinDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string Prop32Name = WinDirPath + @"\SysWOW64\IME\IMEJP\imjpset.exe";
            string Prop64Name = WinDirPath + @"\System32\IME\IMEJP\imjpset.exe";

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
                InBuf[0].mkhi.ki.wVk = VK_CONTROL;
                InBuf[0].mkhi.ki.dwFlags = KEYEVENTF_EXTENDEDKEY;

                InBuf[1].type = INPUT_KEYBOARD;
                InBuf[1].mkhi.ki.wVk = VK_F10;
                InBuf[1].mkhi.ki.dwFlags = 0;

                InBuf[2].type = INPUT_KEYBOARD;
                InBuf[2].mkhi.ki.wVk = VK_F10;
                InBuf[2].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

                InBuf[3].type = INPUT_KEYBOARD;
                InBuf[3].mkhi.ki.wVk = VK_CONTROL;
                InBuf[3].mkhi.ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

                // [P]
                InBuf[4].type = INPUT_KEYBOARD;
                InBuf[4].mkhi.ki.wVk = 'P';
                InBuf[4].mkhi.ki.dwFlags = 0;

                InBuf[5].type = INPUT_KEYBOARD;
                InBuf[5].mkhi.ki.wVk = 'P';
                InBuf[5].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

                RetCnt = SendInput(6, InBuf, Marshal.SizeOf(typeof(INPUT)));

                if (RetCnt != 6)
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

            if (!IsWow64Process(GetCurrentProcess(), out Wow64Sts))
                return false;

            if (Wow64Sts)
                RetSts = ManagedShell.Common.Helpers.ShellHelper.StartProcess(Exec32bit);     // 32bit
            else
                RetSts = ManagedShell.Common.Helpers.ShellHelper.StartProcess(Exec64bit);     // 64bit

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

            if ((ChkWk = RegOpenKey((UIntPtr)RegistryHive.CurrentUser, RegKeyStrKanaMd, out hKey)) != ERROR_SUCCESS)
                return false;   // If unknown, treat as "roma"

            ChkWk = RegQueryValueEx(hKey, "kanaMd", (IntPtr)0, out GetType, out GetData, ref GetLen);

            RegCloseKey(hKey);

            if (ChkWk == ERROR_SUCCESS && GetType == REG_DWORD)
            {
                if (GetData == 1)
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
                InBuf[0].mkhi.ki.wVk = VK_CONTROL;
                InBuf[0].mkhi.ki.dwFlags = KEYEVENTF_EXTENDEDKEY;

                InBuf[1].type = INPUT_KEYBOARD;
                InBuf[1].mkhi.ki.wVk = VK_SHIFT;
                InBuf[1].mkhi.ki.dwFlags = 0;

                // https://atmarkit.itmedia.co.jp/bbs/phpBB/viewtopic.php?topic=42587&forum=7
                //	VK_OEM_COPY = 0xF2,		// kana key button.

                InBuf[2].type = INPUT_KEYBOARD;
                InBuf[2].mkhi.ki.wVk = VK_OEM_COPY;     // kana key button.
                InBuf[2].mkhi.ki.dwFlags = KEYEVENTF_EXTENDEDKEY;

                InBuf[3].type = INPUT_KEYBOARD;
                InBuf[3].mkhi.ki.wVk = VK_OEM_COPY;     // kana key button.
                InBuf[3].mkhi.ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

                InBuf[4].type = INPUT_KEYBOARD;
                InBuf[4].mkhi.ki.wVk = VK_SHIFT;
                InBuf[4].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

                InBuf[5].type = INPUT_KEYBOARD;
                InBuf[5].mkhi.ki.wVk = VK_CONTROL;
                InBuf[5].mkhi.ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

                RetCnt = SendInput(6, InBuf, Marshal.SizeOf(typeof(INPUT)));

                if (RetCnt != 6)
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
            ((DispatcherTimer)sender).Stop();
            ImeChk();
            ((DispatcherTimer)sender).Start();
        }

        private void ImeChk()
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

            UpdateOtherTopWindow();

            NeedDspUpdate = false;
            NewImeStatus = @"✖";
            NewImmTip = (string)this.FindResource("ime_input_tip_disabled");

            if ((hImeWnd = ImmGetDefaultIMEWnd(hWndCurFg)) == (IntPtr)0)
                NewImeEnabled = false;  // IME window not found

            if (NewImeEnabled)
            {
                NewImmTip = (string)this.FindResource("ime_input_tip_enabled");

                NewInputMode = 0;

                ImeOpenStatus = (int)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, (IntPtr)0);

                if (ImeOpenStatus != 0)
                {
                    // conversion enabled
                    NewInputFlag = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, (IntPtr)0);
                    NewImmConversion = (uint)SendMessage(hImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETSENTENCEMODE, (IntPtr)0);

                    if (GetRegKanaMd())
                    {
                        // Kana Input
                        NewInputRoma = false;
                    }
                    else
                    {
                        // Romanized Input
                        NewInputRoma = true;
                    }

                    if ((NewInputFlag & IME_CMODE_FULLSHAPE) != 0)
                    {
                        // full shape
                        if ((NewInputFlag & IME_CMODE_NATIVE) != 0)
                        {
                            // full shape / japanese
                            if ((NewInputFlag & IME_CMODE_KATAKANA) != 0)
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
                        if ((NewInputFlag & IME_CMODE_KATAKANA) != 0)
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

                if (CurInputRoma != NewInputRoma)
                {
                    CurInputRoma = NewInputRoma;
                    NeedDspUpdate = true;
                }

                if (CurInputMode != NewInputMode)
                {
                    CurInputMode = NewInputMode;
                    NeedDspUpdate = true;
                }

                if (CurImmConversion != NewImmConversion)
                {
                    CurImmConversion = NewImmConversion;
                    NeedDspUpdate = true;
                }
            }

            if (JapaneseImeEnabled != NewImeEnabled)
            {
                JapaneseImeEnabled = NewImeEnabled;
                NeedDspUpdate = true;
            }

            if (JapaneseImeTip != NewImmTip)
            {
                JapaneseImeTip = NewImmTip;
                NeedDspUpdate = true;
            }

            if (JapaneseImeStatus != NewImeStatus)
            {
                JapaneseImeStatus = NewImeStatus;
                NeedDspUpdate = true;
            }

            if (NeedDspUpdate)
            {
                JapaneseIme_full_shape_hiragana.IsChecked = CurInputMode == MODE_FULL_SHAPE_HIRAGANA;
                JapaneseIme_full_shape_katakana.IsChecked = CurInputMode == MODE_FULL_SHAPE_KATAKANA;
                JapaneseIme_full_shape_alphanumeric.IsChecked = CurInputMode == MODE_FULL_SHAPE_ALPHANUMERIC;
                JapaneseIme_kana.IsChecked = CurInputMode == MODE_KANA;
                JapaneseIme_alphanumeric.IsChecked = CurInputMode == MODE_ALPHANUMERIC;
                JapaneseIme_direct.IsChecked = CurInputMode == MODE_DIRECT;

                if (CurInputMode != MODE_DIRECT)
                {
                    JapaneseIme_input_key_roma.IsChecked = CurInputRoma;
                    JapaneseIme_input_key_kana.IsChecked = !CurInputRoma;
                    JapaneseIme_conversion_general.IsChecked = CurImmConversion != 0;
                    JapaneseIme_conversion_none.IsChecked = CurImmConversion == 0;
                }
                else
                {
                    JapaneseIme_input_key_roma.IsChecked = false;
                    JapaneseIme_input_key_kana.IsChecked = false;
                    JapaneseIme_conversion_general.IsChecked = false;
                    JapaneseIme_conversion_none.IsChecked = false;
                }
            }
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
            ImeCheckTimer.Stop();

            _isLoaded = false;
        }
  };
}