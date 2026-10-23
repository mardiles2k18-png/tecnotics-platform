using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VolumeMixer
{
    /// <summary>
    /// Ventana emergente compacta con un control por dispositivo y por aplicacion.
    /// Aparece en la esquina inferior derecha de la pantalla donde esta el mouse
    /// y se oculta sola al hacer clic fuera.
    /// </summary>
    internal sealed class MixerForm : Form
    {
        private const int WidthDip = 380;
        private readonly AudioService _audio;
        private readonly Settings _settings;
        private readonly Panel _list = new Panel { AutoScroll = true, BackColor = Theme.Background };
        private readonly Label _header = new Label { ForeColor = Theme.Text, Font = Theme.TitleFont, BackColor = Theme.Background };
        private readonly Label _footer = new Label { ForeColor = Theme.SubText, Font = Theme.SmallFont, BackColor = Theme.Background };
        private readonly Timer _refreshTimer = new Timer { Interval = 1000 };
        private readonly Timer _meterTimer = new Timer { Interval = 50 };
        private readonly Dictionary<string, VolumeRow> _rows = new Dictionary<string, VolumeRow>();
        private List<VolumeRow> _ordered = new List<VolumeRow>();
        private int _selected;
        private int _anchorBottom;
        private Rectangle _area;

        public MixerForm(AudioService audio, Settings settings)
        {
            _audio = audio;
            _settings = settings;

            Text = "Mezclador de volumen";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Background;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            Icon = Icons.AppIcon;

            _header.Text = "Volumen por aplicación";
            _header.TextAlign = ContentAlignment.MiddleLeft;
            _footer.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_list);
            Controls.Add(_header);
            Controls.Add(_footer);

            _refreshTimer.Tick += (s, e) => Rebuild();
            _meterTimer.Tick += (s, e) => { foreach (var r in _ordered) r.UpdatePeak(); };
        }

        /// <summary>Momento en que se oculto por perder el foco (para no reabrir con el mismo clic).</summary>
        public DateTime LastHiddenUtc { get; private set; }

        private float Dpi => DeviceDpi / 96f;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;        // WS_EX_TOOLWINDOW: no aparece en Alt+Tab
                cp.ClassStyle |= 0x20000;  // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.RoundCorners(Handle);
        }

        public void Toggle()
        {
            if (Visible) HideMixer();
            else ShowMixer();
        }

        public void ShowMixer()
        {
            _area = Native.WorkingAreaAtCursor();
            Width = (int)(WidthDip * Dpi);
            Left = _area.Right - Width - (int)(12 * Dpi);
            _anchorBottom = _area.Bottom - (int)(12 * Dpi);
            _selected = 0;
            Rebuild();

            Show();
            Activate();
            _refreshTimer.Start();
            _meterTimer.Start();
        }

        public void HideMixer()
        {
            _refreshTimer.Stop();
            _meterTimer.Stop();
            Hide();
        }

        /// <summary>Vuelve a leer dispositivos y sesiones, reutilizando las filas existentes.</summary>
        public void Rebuild()
        {
            List<DeviceTarget> devices;
            try { devices = _audio.GetSnapshot(); }
            catch (Exception ex) { Log.Error(ex); return; }

            var targets = new List<IVolumeTarget>();
            foreach (var d in devices)
            {
                // Los dispositivos secundarios sin nada sonando solo agregan ruido.
                if (!d.IsDefault && d.Apps.Count == 0) continue;
                targets.Add(d);
                targets.AddRange(d.Apps);
            }

            var keys = new HashSet<string>(targets.Select(t => t.Key));
            foreach (var key in _rows.Keys.Where(k => !keys.Contains(k)).ToList())
            {
                _rows[key].Dispose();
                _rows.Remove(key);
            }

            var ordered = new List<VolumeRow>();
            foreach (var t in targets)
            {
                if (!_rows.TryGetValue(t.Key, out var row))
                {
                    row = new VolumeRow();
                    row.Activated += (s, e) => SelectRow(_ordered.IndexOf((VolumeRow)s));
                    _rows[t.Key] = row;
                    _list.Controls.Add(row);
                }
                row.StepPercent = _settings.StepPercent;
                row.Target = t;
                ordered.Add(row);
            }
            _ordered = ordered;
            SelectRow(Math.Min(_selected, _ordered.Count - 1));
            LayoutRows();
        }

        private void LayoutRows()
        {
            var s = Dpi;
            var rowH = VolumeRow.RowHeight(s);
            var headerH = (int)(40 * s);
            var footerH = (int)(30 * s);
            var gap = (int)(6 * s);

            // Altura de cada fila + un pequeno espacio antes de cada dispositivo (salvo el primero).
            var contentH = 0;
            for (var i = 0; i < _ordered.Count; i++)
                contentH += rowH + (i > 0 && _ordered[i].Target.IsDevice ? gap * 2 : 0);

            var maxListH = (int)(_area.Height * 0.75) - headerH - footerH;
            var listH = Math.Max(rowH, Math.Min(contentH, maxListH));
            var scroll = contentH > listH;

            Height = headerH + listH + footerH;
            Top = _anchorBottom - Height;

            _header.SetBounds((int)(18 * s), 0, Width - (int)(36 * s), headerH);
            _list.SetBounds(0, headerH, Width, listH);
            _footer.SetBounds((int)(18 * s), headerH + listH, Width - (int)(36 * s), footerH);
            _footer.Text = $"Rueda: ±{_settings.StepPercent}%   Clic der.: silenciar   ↑↓←→ / M";

            var rowW = Width - (scroll ? SystemInformation.VerticalScrollBarWidth : 0);
            var y = _list.AutoScrollPosition.Y;
            for (var i = 0; i < _ordered.Count; i++)
            {
                if (i > 0 && _ordered[i].Target.IsDevice) y += gap * 2;
                _ordered[i].SetBounds(0, y, rowW, rowH);
                y += rowH;
            }
            Invalidate();
        }

        private void SelectRow(int index)
        {
            if (_ordered.Count == 0) return;
            _selected = Math.Max(0, Math.Min(index, _ordered.Count - 1));
            for (var i = 0; i < _ordered.Count; i++) _ordered[i].Selected = i == _selected;
            _list.ScrollControlIntoView(_ordered[_selected]);
        }

        private VolumeRow SelectedRow => _selected >= 0 && _selected < _ordered.Count ? _ordered[_selected] : null;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var step = (keyData & Keys.Shift) != 0 ? 1 : _settings.StepPercent;
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Escape: HideMixer(); return true;
                case Keys.Up: SelectRow(_selected - 1); return true;
                case Keys.Down: SelectRow(_selected + 1); return true;
                case Keys.Tab: SelectRow((_selected + 1) % Math.Max(1, _ordered.Count)); return true;
                case Keys.Right: SelectedRow?.Nudge(step); return true;
                case Keys.Left: SelectedRow?.Nudge(-step); return true;
                case Keys.M:
                case Keys.Space: SelectedRow?.ToggleMute(); return true;
            }

            // Teclas 1..9 fijan 10%..90%, 0 fija 100%.
            var code = keyData & Keys.KeyCode;
            var digit = code >= Keys.D0 && code <= Keys.D9 ? code - Keys.D0
                : code >= Keys.NumPad0 && code <= Keys.NumPad9 ? code - Keys.NumPad0 : -1;
            if (digit >= 0 && SelectedRow?.Target != null)
            {
                var target = (digit == 0 ? 100 : digit * 10) - (int)Math.Round(SelectedRow.Target.Volume * 100);
                SelectedRow.Nudge(target);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            LastHiddenUtc = DateTime.UtcNow;
            HideMixer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Theme.Border))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Cerrar solo oculta; la app sigue en la bandeja.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideMixer();
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
