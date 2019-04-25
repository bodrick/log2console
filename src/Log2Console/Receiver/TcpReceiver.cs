using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Log2Console.Receiver
{
    [DisplayName("TCP (IP v4 and v6)")]
    public class TcpReceiver : BaseReceiver
    {
        #region Properties

        [Category("Configuration")]
        [DisplayName("TCP Port Number")]
        [DefaultValue(4505)]
        public int Port { get; set; } = 4505;

        [Category("Configuration")]
        [DisplayName("Use IPv6 Addresses")]
        [DefaultValue(false)]
        public bool IpV6 { get; set; } = false;

        [Category("Configuration")]
        [DisplayName("Receive Buffer Size")]
        [DefaultValue(10000)]
        public int BufferSize { get; set; } = 10000;

        #endregion Properties

        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig =>
            "Configuration for NLog:" + Environment.NewLine +
            "<target name=\"TcpOutlet\" xsi:type=\"NLogViewer\" address=\"tcp://localhost:4505\"/>";

        private Socket _socket;

        public override void Initialize()
        {
            if (_socket != null) return;

            _socket = new Socket(IpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp)
            {
                ExclusiveAddressUse = true
            };
            _socket.Bind(new IPEndPoint(IpV6 ? IPAddress.IPv6Any : IPAddress.Any, Port));
            _socket.Listen(100);
            _socket.ReceiveBufferSize = BufferSize;

            var args = new SocketAsyncEventArgs();
            args.Completed += AcceptAsyncCompleted;

            _socket.AcceptAsync(args);
        }

        private void AcceptAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (_socket == null || e.SocketError != SocketError.Success) return;

            new Thread(Start)
            {
                IsBackground = true
            }.Start(e.AcceptSocket);

            e.AcceptSocket = null;
            _socket.AcceptAsync(e);
        }

        private void Start(object newSocket)
        {
            try
            {
                using (var socket = (Socket)newSocket)
                {
                    using (var ns = new NetworkStream(socket, FileAccess.Read, false))
                    {
                        while (_socket != null)
                        {
                            var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(ns, "TcpLogger");
                            logMsg.RootLoggerName = logMsg.LoggerName;
                            logMsg.LoggerName = string.Format(":{1}.{0}", logMsg.LoggerName, Port);

                            Notifiable?.Notify(logMsg);
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void Terminate()
        {
            if (_socket == null) return;

            _socket.Close();
            _socket = null;
        }

        #endregion IReceiver Members
    }
}