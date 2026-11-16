using ManagedShell.Common.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Threading;

namespace RetroBar.Utilities
{
    public enum NetworkConnectionType
    {
        None,
        Ethernet,
        WiFi,
        Other
    }

    public enum NetworkConnectivity
    {
        Disconnected,
        Limited,
        Internet
    }

    public record NetworkState(NetworkConnectionType Type, NetworkConnectivity Connectivity, int SignalBars, string Name)
    {
        public static readonly NetworkState Disconnected = new NetworkState(NetworkConnectionType.None, NetworkConnectivity.Disconnected, 0, null);
    }

    public record NetworkAdapter(uint Index, Guid Guid, string Name, NetworkConnectionType Type);

    /// <summary>
    /// Tracks the state of the primary physical network connection using Windows network change notifications.
    /// </summary>
    public class NetworkMonitor : IDisposable
    {
        private const uint IF_TYPE_ETHERNET_CSMACD = 6;
        private const uint IF_TYPE_IEEE80211 = 71;
        private const int IfOperStatusUp = 1;
        private const byte IfFlagConnectorPresent = 0x04;
        private const byte IfFlagFilterInterface = 0x02;

        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _refreshTimer;
        private readonly WlanNotificationCallback _wlanCallback;

        private INetworkListManager _networkListManager;
        private IConnectionPoint _nlmConnectionPoint;
        private int _nlmCookie;
        private IntPtr _wlanHandle;
        private bool _isDisposed;

        public NetworkState State { get; private set; } = NetworkState.Disconnected;

        public event EventHandler StateChanged;

        public NetworkMonitor()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _wlanCallback = WlanCallback;

            // Notifications often arrive in bursts (e.g. several per Wi-Fi association), so coalesce them.
            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            StartNetworkListManager();
            OpenWlanHandle();
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            Refresh();
        }

