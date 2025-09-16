using System;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor
{
    internal static class AppConfig
    {
        public static void Initialize()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
            }
            catch
            {
            }
            var defaultFont = new Font("Segoe UI", 9F);
            Application.SetDefaultFont(defaultFont);
        }
    }
}
