using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Core;

namespace AtomLitePIR.Bluetooth
{
    public class BluetoothWatcher
    {


        /// <summary>
        /// 接続対象PIRサーバー名
        /// </summary>
        public string PIRServer
        {
            set { this.pirServer=value; }
        }

        private string pirServer;

        /// <summary>
        /// 接続PIRサーバー名
        /// </summary>
        public string PIRServerSearched
        {
            get { return this.pirServerSearched; }
        }
        private string pirServerSearched;

        private DeviceWatcher deviceWatcher;
        private ObservableCollection<BluetoothLEDeviceDisplay> KnownDevices = new ObservableCollection<BluetoothLEDeviceDisplay>();
        private CoreDispatcher dispatcher;
        private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();

        /// <summary>
        /// PIRServerで設定したServer名のBluetoothLEDeviceDisplayインスタンスを返す
        /// </summary>
        public BluetoothLEDeviceDisplay DeviceInfoSerchedServer
        {
            get { return this.deviceInfoSerchedServer; }
        }

        private BluetoothLEDeviceDisplay deviceInfoSerchedServer = null;

        public BluetoothWatcher(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.pirServer = null;
        }

        public void StartBleDeviceWatcher()
        {
            try
            {
                // Additional properties we would like about the device.
                // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

                // BT_Code: Example showing paired and non-paired in a single query.
                string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

                deviceWatcher =
                        DeviceInformation.CreateWatcher(
                            aqsAllBluetoothLEDevices,
                            requestedProperties,
                            DeviceInformationKind.AssociationEndpoint);

                // Register event handlers before starting the watcher.
                deviceWatcher.Added += DeviceWatcher_Added;
                deviceWatcher.Updated += DeviceWatcher_Updated;
                deviceWatcher.Removed += DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped += DeviceWatcher_Stopped;

                // Start over with an empty collection.
                KnownDevices.Clear();

                // Start the watcher. Active enumeration is limited to approximately 30 seconds.
                // This limits power usage and reduces interference with other Bluetooth activities.
                // To monitor for the presence of Bluetooth LE devices for an extended period,
                // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
                // sample for an example.
                deviceWatcher.Start();
            }
            catch (Exception err)
            {
                Debug.WriteLine("err");
            }
        }

        /// <summary>
        /// Stops watching for all nearby Bluetooth devices.
        /// </summary>
        public void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                // Unregister the event handlers.
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped -= DeviceWatcher_Stopped;

                // Stop the watcher.
                deviceWatcher.Stop();
                deviceWatcher = null;
            }
        }

        /// <summary>
        /// とりあえず見つかったBluetoothデバイスを順番に列挙していく
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfo"></param>
        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            // We must update the collection on the UI thread because the collection is databound to a UI element.
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    try
                    {
                        Debug.WriteLine(String.Format("Added {0}{1}", deviceInfo.Id, deviceInfo.Name));

                        // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                        if (sender == deviceWatcher)
                        {
                            // Make sure device isn't already present in the list.
                            if (FindBluetoothLEDeviceDisplay(deviceInfo.Id) == null)
                            {
                                if (deviceInfo.Name != string.Empty)
                                {
                                    if (this.pirServer == deviceInfo.Name)
                                    {
                                        this.deviceInfoSerchedServer = new BluetoothLEDeviceDisplay(deviceInfo);
                                        this.pirServerSearched=this.pirServer;
                                    }
                                    Debug.WriteLine("Detect Deveice" + deviceInfo.Id + ":" + deviceInfo.Name);
                                    // If device has a friendly name display it immediately.
                                    //nameのあるDeviceを配列に保存する
                                    KnownDevices.Add(new BluetoothLEDeviceDisplay(deviceInfo));
                                }
                                else
                                {
                                    //nameのないDeviceを配列に保存する
                                    // Add it to a list in case the name gets updated later. 
                                    UnknownDevices.Add(deviceInfo);
                                }
                            }

                        }
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine("Error");
                    }
                }
            });
        }

        /// <summary>
        /// 列挙されたBluetoothデバイスの中で変更があれば呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfoUpdate"></param>
        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // We must update the collection on the UI thread because the collection is databound to a UI element.
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    Debug.WriteLine(String.Format("Updated {0}{1}", deviceInfoUpdate.Id, ""));

                    // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                    if (sender == deviceWatcher)
                    {
                        /*
                        BluetoothLEDeviceDisplay bleDeviceDisplay = FindBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                        if (bleDeviceDisplay != null)
                        {
                            // Device is already being displayed - update UX.
                            bleDeviceDisplay.Update(deviceInfoUpdate);
                            return;
                        }
                        */
                        DeviceInformation deviceInfo = FindUnknownDevices(deviceInfoUpdate.Id);
                        if (deviceInfo != null)
                        {
                            deviceInfo.Update(deviceInfoUpdate);
                            // If device has been updated with a friendly name it's no longer unknown.
                            if (deviceInfo.Name != String.Empty)
                            {
                                KnownDevices.Add(new BluetoothLEDeviceDisplay(deviceInfo));
                                UnknownDevices.Remove(deviceInfo);
                            }
                        }

                    }
                }
            });
        }
        /// <summary>
        /// 列挙されたBluetoothデバイスのなかで削除されたら呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfoUpdate"></param>
        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // We must update the collection on the UI thread because the collection is databound to a UI element.
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    Debug.WriteLine(String.Format("Removed {0}{1}", deviceInfoUpdate.Id, ""));

                    // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                    if (sender == deviceWatcher)
                    {
                        // Find the corresponding DeviceInformation in the collection and remove it.
                        BluetoothLEDeviceDisplay bleDeviceDisplay = FindBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                        if (bleDeviceDisplay != null)
                        {
                            KnownDevices.Remove(bleDeviceDisplay);
                        }

                        DeviceInformation deviceInfo = FindUnknownDevices(deviceInfoUpdate.Id);
                        if (deviceInfo != null)
                        {
                            UnknownDevices.Remove(deviceInfo);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 列挙が一通り終わった段階で呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
        {
            // We must update the collection on the UI thread because the collection is databound to a UI element.
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                }
            });
        }

        private async void DeviceWatcher_Stopped(DeviceWatcher sender, object e)
        {
            // We must update the collection on the UI thread because the collection is databound to a UI element.
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    /*
                    rootPage.NotifyUser($"No longer watching for devices.",
                            sender.Status == DeviceWatcherStatus.Aborted ? NotifyType.ErrorMessage : NotifyType.StatusMessage);
                    */
                }
            });
        }


        /// <summary>
        /// KnownDevices配列（NameがあるBluetoothデバイス）の中から指定されたIDのDeviceInformationを取り出す
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private BluetoothLEDeviceDisplay FindBluetoothLEDeviceDisplay(string id)
        {
            foreach (BluetoothLEDeviceDisplay bleDeviceDisplay in KnownDevices)
            {
                if (bleDeviceDisplay.Id == id)
                {
                    return bleDeviceDisplay;
                }
            }
            return null;
        }

        /// UnknownDevices配列（NameがなかったBluetoothデバイス）の中から指定されたIDのDeviceInformationを取り出す
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private DeviceInformation FindUnknownDevices(string id)
        {
            foreach (DeviceInformation bleDeviceInfo in UnknownDevices)
            {
                if (bleDeviceInfo.Id == id)
                {
                    return bleDeviceInfo;
                }
            }
            return null;
        }
    }
}
