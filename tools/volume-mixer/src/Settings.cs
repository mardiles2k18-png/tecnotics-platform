using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace VolumeMixer
{
    internal enum HotkeyAction
    {
        OpenMixer,
        FocusUp,
        FocusDown,
        FocusMute,
        CursorUp,
        CursorDown,
        CursorMute,
    }

    /// <summary>Configuracion en %APPDATA%\TecnoticsVolumeMixer\settings.ini (texto clave=valor).</summary>
    internal sealed class Settings
    {
        public static readonly string Folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TecnoticsVolumeMixer");
        public static readonly string FilePath = Path.Combine(Folder, "settings.ini");

        private static readonly (HotkeyAction Action, string Key, string Default, string Help)[] HotkeyDefs =
        {
            (HotkeyAction.OpenMixer, "abrir_mezclador", "Ctrl+Alt+V", "Abrir / cerrar el mezclador"),
            (HotkeyAction.FocusUp, "app_activa_subir", "Ctrl+Alt+PageUp", "Subir volumen de la app activa"),
            (HotkeyAction.FocusDown, "app_activa_bajar", "Ctrl+Alt+PageDown", "Bajar volumen de la app activa"),
            (HotkeyAction.FocusMute, "app_activa_silenciar", "Ctrl+Alt+End", "Silenciar la app activa"),
            (HotkeyAction.CursorUp, "app_bajo_mouse_subir", "Ctrl+Alt+Shift+PageUp", "Subir volumen de la app bajo el mouse"),
            (HotkeyAction.CursorDown, "app_bajo_mouse_bajar", "Ctrl+Alt+Shift+PageDown", "Bajar volumen de la app bajo el mouse"),
            (HotkeyAction.CursorMute, "app_bajo_mouse_silenciar", "Ctrl+Alt+Shift+End", "Silenciar la app bajo el mouse"),
        };

        public int StepPercent { get; private set; } = 5;
        public bool ShowOsd { get; private set; } = true;
        public Dictionary<HotkeyAction, string> Hotkeys { get; } = new Dictionary<HotkeyAction, string>();

        public static string Describe(HotkeyAction action)
        {
            foreach (var d in HotkeyDefs)
                if (d.Action == action) return d.Help;
            return action.ToString();
        }

        public static Settings Load()
        {
            var s = new Settings();
            foreach (var d in HotkeyDefs) s.Hotkeys[d.Action] = d.Default;

            try
            {
                if (!File.Exists(FilePath)) WriteDefaults();
                foreach (var raw in File.ReadAllLines(FilePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    var value = line.Substring(eq + 1).Trim();

                    if (key == "paso" && int.TryParse(value, out var step)) s.StepPercent = Math.Max(1, Math.Min(25, step));
                    else if (key == "mostrar_indicador") s.ShowOsd = !value.Equals("no", StringComparison.OrdinalIgnoreCase) && value != "0";
                    else
                        foreach (var d in HotkeyDefs)
                            if (d.Key == key) s.Hotkeys[d.Action] = value;
                }
            }
            catch (Exception ex) { Log.Error(ex); }
            return s;
        }

        private static void WriteDefaults()
        {
            Directory.CreateDirectory(Folder);
            var sb = new StringBuilder();
            sb.AppendLine("# Mezclador de volumen Tecnotics - configuracion");
            sb.AppendLine("# Modificadores: Ctrl, Alt, Shift, Win. Teclas: A-Z, F1-F24, PageUp, PageDown, Home, End,");
            sb.AppendLine("# Up, Down, Left, Right, Insert, Delete, NumPad0-NumPad9, Add, Subtract, etc.");
            sb.AppendLine("# Deja un atajo vacio para desactivarlo. Los cambios se aplican al guardar.");
            sb.AppendLine();
            sb.AppendLine("# Cuanto cambia el volumen en cada paso (1-25 %)");
            sb.AppendLine("paso=5");
            sb.AppendLine("# Mostrar el indicador en pantalla al usar un atajo (si/no)");
            sb.AppendLine("mostrar_indicador=si");
            sb.AppendLine();
            foreach (var d in HotkeyDefs)
            {
                sb.AppendLine("# " + d.Help);
                sb.AppendLine(d.Key + "=" + d.Default);
            }
            File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>Convierte "Ctrl+Alt+PageUp" en modificadores Win32 + tecla virtual.</summary>
        public static bool TryParseHotkey(string text, out uint modifiers, out Keys key)
        {
            modifiers = 0;
            key = Keys.None;
            if (string.IsNullOrWhiteSpace(text)) return false;

            foreach (var part in text.Split('+'))
            {
                var token = part.Trim();
                switch (token.ToLowerInvariant())
                {
                    case "ctrl": case "control": modifiers |= 0x2; break;
                    case "alt": modifiers |= 0x1; break;
                    case "shift": modifiers |= 0x4; break;
                    case "win": case "windows": modifiers |= 0x8; break;
                    case "repag": key = Keys.PageUp; break;
                    case "avpag": key = Keys.PageDown; break;
                    case "fin": key = Keys.End; break;
                    case "inicio": key = Keys.Home; break;
                    default:
                        if (token.Length == 1 && char.IsDigit(token[0])) token = "D" + token;
                        if (!Enum.TryParse(token, true, out key)) return false;
                        break;
                }
            }
            return key != Keys.None;
        }
    }

    internal static class Log
    {
        public static void Error(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Settings.Folder);
                File.AppendAllText(Path.Combine(Settings.Folder, "error.log"), $"[{DateTime.Now:s}] {ex}\r\n");
            }
            catch { }
        }
    }
}
