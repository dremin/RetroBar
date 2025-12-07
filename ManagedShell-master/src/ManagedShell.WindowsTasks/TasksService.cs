using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ManagedShell.Common.Enums;
using ManagedShell.Common.SupportingClasses;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTasks
{
    public class TasksService : DependencyObject, IDisposable
    {
        public static readonly IconSize DEFAULT_ICON_SIZE = IconSize.Small;

        public event EventHandler<WindowEventArgs> WindowActivated;
        public event EventHandler<EventArgs> DesktopActivated;
        public event EventHandler<FullScreenEventArgs> FullScreenChanged;
        public event EventHandler<WindowEventArgs> MonitorChanged;

        private NativeWindowEx _HookWin;
        private object _windowsLock = new object();
        internal bool IsInitialized;
        private IconSize _taskIconSize;

        private static int WM_SHELLHOOKMESSAGE = -1;
        private static int WM_TASKBARCREATEDMESSAGE = -1;
        private static int TASKBARBUTTONCREATEDMESSAGE = -1;
        private static IntPtr cloakEventHook = IntPtr.Zero;
        private WinEventProc cloakEventProc;
        private static IntPtr moveEventHook = IntPtr.Zero;
        private WinEventProc moveEventProc;

        internal ITaskCategoryProvider TaskCategoryProvider;
        private TaskCategoryChangeDelegate CategoryChangeDelegate;

        // P/Invoke for ImageList icon extraction
        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // ImageList_GetIcon flags
        private const int ILD_NORMAL = 0x00000000;
        private const int ILD_TRANSPARENT = 0x00000001;

        public IconSize TaskIconSize
        {
            get { return _taskIconSize; }
            set
            {
                if (value == _taskIconSize)
                {
                    return;
                }

                _taskIconSize = value;

                if (!IsInitialized)
                {
                    return;
                }

                foreach (var window in Windows)
                {
                    if (!window.ShowInTaskbar)
                    {
                        return;
                    }

                    window.UpdateProperties();
                }
            }
        }

        public TasksService() : this(DEFAULT_ICON_SIZE)
        {
        }
        
        public TasksService(IconSize iconSize)
        {
            TaskIconSize = iconSize;
        }

        internal void Initialize(bool withMultiMonTracking)
        {
            if (IsInitialized)
            {
                return;
            }

            try
            {
                ShellLogger.Debug("TasksService: Starting");

                // Test file writing
                string debugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");
                try
                {
                    System.IO.File.WriteAllText(debugPath, $"{DateTime.Now}: TasksService initialized - file writing works!\n");
                }
                catch (Exception ex)
                {
                    ShellLogger.Error($"Failed to write debug file to {debugPath}: {ex.Message}");
                }

                // create window to receive task events
                _HookWin = new NativeWindowEx();
                _HookWin.CreateHandle(new CreateParams());

                // prevent other shells from working properly
                SetTaskmanWindow(_HookWin.Handle);

                // register to receive task events
                RegisterShellHookWindow(_HookWin.Handle);
                WM_SHELLHOOKMESSAGE = RegisterWindowMessage("SHELLHOOK");
                WM_TASKBARCREATEDMESSAGE = RegisterWindowMessage("TaskbarCreated");
                TASKBARBUTTONCREATEDMESSAGE = RegisterWindowMessage("TaskbarButtonCreated");
                _HookWin.MessageReceived += ShellWinProc;

                if (EnvironmentHelper.IsWindows8OrBetter)
                {
                    // set event hook for cloak/uncloak events
                    cloakEventProc = CloakEventCallback;

                    if (cloakEventHook == IntPtr.Zero)
                    {
                        cloakEventHook = SetWinEventHook(
                            EVENT_OBJECT_CLOAKED,
                            EVENT_OBJECT_UNCLOAKED,
                            IntPtr.Zero,
                            cloakEventProc,
                            0,
                            0,
                            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
                    }
                }

                if (withMultiMonTracking && !EnvironmentHelper.IsWindows8OrBetter)
                {
                    // set event hook for move events
                    // In Windows 8 and newer, use HSHELL_MONITORCHANGED instead
                    moveEventProc = MoveEventCallback;

                    if (moveEventHook == IntPtr.Zero)
                    {
                        moveEventHook = SetWinEventHook(
                            EVENT_OBJECT_LOCATIONCHANGE,
                            EVENT_OBJECT_LOCATIONCHANGE,
                            IntPtr.Zero,
                            moveEventProc,
                            0,
                            0,
                            WINEVENT_OUTOFCONTEXT);
                    }
                }

                // set window for ITaskbarList
                setTaskbarListHwnd(_HookWin.Handle);

                // adjust minimize animation
                SetMinimizedMetrics();

                // enumerate windows already opened and set active window
                getInitialWindows();

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                ShellLogger.Info("TasksService: Unable to start: " + ex.Message);
            }
        }

        internal void SetTaskCategoryProvider(ITaskCategoryProvider provider)
        {
            TaskCategoryProvider = provider;

            if (CategoryChangeDelegate == null)
            {
                CategoryChangeDelegate = CategoriesChanged;
            }

            TaskCategoryProvider.SetCategoryChangeDelegate(CategoryChangeDelegate);
        }

        private void getInitialWindows()
        {
            EnumWindows((hwnd, lParam) =>
            {
                ApplicationWindow win = new ApplicationWindow(this, hwnd);

                if (win.CanAddToTaskbar && win.ShowInTaskbar && !Windows.Contains(win))
                {
                    Windows.Add(win);

                    sendTaskbarButtonCreatedMessage(win.Handle);
                }

                return true;
            }, 0);

            IntPtr hWndForeground = GetForegroundWindow();
            if (Windows.Any(i => i.Handle == hWndForeground && i.ShowInTaskbar))
            {
                ApplicationWindow win = Windows.First(wnd => wnd.Handle == hWndForeground);
                win.State = ApplicationWindow.WindowState.Active;
                win.SetShowInTaskbar();
            }
        }

        public void Dispose()
        {
            if (IsInitialized)
            {
                ShellLogger.Debug("TasksService: Deregistering hooks");
                DeregisterShellHookWindow(_HookWin.Handle);
                if (cloakEventHook != IntPtr.Zero) UnhookWinEvent(cloakEventHook);
                if (moveEventHook != IntPtr.Zero) UnhookWinEvent(moveEventHook);
                _HookWin.DestroyHandle();
                setTaskbarListHwnd(IntPtr.Zero);
                IsInitialized = false;
                Windows.Clear();
            }

            TaskCategoryProvider?.Dispose();
        }

        private void CategoriesChanged()
        {
            foreach (ApplicationWindow window in Windows)
            {
                if (window.ShowInTaskbar)
                {
                    window.Category = TaskCategoryProvider?.GetCategory(window);
                }
            }
        }

        private void SetMinimizedMetrics()
        {
            MinimizedMetrics mm = new MinimizedMetrics
            {
                cbSize = (uint)Marshal.SizeOf(typeof(MinimizedMetrics))
            };

            IntPtr mmPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MinimizedMetrics)));

            try
            {
                Marshal.StructureToPtr(mm, mmPtr, true);
                SystemParametersInfo(SPI.GETMINIMIZEDMETRICS, mm.cbSize, mmPtr, SPIF.None);
                mm.iWidth = 140;
                mm.iArrange |= MinimizedMetricsArrangement.Hide;
                Marshal.StructureToPtr(mm, mmPtr, true);
                SystemParametersInfo(SPI.SETMINIMIZEDMETRICS, mm.cbSize, mmPtr, SPIF.None);
            }
            finally
            {
                Marshal.DestroyStructure(mmPtr, typeof(MinimizedMetrics));
                Marshal.FreeHGlobal(mmPtr);
            }
        }

        public void CloseWindow(ApplicationWindow window)
        {
            if (window.DoClose() != IntPtr.Zero)
            {
                ShellLogger.Debug($"TasksService: Removing window {window.Title} from collection due to no response");
                window.Dispose();
                Windows.Remove(window);
            }
        }

        private void sendTaskbarButtonCreatedMessage(IntPtr hWnd)
        {
            // Server Core doesn't support ITaskbarList, so sending this message on that OS could cause some assuming apps to crash
            if (!EnvironmentHelper.IsServerCore) SendNotifyMessage(hWnd, (uint)TASKBARBUTTONCREATEDMESSAGE, UIntPtr.Zero, IntPtr.Zero);
        }

        private ApplicationWindow addWindow(IntPtr hWnd, ApplicationWindow.WindowState initialState = ApplicationWindow.WindowState.Inactive, bool sanityCheck = false)
        {
            ApplicationWindow win = new ApplicationWindow(this, hWnd);

            // set window state if a non-default value is provided
            if (initialState != ApplicationWindow.WindowState.Inactive) win.State = initialState;

            // add window unless we need to validate it is eligible to show in taskbar
            if (!sanityCheck || win.CanAddToTaskbar)
            {
                Windows.Add(win);
                ShellLogger.Debug($"TasksService: Added window {hWnd} ({win.Title})");
            }

            // Only send TaskbarButtonCreated if we are shell, and if OS is not Server Core
            // This is because if Explorer is running, it will send the message, so we don't need to
            if (EnvironmentHelper.IsAppRunningAsShell) sendTaskbarButtonCreatedMessage(win.Handle);

            return win;
        }

        private void removeWindow(IntPtr hWnd)
        {
            if (Windows.Any(i => i.Handle == hWnd))
            {
                do
                {
                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == hWnd);
                    win.Dispose();
                    Windows.Remove(win);

                    ShellLogger.Debug($"TasksService: Removed window {hWnd} ({win.Title})");
                }
                while (Windows.Any(i => i.Handle == hWnd));
            }
        }

        private void redrawWindow(ApplicationWindow win)
        {
            win.UpdateProperties();
            ShellLogger.Debug($"TasksService: Updated window {win.Handle} ({win.Title})");

            foreach (ApplicationWindow wind in Windows)
            {
                if (wind.WinFileName == win.WinFileName && wind.Handle != win.Handle)
                {
                    wind.UpdateProperties();
                }
            }
        }

        private void ShellWinProc(ref Message msg, ref bool handled)
        {
            Message msgCopy = msg;
            handled = true;
            if (msg.Msg == WM_SHELLHOOKMESSAGE)
            {
                try
                {
                    lock (_windowsLock)
                    {
                        switch ((HSHELL)msg.WParam.ToInt32())
                        {
                            case HSHELL.WINDOWCREATED:
                                if (!Windows.Any(i => i.Handle == msgCopy.LParam))
                                {
                                    addWindow(msg.LParam);
                                }
                                else
                                {
                                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == msgCopy.LParam);
                                    win.UpdateProperties();
                                }
                                break;

                            case HSHELL.WINDOWDESTROYED:
                                removeWindow(msg.LParam);
                                break;

                            case HSHELL.WINDOWREPLACING:
                                if (Windows.Any(i => i.Handle == msgCopy.LParam))
                                {
                                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == msgCopy.LParam);
                                    win.State = ApplicationWindow.WindowState.Inactive;
                                    win.SetShowInTaskbar();
                                }
                                else
                                {
                                    addWindow(msg.LParam);
                                }
                                break;
                            case HSHELL.WINDOWREPLACED:
                                // TODO: If a window gets replaced, we lose app-level state such as overlay icons.
                                removeWindow(msg.LParam);
                                break;

                            case HSHELL.WINDOWACTIVATED:
                            case HSHELL.RUDEAPPACTIVATED:
                                foreach (var aWin in Windows.Where(w => w.State == ApplicationWindow.WindowState.Active))
                                {
                                    aWin.State = ApplicationWindow.WindowState.Inactive;
                                }

                                if (msg.LParam != IntPtr.Zero)
                                {
                                    ApplicationWindow win = null;

                                    if (Windows.Any(i => i.Handle == msgCopy.LParam))
                                    {
                                        win = Windows.First(wnd => wnd.Handle == msgCopy.LParam);
                                        win.State = ApplicationWindow.WindowState.Active;
                                        win.SetShowInTaskbar();
                                        ShellLogger.Debug($"TasksService: Activated window {win.Handle} ({win.Title})");
                                    }
                                    else
                                    {
                                        win = addWindow(msg.LParam, ApplicationWindow.WindowState.Active);
                                    }

                                    if (win != null)
                                    {
                                        foreach (ApplicationWindow wind in Windows)
                                        {
                                            if (wind.WinFileName == win.WinFileName && wind.Handle != win.Handle)
                                                wind.SetShowInTaskbar();
                                        }

                                        WindowEventArgs args = new WindowEventArgs
                                        {
                                            Window = win
                                        };

                                        WindowActivated?.Invoke(this, args);
                                    }
                                }
                                else
                                {
                                    DesktopActivated?.Invoke(this, new EventArgs());
                                }
                                break;

                            case HSHELL.FLASH:
                                if (Windows.Any(i => i.Handle == msgCopy.LParam))
                                {
                                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == msgCopy.LParam);
                                    
                                    if (win.State != ApplicationWindow.WindowState.Active)
                                    {
                                        win.State = ApplicationWindow.WindowState.Flashing;
                                    }

                                    redrawWindow(win);
                                }
                                else
                                {
                                    addWindow(msg.LParam, ApplicationWindow.WindowState.Flashing, true);
                                }
                                break;

                            case HSHELL.ACTIVATESHELLWINDOW:
                                ShellLogger.Debug("TasksService: Activate shell window called.");
                                break;

                            case HSHELL.ENDTASK:
                                removeWindow(msg.LParam);
                                break;

                            case HSHELL.REDRAW:
                                if (Windows.Any(i => i.Handle == msgCopy.LParam))
                                {
                                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == msgCopy.LParam);

                                    if (win.State == ApplicationWindow.WindowState.Flashing)
                                    {
                                        win.State = ApplicationWindow.WindowState.Inactive;
                                    }

                                    redrawWindow(win);
                                }
                                else
                                {
                                    addWindow(msg.LParam, ApplicationWindow.WindowState.Inactive, true);
                                }
                                break;

                            case HSHELL.MONITORCHANGED:
                                if (Windows.Any(i => i.Handle == msgCopy.LParam))
                                {
                                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == msgCopy.LParam);
                                    win.SetMonitor();

                                    WindowEventArgs args = new WindowEventArgs
                                    {
                                        Window = win
                                    };

                                    MonitorChanged?.Invoke(this, args);
                                }
                                break;

                            case HSHELL.FULLSCREENENTER:
                                {
                                    FullScreenEventArgs args = new FullScreenEventArgs
                                    {
                                        Handle = msgCopy.LParam,
                                        IsEntering = true
                                    };

                                    FullScreenChanged?.Invoke(this, args);
                                    ShellLogger.Debug($"TasksService: Full screen entered by window {msgCopy.LParam}");
                                    break;
                                }

                            case HSHELL.FULLSCREENEXIT:
                                {
                                    FullScreenEventArgs args = new FullScreenEventArgs
                                    {
                                        Handle = msgCopy.LParam,
                                        IsEntering = false
                                    };

                                    FullScreenChanged?.Invoke(this, args);
                                    ShellLogger.Debug($"TasksService: Full screen exited by window {msgCopy.LParam}");
                                    break;
                                }

                            case HSHELL.GETMINRECT:
                                SHELLHOOKINFO minRectInfo = Marshal.PtrToStructure<SHELLHOOKINFO>(msg.LParam);
                                if (Windows.Any(i => i.Handle == minRectInfo.hwnd))
                                {
                                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == minRectInfo.hwnd);
                                    minRectInfo.rc = win.GetButtonRectFromShell();

                                    if (minRectInfo.rc.Width <= 0 && minRectInfo.rc.Height <= 0)
                                    {
                                        break;
                                    }
                                    Marshal.StructureToPtr(minRectInfo, msg.LParam, false);
                                    msg.Result = (IntPtr)1;
                                    ShellLogger.Debug($"TasksService: MinRect {minRectInfo.rc.Width}x{minRectInfo.rc.Height} provided for {win.Handle} ({win.Title})");
                                    return; // return here so the result isnt reset to DefWindowProc
                                }
                                break;

                            // TaskMan needs to return true if we provide our own task manager to prevent explorers.
                            // case HSHELL.TASKMAN:
                            //     SingletonLogger.Instance.Info("TaskMan Message received.");
                            //     break;

                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShellLogger.Error("TasksService: Error in ShellWinProc. ", ex);
                    Debugger.Break();
                }
            }
            else if (msg.Msg == WM_TASKBARCREATEDMESSAGE)
            {
                ShellLogger.Debug("TasksService: TaskbarCreated received, setting ITaskbarList window");
                setTaskbarListHwnd(_HookWin.Handle);
            }
            else if (msg.Msg >= (int)WM.USER)
            {
                // Handle ITaskbarList functions, most not implemented yet

                ApplicationWindow win = null;

                switch (msg.Msg)
                {
                    case (int)WM.USER + 50:
                        // ActivateTab
                        // Also sends WM_SHELLHOOK message
                        ShellLogger.Debug("TasksService: ITaskbarList: ActivateTab HWND:" + msg.LParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 60:
                        // MarkFullscreenWindow
                        // Also sends WM_SHELLHOOK message
                        ShellLogger.Debug("TasksService: ITaskbarList: MarkFullscreenWindow HWND:" + msg.LParam + " Entering? " + msg.WParam);
                        FullScreenEventArgs args = new FullScreenEventArgs
                        {
                            Handle = msgCopy.LParam,
                            IsEntering = msg.WParam != IntPtr.Zero
                        };

                        FullScreenChanged?.Invoke(this, args);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 64:
                        // SetProgressValue
                        ShellLogger.Debug("TasksService: ITaskbarList: SetProgressValue HWND:" + msg.WParam + " Progress: " + msg.LParam);

                        win = new ApplicationWindow(this, msg.WParam);
                        if (Windows.Contains(win))
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            win.ProgressValue = (int)msg.LParam;
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 65:
                        // SetProgressState
                        ShellLogger.Debug("TasksService: ITaskbarList: SetProgressState HWND:" + msg.WParam + " Flags: " + msg.LParam);

                        win = new ApplicationWindow(this, msg.WParam);
                        if (Windows.Contains(win))
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            win.ProgressState = (TBPFLAG)msg.LParam;
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 67:
                        // RegisterTab
                        ShellLogger.Debug("TasksService: ITaskbarList: RegisterTab MDI HWND:" + msg.LParam + " Tab HWND: " + msg.WParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 68:
                        // UnregisterTab
                        ShellLogger.Debug("TasksService: ITaskbarList: UnregisterTab Tab HWND: " + msg.WParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 71:
                        // SetTabOrder
                        ShellLogger.Debug("TasksService: ITaskbarList: SetTabOrder HWND:" + msg.WParam + " Before HWND: " + msg.LParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 72:
                        // SetTabActive
                        ShellLogger.Debug("TasksService: ITaskbarList: SetTabActive HWND:" + msg.WParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 75:
                        // Unknown
                        ShellLogger.Debug("TasksService: ITaskbarList: Unknown HWND:" + msg.WParam + " LParam: " + msg.LParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 76:
                        // ThumbBarAddButtons
                        ShellLogger.Debug($"TasksService: ITaskbarList: ThumbBarAddButtons HWND:{msg.WParam} LParam:{msg.LParam}");
                        string debugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: MESSAGE RECEIVED - ThumbBarAddButtons HWND:{msg.WParam}\n");

                        win = new ApplicationWindow(this, msg.WParam);
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Created temp window, checking if exists in collection. Window count: {Windows.Count}\n");
                        if (Windows.Contains(win))
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            ShellLogger.Debug($"TasksService: Found window for HWND {msg.WParam}: {win.Title}");

                            try
                            {
                                // LParam is a pointer to shared memory containing button count and button array pointer
                                IntPtr pData = msg.LParam;

                                ShellLogger.Debug($"TasksService: Data pointer: {pData}");
                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: ThumbBarAddButtons - HWND:{msg.WParam}, DataPointer:{pData}, Title:{win.Title}\n");

                                if (pData != IntPtr.Zero && win.ProcId is uint procId)
                                {
                                    IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryOperation, false, (int)procId);
                                    if (hProcess != IntPtr.Zero)
                                    {
                                        IntPtr hShared = SHLockShared(pData, procId);
                                        if (hShared != IntPtr.Zero)
                                        {
                                            // Read the count from the first DWORD
                                            int buttonCount = Marshal.ReadInt32(hShared, 0);

                                            System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Read from shared memory - ButtonCount:{buttonCount}\n");

                                            if (buttonCount > 0 && buttonCount <= 7)
                                            {
                                                THUMBBUTTON[] buttons = new THUMBBUTTON[buttonCount];
                                                int buttonSize = Marshal.SizeOf(typeof(THUMBBUTTON));
                                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: THUMBBUTTON C# struct size: {buttonSize} bytes\n");

                                                // Buttons stored inline after count
                                                int offset = sizeof(int);
                                                for (int i = 0; i < buttonCount; i++)
                                                {
                                                    try
                                                    {
                                                        IntPtr buttonPtr = new IntPtr(hShared.ToInt64() + offset + (i * buttonSize));
                                                        buttons[i] = Marshal.PtrToStructure<THUMBBUTTON>(buttonPtr);
                                                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Button {i} at offset {offset + (i * buttonSize)}: Mask={buttons[i].dwMask}, ID={buttons[i].iId}, Bitmap={buttons[i].iBitmap}, Flags={buttons[i].dwFlags}, Tooltip=\"{buttons[i].szTip}\", Icon=0x{buttons[i].hIcon:X}\n");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Error reading button {i}: {ex.Message}\n");
                                                    }
                                                }

                                                // Convert to wrapper class for WPF binding (WPF cannot bind to struct fields)
                                                // Also extract icons from ImageList if available
                                                var thumbnailButtons = new ThumbnailButton[buttonCount];
                                                for (int i = 0; i < buttonCount; i++)
                                                {
                                                    thumbnailButtons[i] = new ThumbnailButton(buttons[i]);

                                                    // Try to extract icon from ImageList
                                                    if (win.ThumbnailButtonImageList != IntPtr.Zero && buttons[i].iBitmap > 0)
                                                    {
                                                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Trying to extract icon for button {i}, bitmap index {buttons[i].iBitmap} from ImageList {win.ThumbnailButtonImageList}\n");

                                                        IntPtr hIcon = ImageList_GetIcon(win.ThumbnailButtonImageList, (int)buttons[i].iBitmap, ILD_NORMAL);
                                                        if (hIcon != IntPtr.Zero)
                                                        {
                                                            try
                                                            {
                                                                var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                                                                    hIcon,
                                                                    Int32Rect.Empty,
                                                                    BitmapSizeOptions.FromEmptyOptions());
                                                                bitmap.Freeze();
                                                                thumbnailButtons[i].IconSource = bitmap;
                                                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: SUCCESS - Extracted icon for button {i}\n");
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Error creating bitmap from icon: {ex.Message}\n");
                                                            }
                                                            finally
                                                            {
                                                                DestroyIcon(hIcon);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: ImageList_GetIcon returned NULL\n");
                                                        }
                                                    }
                                                }

                                                win.ThumbnailButtons = thumbnailButtons;
                                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: SUCCESS - Set {buttonCount} buttons\n");
                                            }
                                            else
                                            {
                                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Invalid buttonCount:{buttonCount}\n");
                                            }

                                            SHUnlockShared(hShared);
                                        }
                                        else
                                        {
                                            System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: SHLockShared on data pointer FAILED\n");
                                        }

                                        CloseHandle(hProcess);
                                    }
                                    else
                                    {
                                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: OpenProcess FAILED for ProcId:{procId}\n");
                                    }
                                }
                                else
                                {
                                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Invalid pData or no ProcId\n");
                                }
                            }
                            catch (Exception ex)
                            {
                                ShellLogger.Error($"TasksService: Error parsing ThumbBarAddButtons: {ex.Message}");
                                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: EXCEPTION: {ex.Message}\n{ex.StackTrace}\n");
                            }
                        }
                        else
                        {
                            System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Window not found for HWND {msg.WParam}\n");
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 77:
                        // ThumbBarUpdateButtons
                        ShellLogger.Debug("TasksService: ITaskbarList: ThumbBarUpdateButtons HWND:" + msg.WParam);
                        string updateDebugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");
                        System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: MESSAGE RECEIVED - ThumbBarUpdateButtons HWND:{msg.WParam}\n");

                        win = new ApplicationWindow(this, msg.WParam);
                        bool windowFound = Windows.Contains(win);
                        System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: Window found: {windowFound}\n");

                        if (windowFound)
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: Processing update for window: {win.Title}\n");

                            try
                            {
                                // LParam contains: low word = button count, high word = pointer to THUMBBUTTON array
                                int buttonCount = msg.LParam.ToInt32() & 0xFFFF;
                                IntPtr pButtons = new IntPtr(msg.LParam.ToInt64() >> 16);

                                System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: ThumbBarUpdateButtons parse - buttonCount={buttonCount}, pButtons=0x{pButtons.ToInt64():X}\n");

                                if (buttonCount > 0 && buttonCount <= 7 && pButtons != IntPtr.Zero)
                                {
                                    System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: ThumbBarUpdateButtons - Entering update block\n");
                                    THUMBBUTTON[] buttons = new THUMBBUTTON[buttonCount];
                                    int buttonSize = Marshal.SizeOf(typeof(THUMBBUTTON));

                                    if (win.ProcId is uint procId)
                                    {
                                        IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryOperation, false, (int)procId);
                                        if (hProcess != IntPtr.Zero)
                                        {
                                            IntPtr hShared = SHLockShared(pButtons, procId);
                                            if (hShared != IntPtr.Zero)
                                            {
                                                for (int i = 0; i < buttonCount; i++)
                                                {
                                                    IntPtr buttonPtr = new IntPtr(hShared.ToInt64() + (i * buttonSize));
                                                    buttons[i] = Marshal.PtrToStructure<THUMBBUTTON>(buttonPtr);
                                                }

                                                SHUnlockShared(hShared);
                                                // Convert to wrapper class for WPF binding (WPF cannot bind to struct fields)
                                                win.ThumbnailButtons = buttons.Select(b => new ThumbnailButton(b)).ToArray();
                                                ShellLogger.Debug($"TasksService: Updated {buttonCount} thumbnail buttons for HWND {msg.WParam}");

                                                System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: ThumbBarUpdateButtons - Updated {buttonCount} buttons\n");
                                                for (int i = 0; i < buttons.Length; i++)
                                                {
                                                    System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: Updated Button {i}: ID={buttons[i].iId}, Bitmap={buttons[i].iBitmap}, Flags={buttons[i].dwFlags}, Tooltip=\"{buttons[i].szTip}\"\n");
                                                }
                                            }

                                            CloseHandle(hProcess);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ShellLogger.Error($"TasksService: Error parsing ThumbBarUpdateButtons: {ex.Message}");
                                System.IO.File.AppendAllText(updateDebugPath, $"{DateTime.Now}: EXCEPTION in ThumbBarUpdateButtons: {ex.Message}\n{ex.StackTrace}\n");
                            }
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 78:
                        // ThumbBarSetImageList
                        ShellLogger.Debug("TasksService: ITaskbarList: ThumbBarSetImageList HWND:" + msg.WParam);
                        string imageListDebugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");
                        System.IO.File.AppendAllText(imageListDebugPath, $"{DateTime.Now}: MESSAGE RECEIVED - ThumbBarSetImageList HWND:{msg.WParam}, ImageList:{msg.LParam}\n");

                        win = new ApplicationWindow(this, msg.WParam);
                        if (Windows.Contains(win))
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            win.ThumbnailButtonImageList = msg.LParam;
                            ShellLogger.Debug($"TasksService: Set thumbnail button image list for HWND {msg.WParam}: {msg.LParam}");
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 79:
                        // SetOverlayIcon - Icon
                        ShellLogger.Debug("TasksService: ITaskbarList: SetOverlayIcon - Icon HWND:" + msg.WParam);

                        win = new ApplicationWindow(this, msg.WParam);
                        if (Windows.Contains(win))
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            win.SetOverlayIcon(msg.LParam);
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 80:
                        // SetThumbnailTooltip
                        ShellLogger.Debug("TasksService: ITaskbarList: SetThumbnailTooltip HWND:" + msg.WParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 81:
                        // SetThumbnailClip
                        ShellLogger.Debug("TasksService: ITaskbarList: SetThumbnailClip HWND:" + msg.WParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 85:
                        // SetOverlayIcon - Description
                        ShellLogger.Debug("TasksService: ITaskbarList: SetOverlayIcon - Description HWND:" + msg.WParam);

                        win = new ApplicationWindow(this, msg.WParam);
                        if (Windows.Contains(win))
                        {
                            win = Windows.First(wnd => wnd.Handle == msgCopy.WParam);
                            win.SetOverlayIconDescription(msg.LParam);
                        }

                        msg.Result = IntPtr.Zero;
                        return;
                    case (int)WM.USER + 87:
                        // SetTabProperties
                        ShellLogger.Debug("TasksService: ITaskbarList: SetTabProperties HWND:" + msg.WParam);
                        msg.Result = IntPtr.Zero;
                        return;
                    default:
                        ShellLogger.Debug($"TasksService: Unknown ITaskbarList Msg: {msg.Msg} LParam: {msg.LParam} WParam: {msg.WParam}");
                        break;
                }
            }

            handled = false;
        }

        private void MoveEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd != IntPtr.Zero && idObject == 0 && idChild == 0)
            {
                if (Windows.Any(i => i.Handle == hWnd))
                {
                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == hWnd);
                    win.SetMonitor();
                }
            }
        }

        private void CloakEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd != IntPtr.Zero && idObject == 0 && idChild == 0)
            {
                if (Windows.Any(i => i.Handle == hWnd))
                {
                    ApplicationWindow win = Windows.First(wnd => wnd.Handle == hWnd);
                    ShellLogger.Debug($"TasksService: {(eventType == EVENT_OBJECT_CLOAKED ? "Cloak" : "Uncloak")} event received for {win.Title}");
                    win.SetShowInTaskbar();
                }
            }
        }

        // set property on hook window that should receive ITaskbarList messages
        private void setTaskbarListHwnd(IntPtr hwndHook)
        {
            bool resetProp = true;

            // get the topmost tray
            IntPtr taskbarHwnd = WindowHelper.FindWindowsTray(IntPtr.Zero);
            
            if (taskbarHwnd == IntPtr.Zero)
            {
                return;
            }

            // if our tray is running, there may also be a second tray running
            IntPtr systemTaskbarHwnd = WindowHelper.FindWindowsTray(taskbarHwnd);

            if (hwndHook == IntPtr.Zero)
            {
                // no target hwnd provided
                // Try to find and use the handle of the Explorer hook window
                resetProp = false;
                hwndHook = getChildHwndByClass(systemTaskbarHwnd == IntPtr.Zero ? taskbarHwnd : systemTaskbarHwnd, "MSTaskSwWClass");
            }

            if (hwndHook == IntPtr.Zero)
            {
                // if still no hwnd to hook, we can't do anything
                return;
            }

            ShellLogger.Debug("TasksService: Adding TaskbandHWND prop to hwnd: " + taskbarHwnd);
            SetProp(taskbarHwnd, "TaskbandHWND", hwndHook);

            // Remove the property from the Explorer taskbar, if it is not the only tray
            if (resetProp && systemTaskbarHwnd != IntPtr.Zero)
            {
                ShellLogger.Debug("TasksService: Removing TaskbandHWND prop from hwnd: " + systemTaskbarHwnd);
                RemoveProp(systemTaskbarHwnd, "TaskbandHWND");
            }
        }

        private IntPtr getChildHwndByClass(IntPtr parentHwnd, string wndClass)
        {
            IntPtr childHwnd = IntPtr.Zero;
            EnumChildWindows(parentHwnd, (hwnd, lParam) =>
            {
                StringBuilder cName = new StringBuilder(256);
                GetClassName(hwnd, cName, cName.Capacity);
                if (cName.ToString() == wndClass)
                {
                    childHwnd = hwnd;
                    return false;
                }

                return true;
            }, 0);

            return childHwnd;
        }

        internal ObservableCollection<ApplicationWindow> Windows
        {
            get
            {
                return base.GetValue(windowsProperty) as ObservableCollection<ApplicationWindow>;
            }
            set
            {
                SetValue(windowsProperty, value);
            }
        }

        private DependencyProperty windowsProperty = DependencyProperty.Register("Windows",
            typeof(ObservableCollection<ApplicationWindow>), typeof(TasksService),
            new PropertyMetadata(new ObservableCollection<ApplicationWindow>()));
    }
}
