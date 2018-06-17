using System;

namespace Log2Console.Win32ApiCodePack
{
    public sealed class ThumbnailToolbarManager
    {
        internal ThumbnailToolbarManager()
        {
        }

        public void AddButtons(IntPtr windowHandle, params ThumbnailToolbarButton[] buttons)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentException("Window handle cannot be empty", nameof(windowHandle));

            if (buttons == null || buttons.Length == 0)
                throw new ArgumentException("Null or empty arrays are not allowed.", nameof(buttons));

            if (buttons.Length > 7)
                throw new ArgumentException("Maximum number of buttons allowed is 7.", nameof(buttons));

            // Add the buttons to our window manager, which will also create a proxy window
            TaskbarWindowManager.Instance.AddThumbnailButtons(windowHandle, buttons);
        }
    }
}