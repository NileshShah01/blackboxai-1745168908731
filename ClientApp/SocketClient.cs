using System;
using System.Net.Sockets;
using System.Threading;

namespace ClientApp
{
    public class SocketClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly string _serverIp;
        private readonly int _serverPort;
        private bool _isConnected;
        private Thread _receiveThread;

        public event Action<string> OnMessageReceived;
        public event Action OnDisconnected;

        public SocketClient(string serverIp, int serverPort = 8888)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public bool Connect()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(_serverIp, _serverPort);
                _stream = _client.GetStream();
                _isConnected = true;

                _receiveThread = new Thread(new ThreadStart(ReceiveData));
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            _receiveThread?.Join();
            OnDisconnected?.Invoke();
        }

        public void SendMessage(string message)
        {
            if (_isConnected && _stream != null)
            {
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(message);
                _stream.Write(buffer, 0, buffer.Length);
            }
        }

        private void ReceiveData()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (_isConnected)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        OnMessageReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }
    }
}