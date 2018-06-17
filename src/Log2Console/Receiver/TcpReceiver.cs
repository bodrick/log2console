using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Log2Console.Receiver
{
    [Serializable]
    [DisplayName("TCP (IP v4 and v6)")]
    public class TcpReceiver : BaseReceiver
    {
        private int _bufferSize = 10000;

        private bool _ipv6;
        private int _port = 4505;

        [NonSerialized] private Socket _socket;

        [Category("Configuration")]
        [DisplayName("TCP Port Number")]
        [DefaultValue(4505)]
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
        [DisplayName("Receive Buffer Size")]
        [DefaultValue(10000)]
        public int BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value;
        }

        [Browsable(false)]
        public override string SampleClientConfig => "Configuration for NLog:" + Environment.NewLine +
                                                     "<target name=\"TcpOutlet\" xsi:type=\"NLogViewer\" address=\"tcp://localhost:4505\"/>";

        public override void Initialize()
        {
            if (_socket != null) return;

            _socket = new Socket(_ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp) {ExclusiveAddressUse = true};
            _socket.Bind(new IPEndPoint(_ipv6 ? IPAddress.IPv6Any : IPAddress.Any, _port));
            _socket.Listen(100);
            _socket.ReceiveBufferSize = _bufferSize;

            var args = new SocketAsyncEventArgs();
            args.Completed += AcceptAsyncCompleted;

            _socket.AcceptAsync(args);
        }

        private void AcceptAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (_socket == null || e.SocketError != SocketError.Success) return;

            new Thread(Start) {IsBackground = true}.Start(e.AcceptSocket);

            e.AcceptSocket = null;
            _socket.AcceptAsync(e);
        }

        private void Start(object newSocket)
        {
//TODO:edit

            try
            {
                using (var socket = (Socket) newSocket)
                using (var ns = new NetworkStream(socket, FileAccess.Read, false))
                {
                    while (_socket != null)
                    {
                        var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(ns, "TcpLogger");
                        logMsg.RootLoggerName = logMsg.LoggerName;
                        logMsg.LoggerName = string.Format(":{1}.{0}", logMsg.LoggerName, _port);

                        Notifiable?.Notify(logMsg);
                    }
                }
            }
            /*
            try
            {
                using (var socket = (Socket)newSocket)
                using (var ns = new NetworkStream(socket, FileAccess.Read, false))
                    while (_socket != null)
                    {
                        var logMessages = ReceiverUtils.ParseLog4JXmlLogEvents(ns, "TcpLogger");
                        foreach (var logMessage in logMessages)
                        {
                            logMessage.RootLoggerName = logMessage.LoggerName;
                            logMessage.LoggerName = string.Format(":{1}.{0}", logMessage.LoggerName, _port);

                            if (Notifiable != null)
                                Notifiable.Notify(logMessage);
                        }
                    }
            }
            */
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Connection closed");
        }

        public override void Terminate()
        {
            if (_socket == null) return;

            _socket.Close();
            _socket = null;
        }
    }
}