namespace Log2Console.Log
{
    public static class LogUtils
    {
        public static bool IsInRange(int val, int min, int max)
        {
            return val >= min && val <= max;
        }

        public static LogLevelInfo GetLogLevelInfo(int level)
        {
            foreach (var info in LogLevels.Instance.LogLevelInfos)
                if (IsInRange(level, info.RangeMin, info.RangeMax))
                    return info;

            return LogLevels.Instance.InvalidLogLevel;
        }

        public static LogLevelInfo GetLogLevelInfo(LogLevel level)
        {
            foreach (var info in LogLevels.Instance.LogLevelInfos)
                if (level == info.Level)
                    return info;

            return LogLevels.Instance.InvalidLogLevel;
        }
    }
}