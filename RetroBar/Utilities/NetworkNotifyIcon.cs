using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.WindowsTray;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Provides the network notification icon on Windows 11 24H2 and later, where Windows no longer provides one.
    /// </summary>
    public class NetworkNotifyIcon : IDisposable
    {
        private const int CallbackMessage = (int)NativeMethods.WM.USER + 1;
        private const int NIN_SELECT = (int)NativeMethods.WM.USER;
        private const int NIN_KEYSELECT = NIN_SELECT | 0x1;
        private const int WM_CONTEXTMENU = 0x007B;

        private const uint MF_STRING = 0x0;
        private const uint MenuNetworkSettings = 1;
        private const uint MenuNetworkConnections = 2;

        private const double IconSize = 16;
        private const double SignalBackgroundOpacity = 0.35;

        private const char GlyphWiFi1 = '\uEC3C';
        private const char GlyphWiFi2 = '\uEC3D';
        private const char GlyphWiFi3 = '\uEC3E';
        private const char GlyphWiFi4 = '\uE701';
        private const char GlyphNoInternetBadge = '\uE871';
        private const char GlyphEthernet = '\uE839';
        private const char GlyphEthernetNoInternet = '\uEB56';
        private const char GlyphGlobe = '\uE774';
        private const char GlyphDisconnected = '\uF384';

        private readonly NotificationArea _notificationArea;
        private readonly NetworkMonitor _networkMonitor;
        private readonly HwndSource _messageWindow;
        private readonly NotifyIcon _notifyIcon;
        private readonly GlyphTypeface _glyphTypeface;

        /// <summary>
        /// Windows 11 24H2 removed the legacy network notification icon, so we provide our own.
        /// </summary>
        public static bool IsNeeded => EnvironmentHelper.IsWindows1124H2OrBetter && !GroupPolicyHelper.HideScaNetwork;

        public NetworkNotifyIcon(NotificationArea notificationArea)
        {
            Guid networkGuid = new Guid(NotificationArea.NETWORK_GUID);

            if (notificationArea.IsFailed || notificationArea.TrayIcons.Any(icon => icon.GUID == networkGuid))
            {
                ShellLogger.Info("NetworkNotifyIcon: Notification area unavailable or network icon already present");
                return;
            }

            _notificationArea = notificationArea;
            _glyphTypeface = GetGlyphTypeface("Segoe Fluent Icons") ?? GetGlyphTypeface("Segoe MDL2 Assets");

            // Receives the notify icon callback messages, as well as broadcast setting changes for theme updates
            _messageWindow = new HwndSource(new HwndSourceParameters("RetroBarNetworkNotifyIcon")
            {
                WindowStyle = 0,
                ExtendedWindowStyle = (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW
            });
            _messageWindow.AddHook(WndProc);

            _networkMonitor = new NetworkMonitor();
            _networkMonitor.StateChanged += NetworkMonitor_StateChanged;

            // Using the network GUID allows the existing visibility, order, and invert settings to apply to this icon.
            _notifyIcon = new NotifyIcon(notificationArea, _messageWindow.Handle)
            {
                GUID = networkGuid,
                CallbackMessage = CallbackMessage,
                Version = 4,
                Path = ExePath.GetExecutablePath()
            };
            UpdateIcon();
            _notifyIcon.SetPinValues();
            notificationArea.TrayIcons.Add(_notifyIcon);

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private bool IsOwnIcon()
        {
            // If Windows adds a network icon after all, ManagedShell merges it into ours (same GUID), so we step aside.
            return _notifyIcon != null && _notifyIcon.HWnd == _messageWindow.Handle;
        }

        private void NetworkMonitor_StateChanged(object sender, EventArgs e)
        {
            UpdateIcon();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Language))
            {
                UpdateIcon();
            }
        }

        private void UpdateIcon()
        {
            if (!IsOwnIcon())
            {
                return;
            }

            NetworkState state = _networkMonitor.State;
            _notifyIcon.Icon = CreateIconImage(state);
            _notifyIcon.Title = GetTitle(state);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == CallbackMessage && IsOwnIcon())
            {
                // With NOTIFYICON_VERSION_4, the event is in the low word of lParam
                int iconEvent = (int)(lParam.ToInt64() & 0xFFFF);

                // Run after the click handler that sent this message has returned, since we may show a modal menu
                if (iconEvent == NIN_SELECT || iconEvent == NIN_KEYSELECT)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(ShowNetworkFlyout);
                }
                else if (iconEvent == WM_CONTEXTMENU)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(ShowContextMenu);
                }

                handled = true;
            }
            else if (msg == (int)NativeMethods.WM.SETTINGCHANGE && lParam != IntPtr.Zero && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
            {
                // Light/dark mode changed
                UpdateIcon();
            }

            return IntPtr.Zero;
        }

        #region Actions
        private void ShowNetworkFlyout()
        {
            if (!ShellHelper.StartProcess("ms-availablenetworks:"))
            {
                ShellHelper.StartProcess("ms-settings:network");
            }
        }

        private void ShowContextMenu()
        {
            IntPtr menu = CreatePopupMenu();
            AppendMenu(menu, MF_STRING, MenuNetworkSettings, GetMenuText("network_settings", "_Open Network & Internet settings"));
            AppendMenu(menu, MF_STRING, MenuNetworkConnections, GetMenuText("network_connections", "Network _Connections"));

            NativeMethods.GetCursorPos(out NativeMethods.POINT cursor);

            // Allows the menu to close when clicking elsewhere
            NativeMethods.SetForegroundWindow(_messageWindow.Handle);
            uint command = NativeMethods.TrackPopupMenuEx(menu, NativeMethods.TPM.RETURNCMD | NativeMethods.TPM.RIGHTBUTTON, cursor.x, cursor.y, _messageWindow.Handle, IntPtr.Zero);
            DestroyMenu(menu);

            switch (command)
            {
                case MenuNetworkSettings:
                    ShellHelper.StartProcess("ms-settings:network");
                    break;
                case MenuNetworkConnections:
                    ShellHelper.StartProcess("ncpa.cpl");
                    break;
            }
        }
        #endregion

        #region Appearance
        private static string GetTitle(NetworkState state)
        {
            string status = state.Connectivity switch
            {
                NetworkConnectivity.Internet => GetString("network_internet_access", "Internet access"),
                NetworkConnectivity.Limited => GetString("network_no_internet_access", "No internet access"),
                _ => GetString("network_not_connected", "Not connected")
            };

            return string.IsNullOrEmpty(state.Name) ? status : state.Name + "\n" + status;
        }

        private static string GetString(string key, string fallback)
        {
            return Application.Current?.TryFindResource(key) as string ?? fallback;
        }

        private static string GetMenuText(string key, string fallback)
        {
            // Convert the WPF access key marker to the Win32 one
            return GetString(key, fallback).Replace("&", "&&").Replace("_", "&");
        }

        private ImageSource CreateIconImage(NetworkState state)
        {
            // Match the system icons, which are drawn for the Windows taskbar color mode; RetroBar inverts them per theme.
            Brush brush = SystemUsesLightTheme() ? Brushes.Black : Brushes.White;

            DrawingGroup drawing = new DrawingGroup();

            // Fixed bounds keep all glyphs aligned and the same size
            drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, IconSize, IconSize))));

            switch (state.Type)
            {
                case NetworkConnectionType.WiFi:
                    Geometry background = GetGlyph(GlyphWiFi4);
                    Geometry signal = GetGlyph(state.SignalBars switch
                    {
                        1 => GlyphWiFi1,
                        2 => GlyphWiFi2,
                        3 => GlyphWiFi3,
                        _ => GlyphWiFi4
                    });

                    bool showBadge = state.Connectivity != NetworkConnectivity.Internet;

                    if (showBadge)
                    {
                        // Cut out space for the badge so that it remains legible
                        Geometry badgeSpace = new EllipseGeometry(new Point(11.5, 11.5), 5.5, 5.5);
                        background = Geometry.Combine(background, badgeSpace, GeometryCombineMode.Exclude, null);
                        signal = Geometry.Combine(signal, badgeSpace, GeometryCombineMode.Exclude, null);
                    }

                    drawing.Children.Add(new DrawingGroup
                    {
                        Children = { new GeometryDrawing(brush, null, background) },
                        Opacity = SignalBackgroundOpacity
                    });
                    drawing.Children.Add(new GeometryDrawing(brush, null, signal));

                    if (showBadge)
                    {
                        drawing.Children.Add(new GeometryDrawing(brush, null, GetGlyph(GlyphNoInternetBadge)));
                    }
                    break;
                case NetworkConnectionType.Ethernet:
                    drawing.Children.Add(new GeometryDrawing(brush, null, GetGlyph(state.Connectivity == NetworkConnectivity.Internet ? GlyphEthernet : GlyphEthernetNoInternet)));
                    break;
                case NetworkConnectionType.Other:
                    drawing.Children.Add(new GeometryDrawing(brush, null, GetGlyph(GlyphGlobe)));
                    break;
                default:
                    drawing.Children.Add(new GeometryDrawing(brush, null, GetGlyph(GlyphDisconnected)));
                    break;
            }

            DrawingImage image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

        private Geometry GetGlyph(char glyph)
        {
            if (_glyphTypeface == null || !_glyphTypeface.CharacterToGlyphMap.TryGetValue(glyph, out ushort glyphIndex))
            {
                return Geometry.Empty;
            }

            // Glyph outlines are relative to the baseline, which is at the bottom of the icon font's em square
            return new GeometryGroup
            {
                Children = { _glyphTypeface.GetGlyphOutline(glyphIndex, IconSize, IconSize) },
                Transform = new TranslateTransform(0, IconSize)
            };
        }

        private static GlyphTypeface GetGlyphTypeface(string fontFamily)
        {
            Typeface typeface = new Typeface(new FontFamily(fontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            // Typeface falls back to another font if the family is not installed, so check that we got the one we asked for
            if (typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface) && glyphTypeface.FamilyNames.Values.Contains(fontFamily))
            {
                return glyphTypeface;
            }

            return null;
        }

        private static bool SystemUsesLightTheme()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
        }
        #endregion

        public void Dispose()
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            if (_networkMonitor != null)
            {
                _networkMonitor.StateChanged -= NetworkMonitor_StateChanged;
                _networkMonitor.Dispose();
            }

            if (IsOwnIcon())
            {
                _notificationArea.TrayIcons.Remove(_notifyIcon);
            }

            _messageWindow?.RemoveHook(WndProc);
            _messageWindow?.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);
    }
}
