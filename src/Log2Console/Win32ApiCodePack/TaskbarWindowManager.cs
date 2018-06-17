using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Log2Console.Win32ApiCodePack
{
    internal class TaskbarWindowManager
    {
        private static TaskbarWindowManager _instance;

        private readonly List<TaskbarWindow> _taskbarWindowList = new List<TaskbarWindow>();
        private bool _buttonsAdded;

        public static TaskbarWindowManager Instance => _instance ?? (_instance = new TaskbarWindowManager());

        public void AddThumbnailButtons(IntPtr windowHandle, ThumbnailToolbarButton[] buttons)
        {
            // Try to get an existing taskbar window for this user windowhandle,
            // or get one that is created for us
            var taskbarWindow = GetTaskbarWindow(windowHandle);

            if (taskbarWindow == null)
            {
                taskbarWindow = new TaskbarWindow(windowHandle, buttons);
                _taskbarWindowList.Add(taskbarWindow);
            }
            else if (taskbarWindow.ThumbnailButtons == null)
            {
                taskbarWindow.ThumbnailButtons = buttons;
            }
            else
            {
                // We already have buttons assigned
                throw new InvalidOperationException(
                    "Toolbar buttons for this window are already added. Once added, neither the order of the buttons cannot be changed, nor can they be added or removed.");
            }
        }

        private TaskbarWindow GetTaskbarWindow(IntPtr userWindowHandle)
        {
            if (userWindowHandle == IntPtr.Zero)
                throw new ArgumentException("userWindowHandle");

            return _taskbarWindowList.Find(w => w.WindowHandle == userWindowHandle);
        }

        /// <summary>
        ///     Dispatches a window message so that the appropriate events
        ///     can be invoked. This is used for the Taskbar's thumbnail toolbar feature.
        /// </summary>
        /// <param name="m">
        ///     The window message, typically obtained
        ///     from a Windows Forms or WPF window procedure.
        /// </param>
        /// <param name="taskbarWindow">Taskbar window for which we are intercepting the messages</param>
        /// <returns>Returns true if this method handles the window message</returns>
        internal bool DispatchMessage(ref Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int) TaskbarNativeMethods.WM_TASKBARBUTTONCREATED)
            {
                AddButtons(taskbarWindow);
            }
            else
            {
                if (!_buttonsAdded)
                    AddButtons(taskbarWindow);

                switch (m.Msg)
                {
                    case TaskbarNativeMethods.WM_COMMAND:
                        if (CoreNativeMethods.HIWORD(m.WParam.ToInt64(), 16) == THUMBBUTTON.THBN_CLICKED)
                        {
                            var buttonId = CoreNativeMethods.LOWORD(m.WParam.ToInt64());

                            foreach (var button in taskbarWindow.ThumbnailButtons)
                                if (button.Id == buttonId)
                                    button.FireClick(taskbarWindow);
                        }

                        break;
                } // End switch
            } // End else

            return false;
        }

        private void AddButtons(TaskbarWindow taskbarWindow)
        {
            // Add the buttons
            // Get the array of thumbnail buttons in native format
            var nativeButtons = Array.ConvertAll(taskbarWindow.ThumbnailButtons, i => i.Win32ThumbButton);

            // Add the buttons on the taskbar
            var hr = TaskbarManager.Instance.TaskbarList.ThumbBarAddButtons(taskbarWindow.WindowHandle,
                (uint) taskbarWindow.ThumbnailButtons.Length, nativeButtons);

            if (!CoreErrorHelper.Succeeded((int) hr))
                Marshal.ThrowExceptionForHR((int) hr);

            // Set the window handle on the buttons (for future updates)
            _buttonsAdded = true;
            Array.ForEach(taskbarWindow.ThumbnailButtons, UpdateHandle);
        }

        private void UpdateHandle(ThumbnailToolbarButton button)
        {
            button.AddedToTaskbar = _buttonsAdded;
        }
    }
}