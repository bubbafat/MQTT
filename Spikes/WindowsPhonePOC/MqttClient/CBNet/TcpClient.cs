using System.Diagnostics;
using System.Threading;

namespace System.Net.Sockets
{
    public sealed class TcpClient : IDisposable
    {
        EndPoint _endpoint;
        bool _responsePending;
        readonly AutoResetEvent _autoResetEvent;
        readonly NetworkStream _networkStream;

        public TcpClient()
            : this(AddressFamily.InterNetwork) { }


        public TcpClient(IPEndPoint endpoint)
            : this(AddressFamily.InterNetwork)
        {
            Connect(endpoint);
        }

        public TcpClient(string host, int port)
            : this(AddressFamily.InterNetwork)
        {
            var endpoint = new DnsEndPoint(host, port);
            Connect(endpoint);
        }

        public TcpClient(AddressFamily addressFamily)
        {
            _autoResetEvent = new AutoResetEvent(false);
           
            Client = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            _networkStream = new NetworkStream(Client);
        }

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object userToken)
        {
            _endpoint = new IPEndPoint(address, port);
            return BeginConnect(requestCallback, userToken);
        }

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object userToken)
        {
            _endpoint = new DnsEndPoint(host, port);
            return BeginConnect(requestCallback, userToken);
        }

        IAsyncResult BeginConnect(AsyncCallback requestCallback, object userToken)
        {
            var stateObject = new StateObject
            {
                AsyncState = userToken,
                Callback = requestCallback,
                IsCompleted = false
            };

            ConnectAsync(stateObject);
            return stateObject;
        }

        void OnConnectedAsync(object sender, SocketAsyncEventArgs e)
        {
            Continue();

            var stateObject = (StateObject)e.UserToken;
            stateObject.IsCompleted = true;
            stateObject.Callback(stateObject);
        }

        void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            Continue();
        }

        void ConnectAsync(StateObject stateObject)
        {
            var e = new SocketAsyncEventArgs
                {
                    UserToken = stateObject, 
                    RemoteEndPoint = _endpoint
                };
            e.Completed += OnConnectedAsync;
            Client.ConnectAsync(e);
        }

        public void Connect(IPAddress address, int port)
        {
            var endpoint = new IPEndPoint(address, port);
            Connect(endpoint);
        }


        public void Connect(string host, int port)
        {
            var endpoint = new DnsEndPoint(host, port);
            Connect(endpoint);
        }

        public void Connect(EndPoint endpoint)
        {
            _endpoint = endpoint;

            var e = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = _endpoint
                };
            e.Completed += OnConnected;

            try
            {
                Client.ConnectAsync(e);
                WaitOne();
            }
            catch (SocketException)
            {
                Continue();
            }
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            if (!_responsePending)
            {
                WaitOne();
            }
        }

        void Continue()
        {
            _responsePending = false;
            _autoResetEvent.Set();
        }

        void WaitOne()
        {
            _autoResetEvent.WaitOne();
        }

        public NetworkStream GetStream()
        {
            return _networkStream;   
        }

        public int Available
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        public Socket Client { get; set; }
        public bool Connected { get { return Client != null && Client.Connected; } }
        public bool Active { get { return Connected; } }
        public bool ExclusiveAddressUse { get { return false; } }
        public bool NoDelay 
        {
            get { return true; }
        }

        public void Dispose()
        {
            var stream = GetStream();
            stream.Dispose();

            try
            {
                Client.Shutdown(SocketShutdown.Both);
                Client.Close();
            }
            catch (ObjectDisposedException ex)
            {
                // no one cares at this point
                Debug.WriteLine(ex.Message);
            }
            catch (SocketException ex)
            {
            	// no one cares at this point
                Debug.WriteLine(ex.Message);
            }
        }
    }
}