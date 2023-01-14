using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLitePIR.Bluetooth
{
    public class BluetoothConnector
    {
        /*
        private async Task<List<BluetoothService>> Connect()
        {
            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                this.bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(this.deviceInfoSerchedServer.Id);
                if (bluetoothLeDevice == null)
                {
                    Debug.WriteLine("Failed to connect to device.");
                }
            }
            catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
            {
                Debug.WriteLine("Bluetooth radio is not on.");
            }

            if (bluetoothLeDevice != null)
            {
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    foreach (var service in services)
                    {
                        this.services.Add(new BluetoothService()
                        {
                            Service = service,
                            ServiceGattNativeServiceUuid = GetGattNativeServiceUuid(service),
                            ServiceGattNativeServiceUuidString = GetServiceName(service)
                        });
                    }
                }
                else
                {
                    Debug.WriteLine("Device unreachable");
                }
            }
            return this.services;
        }
        */

    }
}
