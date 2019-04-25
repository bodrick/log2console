using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Log2Console.Log;

namespace Log2Console.Receiver
{
    [DisplayName("WebSockets")]
    public class WebSocketsReceiver : BaseReceiver
    {
        private byte[] _buffer;

        private CancellationToken _cancellationToken;

        private StringBuilder _messageBuilder;

        private ClientWebSocket _websocketClient;

        private Thread _worker;


        [Category("Configuration")]
        [DisplayName("Server Host")]
        [DefaultValue("ws://localhost:80")]
        public string WebSocketServerUri { get; set; } = @"wss://localhost:443";

        [Category("Configuration")]
        [DisplayName("Handshake Msg.")]
        [DefaultValue("")]
        public string WebsocketHandshakeMsg { get; set; } = string.Empty;

        [Category("Configuration")]
        [DisplayName("Receive Buffer Size")]
        public int BufferSize { get; set; } = 10000;

        private void Start()
        {
            var buffer = new ArraySegment<byte>(_buffer);
            var lastState = _websocketClient?.State;

            while (true)
            {
                try
                {
                    if (_websocketClient != null && lastState != _websocketClient.State)
                    {
                        NotifyWebSocketStateChange(_websocketClient.State);
                    }

                    lastState = _websocketClient?.State;

                    if (_websocketClient == null
                        || _websocketClient.State != WebSocketState.Open
                        || Notifiable == null)
                    {
                        Thread.Sleep(150); // don't let it throttle so badly
                        continue;
                    }

                    _websocketClient.ReceiveAsync(buffer, _cancellationToken)
                        .ContinueWith(OnBufferReceived, _cancellationToken)
                        .Wait(_cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
            }
        }

        private void OnBufferReceived(Task<WebSocketReceiveResult> obj)
        {
            if (obj.IsCompleted)
            {
                var loggingEvent = Encoding.UTF8.GetString(_buffer);
                _messageBuilder.Append(loggingEvent);

                Console.WriteLine(loggingEvent);

                if (obj.Result.EndOfMessage)
                {
                    var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "wssLogger");
                    logMsg.Level = LogLevels.Instance[LogLevel.Debug];

                    var loggerName = WebSocketServerUri.Replace("wss://", "wss-").Replace(":", "-").Replace(".", "-");
                    logMsg.RootLoggerName = loggerName;
                    logMsg.LoggerName = $"{loggerName}_{logMsg.LoggerName}";
                    Notifiable.Notify(logMsg);

                    _messageBuilder.Clear();
                }
            }
        }

        private void NotifyWebSocketStateChange(WebSocketState state)
        {
            var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent($"WebSocket state changed: {state}", "wssLogger");
            logMsg.Level = LogLevels.Instance[LogLevel.Info];

            var loggerName = WebSocketServerUri.Replace("wss://", "wss-").Replace(":", "-").Replace(".", "-");
            logMsg.RootLoggerName = loggerName;
            logMsg.LoggerName = $"{loggerName}_{logMsg.LoggerName}";
            Notifiable.Notify(logMsg);
        }

        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig =>
            "Configuration for log4net:" + Environment.NewLine +
            "<appender name=\"UdpAppender\" type=\"log4net.Appender.UdpAppender\">" + Environment.NewLine +
            "    <remoteAddress value=\"localhost\" />" + Environment.NewLine +
            "    <remotePort value=\"7071\" />" + Environment.NewLine +
            "    <layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" + Environment.NewLine +
            "</appender>";

        public override void Initialize()
        {
            if (_worker != null && _worker.IsAlive)
            {
                return;
            }

            Connect();

            // We need a working thread
            _worker = new Thread(Start)
            {
                IsBackground = true
            };
            _worker.Start();
        }

        private void Connect()
        {
            try
            {
                if (_websocketClient != null) Disconnect();

                _buffer = new byte[BufferSize];
                _messageBuilder = new StringBuilder();

                _websocketClient = new ClientWebSocket();
                _cancellationToken = new CancellationToken();
                _websocketClient
                    .ConnectAsync(new Uri(WebSocketServerUri), _cancellationToken)
                    .ContinueWith(WssAuthenticate, _cancellationToken);
            }
            catch (Exception ex)
            {
                _websocketClient = null;
                Console.WriteLine(ex);
            }
        }

        private void WssAuthenticate(Task obj)
        {
            if (!string.IsNullOrEmpty(WebsocketHandshakeMsg))
            {
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(WebsocketHandshakeMsg));

                _websocketClient
                    .SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationToken)
                    .ContinueWith(AuthenticationComplete, _cancellationToken);
            }
        }

        private void AuthenticationComplete(Task obj)
        {
            // ignore it
        }

        public override void Terminate()
        {
            Disconnect();

            if (_worker != null && _worker.IsAlive)
            {
                _worker.Abort();
            }

            _worker = null;
        }

        private void Disconnect()
        {
            try
            {
                if (_websocketClient != null)
                {
                    _websocketClient.Abort();
                    _websocketClient.Dispose();
                    _websocketClient = null;
                }
            }
            catch (Exception ex)
            {
                _websocketClient = null;
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}