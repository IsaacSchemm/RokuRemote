using CrossInterfaceRokuDeviceDiscovery;
using RokuDotNet.Client;
using RokuDotNet.Client.Apps;
using RokuDotNet.Client.Input;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RokuRemote {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Using default names from Windows Forms designer")]
    public partial class Form1 : Form {
        public record Roku(IRokuDevice Device, DeviceInfo Info) : IRokuDevice {
            public IRokuDeviceInput Input => Device.Input;
            public string Id => Device.Id;
            public IRokuDeviceApps Apps => Device.Apps;
            IRokuDeviceInput IRokuDevice.Input => Device.Input;

            Task<DeviceInfo> IRokuDevice.GetDeviceInfoAsync(CancellationToken _) {
                return Task.FromResult(Info);
            }

            public void Dispose() => Device.Dispose();

            public override string ToString() => $"{Info.UserDeviceName} ({Info.ModelName})";
        }

        public IRokuDevice CurrentDevice => comboBox1.SelectedItem as IRokuDevice;

        public Form1() {
            InitializeComponent();
        }

        private async void Form1_Shown(object sender, EventArgs e) {
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(15));

            var addresses = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .ToList();
            var client = new CrossInterfaceRokuDeviceDiscoveryClient(addresses);

            try {
                await client.DiscoverDevicesAsync(async ctx => {
                    var info = await ctx.Device.GetDeviceInfoAsync();
                    var roku = new Roku(ctx.Device, info);

                    this.BeginInvoke(new Action(() => {
                        comboBox1.Items.Add(roku);
                        if (comboBox1.SelectedIndex == -1)
                            comboBox1.SelectedIndex = 0;
                        comboBox1.Enabled = true;
                    }));

                    return false;
                }, tokenSource.Token);
            } catch (TaskCanceledException ex) when (ex.CancellationToken == tokenSource.Token) { }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            tableLayoutPanel1.Enabled = CurrentDevice != null;
        }

        private async void button2_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Back));
        }

        private async void button3_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Home));
        }

        private async void button4_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Up));
        }

        private async void button5_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Left));
        }

        private async void button6_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Select));
        }

        private async void button7_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Right));
        }

        private async void button8_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Down));
        }

        private async void button9_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.InstantReplay));
        }

        private async void button10_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Info));
        }

        private async void button11_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Reverse));
        }

        private async void button12_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Play));
        }

        private async void button13_Click(object sender, EventArgs e) {
            await CurrentDevice.Input.KeyPressAsync(new PressedKey(SpecialKeys.Forward));
        }
    }
}
