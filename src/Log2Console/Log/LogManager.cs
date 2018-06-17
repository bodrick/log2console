using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Log2Console.Log
{
    public class LogManager : ILogManager
    {
        private static LogManager _instance;
        private Dictionary<string, LoggerItem> _fullPathLoggers;


        private LogManager()
        {
        }

        internal static ILogManager Instance => _instance ?? (_instance = new LogManager());

        internal LoggerItem RootLoggerItem { get; set; }

        public void Initialize(ILoggerView loggerView, ListView logListView)
        {
            // Root Logger
            RootLoggerItem = LoggerItem.CreateRootLoggerItem("Root", loggerView, logListView);

            // Quick Access Logger Collection
            _fullPathLoggers = new Dictionary<string, LoggerItem>();
        }

        public void ClearAll()
        {
            ClearLogMessages();

            RootLoggerItem.ClearAll();
            _fullPathLoggers.Clear();
        }

        public void ClearLogMessages()
        {
            RootLoggerItem.ClearAllLogMessages();
        }

        public void DeactivateLogger()
        {
            RootLoggerItem.Enabled = false;
        }

        /// <exception cref="NullReferenceException">No Logger for this Log Message.</exception>
        public void ProcessLogMessage(LogMessage logMsg)
        {
            // Check 1st in the global LoggerPath/Logger dictionary
            logMsg.CheckNull();

            if (!_fullPathLoggers.TryGetValue(logMsg.LoggerName, out var logger))
                logger = RootLoggerItem.GetOrCreateLogger(logMsg.LoggerName);
            if (logger == null)
                throw new NullReferenceException("No Logger for this Log Message.");

            logger.AddLogMessage(logMsg);
        }


        public void SearchText(string str)
        {
            RootLoggerItem.SearchText(str);
        }


        public void UpdateLogLevel()
        {
            RootLoggerItem?.UpdateLogLevel();
        }

        public void SetRootLoggerName(string name)
        {
            RootLoggerItem.Name = name;
        }
    }
}