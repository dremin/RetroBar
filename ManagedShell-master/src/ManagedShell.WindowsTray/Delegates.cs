using System;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    /// <summary>
    /// Delegate signature for the system tray callback.
    /// </summary>
    /// <param name="msg">The system tray message number.</param>
    /// <param name="nicData">The NotifyIconData structure</param>
    /// <returns>Indication of message outcome.</returns>
    public delegate bool SystrayDelegate(uint msg, SafeNotifyIconData nicData);

    /// <summary>
    /// Delegate signature for the icon data callback.
    /// </summary>
    /// <param name="dwMessage">The message sent</param>
    /// <param name="hWnd">The handle of the icon</param>
    /// <param name="uID">The the ID ofthe icon</param>
    /// <param name="guidItem">The GUID of the icon</param>
    /// <returns>Indication of message outcome.</returns>
    public delegate IntPtr IconDataDelegate(int dwMessage, uint hWnd, uint uID, Guid guidItem);

    /// <summary>
    /// Delegate signature for the tray host size callback.
    /// </summary>
    /// <returns>Indication of message outcome.</returns>
    public delegate TrayHostSizeData TrayHostSizeDelegate();

    /// <summary>
    /// Delegate signature for SHAppBarMessage callbacks.
    /// </summary>
    /// <param name="amd">The AppBar message data received by Shell_TrayWnd.</param>
    /// <param name="handled">Whether the delegate handled the message.</param>
    /// <returns>Result if handled.</returns>
    public delegate IntPtr AppBarMessageDelegate(APPBARMSGDATAV3 amd, ref bool handled);
}
