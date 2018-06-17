using System.Diagnostics.CodeAnalysis;

namespace Log2Console.Win32ApiCodePack
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum STPFLAG
    {
        STPF_NONE = 0x0,
        STPF_USEAPPTHUMBNAILALWAYS = 0x1,
        STPF_USEAPPTHUMBNAILWHENACTIVE = 0x2,
        STPF_USEAPPPEEKALWAYS = 0x4,
        STPF_USEAPPPEEKWHENACTIVE = 0x8
    }
}