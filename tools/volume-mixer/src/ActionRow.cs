using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VolumeMixer
{
    /// <summary>
    /// Fila generica del panel: icono, titulo, detalle y un boton a la derecha
    /// (Expulsar, Conectar, Activar...). Clic en la fila = clic en el boton.
    /// </summary>
    internal sealed class ActionRow : Control
    {
        private Image _icon;
        private string _title = "";
        private string _detail;
        private Color? _detailColor;
        private string _button;
        private bool _busy;
        private bool _hover;
        private bool _hoverButton;
        private bool _highlight;

        public ActionRow()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Background;
        }

        public event EventHandler ButtonClick;

        /// <summary>Deja un espacio extra antes de esta fila (separa secciones).</summary>
        public bool GapBefore { get; set; }

        /// <summary>Si es false, solo el boton responde al clic (no toda la fila).</summary>
        public bool RowClickable { get; set; } = true;

        public void Set(Image icon, string title, string detail, string button, Color? detailColor = null, bool highlight = false)
        {
            if (_icon == icon && _title == title && _detail == detail && _button == button && _detailColor == detailColor && _highlight == highlight)
                return;
            _icon = icon;
            _title = title ?? "";
            _detail = detail;
            _button = button;
            _detailColor = detailColor;
            _highlight = highlight;
            Cursor = button != null && RowClickable ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        /// <summary>Mientras esta ocupado (expulsando, conectando) el boton no responde.</summary>
        public bool Busy
        {
            get => _busy;
            set { _busy = value; Invalidate(); }
        }

        private float Dpi => DeviceDpi / 96f;

        private Rectangle ButtonRect
        {
            get
            {
                if (_button == null) return Rectangle.Empty;
                var size = TextRenderer.MeasureText(_button, Theme.BodyFont);
                var w = size.Width + (int)(20 * Dpi);
                var h = (int)(28 * Dpi);
                return new Rectangle(Width - w - (int)(14 * Dpi), (Height - h) / 2, w, h);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var over = ButtonRect.Contains(e.Location);
            if (!_hover || over != _hoverButton) { _hover = true; _hoverButton = over; Invalidate(); }
            Cursor = _button != null && (RowClickable || over) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = _hoverButton = false;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || _button == null || _busy) return;
            if (RowClickable || ButtonRect.Contains(e.Location)) ButtonClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var s = Dpi;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Background);
            if (_hover && _button != null && RowClickable)
            {
                using (var b = new SolidBrush(Theme.Hover))
                using (var path = Theme.RoundedRect(new RectangleF(4 * s, 1, Width - 8 * s, Height - 2), 6 * s))
                    g.FillPath(b, path);
            }

            var iconSize = (int)(28 * s);
            var iconRect = new Rectangle((int)(14 * s), (Height - iconSize) / 2, iconSize, iconSize);
            if (_icon != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_icon, iconRect);
            }

            var left = (int)(56 * s);
            var btn = ButtonRect;
            var right = (btn.IsEmpty ? Width - (int)(14 * s) : btn.Left) - (int)(8 * s);
            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            if (string.IsNullOrEmpty(_detail))
            {
                TextRenderer.DrawText(g, _title, Theme.TitleFont, new Rectangle(left, 0, right - left, Height), _highlight ? Theme.Accent : Theme.Text, flags);
            }
            else
            {
                TextRenderer.DrawText(g, _title, Theme.TitleFont, new Rectangle(left, (int)(6 * s), right - left, (int)(22 * s)), _highlight ? Theme.Accent : Theme.Text, flags);
                TextRenderer.DrawText(g, _detail, Theme.SmallFont, new Rectangle(left, (int)(26 * s), right - left, (int)(20 * s)), _detailColor ?? Theme.SubText, flags);
            }

            if (!btn.IsEmpty)
            {
                var fill = _busy ? Theme.Track : _hoverButton ? Theme.Accent : Theme.Selected;
                using (var b = new SolidBrush(fill))
                using (var path = Theme.RoundedRect(btn, 5 * s))
                    g.FillPath(b, path);
                TextRenderer.DrawText(g, _busy ? "…" : _button, Theme.BodyFont, btn,
                    _hoverButton && !_busy ? Theme.Background : Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }

    /// <summary>Pestanas del panel: Volumen / Discos / Wi-Fi.</summary>
    internal sealed class TabBar : Control
    {
        private readonly string[] _labels;
        private int _hover = -1;
        private int _selected;

        public TabBar(params string[] labels)
        {
            _labels = labels;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Background;
            Cursor = Cursors.Hand;
        }

        public event Action<int> SelectedChanged;

        public int Selected
        {
            get => _selected;
            set { if (_selected != value) { _selected = value; Invalidate(); } }
        }

        private float Dpi => DeviceDpi / 96f;

        private Rectangle TabRect(int i)
        {
            var pad = (int)(10 * Dpi);
            var w = (Width - pad * 2) / _labels.Length;
            return new Rectangle(pad + i * w, (int)(8 * Dpi), w, Height - (int)(12 * Dpi));
        }

        private int HitTest(Point p)
        {
            for (var i = 0; i < _labels.Length; i++)
                if (TabRect(i).Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var h = HitTest(e.Location);
            if (h != _hover) { _hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var h = HitTest(e.Location);
            if (e.Button == MouseButtons.Left && h >= 0 && h != _selected)
            {
                Selected = h;
                SelectedChanged?.Invoke(h);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var s = Dpi;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Background);
            for (var i = 0; i < _labels.Length; i++)
            {
                var r = TabRect(i);
                if (i == _selected || i == _hover)
                {
                    using (var b = new SolidBrush(i == _selected ? Theme.Selected : Theme.Hover))
                    using (var path = Theme.RoundedRect(new RectangleF(r.X + 2 * s, r.Y, r.Width - 4 * s, r.Height), 6 * s))
                        g.FillPath(b, path);
                }
                TextRenderer.DrawText(g, _labels[i], i == _selected ? Theme.TitleFont : Theme.BodyFont, r,
                    i == _selected ? Theme.Text : Theme.SubText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (i == _selected)
                {
                    using (var b = new SolidBrush(Theme.Accent))
                    using (var path = Theme.RoundedRect(new RectangleF(r.X + r.Width / 2f - 10 * s, r.Bottom - 3 * s, 20 * s, 3 * s), 1.5f * s))
                        g.FillPath(b, path);
                }
            }
        }
    }
}
