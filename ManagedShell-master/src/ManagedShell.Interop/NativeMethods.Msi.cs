using System.Runtime.InteropServices;
using System.Text;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        const string Msi_DllName = "msi.dll";

        [DllImport(Msi_DllName, CharSet = CharSet.Unicode)]
        public static extern uint MsiGetShortcutTarget(string szShortcutTarget, [Out] StringBuilder szProductCode, [Out] StringBuilder szFeatureId, [Out] StringBuilder szComponentCode);

        [DllImport(Msi_DllName, CharSet = CharSet.Unicode)]
        public static extern int MsiGetComponentPath(string szProduct, string szComponent, [Out] StringBuilder lpPathBuf, [In, Out] ref int pcchBuf);
    }
}
