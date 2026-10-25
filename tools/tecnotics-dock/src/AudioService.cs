using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using TecnoticsDock.Interop;

namespace TecnoticsDock
{
    /// <summary>Algo cuyo volumen se puede controlar: un dispositivo o una aplicacion.</summary>
    internal interface IVolumeTarget
    {
        string Key { get; }
        string Title { get; }
        Image Icon { get; }
        bool IsDevice { get; }
        float Volume { get; set; }
        bool Muted { get; set; }
        float Peak { get; }
    }

    internal sealed class DeviceTarget : IVolumeTarget
    {
        private static Guid _ctx = Guid.Empty;
        private readonly IAudioEndpointVolume _endpoint;
        private readonly IAudioMeterInformation _meter;

        public DeviceTarget(string id, string name, bool isDefault, IAudioEndpointVolume endpoint, IAudioMeterInformation meter)
        {
            Id = id;
            Title = name;
            IsDefault = isDefault;
            _endpoint = endpoint;
            _meter = meter;
        }

        public string Id { get; }
        public bool IsDefault { get; }
        public string Key => "dev|" + Id;
        public string Title { get; }
        public Image Icon => Icons.Speaker;
        public bool IsDevice => true;
        public List<AppTarget> Apps { get; } = new List<AppTarget>();

        public float Volume
        {
            get { try { _endpoint.GetMasterVolumeLevelScalar(out var v); return v; } catch { return 0; } }
            set { try { _endpoint.SetMasterVolumeLevelScalar(Clamp(value), ref _ctx); } catch { } }
        }

        public bool Muted
        {
            get { try { _endpoint.GetMute(out var m); return m; } catch { return false; } }
            set { try { _endpoint.SetMute(value, ref _ctx); } catch { } }
        }

        public float Peak
        {
            get { try { return _meter != null && _meter.GetPeakValue(out var p) == 0 ? p : 0; } catch { return 0; } }
        }

        internal static float Clamp(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }

    /// <summary>Todas las sesiones de audio de un mismo programa en un dispositivo.</summary>
    internal sealed class AppTarget : IVolumeTarget
    {
        private static Guid _ctx = Guid.Empty;
        internal readonly List<(ISimpleAudioVolume Volume, IAudioMeterInformation Meter, uint Pid)> Sessions =
            new List<(ISimpleAudioVolume, IAudioMeterInformation, uint)>();

        public AppTarget(DeviceTarget device, string processName, string title, Image icon)
        {
            Device = device;
            ProcessName = processName;
            Title = title;
            Icon = icon;
        }

        public DeviceTarget Device { get; }
        public string ProcessName { get; }
        public string Key => "app|" + Device.Id + "|" + ProcessName;
        public string Title { get; }
        public Image Icon { get; }
        public bool IsDevice => false;

        public float Volume
        {
            get
            {
                foreach (var s in Sessions)
                {
                    try { s.Volume.GetMasterVolume(out var v); return v; } catch { }
                }
                return 0;
            }
            set
            {
                var v = DeviceTarget.Clamp(value);
                foreach (var s in Sessions)
                {
                    try { s.Volume.SetMasterVolume(v, ref _ctx); } catch { }
                }
            }
        }

        public bool Muted
        {
            get
            {
                foreach (var s in Sessions)
                {
                    try { s.Volume.GetMute(out var m); return m; } catch { }
                }
                return false;
            }
            set
            {
                foreach (var s in Sessions)
                {
                    try { s.Volume.SetMute(value, ref _ctx); } catch { }
                }
            }
        }

        public float Peak
        {
            get
            {
                float max = 0;
                foreach (var s in Sessions)
                {
                    try
                    {
                        if (s.Meter != null && s.Meter.GetPeakValue(out var p) == 0 && p > max) max = p;
                    }
                    catch { }
                }
                return max;
            }
        }
    }

    /// <summary>Lee los dispositivos de salida activos y las sesiones de audio de cada uno.</summary>
    internal sealed class AudioService
    {
        private const string SystemSoundsKey = "#system";
        private readonly IMMDeviceEnumerator _enumerator =
            (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        private readonly Dictionary<string, (IAudioEndpointVolume Endpoint, IAudioMeterInformation Meter, string Name)> _deviceCache =
            new Dictionary<string, (IAudioEndpointVolume, IAudioMeterInformation, string)>();

        /// <summary>Foto actual: dispositivo predeterminado primero, cada uno con sus aplicaciones.</summary>
        public List<DeviceTarget> GetSnapshot()
        {
            var result = new List<DeviceTarget>();
            string defaultId = null;
            if (_enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out var def) == 0 && def != null)
            {
                try { def.GetId(out defaultId); } catch { }
            }

            if (_enumerator.EnumAudioEndpoints(EDataFlow.Render, CoreAudioConst.DEVICE_STATE_ACTIVE, out var collection) != 0)
                return result;

            collection.GetCount(out var count);
            var seenPids = new HashSet<uint>();
            for (var i = 0; i < count; i++)
            {
                try
                {
                    collection.Item(i, out var device);
                    device.GetId(out var id);
                    var cached = GetCachedDevice(id, device);
                    var target = new DeviceTarget(id, cached.Name, id == defaultId, cached.Endpoint, cached.Meter);
                    LoadSessions(device, target, seenPids);
                    result.Add(target);
                }
                catch (COMException)
                {
                    // El dispositivo desaparecio mientras lo leiamos.
                }
            }

            return result.OrderByDescending(d => d.IsDefault).ToList();
        }

