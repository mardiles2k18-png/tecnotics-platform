using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TecnoticsDock
{
    /// <summary>
    /// Indicador en pantalla que aparece al usar un atajo: muestra la app y su volumen
    /// en la pantalla donde esta esa app, sin robar el foco (el juego sigue activo).
    /// </summary>
    internal sealed class OsdForm : Form
    {
        private readonly Timer _hideTimer = new Timer { Interval = 1400 };
        private string _title = "";
        private string _detail;
        private Image _icon;
        private float _volume;
        private bool _muted;

        public OsdForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Background;
            AutoScaleMode = AutoScaleMode.None;
            Opacity = 0.94;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            _hideTimer.Tick += (s, e) => { _hideTimer.Stop(); Hide(); };
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // TOOLWINDOW | NOACTIVATE | TOPMOST | TRANSPARENT (los clics lo atraviesan)
                cp.ExStyle |= 0x80 | 0x08000000 | 0x8 | 0x20;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.RoundCorners(Handle);
        }

        private float Dpi => DeviceDpi / 96f;

        public void ShowVolume(string title, Image icon, float volume, bool muted, Screen screen) =>
            Present(title, icon, null, volume, muted, screen);

        public void ShowMessage(string title, Image icon, string detail, Screen screen) =>
            Present(title, icon, detail, 0, false, screen);

        private void Present(string title, Image icon, string detail, float volume, bool muted, Screen screen)
        {
            _title = title;
            _icon = icon;
            _detail = detail;
            _volume = volume;
            _muted = muted;

            var s = Dpi;
            var area = screen.WorkingArea;
            Size = new Size((int)(320 * s), (int)(64 * s));
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Bottom - Height - (int)(80 * s));
            Invalidate();
            if (!Visible) Show();
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var s = Dpi;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Background);
            using (var p = new Pen(Theme.Border))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            var iconSize = (int)(32 * s);
            var iconRect = new Rectangle((int)(16 * s), (Height - iconSize) / 2, iconSize, iconSize);
            if (_icon != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_icon, iconRect);
            }

            var left = iconRect.Right + (int)(14 * s);
            var right = Width - (int)(16 * s);
            var valueW = (int)(56 * s);
            TextRenderer.DrawText(g, _title, Theme.TitleFont,
                new Rectangle(left, (int)(10 * s), right - left - valueW, (int)(22 * s)), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (_detail != null)
            {
                TextRenderer.DrawText(g, _detail, Theme.BodyFont,
                    new Rectangle(left, (int)(32 * s), right - left, (int)(20 * s)), Theme.SubText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            TextRenderer.DrawText(g, _muted ? "Silenciado" : $"{Math.Round(_volume * 100)}%", Theme.TitleFont,
                new Rectangle(right - valueW, (int)(10 * s), valueW, (int)(22 * s)), _muted ? Theme.Danger : Theme.Text,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var bar = new RectangleF(left, 40 * s, right - left, 6 * s);
            using (var b = new SolidBrush(Theme.Track))
            using (var path = Theme.RoundedRect(bar, bar.Height / 2))
                g.FillPath(b, path);
            if (_volume > 0)
            {
                using (var b = new SolidBrush(_muted ? Theme.AccentDim : Theme.Accent))
                using (var path = Theme.RoundedRect(new RectangleF(bar.X, bar.Y, Math.Max(bar.Height, bar.Width * _volume), bar.Height), bar.Height / 2))
                    g.FillPath(b, path);
            }
        }
    }
}
