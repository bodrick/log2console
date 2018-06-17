using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Log2Console.Log;

namespace Log2Console.Receiver
{
    [Serializable]
    [DisplayName("UDP (IP v4 and v6)")]
    public class UdpReceiver : BaseReceiver
    {
        public enum LogFormat
        {
            Log4J,
            Serilog
        }

        private string _address = string.Empty;
        private int _bufferSize = 10000;

        private bool _ipv6;
        private int _port = 7071;

        [NonSerialized] private IPEndPoint _remoteEndPoint;

        [NonSerialized] private UdpClient _udpClient;

        [NonSerialized] private Thread _worker;


        [Category("Configuration")]
        [DisplayName("UDP Port Number")]
        [DefaultValue(7071)]
        public int Port
        {
            get => _port;
            set => _port = value;
        }

        [Category("Configuration")]
        [DisplayName("Use IPv6 Addresses")]
        [DefaultValue(false)]
        public bool IpV6
        {
            get => _ipv6;
            set => _ipv6 = value;
        }

        [Category("Configuration")]
        [DisplayName("Multicast Group Address (Optional)")]
        public string Address
        {
            get => _address;
            set => _address = value;
        }

        [Category("Configuration")]
        [DisplayName("Receive Buffer Size")]
        public int BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value;
        }

        [Category("Configuration")]
        [DisplayName("Log Format")]
        public LogFormat LogFormatType { get; set; } = LogFormat.Log4J;

        [Category("Configuration")]
        [DisplayName("Condense Camel Case")]
        public bool CondenseCamelCase { get; set; } = true;


        [Browsable(false)]
        public override string SampleClientConfig => "Configuration for log4net:" + Environment.NewLine +
                                                     "<appender name=\"UdpAppender\" type=\"log4net.Appender.UdpAppender\">" +
                                                     Environment.NewLine +
                                                     "    <remoteAddress value=\"localhost\" />" + Environment.NewLine +
                                                     "    <remotePort value=\"7071\" />" + Environment.NewLine +
                                                     "    <layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" +
                                                     Environment.NewLine +
                                                     "</appender>" + Environment.NewLine +
                                                     "Configuration for Serilog:" + Environment.NewLine +
                                                     "Log.Logger = new LoggerConfiguration()       " +
                                                     Environment.NewLine +
                                                     "	.MinimumLevel.Verbose()                   " +
                                                     Environment.NewLine +
                                                     "	.WriteTo.UDPSink(IPAddress.Loopback, 7071)" +
                                                     Environment.NewLine +
                                                     "	.CreateLogger();                          ";

        public void Clear()
        {
        }

        private void Start()
        {
            while (_udpClient != null && _remoteEndPoint != null)
                try
                {
                    var buffer = _udpClient.Receive(ref _remoteEndPoint);
                    var loggingEvent = Encoding.UTF8.GetString(buffer);

                    //Console.WriteLine(loggingEvent);
                    //  Console.WriteLine("Count: " + count++);

                    if (Notifiable == null)
                        continue;
//TODO:edit
/*
                    LogMessage logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "UdpLogger");
                    logMsg.RootLoggerName = _remoteEndPoint.Address.ToString().Replace(".", "-");
                    logMsg.LoggerName = string.Format("{0}_{1}", _remoteEndPoint.Address.ToString().Replace(".", "-"), logMsg.LoggerName);
*/
                    LogMessage logMsg = null;
                    switch (LogFormatType)
                    {
                        case LogFormat.Log4J:
                            logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "UdpLogger");
                            break;
                        case LogFormat.Serilog:
                            logMsg = SerilogParser.Parse(loggingEvent, "UdpLogger");
                            break;
                    }

                    //logMsg.RootLoggerName = _remoteEndPoint.Address.ToString().Replace(".", "-");
                    //logMsg.LoggerName = string.Format("{0}_{1}", _remoteEndPoint.Address.ToString().Replace(".", "-"), logMsg.LoggerName);
                    if (logMsg != null)
                    {
                        logMsg.RootLoggerName = logMsg.LoggerName;
                        if (CondenseCamelCase && logMsg.LoggerName != null)
                        {
                            var newName = "";
                            var stopAt = logMsg.LoggerName.LastIndexOf('.');
                            if (stopAt > 0)
                            {
                                for (var i = 0; i < stopAt; i++)
                                    if (char.IsUpper(logMsg.LoggerName[i]) || logMsg.LoggerName[i] == '.')
                                        newName += logMsg.LoggerName[i];
                                newName += logMsg.LoggerName.Substring(stopAt);
                                logMsg.RootLoggerName = newName;
                            }
                        }

                        logMsg.LoggerName = string.Format(":{1}.{0}", logMsg.LoggerName, _port);

                        Notifiable.Notify(logMsg);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
        }

        public override void Initialize()
        {
            if (_worker != null && _worker.IsAlive)
                return;

            // Init connexion here, before starting the thread, to know the status now
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            _udpClient = _ipv6 ? new UdpClient(_port, AddressFamily.InterNetworkV6) : new UdpClient(_port);
            _udpClient.Client.ReceiveBufferSize = _bufferSize;
            if (!string.IsNullOrEmpty(_address))
                _udpClient.JoinMulticastGroup(IPAddress.Parse(_address));

            // We need a working thread
            _worker = new Thread(Start) {IsBackground = true};
            _worker.Start();
        }

        public override void Terminate()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;

                _remoteEndPoint = null;
            }

            if (_worker != null && _worker.IsAlive)
                _worker.Abort();
            _worker = null;
        }
    }
}