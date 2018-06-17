using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Log2Console.Log;

namespace Log2Console.Receiver
{
    public static class ReceiverUtils
    {
        private static readonly DateTime s1970 = new DateTime(1970, 1, 1);

        /// <summary>
        ///     We can share settings to improve performance
        /// </summary>
        private static readonly XmlReaderSettings XmlSettings = CreateSettings();

        /// <summary>
        ///     We can share parser context to improve performance
        /// </summary>
        private static readonly XmlParserContext XmlContext = CreateContext();

        public static string GetTypeDescription(Type type)
        {
            var attr = (DisplayNameAttribute) Attribute.GetCustomAttribute(type, typeof(DisplayNameAttribute), true);
            return attr != null ? attr.DisplayName : type.ToString();
        }

        private static XmlReaderSettings CreateSettings()
        {
//TODO: edit
            return new XmlReaderSettings
            {
                CloseInput = false,
                ValidationType = ValidationType.None,
                ConformanceLevel = ConformanceLevel.Fragment
            };
            //return new XmlReaderSettings { CloseInput = false, ValidationType = ValidationType.None };
        }

        private static XmlParserContext CreateContext()
        {
            var nt = new NameTable();
            var nsmanager = new XmlNamespaceManager(nt);
            nsmanager.AddNamespace("log4j", "http://jakarta.apache.org/log4j/");
            nsmanager.AddNamespace("nlog", "http://nlog-project.org");
            return new XmlParserContext(nt, nsmanager, "elem", XmlSpace.None, Encoding.UTF8);
        }

        /// <summary>
        ///     Parse LOG4JXml from xml stream
        /// </summary>
        public static LogMessage ParseLog4JXmlLogEvent(Stream logStream, string defaultLogger)
        {
            // In case of ungraceful disconnect 
            // logStream is closed and XmlReader throws the exception,
            // which we handle in TcpReceiver
            using (var reader = XmlReader.Create(logStream, XmlSettings, XmlContext))
            {
                return ParseLog4JXmlLogEvent(reader, defaultLogger);
            }
        }

        /// <summary>
        ///     Try to parse the xml.
        /// </summary>
        /// <param name="outerXml"></param>
        /// <param name="defaultLogger"></param>
        /// <returns></returns>
        private static LogMessage TryParseLog4JXmlLogEvent(string outerXml, string defaultLogger)
        {
            LogMessage logMessage;
            try
            {
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(outerXml));
                logMessage = ParseLog4JXmlLogEvent(ms, defaultLogger);
            }
            catch (Exception e)
            {
                logMessage = new LogMessage
                {
                    LoggerName = nameof(ReceiverUtils),
                    RootLoggerName = nameof(ReceiverUtils),
                    ThreadName = "NA",
                    Message = "Error parsing log" + Environment.NewLine + outerXml,
                    TimeStamp = DateTime.Now,
                    Level = LogLevels.Instance[LogLevel.Warn],
                    ExceptionString = e.Message
                };
            }

            //logMessage.RawLog = outerXml;
            return logMessage;
        }

        /// <summary>
        ///     IEnumerable of log events
        /// </summary>
        /// <param name="logStream"></param>
        /// <param name="defaultLogger"></param>
        /// <returns></returns>
        public static IEnumerable<LogMessage> ParseLog4JXmlLogEvents(Stream logStream, string defaultLogger)
        {
            var buffer = new byte[4096];
            int bytesRead;
            var startPos = 0;
            while ((bytesRead = logStream.Read(buffer, startPos, buffer.Length - startPos)) > 0)
            {
                var xmlText = Encoding.UTF8.GetString(buffer, 0, bytesRead + startPos);

                var leftOversPos = 0;
                // This regex will match the start and end tags we are looking for.
                var matches = Regex.Matches(xmlText, $"<(/?)\\s*(log4j:event)[^<>]*(/?)>");

                // Break up the log messages into single log messages before processing.
                foreach (Match match in matches)
                {
                    var IsBeginElement = string.IsNullOrWhiteSpace(match.Groups[1].Value);
                    var IsEmptyElement = match.Value.EndsWith("/>");
                    if (IsBeginElement)
                    {
                        // Some data before the start tag, this will probably fail.
                        if (startPos < match.Index)
                            yield return TryParseLog4JXmlLogEvent(xmlText.Substring(startPos, match.Index - startPos),
                                defaultLogger);
                        // Empty XML Element, this will always fail, but go ahead and try anyway.
                        if (IsEmptyElement)
                        {
                            yield return TryParseLog4JXmlLogEvent(xmlText.Substring(match.Index, match.Length),
                                defaultLogger);
                            leftOversPos = startPos = match.Index + match.Length;
                        }
                        else
                        {
                            startPos = match.Index;
                        }
                    }
                    else // End element process outer xml
                    {
                        yield return TryParseLog4JXmlLogEvent(
                            xmlText.Substring(startPos, match.Index + match.Length - startPos), defaultLogger);
                        leftOversPos = startPos = match.Index + match.Length;
                    }
                }

                var leftOvers = Encoding.UTF8.GetBytes(xmlText.Substring(leftOversPos));
                leftOvers.CopyTo(buffer, 0);
                startPos = leftOvers.Length;
            }
        }

