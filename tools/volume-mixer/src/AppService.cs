using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VolumeMixer
{
    /// <summary>Un programa en ejecucion con todos sus procesos (p. ej. Steam y sus steamwebhelper).</summary>
    internal sealed class RunningApp
    {
        public string Key => "app|" + Name;
        public string Name { get; set; }
        public string Title { get; set; }
        public Image Icon { get; set; }
        public bool HasWindow { get; set; }
        public long MemoryBytes { get; set; }
        public List<uint> Pids { get; } = new List<uint>();

        public string Detail
        {
            get
            {
                var mb = MemoryBytes / (1024.0 * 1024.0);
                var mem = mb >= 1024 ? $"{mb / 1024:0.0} GB" : $"{mb:0} MB";
                return (HasWindow ? "Con ventana" : "En segundo plano") + " · " + mem;
            }
        }
    }

    /// <summary>
    /// Lista los programas del usuario (no los de Windows) para poder cerrarlos.
    /// Los que no tienen ventana suelen ser los que viven en la bandeja del sistema.
    /// </summary>
    internal static class AppService
    {
        private static readonly string WindowsDir =
            Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";

        public static List<RunningApp> GetApps()
        {
            var session = Process.GetCurrentProcess().SessionId;
            var self = (uint)Process.GetCurrentProcess().Id;
            var processes = Process.GetProcesses();
            var running = new HashSet<uint>(processes.Select(p => (uint)p.Id));
            ProcessInfo.Prune(running);

            // Candidatos: procesos de esta sesion, fuera de C:\Windows, que podemos cerrar.
            var candidates = new Dictionary<uint, (ProcessInfo Info, long Memory)>();
            foreach (var p in processes)
            {
                using (p)
                {
                    try
                    {
                        var pid = (uint)p.Id;
                        if (pid == self || p.SessionId != session) continue;
                        // Se mira la ruta antes de cargar icono y descripcion (lo mas lento).
                        var path = Native.GetProcessPath(pid);
                        if (path == null || path.StartsWith(WindowsDir, StringComparison.OrdinalIgnoreCase)) continue;
                        var info = ProcessInfo.Get(pid);
                        if (info == null) continue;
                        if (!CanTerminate(pid)) continue; // de otro usuario o elevado: no se podria cerrar
                        candidates[pid] = (info, p.WorkingSet64);
                    }
                    catch (InvalidOperationException) { } // termino mientras lo leiamos
                }
            }

            var windows = VisibleWindowOwners();
            var parents = ProcessInfo.GetParentMap();
            var apps = new Dictionary<uint, RunningApp>();
            foreach (var pair in candidates)
            {
                // Cada proceso se agrupa bajo su antepasado mas lejano que tambien sea candidato.
                var root = pair.Key;
                for (var depth = 0; depth < 16 && parents.TryGetValue(root, out var parent) && parent != root && candidates.ContainsKey(parent); depth++)
                    root = parent;
                if (root == self) continue;

                if (!apps.TryGetValue(root, out var app))
                {
                    var rootInfo = candidates[root].Info;
                    app = new RunningApp { Name = rootInfo.Name, Title = rootInfo.Title, Icon = rootInfo.Icon };
                    apps[root] = app;
                }
                app.Pids.Add(pair.Key);
                app.MemoryBytes += pair.Value.Memory;
                if (windows.Contains(pair.Key)) app.HasWindow = true;
            }

            // Si dos raices son el mismo programa (p. ej. dos instancias), se juntan en una fila.
            return apps.Values
                .GroupBy(a => a.Name)
                .Select(g =>
                {
                    var first = g.First();
                    foreach (var other in g.Skip(1))
                    {
                        first.Pids.AddRange(other.Pids);
                        first.MemoryBytes += other.MemoryBytes;
                        first.HasWindow |= other.HasWindow;
                    }
                    return first;
                })
                .OrderBy(a => a.HasWindow)
                .ThenBy(a => a.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>Pide al programa que se cierre (como pulsar la X). Devuelve false si no tiene ventanas.</summary>
        public static bool RequestClose(RunningApp app)
        {
            var pids = new HashSet<uint>(app.Pids);
            var visible = new List<IntPtr>();
            var hidden = new List<IntPtr>();
            EnumWindows((h, _) =>
            {
                GetWindowThreadProcessId(h, out var pid);
                if (!pids.Contains(pid)) return true;
                if (IsAltTabWindow(h)) visible.Add(h);
                // De las ocultas, solo las ventanas principales (con titulo), no las internas del programa.
                else if (GetWindowTextLength(h) > 0 && GetWindow(h, 4 /* GW_OWNER */) == IntPtr.Zero) hidden.Add(h);
                return true;
            }, IntPtr.Zero);

            // Con ventanas visibles basta cerrarlas; los programas de bandeja solo tienen ventanas ocultas.
            var targets = visible.Count > 0 ? visible : hidden;
            foreach (var h in targets) PostMessage(h, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
            return targets.Count > 0;
        }

        public static bool IsRunning(RunningApp app) => app.Pids.Any(IsAlive);

        /// <summary>Termina todos los procesos del programa. Lo que no se haya guardado se pierde.</summary>
        public static string ForceClose(RunningApp app)
        {
            var failed = 0;
            foreach (var pid in app.Pids)
            {
                var h = OpenProcess(PROCESS_TERMINATE, false, pid);
                if (h == IntPtr.Zero) { if (IsAlive(pid)) failed++; continue; }
                try { if (!TerminateProcess(h, 1) && IsAlive(pid)) failed++; }
                finally { Native.CloseHandle(h); }
            }
            return failed == 0 ? null : "Windows no permitió cerrarlo por completo";
        }

        private static bool CanTerminate(uint pid)
        {
            var h = OpenProcess(PROCESS_TERMINATE | (uint)Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return false;
            Native.CloseHandle(h);
            return true;
        }

        private static bool IsAlive(uint pid)
        {
            var h = OpenProcess((uint)Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return false;
            try { return Native.GetExitCodeProcess(h, out var code) && code == 259; }
            finally { Native.CloseHandle(h); }
        }

        /// <summary>PIDs que tienen una ventana normal visible (de las que aparecen en Alt+Tab).</summary>
        private static HashSet<uint> VisibleWindowOwners()
        {
            var owners = new HashSet<uint>();
            EnumWindows((h, _) =>
            {
                if (IsAltTabWindow(h))
                {
                    GetWindowThreadProcessId(h, out var pid);
                    owners.Add(pid);
                }
                return true;
            }, IntPtr.Zero);
            return owners;
        }

        private static bool IsAltTabWindow(IntPtr h)
        {
            if (!IsWindowVisible(h) || GetWindow(h, 4 /* GW_OWNER */) != IntPtr.Zero) return false;
            if ((GetWindowLong(h, -20 /* GWL_EXSTYLE */) & 0x80 /* WS_EX_TOOLWINDOW */) != 0) return false;
            if (GetWindowTextLength(h) == 0) return false;
            // Las ventanas "ocultas" de apps modernas siguen visibles pero encubiertas por DWM.
            return DwmGetWindowAttribute(h, 14 /* DWMWA_CLOAKED */, out var cloaked, 4) != 0 || cloaked == 0;
        }

        // ----- Win32 -----

        private const uint PROCESS_TERMINATE = 0x0001;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr process, uint exitCode);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(Native.EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int value, int size);
    }
}
