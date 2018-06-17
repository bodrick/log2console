using System;
using System.Net.Sockets;
using System.Text;

namespace TestOther
{
    internal static class Program
    {
        private const string logBody =
            @"<log4j:event logger=""UserQuery"" level=""INFO"" timestamp=""THE_TIME_IS"" thread=""12"">
    <log4j:message>{0}</log4j:message>
    <log4j:properties>
        <log4j:data name=""log4japp"" value=""LINQPad Query Server(13380)"" />
        <log4j:data name=""log4jmachinename"" value=""DLM-DEV"" />
    </log4j:properties>
</log4j:event>";

        /// <summary>
        ///     Make sure NLog can gracefully handle partial messages, malformed messages, etc
        ///     http://www.w3schools.com/xml/xml_syntax.asp
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            Console.WriteLine("x to exit, anything else to continue");
            var key = Console.ReadKey();
            while (key.Key != ConsoleKey.X)
            {
                using (var client = new TcpClient("localhost", 4505))
                {
                    var stream = client.GetStream();

                    SendLogMessage(stream, "Extra text in open",
                        logBody.Replace("<log4j:message>", "<log4j:message            123456  >"));

                    SendLogMessage(stream, "Normal Message");

                    SendLogMessage(stream, "Warn message",
                        logBody.Replace("level=\"INFO\"", "level=\"WARN\""));

                    SendLogMessage(stream, "comments",
                        "<!-- Comments? -->" + logBody);

                    SendLogMessage(stream, "comments in the middle",
                        logBody.Replace("<log4j:properties>", "!-- Comments anyone --><log4j:properties>"));

                    SendLogMessage(stream, "Extra spaces in open",
                        logBody.Replace("<log4j:message>", "<log4j:message              >"));

                    SendLogMessage(stream, "Extra spaces in close",
                        logBody.Replace("</log4j:event>", "</log4j:event            >"));

                    SendLogMessage(stream, "Done with Test messages");

                    SendLogMessage(stream, "Good Luck with this one",
                        @"123<log4j:event logger=""UserQuery"" level=""TRACE"" timestamp=""THE_TIME_IS"" thread=""12"">
    < log4j:message abc = ""def"" > Trace Message </ log4j:message >
          < log4j:properties >
               < log4j:data name = ""log4japp"" value = ""LINQPad Query Server(13380)"" />
                  < log4j:data name = ""log4jmachinename"" value = ""DLM-DEV"" />
                 </ log4j:properties >
              </ log4j:event >wft how did I get here?<log4j:event logger=""UserQuery"" level=""DEBUG"" timestamp=""THE_TIME_IS"" thread=""12"">
    <log4j:message 123>Debug Message</log4j:message>
    <log4j:properties>
        <log4j:data name=""log4japp"" value=""LINQPad Query Server(13380)"" />
        <log4j:data name=""log4jmachinename"" value=""DLM-DEV"" />
    </log4j:properties>
</log4j:event>321321321321
<log4j:event logger=""UserQuery"" level=""ERROR"" timestamp=""THE_TIME_IS"" thread=""12"">
    < log4j:message > Hey there's a valid message in the middle of this junk </ log4j:message >
    < log4j:properties >
        < log4j:data name = ""log4japp"" value = ""LINQPad Query Server(13380)"" />
        < log4j:data name = ""log4jmachinename"" value = ""DLM-DEV"" />
    </ log4j:properties >
</ log4j:event>
<log4j:event />123123data name = ""log4japp"" value = ""LINQPad Query Server(13380)"" />
                  < log4j:data name = ""log4jmachinename"" value = ""DLM-DEV"" />
                 </ log4j:properties >
              </ log4j:event >wft how did I get here?<log4j:event logger=""UserQuery"" level=""DEBUG"" timestamp=""THE_TIME_IS"" thread=""12"">
    <log4j:message 123>Debug Message</log4j:message>
    <log4j:properties>
        <log4j:data name=""log4japp"" value=""LINQPad Query Server(13380)"" />
        <log4j:data name=""log4jmachinename"" value=""DLM-DEV"" />
    </log4j:properties>
</log4j:event>321<log4j:event >123123data name = ""log4j");
                    //stream.Flush();
                    stream.Close();
                }

                Console.WriteLine("x to exit, anything else to continue");
                key = Console.ReadKey();
            }
        }

        private static void SendLogMessage(NetworkStream stream, string logMessage, string logBody = logBody)
        {
            logBody = logBody.Replace("THE_TIME_IS",
                (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString("0"));
            Console.WriteLine(logMessage);
            var buffer = Encoding.UTF8.GetBytes(string.Format(logBody, logMessage));
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}