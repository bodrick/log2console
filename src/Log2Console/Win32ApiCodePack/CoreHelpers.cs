using System;

namespace Log2Console.Win32ApiCodePack
{
    internal static class CoreHelpers
    {
        /// <summary>
        ///     Determines if the application is running on Windows Vista or later 7
        /// </summary>
        public static bool RunningOnVistaOrLater => Environment.OSVersion.Version.Major >= 6;

        /// <summary>
        ///     Determines if the application is running on Windows 7
        /// </summary>
        public static bool RunningOnWin7 => Environment.OSVersion.Version.Major > 6 ||
                                            Environment.OSVersion.Version.Major == 6 &&
                                            Environment.OSVersion.Version.Minor >= 1;

        /// <summary>
        ///     Throws PlatformNotSupportedException if the application is not running on Windows 7
        /// </summary>
        public static void ThrowIfNotWin7()
        {
            if (!RunningOnWin7)
                throw new PlatformNotSupportedException("Only supported on Windows 7 or newer.");
        }
    }
}