using System;
using System.Reflection;
using log4net;

namespace Test
{
    public class DummyManager
    {
        public static readonly ILog _log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        public DummyManager()
        {
            if (_log.IsInfoEnabled)
                _log.Info("Dummy Manager ctor");
        }

        public void DoIt()
        {
            if (_log.IsDebugEnabled)
                _log.Debug("DM: Do It, Do It Now!!");

            _log.Warn("This is a Warning from DM...");
            _log.Error("This is an Error from DM...");
            _log.Fatal("This is a Fatal from DM...");

            _log.Error("This is an error from DM with an exception.", new Exception("The message exception here."));
        }
    }
}