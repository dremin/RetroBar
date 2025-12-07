using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    public class NotificationArea : DependencyObject, IDisposable
    {
        public const string HARDWARE_GUID = "7820ae78-23e3-4229-82c1-e41cb67d5b9c";
        public const string UPDATE_GUID = "7820ae81-23e3-4229-82c1-e41cb67d5b9c";
        public const string MICROPHONE_GUID = "7820ae82-23e3-4229-82c1-e41cb67d5b9c";
        public const string LOCATION_GUID = "7820ae77-23e3-4229-82c1-e41cb67d5b9c";
        public const string HEALTH_GUID = "7820ae76-23e3-4229-82c1-e41cb67d5b9c";
        public const string MEETNOW_GUID = "7820ae83-23e3-4229-82c1-e41cb67d5b9c";
        public const string NETWORK_GUID = "7820ae74-23e3-4229-82c1-e41cb67d5b9c";
        public const string POWER_GUID = "7820ae75-23e3-4229-82c1-e41cb67d5b9c";
        public const string VOLUME_GUID = "7820ae73-23e3-4229-82c1-e41cb67d5b9c";

        public static readonly string[] DEFAULT_PINNED = {
            HEALTH_GUID,
            POWER_GUID,
            NETWORK_GUID,
            VOLUME_GUID
        };

        internal static readonly List<string> Win11ActionCenterIcons = EnvironmentHelper.IsWindows1122H2OrBetter ? new List<string>()
        {
            // In 22H2, the network and power flyouts work again
            VOLUME_GUID
        } : new List<string>()
        {
            NETWORK_GUID,
            POWER_GUID,
            VOLUME_GUID
        };

        readonly NativeMethods.Rect defaultPlacement = new NativeMethods.Rect
        {
            Top = 0,
            Left = GetSystemMetrics(0) - 200,
            Bottom = 23,
            Right = 23
        };
        
        public string[] PinnedNotifyIcons { get; internal set; }
        public IntPtr Handle { get; private set; }
        public bool IsFailed { get; private set; }

        public event EventHandler<NotificationBalloonEventArgs> NotificationBalloonShown;

        private AppBarMessageDelegate appBarMessageDelegate;
        private SystrayDelegate trayDelegate;
        private IconDataDelegate iconDataDelegate;
        private object _lockObject = new object();
        private ShellServiceObject shellServiceObject;
        private TrayHostSizeData trayHostSizeData = new TrayHostSizeData
        {
            edge = ABEdge.ABE_TOP,
            rc = new NativeMethods.Rect
            {
                Top = 0,
                Left = 0,
                Bottom = 23,
                Right = GetSystemMetrics(0)
            }
        };

        public NotificationArea(TrayService trayService, ExplorerTrayService explorerTrayService) : this(DEFAULT_PINNED, trayService, explorerTrayService)
        {
        }
        
        public NotificationArea(string[] savedPinnedIcons, TrayService trayService, ExplorerTrayService explorerTrayService)
        {
            PinnedNotifyIcons = savedPinnedIcons;
            _trayService = trayService;
            _explorerTrayService = explorerTrayService;
        }

        public ObservableCollection<NotifyIcon> TrayIcons
        {
            get
            {
                return GetValue(iconListProperty) as ObservableCollection<NotifyIcon>;
            }
            set
            {
                SetValue(iconListProperty, value);
            }
        }

        private static DependencyProperty iconListProperty = DependencyProperty.Register("TrayIcons", typeof(ObservableCollection<NotifyIcon>), typeof(NotificationArea), new PropertyMetadata(new ObservableCollection<NotifyIcon>()));

        public ICollectionView AllIcons
        {
            get
            {
                return GetValue(allIconsProperty) as ICollectionView;
            }
            set
            {
                SetValue(allIconsProperty, value);
            }
        }

        private static DependencyProperty allIconsProperty = DependencyProperty.Register("AllIcons", typeof(ICollectionView), typeof(NotificationArea));

        public ICollectionView PinnedIcons
        {
            get
            {
                return GetValue(pinnedIconsProperty) as ICollectionView;
            }
            set
            {
                SetValue(pinnedIconsProperty, value);
            }
        }

        private static DependencyProperty pinnedIconsProperty = DependencyProperty.Register("PinnedIcons", typeof(ICollectionView), typeof(NotificationArea));

        public ICollectionView UnpinnedIcons
        {
            get
            {
                return GetValue(unpinnedIconsProperty) as ICollectionView;
            }
            set
            {
                SetValue(unpinnedIconsProperty, value);
            }
        }

        private static DependencyProperty unpinnedIconsProperty = DependencyProperty.Register("UnpinnedIcons", typeof(ICollectionView), typeof(NotificationArea));
        private readonly TrayService _trayService;
        private readonly ExplorerTrayService _explorerTrayService;

        public void Initialize()
        {
            try
            {
                prepareCollections();
                trayDelegate = SysTrayCallback;
                iconDataDelegate = IconDataCallback;

                _explorerTrayService.SetSystrayCallback(trayDelegate);
                _explorerTrayService.Run();

                _trayService.SetSystrayCallback(trayDelegate);
                _trayService.SetIconDataCallback(iconDataDelegate);
                Handle = _trayService.Initialize();
                _trayService.Run();

                // load the shell system tray objects (network, power, etc)
                shellServiceObject = new ShellServiceObject();
                shellServiceObject.Start();
            }
            catch
            {
                IsFailed = true;
            }
        }

        public void SetPinnedIcons(string[] pinnedIcons)
        {
            PinnedNotifyIcons = pinnedIcons;
            
            UpdatePinnedIcons();
        }

        internal void UpdatePinnedIcons()
        {
            foreach (NotifyIcon notifyIcon in TrayIcons)
            {
                notifyIcon.SetPinValues();
            }
        }

        #region Collections
        private void prepareCollections()
        {
            // prepare grouped collections like the taskbar
            // then display these in system tray

            // prepare collections
            AllIcons = new ListCollectionView(TrayIcons);
            AllIcons.CollectionChanged += Icons_Changed;
            AllIcons.Filter = AllIcons_Filter;
            var allIconsView = AllIcons as ICollectionViewLiveShaping;
            allIconsView.IsLiveFiltering = true;
            allIconsView.LiveFilteringProperties.Add("IsHidden");

            PinnedIcons = new ListCollectionView(TrayIcons);
            PinnedIcons.CollectionChanged += Icons_Changed;
            PinnedIcons.Filter = PinnedIcons_Filter;
            PinnedIcons.SortDescriptions.Add(new SortDescription("PinOrder", ListSortDirection.Ascending));
            var pinnedIconsView = PinnedIcons as ICollectionViewLiveShaping;
            pinnedIconsView.IsLiveFiltering = true;
            pinnedIconsView.LiveFilteringProperties.Add("IsHidden");
            pinnedIconsView.LiveFilteringProperties.Add("IsPinned");
            pinnedIconsView.IsLiveSorting = true;
            pinnedIconsView.LiveSortingProperties.Add("PinOrder");

            UnpinnedIcons = new ListCollectionView(TrayIcons);
            UnpinnedIcons.CollectionChanged += Icons_Changed;
            UnpinnedIcons.Filter = UnpinnedIcons_Filter;
            var unpinnedIconsView = UnpinnedIcons as ICollectionViewLiveShaping;
            unpinnedIconsView.IsLiveFiltering = true;
            unpinnedIconsView.LiveFilteringProperties.Add("IsHidden");
            unpinnedIconsView.LiveFilteringProperties.Add("IsPinned");
        }

        private void Icons_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            // yup, do nothing. helps prevent a NRE
        }

        private bool AllIcons_Filter(object item)
        {
            return !(item as NotifyIcon).IsHidden;
        }

        private bool PinnedIcons_Filter(object item)
        {
            return (item as NotifyIcon).IsPinned && !(item as NotifyIcon).IsHidden;
        }

        private bool UnpinnedIcons_Filter(object item)
        {
            return !(item as NotifyIcon).IsPinned && !(item as NotifyIcon).IsHidden;
        }
        #endregion

        #region Callbacks
        private IntPtr IconDataCallback(int dwMessage, uint hWnd, uint uID, Guid guidItem)
        {
            NotifyIcon icon = null;
            foreach (NotifyIcon ti in TrayIcons)
            {
                if ((guidItem != Guid.Empty && guidItem == ti.GUID) || (ti.HWnd == (IntPtr)hWnd && ti.UID == uID))
                {
                    icon = ti;
                    break;
                }
            }

            if (icon != null)
            {
                if (dwMessage == 1)
                    return MakeLParamIntPtr(icon.Placement.Left, icon.Placement.Top);
                else if (dwMessage == 2)
                    return MakeLParamIntPtr(icon.Placement.Right, icon.Placement.Bottom);
            }
            else if (guidItem == new Guid(VOLUME_GUID))
            {
                // If the shell is using sndvol32 as a volume control, it won't have a corresponding
                // tray icon in Windows 10+, but it still requests the icon location. Provide the tray.
                if (dwMessage == 1)
                    return MakeLParamIntPtr(trayHostSizeData.rc.Left, trayHostSizeData.rc.Top);
                else if (dwMessage == 2)
                    return MakeLParamIntPtr(trayHostSizeData.rc.Right, trayHostSizeData.rc.Bottom);
            }

            return IntPtr.Zero;
        }

        private bool SysTrayCallback(uint message, SafeNotifyIconData nicData)
        {
            // We always need hWnd for new icons, or for existing icons if no GUID is present
            if (nicData.hWnd == IntPtr.Zero && (nicData.guidItem == Guid.Empty || (NIM)message == NIM.NIM_ADD))
                return false;

            NotifyIcon trayIcon = new NotifyIcon(this, nicData.hWnd);
            trayIcon.UID = nicData.uID;

            lock (_lockObject)
            {
                if ((NIM)message == NIM.NIM_ADD || (NIM)message == NIM.NIM_MODIFY)
                {
                    try
                    {
                        bool exists = false;
                        bool titleChanged = false;

                        // hide icons while we are shell which require UWP support & we have a separate implementation for
                        if (nicData.guidItem == new Guid(VOLUME_GUID) && ((EnvironmentHelper.IsAppRunningAsShell && EnvironmentHelper.IsWindows10OrBetter) || GroupPolicyHelper.HideScaVolume))
                            return false;

                        // hide icons per group policy
                        if ((nicData.guidItem == new Guid(HEALTH_GUID) && GroupPolicyHelper.HideScaHealth) ||
                            (nicData.guidItem == new Guid(MEETNOW_GUID) && GroupPolicyHelper.HideScaMeetNow) ||
                            (nicData.guidItem == new Guid(NETWORK_GUID) && GroupPolicyHelper.HideScaNetwork) ||
                            (nicData.guidItem == new Guid(POWER_GUID) && GroupPolicyHelper.HideScaPower))
                            return false;

                        foreach (NotifyIcon ti in TrayIcons)
                        {
                            if (ti.Equals(nicData))
                            {
                                exists = true;
                                trayIcon = ti;
                                break;
                            }
                        }

                        if ((NIF.STATE & nicData.uFlags) != 0)
                            trayIcon.IsHidden = nicData.dwState == 1;

                        if ((NIF.TIP & nicData.uFlags) != 0 && !string.IsNullOrEmpty(nicData.szTip))
                        {
                            trayIcon.Title = nicData.szTip;
                            titleChanged = true;
                        }

                        if ((NIF.ICON & nicData.uFlags) != 0)
                        {
                            if (nicData.hIcon != IntPtr.Zero)
                            {
                                System.Windows.Media.ImageSource icon = IconImageConverter.GetImageFromHIcon(nicData.hIcon, false);

                                if (icon != null)
                                {
                                    trayIcon.Icon = icon;
                                }
                                else if (icon == null && trayIcon.Icon == null)
                                {
                                    // Use default only if we don't have a valid icon already
                                    trayIcon.Icon = IconImageConverter.GetDefaultIcon();
                                }
                            }
                            else
                            {
                                trayIcon.Icon = null;
                            }
                        }

                        if (nicData.hWnd != IntPtr.Zero)
                        {
                            trayIcon.HWnd = nicData.hWnd;
                            trayIcon.UID = nicData.uID;
                        }

                        if ((NIF.GUID & nicData.uFlags) != 0)
                            trayIcon.GUID = nicData.guidItem;

                        if (nicData.uVersion > 0 && nicData.uVersion <= 4)
                            trayIcon.Version = nicData.uVersion;

                        if ((NIF.MESSAGE & nicData.uFlags) != 0)
                            trayIcon.CallbackMessage = nicData.uCallbackMessage;

                        if (!exists)
                        {
                            // we need a valid hWnd to add a new icon
                            if (nicData.hWnd == IntPtr.Zero)
                                return false;

                            // default placement to a menu bar-like rect
                            trayIcon.Placement = defaultPlacement;

                            // set properties used for pinning
                            trayIcon.Path = ShellHelper.GetPathForWindowHandle(trayIcon.HWnd);
                            trayIcon.SetPinValues();

                            if (trayIcon.Icon == null)
                                trayIcon.Icon = IconImageConverter.GetDefaultIcon();

                            TrayIcons.Add(trayIcon);

                            if ((NIF.INFO & nicData.uFlags) != 0)
                                handleBalloonData(nicData, trayIcon);

                            ShellLogger.Debug($"NotificationArea: Added: {trayIcon.Title} Path: {trayIcon.Path} Hidden: {trayIcon.IsHidden} GUID: {trayIcon.GUID} UID: {trayIcon.UID} Version: {trayIcon.Version}");

                            if ((NIM)message == NIM.NIM_MODIFY)
                            {
                                // return an error to the notifyicon as we received a modify for an icon we did not yet have
                                return false;
                            }
                        }
                        else
                        {
                            if ((NIF.INFO & nicData.uFlags) != 0)
                                handleBalloonData(nicData, trayIcon);

                            if (titleChanged && trayIcon.GUID == default && !trayIcon.IsPinned)
                            {
                                // set properties used for pinning
                                trayIcon.SetPinValues();
                            }

                            ShellLogger.Debug($"NotificationArea: Modified: {trayIcon.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ShellLogger.Error("NotificationArea: Unable to modify the icon in the collection.", ex);
                    }
                }
                else if ((NIM)message == NIM.NIM_DELETE)
                {
                    try
                    {
                        foreach (var icon in TrayIcons)
                        {
                            if (icon.Equals(trayIcon))
                            {
                                TrayIcons.Remove(trayIcon);

                                ShellLogger.Debug($"NotificationArea: Removed: {icon.Title}");

                                return true;
                            }
                        }

                        return false;
                    }
                    catch (Exception ex)
                    {
                        ShellLogger.Error("NotificationArea: Unable to remove the icon from the collection.", ex);
                    }
                }
                else if ((NIM)message == NIM.NIM_SETVERSION)
                {
                    if (nicData.uVersion > 4)
                    {
                        return false;
                    }

                    foreach (NotifyIcon ti in TrayIcons)
                    {
                        if (ti.Equals(nicData))
                        {
                            ti.Version = nicData.uVersion;
                            ShellLogger.Debug($"NotificationArea: Modified version to {ti.Version} on: {ti.Title}");
                            break;
                        }
                    }
                }
            }
            return true;
        }
        #endregion

        private void handleBalloonData(SafeNotifyIconData nicData, NotifyIcon notifyIcon)
        {
            if (string.IsNullOrEmpty(nicData.szInfoTitle))
            {
                return;
            }

            NotificationBalloon balloonInfo = new NotificationBalloon(nicData, notifyIcon);
            NotificationBalloonEventArgs args = new NotificationBalloonEventArgs
            {
                Balloon = balloonInfo
            };

            ShellLogger.Debug($"NotificationArea: Received notification \"{balloonInfo.Title}\" for {notifyIcon.Title}");

            NotificationBalloonShown?.Invoke(this, args);

            if (!args.Handled)
            {
                notifyIcon.TriggerNotificationBalloon(balloonInfo);
            }
        }

        // The notification area control calls this when an icon is clicked to set the placement of its host (such as for ABM_GETTASKBARPOS usage)
        public void SetTrayHostSizeData(TrayHostSizeData data)
        {
            trayHostSizeData = data;
            _trayService?.SetTrayHostSizeData(trayHostSizeData);
        }

        // The AppBarManager calls this to return information regarding the location of the notification area.
        // With Explorer, the notification area is always in an AppBar, but we do not have that assumption.
        public void FillTrayHostSizeData(ref APPBARDATAV2 abd)
        {
            TrayHostSizeData msd = trayHostSizeData;
            abd.rc = msd.rc;
            abd.uEdge = (uint)msd.edge;
        }

        // The AppBarManager calls this to provide the delegate that handles AppBar messages received by the tray
        public void SetAppBarMessageCallback(AppBarMessageDelegate theDelegate)
        {
            if (theDelegate == null)
            {
                return;
            }

            appBarMessageDelegate = theDelegate;
            _trayService?.SetAppBarMessageCallback(appBarMessageDelegate);
        }

        public void Dispose()
        {
            if (!IsFailed && trayDelegate != null)
            {
                shellServiceObject?.Dispose();
                _trayService.Dispose();
            }
        }
    }
}
