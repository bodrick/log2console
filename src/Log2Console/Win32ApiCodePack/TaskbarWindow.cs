using System;

namespace Log2Console.Win32ApiCodePack
{
    internal class TaskbarWindow : IDisposable
    {
        public readonly IntPtr WindowHandle;

        private ThumbnailToolbarButton[] _thumbnailButtons;

        private ThumbnailToolbarProxyWindow _thumbnailToolbarProxyWindow;

        public TaskbarWindow(IntPtr userWindowHandle, ThumbnailToolbarButton[] buttons)
        {
            if (userWindowHandle == IntPtr.Zero)
                throw new ArgumentException("userWindowHandle");

            if (buttons == null || buttons.Length == 0)
                throw new ArgumentException("buttons");

            // Create our proxy window
            _thumbnailToolbarProxyWindow =
                new ThumbnailToolbarProxyWindow(userWindowHandle, buttons) {TaskbarWindow = this};

            // Set our current state
            ThumbnailButtons = buttons;
            WindowHandle = userWindowHandle;
        }

        public ThumbnailToolbarButton[] ThumbnailButtons
        {
            get => _thumbnailButtons;
            set
            {
                _thumbnailButtons = value;
                // Set the window handle on the buttons (for future updates)
                Array.ForEach(_thumbnailButtons, UpdateHandle);
            }
        }

        /// <summary>
        ///     Release the native objects.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void UpdateHandle(ThumbnailToolbarButton button)
        {
            button.WindowHandle = _thumbnailToolbarProxyWindow.WindowHandle;
            button.AddedToTaskbar = false;
        }

        ~TaskbarWindow()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            _thumbnailToolbarProxyWindow?.Dispose();
            _thumbnailToolbarProxyWindow = null;

            // Don't dispose the thumbnail buttons
            // as they might be used in another window.
            // Setting them to null will indicate we don't need use anymore.
            ThumbnailButtons = null;
        }
    }
}