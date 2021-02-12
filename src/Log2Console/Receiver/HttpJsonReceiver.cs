using Log2Console.Log;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace Log2Console.Receiver
{
    [Serializable]
    [DisplayName("HTTP Json (IP v4 and v6)")]
    public class HttpJsonReceiver : IReceiver
    {
        [NonSerialized] 
        private string _displayName;

        [NonSerialized] 
        protected ILogMessageNotifiable Notifiable;

        [NonSerialized]
        private static HttpListener listener;

        [NonSerialized]
        private Thread listenThread1;

        private bool _ipv6;
        private int _port = 4506;

        [Category("Configuration")]
        [DisplayName("TCP Port Number")]
        [DefaultValue(4506)]
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

        [Browsable(false)]
        public string SampleClientConfig => "Configuration for NLog:" + Environment.NewLine +
                                                     "<target name=\"TcpOutlet\" xsi:type=\"NLogViewer\" address=\"tcp://localhost:4506\"/>";

        [Browsable(false)]
        public string DisplayName
        {
            get => _displayName;
            protected set => _displayName = value;
        }

        private void startlistener(object s)
        {
            while (true)
            {
                ////blocks until a client has connected to the server
                ProcessRequest();
            }
        }

        private void ProcessRequest()
        {
            var result = listener.BeginGetContext(ListenerCallback, listener);
            result.AsyncWaitHandle.WaitOne();
        }

        private void ListenerCallback(IAsyncResult result)
        {
            var context = listener.EndGetContext(result);
            //Thread.Sleep(1000);
            var data_text = new StreamReader(context.Request.InputStream,
            context.Request.ContentEncoding).ReadToEnd();

            //functions used to decode json encoded data.
            //JavaScriptSerializer js = new JavaScriptSerializer();
            //var data1 = Uri.UnescapeDataString(data_text);
            //string da = Regex.Unescape(data_text);
            //var unserialized = js.Deserialize(data_text);

            //var cleaned_data = System.Web.HttpUtility.UrlDecode(data_text);

            context.Response.Headers.Clear();
            context.Response.SendChunked = false;
            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";
            context.Response.Headers.Add("Server", String.Empty);
            context.Response.Headers.Add("Date", String.Empty);
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
            context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, HEAD, OPTIONS");
            context.Response.AddHeader("Access-Control-Max-Age", "1728000");
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            context.Response.Close();
            //context.Response.ContentLength64 = 0;
            //context.Response.OutputStream.Close();

            var logMsg = new LogMessage() { 
                LoggerName = "HttpJsonReceiver", 
                Message = data_text, 
                RootLoggerName = "HttpJsonReceiver", 
                Level = LogLevels.Instance[LogLevel.Info],
                SequenceNr = 0,
                TimeStamp = DateTime.Now,
                ThreadName = string.Empty,
                //ExceptionString = ex.Message + ex.StackTrace,
                CallSiteClass = string.Empty,
                CallSiteMethod = string.Empty,
                SourceFileName = string.Empty,
                SourceFileLineNr = 0
            };
                    logMsg.RootLoggerName = logMsg.LoggerName;
                    logMsg.LoggerName = string.Format(":{1}.{0}", logMsg.LoggerName, _port);

                    Notifiable?.Notify(logMsg);
        }
        public void Initialize()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            //string[] prefixes = new string[] { "https://+:4506" };

            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            //if (prefixes == null || prefixes.Length == 0)
            //    throw new ArgumentException("prefixes");

            // Create a listener.
            listener = new HttpListener();
            // Add the prefixes.
            //foreach (string s in prefixes)
            //{
            //    listener.Prefixes.Add(s);
            //}
            listener.Prefixes.Add("http://localhost:" + Port + "/");
            listener.Prefixes.Add("http://127.0.0.1:" + Port + "/");
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            listener.Start();
            this.listenThread1 = new Thread(new ParameterizedThreadStart(startlistener));
            listenThread1.Start();
        }

        public void Terminate()
        {
            listener.Stop();
        }

        public virtual void Attach(ILogMessageNotifiable notifiable)
        {
            Notifiable = notifiable;
        }

        public virtual void Detach()
        {
            Notifiable = null;
        }
    }
}