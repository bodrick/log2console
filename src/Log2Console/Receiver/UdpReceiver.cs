using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Log2Console.Receiver
{
    [DisplayName("UDP (IP v4 and v6)")]
    public class UdpReceiver : BaseReceiver
    {
        private IPEndPoint _remoteEndPoint;

        private UdpClient _udpClient;

        private Thread _worker;

        [Category("Configuration")]
        [DisplayName("UDP Port Number")]
        [DefaultValue(7071)]
        public int Port { get; set; } = 7071;

        [Category("Configuration")]
        [DisplayName("Use IPv6 Addresses")]
        [DefaultValue(false)]
        public bool IpV6 { get; set; }

        [Category("Configuration")]
        [DisplayName("Multicast Group Address (Optional)")]
        public string Address { get; set; } = string.Empty;

        [Category("Configuration")]
        [DisplayName("Receive Buffer Size")]
        public int BufferSize { get; set; } = 10000;

        private void Start()
        {
            while (_udpClient != null && _remoteEndPoint != null)
            {
                try
                {
                    var buffer = _udpClient.Receive(ref _remoteEndPoint);
                    var loggingEvent = Encoding.UTF8.GetString(buffer);

                    if (Notifiable == null)
                    {
                        continue;
                    }

                    var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "UdpLogger");
                    logMsg.RootLoggerName = _remoteEndPoint.Address.ToString().Replace(".", "-");
                    logMsg.LoggerName = $"{_remoteEndPoint.Address.ToString().Replace(".", "-")}_{logMsg.LoggerName}";
                    Notifiable.Notify(logMsg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
            }
        }


        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig =>
            "Configuration for log4net:" + Environment.NewLine +
            "<appender name=\"UdpAppender\" type=\"log4net.Appender.UdpAppender\">" + Environment.NewLine +
            "    <remoteAddress value=\"127.0.0.1\" />" + Environment.NewLine +
            "    <remotePort value=\"7071\" />" + Environment.NewLine +
            "    <layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" + Environment.NewLine +
            "</appender>" + Environment.NewLine +
            Environment.NewLine +
            "And add appender to log4net <root>:" + Environment.NewLine +
            "<appender-ref ref=\"UdpAppender\" />";

        public override void Initialize()
        {
            if (_worker != null && _worker.IsAlive)
            {
                return;
            }

            // Init connection here, before starting the thread, to know the status now
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            _udpClient = IpV6 ? new UdpClient(Port, AddressFamily.InterNetworkV6) : new UdpClient(Port);
            _udpClient.Client.ReceiveBufferSize = BufferSize;
            if (!string.IsNullOrEmpty(Address))
            {
                _udpClient.JoinMulticastGroup(IPAddress.Parse(Address));
            }

            // We need a working thread
            _worker = new Thread(Start)
            {
                IsBackground = true
            };
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
            {
                _worker.Abort();
            }

            _worker = null;
        }

        #endregion IReceiver Members
    }
}