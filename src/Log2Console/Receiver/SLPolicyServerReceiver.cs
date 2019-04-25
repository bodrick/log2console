using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Log2Console.Receiver
{
    [DisplayName("Silverlight Socket Policy")]
    public class SLPolicyServerReceiver : BaseReceiver
    {
        private Socket _socket;

        [Category("Configuration")]
        [DisplayName("TCP Port From")]
        [DefaultValue(4502)]
        public int PortFrom { get; set; } = 4502;

        [Category("Configuration")]
        [DisplayName("TCP Port To")]
        [DefaultValue(4532)]
        public int PortTo { get; set; } = 4532;

        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig => "This receiver allows Silverlight client to use sockets";

        private const string PolicyRequestString = "<policy-file-request/>";

        private const string PolicyTemplate =
            @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<access-policy>                                            
<cross-domain-access>                                                
<policy>                                                   
  <allow-from><domain uri=""*"" /></allow-from>                                                    
  <grant-to>                                                        
    <socket-resource port=""{0}-{1}"" protocol=""tcp"" />                                                    
  </grant-to>                                                
</policy>                                            
</cross-domain-access>                                        
</access-policy>";

        private byte[] _policy;

        public override void Initialize()
        {
            if (_socket != null) return;

            _policy = Encoding.UTF8.GetBytes(string.Format(PolicyTemplate, PortFrom, PortTo));

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ExclusiveAddressUse = true
            };
            _socket.Bind(new IPEndPoint(IPAddress.Any, 943));
            _socket.Listen(100);

            var args = new SocketAsyncEventArgs();
            args.Completed += AcceptAsyncCompleted;

            _socket.AcceptAsync(args);
        }

        private void AcceptAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (_socket == null) return;

            var socket = e.AcceptSocket;

            e.AcceptSocket = null;
            _socket.AcceptAsync(e);

            ProcessRequest(socket);
        }

        private void ProcessRequest(Socket socket)
        {
            using (var client = new TcpClient
            {
                Client = socket, ReceiveTimeout = 5000
            })
            using (var s = client.GetStream())
            {
                var buffer = new byte[PolicyRequestString.Length];
                s.Read(buffer, 0, buffer.Length);
                s.Write(_policy, 0, _policy.Length);
            }
        }

        public override void Terminate()
        {
            if (_socket == null) return;

            _socket.Close();
            _socket = null;
        }

        #endregion
    }
}