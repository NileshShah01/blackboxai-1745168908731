using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SQLite;

namespace ServerApp
{
    public class SocketServer
    {
        private TcpListener _listener;
        private readonly int _port;
        private bool _isRunning;
        private readonly Dictionary<string, TcpClient> _clients = new Dictionary<string, TcpClient>();
        public event Action<string, string> OnClientConnected;
        public event Action<string> OnClientDisconnected;

        public SocketServer(int port = 8888)
        {
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Thread acceptThread = new Thread(new ThreadStart(AcceptClients));
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            
            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
        }

        private void AcceptClients()
        {
            try
            {
                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    string clientId = Guid.NewGuid().ToString();
                    _clients.Add(clientId, client);
                    
                    OnClientConnected?.Invoke(clientId, "New client connected");

                    Thread clientThread = new Thread(() => HandleClient(clientId, client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (SocketException)
            {
                // Listener stopped
            }
        }

        private void HandleClient(string clientId, TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];

                while (_isRunning && client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        ProcessClientMessage(clientId, message);
                    }
                }
            }
            catch (Exception)
            {
                // Client disconnected
            }
            finally
            {
                _clients.Remove(clientId);
                OnClientDisconnected?.Invoke(clientId);
                client.Close();
            }
        }

        private void ProcessClientMessage(string clientId, string message)
        {
            string[] parts = message.Split('|');
            if (parts[0] == "LOGIN")
            {
                string username = parts[1];
                string passwordHash = parts[2];
                bool authenticated = AuthenticateUser(username, passwordHash);
                
                if (authenticated)
                {
                    OnClientConnected?.Invoke(clientId, username);
                    SendCommand(clientId, $"AUTH|SUCCESS|{username}");
                }
                else
                {
                    SendCommand(clientId, "AUTH|FAIL|Invalid credentials");
                }
            }
            else if (parts[0] == "COMMAND")
            {
                string command = parts[1];
                HandleCommand(clientId, command);
            }
        }

        private void HandleCommand(string clientId, string command)
        {
            switch (command)
            {
                case "LOCK":
                    // TODO: Implement lock command logic
                    SendCommand(clientId, "LOCK|SUCCESS");
                    break;
                case "UNLOCK":
                    // TODO: Implement unlock command logic
                    SendCommand(clientId, "UNLOCK|SUCCESS");
                    break;
                case "RESTART":
                    // TODO: Implement restart command logic
                    SendCommand(clientId, "RESTART|SUCCESS");
                    break;
                default:
                    SendCommand(clientId, "COMMAND|FAIL|Unknown command");
                    break;
            }
        }

        private bool AuthenticateUser(string username, string passwordHash)
        {
            using var connection = Database.GetConnection();
            connection.Open();
            var command = new SQLiteCommand(
                "SELECT 1 FROM Users WHERE Username = @username AND PasswordHash = @passwordHash",
                connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            return command.ExecuteScalar() != null;
        }

        public void SendCommand(string clientId, string command)
        {
            if (_clients.TryGetValue(clientId, out TcpClient client))
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(command);
                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }
}