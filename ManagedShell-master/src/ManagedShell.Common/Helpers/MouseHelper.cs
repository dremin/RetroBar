using ManagedShell.Interop;

namespace ManagedShell.Common.Helpers
{
    public static class MouseHelper
    {
        public static uint GetCursorPositionParam()
        {
            return (uint)NativeMethods.MakeLParam(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
        }
    }
}
