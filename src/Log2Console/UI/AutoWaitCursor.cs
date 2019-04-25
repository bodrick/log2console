using System;
using System.Windows.Forms;

namespace Log2Console.UI
{
    /// <summary>
    ///     Auto Wait Cursor utility class.
    ///     Usage:
    ///     using (new AutoWaitCursor())
    ///     { ...long task... }
    /// </summary>
    public class AutoWaitCursor : IDisposable
    {
        public AutoWaitCursor() => Enabled = true;

        public static bool Enabled
        {
            get => Application.UseWaitCursor;
            set
            {
                if (value == Application.UseWaitCursor) return;
                Application.UseWaitCursor = value;

                var f = Form.ActiveForm;
                if (f != null && f.Visible && f.Handle != IntPtr.Zero) // Send WM_SETCURSOR
                {
                    NativeMethods.SendMessage(f.Handle, 0x20, f.Handle, (IntPtr)1);
                }
            }
        }

        public void Dispose() => Enabled = false;
    }
}