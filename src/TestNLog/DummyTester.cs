using System;
using NLog;

namespace TestNLog
{
    public class DummyTester
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public DummyTester()
        {
            if (Log.IsInfoEnabled)
                Log.Info("Dummy Tester ctor");
        }

        public void DoIt()
        {
            if (Log.IsDebugEnabled)
                Log.Debug("DT: Do It, Do It Now!!");

            Log.Warn("This is a Warning from DT...");
            Log.Error("This is an Error from DT...");
            Log.Fatal("This is a Fatal from DT...");

            Log.Error(new Exception("The message exception here."), "This is an error from DT with an exception.");
        }
    }
}