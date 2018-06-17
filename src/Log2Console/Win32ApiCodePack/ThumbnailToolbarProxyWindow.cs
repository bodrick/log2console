using System;
using System.Windows.Forms;

namespace Log2Console.Win32ApiCodePack
{
    internal class ThumbnailToolbarProxyWindow : NativeWindow, IDisposable
    {
        public readonly IntPtr WindowHandle;
        private ThumbnailToolbarButton[] _thumbnailButtons;

        public TaskbarWindow TaskbarWindow;

        public ThumbnailToolbarProxyWindow(IntPtr windowHandle, ThumbnailToolbarButton[] buttons)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentException("Window handle cannot be empty", nameof(windowHandle));

            if (buttons == null || buttons.Length == 0)
                throw new ArgumentException("Null or empty arrays are not allowed.", nameof(buttons));

            WindowHandle = windowHandle;
            _thumbnailButtons = buttons;

            // Set the window handle on the buttons (for future updates)
            Array.ForEach(_thumbnailButtons, UpdateHandle);

            // Assign the window handle (coming from the user) to this native window
            // so we can intercept the window messages sent from the taskbar to this window.
            AssignHandle(windowHandle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void UpdateHandle(ThumbnailToolbarButton button)
        {
            button.WindowHandle = WindowHandle;
            button.AddedToTaskbar = false;
        }

        protected override void WndProc(ref Message m)
        {
            var handled = TaskbarWindowManager.Instance.DispatchMessage(ref m, TaskbarWindow);

            // If it's a WM_Destroy message, then also forward it to the base class (our native window)
            if (m.Msg == (int) TabbedThumbnailNativeMethods.WM_DESTROY ||
                m.Msg == (int) TabbedThumbnailNativeMethods.WM_NCDESTROY ||
                m.Msg == (int) TabbedThumbnailNativeMethods.WM_SYSCOMMAND &&
                (int) m.WParam == TabbedThumbnailNativeMethods.SC_CLOSE)
                base.WndProc(ref m);
            else if (!handled)
                base.WndProc(ref m);
        }

        ~ThumbnailToolbarProxyWindow()
        {
            Dispose(false);
        }

        public void Dispose(bool disposing)
        {
            if (disposing) _thumbnailButtons = null;
        }
    }
}