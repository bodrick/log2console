using System;
using System.IO;
using System.Windows.Forms;

namespace Log2Console
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var mainForm = new MainForm();
                if (args.Length == 1)
                {
                    //First and only argument should be a log file
                    var logFile = Path.GetFullPath(args[0]);
                    mainForm.LogFile = logFile;
                }

                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.ToString());
#else
                MessageBox.Show(ex.Message);
#endif
            }
        }
    }
}