using System;
using System.Windows.Forms;

namespace PingMonitor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            AppConfig.Initialize();
            Application.Run(new MainForm());
        }
    }
}
