using System.Net;
using System.Net.Sockets;

namespace FP300Service
{
    public class TCPConnection : IConnection, IDisposable
    {
        private Socket client = null;
        private string ipAddress = String.Empty;
        private int port = 0;
        private static int supportedBufferSize = ProgramConfig.DEFAULT_BUFFER_SIZE;

        public TCPConnection(String ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        // Destructor of this class.
        ~TCPConnection()
        {
            Dispose();
        }

        public void Open()
        {
            // Close if there is any idle connection
            this.Close();

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(this.ipAddress), this.port);
            client = new Socket(AddressFamily.InterNetwork,
                              SocketType.Stream, ProtocolType.Tcp);
            // Set initalize values
            client.ReceiveTimeout = 4500;
            client.ReceiveBufferSize = supportedBufferSize;
            client.SendBufferSize = supportedBufferSize;
            // Connect to destination
            client.Connect(ipep);
        }

        public bool IsOpen
        {
            get
            {
                if (client != null && client.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Close()
        {
            if (IsOpen)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        public int FPUTimeout
        {
            get
            {
                return client.ReceiveTimeout;
            }
            set
            {
                client.ReceiveTimeout = value;
            }
        }

        public int BufferSize
        {
            get
            {
                return client.SendBufferSize;
            }
            set
            {
                // Close the connection
                Close();
                // Set new buffer size
                supportedBufferSize = value;
                // Re-open the connection
                Open();
            }
        }

        public void Dispose()
        {
            try
            {
                Close();
            }
            catch (System.Exception)
            {

            }
        }

        public object ToObject()
        {
            return client;
        }
    }
}
