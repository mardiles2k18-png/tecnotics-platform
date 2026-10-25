using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace TecnoticsDock
{
    internal static class Theme
    {
        // Paleta de marca Tecnotics (docs/brand.md): celeste #69C7D8, naranja #F7931A, grafito #111318.
        public static readonly Color Background = Color.FromArgb(22, 25, 31);
        public static readonly Color Border = Color.FromArgb(48, 54, 64);
        public static readonly Color Hover = Color.FromArgb(32, 37, 45);
        public static readonly Color Selected = Color.FromArgb(40, 46, 56);
        public static readonly Color Text = Color.FromArgb(240, 242, 245);
        public static readonly Color SubText = Color.FromArgb(150, 158, 170);
        public static readonly Color Accent = Color.FromArgb(105, 199, 216);
        public static readonly Color Orange = Color.FromArgb(247, 147, 26);
        public static readonly Color AccentDim = Color.FromArgb(100, 108, 120);
        public static readonly Color Track = Color.FromArgb(58, 64, 74);
        public static readonly Color Meter = Color.FromArgb(120, 220, 140);
        public static readonly Color Danger = Color.FromArgb(255, 99, 99);

        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 9.75f);
        public static readonly Font BodyFont = new Font("Segoe UI", 9f);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.25f);

        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>Iconos dibujados por codigo, sin archivos externos.</summary>
    internal static class Icons
    {
        public static readonly Image Speaker = Draw(DrawSpeaker);
        public static readonly Image Logo = LoadLogo();
        public static readonly Image Bell = Draw(DrawBell);
        public static readonly Image App = Draw(DrawApp);
        public static readonly Image Drive = Draw(DrawDrive);
        public static readonly Image Warning = Draw(DrawWarning);
        public static readonly Image WifiOff = Draw(g => DrawWifi(g, 0, true));
        public static readonly Image WifiDisconnected = Draw(g => DrawWifi(g, 0, false));
        private static readonly Image[] WifiBars = { Draw(g => DrawWifi(g, 1, false)), Draw(g => DrawWifi(g, 2, false)), Draw(g => DrawWifi(g, 3, false)), Draw(g => DrawWifi(g, 4, false)) };

        /// <summary>Icono de Wi‑Fi con 1 a 4 barras.</summary>
        public static Image Wifi(int bars) => WifiBars[Math.Max(1, Math.Min(4, bars)) - 1];

        public static Icon AppIcon
        {
            get
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
                    return s != null ? new Icon(s) : SystemIcons.Application;
            }
        }

        /// <summary>Chip del logo Tecnotics (assets/logo.png embebido).</summary>
        private static Image LoadLogo()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"))
                return s != null ? new Bitmap(s) : Draw(DrawSpeaker);
        }

        private static Image Draw(Action<Graphics> paint)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                paint(g);
            }
            return bmp;
        }

        private static void DrawSpeaker(Graphics g)
        {
            using (var b = new SolidBrush(Theme.Text))
                g.FillPolygon(b, new[] { new PointF(4, 12), new PointF(10, 12), new PointF(17, 5), new PointF(17, 27), new PointF(10, 20), new PointF(4, 20) });
            using (var p = new Pen(Theme.Accent, 2.4f))
            {
                g.DrawArc(p, 13, 10, 10, 12, -55, 110);
                g.DrawArc(p, 11, 5, 18, 22, -55, 110);
            }
        }

        private static void DrawBell(Graphics g)
        {
            using (var b = new SolidBrush(Theme.Text))
            {
                g.FillPie(b, 7, 5, 18, 22, 180, 180);
                g.FillRectangle(b, 7, 15, 18, 7);
                g.FillRectangle(b, 4, 21, 24, 3);
                g.FillEllipse(b, 13, 24, 6, 5);
            }
        }

        private static void DrawApp(Graphics g)
        {
            using (var b = new SolidBrush(Theme.Track))
            using (var path = Theme.RoundedRect(new RectangleF(3, 5, 26, 22), 4))
                g.FillPath(b, path);
            using (var b = new SolidBrush(Theme.Accent))
                g.FillRectangle(b, 3, 5, 26, 5);
        }
    
        private static void DrawDrive(Graphics g)
        {
            using (var b = new SolidBrush(Theme.Text))
            using (var path = Theme.RoundedRect(new RectangleF(3, 9, 26, 14), 3))
                g.FillPath(b, path);
            using (var b = new SolidBrush(Theme.Background))
                g.FillRectangle(b, 6, 15, 12, 2);
            using (var b = new SolidBrush(Theme.Meter))
                g.FillEllipse(b, 22, 14, 4, 4);
        }

        private static void DrawWarning(Graphics g)
        {
            using (var b = new SolidBrush(Color.FromArgb(255, 196, 0)))
                g.FillPolygon(b, new[] { new PointF(16, 3), new PointF(30, 28), new PointF(2, 28) });
            using (var b = new SolidBrush(Theme.Background))
            {
                g.FillRectangle(b, 14.5f, 11, 3, 10);
                g.FillEllipse(b, 14.3f, 22.5f, 3.4f, 3.4f);
            }
        }

        private static void DrawWifi(Graphics g, int bars, bool off)
        {
            const float cx = 16, cy = 26;
            using (var on = new SolidBrush(off ? Theme.SubText : Theme.Text))
            using (var dim = new SolidBrush(Theme.Track))
                g.FillEllipse(bars >= 1 ? on : dim, cx - 2.5f, cy - 2.5f, 5, 5);
            float[] radii = { 9, 15, 21 };
            for (var i = 0; i < radii.Length; i++)
            {
                var r = radii[i];
                using (var p = new Pen(!off && bars >= i + 2 ? Theme.Text : Theme.Track, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(p, cx - r, cy - r, r * 2, r * 2, 225, 90);
            }
            if (off)
            {
                using (var p = new Pen(Theme.Danger, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(p, 5, 5, 27, 27);
            }
        }
    }

    /// <summary>Menus contextuales oscuros con los colores de la marca.</summary>
    internal sealed class DarkMenuRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new Colors()) { RoundedEdges = false; }

        protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.ForeColor != Theme.Danger) e.TextColor = e.Item.Enabled ? Theme.Text : Theme.AccentDim;
            base.OnRenderItemText(e);
        }

        private sealed class Colors : System.Windows.Forms.ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Theme.Background;
            public override Color MenuBorder => Theme.Border;
            public override Color MenuItemBorder => Theme.Selected;
            public override Color MenuItemSelected => Theme.Selected;
            public override Color MenuItemSelectedGradientBegin => Theme.Selected;
            public override Color MenuItemSelectedGradientEnd => Theme.Selected;
            public override Color ImageMarginGradientBegin => Theme.Background;
            public override Color ImageMarginGradientMiddle => Theme.Background;
            public override Color ImageMarginGradientEnd => Theme.Background;
            public override Color SeparatorDark => Theme.Border;
            public override Color SeparatorLight => Theme.Background;
        }
    }
}
