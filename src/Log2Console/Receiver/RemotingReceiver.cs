using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Log2Console.Log;
using log4net.Appender;
using log4net.Core;

namespace Log2Console.Receiver
{
    [DisplayName(".NET Remoting")]
    public class RemotingReceiver : BaseReceiver, RemotingAppender.IRemoteLoggingSink, ISerializable
    {
        private const string RemotingReceiverChannelName = "RemotingReceiverChannel";

        private IChannel _channel;

        /// <summary>
        ///     Default ctor
        /// </summary>
        public RemotingReceiver()
        {
        }


        [Category("Configuration")]
        [DisplayName("Remote Sink Name")]
        public string SinkName { get; set; } = "LoggingSink";

        [Category("Configuration")]
        [DisplayName("Remote TCP Port Number")]
        public int Port { get; set; } = 7070;

        [Category("Behavior")]
        [DisplayName("Append Host Name to Logger")]
        [Description("Append the remote Host Name to the Logger Name.")]
        public bool AppendHostNameToLogger { get; set; } = true;


        #region Implementation of IRemoteLoggingSink

        public void LogEvents(LoggingEvent[] events)
        {
            if (events == null || events.Length == 0 || Notifiable == null)
            {
                return;
            }

            var logMsgs = new LogMessage[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                logMsgs[i] = CreateLogMessage(events[i]);
            }

            Notifiable.Notify(logMsgs);
        }

        #endregion Implementation of IRemoteLoggingSink


        #region Override implementation of MarshalByRefObject

        /// <summary>
        ///     Obtains a lifetime service object to control the lifetime
        ///     policy for this instance.
        /// </summary>
        /// <returns><c>null</c> to indicate that this instance should live forever.</returns>
        /// <remarks>
        ///     <para>
        ///         Obtains a lifetime service object to control the lifetime
        ///         policy for this instance. This object should live forever
        ///         therefore this implementation returns <c>null</c>.
        ///     </para>
        /// </remarks>
        public override object InitializeLifetimeService() => null;

        #endregion Override implementation of MarshalByRefObject


        protected LogMessage CreateLogMessage(LoggingEvent logEvent)
        {
            var logMsg = new LogMessage();
            if (AppendHostNameToLogger && logEvent.Properties.Contains(LoggingEvent.HostNameProperty))
            {
                logMsg.RootLoggerName = logEvent.Properties[LoggingEvent.HostNameProperty].ToString();
                logMsg.LoggerName =
                    $"[Host: {logEvent.Properties[LoggingEvent.HostNameProperty]}].{logEvent.LoggerName}";
            }
            else
            {
                logMsg.RootLoggerName = logEvent.LoggerName;
                logMsg.LoggerName = logEvent.LoggerName;
            }

            logMsg.ThreadName = logEvent.ThreadName;
            logMsg.Message = logEvent.RenderedMessage;
            logMsg.TimeStamp = logEvent.TimeStamp;
            logMsg.Level = LogUtils.GetLogLevelInfo(logEvent.Level.Value);

            // Per LoggingEvent.ExceptionObject, the exception object is not serialized, but the exception 
            // text is available through LoggingEvent.GetExceptionString
            logMsg.ExceptionString = logEvent.GetExceptionString();

            // Copy properties as string
            foreach (DictionaryEntry entry in logEvent.Properties)
            {
                if (entry.Key == null || entry.Value == null)
                {
                    continue;
                }

                logMsg.Properties.Add(entry.Key.ToString(), entry.Value.ToString());
            }

            return logMsg;
        }

        #region ISerializable Members

        /// <summary>
        ///     Constructor for Serialization
        ///     N.B: Explicit implementation of ISerializable to mask SecurityIdentity Property of mother class
        /// </summary>
        public RemotingReceiver(SerializationInfo info, StreamingContext context)
        {
            SinkName = info.GetString("SinkName");
            Port = info.GetInt32("Port");
        }

        /// <summary>
        ///     ISerializable method override for deserialization
        ///     N.B: Explicit implementation of ISerializable to mask SecurityIdentity Property of mother class
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("SinkName", SinkName);
            info.AddValue("Port", Port);
        }

        #endregion


        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig =>
            "Configuration for log4net:" + Environment.NewLine +
            "<appender name=\"RemotingAppender\" type=\"log4net.Appender.RemotingAppender\" >" + Environment.NewLine +
            "    <!--The remoting URL to the remoting server object-->" + Environment.NewLine +
            "    <sink value=\"tcp://localhost:7070/LoggingSink\" />" + Environment.NewLine +
            "    <!--Send all events, do not discard events when the buffer is full-->" + Environment.NewLine +
            "    <lossy value=\"false\" />" + Environment.NewLine +
            "    <!--The number of events to buffer before sending-->" + Environment.NewLine +
            "    <bufferSize value=\"5\" />" + Environment.NewLine +
            "</appender>";

        public override void Initialize()
        {
            // Channel already open?
            _channel = ChannelServices.GetChannel(RemotingReceiverChannelName);


            if (_channel == null)
            {
                // Allow clients to receive complete Remoting exception information
                if (RemotingConfiguration.CustomErrorsEnabled(true))
                {
                    RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
                }

                // Create TCP Channel
                try
                {
                    BinaryClientFormatterSinkProvider clientProvider = null;
                    var serverProvider =
                        new BinaryServerFormatterSinkProvider
                        {
                            TypeFilterLevel = TypeFilterLevel.Full
                        };

                    IDictionary props = new Hashtable();
                    props["port"] = Port.ToString();
                    props["name"] = RemotingReceiverChannelName;
                    props["typeFilterLevel"] = TypeFilterLevel.Full;
                    _channel = new TcpChannel(props, clientProvider, serverProvider);

                    ChannelServices.RegisterChannel(_channel, false);
                }
                catch (Exception ex)
                {
                    throw new Exception("Remoting TCP Channel Initialization failed", ex);
                }
            }

            var serverType = RemotingServices.GetServerTypeForUri(SinkName);
            if (serverType == null || serverType != typeof(RemotingAppender.IRemoteLoggingSink))
                // Marshal Receiver
            {
                try
                {
                    RemotingServices.Marshal(this, SinkName, typeof(RemotingAppender.IRemoteLoggingSink));
                }
                catch (Exception ex)
                {
                    throw new Exception("Remoting Marshal failed", ex);
                }
            }
        }

        public override void Terminate()
        {
            if (_channel != null)
            {
                ChannelServices.UnregisterChannel(_channel);
            }

            _channel = null;
        }

        #endregion
    }
}