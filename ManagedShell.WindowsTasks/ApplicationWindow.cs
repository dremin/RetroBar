using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ManagedShell.WindowsTasks
{
    [DebuggerDisplay("Title: {Title}, Handle: {Handle}")]
    public class ApplicationWindow : IEquatable<ApplicationWindow>, INotifyPropertyChanged, IDisposable
    {
        const int TITLE_LENGTH = 1024;
        private readonly TasksService _tasksService;
        StringBuilder titleBuilder = new StringBuilder(TITLE_LENGTH);

        public ApplicationWindow(TasksService tasksService, IntPtr handle)
        {
            _tasksService = tasksService;
            Handle = handle;
            State = WindowState.Inactive;
        }

        public void Dispose()
        {
            // no longer required
        }

        public IntPtr Handle
        {
            get;
            set;
        }

        private string _appUserModelId = "";

        public string AppUserModelID
        {
            get
            {
                if (string.IsNullOrEmpty(_appUserModelId))
                {
                    NativeMethods.IPropertyStore propStore;
                    var g = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
                    NativeMethods.SHGetPropertyStoreForWindow(Handle, ref g, out propStore);

                    NativeMethods.PropVariant prop;

                    NativeMethods.PROPERTYKEY PKEY_AppUserModel_ID = new NativeMethods.PROPERTYKEY
                    {
                        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                        pid = 5
                    };

                    if (propStore != null)
                    {
                        propStore.GetValue(PKEY_AppUserModel_ID, out prop);

                        try
                        {
                            _appUserModelId = prop.Value.ToString();
                        }
                        catch
                        {
                            _appUserModelId = "";
                        }

                        prop.Clear();
                    }
                }

                return _appUserModelId;
            }
        }

        private bool? _isUWP = null;

        public bool IsUWP
        {
            get
            {
                if (_isUWP == null)
                {
                    _isUWP = WinFileName.ToLower().Contains("applicationframehost.exe");
                }

                return (bool)_isUWP;
            }
        }

        public void CloseByTaskService()
        {
            _tasksService.CloseWindow(this);
        }

        private string _winFileName = "";

        public string WinFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_winFileName))
                {
                    _winFileName = Shell.GetPathForHandle(Handle);
                }

                return _winFileName;
            }
        }

        private string _category;

        public string Category
        {
            get
            {
                return _category;
            }
            set
            {
                if (value != _category)
                {
                    _category = value;
                    OnPropertyChanged("Category");
                }
            }
        }

        private string _title;

        public string Title
        {
            get
            {
                if (_title == null)
                {
                    setTitle();
                }

                return _title;
            }
        }

        private void setTitle()
        {
            string title = "";
            try
            {
                titleBuilder.Clear();
                NativeMethods.GetWindowText(Handle, titleBuilder, TITLE_LENGTH + 1);

                title = titleBuilder.ToString();
            }
            catch { }

            if (_title != title)
            {
                _title = title;
                OnPropertyChanged("Title");
            }
        }

        public ImageSource Icon
        {
            get
            {
                if (_icon == null)
                    setIcon();

                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged("Icon");
            }
        }

        private bool _iconLoading = false;

        private ImageSource _icon = null;
        private IntPtr _hIcon = IntPtr.Zero;

        private NativeMethods.TBPFLAG _progressState;

        public NativeMethods.TBPFLAG ProgressState
        {
            get
            {
                return _progressState;
            }

            set
            {
                _progressState = value;

                if (value == NativeMethods.TBPFLAG.TBPF_NOPROGRESS)
                {
                    ProgressValue = 0;
                }

                OnPropertyChanged("ProgressState");
            }
        }

        private int _progressValue;

        public int ProgressValue
        {
            get
            {
                return _progressValue;
            }

            set
            {
                _progressValue = value;
                OnPropertyChanged("ProgressValue");
            }
        }

        private WindowState _state;

        public WindowState State
        {
            get
            {
                return _state;
            }

            set
            {
                _state = value;
                OnPropertyChanged("State");
            }
        }

        public bool IsMinimized
        {
            get { return NativeMethods.IsIconic(Handle); }
        }

        public NativeMethods.WindowShowStyle ShowStyle
        {
            get { return GetWindowShowStyle(Handle); }
        }

        public int WindowStyles
        {
            get
            {
                return NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_STYLE);
            }
        }

        public int ExtendedWindowStyles
        {
            get
            {
                return NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE);
            }
        }

        public bool CanAddToTaskbar
        {
            get
            {
                int extendedWindowStyles = ExtendedWindowStyles;
                bool isWindow = NativeMethods.IsWindow(Handle);
                bool isVisible = NativeMethods.IsWindowVisible(Handle);
                bool isToolWindow = (extendedWindowStyles & (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW) != 0;
                bool isAppWindow = (extendedWindowStyles & (int)NativeMethods.ExtendedWindowStyles.WS_EX_APPWINDOW) != 0;
                bool isNoActivate = (extendedWindowStyles & (int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE) != 0;
                IntPtr ownerWin = NativeMethods.GetWindow(Handle, NativeMethods.GetWindow_Cmd.GW_OWNER);

                return isWindow && isVisible && (ownerWin == IntPtr.Zero || isAppWindow) && (!isNoActivate || isAppWindow) && !isToolWindow;
            }
        }

        private bool? _showInTaskbar;

        // True if this window should be shown in the taskbar
        public bool ShowInTaskbar
        {
            get
            {
                if (_showInTaskbar == null)
                {
                    SetShowInTaskbar();
                }

                return (bool)_showInTaskbar;
            }
        }

        public void SetShowInTaskbar()
        {
            bool showInTaskbar = getShowInTaskbar();

            if (_showInTaskbar != showInTaskbar)
            {
                _showInTaskbar = showInTaskbar;
                OnPropertyChanged("ShowInTaskbar");
            }
        }

        private bool getShowInTaskbar()
        {
            // EnumWindows and ShellHook return UWP app windows that are 'cloaked', which should not be visible in the taskbar.
            if (Shell.IsWindows8OrBetter)
            {
                uint cloaked;
                int cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
                NativeMethods.DwmGetWindowAttribute(Handle, NativeMethods.DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out cloaked, cbSize);

                if (cloaked > 0)
                {
                    ShellLogger.Debug(string.Format("Cloaked ({0}) window ({1}) hidden from taskbar", cloaked, Title));
                    return false;
                }

                // UWP shell windows that are not cloaked should be hidden from the taskbar, too.
                StringBuilder cName = new StringBuilder(256);
                NativeMethods.GetClassName(Handle, cName, cName.Capacity);
                if (cName.ToString() == "ApplicationFrameWindow" || cName.ToString() == "Windows.UI.Core.CoreWindow" || cName.ToString() == "Shell_CharmWindow" || cName.ToString() == "ImmersiveLauncher")
                {
                    if ((ExtendedWindowStyles & (int)NativeMethods.ExtendedWindowStyles.WS_EX_WINDOWEDGE) == 0)
                    {
                        ShellLogger.Debug($"Hiding UWP non-window {Title}");
                        return false;
                    }
                }
            }

            return CanAddToTaskbar;
        }

        public void Uncloak()
        {
            ShellLogger.Debug($"Uncloak event received for {Title}");

            SetShowInTaskbar();
        }

        private void setIcon()
        {
            if (!_iconLoading && ShowInTaskbar)
            {
                _iconLoading = true;

                Task.Factory.StartNew(() =>
                {
                    if (IsUWP && !string.IsNullOrEmpty(AppUserModelID))
                    {
                        // UWP apps
                        try
                        {
                            BitmapImage img = new BitmapImage();
                            img.BeginInit();
                            img.UriSource = new Uri(UWPInterop.StoreAppHelper.GetAppIcon(AppUserModelID, (int)_tasksService.TaskIconSize)[0], UriKind.Absolute);
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.EndInit();
                            img.Freeze();
                            Icon = img;
                        }
                        catch
                        {
                            if (_icon == null) Icon = IconImageConverter.GetDefaultIcon();
                        }
                    }
                    else
                    {
                        // non-UWP apps
                        IntPtr hIco = default;
                        uint WM_GETICON = (uint)NativeMethods.WM.GETICON;
                        uint WM_QUERYDRAGICON = (uint)NativeMethods.WM.QUERYDRAGICON;
                        int GCL_HICON = -14;
                        int GCL_HICONSM = -34;
                        IconSize sizeSetting = _tasksService.TaskIconSize;

                        if (sizeSetting == IconSize.Small)
                        {
                            NativeMethods.SendMessageTimeout(Handle, WM_GETICON, 2, 0, 2, 1000, ref hIco);
                            if (hIco == IntPtr.Zero)
                                NativeMethods.SendMessageTimeout(Handle, WM_GETICON, 0, 0, 2, 1000, ref hIco);
                        }
                        else
                        {
                            NativeMethods.SendMessageTimeout(Handle, WM_GETICON, 1, 0, 2, 1000, ref hIco);
                        }

                        if (hIco == IntPtr.Zero && sizeSetting == IconSize.Small)
                        {
                            if (!Environment.Is64BitProcess)
                                hIco = NativeMethods.GetClassLong(Handle, GCL_HICONSM);
                            else
                                hIco = NativeMethods.GetClassLongPtr(Handle, GCL_HICONSM);
                        }

                        if (hIco == IntPtr.Zero)
                        {
                            if (!Environment.Is64BitProcess)
                                hIco = NativeMethods.GetClassLong(Handle, GCL_HICON);
                            else
                                hIco = NativeMethods.GetClassLongPtr(Handle, GCL_HICON);
                        }

                        if (hIco == IntPtr.Zero)
                        {
                            NativeMethods.SendMessageTimeout(Handle, WM_QUERYDRAGICON, 0, 0, 0, 1000, ref hIco);
                        }

                        if (hIco == IntPtr.Zero && _icon == null)
                        {
                            // last resort: find icon by executable. if we already have an icon from a previous fetch, then just skip this
                            if (Shell.Exists(WinFileName))
                            {
                                int size = 1;
                                if (sizeSetting != IconSize.Small)
                                    size = 0;

                                hIco = Shell.GetIconByFilename(WinFileName, size);
                            }
                        }

                        if (hIco != IntPtr.Zero)
                        {
                            if (_hIcon != hIco)
                            {
                                _hIcon = hIco;
                                bool returnDefault = (_icon == null); // only return a default icon if we don't already have one. otherwise let's use what we have.
                                ImageSource icon = IconImageConverter.GetImageFromHIcon(hIco, returnDefault);
                                if (icon != null)
                                {
                                    icon.Freeze();
                                    Icon = icon;
                                }
                            }
                            else
                            {
                                NativeMethods.DestroyIcon(hIco);
                            }
                        }
                    }

                    _iconLoading = false;
                }, CancellationToken.None, TaskCreationOptions.None, Shell.IconScheduler);
            }
        }

        public void UpdateProperties()
        {
            setTitle();
            SetShowInTaskbar();
            setIcon();
        }

        public void BringToFront()
        {
            // call restore if window is minimized
            if (IsMinimized)
            {
                Restore();
            }
            else
            {
                NativeMethods.ShowWindow(Handle, NativeMethods.WindowShowStyle.Show);
                NativeMethods.SetForegroundWindow(Handle);

                if (State == WindowState.Flashing) State = WindowState.Active; // some stubborn windows (Outlook) start flashing while already active, this lets us stop
            }
        }

        public void Minimize()
        {
            if ((WindowStyles & (int)NativeMethods.WindowStyles.WS_MINIMIZEBOX) != 0)
            {
                bool minimizeResult = NativeMethods.ShowWindow(Handle, NativeMethods.WindowShowStyle.Minimize);
                if (!minimizeResult)
                {
                    // elevated windows require WM_SYSCOMMAND messages
                    IntPtr retval = IntPtr.Zero;
                    NativeMethods.SendMessageTimeout(Handle, (int)NativeMethods.WM.SYSCOMMAND, NativeMethods.SC_MINIMIZE, 0, 2, 200, ref retval);
                }
            }
        }

        public void Restore()
        {
            bool restoreResult = NativeMethods.ShowWindow(Handle, NativeMethods.WindowShowStyle.Restore);
            if (!restoreResult)
            {
                // elevated windows require WM_SYSCOMMAND messages
                IntPtr retval = IntPtr.Zero;
                NativeMethods.SendMessageTimeout(Handle, (int)NativeMethods.WM.SYSCOMMAND, NativeMethods.SC_RESTORE, 0, 2, 200, ref retval);
            }
            NativeMethods.SetForegroundWindow(Handle);
        }

        public void Maximize()
        {
            bool maximizeResult = NativeMethods.ShowWindow(Handle, NativeMethods.WindowShowStyle.Maximize);
            if (!maximizeResult)
            {
                // we don't have a fallback for elevated windows here since our only hope, SC_MAXIMIZE, doesn't seem to work for them. fall back to restore.
                IntPtr retval = IntPtr.Zero;
                NativeMethods.SendMessageTimeout(Handle, (int)NativeMethods.WM.SYSCOMMAND, NativeMethods.SC_RESTORE, 0, 2, 200, ref retval);
            }
            NativeMethods.SetForegroundWindow(Handle);
        }

        internal IntPtr Close()
        {
            IntPtr retval = IntPtr.Zero;
            NativeMethods.SendMessageTimeout(Handle, (int)NativeMethods.WM.SYSCOMMAND, NativeMethods.SC_CLOSE, 0, 2, 200, ref retval);

            return retval;
        }

        public void Move()
        {
            // move window via arrow keys; must be active window to control
            BringToFront();
            IntPtr retval = IntPtr.Zero;
            NativeMethods.SendMessageTimeout(Handle, (int)NativeMethods.WM.SYSCOMMAND, NativeMethods.SC_MOVE, 0, 2, 200, ref retval);
        }

        public void Size()
        {
            // size window via arrow keys; must be active window to control
            BringToFront();
            IntPtr retval = IntPtr.Zero;
            NativeMethods.SendMessageTimeout(Handle, (int)NativeMethods.WM.SYSCOMMAND, NativeMethods.SC_SIZE, 0, 2, 200, ref retval);
        }

        /// <summary>
        /// Returns whether a window is normal (1), minimized (2), or maximized (3).
        /// </summary>
        /// <param name="hWnd">The handle of the window.</param>
        public NativeMethods.WindowShowStyle GetWindowShowStyle(IntPtr hWnd)
        {
            NativeMethods.WINDOWPLACEMENT placement = new NativeMethods.WINDOWPLACEMENT();
            NativeMethods.GetWindowPlacement(hWnd, ref placement);
            return placement.showCmd;
        }

        #region IEquatable<Window> Members

        public bool Equals(ApplicationWindow other)
        {
            return Handle.Equals(other.Handle);
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string PropertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
            }
        }

        #endregion

        public enum WindowState
        {
            Active,
            Inactive,
            Hidden,
            Flashing,
            Unknown = 999
        }
    }

}