        #region Change notifications
        private void RequestRefresh()
        {
            // May be called from any thread
            _dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                if (_isDisposed)
                {
                    return;
                }

                _refreshTimer.Stop();
                _refreshTimer.Start();
            });
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _refreshTimer.Stop();
            Refresh();
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            RequestRefresh();
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                RequestRefresh();
            }
        }

        private void WlanCallback(IntPtr notificationData, IntPtr context)
        {
            RequestRefresh();
        }

        private void StartNetworkListManager()
        {
            try
            {
                _networkListManager = (INetworkListManager)new NetworkListManager();

                Guid eventsGuid = typeof(INetworkListManagerEvents).GUID;
                ((IConnectionPointContainer)_networkListManager).FindConnectionPoint(ref eventsGuid, out _nlmConnectionPoint);
                _nlmConnectionPoint.Advise(new NetworkListManagerEventSink(this), out _nlmCookie);
            }
            catch (Exception ex)
            {
                ShellLogger.Warning($"NetworkMonitor: Unable to subscribe to network list manager events: {ex.Message}");
            }
        }

        private void OpenWlanHandle()
        {
            if (_wlanHandle != IntPtr.Zero)
            {
                return;
            }

            try
            {
                // Fails if the WLAN AutoConfig service is not running; we try again on the next refresh with a Wi-Fi adapter.
                if (WlanOpenHandle(2, IntPtr.Zero, out _, out IntPtr handle) != 0)
                {
                    return;
                }

                _wlanHandle = handle;
                WlanRegisterNotification(_wlanHandle, WLAN_NOTIFICATION_SOURCE_ACM | WLAN_NOTIFICATION_SOURCE_MSM, true, _wlanCallback, IntPtr.Zero, IntPtr.Zero, out _);
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
            {
                // wlanapi.dll is not present on some editions (e.g. Server without the Wireless LAN feature)
            }
        }
        #endregion

        #region State
        public void Refresh()
        {
            if (_isDisposed)
            {
                return;
            }

            NetworkState newState;

            try
            {
                newState = GetCurrentState();
            }
            catch (Exception ex)
            {
                ShellLogger.Warning($"NetworkMonitor: Unable to get network state: {ex.Message}");
                return;
            }

            if (newState == State)
            {
                return;
            }

            ShellLogger.Debug($"NetworkMonitor: State changed to {newState}");
            State = newState;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private NetworkState GetCurrentState()
        {
            NetworkAdapter adapter = SelectAdapter(GetConnectedAdapters(), GetBestInterfaceIndex());
            bool? hasInternet = GetHasInternet();

            if (adapter == null)
            {
                // No physical Wi-Fi or Ethernet connection, but we may be connected some other way (e.g. cellular)
                return hasInternet == true ? new NetworkState(NetworkConnectionType.Other, NetworkConnectivity.Internet, 0, null) : NetworkState.Disconnected;
            }

            // If the network list manager is unavailable, assume a connected adapter has internet access
            NetworkConnectivity connectivity = hasInternet == false ? NetworkConnectivity.Limited : NetworkConnectivity.Internet;
            int signalBars = 0;
            string name = adapter.Name;

            if (adapter.Type == NetworkConnectionType.WiFi)
            {
                OpenWlanHandle();

                if (GetWlanConnection(adapter.Guid, out string profileName, out uint signalQuality))
                {
                    name = profileName;
                    signalBars = GetSignalBars(signalQuality);
                }
                else
                {
                    signalBars = 4;
                }
            }

            return new NetworkState(adapter.Type, connectivity, signalBars, name);
        }

        /// <summary>
        /// Chooses the adapter that represents the current connection, preferring the adapter Windows would use to reach the internet.
        /// </summary>
        public static NetworkAdapter SelectAdapter(IEnumerable<NetworkAdapter> connectedAdapters, uint bestInterfaceIndex)
        {
            List<NetworkAdapter> adapters = connectedAdapters.ToList();

            // When the route goes through a VPN or virtual switch, the best interface is not a physical adapter, so fall back
            // to the connected physical adapter that Windows generally prefers (wired over wireless).
            return adapters.FirstOrDefault(adapter => adapter.Index == bestInterfaceIndex)
                ?? adapters.FirstOrDefault(adapter => adapter.Type == NetworkConnectionType.Ethernet)
                ?? adapters.FirstOrDefault();
        }

        public static int GetSignalBars(uint signalQuality)
        {
            if (signalQuality >= 75) return 4;
            if (signalQuality >= 50) return 3;
            if (signalQuality >= 25) return 2;
            return 1;
        }

        private static List<NetworkAdapter> GetConnectedAdapters()
        {
            List<NetworkAdapter> adapters = new List<NetworkAdapter>();

            if (GetIfTable2(out IntPtr table) != 0)
            {
                return adapters;
            }

            try
            {
                int count = Marshal.ReadInt32(table);
                int rowSize = Marshal.SizeOf<MIB_IF_ROW2>();

                for (int i = 0; i < count; i++)
                {
                    // MIB_IF_TABLE2.Table follows NumEntries, aligned to 8 bytes
                    MIB_IF_ROW2 row = Marshal.PtrToStructure<MIB_IF_ROW2>(table + 8 + i * rowSize);

                    NetworkConnectionType type = row.Type switch
                    {
                        IF_TYPE_ETHERNET_CSMACD => NetworkConnectionType.Ethernet,
                        IF_TYPE_IEEE80211 => NetworkConnectionType.WiFi,
                        _ => NetworkConnectionType.None
                    };

                    // Only consider physical adapters, which excludes virtual switches, VM adapters, and VPN tunnels.
                    if (type == NetworkConnectionType.None ||
                        row.OperStatus != IfOperStatusUp ||
                        (row.InterfaceAndOperStatusFlags & IfFlagConnectorPresent) == 0 ||
                        (row.InterfaceAndOperStatusFlags & IfFlagFilterInterface) != 0)
                    {
                        continue;
                    }

                    adapters.Add(new NetworkAdapter(row.InterfaceIndex, row.InterfaceGuid, row.Alias, type));
                }
            }
            finally
            {
                FreeMibTable(table);
            }

            return adapters;
        }

        private static uint GetBestInterfaceIndex()
        {
            // Determines the route to the internet; no traffic is sent.
            foreach (IPAddress address in new[] { IPAddress.Parse("8.8.8.8"), IPAddress.Parse("2001:4860:4860::8888") })
            {
                SocketAddress socketAddress = new IPEndPoint(address, 0).Serialize();
                byte[] sockaddr = new byte[socketAddress.Size];

                for (int i = 0; i < socketAddress.Size; i++)
                {
                    sockaddr[i] = socketAddress[i];
                }

                if (GetBestInterfaceEx(sockaddr, out uint index) == 0)
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>Physical interface counters only: never sum a VPN/virtual switch with its underlying link.</summary>
        public static List<NetworkTrafficCounter> GetTrafficCounters()
        {
            if (GetIfTable2(out IntPtr table) != 0)
                throw new NetworkInformationException();

            try
            {
                var counters = new List<NetworkTrafficCounter>();
                int count = Marshal.ReadInt32(table);
                int size = Marshal.SizeOf<MIB_IF_ROW2>();
                for (int i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_IF_ROW2>(table + 8 + i * size);
                    if (IsTrafficInterface(row.OperStatus, row.InterfaceAndOperStatusFlags))
                        counters.Add(new NetworkTrafficCounter(row.InterfaceGuid, row.Alias,
                            row.Statistics[2], row.Statistics[11]));
                }
                return counters;
            }
            finally
            {
                FreeMibTable(table);
            }
        }

        public static bool IsTrafficInterface(int operStatus, byte flags) =>
            operStatus == IfOperStatusUp && (flags & 0x05) == 0x05 &&
            (flags & (IfFlagFilterInterface | 0x10 | 0x20 | 0x40 | 0x80)) == 0;

        private bool? GetHasInternet()
        {
            if (_networkListManager == null)
            {
                return null;
            }

            try
            {
                return (_networkListManager.GetConnectivity() & (NLM_CONNECTIVITY.IPV4_INTERNET | NLM_CONNECTIVITY.IPV6_INTERNET)) != 0;
            }
            catch (Exception ex)
            {
                ShellLogger.Debug($"NetworkMonitor: Unable to get connectivity: {ex.Message}");
                return null;
            }
        }

        private bool GetWlanConnection(Guid interfaceGuid, out string profileName, out uint signalQuality)
        {
            profileName = null;
            signalQuality = 0;

            if (_wlanHandle == IntPtr.Zero ||
                WlanQueryInterface(_wlanHandle, ref interfaceGuid, WLAN_INTF_OPCODE_CURRENT_CONNECTION, IntPtr.Zero, out _, out IntPtr data, IntPtr.Zero) != 0)
            {
                return false;
            }

            try
            {
                WLAN_CONNECTION_ATTRIBUTES attributes = Marshal.PtrToStructure<WLAN_CONNECTION_ATTRIBUTES>(data);

                if (attributes.isState != WLAN_INTERFACE_STATE_CONNECTED)
                {
                    return false;
                }

                profileName = attributes.strProfileName;
                signalQuality = attributes.wlanAssociationAttributes.wlanSignalQuality;
                return true;
            }
            finally
            {
                WlanFreeMemory(data);
            }
        }
        #endregion

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _refreshTimer.Stop();
            NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

            if (_wlanHandle != IntPtr.Zero)
            {
                // Also unregisters notifications, waiting for any in-progress callbacks to finish
                WlanCloseHandle(_wlanHandle, IntPtr.Zero);
                _wlanHandle = IntPtr.Zero;
            }

            try
            {
                if (_nlmConnectionPoint != null && _nlmCookie != 0)
                {
                    _nlmConnectionPoint.Unadvise(_nlmCookie);
                }
            }
            catch (Exception ex)
            {
                ShellLogger.Debug($"NetworkMonitor: Unable to unsubscribe from network list manager events: {ex.Message}");
            }

            if (_nlmConnectionPoint != null)
            {
                Marshal.ReleaseComObject(_nlmConnectionPoint);
                _nlmConnectionPoint = null;
            }

            if (_networkListManager != null)
            {
                Marshal.ReleaseComObject(_networkListManager);
                _networkListManager = null;
            }
        }

        #region Network list manager
        [Flags]
        public enum NLM_CONNECTIVITY
        {
            DISCONNECTED = 0,
            IPV4_NOTRAFFIC = 0x1,
            IPV6_NOTRAFFIC = 0x2,
            IPV4_SUBNET = 0x10,
            IPV4_LOCALNETWORK = 0x20,
            IPV4_INTERNET = 0x40,
            IPV6_SUBNET = 0x100,
            IPV6_LOCALNETWORK = 0x200,
            IPV6_INTERNET = 0x400
        }

        [ComImport, Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B")]
        private class NetworkListManager
        {
        }

        [ComImport, Guid("DCB00000-570F-4A9B-8D69-199FDBA5723B"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        private interface INetworkListManager
        {
            // Only GetConnectivity is used; the preceding methods are declared to preserve the vtable layout.
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetNetworks(int flags);
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetNetwork(Guid networkId);
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetNetworkConnections();
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetNetworkConnection(Guid networkConnectionId);
            bool IsConnectedToInternet { [return: MarshalAs(UnmanagedType.VariantBool)] get; }
            bool IsConnected { [return: MarshalAs(UnmanagedType.VariantBool)] get; }
            NLM_CONNECTIVITY GetConnectivity();
        }

        [ComImport, Guid("DCB00001-570F-4A9B-8D69-199FDBA5723B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface INetworkListManagerEvents
        {
            void ConnectivityChanged(NLM_CONNECTIVITY newConnectivity);
        }

        [ComVisible(true)]
        public class NetworkListManagerEventSink : INetworkListManagerEvents
        {
            // Weak so that the connection point does not keep the monitor alive
            private readonly WeakReference<NetworkMonitor> _monitor;

            internal NetworkListManagerEventSink(NetworkMonitor monitor)
            {
                _monitor = new WeakReference<NetworkMonitor>(monitor);
            }

            public void ConnectivityChanged(NLM_CONNECTIVITY newConnectivity)
            {
                if (_monitor.TryGetTarget(out NetworkMonitor monitor))
                {
                    monitor.RequestRefresh();
                }
            }
        }
        #endregion

        #region IP Helper
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MIB_IF_ROW2
        {
            public ulong InterfaceLuid;
            public uint InterfaceIndex;
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            public string Alias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            public string Description;
            public uint PhysicalAddressLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] PhysicalAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] PermanentPhysicalAddress;
            public uint Mtu;
            public uint Type;
            public int TunnelType;
            public int MediaType;
            public int PhysicalMediumType;
            public int AccessType;
            public int DirectionType;
            public byte InterfaceAndOperStatusFlags;
            public int OperStatus;
            public int AdminStatus;
            public int MediaConnectState;
            public Guid NetworkGuid;
            public int ConnectionType;
            // Transmit/receive link speeds, then InOctets at [2] and OutOctets at [11].
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public ulong[] Statistics;
        }

        [DllImport("iphlpapi.dll")]
        private static extern int GetIfTable2(out IntPtr table);

        [DllImport("iphlpapi.dll")]
        private static extern void FreeMibTable(IntPtr memory);

        [DllImport("iphlpapi.dll")]
        private static extern int GetBestInterfaceEx(byte[] destAddr, out uint bestIfIndex);
        #endregion

        #region WLAN
        private const uint WLAN_NOTIFICATION_SOURCE_ACM = 0x08;
        private const uint WLAN_NOTIFICATION_SOURCE_MSM = 0x10;
        private const int WLAN_INTF_OPCODE_CURRENT_CONNECTION = 7;
        private const int WLAN_INTERFACE_STATE_CONNECTED = 1;

        private delegate void WlanNotificationCallback(IntPtr notificationData, IntPtr context);

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_ASSOCIATION_ATTRIBUTES
        {
            public DOT11_SSID dot11Ssid;
            public int dot11BssType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public int dot11PhyType;
            public uint uDot11PhyIndex;
            public uint wlanSignalQuality;
            public uint ulRxRate;
            public uint ulTxRate;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_ATTRIBUTES
        {
            public int isState;
            public int wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
            // Followed by WLAN_SECURITY_ATTRIBUTES, which we do not use
        }

        [DllImport("wlanapi.dll")]
        private static extern uint WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr reserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanRegisterNotification(IntPtr clientHandle, uint notifSource, bool ignoreDuplicate, WlanNotificationCallback funcCallback, IntPtr callbackContext, IntPtr reserved, out uint prevNotifSource);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanQueryInterface(IntPtr clientHandle, ref Guid interfaceGuid, int opCode, IntPtr reserved, out uint dataSize, out IntPtr data, IntPtr opcodeValueType);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr memory);
        #endregion
    }
}
