using CrossInterfaceRokuDeviceDiscovery;
using RokuDotNet.Client;
using RokuDotNet.Client.Apps;
using RokuDotNet.Client.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RokuRemote {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Using default names from Windows Forms designer")]
    public partial class Form1 : Form {
        public record Roku(IHttpRokuDevice Device, DeviceInfo Info) : IHttpRokuDevice {
            public IRokuDeviceInput Input => Device.Input;
            public string Id => Device.Id;
            public IRokuDeviceApps Apps => Device.Apps;
            IRokuDeviceInput IRokuDevice.Input => Device.Input;
            public Uri Location => Device.Location;

            Task<DeviceInfo> IRokuDevice.GetDeviceInfoAsync(CancellationToken _) {
                return Task.FromResult(Info);
            }

            public void Dispose() => Device.Dispose();

            public override string ToString() => $"{Info.UserDeviceName} ({Info.ModelName})";
        }

        public IHttpRokuDevice CurrentDevice => comboBox1.SelectedItem as IHttpRokuDevice;

        private bool keyboardControlEnabled => checkBox1.Checked;

        public Form1() {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            comboBox1.Focus();

            groupBox1.Enabled = !checkBox1.Checked;
            groupBox2.Enabled = !checkBox1.Checked;
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
                    if (ctx.Device is IHttpRokuDevice h) {
                        var info = await ctx.Device.GetDeviceInfoAsync();
                        var roku = new Roku(h, info);

                        this.BeginInvoke(new Action(() => {
                            comboBox1.Items.Add(roku);
                            if (comboBox1.SelectedIndex == -1)
                                comboBox1.SelectedIndex = 0;
                        }));
                    }

                    return false;
                }, tokenSource.Token);
            } catch (TaskCanceledException ex) when (ex.CancellationToken == tokenSource.Token) { }
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

        private static PressedKey? ToKey(Keys k) {
            return k switch {
                Keys.Escape => new PressedKey(SpecialKeys.Back),
                Keys.Home => new PressedKey(SpecialKeys.Home),

                Keys.Up => new PressedKey(SpecialKeys.Up),
                Keys.Down => new PressedKey(SpecialKeys.Down),
                Keys.Left => new PressedKey(SpecialKeys.Left),
                Keys.Right => new PressedKey(SpecialKeys.Right),
                Keys.Enter => new PressedKey(SpecialKeys.Select),

                Keys.Back => new PressedKey(SpecialKeys.Backspace),

                Keys.MediaPlayPause => new PressedKey(SpecialKeys.Play),

                Keys.PageUp => new PressedKey(SpecialKeys.ChannelUp),
                Keys.PageDown => new PressedKey(SpecialKeys.ChannelDown),

                _ => null,
            };
        }

        private static readonly HashSet<PressedKey> pressedKeys = new();
        private static readonly SemaphoreSlim keyLock = new(1, 1);

        private async void Form1_KeyDown(object sender, KeyEventArgs e) {
            if (CurrentDevice == null)
                return;
            if (!keyboardControlEnabled)
                return;

            this.Enabled = false;
            await keyLock.WaitAsync();
            try {
                if (ToKey(e.KeyCode) is PressedKey p && !pressedKeys.Contains(p)) {
                    e.Handled = true;
                    pressedKeys.Add(p);
                    await CurrentDevice.Input.KeyDownAsync(p);
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
                Console.Error.WriteLine(ex);
            } finally {
                keyLock.Release();
                this.Enabled = true;
            }
        }

        private async void Form1_KeyUp(object sender, KeyEventArgs e) {
            if (CurrentDevice == null)
                return;
            if (!keyboardControlEnabled)
                return;

            await keyLock.WaitAsync();
            try {
                if (ToKey(e.KeyCode) is PressedKey p && pressedKeys.Contains(p)) {
                    e.Handled = true;
                    pressedKeys.Remove(p);
                    await CurrentDevice.Input.KeyUpAsync(p);
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
                Console.Error.WriteLine(ex);
            } finally {
                keyLock.Release();
            }
        }

        private async void Form1_KeyPress(object sender, KeyPressEventArgs e) {
            if (CurrentDevice == null)
                return;
            if (!keyboardControlEnabled)
                return;

            e.Handled = true;

            if (!char.IsControl(e.KeyChar))
                await CurrentDevice.Input.KeyPressAsync(new PressedKey(e.KeyChar));
        }

        private const string ua = "RokuRemote/1.0 (https://www.github.com/IsaacSchemm/RokuRemote)";

        private static bool IsYouTube(Uri uri) {
            string h = $".{uri.Host}";
            if (h.EndsWith(".youtube.com", StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (h.EndsWith(".youtu.be", StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }

        private async void button1_Click(object sender, EventArgs e) {
            if (CurrentDevice == null)
                return;

            Uri uri = new(textBox1.Text);

            if (IsYouTube(uri)) {
                var req1 = WebRequest.CreateHttp(uri);
                req1.Method = "GET";
                req1.UserAgent = ua;
                req1.Accept = "text/html";
                using var resp1 = await req1.GetResponseAsync();
                using var stream1 = resp1.GetResponseStream();
                using var sr1 = new StreamReader(stream1);
                string html = await sr1.ReadToEndAsync();

                var match = Regex.Match(html, "<meta itemprop=['\"]videoId['\"] content=['\"]([^'\"]+)['\"]");
                if (match.Success) {
                    string id = match.Groups[1].Value;

                    try {
                        var req2 = WebRequest.CreateHttp(new Uri(CurrentDevice.Location, $"/launch/837?contentId={Uri.EscapeDataString(id)}"));
                        req2.Method = "POST";
                        req2.UserAgent = ua;
                        using var resp2 = await req2.GetResponseAsync();
                    } catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound) {
                        var req2 = WebRequest.CreateHttp(new Uri(CurrentDevice.Location, $"/install/837?contentId={Uri.EscapeDataString(id)}"));
                        req2.Method = "POST";
                        req2.UserAgent = ua;
                        using var resp2 = await req2.GetResponseAsync();
                    }

                    return;
                }
            }

            var req = WebRequest.CreateHttp(new Uri(CurrentDevice.Location, $"/input/15985?t=v&u={Uri.EscapeDataString(uri.AbsoluteUri)}&k=(null)"));
            req.Method = "POST";
            req.UserAgent = ua;
            using var resp = await req.GetResponseAsync();
        }

        private async void button14_Click(object sender, EventArgs e) {
            if (CurrentDevice == null)
                return;

            var req = WebRequest.CreateHttp(new Uri(CurrentDevice.Location, $"/search/browse?keyword={Uri.EscapeDataString(textBox2.Text)}"));
            req.Method = "POST";
            req.UserAgent = ua;
            using var resp = await req.GetResponseAsync();
        }
    }
}
