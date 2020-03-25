using System;
using System.Collections.Generic;
using System.Text;
using Log2Console.Settings;

namespace Log2Console.Log
{
    public class LogMessage
    {
        /// <summary>
        ///     The CallSite Class
        /// </summary>
        public string CallSiteClass { get; set; } = string.Empty;

        /// <summary>
        ///     The CallSite Method in which the Log is made
        /// </summary>
        public string CallSiteMethod { get; set; } = string.Empty;

        /// <summary>
        ///     An exception message to associate to this message.
        /// </summary>
        public string ExceptionString { get; set; } = string.Empty;

        /// <summary>
        ///     Log Level.
        /// </summary>
        public LogLevelInfo Level { get; set; } = LogLevels.Instance[LogLevel.Error];

        /// <summary>
        ///     Logger Name.
        /// </summary>
        public string LoggerName { get; set; } = "Unknown";

        /// <summary>
        ///     Log Message.
        /// </summary>
        public string Message { get; set; } = "Unknown";

        /// <summary>
        ///     Properties collection.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Root Logger Name.
        /// </summary>
        public string RootLoggerName { get; set; } = "Unknown";

        /// <summary>
        ///     The Line Number of the Log Message
        /// </summary>
        public ulong SequenceNr { get; set; }

        /// <summary>
        ///     The Line of the Source File
        /// </summary>
        public uint SourceFileLineNr { get; set; }

        /// <summary>
        ///     The Name of the Source File
        /// </summary>
        public string SourceFileName { get; set; } = string.Empty;

        /// <summary>
        ///     Thread Name.
        /// </summary>
        public string ThreadName { get; set; } = string.Empty;

        /// <summary>
        ///     Time Stamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var fieldType in UserSettings.Instance.ColumnConfiguration)
            {
                sb.Append(GetInformation(fieldType));
                sb.Append("\t");
            }

            return sb.ToString();
        }

        private string GetInformation(FieldType fieldType)
        {
            string result;
            switch (fieldType.Field)
            {
                case LogMessageField.SequenceNr:
                    result = SequenceNr.ToString();
                    break;
                case LogMessageField.LoggerName:
                    result = LoggerName;
                    break;
                case LogMessageField.RootLoggerName:
                    result = RootLoggerName;
                    break;
                case LogMessageField.Level:
                    result = Level.Level.ToString();
                    break;
                case LogMessageField.Message:
                    result = Message;
                    break;
                case LogMessageField.ThreadName:
                    result = ThreadName;
                    break;
                case LogMessageField.TimeStamp:
                    result = TimeStamp.ToString(UserSettings.Instance.TimeStampFormatString);
                    break;
                case LogMessageField.Exception:
                    result = ExceptionString;
                    break;
                case LogMessageField.CallSiteClass:
                    result = CallSiteClass;
                    break;
                case LogMessageField.CallSiteMethod:
                    result = CallSiteMethod;
                    break;
                case LogMessageField.SourceFileName:
                    result = SourceFileName;
                    break;
                case LogMessageField.SourceFileLineNr:
                    result = SourceFileLineNr.ToString();
                    break;
                case LogMessageField.Properties:
                    result = Properties.ContainsKey(fieldType.Property) ? Properties[fieldType.Property] : "";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        public string GetMessageDetails()
        {
            var sb = new StringBuilder();

            if (UserSettings.Instance.UseMsgDetailsRtf)
            {
                sb.Append(@"{\rtf1\ansi ");
                foreach (var fieldType in UserSettings.Instance.MessageDetailConfiguration)
                {
                    var info = GetInformation(fieldType).
                        Replace(@"\", @"\\").
                        Replace("{", @"\{").
                        Replace("}", @"\}").
                        Replace("\r\n", @" \line ").Replace("\n", @" \line ");

                    if ((fieldType.Field == LogMessageField.Properties && !string.IsNullOrWhiteSpace(info)) || fieldType.Field != LogMessageField.Properties)
                    {
                        sb.Append(@"\b " + fieldType.Name + @": \b0 ");
                        if (info.Length > 40)
                        {
                            sb.Append(@" \line ");
                        }

                        sb.Append(info + @" \line ");
                    }
                }

                sb.Append(@"}");
            }
            else
            {
                foreach (var fieldType in UserSettings.Instance.MessageDetailConfiguration)
                {
                    var info = GetInformation(fieldType);
                    sb.Append(fieldType.Field + ": ");
                    if (info.Length > 40)
                    {
                        sb.AppendLine();
                    }

                    sb.AppendLine(info);
                }
            }

            return sb.ToString();
        }
    }
}
