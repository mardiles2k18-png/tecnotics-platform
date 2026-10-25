using System;
using System.Drawing;
using System.Windows.Forms;

namespace TecnoticsDock
{
    /// <summary>Pide la contraseña de una red Wi‑Fi nueva.</summary>
    internal sealed class PasswordForm : Form
    {
        private readonly TextBox _password;

        public PasswordForm(string ssid, Screen screen)
        {
            var s = DeviceDpi / 96f;
            Text = "Conectar a " + ssid;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.BodyFont;
            Icon = Icons.AppIcon;
            ClientSize = new Size((int)(340 * s), (int)(150 * s));
            Location = new Point(screen.WorkingArea.Left + (screen.WorkingArea.Width - Width) / 2,
                                 screen.WorkingArea.Top + (screen.WorkingArea.Height - Height) / 2);

            var label = new Label
            {
                Text = $"Contraseña de «{ssid}»",
                AutoEllipsis = true,
                Bounds = new Rectangle((int)(16 * s), (int)(14 * s), ClientSize.Width - (int)(32 * s), (int)(22 * s)),
            };
            _password = new TextBox
            {
                UseSystemPasswordChar = true,
                BackColor = Theme.Selected,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Bounds = new Rectangle((int)(16 * s), (int)(40 * s), ClientSize.Width - (int)(32 * s), (int)(26 * s)),
            };
            var show = new CheckBox
            {
                Text = "Mostrar contraseña",
                Bounds = new Rectangle((int)(16 * s), (int)(72 * s), (int)(200 * s), (int)(24 * s)),
            };
            show.CheckedChanged += (sender, e) => _password.UseSystemPasswordChar = !show.Checked;

            var ok = MakeButton("Conectar", DialogResult.OK, ClientSize.Width - (int)(196 * s), (int)(108 * s), s);
            var cancel = MakeButton("Cancelar", DialogResult.Cancel, ClientSize.Width - (int)(104 * s), (int)(108 * s), s);
            ok.BackColor = Theme.Accent;
            ok.ForeColor = Theme.Background;
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { label, _password, show, ok, cancel });
        }

        public string Password => _password.Text;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            _password.Focus();
        }

        private static Button MakeButton(string text, DialogResult result, int x, int y, float s) => new Button
        {
            Text = text,
            DialogResult = result,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Selected,
            ForeColor = Theme.Text,
            Bounds = new Rectangle(x, y, (int)(88 * s), (int)(30 * s)),
            FlatAppearance = { BorderSize = 0 },
        };
    }
}
