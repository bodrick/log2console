using System.ComponentModel;

namespace Log2Console.Settings
{
    public class SourceFileLocation
    {
        [DisplayName("Log File Source Code Path")]
        [Description("The Base Path of the Source Code in the Log File")]
        [Category("Source Location Mapping")]
        public string LogSource { get; set; }

        [Description("The Base Path of the Source Code on the Local Computer")]
        [Category("Source Location Mapping")]
        [DisplayName("Local Source Code Path")]
        public string LocalSource { get; set; }
    }
}