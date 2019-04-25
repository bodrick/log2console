#region Copyright & License

//
// Copyright 2001-2005 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

using System.IO;
using System.Messaging;
using System.Text;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;

namespace Test
{
    /// <summary>
    ///     Appender writes to a Microsoft Message Queue
    /// </summary>
    /// <remarks>
    ///     This appender sends log events via a specified MSMQ queue.
    ///     The queue specified in the QueueName (e.g. .\Private$\log-test) must already exist on
    ///     the source machine.
    ///     The message label and body are rendered using separate layouts.
    /// </remarks>
    public class MsmqAppender : AppenderSkeleton
    {
        private MessageQueue m_queue;

        public string QueueName { get; set; }

        public PatternLayout LabelLayout { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (m_queue == null)
            {
                if (MessageQueue.Exists(QueueName))
                {
                    m_queue = new MessageQueue(QueueName);
                }
                else
                {
                    ErrorHandler.Error("Queue [" + QueueName + "] not found");
                }
            }

            if (m_queue != null)
            {
                var message = new Message
                {
                    Label = RenderLabel(loggingEvent)
                };

                using (var stream = new MemoryStream())
                {
                    var writer = new StreamWriter(stream, new UTF8Encoding(false, true));
                    RenderLoggingEvent(writer, loggingEvent);
                    writer.Flush();
                    stream.Position = 0;
                    message.BodyStream = stream;

                    m_queue.Send(message);
                }
            }
        }

        private string RenderLabel(LoggingEvent loggingEvent)
        {
            if (LabelLayout == null)
            {
                return null;
            }

            var writer = new StringWriter();
            LabelLayout.Format(writer, loggingEvent);

            return writer.ToString();
        }
    }
}