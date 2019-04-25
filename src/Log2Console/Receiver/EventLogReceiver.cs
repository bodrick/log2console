using System;
using System.ComponentModel;
using System.Diagnostics;
using Log2Console.Log;

namespace Log2Console.Receiver
{
    [DisplayName("Windows Event Log")]
    public class EventLogReceiver : BaseReceiver
    {
        private string _baseLoggerName;

        private EventLog _eventLog;

        [Category("Configuration")]
        [DisplayName("Event Log Name")]
        [Description("The name of the log on the specified computer.")]
        public string LogName { get; set; }

        [Category("Configuration")]
        [DisplayName("Machine Name")]
        [Description("The computer on which the log exists.")]
        public string MachineName { get; set; } = ".";

        [Category("Configuration")]
        [DisplayName("Event Log Source")]
        [Description("The source of event log entries.")]
        public string Source { get; set; }

        [Category("Behavior")]
        [DisplayName("Append Machine Name to Logger")]
        [Description("Append the remote Machine Name to the Logger Name.")]
        public bool AppendHostNameToLogger { get; set; } = true;


        private void EventLogOnEntryWritten(object sender, EntryWrittenEventArgs entryWrittenEventArgs)
        {
            var logMsg = new LogMessage
            {
                RootLoggerName = _baseLoggerName,
                LoggerName = string.IsNullOrEmpty(entryWrittenEventArgs.Entry.Source)
                    ? _baseLoggerName
                    : $"{_baseLoggerName}.{entryWrittenEventArgs.Entry.Source}",
                Message = entryWrittenEventArgs.Entry.Message,
                TimeStamp = entryWrittenEventArgs.Entry.TimeGenerated,
                Level = LogUtils.GetLogLevelInfo(GetLogLevel(entryWrittenEventArgs.Entry.EntryType)),
                ThreadName = entryWrittenEventArgs.Entry.InstanceId.ToString()
            };


            if (!string.IsNullOrEmpty(entryWrittenEventArgs.Entry.Category))
            {
                logMsg.Properties.Add("Category", entryWrittenEventArgs.Entry.Category);
            }

            if (!string.IsNullOrEmpty(entryWrittenEventArgs.Entry.UserName))
            {
                logMsg.Properties.Add("User Name", entryWrittenEventArgs.Entry.UserName);
            }

            Notifiable.Notify(logMsg);
        }

        private static LogLevel GetLogLevel(EventLogEntryType entryType)
        {
            switch (entryType)
            {
                case EventLogEntryType.Warning:
                    return LogLevel.Warn;
                case EventLogEntryType.FailureAudit:
                case EventLogEntryType.Error:
                    return LogLevel.Error;
                case EventLogEntryType.SuccessAudit:
                case EventLogEntryType.Information:
                    return LogLevel.Info;
                default:
                    return LogLevel.None;
            }
        }


        #region Overrides of BaseReceiver

        [Browsable(false)]
        public override string SampleClientConfig =>
            "Use Log2Console to display the Windows Event Logs." + Environment.NewLine +
            "Note that the Thread column is used to display the Instance ID (Event ID).";

        public override void Initialize()
        {
            if (string.IsNullOrEmpty(MachineName))
            {
                MachineName = ".";
            }

            _eventLog = new EventLog(LogName, MachineName, Source);
            _eventLog.EntryWritten += EventLogOnEntryWritten;
            _eventLog.EnableRaisingEvents = true;

            _baseLoggerName = AppendHostNameToLogger && !string.IsNullOrEmpty(MachineName) && MachineName != "."
                ? $"[Host: {MachineName}].{LogName}"
                : LogName;
        }

        public override void Terminate()
        {
            _eventLog?.Dispose();
            _eventLog = null;
        }

        #endregion
    }
}