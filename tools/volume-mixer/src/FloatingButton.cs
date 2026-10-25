using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VolumeMixer
{
    /// <summary>
    /// Boton redondo siempre visible que se puede arrastrar a cualquier pantalla (p. ej. la secundaria,
    /// que no tiene bandeja del sistema). Clic: abre el mezclador al lado. Clic derecho: menu.
    /// Nunca toma el foco, asi que usarlo no saca al juego de primer plano.
    /// </summary>
    internal sealed class FloatingButton : Form
    {
        private const string RegKey = @"Software\TecnoticsVolumeMixer";
        private const int SizeDip = 44;
        private Point _downScreen;
        private Point _downLocation;
        private bool _pressed;
        private bool _dragging;
        private bool _hover;
        private Image _badge;

        public FloatingButton(ContextMenuStrip menu)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Background;
            ContextMenuStrip = menu;
            Text = "Mezclador de volumen";
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            var size = (int)(SizeDip * DeviceDpi / 96f);
            Size = new Size(size, size);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, size, size);
                Region = new Region(path);
            }
            Location = LoadPosition(size);
        }

        /// <summary>Icono pequeno en la esquina (estado del Wi‑Fi); null para no mostrar nada.</summary>
        public void SetWifi(Image badge)
        {
            if (_badge == badge) return;
            _badge = badge;
            Invalidate();
        }

        /// <summary>Clic izquierdo sin arrastrar.</summary>
        public event EventHandler Pressed;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80 | 0x08000000 | 0x8; // TOOLWINDOW | NOACTIVATE | TOPMOST
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x21 /* WM_MOUSEACTIVATE */)
            {
                m.Result = (IntPtr)3; // MA_NOACTIVATE
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _pressed = true;
            _dragging = false;
            _downScreen = Cursor.Position;
            _downLocation = Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_pressed) return;
            var p = Cursor.Position;
            var dx = p.X - _downScreen.X;
            var dy = p.Y - _downScreen.Y;
            if (!_dragging && Math.Abs(dx) + Math.Abs(dy) > 5) _dragging = true;
            if (_dragging) Location = new Point(_downLocation.X + dx, _downLocation.Y + dy);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_pressed) return;
            _pressed = false;
            Capture = false;
            if (_dragging) SavePosition();
            else Pressed?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_hover ? Theme.Hover : Theme.Background);
            using (var p = new Pen(_hover ? Theme.Accent : Theme.Border, 2))
                g.DrawEllipse(p, 1, 1, Width - 3, Height - 3);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (_badge == null)
            {
                var icon = (int)(Width * 0.6);
                g.DrawImage(Icons.Speaker, (Width - icon) / 2, (Height - icon) / 2, icon, icon);
            }
            else
            {
                // Parlante a la izquierda y Wi‑Fi a la derecha, ambos dentro del circulo.
                var icon = (int)(Width * 0.42);
                var y = (Height - icon) / 2;
                g.DrawImage(Icons.Speaker, Width / 2 - icon + 2, y, icon, icon);
                g.DrawImage(_badge, Width / 2 - 1, y, icon, icon);
            }
        }

        private void SavePosition()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegKey))
                {
                    key.SetValue("ButtonX", Left);
                    key.SetValue("ButtonY", Top);
                }
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        /// <summary>Ultima posicion guardada; si no hay (o la pantalla ya no existe), la esquina de la pantalla secundaria.</summary>
        private static Point LoadPosition(int size)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegKey))
                {
                    if (key?.GetValue("ButtonX") is int x && key.GetValue("ButtonY") is int y)
                    {
                        var rect = new Rectangle(x, y, size, size);
                        if (Screen.AllScreens.Any(s => s.WorkingArea.Contains(rect))) return rect.Location;
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex); }

            var screen = Screen.AllScreens.FirstOrDefault(s => !s.Primary) ?? Screen.PrimaryScreen;
            var area = screen.WorkingArea;
            return new Point(area.Right - size - 16, area.Bottom - size - 16);
        }
    }
}
