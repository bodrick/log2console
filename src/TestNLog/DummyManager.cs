using System;
using NLog;

namespace TestNLog
{
    public class DummyManager
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();


        public DummyManager()
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info("Dummy Manager ctor");
            }
        }

        public void DoIt()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("DM: Do It, Do It Now!!");
            }

            Log.Warn("This is a Warning from DM...");
            Log.Error("This is an Error from DM...");
            Log.Fatal("This is a Fatal from DM...");

            Log.Error(new Exception("The message exception here."), "This is an error from DM with an exception.");
        }
    }
}