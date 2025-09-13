using System;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace PingMonitor
{
    public class NetworkStatusPanel : UserControl
    {
        private readonly Label _lblTitle;
        private readonly Label _lblAdapter;
        private readonly Label _lblSpeed;
        private readonly Label _lblStatus;
        private readonly System.Windows.Forms.Timer _timer;

        public int AdapterIndex { get; set; } = -1;

        public int RefreshIntervalMs { get; set; } = 3000;

        public NetworkStatusPanel()
        {
            Height = 70;
            Dock = DockStyle.Top;
            Padding = new Padding(8, 6, 8, 6);
            BackColor = Color.WhiteSmoke;

            _lblTitle = new Label { Text = "Rede (Cabo):", AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold), Location = new Point(8, 8) };
            _lblAdapter = new Label { Text = "Adaptador: -", AutoSize = true, Location = new Point(8, 32) };
            _lblSpeed = new Label { Text = "Velocidade: -", AutoSize = true, Location = new Point(280, 32) };
            _lblStatus = new Label { Text = "Status: -", AutoSize = true, Location = new Point(520, 28), Font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold) };

            Controls.Add(_lblTitle);
            Controls.Add(_lblAdapter);
            Controls.Add(_lblSpeed);
            Controls.Add(_lblStatus);

            _timer = new System.Windows.Forms.Timer();
            _timer.Tick += (s, e) => RefreshStatus();
            _timer.Interval = RefreshIntervalMs;
            _timer.Start();

            Disposed += (s, e) => _timer.Dispose();

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            try
            {
                var wired = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => IsWired(nic.NetworkInterfaceType))
                    .OrderBy(nic => nic.Name)
                    .ToList();

                if (wired.Count == 0)
                {
                    ShowStatus("Nenhum adaptador cabeado", "-", "-", Color.Crimson);
                    return;
                }

                NetworkInterface nic;
                if (AdapterIndex >= 0 && AdapterIndex < wired.Count)
                    nic = wired[AdapterIndex];
                else
                    nic = wired.First();

                var status = nic.OperationalStatus;
                var speedBitsPerSec = nic.Speed;
                var (speedText, color) = FormatSpeed(speedBitsPerSec);

                _lblAdapter.Text = $"Adaptador: {nic.Name}";
                _lblSpeed.Text = status == OperationalStatus.Up ? $"Velocidade: {speedText}" : "Velocidade: -";
                _lblStatus.Text = status == OperationalStatus.Up ? "Status: Conectado" : "Status: Desconectado";
                _lblStatus.ForeColor = status == OperationalStatus.Up ? color : Color.Crimson;
            }
            catch (Exception ex)
            {
                ShowStatus("Erro ao ler rede", "-", ex.Message, Color.Crimson);
            }
        }

        private static bool IsWired(NetworkInterfaceType type)
        {
            return type == NetworkInterfaceType.Ethernet
                || type == NetworkInterfaceType.GigabitEthernet
                || type == NetworkInterfaceType.FastEthernetFx
                || type == NetworkInterfaceType.FastEthernetT
                || type == NetworkInterfaceType.Ethernet3Megabit;
        }

        private void ShowStatus(string status, string speed, string details, Color color)
        {
            _lblAdapter.Text = $"Adaptador: {details}";
            _lblSpeed.Text = $"Velocidade: {speed}";
            _lblStatus.Text = $"Status: {status}";
            _lblStatus.ForeColor = color;
        }

        private static (string text, Color color) FormatSpeed(long bitsPerSec)
        {
            // Valores típicos: 100_000_000 (100 Mbps), 1_000_000_000 (1 Gbps)
            if (bitsPerSec >= 1_000_000_000)
            {
                // Converter para Gbps com 1 casa decimal
                double gbps = bitsPerSec / 1_000_000_000.0;
                return ($"{gbps:0.0} Gbps", Color.ForestGreen);
            }
            else if (bitsPerSec >= 1_000_000)
            {
                double mbps = bitsPerSec / 1_000_000.0;
                return ($"{mbps:0} Mbps", Color.DarkOrange);
            }
            else if (bitsPerSec > 0)
            {
                return ($"{bitsPerSec} bps", Color.Gray);
            }
            return ("-", Color.Gray);
        }
    }
}
