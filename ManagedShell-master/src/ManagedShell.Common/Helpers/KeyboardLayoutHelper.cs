using ManagedShell.Common.Structs;
using ManagedShell.Interop;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ManagedShell.Common.Helpers
{
    public static class KeyboardLayoutHelper
    {
        public static KeyboardLayout GetKeyboardLayout(bool currentThread = false)
        {
            uint threadId = 0;
            if (!currentThread)
                threadId = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
            var layout = NativeMethods.GetKeyboardLayout(threadId);

            return new KeyboardLayout()
            {
                HKL = layout,
                NativeName = CultureInfo.GetCultureInfo((short)layout).NativeName,
                ThreeLetterName = CultureInfo.GetCultureInfo((short)layout).ThreeLetterISOLanguageName.ToUpper()
            };
        }

        public static List<KeyboardLayout> GetKeyboardLayoutList()
        {
            var size = NativeMethods.GetKeyboardLayoutList(0, null);
            var result = new long[size];
            NativeMethods.GetKeyboardLayoutList(size, result);

            return result.Select(x => new KeyboardLayout()
            {
                HKL = (int)x,
                NativeName = CultureInfo.GetCultureInfo((short)x).NativeName,
                ThreeLetterName = CultureInfo.GetCultureInfo((short)x).ThreeLetterISOLanguageName.ToUpper()
            }).ToList();
        }

        public static bool SetKeyboardLayout(int layoutId)
        {
            return NativeMethods.PostMessage(0xffff,
                (uint) NativeMethods.WM.INPUTLANGCHANGEREQUEST,
                0,
                NativeMethods.LoadKeyboardLayout(layoutId.ToString("x8"), (uint)(NativeMethods.KLF.SUBSTITUTE_OK | NativeMethods.KLF.ACTIVATE)));
        }
    }
}
