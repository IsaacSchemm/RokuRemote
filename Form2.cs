using CrossInterfaceRokuDeviceDiscovery;
using RokuDotNet.Client;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace RokuRemote {
    public partial class Form2 : Form {
        public Form2() {
            InitializeComponent();
        }

        public IRokuDeviceDiscoveryClient CreateDeviceDiscoveryClientFromSelectedOptions() {
            if (radioButton1.Checked) {
                return new UdpRokuDeviceDiscoveryClient();
            } else if (radioButton2.Checked) {
                var addresses = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .ToList();
                return new CrossInterfaceRokuDeviceDiscoveryClient(addresses);
            } else if (radioButton3.Checked) {
                return new ManualRokuDeviceDiscoveryClient(new[] { textBox1.Text });
            } else {
                return new ManualRokuDeviceDiscoveryClient(Enumerable.Empty<string>());
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e) {
            textBox1.Enabled = radioButton3.Checked;
        }
    }
}
