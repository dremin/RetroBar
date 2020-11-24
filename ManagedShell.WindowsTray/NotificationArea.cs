using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using ManagedShell.Common.DesignPatterns;
using ManagedShell.Common.Helpers;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    public class NotificationArea : SingletonObject<NotificationArea>, IDisposable
    {
        const string VOLUME_GUID = "7820ae73-23e3-4229-82c1-e41cb67d5b9c";
        NativeMethods.Rect defaultPlacement = new NativeMethods.Rect { Top = 0, Left = GetSystemMetrics(0) - 200, Bottom = 23, Right = 23 };
        private SystrayDelegate trayDelegate;
        private IconDataDelegate iconDataDelegate;
        private TrayHostSizeDelegate trayHostSizeDelegate;
        private object _lockObject = new object();
        public IntPtr Handle;
        public bool IsFailed;
        private ShellServiceObject shellServiceObject;
        private TrayHostSizeData trayHostSizeData = new TrayHostSizeData { edge = (int)ABEdge.ABE_TOP, rc = new NativeMethods.Rect { Top = 0, Left = 0, Bottom = 23, Right = GetSystemMetrics(0) } };

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


        private NotificationArea() { }

        public void Initialize()
        {
            try
            {
                prepareCollections();
                trayDelegate = SysTrayCallback;
                iconDataDelegate = IconDataCallback;
                trayHostSizeDelegate = TrayHostSizeCallback;

                ExplorerTrayService.Instance.SetSystrayCallback(trayDelegate);
                ExplorerTrayService.Instance.Run();

                TrayService.Instance.SetSystrayCallback(trayDelegate);
                TrayService.Instance.SetIconDataCallback(iconDataDelegate);
                TrayService.Instance.SetTrayHostSizeCallback(trayHostSizeDelegate);
                Handle = TrayService.Instance.Initialize();
                TrayService.Instance.Run();

                // load the shell system tray objects (network, power, etc)
                shellServiceObject = new ShellServiceObject();
                shellServiceObject.Start();
            }
            catch
            {
                IsFailed = true;
            }
        }

        #region Collections
        private void prepareCollections()
        {
            // prepare grouped collections like the taskbar
            // then display these in system tray

            // prepare collections
            PinnedIcons = new ListCollectionView(TrayIcons);
            PinnedIcons.CollectionChanged += PinnedIcons_Changed;
            PinnedIcons.Filter = PinnedIcons_Filter;
            PinnedIcons.SortDescriptions.Add(new SortDescription("PinOrder", ListSortDirection.Ascending));
            var pinnedIconsView = PinnedIcons as ICollectionViewLiveShaping;
            pinnedIconsView.IsLiveFiltering = true;
            pinnedIconsView.LiveFilteringProperties.Add("IsPinned");
            pinnedIconsView.IsLiveSorting = true;
            pinnedIconsView.LiveSortingProperties.Add("PinOrder");

            UnpinnedIcons = new ListCollectionView(TrayIcons);
            UnpinnedIcons.CollectionChanged += PinnedIcons_Changed;
            UnpinnedIcons.Filter = UnpinnedIcons_Filter;
            var unpinnedIconsView = UnpinnedIcons as ICollectionViewLiveShaping;
            unpinnedIconsView.IsLiveFiltering = true;
            unpinnedIconsView.LiveFilteringProperties.Add("IsPinned");
        }

        private void PinnedIcons_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            // yup, do nothing. helps prevent a NRE
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
        private TrayHostSizeData TrayHostSizeCallback()
        {
            return trayHostSizeData;
        }

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
                    return Shell.MakeLParam(icon.Placement.Left, icon.Placement.Top);
                else if (dwMessage == 2)
                    return Shell.MakeLParam(icon.Placement.Right, icon.Placement.Bottom);
            }
            else if (guidItem == new Guid(VOLUME_GUID))
            {
                if (dwMessage == 1)
                    return Shell.MakeLParam(defaultPlacement.Left, defaultPlacement.Top);
                else if (dwMessage == 2)
                    return Shell.MakeLParam(defaultPlacement.Right, defaultPlacement.Bottom);
            }

            return IntPtr.Zero;
        }

        private bool SysTrayCallback(uint message, SafeNotifyIconData nicData)
        {
            if (nicData.hWnd == IntPtr.Zero)
                return false;

            NotifyIcon trayIcon = new NotifyIcon(nicData.hWnd);
            trayIcon.UID = nicData.uID;

            lock (_lockObject)
            {
                if ((NIM)message == NIM.NIM_ADD || (NIM)message == NIM.NIM_MODIFY)
                {
                    try
                    {
                        bool exists = false;

                        // hide icons while we are shell which require UWP support & we have a separate implementation for
                        if (nicData.guidItem == new Guid(VOLUME_GUID) && ((Shell.IsCairoRunningAsShell && Shell.IsWindows10OrBetter) || GroupPolicyManager.Instance.HideScaVolume))
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
                            trayIcon.Title = nicData.szTip;

                        if ((NIF.ICON & nicData.uFlags) != 0)
                        {
                            if (nicData.hIcon != IntPtr.Zero)
                            {
                                try
                                {
                                    System.Windows.Media.Imaging.BitmapSource bs =
                                        System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                            nicData.hIcon, Int32Rect.Empty,
                                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                                    DestroyIcon(nicData.hIcon);

                                    if (bs != null)
                                    {
                                        bs.Freeze();
                                        trayIcon.Icon = bs;
                                    }
                                }
                                catch
                                {
                                    if (trayIcon.Icon == null)
                                        trayIcon.Icon = IconImageConverter.GetDefaultIcon();
                                }
                            }
                            else
                            {
                                trayIcon.Icon = null;
                            }
                        }

                        trayIcon.HWnd = nicData.hWnd;
                        trayIcon.UID = nicData.uID;

                        if ((NIF.GUID & nicData.uFlags) != 0)
                            trayIcon.GUID = nicData.guidItem;

                        // guess version in case we are receiving icons that aren't sending NIM_SETVERSION to new explorers
                        if ((NIF.VISTA_MASK & nicData.uFlags) != 0)
                            trayIcon.Version = 4;
                        else if ((NIF.XP_MASK & nicData.uFlags) != 0)
                            trayIcon.Version = 3;

                        if (nicData.uVersion > 0 && nicData.uVersion <= 4)
                            trayIcon.Version = nicData.uVersion;

                        if ((NIF.MESSAGE & nicData.uFlags) != 0)
                            trayIcon.CallbackMessage = nicData.uCallbackMessage;

                        if (!exists)
                        {
                            // default placement to a menu bar-like rect
                            trayIcon.Placement = defaultPlacement;

                            // set properties used for pinning
                            trayIcon.Path = Shell.GetPathForHandle(trayIcon.HWnd);
                            trayIcon.SetPinValues();

                            if (trayIcon.Icon == null)
                                trayIcon.Icon = IconImageConverter.GetDefaultIcon();

                            TrayIcons.Add(trayIcon);
                            CairoLogger.Instance.Debug($"NotificationArea: Added: {trayIcon.Title} Path: {trayIcon.Path} Hidden: {trayIcon.IsHidden} GUID: {trayIcon.GUID} UID: {trayIcon.UID} Version: {trayIcon.Version}");

                            if ((NIM)message == NIM.NIM_MODIFY)
                            {
                                // return an error to the notifyicon as we received a modify for an icon we did not yet have
                                return false;
                            }
                        }
                        else
                            CairoLogger.Instance.Debug($"NotificationArea: Modified: {trayIcon.Title}");
                    }
                    catch (Exception ex)
                    {
                        CairoLogger.Instance.Error("NotificationArea: Unable to modify the icon in the collection.", ex);
                    }
                }
                else if ((NIM)message == NIM.NIM_DELETE)
                {
                    try
                    {
                        if (!TrayIcons.Contains(trayIcon))
                        {
                            // Nothing to remove.
                            return false;
                        }

                        TrayIcons.Remove(trayIcon);

                        CairoLogger.Instance.Debug($"NotificationArea: Removed: {nicData.szTip}");
                    }
                    catch (Exception ex)
                    {
                        CairoLogger.Instance.Error("NotificationArea: Unable to remove the icon from the collection.", ex);
                    }
                }
                else if ((NIM)message == NIM.NIM_SETVERSION)
                {
                    foreach (NotifyIcon ti in TrayIcons)
                    {
                        if (ti.Equals(nicData))
                        {
                            ti.Version = nicData.uVersion;
                            CairoLogger.Instance.Debug($"NotificationArea: Modified version to {ti.Version} on: {ti.Title}");
                            break;
                        }
                    }
                }
            }
            return true;
        }
        #endregion

        // The notification area control calls this when an icon is clicked to set the placement of its host for ABM_GETTASKBARPOS usage
        public void SetTrayHostSizeData(TrayHostSizeData data)
        {
            trayHostSizeData = data;
        }

        public void Dispose()
        {
            if (!IsFailed && trayDelegate != null)
            {
                shellServiceObject?.Dispose();
                TrayService.Instance.Dispose();
            }
        }
    }
}
