using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Log2Console.Win32ApiCodePack
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class TaskbarNativeMethods
    {
        internal const int WM_COMMAND = 0x0111;

        internal static readonly uint WM_TASKBARBUTTONCREATED = RegisterWindowMessage("TaskbarButtonCreated");

        [DllImport(CommonDllNames.Shell32)]
        internal static extern void GetCurrentProcessExplicitAppUserModelID([Out] [MarshalAs(UnmanagedType.LPWStr)]
            out string AppID);

        [DllImport(CommonDllNames.Shell32)]
        internal static extern void SetCurrentProcessExplicitAppUserModelID(
            [MarshalAs(UnmanagedType.LPWStr)] string AppID);

        [DllImport(CommonDllNames.User32, EntryPoint = "RegisterWindowMessage", SetLastError = true,
            CharSet = CharSet.Unicode)]
        internal static extern uint RegisterWindowMessage([MarshalAs(UnmanagedType.LPWStr)] string lpString);
    }
}