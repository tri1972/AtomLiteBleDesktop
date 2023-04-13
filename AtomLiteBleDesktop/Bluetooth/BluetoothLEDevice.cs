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
        }

        /// <summary>
        /// Connect時の最大リトライ回数
        /// </summary>
        private const int MAX_RETRY_CONNECT = 5;

        private BluetoothConnector bluetoothConnector;
        /// <summary>
        /// BluetoothConnectorインスタンス
        /// </summary>
        public BluetoothConnector BluetoothConnector
        {
            get { return this.bluetoothConnector; }
        }
        /*
        /// <summary>
        /// Characteristicの名前を取得します
        /// </summary>
        public List<string> CharacteristicNames
        {
            get { return this.bluetoothConnector.CharacteristicNames; }
        }
        */
        /*
        private GattCharacteristic registeredCharacteristic;
        */
        public DeviceInformation DeviceInformation { get; private set; }

        private bool isFindDevice;
        public bool IsFindDevice
        {
            get { return this.isFindDevice; }
        }

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
        public bool IsPaired
        {
            get { return this.isPaired; }
        }
        private bool isConnected;
        public bool IsConnected
        {
            get { return this.isConnected; }
        }
        private bool isConnectable;
        public bool IsConnectable
        {
            get { return this.isConnectable; }
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

        private IReadOnlyDictionary<string, object> properties;
        public IReadOnlyDictionary<string, object> Properties
        {
            get { return this.properties; }
        }

        /// <summary>
        /// コンストラクタ
        /// Deviceが見つかった場合
        /// </summary>
        /// <param name="deviceInfoIn"></param>
        public BluetoothLEDevice(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
            this.id = DeviceInformation.Id;
            this.name= DeviceInformation.Name;
            this.isFindDevice = true;
            this.isPaired = DeviceInformation.Pairing.IsPaired;
            try
            {
                if ((bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true)
                {
                    this.isConnected = true;
                }
                else
                {
                    this.isConnected = false;
                }
                if ((bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true)
                {
                    this.isConnectable = true;

                }
                else
                {
                    this.isConnectable = false;
                }
            }catch(Exception err)
            {
                Debug.WriteLine(err.Message);
            }

                this.properties = DeviceInformation.Properties;
        }

        /// <summary>
        /// コンストラクタ
        /// Deviceが見つからなかった場合
        /// </summary>
        public BluetoothLEDevice(string name)
        {
            this.id = null;
            this.name = name;
            this.isFindDevice = false;
            this.isPaired = false;
            this.isConnected = false;
            this.isConnectable = false;
            this.properties = null;
        }

        public async void Connect()
        {
            int counter = 0;
            await Task.Run(async () =>
            {
                this.bluetoothConnector = new BluetoothConnector(this);

                //this.bluetoothConnector.NotifyReceiveCharacteristic += this.registeredCharacteristicNotify;

                counter = MAX_RETRY_CONNECT;
                while (counter > 0)
                {
                    var task = await Task.Run(this.bluetoothConnector.Connect);
                    if (task)
                    {
                        this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connected);


                        foreach(var server in this.bluetoothConnector.Services)
                        {
                            server.NotifyReceiveCharacteristic += NotifyReceiveServerCharacteristic;
                        }
                        /*
                        this.registeredCharacteristic = this.bluetoothConnector.RegisteredCharacteristic;

                        //Notify受信イベントハンドラの登録とデバイスから ValueChanged イベントを受信できるようにします。
                        if (this.registeredCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            await this.registeredCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        }
                        */
                        break;
                    }
                    else
                    {
                        this.OnNotifyConnectingServer("Connecting...", NotifyBluetoothAccesserEventArgs.Status.Connecting);
                    }
                    counter--;
                }
                if (counter == 0)
                {
                    this.OnNotifyConnectingServer("Server Connecting Aborted", NotifyBluetoothAccesserEventArgs.Status.Abort);
                }

            });
        }

        /// <summary>
        /// 指定したデータを送信する
        /// </summary>
        /// <param name="serviceUUID"></param>
        /// <param name="characteristicUUID"></param>
        public void SendData(string serviceUUID, string characteristicUUID, BluetoothCharacteristic.TypeStateWaitingSend sendData)
        {
            foreach (var server in this.bluetoothConnector.Services)
            {
                if (server.Service.Uuid == new Guid(serviceUUID))
                {
                    foreach (var characteristic in server.Characteristics)
                    {
                        if (characteristic.Characteristic.Uuid == new Guid(characteristicUUID))
                        {
                            characteristic.WriteCharacterCharacteristic(sendData);
                        }
                    }
                    break;
                }
            }
        }

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

        /*
        /// <summary>
        /// Characteristic受信時イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void registeredCharacteristicNotify(object sender, NotifyReceiveCharacteristicEventArgs e)
        {
            OnNotifyReceiveCharacteristic(e);
        }
        */
        //public event PropertyChangedEventHandler PropertyChanged=null;
    }
}
