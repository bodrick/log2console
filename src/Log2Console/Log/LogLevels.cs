using System;
using System.Drawing;
using Log2Console.Settings;

namespace Log2Console.Log
{
    public class LogLevels
    {
        private static readonly Lazy<LogLevels> instance = new Lazy<LogLevels>(() => new LogLevels());

        private LogLevels()
        {
            InvalidLogLevel = new LogLevelInfo(LogLevel.None, Color.IndianRed);

            LogLevelInfos = new[]
            {
                new LogLevelInfo(LogLevel.Trace, "Trace", Defaults.DefaultTraceLevelColor, 10000, 0, 10000),
                new LogLevelInfo(LogLevel.Debug, "Debug", Defaults.DefaultDebugLevelColor, 30000, 10001, 30000),
                new LogLevelInfo(LogLevel.Info, "Info", Defaults.DefaultInfoLevelColor, 40000, 30001, 40000),
                new LogLevelInfo(LogLevel.Warn, "Warn", Defaults.DefaultWarnLevelColor, 60000, 40001, 60000),
                new LogLevelInfo(LogLevel.Error, "Error", Defaults.DefaultErrorLevelColor, 70000, 60001, 70000),
                new LogLevelInfo(LogLevel.Fatal, "Fatal", Defaults.DefaultFatalLevelColor, 110000, 70001, 110000)
            };
        }

        public LogLevelInfo InvalidLogLevel { get; set; }
        public LogLevelInfo[] LogLevelInfos { get; set; }

        public static LogLevels Instance => instance.Value;

        internal LogLevelInfo this[int level]
        {
            get
            {
                if (level < (int)LogLevel.Trace || level > (int)LogLevel.Fatal)
                {
                    return InvalidLogLevel;
                }

                return LogLevelInfos[level];
            }
        }

        internal LogLevelInfo this[LogLevel logLevel]
        {
            get
            {
                var level = (int)logLevel;
                if (level < (int)LogLevel.Trace || level > (int)LogLevel.Fatal)
                {
                    return InvalidLogLevel;
                }

                return LogLevelInfos[level];
            }
        }

        internal LogLevelInfo this[string level]
        {
            get
            {
                foreach (var info in LogLevelInfos)
                {
                    if (info.Name.Equals(level, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return info;
                    }
                }

                return InvalidLogLevel;
            }
        }
    }
}