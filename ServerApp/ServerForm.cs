using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace ServerApp
{
    public partial class ServerForm : MaterialForm
    {
        private readonly MaterialSkinManager materialSkinManager;
        private SocketServer _socketServer;
        private Dictionary<string, SessionInfo> _activeSessions = new Dictionary<string, SessionInfo>();

        // UI Components
        private MaterialListView clientListView;
        private MaterialButton btnLockClient;
        private MaterialButton btnUnlockClient;
        private MaterialButton btnRestartClient;
        private MaterialLabel lblActiveSessions;

        public ServerForm()
        {
            InitializeComponent();
            
            // Initialize MaterialSkin
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue800, Primary.Blue900,
                Primary.Blue500, Accent.LightBlue200,
                TextShade.WHITE
            );

            // Initialize database
            Database.Initialize();

            // Initialize socket server
            _socketServer = new SocketServer();
            _socketServer.OnClientConnected += HandleClientConnected;
            _socketServer.OnClientDisconnected += HandleClientDisconnected;
            _socketServer.Start();
        }

        private void InitializeComponent()
        {
            this.clientListView = new MaterialListView();
            this.btnLockClient = new MaterialButton();
            this.btnUnlockClient = new MaterialButton();
            this.btnRestartClient = new MaterialButton();
            this.lblActiveSessions = new MaterialLabel();
            
            this.SuspendLayout();

            // Client List View
            this.clientListView.BorderStyle = BorderStyle.None;
            this.clientListView.Columns.AddRange(new ColumnHeader[] {
                new ColumnHeader() { Text = "Client ID", Width = 150 },
                new ColumnHeader() { Text = "Username", Width = 150 },
                new ColumnHeader() { Text = "Connection Time", Width = 180 },
                new ColumnHeader() { Text = "Session Duration", Width = 150 },
                new ColumnHeader() { Text = "Status", Width = 100 }
            });
            this.clientListView.Dock = DockStyle.Top;
            this.clientListView.Height = 300;
            this.clientListView.FullRowSelect = true;
            this.clientListView.View = View.Details;

            // Command Buttons
            this.btnLockClient.Text = "Lock Client";
            this.btnLockClient.Location = new Point(20, 320);
            this.btnLockClient.Click += BtnLockClient_Click;

            this.btnUnlockClient.Text = "Unlock Client";
            this.btnUnlockClient.Location = new Point(180, 320);
            this.btnUnlockClient.Click += BtnUnlockClient_Click;

            this.btnRestartClient.Text = "Restart Client";
            this.btnRestartClient.Location = new Point(340, 320);
            this.btnRestartClient.Click += BtnRestartClient_Click;

            // Status Label
            this.lblActiveSessions.Text = "Active Sessions: 0";
            this.lblActiveSessions.Location = new Point(20, 380);

            // Main form properties
            this.Text = "Cyber Cafe Management - Server";
            this.WindowState = FormWindowState.Maximized;
            this.Controls.AddRange(new Control[] {
                this.clientListView,
                this.btnLockClient,
                this.btnUnlockClient,
                this.btnRestartClient,
                this.lblActiveSessions
            });
            
            this.ResumeLayout(true);
        }

        // Command Button Handlers
        private void BtnLockClient_Click(object sender, EventArgs e)
        {
            if (clientListView.SelectedItems.Count > 0)
            {
                string clientId = clientListView.SelectedItems[0].Tag.ToString();
                _socketServer.SendCommand(clientId, "COMMAND|LOCK");
            }
        }

        private void BtnUnlockClient_Click(object sender, EventArgs e)
        {
            if (clientListView.SelectedItems.Count > 0)
            {
                string clientId = clientListView.SelectedItems[0].Tag.ToString();
                _socketServer.SendCommand(clientId, "COMMAND|UNLOCK");
            }
        }

        private void BtnRestartClient_Click(object sender, EventArgs e)
        {
            if (clientListView.SelectedItems.Count > 0 && 
                MessageBox.Show("Are you sure you want to restart this client?", 
                "Confirm Restart", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string clientId = clientListView.SelectedItems[0].Tag.ToString();
                _socketServer.SendCommand(clientId, "COMMAND|RESTART");
            }
        }

        // Session Management Methods
        private void HandleClientConnected(string clientId, string username)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(HandleClientConnected), clientId, username);
                return;
            }

            var session = new SessionInfo
            {
                ClientId = clientId,
                Username = username,
                StartTime = DateTime.Now,
                IsActive = true
            };

            _activeSessions[clientId] = session;
            UpdateClientList();
            UpdateSessionCount();
        }

        private void HandleClientDisconnected(string clientId)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(HandleClientDisconnected), clientId);
                return;
            }

            if (_activeSessions.TryGetValue(clientId, out var session))
            {
                session.EndTime = DateTime.Now;
                session.TotalTime = (int)(session.EndTime.Value - session.StartTime).TotalSeconds;
                session.IsActive = false;
                SaveSessionToDatabase(session);
                _activeSessions.Remove(clientId);
            }

            UpdateClientList();
            UpdateSessionCount();
        }

        private void UpdateClientList()
        {
            clientListView.Items.Clear();
            foreach (var session in _activeSessions.Values)
            {
                var item = new ListViewItem(new[] {
                    session.ClientId,
                    session.Username,
                    session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    session.IsActive ? (DateTime.Now - session.StartTime).ToString(@"hh\:mm\:ss") : "Ended",
                    session.IsActive ? "Active" : "Inactive"
                });
                item.Tag = session.ClientId;
                clientListView.Items.Add(item);
            }
        }

        private void UpdateSessionCount()
        {
            lblActiveSessions.Text = $"Active Sessions: {_activeSessions.Count}";
        }

        private void SaveSessionToDatabase(SessionInfo session)
        {
            using var connection = Database.GetConnection();
            connection.Open();
            var command = new SQLiteCommand(
                "INSERT INTO Sessions (UserId, StartTime, EndTime, TotalTime) " +
                "VALUES ((SELECT Id FROM Users WHERE Username = @username), @startTime, @endTime, @totalTime)",
                connection);
            command.Parameters.AddWithValue("@username", session.Username);
            command.Parameters.AddWithValue("@startTime", session.StartTime);
            command.Parameters.AddWithValue("@endTime", session.EndTime);
            command.Parameters.AddWithValue("@totalTime", session.TotalTime);
            command.ExecuteNonQuery();
        }

        private class SessionInfo
        {
            public string ClientId { get; set; }
            public string Username { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public int? TotalTime { get; set; }
            public bool IsActive { get; set; }
        }
    }
}