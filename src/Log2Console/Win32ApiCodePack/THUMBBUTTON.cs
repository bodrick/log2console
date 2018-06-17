using System;
using System.Runtime.InteropServices;

namespace Log2Console.Win32ApiCodePack
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct THUMBBUTTON
    {
        /// <summary>
        ///     WPARAM value for a THUMBBUTTON being clicked.
        /// </summary>
        internal const int THBN_CLICKED = 0x1800;

        [MarshalAs(UnmanagedType.U4)] internal THBMASK dwMask;
        internal uint iId;
        internal uint iBitmap;
        internal IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string szTip;

        [MarshalAs(UnmanagedType.U4)] internal THBFLAGS dwFlags;
    }
}