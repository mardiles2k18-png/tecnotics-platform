using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace VolumeMixer
{
    internal sealed class WifiNetwork
    {
        public string Ssid { get; set; }
        public byte[] SsidBytes { get; set; }
        public string ProfileName { get; set; }
        public bool Connected { get; set; }
        public bool HasProfile => !string.IsNullOrEmpty(ProfileName);
        public int Signal { get; set; }          // 0-100
        public bool Secure { get; set; }
        public int Auth { get; set; }            // DOT11_AUTH_ALGORITHM
        public int Cipher { get; set; }          // DOT11_CIPHER_ALGORITHM

        public int Bars => Signal >= 75 ? 4 : Signal >= 50 ? 3 : Signal >= 25 ? 2 : 1;

        /// <summary>WPA/WPA2/WPA3 Enterprise: requieren usuario y certificado, se configuran en Windows.</summary>
        public bool IsEnterprise => Auth == 3 /* WPA */ || Auth == 6 /* RSNA */ || Auth == 8 || Auth == 11;
    }

    internal sealed class WifiStatus
    {
        public bool HasAdapter { get; set; }
        public bool RadioOn { get; set; }
        public bool HardwareOff { get; set; }
        public bool Connected { get; set; }
        /// <summary>Windows 11 exige el permiso de ubicacion para que las apps vean las redes cercanas.</summary>
        public bool LocationBlocked { get; set; }
        public List<WifiNetwork> Networks { get; } = new List<WifiNetwork>();
        public WifiNetwork Current => Networks.FirstOrDefault(n => n.Connected);
    }

    /// <summary>Wi-Fi mediante la API nativa de Windows (wlanapi.dll).</summary>
    internal sealed class WifiService : IDisposable
    {
        private IntPtr _handle;

        /// <param name="includeNetworks">
        /// false = solo adaptador, radio y si hay conexion. Pedir la lista de redes puede hacer que Windows 11
        /// pregunte por el permiso de ubicacion, asi que solo se pide con el panel abierto.
        /// </param>
        public WifiStatus GetStatus(bool includeNetworks = true)
        {
            var status = new WifiStatus();
            if (!TryGetInterface(out var guid, out var state)) return status;
            status.HasAdapter = true;
            status.Connected = state == 1; // wlan_interface_state_connected
            ReadRadio(guid, status);
            if (!status.RadioOn || !includeNetworks) return status;

            var err = WlanGetAvailableNetworkList(_handle, ref guid, 0, IntPtr.Zero, out var list);
            if (err == 5 /* ERROR_ACCESS_DENIED */) { status.LocationBlocked = true; return status; }
            if (err != 0) return status;
            try
            {
                var count = Marshal.ReadInt32(list, 0);
                var all = new List<WifiNetwork>();
                for (var i = 0; i < count; i++)
                {
                    var item = list + 8 + i * 628; // sizeof(WLAN_AVAILABLE_NETWORK)
                    var ssidLen = Math.Min(32, Marshal.ReadInt32(item, 512));
                    if (ssidLen == 0) continue; // red oculta
                    var ssid = new byte[ssidLen];
                    Marshal.Copy(item + 516, ssid, 0, ssidLen);
                    var flags = Marshal.ReadInt32(item, 620);
                    all.Add(new WifiNetwork
                    {
                        ProfileName = Marshal.PtrToStringUni(item),
                        SsidBytes = ssid,
                        Ssid = DecodeSsid(ssid),
                        Signal = Marshal.ReadInt32(item, 604),
                        Secure = Marshal.ReadInt32(item, 608) != 0,
                        Auth = Marshal.ReadInt32(item, 612),
                        Cipher = Marshal.ReadInt32(item, 616),
                        Connected = (flags & 1) != 0, // WLAN_AVAILABLE_NETWORK_CONNECTED
                    });
                }

                // Windows repite la misma red con y sin perfil: nos quedamos con la mas util.
                status.Networks.AddRange(all
                    .GroupBy(n => n.Ssid)
                    .Select(g => g.OrderByDescending(n => n.Connected).ThenByDescending(n => n.HasProfile).ThenByDescending(n => n.Signal).First())
                    .OrderByDescending(n => n.Connected)
                    .ThenByDescending(n => n.Signal));
            }
            finally { WlanFreeMemory(list); }
            return status;
        }

        /// <summary>Pide a Windows que busque redes; los resultados llegan unos segundos despues.</summary>
        public void Scan()
        {
            if (TryGetInterface(out var guid, out _)) WlanScan(_handle, ref guid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>Conecta usando el perfil guardado de la red. Devuelve null o un mensaje de error.</summary>
        public string Connect(string profileName)
        {
            if (!TryGetInterface(out var guid, out _)) return "No hay adaptador Wi‑Fi";
            var p = new WLAN_CONNECTION_PARAMETERS
            {
                wlanConnectionMode = 0, // wlan_connection_mode_profile
                strProfile = profileName,
                dot11BssType = 1,       // infraestructura
            };
            var err = WlanConnect(_handle, ref guid, ref p, IntPtr.Zero);
            return err == 0 ? null : $"No se pudo conectar (error {err})";
        }

        /// <summary>Crea un perfil para una red nueva (abierta o WPA/WPA2/WPA3 personal) y devuelve su nombre.</summary>
        public string CreateProfile(WifiNetwork network, string password, out string error)
        {
            error = null;
            if (!TryGetInterface(out var guid, out _)) { error = "No hay adaptador Wi‑Fi"; return null; }

            string auth, encryption;
            if (!network.Secure) { auth = "open"; encryption = "none"; }
            else if (network.Auth == 9) { auth = "WPA3SAE"; encryption = "AES"; }
            else if (network.Auth == 4) { auth = "WPAPSK"; encryption = network.Cipher == 2 ? "TKIP" : "AES"; }
            else { auth = "WPA2PSK"; encryption = network.Cipher == 2 ? "TKIP" : "AES"; }

            var name = SecurityElement.Escape(network.Ssid);
            var hex = BitConverter.ToString(network.SsidBytes).Replace("-", "");
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">");
            sb.Append($"<name>{name}</name><SSIDConfig><SSID><hex>{hex}</hex><name>{name}</name></SSID></SSIDConfig>");
            sb.Append("<connectionType>ESS</connectionType><connectionMode>auto</connectionMode><MSM><security>");
            sb.Append($"<authEncryption><authentication>{auth}</authentication><encryption>{encryption}</encryption><useOneX>false</useOneX></authEncryption>");
            if (network.Secure)
                sb.Append($"<sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{SecurityElement.Escape(password)}</keyMaterial></sharedKey>");
            sb.Append("</security></MSM></WLANProfile>");

            // Primero como perfil para todos los usuarios (lo normal); si la directiva no lo permite, solo para este usuario.
            var err = WlanSetProfile(_handle, ref guid, 0, sb.ToString(), null, true, IntPtr.Zero, out var reason);
            if (err != 0) err = WlanSetProfile(_handle, ref guid, 2 /* WLAN_PROFILE_USER */, sb.ToString(), null, true, IntPtr.Zero, out reason);
            if (err != 0)
            {
                error = reason != 0 ? $"Windows rechazó la configuración de la red (motivo {reason})" : $"No se pudo guardar la red (error {err})";
                return null;
            }
            return network.Ssid;
        }

        public void DeleteProfile(string profileName)
        {
            if (TryGetInterface(out var guid, out _)) WlanDeleteProfile(_handle, ref guid, profileName, IntPtr.Zero);
        }

        public string Disconnect()
        {
            if (!TryGetInterface(out var guid, out _)) return "No hay adaptador Wi‑Fi";
            var err = WlanDisconnect(_handle, ref guid, IntPtr.Zero);
            return err == 0 ? null : $"No se pudo desconectar (error {err})";
        }

        /// <summary>Enciende o apaga el Wi‑Fi (el interruptor de software, como en Configuracion rapida).</summary>
        public string SetRadio(bool on)
        {
            if (!TryGetInterface(out var guid, out _)) return "No hay adaptador Wi‑Fi";
            var phys = new List<int>();
            if (WlanQueryInterface(_handle, ref guid, 4 /* radio_state */, IntPtr.Zero, out _, out var data, IntPtr.Zero) == 0)
            {
                try
                {
                    var count = Marshal.ReadInt32(data, 0);
                    for (var i = 0; i < count; i++) phys.Add(Marshal.ReadInt32(data, 4 + i * 12));
                }
                finally { WlanFreeMemory(data); }
            }
            if (phys.Count == 0) phys.Add(0);

            var buf = Marshal.AllocHGlobal(12);
            try
            {
                foreach (var phy in phys)
                {
                    // WLAN_PHY_RADIO_STATE { dwPhyIndex, dot11SoftwareRadioState, dot11HardwareRadioState }
                    Marshal.WriteInt32(buf, 0, phy);
                    Marshal.WriteInt32(buf, 4, on ? 1 : 2);
                    Marshal.WriteInt32(buf, 8, on ? 1 : 2);
                    var err = WlanSetInterface(_handle, ref guid, 4, 12, buf, IntPtr.Zero);
                    if (err != 0) return $"Windows no permitió cambiar el Wi‑Fi (error {err})";
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
            return null;
        }

        private void ReadRadio(Guid guid, WifiStatus status)
        {
            status.RadioOn = true;
            if (WlanQueryInterface(_handle, ref guid, 4, IntPtr.Zero, out _, out var data, IntPtr.Zero) != 0) return;
            try
            {
                var count = Marshal.ReadInt32(data, 0);
                if (count == 0) return;
                var anyOn = false;
                for (var i = 0; i < count; i++)
                {
                    var sw = Marshal.ReadInt32(data, 4 + i * 12 + 4);
                    var hw = Marshal.ReadInt32(data, 4 + i * 12 + 8);
                    if (hw == 2) status.HardwareOff = true;
                    if (sw == 1 && hw == 1) anyOn = true;
                }
                status.RadioOn = anyOn;
            }
            finally { WlanFreeMemory(data); }
        }

        private bool TryGetInterface(out Guid guid, out int state)
        {
            guid = Guid.Empty;
            state = 0;
            if (_handle == IntPtr.Zero && WlanOpenHandle(2, IntPtr.Zero, out _, out _handle) != 0)
            {
                _handle = IntPtr.Zero; // servicio WLAN apagado o PC sin Wi‑Fi
                return false;
            }
            if (WlanEnumInterfaces(_handle, IntPtr.Zero, out var list) != 0) return false;
            try
            {
                if (Marshal.ReadInt32(list, 0) == 0) return false;
                // WLAN_INTERFACE_INFO { GUID; WCHAR[256]; WLAN_INTERFACE_STATE } despues de 8 bytes de cabecera
                var bytes = new byte[16];
                Marshal.Copy(list + 8, bytes, 0, 16);
                guid = new Guid(bytes);
                state = Marshal.ReadInt32(list, 8 + 16 + 512);
                return true;
            }
            finally { WlanFreeMemory(list); }
        }

        private static string DecodeSsid(byte[] ssid)
        {
            try { return new UTF8Encoding(false, true).GetString(ssid); }
            catch (ArgumentException) { return Encoding.Default.GetString(ssid); }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero) WlanCloseHandle(_handle, IntPtr.Zero);
            _handle = IntPtr.Zero;
        }

        // ----- wlanapi.dll -----

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_PARAMETERS
        {
            public int wlanConnectionMode;
            [MarshalAs(UnmanagedType.LPWStr)] public string strProfile;
            public IntPtr pDot11Ssid;
            public IntPtr pDesiredBssidList;
            public int dot11BssType;
            public int dwFlags;
        }

        [DllImport("wlanapi.dll")]
        private static extern int WlanOpenHandle(int clientVersion, IntPtr reserved, out int negotiatedVersion, out IntPtr handle);

        [DllImport("wlanapi.dll")]
        private static extern int WlanCloseHandle(IntPtr handle, IntPtr reserved);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr memory);

        [DllImport("wlanapi.dll")]
        private static extern int WlanEnumInterfaces(IntPtr handle, IntPtr reserved, out IntPtr list);

        [DllImport("wlanapi.dll")]
        private static extern int WlanGetAvailableNetworkList(IntPtr handle, ref Guid iface, int flags, IntPtr reserved, out IntPtr list);

        [DllImport("wlanapi.dll")]
        private static extern int WlanScan(IntPtr handle, ref Guid iface, IntPtr ssid, IntPtr ie, IntPtr reserved);

        [DllImport("wlanapi.dll")]
        private static extern int WlanConnect(IntPtr handle, ref Guid iface, ref WLAN_CONNECTION_PARAMETERS parameters, IntPtr reserved);

        [DllImport("wlanapi.dll")]
        private static extern int WlanDisconnect(IntPtr handle, ref Guid iface, IntPtr reserved);

        [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
        private static extern int WlanSetProfile(IntPtr handle, ref Guid iface, int flags, string xml, string security, bool overwrite, IntPtr reserved, out int reasonCode);

        [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
        private static extern int WlanDeleteProfile(IntPtr handle, ref Guid iface, string profileName, IntPtr reserved);

        [DllImport("wlanapi.dll")]
        private static extern int WlanQueryInterface(IntPtr handle, ref Guid iface, int opCode, IntPtr reserved, out int dataSize, out IntPtr data, IntPtr opcodeValueType);

        [DllImport("wlanapi.dll")]
        private static extern int WlanSetInterface(IntPtr handle, ref Guid iface, int opCode, int dataSize, IntPtr data, IntPtr reserved);
    }
}
