using System;
using System.Diagnostics;
using System.Drawing;

namespace Log2Console.Win32ApiCodePack
{
    public sealed class TaskbarManager
    {
        private static readonly object Lock = new object();
        private static volatile TaskbarManager _instance;

        private IntPtr _ownerHandle;

        // Internal implemenation of ITaskbarList4 interface
        private ITaskbarList4 _taskbarList;

        private ThumbnailToolbarManager _thumbnailToolbarManager;

        private TaskbarManager()
        {
        }

        /// <summary>
        ///     Indicates whether this feature is supported on the current platform.
        /// </summary>
        public static bool IsPlatformSupported => CoreHelpers.RunningOnWin7;

        /// <summary>
        ///     Gets the Thumbnail toolbar manager class for adding/updating
        ///     toolbar buttons.
        /// </summary>
        public ThumbnailToolbarManager ThumbnailToolbars
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();

                return _thumbnailToolbarManager ?? (_thumbnailToolbarManager = new ThumbnailToolbarManager());
            }
        }

        /// <summary>
        ///     Represents an instance of the Windows Taskbar
        /// </summary>
        public static TaskbarManager Instance
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();

                if (_instance == null)
                    lock (Lock)
                    {
                        if (_instance == null)
                            _instance = new TaskbarManager();
                    }

                return _instance;
            }
        }

        /// <summary>
        ///     Gets or sets the application user model id. Use this to explicitly
        ///     set the application id when generating custom jump lists
        /// </summary>
        public string ApplicationId
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();

                return GetCurrentProcessAppId();
            }
            set
            {
                CoreHelpers.ThrowIfNotWin7();

                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value), "Application Id cannot be an empty or null string.");

                SetCurrentProcessAppId(value);
            }
        }

        /// <summary>
        ///     Sets the handle of the window whose taskbar button will be used
        ///     to display progress.
        /// </summary>
        private IntPtr OwnerHandle
        {
            get
            {
                if (_ownerHandle == IntPtr.Zero)
                {
                    var currentProcess = Process.GetCurrentProcess();

                    if (currentProcess.MainWindowHandle != IntPtr.Zero)
                        _ownerHandle = currentProcess.MainWindowHandle;
                }

                return _ownerHandle;
            }
        }

        internal ITaskbarList4 TaskbarList
        {
            get
            {
                if (_taskbarList == null)
                    lock (Lock)
                    {
                        if (_taskbarList == null)
                        {
                            _taskbarList = (ITaskbarList4) new CTaskbarList();
                            _taskbarList.HrInit();
                        }
                    }

                return _taskbarList;
            }
        }

        private static void SetCurrentProcessAppId(string value)
        {
            TaskbarNativeMethods.SetCurrentProcessExplicitAppUserModelID(value);
        }

        private static string GetCurrentProcessAppId()
        {
            TaskbarNativeMethods.GetCurrentProcessExplicitAppUserModelID(out var appId);
            return appId;
        }


        public void SetProgressState(TaskbarProgressBarState state)
        {
            CoreHelpers.ThrowIfNotWin7();

            if (OwnerHandle != IntPtr.Zero)
                TaskbarList.SetProgressState(OwnerHandle, (TBPFLAG) state);
        }

        public void SetOverlayIcon(Icon icon, string accessibilityText)
        {
            CoreHelpers.ThrowIfNotWin7();

            if (OwnerHandle != IntPtr.Zero)
                TaskbarList.SetOverlayIcon(OwnerHandle, icon?.Handle ?? IntPtr.Zero, accessibilityText);
        }
    }
}