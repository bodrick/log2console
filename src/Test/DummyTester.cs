using System;
using System.Reflection;
using log4net;

namespace Test
{
    public class DummyTester
    {
        public static readonly ILog _log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        public DummyTester()
        {
            if (_log.IsInfoEnabled)
                _log.Info("Dummy Tester ctor");
        }

        public void DoIt()
        {
            if (_log.IsDebugEnabled)
                _log.Debug("DT: Do It, Do It Now!!");

            _log.Warn("This is a Warning from DT...");
            _log.Error("This is an Error from DT...");
            _log.Fatal("This is a Fatal from DT...");

            _log.Error("This is an error from DT with an exception.", new Exception("The message exception here."));
        }
    }
}