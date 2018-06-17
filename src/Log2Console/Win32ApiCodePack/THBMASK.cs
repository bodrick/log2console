using System;

namespace Log2Console.Win32ApiCodePack
{
    [Flags]
    internal enum THBMASK
    {
        THB_BITMAP = 0x1,
        THB_ICON = 0x2,
        THB_TOOLTIP = 0x4,
        THB_FLAGS = 0x8
    }
}