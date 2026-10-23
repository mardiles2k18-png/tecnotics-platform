using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace VolumeMixer
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(32, 32, 32);
        public static readonly Color Border = Color.FromArgb(58, 58, 58);
        public static readonly Color Hover = Color.FromArgb(44, 44, 44);
        public static readonly Color Selected = Color.FromArgb(52, 52, 52);
        public static readonly Color Text = Color.FromArgb(240, 240, 240);
        public static readonly Color SubText = Color.FromArgb(160, 160, 160);
        public static readonly Color Accent = Color.FromArgb(76, 194, 255);
        public static readonly Color AccentDim = Color.FromArgb(110, 110, 110);
        public static readonly Color Track = Color.FromArgb(80, 80, 80);
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
        public static readonly Image Bell = Draw(DrawBell);
        public static readonly Image App = Draw(DrawApp);

        public static Icon AppIcon
        {
            get
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
                    return s != null ? new Icon(s) : SystemIcons.Application;
            }
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
    }
}
