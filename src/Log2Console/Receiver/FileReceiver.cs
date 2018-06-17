using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Log2Console.Log;

namespace Log2Console.Receiver
{
    /// <summary>
    ///     This receiver watch a given file, like a 'tail' program, with one log event by line.
    ///     Ideally the log events should use the log4j XML Schema layout.
    /// </summary>
    [Serializable]
    [DisplayName("Log File (Flat or Log4j XML Formatted)")]
    public class FileReceiver : BaseReceiver
    {
        public enum FileFormatEnums
        {
            Log4jXml,
            Flat
        }

        private FileFormatEnums _fileFormat;

        [NonSerialized] private string _filename;

        [NonSerialized] private StreamReader _fileReader;

        private string _fileToWatch = string.Empty;


        [NonSerialized] private FileSystemWatcher _fileWatcher;

        [NonSerialized] private string _fullLoggerName;

        [NonSerialized] private long _lastFileLength;

        private string _loggerName;
        private bool _showFromBeginning;


        [Category("Configuration")]
        [DisplayName("File to Watch")]
        public string FileToWatch
        {
            get => _fileToWatch;
            set
            {
                if (string.Compare(_fileToWatch, value, StringComparison.OrdinalIgnoreCase) == 0)
                    return;

                _fileToWatch = value;

                Restart();
            }
        }

        [Category("Configuration")]
        [DisplayName("File Format (Flat or Log4j XML)")]
        public FileFormatEnums FileFormat
        {
            get => _fileFormat;
            set => _fileFormat = value;
        }

        [Category("Configuration")]
        [DisplayName("Show from Beginning")]
        [Description("Show file contents from the beginning (not just newly appended lines)")]
        [DefaultValue(false)]
        public bool ShowFromBeginning
        {
            get => _showFromBeginning;
            set
            {
                _showFromBeginning = value;

                if (value && _lastFileLength == 0) ReadFile();
            }
        }

        [Category("Behavior")]
        [DisplayName("Logger Name")]
        [Description("Append the given Name to the Logger Name. If left empty, the filename will be used.")]
        public string LoggerName
        {
            get => _loggerName;
            set
            {
                _loggerName = value;

                ComputeFullLoggerName();
            }
        }


        [Browsable(false)]
        public override string SampleClientConfig => "Configuration for log4net:" + Environment.NewLine +
                                                     "<appender name=\"FileAppender\" type=\"log4net.Appender.FileAppender\">" +
                                                     Environment.NewLine +
                                                     "    <file value=\"log-file.txt\" />" + Environment.NewLine +
                                                     "    <appendToFile value=\"true\" />" + Environment.NewLine +
                                                     "    <lockingModel type=\"log4net.Appender.FileAppender+MinimalLock\" />" +
                                                     Environment.NewLine +
                                                     "    <layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" +
                                                     Environment.NewLine +
                                                     "</appender>";


        private void Restart()
        {
            Terminate();
            Initialize();
        }

        private void ComputeFullLoggerName()
        {
            _fullLoggerName =
                $"FileLogger.{(string.IsNullOrEmpty(_loggerName) ? _filename.Replace('.', '_') : _loggerName)}";

            DisplayName = string.IsNullOrEmpty(_loggerName)
                ? string.Empty
                : $"Log File [{_loggerName}]";
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            ReadFile();
        }

        private void ReadFile()
        {
            if (_fileReader == null || _fileReader.BaseStream.Length == _lastFileLength)
                return;

            // Seek to the last file length
            _fileReader.BaseStream.Seek(_lastFileLength, SeekOrigin.Begin);

            // Get last added lines
            string line;
            var sb = new StringBuilder();
            var logMsgs = new List<LogMessage>();

            while ((line = _fileReader.ReadLine()) != null)
                if (_fileFormat == FileFormatEnums.Flat)
                {
                    var logMsg = new LogMessage
                    {
                        RootLoggerName = _loggerName,
                        LoggerName = _fullLoggerName,
                        ThreadName = "NA",
                        Message = line,
                        TimeStamp = DateTime.Now,
                        Level = LogLevels.Instance[LogLevel.Info]
                    };

                    logMsgs.Add(logMsg);
                }
                else
                {
                    sb.Append(line);

                    // This condition allows us to process events that spread over multiple lines
                    if (line.Contains("</log4j:event>"))
                    {
                        var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(sb.ToString(), _fullLoggerName);
                        logMsgs.Add(logMsg);
                        sb = new StringBuilder();
                    }
                }

            // Notify the UI with the set of messages
            Notifiable.Notify(logMsgs.ToArray());

            // Update the last file length
            _lastFileLength = _fileReader.BaseStream.Position;
        }

        public override void Initialize()
        {
            if (string.IsNullOrEmpty(_fileToWatch))
                return;

            _fileReader =
                new StreamReader(new FileStream(_fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            _lastFileLength = _showFromBeginning ? 0 : _fileReader.BaseStream.Length;

            var path = Path.GetDirectoryName(_fileToWatch);
            _filename = Path.GetFileName(_fileToWatch);
            _fileWatcher =
                new FileSystemWatcher(path ?? throw new InvalidOperationException(), _filename)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.EnableRaisingEvents = true;

            ComputeFullLoggerName();
        }

        public override void Terminate()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Changed -= OnFileChanged;
                _fileWatcher = null;
            }

            _fileReader?.Close();
            _fileReader = null;

            _lastFileLength = 0;
        }

        public override void Attach(ILogMessageNotifiable notifiable)
        {
            base.Attach(notifiable);

            if (_showFromBeginning)
                ReadFile();
        }
    }
}