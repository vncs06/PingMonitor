using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingMonitor
{
    public class PingPanel : UserControl
    {
        private readonly TextBox _txtHost;
        private readonly NumericUpDown _intervalUpDown;
    private readonly Button _btnStartStop;
    private readonly ComboBox _cmbSource;
    private readonly Label _lblSource;
        private readonly Label _lblStatus;
        private readonly Label _lblLatency;
        private readonly System.Windows.Forms.Timer _timer;
        private bool _running;
        private bool _busy;

        public string Host
        {
            get => _txtHost.Text.Trim();
            set => _txtHost.Text = value;
        }

        public int IntervalMs
        {
            get => (int)_intervalUpDown.Value;
            set => _intervalUpDown.Value = Math.Min(Math.Max(value, (int)_intervalUpDown.Minimum), (int)_intervalUpDown.Maximum);
        }

        public PingPanel()
        {
            Height = 170;
            Dock = DockStyle.Fill;

            var lblHost = new Label { Text = "IP/Host:", AutoSize = true, Location = new Point(8, 12) };
            _txtHost = new TextBox { Location = new Point(70, 8), Width = 180, Text = "8.8.8.8" };

            var lblInterval = new Label { Text = "Intervalo (ms):", AutoSize = true, Location = new Point(8, 44) };
            _intervalUpDown = new NumericUpDown { Location = new Point(100, 40), Width = 80, Minimum = 250, Maximum = 60000, Increment = 250, Value = 1000 };

            _btnStartStop = new Button { Text = "Iniciar", Location = new Point(260, 8), Width = 90, Height = 28 };
            _btnStartStop.Click += BtnStartStop_Click;

            _lblSource = new Label { Text = "Origem:", AutoSize = true, Location = new Point(200, 44) };
            _cmbSource = new ComboBox { Location = new Point(250, 40), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSource.DropDown += (s, e) => PopulateSources();
            PopulateSources();

            _lblStatus = new Label { Text = "Status: aguardando", AutoSize = true, Location = new Point(8, 90), ForeColor = Color.DimGray, Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold) };
            _lblLatency = new Label { Text = "Latência:", AutoSize = true, Location = new Point(8, 118), ForeColor = Color.DimGray };

            Controls.Add(lblHost);
            Controls.Add(_txtHost);
            Controls.Add(lblInterval);
            Controls.Add(_intervalUpDown);
            Controls.Add(_btnStartStop);
            Controls.Add(_lblSource);
            Controls.Add(_cmbSource);
            Controls.Add(_lblStatus);
            Controls.Add(_lblLatency);

            _timer = new System.Windows.Forms.Timer();
            _timer.Tick += async (s, e) => await DoPing();
            Disposed += (s, e) => { _timer.Stop(); _timer.Dispose(); };
        }

        private void BtnStartStop_Click(object? sender, EventArgs e)
        {
            if (!_running)
            {
                _running = true;
                _btnStartStop.Text = "Parar";
                _txtHost.Enabled = false;
                _intervalUpDown.Enabled = true;
                _timer.Interval = (int)_intervalUpDown.Value;
                _timer.Start();
                _ = DoPing();
            }
            else
            {
                _running = false;
                _btnStartStop.Text = "Iniciar";
                _txtHost.Enabled = true;
                _timer.Stop();
                UpdateStatus("parado", Color.DimGray);
                _lblLatency.Text = "Latência:";
            }
        }

        private async Task DoPing()
        {
            if (_busy) return;
            _busy = true;

            string host = Host;
            if (string.IsNullOrWhiteSpace(host))
            {
                UpdateStatus("Informe um IP ou host.", Color.DarkRed);
                _busy = false;
                return;
            }

            try
            {
                var selected = _cmbSource.SelectedItem as SourceItem;
                if (selected != null && selected.IPAddress != null)
                {
                    var result = await PingViaProcess(host, selected.IPAddress);
                    if (result.success)
                    {
                        UpdateStatus($"Online", Color.ForestGreen);
                        _lblLatency.Text = $"Latência: {result.latencyMs}";
                    }
                    else
                    {
                        UpdateStatus("Offline", Color.Crimson);
                        _lblLatency.Text = "Latência: -";
                    }
                }
                else
                {
                    using var pinger = new Ping();
                    var reply = await pinger.SendPingAsync(host, 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        UpdateStatus($"Online ({reply.Address})", Color.ForestGreen);
                        _lblLatency.Text = $"Latência: {reply.RoundtripTime} ms";
                    }
                    else
                    {
                        UpdateStatus($"Offline: {reply.Status}", Color.Crimson);
                        _lblLatency.Text = "Latência: -";
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro: {ex.Message}", Color.Crimson);
                _lblLatency.Text = "Latência: -";
            }
            finally
            {
                if (_running)
                {
                    _timer.Interval = (int)_intervalUpDown.Value;
                }
                _busy = false;
            }
        }

        private void UpdateStatus(string text, Color color)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateStatus(text, color)));
                return;
            }
            _lblStatus.Text = $"Status: {text}";
            _lblStatus.ForeColor = color;
        }

        private void PopulateSources()
        {
            var selected = _cmbSource.SelectedItem as SourceItem;
            var selectedIp = selected?.IPAddress?.ToString();

            _cmbSource.Items.Clear();
            _cmbSource.Items.Add(new SourceItem { Display = "Auto (rota padrão)", IPAddress = null });

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                         .Where(n => IsWired(n.NetworkInterfaceType)))
            {
                var props = nic.GetIPProperties();
                foreach (var uni in props.UnicastAddresses)
                {
                    if (uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var item = new SourceItem { Display = $"{nic.Name} - {uni.Address}", IPAddress = uni.Address };
                        _cmbSource.Items.Add(item);
                    }
                }
            }

            // Reselect previous if present
            for (int i = 0; i < _cmbSource.Items.Count; i++)
            {
                var it = _cmbSource.Items[i] as SourceItem;
                if (it?.IPAddress?.ToString() == selectedIp)
                {
                    _cmbSource.SelectedIndex = i;
                    return;
                }
            }
            _cmbSource.SelectedIndex = 0;
        }

        private static bool IsWired(NetworkInterfaceType type)
        {
            return type == NetworkInterfaceType.Ethernet
                || type == NetworkInterfaceType.GigabitEthernet
                || type == NetworkInterfaceType.FastEthernetFx
                || type == NetworkInterfaceType.FastEthernetT
                || type == NetworkInterfaceType.Ethernet3Megabit;
        }

        private async Task<(bool success, string latencyMs)> PingViaProcess(string host, IPAddress source)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = $"-n 1 -w 2000 -4 -S {source} {host}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            var started = proc.Start();
            if (!started) return (false, "-");
            string output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            bool ok = proc.ExitCode == 0;

            string latency = "-";
            if (!string.IsNullOrEmpty(output))
            {
                // Match number followed by optional space and ms
                var m = Regex.Match(output, @"(?i)(?:time|tempo)[=\s]*([0-9]+)\s*ms");
                if (m.Success) latency = m.Groups[1].Value + " ms";
            }

            return (ok, latency);
        }

        private class SourceItem
        {
            public string Display { get; set; } = string.Empty;
            public IPAddress? IPAddress { get; set; }
            public override string ToString() => Display;
        }
    }
}
