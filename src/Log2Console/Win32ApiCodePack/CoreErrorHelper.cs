namespace Log2Console.Win32ApiCodePack
{
    internal static class CoreErrorHelper
    {
        /// <summary>
        ///     This is intended for Library Internal use only.
        /// </summary>
        /// <param name="hresult">The error code.</param>
        /// <returns>True if the error code indicates success.</returns>
        public static bool Succeeded(int hresult)
        {
            return hresult >= 0;
        }
    }
}