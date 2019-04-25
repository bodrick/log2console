using System.Drawing;

namespace Log2Console.Log
{
    public class LogLevelInfo
    {
        public LogLevelInfo(LogLevel level, Color color)
        {
            Level = level;
            Name = level.ToString();
            Color = color;
            RangeMax = RangeMin = 0;
        }

        public LogLevelInfo(LogLevel level, string name, Color color, int value, int rangeMin, int rangeMax)
        {
            Level = level;
            Name = name;
            Color = color;
            Value = value;
            RangeMin = rangeMin;
            RangeMax = rangeMax;
        }

        public int Value { get; }
        public Color Color { get; set; }
        public LogLevel Level { get; set; }
        public string Name { get; set; }
        public int RangeMax { get; set; }
        public int RangeMin { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is LogLevelInfo info)
            {
                return info == this;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(LogLevelInfo first, LogLevelInfo second)
        {
            if (first is null || second is null)
            {
                return first is null && second is null;
            }

            return first.Value == second.Value;
        }

        public static bool operator !=(LogLevelInfo first, LogLevelInfo second)
        {
            if (first is null || second is null)
            {
                return !(first is null && second is null);
            }

            return first.Value != second.Value;
        }
    }
}