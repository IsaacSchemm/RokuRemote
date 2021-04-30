using RokuDotNet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RokuRemote {
    public class ManualRokuDeviceDiscoveryClient : IRokuDeviceDiscoveryClient {
        private readonly IReadOnlyList<string> _ipAddresses;

        public ManualRokuDeviceDiscoveryClient(IEnumerable<string> ipAddresses) {
            if (ipAddresses == null)
                throw new ArgumentNullException(nameof(ipAddresses));

            _ipAddresses = ipAddresses.ToList();
        }

        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;

        private IEnumerable<DiscoveredDeviceContext> DiscoverDevices() {
            foreach (string ipAddress in _ipAddresses) {
                var device = new HttpRokuDevice(ipAddress, new Uri($"http://{ipAddress}"));
                yield return new HttpDiscoveredDeviceContext(device, ipAddress);
            }
        }

        public Task DiscoverDevicesAsync(CancellationToken cancellationToken = default) {
            foreach (var context in DiscoverDevices()) {
                var e = new DeviceDiscoveredEventArgs(context);
                DeviceDiscovered?.Invoke(this, e);
                if (e.CancelDiscovery)
                    break;
            }
            return Task.CompletedTask;
        }

        public async Task DiscoverDevicesAsync(Func<DiscoveredDeviceContext, Task<bool>> onDeviceDiscovered, CancellationToken cancellationToken = default) {
            foreach (var context in DiscoverDevices()) {
                if (await onDeviceDiscovered(context))
                    break;
            }
        }
    }
}
