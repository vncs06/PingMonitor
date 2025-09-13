using System;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor
{
    public class MainForm : Form
    {
        private readonly PingPanel _panel1;
        private readonly PingPanel _panel2;
        private readonly PingPanel _panel3;
        private readonly PingPanel _panel4;

        public MainForm()
        {
            Text = "Ping Monitor - 4 Hosts";
            MinimumSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterScreen;

            _panel1 = new PingPanel();
            _panel2 = new PingPanel();
            _panel3 = new PingPanel();
            _panel4 = new PingPanel();

            // Valores padrão úteis
            _panel1.Host = "8.8.8.8";
            _panel2.Host = "1.1.1.1";
            _panel3.Host = "cloudflare.com";
            _panel4.Host = ""; // deixe em branco para o usuário definir

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(6)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Group each PingPanel with its own NetworkStatusPanel
            var gb1 = new GroupBox { Text = "Ping 1", Dock = DockStyle.Fill, Padding = new Padding(8) };
            var gb2 = new GroupBox { Text = "Ping 2", Dock = DockStyle.Fill, Padding = new Padding(8) };
            var gb3 = new GroupBox { Text = "Ping 3", Dock = DockStyle.Fill, Padding = new Padding(8) };
            var gb4 = new GroupBox { Text = "Ping 4", Dock = DockStyle.Fill, Padding = new Padding(8) };

            var net1 = new NetworkStatusPanel { Dock = DockStyle.Top, AdapterIndex = 0 };
            var net2 = new NetworkStatusPanel { Dock = DockStyle.Top, AdapterIndex = 1 };
            var net3 = new NetworkStatusPanel { Dock = DockStyle.Top, AdapterIndex = 2 };
            var net4 = new NetworkStatusPanel { Dock = DockStyle.Top, AdapterIndex = 3 };

            gb1.Controls.Add(_panel1);
            gb1.Controls.Add(net1);
            gb2.Controls.Add(_panel2);
            gb2.Controls.Add(net2);
            gb3.Controls.Add(_panel3);
            gb3.Controls.Add(net3);
            gb4.Controls.Add(_panel4);
            gb4.Controls.Add(net4);

            grid.Controls.Add(gb1, 0, 0);
            grid.Controls.Add(gb2, 1, 0);
            grid.Controls.Add(gb3, 0, 1);
            grid.Controls.Add(gb4, 1, 1);

            Controls.Add(grid);
            grid.BringToFront();
        }
    }
}
