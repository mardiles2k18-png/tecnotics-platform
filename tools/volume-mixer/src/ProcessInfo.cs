using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VolumeMixer
{
    /// <summary>Nombre, titulo legible e icono de un proceso, con cache por PID.</summary>
    internal sealed class ProcessInfo
    {
        private static readonly Dictionary<uint, ProcessInfo> Cache = new Dictionary<uint, ProcessInfo>();

        public string Name { get; private set; }   // nombre del .exe en minusculas, sin extension
        public string Title { get; private set; }  // descripcion del archivo, p. ej. "Google Chrome"
        public Image Icon { get; private set; }

        public static ProcessInfo Get(uint pid)
        {
            if (pid == 0) return null;
            if (Cache.TryGetValue(pid, out var cached) && IsAlive(pid)) return cached;

            var info = Load(pid);
            if (info == null) Cache.Remove(pid);
            else Cache[pid] = info;
            return info;
        }

        /// <summary>Descarta del cache los procesos que ya no tienen sesiones de audio.</summary>
        public static void Prune(HashSet<uint> keep)
        {
            var stale = new List<uint>();
            foreach (var pid in Cache.Keys)
                if (!keep.Contains(pid)) stale.Add(pid);
            foreach (var pid in stale) Cache.Remove(pid);
        }

        private static ProcessInfo Load(uint pid)
        {
            var path = Native.GetProcessPath(pid);
            if (path == null)
            {
                // Sin permiso para leer la ruta (p. ej. proceso protegido): basta con el nombre.
                try
                {
                    using (var p = Process.GetProcessById((int)pid))
                        return new ProcessInfo { Name = p.ProcessName.ToLowerInvariant(), Title = p.ProcessName, Icon = Icons.App };
                }
                catch (ArgumentException) { return null; }      // ya no existe
                catch (InvalidOperationException) { return null; }
            }

            var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            string title = null;
            try { title = FileVersionInfo.GetVersionInfo(path).FileDescription?.Trim(); } catch { }
            if (string.IsNullOrEmpty(title))
                title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Path.GetFileNameWithoutExtension(path));

            Image icon = Icons.App;
            try
            {
                using (var ico = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    if (ico != null) icon = new Icon(ico, 32, 32).ToBitmap();
            }
            catch { }

            return new ProcessInfo { Name = name, Title = title, Icon = icon };
        }

        private static bool IsAlive(uint pid)
        {
            var h = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            // ERROR_INVALID_PARAMETER: el PID ya no existe. Otro error (sin acceso): asumimos que sigue vivo.
            if (h == IntPtr.Zero) return Marshal.GetLastWin32Error() != 87;
            try { return Native.GetExitCodeProcess(h, out var code) && code == 259 /* STILL_ACTIVE */; }
            finally { Native.CloseHandle(h); }
        }

        /// <summary>Mapa PID -> PID padre de todos los procesos en ejecucion.</summary>
        public static Dictionary<uint, uint> GetParentMap()
        {
            var map = new Dictionary<uint, uint>();
            var snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
            if (snap == IntPtr.Zero || snap == new IntPtr(-1)) return map;
            try
            {
                var entry = new Native.PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf(typeof(Native.PROCESSENTRY32)) };
                if (!Native.Process32First(snap, ref entry)) return map;
                do map[entry.th32ProcessID] = entry.th32ParentProcessID;
                while (Native.Process32Next(snap, ref entry));
            }
            finally { Native.CloseHandle(snap); }
            return map;
        }

        public static bool IsDescendant(uint pid, uint ancestor, Dictionary<uint, uint> parents)
        {
            for (var depth = 0; depth < 16 && parents.TryGetValue(pid, out var parent) && parent != 0 && parent != pid; depth++)
            {
                if (parent == ancestor) return true;
                pid = parent;
            }
            return false;
        }
    }

    internal static partial class Native
    {
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint TH32CS_SNAPPROCESS = 0x2;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32First(IntPtr snapshot, ref PROCESSENTRY32 entry);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32Next(IntPtr snapshot, ref PROCESSENTRY32 entry);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
        }

        public static string GetProcessPath(uint pid)
        {
            var h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return null;
            try
            {
                var sb = new StringBuilder(1024);
                var size = (uint)sb.Capacity;
                return QueryFullProcessImageName(h, 0, sb, ref size) ? sb.ToString() : null;
            }
            finally { CloseHandle(h); }
        }
    }
}