        /// <summary>Busca las aplicaciones (en todos los dispositivos) que pertenecen al proceso indicado.</summary>
        public List<AppTarget> FindApps(uint pid)
        {
            var info = ProcessInfo.Get(pid);
            if (info == null) return new List<AppTarget>();

            var apps = GetSnapshot().SelectMany(d => d.Apps).ToList();
            var byName = apps.Where(a => a.ProcessName == info.Name).ToList();
            if (byName.Count > 0) return byName;

            // Algunos programas reproducen el audio desde un proceso hijo con otro nombre.
            var parents = ProcessInfo.GetParentMap();
            return apps.Where(a => a.Sessions.Any(s => ProcessInfo.IsDescendant(s.Pid, pid, parents))).ToList();
        }

        private (IAudioEndpointVolume Endpoint, IAudioMeterInformation Meter, string Name) GetCachedDevice(string id, IMMDevice device)
        {
            if (_deviceCache.TryGetValue(id, out var cached)) return cached;

            var iid = CoreAudioConst.IID_IAudioEndpointVolume;
            device.Activate(ref iid, CoreAudioConst.CLSCTX_ALL, IntPtr.Zero, out var endpointObj);
            IAudioMeterInformation meter = null;
            try
            {
                var meterIid = CoreAudioConst.IID_IAudioMeterInformation;
                device.Activate(ref meterIid, CoreAudioConst.CLSCTX_ALL, IntPtr.Zero, out var meterObj);
                meter = (IAudioMeterInformation)meterObj;
            }
            catch (COMException) { }

            cached = ((IAudioEndpointVolume)endpointObj, meter, ReadFriendlyName(device));
            _deviceCache[id] = cached;
            return cached;
        }

        private static string ReadFriendlyName(IMMDevice device)
        {
            try
            {
                device.OpenPropertyStore(0 /* STGM_READ */, out var store);
                var key = CoreAudioConst.PKEY_Device_FriendlyName;
                store.GetValue(ref key, out var value);
                var name = value.VarType == 31 /* VT_LPWSTR */ ? Marshal.PtrToStringUni(value.Pointer) : null;
                PropVariant.PropVariantClear(ref value);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (COMException) { }
            return "Dispositivo de audio";
        }

        private static void LoadSessions(IMMDevice device, DeviceTarget target, HashSet<uint> seenPids)
        {
            // Se activa un administrador nuevo en cada lectura: el enumerador de uno
            // reutilizado no siempre refleja las sesiones creadas despues.
            var iid = CoreAudioConst.IID_IAudioSessionManager2;
            device.Activate(ref iid, CoreAudioConst.CLSCTX_ALL, IntPtr.Zero, out var managerObj);
            var manager = (IAudioSessionManager2)managerObj;
            manager.GetSessionEnumerator(out var sessions);
            sessions.GetCount(out var count);

            var byKey = new Dictionary<string, AppTarget>();
            for (var i = 0; i < count; i++)
            {
                try
                {
                    sessions.GetSession(i, out var session);
                    if (session.GetState(out var state) != 0 || state == CoreAudioConst.AudioSessionStateExpired)
                        continue;

                    var isSystem = session.IsSystemSoundsSession() == 0;
                    session.GetProcessId(out var pid);

                    string key, title;
                    Image icon;
                    if (isSystem)
                    {
                        key = SystemSoundsKey;
                        title = "Sonidos del sistema";
                        icon = Icons.Bell;
                    }
                    else
                    {
                        var info = ProcessInfo.Get(pid);
                        if (info == null) continue; // El proceso ya termino.
                        seenPids.Add(pid);
                        key = info.Name;
                        title = info.Title;
                        icon = info.Icon;
                    }

                    if (!byKey.TryGetValue(key, out var app))
                    {
                        app = new AppTarget(target, key, title, icon);
                        byKey[key] = app;
                    }

                    var meter = session as IAudioMeterInformation;
                    app.Sessions.Add(((ISimpleAudioVolume)session, meter, pid));
                }
                catch (COMException) { }
                catch (InvalidCastException) { }
            }

            // Aplicaciones en orden alfabetico y los sonidos del sistema al final.
            target.Apps.AddRange(byKey.Values
                .OrderBy(a => a.ProcessName == SystemSoundsKey)
                .ThenBy(a => a.Title, StringComparer.CurrentCultureIgnoreCase));
        }
    }
}
