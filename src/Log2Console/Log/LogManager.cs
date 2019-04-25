using System;
using System.Windows.Forms;

namespace Log2Console.Log
{
    public class LogManager : ILogManager
    {
        private static readonly Lazy<ILogManager> instance = new Lazy<ILogManager>(() => new LogManager());

        private LogManager()
        {
        }

        internal LoggerItem RootLoggerItem { get; set; }

        public static ILogManager Instance => instance.Value;

        public void Initialize(ILoggerView loggerView, ListView logListView) =>
            RootLoggerItem = LoggerItem.CreateRootLoggerItem("Root", loggerView, logListView);

        public void ClearAll()
        {
            ClearLogMessages();

            RootLoggerItem.ClearAll();
        }

        public void ClearLogMessages() => RootLoggerItem.ClearAllLogMessages();

        public void DeactivateLogger() => RootLoggerItem.Enabled = false;

        /// <exception cref="NullReferenceException">No Logger for this Log Message.</exception>
        public void ProcessLogMessage(LogMessage logMsg)
        {
            var logger = RootLoggerItem.GetOrCreateLogger(logMsg.LoggerName);
            if (logger == null)
            {
                throw new NullReferenceException("No Logger for this Log Message.");
            }

            logger.AddLogMessage(logMsg);
        }

        public void SearchText(string str) => RootLoggerItem.SearchText(str);

        public void UpdateLogLevel() => RootLoggerItem?.UpdateLogLevel();

        public void SetRootLoggerName(string name) => RootLoggerItem.Name = name;
    }
}