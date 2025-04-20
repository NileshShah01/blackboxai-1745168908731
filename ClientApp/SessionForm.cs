using System;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using Timer = System.Windows.Forms.Timer;

namespace ClientApp
{
    public partial class SessionForm : MaterialForm
    {
        private readonly MaterialSkinManager materialSkinManager;
        private Timer _sessionTimer;
        private TimeSpan _remainingTime;
        private bool _isSessionActive;

        public SessionForm(TimeSpan sessionDuration)
        {
            InitializeComponent();
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue800, Primary.Blue900,
                Primary.Blue500, Accent.LightBlue200,
                TextShade.WHITE
            );

            _remainingTime = sessionDuration;
            lblTimer.Text = _remainingTime.ToString(@"hh\:mm\:ss");
            _sessionTimer = new Timer();
            _sessionTimer.Interval = 1000; // 1 second
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();
            _isSessionActive = true;
        }

        private MaterialLabel lblTimer;
        private MaterialFlatButton btnLogout;

        private void InitializeComponent()
        {
            this.lblTimer = new MaterialLabel();
            this.btnLogout = new MaterialFlatButton();

            this.SuspendLayout();

            // Timer Label
            this.lblTimer.Location = new System.Drawing.Point(50, 80);
            this.lblTimer.Size = new System.Drawing.Size(300, 50);
            this.lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTimer.Font = new System.Drawing.Font("Roboto", 24F, System.Drawing.FontStyle.Bold);

            // Logout Button
            this.btnLogout.Text = "Logout";
            this.btnLogout.Location = new System.Drawing.Point(50, 150);
            this.btnLogout.Size = new System.Drawing.Size(300, 50);
            this.btnLogout.Click += BtnLogout_Click;

            // Form properties
            this.ClientSize = new System.Drawing.Size(400, 250);
            this.Text = "Session Timer";
            this.Controls.AddRange(new Control[] {
                this.lblTimer,
                this.btnLogout
            });

            this.ResumeLayout(false);
        }

        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime = _remainingTime.Add(TimeSpan.FromSeconds(-1));
                lblTimer.Text = _remainingTime.ToString(@"hh\:mm\:ss");
            }
            else
            {
                EndSession();
            }
        }

        private void EndSession()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(EndSession));
                return;
            }

            _sessionTimer.Stop();
            _isSessionActive = false;
            LockScreen();
        }

        private void LockScreen()
        {
            var lockForm = new Form() {
                Text = "Session Locked",
                Size = new System.Drawing.Size(400, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                ControlBox = false
            };
            
            var label = new Label() {
                Text = "Your session has ended. Please contact an administrator.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Arial", 12)
            };
            
            lockForm.Controls.Add(label);
            lockForm.ShowDialog();
            this.Close();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _sessionTimer.Stop();
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_isSessionActive)
            {
                var result = MessageBox.Show("Are you sure you want to end your session?", 
                    "Confirm Session End", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    _sessionTimer.Stop();
                    LockScreen();
                }
            }
        }
    }
}