        /// <summary>
        ///     Parse LOG4JXml from string
        /// </summary>
        public static LogMessage ParseLog4JXmlLogEvent(string logEvent, string defaultLogger)
        {
            try
            {
                using (var reader = new XmlTextReader(logEvent, XmlNodeType.Element, XmlContext))
                {
                    return ParseLog4JXmlLogEvent(reader, defaultLogger);
                }
            }
            catch (Exception e)
            {
                return new LogMessage
                {
                    // Create a simple log message with some default values
                    LoggerName = defaultLogger,
                    RootLoggerName = defaultLogger,
                    ThreadName = "NA",
                    Message = logEvent,
                    TimeStamp = DateTime.Now,
                    Level = LogLevels.Instance[LogLevel.Info],
                    ExceptionString = e.Message
                };
            }
        }

        /// <summary>
        ///     Here we expect the log event to use the log4j schema.
        ///     Sample:
        ///     <log4j:event logger="Statyk7.Another.Name.DummyManager" timestamp="1184286222308" level="ERROR" thread="1">
        ///         <log4j:message>This is an Message</log4j:message>
        ///         <log4j:properties>
        ///             <log4j:data name="log4jmachinename" value="remserver" />
        ///             <log4j:data name="log4net:HostName" value="remserver" />
        ///             <log4j:data name="log4net:UserName" value="REMSERVER\Statyk7" />
        ///             <log4j:data name="log4japp" value="Test.exe" />
        ///         </log4j:properties>
        ///     </log4j:event>
        /// </summary>
        /// Implementation inspired from: http://geekswithblogs.net/kobush/archive/2006/04/20/75717.aspx
        public static LogMessage ParseLog4JXmlLogEvent(XmlReader reader, string defaultLogger)
        {
            /*
             // for debugging the whole xml
            StringBuilder sb = new StringBuilder();
            while (reader.Read())
                sb.AppendLine(reader.ReadOuterXml());
            var x= sb.ToString();
            */

            var logMsg = new LogMessage();

            //TODO: edit
            while (!reader.EOF && (reader.NodeType != XmlNodeType.Element || reader.Name != "log4j:event"))
                reader.Read();
            if (reader.MoveToContent() != XmlNodeType.Element || reader.Name != "log4j:event")
                throw new Exception("The Log Event is not a valid log4j Xml block.");

            logMsg.LoggerName = reader.GetAttribute("logger");
            logMsg.Level = LogLevels.Instance[reader.GetAttribute("level")];
            logMsg.ThreadName = reader.GetAttribute("thread");

            if (long.TryParse(reader.GetAttribute("timestamp"), out var timeStamp))
                logMsg.TimeStamp = s1970.AddMilliseconds(timeStamp).ToLocalTime();

            var eventDepth = reader.Depth;
            reader.Read();
            while (reader.Depth > eventDepth)
            {
                if (reader.MoveToContent() == XmlNodeType.Element)
                    switch (reader.Name)
                    {
                        case "log4j:message":
                            logMsg.Message = reader.ReadString();
                            break;

                        case "log4j:throwable":
                            logMsg.ExceptionString = reader.ReadString();
                            //logMsg.Message += Environment.NewLine + reader.ReadString();
                            break;

                        case "log4j:ndc":
                            break;

                        case "log4j:locationInfo":
                            logMsg.CallSiteClass = reader.GetAttribute("class");
                            logMsg.CallSiteMethod = reader.GetAttribute("method");
                            logMsg.SourceFileName = reader.GetAttribute("file");
                            if (uint.TryParse(reader.GetAttribute("line"), out var sourceFileLine))
                                logMsg.SourceFileLineNr = sourceFileLine;
                            break;
                        case "nlog:eventSequenceNumber":
                            if (ulong.TryParse(reader.ReadString(), out var sequenceNumber))
                                logMsg.SequenceNr = sequenceNumber;
                            break;

                        case "nlog:locationInfo":
                            logMsg.SourceAssembly = reader.GetAttribute("assembly");
                            break;

                        case "log4j:properties":
                            reader.Read();
                            while (reader.MoveToContent() == XmlNodeType.Element
                                   && reader.Name == "log4j:data")
                            {
                                var name = reader.GetAttribute("name");
                                var value = reader.GetAttribute("value");
                                if (name != null && name.ToLower().Equals("exceptions") && String.IsNullOrWhiteSpace(logMsg.ExceptionString))
                                    logMsg.ExceptionString = value;
                                else
                                    logMsg.Properties[name ?? throw new InvalidOperationException()] = value;

                                reader.Read();
                            }

                            break;
                    }
                reader.Read();
            }

            return logMsg;
        }
    }
}