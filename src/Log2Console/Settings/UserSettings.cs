using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Log2Console.Log;
using Log2Console.Receiver;
using Newtonsoft.Json;

namespace Log2Console.Settings
{
    public sealed class UserSettings
    {
        private const string SettingsFileName = "UserSettings.json";

        private static readonly Lazy<UserSettings> instance = new Lazy<UserSettings>(() => new UserSettings());
        private string _timeStampFormatString = "yyyy-MM-dd HH:mm:ss.ffff";
        private uint _transparency = 100;

        private UserSettings()
        {
            var settingsFilePath = GetSettingsFilePath();
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    var serializerSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto
                    };
                    // deserialize JSON directly from a file
                    using (var file = File.OpenText(settingsFilePath))
                    {
                        JsonConvert.PopulateObject(file.ReadToEnd(), this, serializerSettings);
                    }

                    FirstStartup = false;
                }
                catch (Exception)
                {
                    // The settings file might be corrupted or from too different version, delete it...
                    try
                    {
                        File.Delete(settingsFilePath);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (FirstStartup)
            {
                MessageDetailConfiguration = Defaults.DefaultDetailsMessageConfiguration;
                ColumnConfiguration = Defaults.DefaultColumnConfiguration;
                CsvHeaderColumns = Defaults.DefaultCsvColumnHeaderConfiguration;
            }
        }

        internal bool FirstStartup { get; } = true;

        public static UserSettings Instance => instance.Value;

        [Category("Appearance")]
        [Description("Hides the taskbar icon, only the tray icon will remain visible.")]
        [DisplayName("Hide Taskbar Icon")]
        public bool HideTaskbarIcon { get; set; }

        [Category("Appearance")]
        [Description("The Log2Console window will remain on top of all other windows.")]
        [DisplayName("Always On Top")]
        public bool AlwaysOnTop { get; set; }

        [Category("Appearance")]
        [Description("Select a transparency factor for the main window.")]
        public uint Transparency
        {
            get => _transparency;
            set => _transparency = Math.Max(10, Math.Min(100, value));
        }

        [Category("Appearance")]
        [Description("Highlight the Logger of the selected Log Message.")]
        [DisplayName("Highlight Logger")]
        public bool HighlightLogger { get; set; } = true;

        [Category("Appearance")]
        [Description("Highlight the Log Messages of the selected Logger.")]
        [DisplayName("Highlight Log Messages")]
        public bool HighlightLogMessages { get; set; } = true;

        [Category("Columns")]
        [DisplayName("Column Settings")]
        [Description("Configure which Columns to Display")]
        public List<FieldType> ColumnConfiguration { get; set; }

        [Category("Columns")]
        [DisplayName("CSV File Header Column Settings")]
        [Description(
            "Configures which columns maps to which fields when auto detecting the CSV structure based on the header")]
        public List<FieldType> CsvHeaderColumns { get; set; }

        [Category("Source File Configuration")]
        [DisplayName("Source Location")]
        [Description("Map the Log File Location to the Local Source Code Location")]
        public List<SourceFileLocation> SourceLocationMapConfiguration { get; set; }

        [Category("Notification")]
        [Description("A balloon tip will be displayed when a new log message arrives and the window is hidden.")]
        [DisplayName("Notify New Log When Hidden")]
        public bool NotifyNewLogWhenHidden { get; set; } = true;

        [Category("Notification")]
        [Description("Automatically scroll to the last log message.")]
        [DisplayName("Auto Scroll to Last Log")]
        public bool AutoScrollToLastLog { get; set; } = true;


        [Category("Logging")]
        [Description("Groups the log messages based on the Logger Name.")]
        [DisplayName("Group Log Messages by Loggers")]
        public bool GroupLogMessages { get; set; }

        [Category("Logging")]
        [Description("When greater than 0, the log messages are limited to that number.")]
        [DisplayName("Message Cycle Count")]
        public int MessageCycleCount { get; set; }

        [Category("Logging")]
        [Description(
            "Defines the format to be used to display the log message timestamps (cf. DateTime.ToString(format) in the .NET Framework.")]
        [DisplayName("TimeStamp Format String")]
        public string TimeStampFormatString
        {
            get => _timeStampFormatString;
            set
            {
                // Check validity
                try
                {
                    var dummy = DateTime.Now.ToString(value); // If error, will throw FormatException
                    _timeStampFormatString = value;
                }
                catch (FormatException ex)
                {
                    MessageBox.Show(Form.ActiveForm, ex.Message, Form.ActiveForm?.Text, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    _timeStampFormatString = "G"; // Back to default
                }
            }
        }

        [Category("Logging")]
        [Description("When a logger is enabled or disabled, do the same for all child loggers.")]
        [DisplayName("Recursively Enable Loggers")]
        public bool RecursivelyEnableLoggers { get; set; } = true;

        [Category("Message Details")]
        [DisplayName("Details information")]
        [Description("Configure which information to Display in the message details")]
        public List<FieldType> MessageDetailConfiguration { get; set; }

        [Category("Message Details")]
        [Description("Show or hide the message properties in the message details panel.")]
        [DisplayName("Show Properties")]
        public bool ShowMsgDetailsProperties { get; set; }

        [Category("Message Details")]
        [Description("Show or hide the exception in the message details panel.")]
        [DisplayName("Show Exception")]
        public bool ShowMsgDetailsException { get; set; } = true;

        [Category("Message Details")]
        [Description("Use the Rich Text Format")]
        [DisplayName("Use Rtf")]
        public bool UseMsgDetailsRtf { get; set; } = true;

        [Category("Fonts")]
        [Description("Set the default Font.")]
        [DisplayName("Default Font")]
        public Font DefaultFont { get; set; }

        [Category("Fonts")]
        [Description("Set the Font of the Log List View.")]
        [DisplayName("Log List View Font")]
        public Font LogListFont { get; set; }

        [Category("Fonts")]
        [Description("Set the Font of the Log Detail View.")]
        [DisplayName("Log Detail View Font")]
        public Font LogDetailFont { get; set; }

        [Category("Fonts")]
        [Description("Set the Font of the Logger Tree.")]
        [DisplayName("Logger Tree Font")]
        public Font LoggerTreeFont { get; set; }

        [Category("Colors")]
        [Description("Set the Background Color of the Log List View.")]
        [DisplayName("Log List View Background Color")]
        public Color LogListBackColor { get; set; } = Color.Empty;

        [Category("Colors")]
        [Description("Set the Background Color of the Log Message details.")]
        [DisplayName("Log Message details Background Color")]
        public Color LogMessageBackColor { get; set; } = Color.Empty;


        [Category("Log Level Colors")]
        [DisplayName("1 - Trace Level Color")]
        public Color TraceLevelColor { get; set; } = Defaults.DefaultTraceLevelColor;

        [Category("Log Level Colors")]
        [DisplayName("2 - Debug Level Color")]
        public Color DebugLevelColor { get; set; } = Defaults.DefaultDebugLevelColor;

        [Category("Log Level Colors")]
        [DisplayName("3 - Info Level Color")]
        public Color InfoLevelColor { get; set; } = Defaults.DefaultInfoLevelColor;

        [Category("Log Level Colors")]
        [DisplayName("4 - Warning Level Color")]
        public Color WarnLevelColor { get; set; } = Defaults.DefaultWarnLevelColor;

        [Category("Log Level Colors")]
        [DisplayName("5 - Error Level Color")]
        public Color ErrorLevelColor { get; set; } = Defaults.DefaultErrorLevelColor;

        [Category("Log Level Colors")]
        [DisplayName("6 - Fatal Level Color")]
        public Color FatalLevelColor { get; set; } = Defaults.DefaultFatalLevelColor;

        /// <summary>
        ///     This setting is not available through the Settings PropertyGrid.
        /// </summary>
        [Browsable(false)]
        internal LogLevelInfo LogLevelInfo { get; set; } = LogLevels.Instance[LogLevel.Trace];

        /// <summary>
        ///     This setting is not available through the Settings PropertyGrid.
        /// </summary>
        [JsonProperty]
        internal List<IReceiver> Receivers { get; set; } = new List<IReceiver>();

        /// <summary>
        ///     This setting is not available through the Settings PropertyGrid.
        /// </summary>
        [JsonProperty]
        internal LayoutSettings Layout { get; set; } = new LayoutSettings();

        private static string GetSettingsFilePath()
        {
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var di = new DirectoryInfo(userDir);
            di = di.CreateSubdirectory("Log2Console");

            return di.FullName + Path.DirectorySeparatorChar + SettingsFileName;
        }

        public void Save()
        {
            var settingsFilePath = GetSettingsFilePath();
            using (var file = File.CreateText(settingsFilePath))
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto
                };
                serializer.Serialize(file, this);
            }
        }

        public void Close()
        {
            foreach (var receiver in Receivers)
            {
                receiver.Detach();
                receiver.Terminate();
            }

            Receivers.Clear();
        }
    }
}