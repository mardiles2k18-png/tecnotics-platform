using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace VolumeMixer
{
    /// <summary>Un disco externo (USB, tarjeta SD...) con todas sus letras de unidad.</summary>
    internal sealed class ExternalDisk
    {
        public int DiskNumber { get; set; }
        public string Model { get; set; }
        public string Bus { get; set; }
        public List<DriveInfo> Volumes { get; } = new List<DriveInfo>();

        public string Key => "disk|" + DiskNumber;

        public string Title
        {
            get
            {
                var parts = Volumes.Select(v =>
                {
                    var label = SafeLabel(v);
                    var letter = v.Name.TrimEnd('\\');
                    return string.IsNullOrEmpty(label) ? letter : $"{label} ({letter})";
                });
                return string.Join(", ", parts);
            }
        }

        public string Detail
        {
            get
            {
                long free = 0, total = 0;
                foreach (var v in Volumes)
                {
                    try { free += v.AvailableFreeSpace; total += v.TotalSize; } catch (IOException) { }
                }
                var size = total > 0 ? $"{FormatSize(free)} libres de {FormatSize(total)}" : "";
                return string.Join(" · ", new[] { size, Model, Bus }.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private static string SafeLabel(DriveInfo d)
        {
            try { return d.VolumeLabel; } catch (IOException) { return null; } catch (UnauthorizedAccessException) { return null; }
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes;
            var u = 0;
            while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
            return v.ToString(v >= 100 || u == 0 ? "0" : "0.#") + " " + units[u];
        }
    }

    /// <summary>Lista los discos externos y los expulsa de forma segura (como "Quitar hardware de forma segura").</summary>
    internal static class DriveService
    {
        public static List<ExternalDisk> GetExternalDisks()
        {
            var systemRoot = Path.GetPathRoot(Environment.SystemDirectory)?.ToUpperInvariant();
            var disks = new Dictionary<int, ExternalDisk>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Fixed) continue;
                if (string.Equals(drive.Name, systemRoot, StringComparison.OrdinalIgnoreCase)) continue;
                if (!drive.IsReady) continue;

                var letter = drive.Name.Substring(0, 2); // "E:"
                using (var h = OpenVolume(@"\\.\" + letter))
                {
                    if (h.IsInvalid) continue;
                    if (!TryGetDeviceNumber(h, out var number)) continue;
                    var (bus, model) = QueryDevice(h);
                    var external = drive.DriveType == DriveType.Removable || IsExternalBus(bus);
                    if (!external) continue;

                    if (!disks.TryGetValue(number, out var disk))
                    {
                        disk = new ExternalDisk { DiskNumber = number, Model = model, Bus = BusName(bus) };
                        disks[number] = disk;
                    }
                    disk.Volumes.Add(drive);
                }
            }

            // Si el disco del sistema comparte numero con otro volumen, nunca ofrecerlo.
            if (systemRoot != null)
            {
                using (var h = OpenVolume(@"\\.\" + systemRoot.Substring(0, 2)))
                    if (!h.IsInvalid && TryGetDeviceNumber(h, out var sys)) disks.Remove(sys);
            }
            return disks.Values.OrderBy(d => d.Volumes[0].Name).ToList();
        }

        /// <summary>Expulsa el disco. Devuelve null si salio bien, o el motivo si Windows no lo permitio.</summary>
        public static string Eject(ExternalDisk disk)
        {
            var devInst = FindDiskDevInst(disk.DiskNumber);
            if (devInst == 0) return "No se encontró el dispositivo (¿ya se desconectó?)";

            // Se expulsa el padre del disco (el dispositivo USB), igual que hace Windows.
            if (CM_Get_Parent(out var parent, devInst, 0) != 0) parent = devInst;

            var vetoType = 0;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var vetoName = new StringBuilder(260);
                var cr = CM_Request_Device_EjectW(parent, out vetoType, vetoName, vetoName.Capacity, 0);
                if (cr == 0 && vetoType == 0) return null;
                System.Threading.Thread.Sleep(300);
            }
            // PNP_VETO_TYPE 2..6: cierre pendiente, app, servicio, archivo abierto u otro dispositivo usandolo.
            return vetoType >= 2 && vetoType <= 6
                ? "Un programa está usando el disco. Cierra los archivos abiertos e inténtalo de nuevo."
                : $"Windows no permitió expulsarlo (código {vetoType}).";
        }

        private static bool IsExternalBus(int bus) => bus == 7 /* USB */ || bus == 4 /* 1394 */ || bus == 12 /* SD */ || bus == 13 /* MMC */;

        private static string BusName(int bus)
        {
            switch (bus)
            {
                case 7: return "USB";
                case 4: return "FireWire";
                case 12: return "SD";
                case 13: return "MMC";
                default: return null;
            }
        }

        private static SafeFileHandle OpenVolume(string path) =>
            CreateFileW(path, 0, 3 /* FILE_SHARE_READ | FILE_SHARE_WRITE */, IntPtr.Zero, 3 /* OPEN_EXISTING */, 0, IntPtr.Zero);

        private static bool TryGetDeviceNumber(SafeFileHandle h, out int number)
        {
            number = -1;
            var buf = new int[3]; // STORAGE_DEVICE_NUMBER: DeviceType, DeviceNumber, PartitionNumber
            if (!DeviceIoControl(h, 0x2D1080 /* IOCTL_STORAGE_GET_DEVICE_NUMBER */, IntPtr.Zero, 0, buf, 12, out _, IntPtr.Zero))
                return false;
            number = buf[1];
            return true;
        }

        /// <summary>Tipo de bus y "Fabricante Modelo" del disco que contiene el volumen.</summary>
        private static (int Bus, string Model) QueryDevice(SafeFileHandle h)
        {
            var query = new byte[12]; // STORAGE_PROPERTY_QUERY { StorageDeviceProperty, PropertyStandardQuery }
            var output = Marshal.AllocHGlobal(1024);
            try
            {
                var queryPtr = Marshal.AllocHGlobal(query.Length);
                try
                {
                    Marshal.Copy(query, 0, queryPtr, query.Length);
                    if (!DeviceIoControl(h, 0x2D1400 /* IOCTL_STORAGE_QUERY_PROPERTY */, queryPtr, query.Length, output, 1024, out var returned, IntPtr.Zero) || returned < 32)
                        return (0, null);
                }
                finally { Marshal.FreeHGlobal(queryPtr); }

                // STORAGE_DEVICE_DESCRIPTOR
                var vendorOffset = Marshal.ReadInt32(output, 12);
                var productOffset = Marshal.ReadInt32(output, 16);
                var bus = Marshal.ReadInt32(output, 28);
                string Read(int offset) => offset > 0 && offset < 1024 ? Marshal.PtrToStringAnsi(output + offset)?.Trim() : null;
                var model = string.Join(" ", new[] { Read(vendorOffset), Read(productOffset) }.Where(s => !string.IsNullOrEmpty(s)));
                return (bus, model.Length > 0 ? model : null);
            }
            finally { Marshal.FreeHGlobal(output); }
        }

        /// <summary>Busca entre los discos presentes el que tiene ese numero y devuelve su nodo de dispositivo.</summary>
        private static int FindDiskDevInst(int diskNumber)
        {
            var diskGuid = new Guid("53F56307-B6BF-11D0-94F2-00A0C91EFB8B"); // GUID_DEVINTERFACE_DISK
            var set = SetupDiGetClassDevsW(ref diskGuid, IntPtr.Zero, IntPtr.Zero, 0x2 | 0x10 /* PRESENT | DEVICEINTERFACE */);
            if (set == IntPtr.Zero || set == new IntPtr(-1)) return 0;
            var detail = Marshal.AllocHGlobal(2048);
            try
            {
                for (var i = 0; ; i++)
                {
                    var iface = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA)) };
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref diskGuid, i, ref iface)) break;

                    var info = new SP_DEVINFO_DATA { cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)) };
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6); // SP_DEVICE_INTERFACE_DETAIL_DATA_W.cbSize
                    if (!SetupDiGetDeviceInterfaceDetailW(set, ref iface, detail, 2048, out _, ref info)) continue;

                    var path = Marshal.PtrToStringUni(detail + 4);
                    using (var h = OpenVolume(path))
                        if (!h.IsInvalid && TryGetDeviceNumber(h, out var n) && n == diskNumber) return info.DevInst;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(detail);
                SetupDiDestroyDeviceInfoList(set);
            }
            return 0;
        }

        // ----- Win32 -----

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle h, uint code, IntPtr inBuf, int inSize, [Out] int[] outBuf, int outSize, out int returned, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle h, uint code, IntPtr inBuf, int inSize, IntPtr outBuf, int outSize, out int returned, IntPtr overlapped);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, IntPtr enumerator, IntPtr parent, int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr devInfo, ref Guid guid, int index, ref SP_DEVICE_INTERFACE_DATA data);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr set, ref SP_DEVICE_INTERFACE_DATA data, IntPtr detail, int size, out int required, ref SP_DEVINFO_DATA info);

        [DllImport("setupapi.dll")]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

        [DllImport("cfgmgr32.dll")]
        private static extern int CM_Get_Parent(out int parent, int devInst, int flags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        private static extern int CM_Request_Device_EjectW(int devInst, out int vetoType, StringBuilder vetoName, int nameLength, int flags);
    }
}
