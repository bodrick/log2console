using System.Linq;

namespace Log2Console.Log
{
    public static class LogUtils
    {
        public static LogLevelInfo GetLogLevelInfo(int level)
        {
            var info = LogLevels.Instance.LogLevelInfos.SingleOrDefault(f =>
                level >= f.RangeMin && level <= f.RangeMax);
            return info ?? LogLevels.Instance.InvalidLogLevel;
        }

        public static LogLevelInfo GetLogLevelInfo(LogLevel level)
        {
            var info = LogLevels.Instance.LogLevelInfos.SingleOrDefault(f => f.Level == level);
            return info ?? LogLevels.Instance.InvalidLogLevel;
        }
    }
}