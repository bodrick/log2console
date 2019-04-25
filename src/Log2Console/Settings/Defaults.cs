using System.Collections.Generic;
using System.Drawing;
using Log2Console.Log;

namespace Log2Console.Settings
{
    internal static class Defaults
    {
        internal static readonly Color DefaultTraceLevelColor = Color.Gray;
        internal static readonly Color DefaultDebugLevelColor = Color.Black;
        internal static readonly Color DefaultInfoLevelColor = Color.Green;
        internal static readonly Color DefaultWarnLevelColor = Color.Orange;
        internal static readonly Color DefaultErrorLevelColor = Color.Red;
        internal static readonly Color DefaultFatalLevelColor = Color.Purple;

        internal static readonly List<FieldType> DefaultColumnConfiguration = new List<FieldType>
        {
            new FieldType(LogMessageField.TimeStamp, "Time"),
            new FieldType(LogMessageField.Level, "Level"),
            new FieldType(LogMessageField.RootLoggerName, "RootLoggerName"),
            new FieldType(LogMessageField.ThreadName, "Thread"),
            new FieldType(LogMessageField.Message, "Message")
        };

        internal static readonly List<FieldType> DefaultDetailsMessageConfiguration = new List<FieldType>
        {
            new FieldType(LogMessageField.TimeStamp, "Time"),
            new FieldType(LogMessageField.Level, "Level"),
            new FieldType(LogMessageField.RootLoggerName, "RootLoggerName"),
            new FieldType(LogMessageField.ThreadName, "Thread"),
            new FieldType(LogMessageField.Message, "Message")
        };

        internal static readonly List<FieldType> DefaultCsvColumnHeaderConfiguration = new List<FieldType>
        {
            new FieldType(LogMessageField.SequenceNr, "sequence"),
            new FieldType(LogMessageField.TimeStamp, "time"),
            new FieldType(LogMessageField.Level, "level"),
            new FieldType(LogMessageField.ThreadName, "thread"),
            new FieldType(LogMessageField.CallSiteClass, "class"),
            new FieldType(LogMessageField.CallSiteMethod, "method"),
            new FieldType(LogMessageField.Message, "message"),
            new FieldType(LogMessageField.Exception, "exception"),
            new FieldType(LogMessageField.SourceFileName, "file")
        };
    }
}