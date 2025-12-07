using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        const string Kernel32_DllName = "kernel32.dll";

        [StructLayout(LayoutKind.Explicit)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            [FieldOffset(0)]
            public uint FileAttributes;

            [FieldOffset(4)]
            public FILETIME CreationTime;

            [FieldOffset(12)]
            public FILETIME LastAccessTime;

            [FieldOffset(20)]
            public FILETIME LastWriteTime;

            [FieldOffset(28)]
            public uint VolumeSerialNumber;

            [FieldOffset(32)]
            public uint FileSizeHigh;

            [FieldOffset(36)]
            public uint FileSizeLow;

            [FieldOffset(40)]
            public uint NumberOfLinks;

            [FieldOffset(44)]
            public uint FileIndexHigh;

            [FieldOffset(48)]
            public uint FileIndexLow;
        }

        [DllImport(Kernel32_DllName, SetLastError = true)]
        public static extern bool GetFileInformationByHandle(SafeFileHandle hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport(Kernel32_DllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile);

        // Handling the close splash screen event
        [DllImport(Kernel32_DllName)]
        public static extern Int32 OpenEvent(Int32 DesiredAccess, bool InheritHandle, string Name);

        // OpenEvent DesiredAccess defines
        public const int EVENT_MODIFY_STATE = 0x00000002;

        [DllImport(Kernel32_DllName)]
        public static extern Int32 SetEvent(Int32 Handle);

        [DllImport(Kernel32_DllName)]
        public static extern Int32 CloseHandle(Int32 Handle);

        [DllImport(Kernel32_DllName)]
        public static extern bool CloseHandle(IntPtr Handle);

        [DllImport(Kernel32_DllName, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        [DllImport(Kernel32_DllName)]
        public static extern uint GetCurrentProcessId();

        [DllImport(Kernel32_DllName, SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport(Kernel32_DllName, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpExeName, ref int lpdwSize);


        [DllImport(Kernel32_DllName, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport(Kernel32_DllName, SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            Int32 nSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport(Kernel32_DllName, SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            UIntPtr lpBaseAddress,
            IntPtr lpBuffer,
            Int32 nSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport(Kernel32_DllName, SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
            int dwSize, AllocationType dwFreeType);

        [DllImport(Kernel32_DllName, SetLastError = true)]
        public static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint applicationUserModelIdLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);
    }
}
