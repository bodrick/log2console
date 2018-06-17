using System;
using System.Diagnostics.CodeAnalysis;

namespace Log2Console.Win32ApiCodePack
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum THBFLAGS
    {
        THBF_ENABLED = 0x00000000,
        THBF_DISABLED = 0x00000001,
        THBF_DISMISSONCLICK = 0x00000002,
        THBF_NOBACKGROUND = 0x00000004,
        THBF_HIDDEN = 0x00000008,
        THBF_NONINTERACTIVE = 0x00000010
    }
}