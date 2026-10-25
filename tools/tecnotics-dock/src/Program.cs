using System;
using System.Threading;
using System.Windows.Forms;

namespace TecnoticsDock
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (var mutex = new Mutex(true, @"Local\" + AppInfo.StorageName, out var isFirst))
            {
                if (!isFirst)
                {
                    MessageBox.Show(AppInfo.Name + " ya está abierto: búscalo en el botón flotante o en la bandeja del sistema.",
                        AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) => Log.Error(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Error(e.ExceptionObject as Exception);
                Application.Run(new TrayApp());
                GC.KeepAlive(mutex);
            }
        }
    }

    internal static class AppInfo
    {
        public const string Name = "Tecnotics Dock";

        /// <summary>
        /// Nombre de la carpeta de configuracion, la clave de registro y el mutex. Es el de la primera
        /// version ("Mezclador de volumen") y no se cambia para conservar la configuracion ya guardada.
        /// </summary>
        public const string StorageName = "TecnoticsVolumeMixer";
    }
}
