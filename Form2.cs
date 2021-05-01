using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RokuRemote {
    public partial class Form2 : Form {
        private readonly HashSet<string> _seen = new();
        private readonly SemaphoreSlim _seenLock = new(1, 1);

        public event Action<Uri> PlayRequested;

        public Form2() {
            InitializeComponent();
        }

        public void Navigate(Uri uri) {
            webView21.Source = uri;
        }

        private void webView21_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e) {
            textBox1.Text = webView21.Source.AbsoluteUri;
        }

        private void webView21_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
            webView21.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            webView21.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }

        private enum MediaType {
            Unknown,
            Audio,
            Video,
            HLS,
            DASH,
            MSS
        }

        private static MediaType GetMediaType(string contentType) {
            foreach ((string prefix, MediaType result) in new[] {
                ("application/vnd.apple.mpegurl", MediaType.HLS),
                ("audio/mpegurl", MediaType.HLS),
                ("application/vnd.ms-sstr+xml", MediaType.MSS),
                ("application/dash+xml", MediaType.DASH),
                ("audio/", MediaType.Audio),
                ("video/", MediaType.Video),
            }) {
                if (contentType != null && contentType.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                    return result;
                }
            }

            return MediaType.Unknown;
        }

        private static bool IsMedia(string contentType) {
            return GetMediaType(contentType) switch {
                MediaType.Audio or MediaType.Video or MediaType.HLS or MediaType.DASH or MediaType.MSS => true,
                _ => false,
            };
        }

        private static async Task<bool> IsAdaptiveStreamAsync(string url, string contentType) {
            switch (GetMediaType(contentType)) {
                case MediaType.DASH:
                case MediaType.MSS:
                    return true;
                case MediaType.HLS:
                    var req = WebRequest.CreateHttp(url);
                    System.Diagnostics.Debug.WriteLine(url);
                    req.Method = "GET";
                    using (var resp = await req.GetResponseAsync())
                    using (var stream = resp.GetResponseStream())
                    using (var sr = new StreamReader(stream)) {
                        string line;
                        while ((line = await sr.ReadLineAsync()) != null) {
                            System.Diagnostics.Debug.WriteLine(line);
                            if (line.StartsWith("#EXT-X-STREAM-INF:")) {
                                return true;
                            }
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e) {
            foreach (var pair in e.Response.Headers) {
                if (pair.Key.ToLowerInvariant() == "content-type") {
                    string mediaUri = e.Request.Uri;

                    await _seenLock.WaitAsync();
                    bool added = _seen.Add(mediaUri);
                    _seenLock.Release();

                    if (added) {
                        string contentType;
                        try {
                            var req1 = WebRequest.CreateHttp(mediaUri);
                            req1.Method = "HEAD";
                            using var resp1 = await req1.GetResponseAsync();
                            contentType = resp1.ContentType;
                        } catch (WebException) {
                            contentType = null;
                        }

                        if (IsMedia(contentType)) {
                            var panel = new Panel {
                                Dock = DockStyle.Top,
                                Height = 23
                            };
                            var textBox = new TextBox {
                                Dock = DockStyle.Fill,
                                Text = mediaUri
                            };
                            var button = new Button {
                                Dock = DockStyle.Right,
                                Text = "Play"
                            };
                            button.Click += (sender, e) => {
                                PlayRequested?.Invoke(new Uri(mediaUri));
                            };
                            panel.Controls.Add(textBox);
                            panel.Controls.Add(button);

                            if (await IsAdaptiveStreamAsync(mediaUri, contentType)) {
                                panel1.Controls.Add(panel);
                            } else {
                                panel2.Controls.Add(panel);
                            }
                        }
                    }
                }
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e) {
            e.Handled = true;
            webView21.Source = new Uri(e.Uri);
        }
    }
}
