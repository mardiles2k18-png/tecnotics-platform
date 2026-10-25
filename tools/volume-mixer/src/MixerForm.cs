using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VolumeMixer
{
    /// <summary>
    /// Panel rapido con tres pestanas: volumen por aplicacion, discos externos y Wi‑Fi.
    /// Aparece junto al boton flotante (o en la esquina de la pantalla del mouse)
    /// y se oculta solo al hacer clic fuera.
    /// </summary>
    internal sealed class MixerForm : Form
    {
        private const int WidthDip = 380;
        private const int TabVolume = 0, TabDrives = 1, TabWifi = 2, TabApps = 3;
        private const int TabCount = 4;

        private readonly AudioService _audio;
        private readonly WifiService _wifi;
        private readonly Settings _settings;
        private readonly Action<string, Image, string, Screen> _notify;
        private readonly Panel _list = new Panel { AutoScroll = true, BackColor = Theme.Background };
        private readonly TabBar _tabs = new TabBar("Volumen", "Discos", "Wi‑Fi", "Apps");
        private readonly Label _footer = new Label { ForeColor = Theme.SubText, Font = Theme.SmallFont, BackColor = Theme.Background };
        private readonly Timer _refreshTimer = new Timer { Interval = 1000 };
        private readonly Timer _meterTimer = new Timer { Interval = 50 };
        private readonly Timer _wifiWatch = new Timer { Interval = 1000 };
        private readonly Dictionary<string, Control> _rows = new Dictionary<string, Control>();
        private readonly HashSet<string> _used = new HashSet<string>();
        private List<Control> _layout = new List<Control>();
        private List<VolumeRow> _volumeRows = new List<VolumeRow>();
        private int _tab;
        private int _selected;
        private int _anchorBottom;
        private int _anchorTop;
        private bool _anchorAbove;
        private Rectangle _anchorRect;
        private bool _passive;
        private Rectangle _area;
        private int _ticks;

        // Discos
        private List<ExternalDisk> _disks = new List<ExternalDisk>();
        private readonly HashSet<int> _ejecting = new HashSet<int>();
        private readonly Dictionary<int, string> _driveErrors = new Dictionary<int, string>();
        private string _driveMessage;

        // Wi‑Fi
        private List<WifiNetwork> _networks = new List<WifiNetwork>();
        private string _wifiMessage;
        private string _pendingSsid;
        private string _createdProfile;
        private DateTime _pendingSince;

        // Apps
        private List<RunningApp> _apps = new List<RunningApp>();
        private readonly Dictionary<string, DateTime> _closeRequested = new Dictionary<string, DateTime>();

        public MixerForm(AudioService audio, WifiService wifi, Settings settings, Action<string, Image, string, Screen> notify)
        {
            _audio = audio;
            _wifi = wifi;
            _settings = settings;
            _notify = notify;

            Text = "Panel rápido";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Background;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            Icon = Icons.AppIcon;

            _footer.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_list);
            Controls.Add(_tabs);
            Controls.Add(_footer);

            _tabs.SelectedChanged += SwitchTab;
            _refreshTimer.Tick += (s, e) =>
            {
                _ticks++;
                if (_tab == TabWifi && _ticks % 5 == 0) _wifi.Scan();
                Rebuild();
            };
            _meterTimer.Tick += (s, e) =>
            {
                foreach (var r in _volumeRows) r.UpdatePeak();
                // En modo pasivo no hay evento Deactivate: se cierra al hacer clic fuera.
                if (_passive && Native.AnyMouseButtonDown())
                {
                    var p = Cursor.Position;
                    if (!Bounds.Contains(p) && !_anchorRect.Contains(p))
                    {
                        LastHiddenUtc = DateTime.UtcNow;
                        HideMixer();
                    }
                }
            };
            _wifiWatch.Tick += (s, e) => WatchPendingConnection();
        }

        /// <summary>Momento en que se oculto por perder el foco (para no reabrir con el mismo clic).</summary>
        public DateTime LastHiddenUtc { get; private set; }

        private float Dpi => DeviceDpi / 96f;

        private Screen CurrentScreen => Screen.FromRectangle(Bounds);

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
            Native.SetNoActivate(Handle, _passive);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x21 /* WM_MOUSEACTIVATE */ && _passive)
            {
                m.Result = (IntPtr)3; // MA_NOACTIVATE
                return;
            }
            base.WndProc(ref m);
        }

        protected override bool ShowWithoutActivation => _passive;

        public void Toggle(bool passive)
        {
            if (Visible) HideMixer();
            else ShowMixer(null, passive);
        }

        /// <summary>
        /// Muestra el panel junto a <paramref name="anchor"/> (p. ej. el boton flotante) o, sin ancla,
        /// en la esquina inferior derecha de la pantalla del mouse. En modo pasivo la ventana no toma
        /// el foco: el juego sigue recibiendo el teclado mientras se usa el panel con el mouse.
        /// </summary>
        public void ShowMixer(Rectangle? anchor = null, bool passive = false)
        {
            var gap = (int)(12 * Dpi);
            Width = (int)(WidthDip * Dpi);
            if (anchor is Rectangle a)
            {
                _anchorRect = a;
                _area = Screen.FromRectangle(a).WorkingArea;
                Left = Math.Max(_area.Left + gap, Math.Min(a.Right - Width, _area.Right - Width - gap));
                // Arriba del boton si esta en la mitad inferior de la pantalla; si no, debajo.
                _anchorAbove = a.Top + a.Height / 2 > _area.Top + _area.Height / 2;
                _anchorBottom = a.Top - gap / 2;
                _anchorTop = a.Bottom + gap / 2;
            }
            else
            {
                _anchorRect = Rectangle.Empty;
                _area = Native.WorkingAreaAtCursor();
                Left = _area.Right - Width - gap;
                _anchorAbove = true;
                _anchorBottom = _area.Bottom - gap;
            }

            _passive = passive;
            if (IsHandleCreated) Native.SetNoActivate(Handle, passive);
            _selected = 0;
            _driveMessage = null;
            _wifiMessage = null;
            if (_tab == TabWifi) _wifi.Scan();
            Rebuild();

            Show();
            if (!passive) Activate();
            _refreshTimer.Start();
            _meterTimer.Start();
        }

        public void HideMixer()
        {
            _refreshTimer.Stop();
            _meterTimer.Stop();
            Hide();
        }

        private void SwitchTab(int tab)
        {
            _tab = tab;
            _tabs.Selected = tab;
            _selected = 0;
            _driveMessage = null;
            _wifiMessage = null;
            _list.AutoScrollPosition = Point.Empty;
            if (tab == TabWifi) _wifi.Scan();
            _refreshTimer.Interval = tab == TabVolume || tab == TabApps ? 1000 : 2000;
            Rebuild();
        }

        // ===================== Construccion de filas =====================

        /// <summary>Vuelve a leer el estado de la pestana activa, reutilizando las filas existentes.</summary>
        public void Rebuild()
        {
            _used.Clear();
            var layout = new List<Control>();
            try
            {
                switch (_tab)
                {
                    case TabVolume: BuildVolume(layout); break;
                    case TabDrives: BuildDrives(layout); break;
                    case TabWifi: BuildWifi(layout); break;
                    case TabApps: BuildApps(layout); break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return;
            }

            foreach (var key in _rows.Keys.Where(k => !_used.Contains(k)).ToList())
            {
                _rows[key].Dispose();
                _rows.Remove(key);
            }
            _layout = layout;
            _volumeRows = layout.OfType<VolumeRow>().ToList();
            if (_tab == TabVolume) SelectRow(Math.Min(_selected, _volumeRows.Count - 1));
            LayoutRows();
        }

        private T Row<T>(string key, Action<T> init = null) where T : Control, new()
        {
            _used.Add(key);
            if (_rows.TryGetValue(key, out var existing) && existing is T typed) return typed;
            existing?.Dispose();
            var row = new T { Tag = key };
            init?.Invoke(row);
            _rows[key] = row;
            _list.Controls.Add(row);
            return row;
        }

        private ActionRow Info(string key, Image icon, string title, string detail, string button = null, Action onClick = null, bool gap = false)
        {
            var row = Row<ActionRow>(key, r => r.ButtonClick += (s, e) => onClick?.Invoke());
            row.GapBefore = gap;
            row.Set(icon, title, detail, button);
            return row;
        }

        private void BuildVolume(List<Control> layout)
        {
            foreach (var d in _audio.GetSnapshot())
            {
                // Los dispositivos secundarios sin nada sonando solo agregan ruido.
                if (!d.IsDefault && d.Apps.Count == 0) continue;
                foreach (var t in new IVolumeTarget[] { d }.Concat(d.Apps))
                {
                    var row = Row<VolumeRow>(t.Key, r => r.Activated += (s, e) => SelectRow(_volumeRows.IndexOf((VolumeRow)s)));
                    row.StepPercent = _settings.StepPercent;
                    row.Target = t;
                    layout.Add(row);
                }
            }
        }

        private void BuildDrives(List<Control> layout)
        {
            _disks = DriveService.GetExternalDisks();

            if (_driveMessage != null)
                layout.Add(Info("drive|msg", Icons.Drive, _driveMessage, "Ya puedes desconectarlo"));

            if (_disks.Count == 0)
            {
                layout.Add(Info("drive|none", Icons.Drive, "No hay discos externos", "Conecta un USB o disco externo y aparecerá aquí"));
                return;
            }

            foreach (var disk in _disks)
            {
                var row = Row<ActionRow>(disk.Key, r => r.ButtonClick += (s, e) => EjectClicked((string)((Control)s).Tag));
                var hasError = _driveErrors.TryGetValue(disk.DiskNumber, out var error);
                row.Set(Icons.Drive, disk.Title, hasError ? error : disk.Detail, "Expulsar", hasError ? Theme.Danger : (Color?)null);
                row.Busy = _ejecting.Contains(disk.DiskNumber);
                layout.Add(row);
            }
        }

        private void BuildWifi(List<Control> layout)
        {
            var st = _wifi.GetStatus();
            _networks = st.Networks;

            if (!st.HasAdapter)
            {
                layout.Add(Info("wifi|none", Icons.WifiOff, "No se encontró Wi‑Fi", "Este equipo no tiene un adaptador Wi‑Fi activo"));
                return;
            }

            var header = Row<ActionRow>("wifi|state", r =>
            {
                r.RowClickable = false;
                r.ButtonClick += (s, e) => ToggleRadio();
            });
            if (!st.RadioOn)
            {
                header.Set(Icons.WifiOff, "Wi‑Fi desactivado",
                    st.HardwareOff ? "Apagado por el interruptor del equipo o el modo avión" : null, "Activar");
            }
            else
            {
                var current = st.Current;
                var title = current?.Ssid ?? (st.Connected ? "Conectado" : "Sin conexión");
                var detail = current != null ? $"Conectado · señal {current.Signal}%" : st.Connected ? "Conectado a una red Wi‑Fi" : "Wi‑Fi activado";
                header.Set(st.Connected ? Icons.Wifi(current?.Bars ?? 4) : Icons.WifiDisconnected, title, detail, "Desactivar", null, st.Connected);
            }
            layout.Add(header);

            if (_wifiMessage != null)
                layout.Add(Info("wifi|msg", Icons.Warning, _wifiMessage, null));

            if (!st.RadioOn) return;

            if (st.LocationBlocked)
            {
                layout.Add(Info("wifi|location", Icons.Warning, "Windows oculta las redes cercanas",
                    "Permite la ubicación a las apps de escritorio", "Abrir", () => OpenUri("ms-settings:privacy-location"), gap: true));
                return;
            }

            if (_networks.Count == 0)
            {
                layout.Add(Info("wifi|searching", Icons.Wifi(1), "Buscando redes…", null, gap: true));
                return;
            }

            var first = true;
            foreach (var n in _networks)
            {
                var row = Row<ActionRow>("wifi|net|" + n.Ssid, r => r.ButtonClick += (s, e) => NetworkClicked(((string)((Control)s).Tag).Substring(9)));
                row.GapBefore = first;
                first = false;
                string detail;
                if (n.Connected) detail = "Conectado";
                else if (n.Ssid == _pendingSsid) detail = "Conectando…";
                else detail = (n.HasProfile ? "Guardada · " : "") + (n.Secure ? "Segura" : "Abierta") + $" · {n.Signal}%";
                row.Set(Icons.Wifi(n.Bars), n.Ssid, detail, n.Connected ? "Desconectar" : "Conectar", null, n.Connected);
                row.Busy = n.Ssid == _pendingSsid;
                layout.Add(row);
            }
        }

        private void BuildApps(List<Control> layout)
        {
            _apps = AppService.GetApps();
            var keys = new HashSet<string>(_apps.Select(a => a.Key));
            foreach (var gone in _closeRequested.Keys.Where(k => !keys.Contains(k)).ToList()) _closeRequested.Remove(gone);

            if (_apps.Count == 0)
            {
                layout.Add(Info("apps|none", Icons.App, "No hay programas abiertos", null));
                return;
            }

            bool? lastHadWindow = null;
            foreach (var app in _apps)
            {
                if (lastHadWindow != app.HasWindow)
                {
                    var section = Row<SectionRow>(app.HasWindow ? "apps|sec|win" : "apps|sec|bg");
                    section.Text = app.HasWindow ? "CON VENTANA" : "EN SEGUNDO PLANO (BANDEJA)";
                    layout.Add(section);
                    lastHadWindow = app.HasWindow;
                }

                var row = Row<ActionRow>(app.Key, r =>
                {
                    // Solo el boton cierra: un clic por error en la fila no debe cerrar nada.
                    r.RowClickable = false;
                    r.ButtonClick += (s, e) => CloseClicked((string)((Control)s).Tag);
                });
                if (_closeRequested.TryGetValue(app.Key, out var since) && (DateTime.UtcNow - since).TotalSeconds >= 3)
                {
                    row.Set(app.Icon, app.Title, "No se cerró. Forzar puede perder lo no guardado", "Forzar", Theme.Danger);
                    row.Busy = false;
                }
                else
                {
                    row.Set(app.Icon, app.Title, app.Detail, "Cerrar");
                    row.Busy = _closeRequested.ContainsKey(app.Key);
                }
                layout.Add(row);
            }
        }

        private void CloseClicked(string key)
        {
            var app = _apps.FirstOrDefault(a => a.Key == key);
            if (app == null) return;

            if (_closeRequested.TryGetValue(key, out var since) && (DateTime.UtcNow - since).TotalSeconds >= 3)
            {
                _closeRequested.Remove(key);
                var error = AppService.ForceClose(app);
                if (error != null) _notify(app.Title, Icons.Warning, error, CurrentScreen);
            }
            else if (AppService.RequestClose(app))
            {
                _closeRequested[key] = DateTime.UtcNow;
            }
            else
            {
                // Sin ventanas a las que pedirle que se cierre: se ofrece forzar de inmediato.
                _closeRequested[key] = DateTime.UtcNow.AddSeconds(-3);
            }
            Rebuild();
        }

        private void LayoutRows()
        {
            var s = Dpi;
            var rowH = VolumeRow.RowHeight(s);
            var headerH = (int)(46 * s);
            var footerH = (int)(30 * s);
            var gap = (int)(12 * s);

            int HeightOf(Control c) => c is SectionRow ? SectionRow.RowHeight(s) : rowH;
            int GapBefore(int i) =>
                i > 0 && ((_layout[i] as VolumeRow)?.Target.IsDevice == true || (_layout[i] as ActionRow)?.GapBefore == true) ? gap : 0;

            var contentH = 0;
            for (var i = 0; i < _layout.Count; i++) contentH += HeightOf(_layout[i]) + GapBefore(i);

            var maxListH = (int)(_area.Height * 0.75) - headerH - footerH;
            var listH = Math.Max(rowH, Math.Min(contentH, maxListH));
            var scroll = contentH > listH;

            Height = headerH + listH + footerH;
            var top = _anchorAbove ? _anchorBottom - Height : _anchorTop;
            Top = Math.Max(_area.Top, Math.Min(top, _area.Bottom - Height));

            _tabs.SetBounds(0, 0, Width, headerH);
            _list.SetBounds(0, headerH, Width, listH);
            _footer.SetBounds((int)(18 * s), headerH + listH, Width - (int)(36 * s), footerH);
            switch (_tab)
            {
                case TabVolume:
                    _footer.Text = _passive
                        ? $"Rueda: ±{_settings.StepPercent}%   Clic der.: silenciar   Clic fuera: cerrar"
                        : $"Rueda: ±{_settings.StepPercent}%   Clic der.: silenciar   ↑↓←→ / M";
                    break;
                case TabDrives: _footer.Text = "Expulsa el disco antes de desconectarlo"; break;
                case TabWifi: _footer.Text = "Clic en una red para conectarte"; break;
                default: _footer.Text = "Cerrar pide al programa que se cierre; si no responde, Forzar"; break;
            }

            var rowW = Width - (scroll ? SystemInformation.VerticalScrollBarWidth : 0);
            var y = _list.AutoScrollPosition.Y;
            for (var i = 0; i < _layout.Count; i++)
            {
                y += GapBefore(i);
                var h = HeightOf(_layout[i]);
                _layout[i].SetBounds(0, y, rowW, h);
                y += h;
            }
            Invalidate();
        }

        // ===================== Acciones: discos =====================

        private void EjectClicked(string key)
        {
            var disk = _disks.FirstOrDefault(d => d.Key == key);
            if (disk == null || _ejecting.Contains(disk.DiskNumber)) return;

            var number = disk.DiskNumber;
            var title = disk.Title;
            _ejecting.Add(number);
            _driveErrors.Remove(number);
            Rebuild();

            Task.Run(() => DriveService.Eject(disk)).ContinueWith(t =>
            {
                _ejecting.Remove(number);
                var error = t.IsFaulted ? "Error inesperado al expulsar" : t.Result;
                if (t.IsFaulted) Log.Error(t.Exception);
                if (error == null)
                {
                    _driveMessage = title;
                    _notify(title, Icons.Drive, "Ya puedes desconectar el dispositivo", CurrentScreen);
                }
                else
                {
                    _driveErrors[number] = error;
                }
                if (Visible && _tab == TabDrives) Rebuild();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        // ===================== Acciones: Wi‑Fi =====================

        private void ToggleRadio()
        {
            var st = _wifi.GetStatus(includeNetworks: false);
            var error = _wifi.SetRadio(!st.RadioOn);
            if (error != null)
            {
                // Algunos equipos no dejan cambiar la radio desde apps: se abre la configuracion de Windows.
                _wifiMessage = error;
                OpenUri("ms-settings:network-wifi");
            }
            else if (!st.RadioOn)
            {
                _wifi.Scan();
            }
            Rebuild();
        }

        private void NetworkClicked(string ssid)
        {
            var n = _networks.FirstOrDefault(x => x.Ssid == ssid);
            if (n == null) return;
            _wifiMessage = null;

            if (n.Connected)
            {
                _wifiMessage = _wifi.Disconnect();
                Rebuild();
                return;
            }

            if (n.HasProfile)
            {
                StartConnect(n.Ssid, _wifi.Connect(n.ProfileName), null);
                return;
            }

            if (n.IsEnterprise)
            {
                _wifiMessage = "Esta red requiere usuario: configúrala en Windows";
                OpenUri("ms-settings:network-wifi");
                Rebuild();
                return;
            }

            string password = null;
            if (n.Secure)
            {
                var screen = CurrentScreen;
                HideMixer();
                using (var dialog = new PasswordForm(n.Ssid, screen))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    password = dialog.Password;
                }
                if (password.Length < 8 || password.Length > 63)
                {
                    _notify(n.Ssid, Icons.Warning, "La contraseña debe tener entre 8 y 63 caracteres", screen);
                    return;
                }
            }

            var profile = _wifi.CreateProfile(n, password, out var error);
            if (profile == null)
            {
                _notify(n.Ssid, Icons.Warning, error, CurrentScreen);
                _wifiMessage = error;
                return;
            }
            StartConnect(n.Ssid, _wifi.Connect(profile), profile);
        }

        private void StartConnect(string ssid, string error, string createdProfile)
        {
            if (error != null)
            {
                _wifiMessage = error;
                if (createdProfile != null) _wifi.DeleteProfile(createdProfile);
                if (Visible) Rebuild();
                else _notify(ssid, Icons.Warning, error, CurrentScreen);
                return;
            }
            _pendingSsid = ssid;
            _createdProfile = createdProfile;
            _pendingSince = DateTime.UtcNow;
            _wifiWatch.Start();
            if (Visible) Rebuild();
            else _notify(ssid, Icons.Wifi(4), "Conectando…", CurrentScreen);
        }

        /// <summary>Sigue la conexion pendiente (aunque el panel este cerrado) y avisa el resultado.</summary>
        private void WatchPendingConnection()
        {
            if (_pendingSsid == null) { _wifiWatch.Stop(); return; }
            var st = _wifi.GetStatus();
            var ok = st.Current?.Ssid == _pendingSsid || (st.LocationBlocked && st.Connected);
            var timedOut = (DateTime.UtcNow - _pendingSince).TotalSeconds > 20;
            if (!ok && !timedOut) return;

            var ssid = _pendingSsid;
            _pendingSsid = null;
            _wifiWatch.Stop();
            if (ok)
            {
                _notify(ssid, Icons.Wifi(st.Current?.Bars ?? 4), "Conectado", CurrentScreen);
            }
            else
            {
                // No guardamos una contraseña que no funciono.
                if (_createdProfile != null) _wifi.DeleteProfile(_createdProfile);
                _wifiMessage = $"No se pudo conectar a «{ssid}»";
                _notify(ssid, Icons.Warning, _createdProfile != null ? "No se pudo conectar. Revisa la contraseña." : "No se pudo conectar", CurrentScreen);
            }
            _createdProfile = null;
            if (Visible && _tab == TabWifi) Rebuild();
        }

        private static void OpenUri(string uri)
        {
            try { Process.Start(uri); }
            catch (Exception ex) { Log.Error(ex); }
        }

        // ===================== Teclado (pestana Volumen) =====================

        private void SelectRow(int index)
        {
            if (_volumeRows.Count == 0) return;
            _selected = Math.Max(0, Math.Min(index, _volumeRows.Count - 1));
            for (var i = 0; i < _volumeRows.Count; i++) _volumeRows[i].Selected = i == _selected;
            _list.ScrollControlIntoView(_volumeRows[_selected]);
        }

        private VolumeRow SelectedRow => _selected >= 0 && _selected < _volumeRows.Count ? _volumeRows[_selected] : null;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var code = keyData & Keys.KeyCode;
            if (code == Keys.Escape) { HideMixer(); return true; }
            if ((keyData & Keys.Control) != 0 && code == Keys.Tab)
            {
                SwitchTab((_tab + 1) % TabCount);
                return true;
            }
            if (_tab != TabVolume) return base.ProcessCmdKey(ref msg, keyData);

            var step = (keyData & Keys.Shift) != 0 ? 1 : _settings.StepPercent;
            switch (code)
            {
                case Keys.Up: SelectRow(_selected - 1); return true;
                case Keys.Down: SelectRow(_selected + 1); return true;
                case Keys.Tab: SelectRow((_selected + 1) % Math.Max(1, _volumeRows.Count)); return true;
                case Keys.Right: SelectedRow?.Nudge(step); return true;
                case Keys.Left: SelectedRow?.Nudge(-step); return true;
                case Keys.M:
                case Keys.Space: SelectedRow?.ToggleMute(); return true;
            }

            // Teclas 1..9 fijan 10%..90%, 0 fija 100%.
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer.Dispose();
                _meterTimer.Dispose();
                _wifiWatch.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
