using AtomLiteBleDesktop.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Core;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothWatcher
    {
        /// <summary>
        /// 登録されたサーバが見つかる間、何回検索を繰り返すか
        /// </summary>
        private const int NUM_RETRY_FIND_SERVER = 100;

        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        /// <summary>
        /// 見つかったデバイスを格納する配列
        /// </summary>
        public ObservableCollection<BluetoothLEDevice> KnownDevices
        {
            get { return this.knownDevices; }
        }
        //見つかったデバイスを格納する配列
        private ObservableCollection<BluetoothLEDevice> knownDevices = new ObservableCollection<BluetoothLEDevice>();

        //UIスレッドにアクセスするためのDispatcher
        //private static CoreDispatcher _mDispatcher;
        private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();

        /// <summary>
        /// PIRServerで設定したServer名のBluetoothLEDeviceDisplayインスタンスを返す
        /// </summary>
        public BluetoothLEDevice DeviceInfoSerchedServer
        {
            get { return this.deviceInfoSerchedServer; }
        }
        private BluetoothLEDevice deviceInfoSerchedServer = null;

        /// <summary>
        /// このクラスのインスタンスを作成
        /// </summary>
        private static BluetoothWatcher instance = new BluetoothWatcher();

        /// <summary>
        /// コンストラクタ(singleton用のためprivete)
        /// </summary>
        private BluetoothWatcher()
        {
        }
        /// <summary>
        /// このクラスのインスタンスを取得します
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public static BluetoothWatcher GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// 指定したサーバ名を探し、存在すればDeviceを返却
        /// 存在しなければ、見つからなかったという情報のDeviceを返却
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public BluetoothLEDevice FindServerDevice(string server)
        {
            BluetoothLEDevice output = null;
            this.startBleDeviceWatcher();
            //一定回数検索をくりかえし、接続Server名が取得できなければnullを返す
            int counter = NUM_RETRY_FIND_SERVER;
            while (counter>0)
            {
                foreach (var device in this.knownDevices)
                {//TODO:この関数がnullを返すため、二つあるデバイスの内一つしか認識しない→knownDevicesに近隣のデバイスが乗ってこない内にここにくるためnullが返る
                    if (device.Name == server)
                    {
                        output = device;
                        break;
                    }
                }
                if (output!=null && output.Status== BluetoothLEDevice.TypeStatus.Finded)
                {
#if DEBUG
                    logger.Info( "Finded " + server);
#endif
                    break;
                }
                Thread.Sleep(10);
                counter--;
            }
#if DEBUG
            logger.Info("Find Server retried " + (counter * 10).ToString() + "ms");
            if (output == null)
            {
                logger.Info("Cant find " + server);
            }
#endif
            return output;
        }
        /*
        public BluetoothLEDevice FindServerDevice(string server)
        {
            BluetoothLEDevice output = new BluetoothLEDevice(server);
            this.startBleDeviceWatcher();
            //1s待って。接続Server名が取得できなければfalseを返す
            int counter = 10;
            while (counter > 0)
            {
                foreach (var device in this.knownDevices)
                {
                    if (device.Name == server)
                    {
                        output = device;
                        break;
                    }
                }
                if (output.Status == BluetoothLEDevice.TypeStatus.Finded)
                {
#if DEBUG
                    logger.Info("Finded " + server);
#endif
                    break;
                }
                Thread.Sleep(10);
                counter--;
            }
            return output;
        }
        */
        private void startBleDeviceWatcher()
        {
            try
            {
                // Additional properties we would like about the device.
                // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
                //下記下側のrequestedPropertiesを選択しないとDeviceInformation.Properties["System.Devices.Aep.IsConnected"] 等で例外スローとなる
                //string[] requestedProperties = { "System.Devices.Aep.DeviceAddress"};
                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

                // BT_Code: Example showing paired and non-paired in a single query.
                string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

                deviceWatcher =
                        DeviceInformation.CreateWatcher(
                            aqsAllBluetoothLEDevices,
                            requestedProperties,
                            DeviceInformationKind.AssociationEndpoint
                            //DeviceInformationKind.DeviceContainer
                            );

                // Register event handlers before starting the watcher.
                deviceWatcher.Added += deviceWatcher_Added;
                deviceWatcher.Updated += deviceWatcher_Updated;
                deviceWatcher.Removed += deviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted += deviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped += deviceWatcher_Stopped;

                // Start over with an empty collection.
                knownDevices.Clear();

                // Start the watcher. Active enumeration is limited to approximately 30 seconds.
                // This limits power usage and reduces interference with other Bluetooth activities.
                // To monitor for the presence of Bluetooth LE devices for an extended period,
                // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
                // sample for an example.
                deviceWatcher.Start();
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }

        /// <summary>
        /// Stops watching for all nearby Bluetooth devices.
        /// </summary>
        public void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                //this.isScanning = false;
                // Unregister the event handlers.
                deviceWatcher.Added -= deviceWatcher_Added;
                deviceWatcher.Updated -= deviceWatcher_Updated;
                deviceWatcher.Removed -= deviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= deviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped -= deviceWatcher_Stopped;

                // Stop the watcher.
                deviceWatcher.Stop();
                deviceWatcher = null;
            }
        }
        private static BluetoothLEDevice getInstanceBluetoothLEDevice(DeviceInformation deviceInfo)
        {
            var type = BleContext.GetServerType(deviceInfo.Name);
            BluetoothLEDevice findDevice;
            if (type == BleContext.ServerType.AtomLite)
            {
                findDevice = new BluetoothLEDeviceATOMLite(deviceInfo);

            }
            else if (type == BleContext.ServerType.M5Stack)
            {
                findDevice = new BluetoothLEDeviceM5Stack(deviceInfo);

            }
            else
            {
                findDevice = new BluetoothLEDeviceUnregistered(deviceInfo);

            }

            return findDevice;
        }

        /// <summary>
        /// とりあえず見つかったBluetoothデバイスを順番に列挙していく
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfo"></param>
        private void deviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
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
                        if (findBluetoothLEDeviceDisplay(deviceInfo.Id) == null)
                        {
                            if (deviceInfo.Name != string.Empty)
                            {
                                BluetoothLEDevice findDevice = getInstanceBluetoothLEDevice(deviceInfo);
                                if (this.pirServer == deviceInfo.Name)
                                {
                                    //接続したBleデバイス情報で接続用のデバイスを作成する
                                    this.deviceInfoSerchedServer = findDevice;
                                    this.pirServerSearched = this.pirServer;
                                }
                                else
                                {
                                    this.pirServerSearched = null;
                                }
                                Debug.WriteLine("Detect Deveice" + deviceInfo.Id + ":" + deviceInfo.Name);
                                // If device has a friendly name display it immediately.
                                //nameのあるDeviceを配列に保存する
                                knownDevices.Add(findDevice);
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
                    Debug.WriteLine(err.Message);
                }
            }
        }

        /// <summary>
        /// 列挙されたBluetoothデバイスの中で変更があれば呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfoUpdate"></param>
        private void deviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            lock (this)
            {
                Debug.WriteLine(String.Format("Updated {0}{1}", deviceInfoUpdate.Id, ""));

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    DeviceInformation deviceInfo = findUnknownDevices(deviceInfoUpdate.Id);
                    if (deviceInfo != null)
                    {
                        deviceInfo.Update(deviceInfoUpdate);
                        // If device has been updated with a friendly name it's no longer unknown.
                        if (deviceInfo.Name != String.Empty)
                        {
                            knownDevices.Add(getInstanceBluetoothLEDevice(deviceInfo));
                            UnknownDevices.Remove(deviceInfo);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// 列挙されたBluetoothデバイスのなかで削除されたら呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfoUpdate"></param>
        private void deviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            lock (this)
            {
                Debug.WriteLine(String.Format("Removed {0}{1}", deviceInfoUpdate.Id, ""));

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    // Find the corresponding DeviceInformation in the collection and remove it.
                    BluetoothLEDevice bleDeviceDisplay = findBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                    if (bleDeviceDisplay != null)
                    {
                        knownDevices.Remove(bleDeviceDisplay);
                    }

                    DeviceInformation deviceInfo = findUnknownDevices(deviceInfoUpdate.Id);
                    if (deviceInfo != null)
                    {
                        UnknownDevices.Remove(deviceInfo);
                    }
                }
            }
        }

        /// <summary>
        /// 列挙が一通り終わった段階で呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
        {

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
            }
        }

        private void deviceWatcher_Stopped(DeviceWatcher sender, object e)
        {
            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {

            }
        }


        /// <summary>
        /// KnownDevices配列（NameがあるBluetoothデバイス）の中から指定されたIDのDeviceInformationを取り出す
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private BluetoothLEDevice findBluetoothLEDeviceDisplay(string id)
        {
            foreach (BluetoothLEDevice bleDeviceDisplay in knownDevices)
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
        private DeviceInformation findUnknownDevices(string id)
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
