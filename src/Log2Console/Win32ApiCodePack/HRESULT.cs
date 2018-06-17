using System.Diagnostics.CodeAnalysis;

namespace Log2Console.Win32ApiCodePack
{
    /// <summary>
    ///     HRESULT Wrapper
    ///     This is intended for Library Internal use only.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum HRESULT : uint
    {
        /// <summary>
        ///     S_FALSE
        /// </summary>
        S_FALSE = 0x0001,

        /// <summary>
        ///     S_OK
        /// </summary>
        S_OK = 0x0000,

        /// <summary>
        ///     E_INVALIDARG
        /// </summary>
        E_INVALIDARG = 0x80070057,

        /// <summary>
        ///     E_OUTOFMEMORY
        /// </summary>
        E_OUTOFMEMORY = 0x8007000E,

        /// <summary>
        ///     E_NOINTERFACE
        /// </summary>
        E_NOINTERFACE = 0x80004002,

        /// <summary>
        ///     E_FAIL
        /// </summary>
        E_FAIL = 0x80004005,

        /// <summary>
        ///     E_ELEMENTNOTFOUND
        /// </summary>
        E_ELEMENTNOTFOUND = 0x80070490,

        /// <summary>
        ///     TYPE_E_ELEMENTNOTFOUND
        /// </summary>
        TYPE_E_ELEMENTNOTFOUND = 0x8002802B,

        /// <summary>
        ///     NO_OBJECT
        /// </summary>
        NO_OBJECT = 0x800401E5,

        /// <summary>
        ///     Win32 Error code: ERROR_CANCELLED
        /// </summary>
        ERROR_CANCELLED = 1223,

        /// <summary>
        ///     ERROR_CANCELLED
        /// </summary>
        E_ERROR_CANCELLED = 0x800704C7,

        /// <summary>
        ///     The requested resource is in use
        /// </summary>
        RESOURCE_IN_USE = 0x800700AA
    }
}