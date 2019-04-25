using System;
using System.Diagnostics;
using NLog;

namespace TestNLog
{
    internal class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            var key = Console.ReadKey();
            while (key.Key != ConsoleKey.X)
            {
                DoLog();
                DoWinDebug();
                key = Console.ReadKey();
            }
        }

        private static void DoWinDebug()
        {
            Console.WriteLine("Doing WinDebug!");

            Debug.WriteLine("This is a call to System.Diagnostics.Debug");
            Trace.WriteLine("This is a call to System.Diagnostics.Trace");
        }

        private static void DoLog()
        {
            Console.WriteLine("Doing Log!");

            if (_log.IsErrorEnabled)
            {
                _log.Error("This is an Error...");
            }

            if (_log.IsDebugEnabled)
            {
                for (var i = 0; i < 10; i++)
                {
                    _log.Debug("This is a simple log!");
                }
            }

            if (_log.IsErrorEnabled)
            {
                _log.Error("This is an Error...");
            }

            if (_log.IsInfoEnabled)
            {
                _log.Info("This is an Info...");
            }

            _log.Warn("This is a Warning...");
            _log.Fatal("This is a Fatal...");

            _log.Error(new Exception("The message exception here."), "This is an error with an exception.");

            _log.Warn("This is a message on many lines...\nlines...\nlines...\nlines...");
            _log.Warn("This is a message on many lines...\r\nlines...\r\nlines...\r\nlines...");

            var dm = new DummyManager();
            dm.DoIt();

            var dt = new DummyTester();
            dt.DoIt();
        }
    }
}