using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using AtomLiteBleDesktop.Bluetooth;
using static AtomLiteBleDesktop.Bluetooth.BluetoothAccesser;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static AtomLiteBleDesktop.BluetoothService;
using Windows.Devices.Bluetooth;
using static AtomLiteBleDesktop.Bluetooth.BluetoothUuidDefine;

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// BluetoothLe(BLE)のデバイス情報を格納する
    /// </summary>
    public class BluetoothLEDevice //: INotifyPropertyChanged
    {

        //NotifyReceiveCharacteristicイベントで返されるデータ
        //ここではstring型のひとつのデータのみ返すものとする
        public class NotifyReceiveLEDeviceCharacteristicEventArgs : EventArgs
        {
            public BluetoothService Service;
            public BluetoothCharacteristic Characteristic;
            public string Message;
            public BluetoothCharacteristic.TypeStateReseive State;
        }

        /// <summary>
        /// このデバイスの現状のステータスを示すタイプ
        /// </summary>
        public enum TypeStatus
        {
            Disconnect,
            Coonected,
            Connecting,
            Abort,
            Sending,
            Finded,
            NoFinded,
            None
        }

        #region Error Codes
        //readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        //readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        //readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion


        /// <summary>
        /// Connect時の最大リトライ回数
        /// </summary>
        private const int MAX_RETRY_CONNECT = 5;

        private List<BluetoothService> services;
        /// <summary>
        /// servicesを取得します
        /// </summary>
        public List<BluetoothService> Services
        {
            get { return this.services; }
        }

        public DeviceInformation DeviceInformation { get; private set; }

        private string id;
        public string Id
        {
            get { return this.id; }
        }
        private string name;
        public string Name
        {
            set { this.name = value; }
            get { return this.name; }
        }
        private bool isPaired;
        /// <summary>
        /// 現状のペアリングの有無を取得します
        /// </summary>
        public bool IsPaired
        {
            get { return this.isPaired; }
        }

        private TypeStatus status;
        /// <summary>
        /// デバイスの現状の接続状態を取得します
        /// </summary>
        public TypeStatus Status
        {
            set { this.status = value; }
            get { return this.status; }
        }

        /// <summary>
        /// BluetoothLEDeviceのeventハンドラの関数の引数と戻り値を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void NotifyBluetoothLEDeviceEventHandler(object sender, NotifyReceiveLEDeviceCharacteristicEventArgs e);

        private event NotifyBluetoothLEDeviceEventHandler notifyReceiveCharacteristic;
        /// <summary>
        /// BleServerからのNotify受信イベント
        /// </summary>
        public event NotifyBluetoothLEDeviceEventHandler NotifyReceiveCharacteristic
        {
            add
            {
                if (this.notifyReceiveCharacteristic == null)
                {
                    this.notifyReceiveCharacteristic += value;
                }
                else
                {
                    Debug.WriteLine("重複登録ですよ");
                }
            }
            remove
            {
                this.notifyReceiveCharacteristic -= value;
            }
        }

        private event NotifyBluetoothAccesserEventHandler notifyConnectingServer;

        /// <summary>
        /// ServerConnectイベント
        /// </summary>
        public event NotifyBluetoothAccesserEventHandler NotifyConnectingServer
        {
            add
            {
                if (this.notifyConnectingServer == null)
                {
                    this.notifyConnectingServer += value;
                }
                else
                {
                    Debug.WriteLine("重複登録ですよ");
                }
            }
            remove
            {
                this.notifyConnectingServer -= value;
            }
        }

        private Windows.Devices.Bluetooth.BluetoothLEDevice bluetoothLeDevice = null;

        /// <summary>
        /// コンストラクタ
        /// Deviceが見つかった場合
        /// </summary>
        /// <param name="deviceInfoIn"></param>
        public BluetoothLEDevice(DeviceInformation deviceInfoIn)
        {
            this.status = TypeStatus.Disconnect;
            DeviceInformation = deviceInfoIn;
            this.id = DeviceInformation.Id;
            this.name = DeviceInformation.Name;
            this.status = TypeStatus.Finded;
            this.isPaired = DeviceInformation.Pairing.IsPaired;
            this.services = new List<BluetoothService>();
            //setStatusConnection();
        }
        
        /// <summary>
        /// コンストラクタ
        /// Deviceが見つからなかった場合
        /// </summary>
        public BluetoothLEDevice(string name)
        {
            this.id = null;
            this.name = name;
            this.status = TypeStatus.NoFinded;
            this.isPaired = false;
        }

        public async void Connect()
        {
            int counter = 0;
            await Task.Run(async () =>
            {

                counter = MAX_RETRY_CONNECT;
                while (counter > 0)
                {
                    var task = await Task.Run(ConnectServer);
                    if (task)
                    {
                        this.status = TypeStatus.Coonected;
                        this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connected);

                        foreach(var server in this.services)
                        {
                            server.NotifyReceiveCharacteristic += NotifyReceiveServerCharacteristic;
                        }
                        break;
                    }
                    else
                    {
                        this.status = TypeStatus.Connecting;
                        this.OnNotifyConnectingServer("Connecting...", NotifyBluetoothAccesserEventArgs.Status.Connecting);
                    }
                    counter--;
                }
                if (counter == 0)
                {
                    this.status = TypeStatus.Abort;
                    this.OnNotifyConnectingServer("Server Connecting Aborted", NotifyBluetoothAccesserEventArgs.Status.Abort);
                }
                //setStatusConnection();

            });
        }

        /// <summary>
        /// 指定したデータを送信する
        /// </summary>
        /// <param name="serviceUUID"></param>
        /// <param name="characteristicUUID"></param>
        public void SendData(string serviceUUID, string characteristicUUID, BluetoothCharacteristic.TypeStateWaitingSend sendData)
        {
            var beforeStatus = this.status;
            this.status = TypeStatus.Sending;
            foreach (var server in this.services)
            {
                if (server.Service.Uuid == new Guid(serviceUUID))
                {
                    foreach (var characteristic in server.Characteristics)
                    {
                        if (characteristic.Characteristic.Uuid == new Guid(characteristicUUID))
                        {
                            characteristic.WriteCharacterCharacteristic(sendData);
                            this.status = beforeStatus;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Characteristic受信のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NotifyReceiveServerCharacteristic(object sender, NotifyReceiveServerCharacteristicEventArgs e)
        {
            if (sender is BluetoothService)
            {
                OnNotifyReceiveCharacteristic(sender as BluetoothService, e);
            }
        }

        /// <summary>
        /// Characteristicにてデータを受信した場合のイベントキック用関数
        /// </summary>
        /// <param name="e"></param>
        public void OnNotifyReceiveCharacteristic(BluetoothService sender, NotifyReceiveServerCharacteristicEventArgs e)
        {
            if (this.notifyReceiveCharacteristic != null)
            {
                var data = new NotifyReceiveLEDeviceCharacteristicEventArgs();
                data.Message = e.Message;
                data.State = e.State;
                data.Characteristic = e.Characteristic;
                data.Service = sender;

                this.notifyReceiveCharacteristic(this, data);
            }
        }

        /// <summary>
        /// ServerConnect時イベントキック用関数
        /// </summary>
        /// <param name="e"></param>
        public void OnNotifyConnectingServer(string message, NotifyBluetoothAccesserEventArgs.Status state)
        {
            if (this.notifyConnectingServer != null)
            {
                var e = new NotifyBluetoothAccesserEventArgs();
                e.Message = message;
                e.State = state;
                this.notifyConnectingServer(this, e);
            }
        }
        /// <summary>
        /// 非同期BLE接続処理を実行します
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectServer()
        {
            Task<GattCharacteristic> task = null;
            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                this.bluetoothLeDevice = await Windows.Devices.Bluetooth.BluetoothLEDevice.FromIdAsync(this.Id);
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
                bluetoothLeDevice.ConnectionStatusChanged += eventConnectionStatusChanged;
                bluetoothLeDevice.GattServicesChanged += eventGattServicesChanged;
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.

                var tasksub = Task.Run(async () =>
                {

                    GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        var services = result.Services;
                        foreach (var service in services)
                        {
                            this.services.Add(new BluetoothService()
                            {
                                Service = service,
                                ServiceGattNativeServiceUuid = BluetoothHelper.GetGattNativeServiceUuid(service),
                                ServiceGattNativeServiceUuidString = BluetoothHelper.GetServiceName(service)
                            });
                        }
                        task = this.getUserCustomService(this.services).GetRegisteredCharacteristic();
                        //this.registeredCharacteristic = task.Result;
                        //this.registeredCharacteristic.ValueChanged += this.registeredCharacteristicNotify;
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("Device unreachable");
                        return false;
                    }
                });
                if (tasksub.Result)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            //task= this.getRegisteredCharacteristic(this.getUserCustomService(this.services));
            //this.registeredCharacteristic = task.Result;
        }

        /// <summary>
        /// Bleサーバー接続状況変化時イベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        private void eventConnectionStatusChanged(Windows.Devices.Bluetooth.BluetoothLEDevice e, object sender)
        {
#warning この関数はBluetoothLEDeviceクラスへ移動すべき
            if (e.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {//初回Connect関数実行時にConnectできなければthis.services.Count=0のため再接続動作ができない
                //→Characteristicクラスのコンストラクタにてnotifyイベントを受けるようにする
                if (this.services != null && this.services.Count > 0)
                {//初回接続時のイベントハンドラによるリクエストについては処理をしないようにする

                    foreach (var service in services)
                    {
                        foreach (var characteristic in service.Characteristics)
                        {//切断された後、再度接続した際はnotifyによるValueChanged イベントを再度受信できるようにする
                            characteristic.CanNotifyCharacteristic();
                        }
                    }
                }
                this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connected);
                this.Status = TypeStatus.Coonected;
            }
            else
            {
                this.Status = BluetoothLEDevice.TypeStatus.Disconnect;
                this.OnNotifyConnectingServer("ServerStatusChange", NotifyBluetoothAccesserEventArgs.Status.Disconnected);
                this.Status = TypeStatus.Disconnect;
            }
            //disconnectになった場合、ここで再度イベントハンドラの登録を行うようにしてみる
        }

        /// <summary>
        /// Bleサーバーサービス変更時イベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        private void eventGattServicesChanged(Windows.Devices.Bluetooth.BluetoothLEDevice e, object sender)
        {
            ;
        }
        /// <summary>
        /// 取得したserviceの中からUserCustomServiceを取得する
        /// </summary>
        /// <param name="services"></param>
        private BluetoothService getUserCustomService(List<BluetoothService> services)
        {
            List<string> gattNativeServiceUuidName = new List<string>(); ;
            foreach (var service in services)
            {
                gattNativeServiceUuidName.Add(string.Copy(service.ServiceGattNativeServiceUuidString));
                if (service.ServiceGattNativeServiceUuid == GattNativeServiceUuid.None)
                {
                    return service;
                }
            }
            return null;
        }
    }
}
