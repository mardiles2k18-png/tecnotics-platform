using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VolumeMixer
{
    /// <summary>Icono de bandeja, atajos globales y acciones sobre la app activa / bajo el mouse.</summary>
    internal sealed class TrayApp : ApplicationContext
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "TecnoticsVolumeMixer";

        private readonly AudioService _audio = new AudioService();
        private readonly NotifyIcon _tray;
        private readonly HotkeyWindow _hotkeys = new HotkeyWindow();
        private readonly ToolStripMenuItem _autostartItem;
        private readonly ToolStripMenuItem _buttonItem;
        private readonly FloatingButton _button;
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _reloadDebounce = new Timer { Interval = 400 };
        private Settings _settings;
        private MixerForm _mixer;
        private OsdForm _osd;

        public TrayApp()
        {
            _settings = Settings.Load();
            _mixer = new MixerForm(_audio, _settings);
            _osd = new OsdForm();
            _ = _osd.Handle; // crea la ventana ya, para poder recibir eventos de otros hilos via BeginInvoke

            _autostartItem = new ToolStripMenuItem("Iniciar con Windows", null, (s, e) => ToggleAutostart()) { Checked = IsAutostartEnabled() };
            var menu = new ContextMenuStrip();
            menu.Items.Add("Abrir mezclador", null, (s, e) => _mixer.ShowMixer()).Font = Theme.TitleFont;
            menu.Items.Add("Ver atajos de teclado", null, (s, e) => ShowHotkeyHelp());
            menu.Items.Add("Editar configuración", null, (s, e) => OpenSettingsFile());
            _buttonItem = new ToolStripMenuItem("Botón flotante", null, (s, e) => SetButtonVisible(!_button.Visible));
            menu.Items.Add(_buttonItem);
            menu.Items.Add(_autostartItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir", null, (s, e) => ExitThread());

            _tray = new NotifyIcon
            {
                Icon = Icons.AppIcon,
                Text = "Mezclador de volumen",
                ContextMenuStrip = menu,
                Visible = true,
            };
            _tray.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                // El clic en la bandeja desactiva primero el mezclador: no lo reabrimos con el mismo clic.
                if ((DateTime.UtcNow - _mixer.LastHiddenUtc).TotalMilliseconds < 300) return;
                _mixer.Toggle(passive: false);
            };

            _button = new FloatingButton(menu);
            _button.Pressed += (s, e) =>
            {
                if ((DateTime.UtcNow - _mixer.LastHiddenUtc).TotalMilliseconds < 300) return;
                if (_mixer.Visible) _mixer.HideMixer();
                else _mixer.ShowMixer(_button.Bounds, passive: true);
            };
            SetButtonVisible(IsButtonEnabled());

            _hotkeys.Pressed += OnHotkey;
            var failed = RegisterHotkeys();

            _watcher = new FileSystemWatcher(Settings.Folder, Path.GetFileName(Settings.FilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true,
            };
            // Los editores guardan en varias escrituras: esperamos un momento antes de recargar.
            _watcher.Changed += (s, e) => _osd.BeginInvoke((Action)(() => { _reloadDebounce.Stop(); _reloadDebounce.Start(); }));
            _reloadDebounce.Tick += (s, e) => { _reloadDebounce.Stop(); ReloadSettings(); };

            _tray.ShowBalloonTip(4000, "Mezclador de volumen listo",
                failed.Count == 0
                    ? $"{_settings.Hotkeys[HotkeyAction.OpenMixer]} abre el mezclador.\n{_settings.Hotkeys[HotkeyAction.FocusUp]} / {_settings.Hotkeys[HotkeyAction.FocusDown]} cambian el volumen de la app activa."
                    : "Algunos atajos ya los usa otro programa: " + string.Join(", ", failed),
                failed.Count == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private List<string> RegisterHotkeys()
        {
            _hotkeys.UnregisterAll();
            var failed = new List<string>();
            foreach (var pair in _settings.Hotkeys)
            {
                if (string.IsNullOrWhiteSpace(pair.Value)) continue;
                // Subir/bajar se repiten al mantener la tecla; abrir y silenciar no.
                var repeat = pair.Key != HotkeyAction.OpenMixer && pair.Key != HotkeyAction.FocusMute && pair.Key != HotkeyAction.CursorMute;
                if (!Settings.TryParseHotkey(pair.Value, out var mods, out var key) || !_hotkeys.Register((int)pair.Key, mods, key, repeat))
                    failed.Add(pair.Value);
            }
            return failed;
        }

        private void ReloadSettings()
        {
            _settings = Settings.Load();
            var wasVisible = _mixer.Visible;
            _mixer.Dispose();
            _mixer = new MixerForm(_audio, _settings);
            if (wasVisible) _mixer.ShowMixer();

            var failed = RegisterHotkeys();
            _tray.ShowBalloonTip(2500, "Configuración actualizada",
                failed.Count == 0 ? "Los atajos nuevos ya están activos." : "No se pudieron registrar: " + string.Join(", ", failed),
                failed.Count == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private void OnHotkey(int id)
        {
            try
            {
                var step = _settings.StepPercent;
                switch ((HotkeyAction)id)
                {
                    // Sin tomar el foco: abrir el mezclador con el atajo no saca al juego de primer plano.
                    case HotkeyAction.OpenMixer: _mixer.Toggle(passive: true); break;
                    case HotkeyAction.FocusUp: AdjustApp(WindowHelper.ForegroundProcess(out var h1), h1, step, false); break;
                    case HotkeyAction.FocusDown: AdjustApp(WindowHelper.ForegroundProcess(out var h2), h2, -step, false); break;
                    case HotkeyAction.FocusMute: AdjustApp(WindowHelper.ForegroundProcess(out var h3), h3, 0, true); break;
                    case HotkeyAction.CursorUp: AdjustApp(WindowHelper.ProcessUnderCursor(out var h4), h4, step, false); break;
                    case HotkeyAction.CursorDown: AdjustApp(WindowHelper.ProcessUnderCursor(out var h5), h5, -step, false); break;
                    case HotkeyAction.CursorMute: AdjustApp(WindowHelper.ProcessUnderCursor(out var h6), h6, 0, true); break;
                }
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        private void AdjustApp(uint pid, IntPtr hwnd, int deltaPercent, bool toggleMute)
        {
            if (pid == 0 || pid == (uint)Process.GetCurrentProcess().Id) return;
            var info = ProcessInfo.Get(pid);
            if (info == null) return;

            var screen = Native.ScreenOf(hwnd);
            var apps = _audio.FindApps(pid);
            if (apps.Count == 0)
            {
                // Explorer/escritorio en primer plano: no tiene sesion, pero el usuario quiere un resultado.
                if (_settings.ShowOsd)
                    _osd.ShowMessage(info.Title, info.Icon, "Esta aplicación no está reproduciendo audio", screen);
                return;
            }

            var first = apps[0];
            bool muted;
            float volume;
            if (toggleMute)
            {
                muted = !first.Muted;
                volume = first.Volume;
                foreach (var a in apps) a.Muted = muted;
            }
            else
            {
                volume = (float)Math.Round(first.Volume * 100 + deltaPercent) / 100f;
                volume = volume < 0 ? 0 : volume > 1 ? 1 : volume;
                muted = first.Muted && deltaPercent <= 0;
                foreach (var a in apps)
                {
                    a.Volume = volume;
                    if (deltaPercent > 0) a.Muted = false;
                }
            }

            if (_settings.ShowOsd) _osd.ShowVolume(first.Title, first.Icon, volume, muted, screen);
            if (_mixer.Visible) _mixer.Rebuild();
        }

        private void ShowHotkeyHelp()
        {
            var lines = _settings.Hotkeys
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p => $"{p.Value,-26}  {Settings.Describe(p.Key)}");
            MessageBox.Show(
                string.Join("\n", lines) +
                "\n\nBotón flotante: arrástralo a cualquier pantalla; clic para abrir el mezclador ahí\n" +
                "(no le quita el foco al juego). Clic derecho: este menú.\n" +
                "\nEn el mezclador:\n" +
                $"  Rueda del mouse sobre una fila: ±{_settings.StepPercent}% (Shift: ±1%)\n" +
                "  Clic derecho o clic en el icono: silenciar\n" +
                "  Clic con la rueda: volumen al 100%\n" +
                "  Flechas ↑↓ elegir, ←→ ajustar, M silenciar, 1-9 = 10-90%, 0 = 100%",
                "Atajos de teclado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OpenSettingsFile()
        {
            try { Process.Start("notepad.exe", "\"" + Settings.FilePath + "\""); }
            catch (Exception ex) { Log.Error(ex); }
        }

        private static bool IsButtonEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\TecnoticsVolumeMixer"))
                return !(key?.GetValue("ShowButton") is int v) || v != 0;
        }

        private void SetButtonVisible(bool visible)
        {
            if (visible) _button.Show();
            else _button.Hide();
            _buttonItem.Checked = visible;
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\TecnoticsVolumeMixer"))
                    key.SetValue("ShowButton", visible ? 1 : 0);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        private static bool IsAutostartEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                return key?.GetValue(RunValue) is string;
        }

        private void ToggleAutostart()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (IsAutostartEnabled()) key.DeleteValue(RunValue, false);
                else key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
            }
            _autostartItem.Checked = IsAutostartEnabled();
        }

        protected override void ExitThreadCore()
        {
            _hotkeys.UnregisterAll();
            _hotkeys.DestroyHandle();
            _watcher.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _mixer.Dispose();
            _button.Dispose();
            _osd.Dispose();
            base.ExitThreadCore();
        }
    }

    /// <summary>Ventana invisible que recibe los WM_HOTKEY de los atajos globales.</summary>
    internal sealed class HotkeyWindow : NativeWindow
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_NOREPEAT = 0x4000;
        private readonly List<int> _ids = new List<int>();

        public HotkeyWindow() => CreateHandle(new CreateParams());

        public event Action<int> Pressed;

        public bool Register(int id, uint modifiers, Keys key, bool repeat)
        {
            if (!Native.RegisterHotKey(Handle, id, modifiers | (repeat ? 0 : MOD_NOREPEAT), (uint)key)) return false;
            _ids.Add(id);
            return true;
        }

        public void UnregisterAll()
        {
            foreach (var id in _ids) Native.UnregisterHotKey(Handle, id);
            _ids.Clear();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY) Pressed?.Invoke(m.WParam.ToInt32());
            base.WndProc(ref m);
        }
    }
}
