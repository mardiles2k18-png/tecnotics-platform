using System;
using System.Threading;
using System.Windows.Forms;

namespace VolumeMixer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (var mutex = new Mutex(true, @"Local\TecnoticsVolumeMixer", out var isFirst))
            {
                if (!isFirst)
                {
                    MessageBox.Show("El mezclador ya está abierto: búscalo en la bandeja del sistema (junto al reloj).",
                        "Mezclador de volumen", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
}
