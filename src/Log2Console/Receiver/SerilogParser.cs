using System;
using System.Diagnostics;
using System.Linq;
using Log2Console.Log;
using Newtonsoft.Json.Linq;

namespace Log2Console.Receiver
{
    internal static class SerilogParser
    {
        internal static LogMessage Parse(string logEvent, string defaultLogger)
        {
            LogMessage message;
            try
            {
                message = ParseEvent(logEvent);
            }
            catch (Exception ex)
            {
                message = new LogMessage
                {
                    LoggerName = defaultLogger,
                    RootLoggerName = defaultLogger,
                    ThreadName = "N/A",
                    Message = logEvent,
                    TimeStamp = DateTime.Now,
                    Level = LogLevels.Instance[LogLevel.Info],
                    ExceptionString = ex.Message
                };
            }

            return message;
        }

        internal static LogMessage ParseEvent(string logEvent)
        {
            var logJson = JObject.Parse(logEvent);
            var logMessage = new LogMessage();
            foreach (var child in logJson.Children().OfType<JProperty>())
            {
#if DEBUG
                Debug.WriteLine($"{child.Name}={child.Value}");
#endif
                switch (child.Name)
                {
                    case "timestamp":
                        logMessage.TimeStamp = DateTime.Parse(child.Value.ToString());
                        break;
                    case "level":
                        var levels = new[] {"Verbose", "Debug", "Information", "Warning", "Error", "Fatal"};
                        logMessage.Level = LogLevels.Instance[Array.IndexOf(levels, child.Value.ToString())];
                        break;
                    case "message":
                        logMessage.Message = child.Value.ToString();
                        break;
                    case "sourceContext":
                        logMessage.LoggerName = child.Value.ToString();
                        break;
                    default:
                        logMessage.Message += $"{Environment.NewLine}{child.Name}:{child.Value}";
                        logMessage.Properties[child.Name] = child.Value.ToString();
                        break;
                }
            }

            return logMessage;
        }
    }
}