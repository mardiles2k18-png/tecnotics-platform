using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TecnoticsDock
{
    /// <summary>
    /// Fila generica del panel: icono, titulo, detalle y hasta dos botones a la derecha
    /// (Expulsar, Conectar, Abrir / Cerrar...). Clic en la fila = clic en el boton principal.
    /// </summary>
    internal sealed class ActionRow : Control
    {
        private Image _icon;
        private string _title = "";
        private string _detail;
        private Color? _detailColor;
        private string _button;
        private string _button2;
        private bool _button2Danger;
        private bool _busy;
        private bool _hover;
        private int _hoverButton; // 0 ninguno, 1 principal, 2 secundario
        private bool _highlight;

        public ActionRow()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Background;
        }

        /// <summary>Boton principal (el de la derecha).</summary>
        public event EventHandler ButtonClick;

        /// <summary>Boton secundario (a la izquierda del principal).</summary>
        public event EventHandler Button2Click;

        /// <summary>Clic derecho en cualquier parte de la fila.</summary>
        public event EventHandler RightClick;

        /// <summary>Deja un espacio extra antes de esta fila (separa secciones).</summary>
        public bool GapBefore { get; set; }

        /// <summary>Si es false, solo los botones responden al clic (no toda la fila).</summary>
        public bool RowClickable { get; set; } = true;

        public void Set(Image icon, string title, string detail, string button, Color? detailColor = null, bool highlight = false,
            string button2 = null, bool button2Danger = false)
        {
            if (_icon == icon && _title == title && _detail == detail && _button == button && _detailColor == detailColor &&
                _highlight == highlight && _button2 == button2 && _button2Danger == button2Danger)
                return;
            _icon = icon;
            _title = title ?? "";
            _detail = detail;
            _button = button;
            _button2 = button2;
            _button2Danger = button2Danger;
            _detailColor = detailColor;
            _highlight = highlight;
            Cursor = button != null && RowClickable ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        /// <summary>Mientras esta ocupado (expulsando, conectando) los botones no responden.</summary>
        public bool Busy
        {
            get => _busy;
            set { if (_busy != value) { _busy = value; Invalidate(); } }
        }

        private float Dpi => DeviceDpi / 96f;

        private Rectangle MakeButton(string text, int right)
        {
            var size = TextRenderer.MeasureText(text, Theme.BodyFont);
            var w = size.Width + (int)(18 * Dpi);
            var h = (int)(28 * Dpi);
            return new Rectangle(right - w, (Height - h) / 2, w, h);
        }

        private Rectangle ButtonRect => _button == null ? Rectangle.Empty : MakeButton(_button, Width - (int)(14 * Dpi));

        private Rectangle Button2Rect
        {
            get
            {
                if (_button2 == null) return Rectangle.Empty;
                var right = _button == null ? Width - (int)(14 * Dpi) : ButtonRect.Left - (int)(6 * Dpi);
                return MakeButton(_button2, right);
            }
        }

        private int HitButton(Point p) => ButtonRect.Contains(p) ? 1 : Button2Rect.Contains(p) ? 2 : 0;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var over = HitButton(e.Location);
            if (!_hover || over != _hoverButton) { _hover = true; _hoverButton = over; Invalidate(); }
            Cursor = over != 0 || (_button != null && RowClickable) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            _hoverButton = 0;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                RightClick?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (e.Button != MouseButtons.Left || _busy) return;
            var hit = HitButton(e.Location);
            if (hit == 2) Button2Click?.Invoke(this, EventArgs.Empty);
            else if (_button != null && (hit == 1 || RowClickable)) ButtonClick?.Invoke(this, EventArgs.Empty);
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
            var btn2 = Button2Rect;
            var firstButton = !btn2.IsEmpty ? btn2 : btn;
            var right = (firstButton.IsEmpty ? Width - (int)(14 * s) : firstButton.Left) - (int)(8 * s);
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

            DrawButton(g, btn, _button, _hoverButton == 1, false);
            DrawButton(g, btn2, _button2, _hoverButton == 2, _button2Danger);
        }

        private void DrawButton(Graphics g, Rectangle rect, string text, bool hover, bool danger)
        {
            if (rect.IsEmpty) return;
            var hoverColor = danger ? Theme.Danger : Theme.Accent;
            var fill = _busy ? Theme.Track : hover ? hoverColor : Theme.Selected;
            using (var b = new SolidBrush(fill))
            using (var path = Theme.RoundedRect(rect, 5 * Dpi))
                g.FillPath(b, path);
            var color = hover && !_busy ? Theme.Background : danger ? Theme.Danger : Theme.Text;
            TextRenderer.DrawText(g, _busy ? "…" : text, Theme.BodyFont, rect, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>Franja superior con el logo y el nombre Tecnotics Dock.</summary>
    internal sealed class BrandHeader : Control
    {
        public BrandHeader()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Background;
        }

        private static readonly Font BrandFont = new Font("Segoe UI Black", 10.5f);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var s = DeviceDpi / 96f;
            g.Clear(Theme.Background);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var logo = (int)(22 * s);
            var x = (int)(16 * s);
            g.DrawImage(Icons.Logo, x, (Height - logo) / 2 + (int)(2 * s), logo, logo);
            x += logo + (int)(8 * s);

            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            var bounds = new Rectangle(x, (int)(2 * s), Width - x, Height);
            TextRenderer.DrawText(g, "TECNOTICS", BrandFont, bounds, Theme.Accent, flags);
            x += TextRenderer.MeasureText(g, "TECNOTICS", BrandFont, Size.Empty, flags).Width + (int)(5 * s);
            TextRenderer.DrawText(g, "DOCK", BrandFont, new Rectangle(x, (int)(2 * s), Width - x, Height), Theme.Orange, flags);
        }
    }

    /// <summary>Pestanas del panel: Volumen / Discos / Wi-Fi / Apps.</summary>
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

    /// <summary>Titulo pequeno que separa secciones dentro de una pestana.</summary>
    internal sealed class SectionRow : Control
    {
        public SectionRow()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Background;
        }

        public static int RowHeight(float scale) => (int)(28 * scale);

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var s = DeviceDpi / 96f;
            e.Graphics.Clear(Theme.Background);
            TextRenderer.DrawText(e.Graphics, Text, Theme.SmallFont,
                new Rectangle((int)(16 * s), 0, Width - (int)(32 * s), Height), Theme.SubText,
                TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
