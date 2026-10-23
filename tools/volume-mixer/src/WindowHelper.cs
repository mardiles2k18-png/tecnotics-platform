using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VolumeMixer
{
    /// <summary>Averigua que proceso esta detras de la ventana activa o de la ventana bajo el mouse.</summary>
    internal static class WindowHelper
    {
        public static uint ForegroundProcess(out IntPtr hwnd)
        {
            hwnd = Native.GetForegroundWindow();
            return ResolveProcess(hwnd);
        }

        public static uint ProcessUnderCursor(out IntPtr hwnd)
        {
            Native.GetCursorPos(out var pt);
            hwnd = Native.WindowFromPoint(pt);
            if (hwnd != IntPtr.Zero) hwnd = Native.GetAncestor(hwnd, 2 /* GA_ROOT */);
            return ResolveProcess(hwnd);
        }

        private static uint ResolveProcess(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return 0;
            Native.GetWindowThreadProcessId(hwnd, out var pid);

            // Las apps de la Tienda (UWP) viven dentro de ApplicationFrameHost:
            // el proceso real es el de la ventana hija.
            if (ProcessInfo.Get(pid)?.Name == "applicationframehost")
            {
                uint child = 0;
                Native.EnumChildWindows(hwnd, (h, _) =>
                {
                    Native.GetWindowThreadProcessId(h, out var p);
                    if (p != pid) { child = p; return false; }
                    return true;
                }, IntPtr.Zero);
                if (child != 0) pid = child;
            }
            return pid;
        }
    }

    internal static partial class Native
    {
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>Esquinas redondeadas en Windows 11 (en Windows 10 no hace nada).</summary>
        public static void RoundCorners(IntPtr hwnd)
        {
            try
            {
                var round = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref round, sizeof(int));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        public static Screen ScreenOf(IntPtr hwnd) =>
            hwnd != IntPtr.Zero ? Screen.FromHandle(hwnd) : Screen.FromPoint(Cursor.Position);

        public static Rectangle WorkingAreaAtCursor() =>
            Screen.FromPoint(Cursor.Position).WorkingArea;
    }
}
