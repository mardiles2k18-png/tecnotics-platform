using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TecnoticsDock
{
    /// <summary>
    /// Fila del mezclador dibujada a mano: icono, nombre, barra de volumen y porcentaje.
    /// Arrastrar o usar la rueda cambia el volumen; clic en el icono o clic derecho silencia.
    /// </summary>
    internal sealed class VolumeRow : Control
    {
        private IVolumeTarget _target;
        private float _volume;
        private bool _muted;
        private float _peak;
        private bool _dragging;
        private bool _hover;
        private bool _selected;

        public VolumeRow()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            // Sin ControlStyles.Selectable: tomar el foco activaria la ventana y le quitaria el teclado al juego.
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Background;
            Cursor = Cursors.Hand;
        }

        /// <summary>Se dispara cuando el usuario interactua con la fila (para marcarla como seleccionada).</summary>
        public event EventHandler Activated;

        public int StepPercent { get; set; } = 5;

        public IVolumeTarget Target
        {
            get => _target;
            set { _target = value; SyncFromTarget(); }
        }

        public bool Selected
        {
            get => _selected;
            set { if (_selected != value) { _selected = value; Invalidate(); } }
        }

        public static int RowHeight(float scale) => (int)(52 * scale);

        private float Dpi => DeviceDpi / 96f;

        /// <summary>Relee volumen y silencio del sistema (si el usuario no esta arrastrando).</summary>
        public void SyncFromTarget()
        {
            if (_target == null || _dragging) return;
            var v = _target.Volume;
            var m = _target.Muted;
            if (Math.Abs(v - _volume) > 0.001f || m != _muted)
            {
                _volume = v;
                _muted = m;
                Invalidate();
            }
        }

        public void UpdatePeak()
        {
            if (_target == null) return;
            var p = _muted ? 0 : _target.Peak;
            // Caida suave para que el medidor no parpadee.
            p = Math.Max(p, _peak * 0.75f);
            if (Math.Abs(p - _peak) > 0.004f)
            {
                _peak = p;
                Invalidate(MeterRect);
            }
        }

        public void Nudge(int deltaPercent)
        {
            if (_target == null) return;
            SetVolume((float)Math.Round(_volume * 100 + deltaPercent) / 100f);
        }

        public void ToggleMute()
        {
            if (_target == null) return;
            _muted = !_muted;
            _target.Muted = _muted;
            Invalidate();
        }

        private void SetVolume(float v)
        {
            v = v < 0 ? 0 : v > 1 ? 1 : v;
            // Subir el volumen de algo silenciado lo vuelve a activar.
            if (_muted && v > _volume)
            {
                _muted = false;
                _target.Muted = false;
            }
            _volume = v;
            _target.Volume = v;
            Invalidate();
        }

        // ----- Geometria -----

        private Rectangle IconRect
        {
            get
            {
                var s = (int)(28 * Dpi);
                return new Rectangle((int)(14 * Dpi), (Height - s) / 2, s, s);
            }
        }

        private Rectangle PercentRect => new Rectangle(Width - (int)(56 * Dpi), 0, (int)(44 * Dpi), Height);

        private Rectangle SliderRect
        {
            get
            {
                var left = (int)(56 * Dpi);
                return new Rectangle(left, (int)(32 * Dpi), PercentRect.Left - left - (int)(8 * Dpi), (int)(4 * Dpi));
            }
        }

        private Rectangle MeterRect
        {
            get
            {
                var s = SliderRect;
                return new Rectangle(s.Left, s.Bottom + (int)(5 * Dpi), s.Width, Math.Max(2, (int)(2 * Dpi)));
            }
        }

        private Rectangle SliderHitRect
        {
            get
            {
                var s = SliderRect;
                return new Rectangle(s.Left - (int)(8 * Dpi), (int)(22 * Dpi), s.Width + (int)(16 * Dpi), Height - (int)(22 * Dpi));
            }
        }

        // ----- Mouse -----

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_target == null) return;
            Activated?.Invoke(this, EventArgs.Empty);

            if (e.Button == MouseButtons.Right || (e.Button == MouseButtons.Left && IconRect.Contains(e.Location)))
            {
                ToggleMute();
            }
            else if (e.Button == MouseButtons.Left && SliderHitRect.Contains(e.Location))
            {
                _dragging = true;
                Capture = true;
                SetVolumeFromX(e.X);
            }
            else if (e.Button == MouseButtons.Middle)
            {
                SetVolume(1f);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging) SetVolumeFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_target == null) return;
            Activated?.Invoke(this, EventArgs.Empty);
            var step = (ModifierKeys & Keys.Shift) != 0 ? 1 : StepPercent;
            Nudge(e.Delta > 0 ? step : -step);
            if (e is HandledMouseEventArgs h) h.Handled = true;
        }

        private void SetVolumeFromX(int x)
        {
            var s = SliderRect;
            SetVolume((float)Math.Round((x - s.Left) / (float)s.Width * 100) / 100f);
        }

        // ----- Pintado -----

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var bg = _selected ? Theme.Selected : _hover ? Theme.Hover : Theme.Background;
            g.Clear(Theme.Background);
            if (bg != Theme.Background)
            {
                using (var b = new SolidBrush(bg))
                using (var path = Theme.RoundedRect(new RectangleF(4 * Dpi, 1, Width - 8 * Dpi, Height - 2), 6 * Dpi))
                    g.FillPath(b, path);
            }
            if (_target == null) return;

            // Icono (atenuado y tachado si esta silenciado)
            var icon = IconRect;
            if (_muted)
            {
                using (var attrs = new System.Drawing.Imaging.ImageAttributes())
                {
                    var m = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.35f };
                    attrs.SetColorMatrix(m);
                    g.DrawImage(_target.Icon, icon, 0, 0, _target.Icon.Width, _target.Icon.Height, GraphicsUnit.Pixel, attrs);
                }
                using (var p = new Pen(Theme.Danger, 2.5f * Dpi))
                    g.DrawLine(p, icon.Left + 2, icon.Bottom - 2, icon.Right - 2, icon.Top + 2);
            }
            else
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_target.Icon, icon);
            }

            // Nombre
            var slider = SliderRect;
            var titleFont = _target.IsDevice ? Theme.TitleFont : Theme.BodyFont;
            var titleRect = new Rectangle(slider.Left, (int)(7 * Dpi), PercentRect.Right - slider.Left, (int)(20 * Dpi));
            TextRenderer.DrawText(g, _target.Title, titleFont, titleRect,
                _muted ? Theme.SubText : Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // Barra
            var radius = slider.Height / 2f;
            using (var b = new SolidBrush(Theme.Track))
            using (var path = Theme.RoundedRect(slider, radius))
                g.FillPath(b, path);
            var filled = (int)(slider.Width * _volume);
            var accent = _muted ? Theme.AccentDim : Theme.Accent;
            if (filled > 0)
            {
                using (var b = new SolidBrush(accent))
                using (var path = Theme.RoundedRect(new RectangleF(slider.Left, slider.Top, filled, slider.Height), radius))
                    g.FillPath(b, path);
            }
            var knob = 14 * Dpi;
            var cx = slider.Left + filled;
            var cy = slider.Top + slider.Height / 2f;
            using (var b = new SolidBrush(Theme.Background))
                g.FillEllipse(b, cx - knob / 2, cy - knob / 2, knob, knob);
            using (var b = new SolidBrush(accent))
                g.FillEllipse(b, cx - knob / 2 + 2 * Dpi, cy - knob / 2 + 2 * Dpi, knob - 4 * Dpi, knob - 4 * Dpi);

            // Medidor de nivel: muestra que programa esta sonando ahora mismo.
            var meter = MeterRect;
            var level = (int)(meter.Width * Math.Min(1f, _peak) * (_muted ? 0 : 1));
            if (level > 0)
            {
                using (var b = new SolidBrush(Color.FromArgb(170, Theme.Meter)))
                    g.FillRectangle(b, meter.Left, meter.Top, level, meter.Height);
            }

            // Porcentaje
            TextRenderer.DrawText(g, _muted ? "mute" : ((int)Math.Round(_volume * 100)).ToString(),
                _muted ? Theme.SmallFont : Theme.TitleFont, PercentRect, _muted ? Theme.Danger : Theme.Text,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
