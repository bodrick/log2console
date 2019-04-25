using System;
using System.ComponentModel;
using System.IO;
using System.Messaging;
using System.Text;
using System.Threading;
using Log2Console.Log;

namespace Log2Console.Receiver
{
    [DisplayName("Windows Message Queue (MSMQ)")]
    public class MsmqReceiver : BaseReceiver
    {
        private const int QueueCheckTimerDelayAndInterval = 5000;
        private MessageQueue _queue;

        private Timer _queueCreationCheckTimer;

        [Category("Configuration")]
        [DisplayName("Queue Name")]
        [DefaultValue(@".\private$\log")]
        [Description(@"Name of the queue to create.  I.e. .\private$\log-test")]
        public string QueueName { get; set; } = @".\private$\log";

        [Category("Configuration")]
        [DisplayName("Create Queue")]
        [Description(
            "Determines how to handle queue creation.  If true and the queue does not exist it will be created.  If false and the queue does not exist the receiver will wait for the queue to be created.")]
        public bool Create { get; set; }

        [Category("Configuration")]
        public bool Transactional { get; set; }

        [Category("Behavior")]
        [DefaultValue(true)]
        [DisplayName("Bulk Process Backed Up Messages")]
        [Description(
            "If true multiple messages in the queue are processed as one update to the log viewer.  This improves the performance of the viewer")]
        public bool BulkProcessBackedUpMessages { get; set; } = true;


        [Browsable(false)]
        public override string SampleClientConfig =>
            "Configuration for NLog:" + Environment.NewLine +
            "<target name=\"queue\" type=\"MSMQ\" layout=\"${log4jxmlevent}\" " + Environment.NewLine +
            "\tqueue = \".\\private$\\nlog\" recoverable= \"true\"" + Environment.NewLine +
            "\tlabel = \"${logger}\" />" + Environment.NewLine +
            Environment.NewLine + Environment.NewLine +
            "Configuration for log4net:" + Environment.NewLine +
            Environment.NewLine +
            "NOTE:  log4net (1.2.10) does not include an MSMQ appender.  The following configuration is based on the MSMQ Appender in '.\\examples\\net\\1.0\\Appenders\\SampleAppendersApp\\cs\\src' that is included in the log4net download." +
            Environment.NewLine + Environment.NewLine +
            "<appender name=\"MsmqAppender\" type=\"SampleAppendersApp.Appender.MsmqAppender, SampleAppendersApp\">" +
            Environment.NewLine +
            "\t<queueName value=\".\\Private$\\log-test\" />" + Environment.NewLine +
            "\t<labelLayout value=\"LOG [%level] %date\" />" + Environment.NewLine +
            "\t<layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" + Environment.NewLine +
            "</appender>";


        /// <summary>
        /// </summary>
        public override void Initialize()
        {
            if (!MessageQueue.Exists(QueueName))
            {
                if (Create)
                {
                    MessageQueue.Create(QueueName, Transactional);
                }
                else
                {
                    /*
                     * Start the queue check timer.  Should the time be configurable?
                     */
                    _queueCreationCheckTimer = new Timer(QueueCreationCheckTimerFunction, this,
                        QueueCheckTimerDelayAndInterval,
                        QueueCheckTimerDelayAndInterval);
                    return;
                }
            }

            Start();
        }


        /// <summary>
        /// </summary>
        private void Start()
        {
            _queue = new MessageQueue(QueueName);

            _queue.ReceiveCompleted += delegate(object source, ReceiveCompletedEventArgs asyncResult)
            {
                try
                {
                    // End the asynchronous receive operation.
                    var m = ((MessageQueue)source).EndReceive(asyncResult.AsyncResult);

                    if (Notifiable != null)
                    {
                        var loggingEvent = Encoding.ASCII.GetString(((MemoryStream)m.BodyStream).ToArray());
                        var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "MSMQLogger");
                        logMsg.LoggerName = $"{QueueName.TrimStart('.')}_{logMsg.LoggerName}";
                        logMsg.RootLoggerName = QueueName;
                        Notifiable.Notify(logMsg);
                    }


                    if (BulkProcessBackedUpMessages)
                    {
                        var all = ((MessageQueue)source).GetAllMessages();
                        if (all.Length > 0)
                        {
                            var numberOfMessages = all.Length > 1000 ? 1000 : all.Length;

                            var logs = new LogMessage[numberOfMessages];

                            for (var i = 0; i < numberOfMessages; i++)
                            {
                                var thisOne = ((MessageQueue)source).Receive();

                                var loggingEvent =
                                    Encoding.ASCII.GetString(((MemoryStream)thisOne.BodyStream).ToArray());
                                var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "MSMQLogger");
                                logMsg.LoggerName = $"{QueueName.TrimStart('.')}_{logMsg.LoggerName}";
                                logs[i] = logMsg;
                            }

                            Notifiable?.Notify(logs);
                        }
                    }

                    ((MessageQueue)source).BeginReceive();
                }
                catch (MessageQueueException)
                {
                    // Handle sources of MessageQueueException.
                }
            };

            _queue.BeginReceive();
        }


        /// <summary>
        /// </summary>
        public override void Terminate() => _queue?.Close();


        /// <summary>
        /// </summary>
        /// <param name="state"></param>
        private static void QueueCreationCheckTimerFunction(object state)
        {
            //TODOCJH:  If this timer gets called then we did not finish the job before the maximum allowable time.
            //_logger.Fatal("JobMaxExecutionTimerFunction");

            if (state is MsmqReceiver rcv && MessageQueue.Exists(rcv.QueueName))
            {
                rcv._queueCreationCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
                rcv._queueCreationCheckTimer.Dispose();
                rcv.Start();
            }
        }
    }
}