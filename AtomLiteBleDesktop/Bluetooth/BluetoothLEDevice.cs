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
using Windows.Storage;
using Windows.UI.Core;

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// BluetoothLe(BLE)のデバイス情報を格納する
    /// </summary>
    public abstract class BluetoothLEDevice //: INotifyPropertyChanged
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

        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Error Codes
        //readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        //readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        //readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        /// <summary>
        /// Connect時の最大リトライ回数
        /// </summary>
        private const int MAX_RETRY_CONNECT = 20;

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

        /// <summary>
        /// デバイスの現状の接続状態を取得します
        /// </summary>
        abstract public TypeStatus Status { get; set; }

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
            this.Status = TypeStatus.Disconnect;
            DeviceInformation = deviceInfoIn;
            this.id = DeviceInformation.Id;
            this.name = DeviceInformation.Name;
            this.Status = TypeStatus.Finded;
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
            this.Status = TypeStatus.NoFinded;
            this.isPaired = false;
        }

        public async void Connect(CoreDispatcher dispatcher)
        {
            int counter = 0;
            await Task.Run(async () =>
            {

                logger.Info("Connecting Start :"+this.name);
                counter = MAX_RETRY_CONNECT;
                while (counter > 0)
                {
                    var task = await Task.Run(()=>ConnectServer(dispatcher));
                    if (task)
                    {//デバイスが電源オフだったとしても、キャッシュでデバイスが見つかればTrueとなる→実際に接続できたかどうかの証明にはならない
                        this.Status = TypeStatus.Coonected;
                        this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connecting);//最初の接続時については接続とせずに接続中とする（接続状態のイベントハンドラで接続を検知するようにする）

                        foreach(var server in this.services)
                        {
                            server.NotifyReceiveCharacteristic += NotifyReceiveServerCharacteristic;
                        }
#if DEBUG
                        logger.Info("Connect Server" + this.name);
#else
#endif
                        break;
                    }
                    else
                    {
                        this.Status = TypeStatus.Connecting;
                        this.OnNotifyConnectingServer("Connecting...", NotifyBluetoothAccesserEventArgs.Status.Connecting);
                    }
                    counter--;
                }
#if DEBUG
                if(counter != MAX_RETRY_CONNECT)
                {
                    logger.Info("Number connecting retry "+(MAX_RETRY_CONNECT-counter).ToString()+" Server :" + this.name);
                }
#endif
                if (counter == 0)
                {
                    this.Status = TypeStatus.Abort;
                    this.OnNotifyConnectingServer("Server Connecting Aborted", NotifyBluetoothAccesserEventArgs.Status.Abort);
#if DEBUG
                    logger.Info("Connecting Aborted"+ this.name);
#else
#endif
                }

            });
            logger.Info("End of Async Connect");
        }

        public BluetoothCharacteristic.TypeStateWaitingSend ToTypeStateWaitingSend(string strType)
        {
            BluetoothCharacteristic.TypeStateWaitingSend output;

            if (strType.Equals(BluetoothCharacteristic.TypeStateWaitingSend.ASAP.ToString()))
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.ASAP;
            }
            else if (strType.Equals(BluetoothCharacteristic.TypeStateWaitingSend.Cancel.ToString()))
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.Cancel;
            }
            else if (strType.Equals(BluetoothCharacteristic.TypeStateWaitingSend.None.ToString()))
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.None;
            }
            else if (strType.Equals(BluetoothCharacteristic.TypeStateWaitingSend.WAIT.ToString()))
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.WAIT;
            }
            else if (strType.Equals(BluetoothCharacteristic.TypeStateWaitingSend.WRONG.ToString()))
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.WRONG;
            }
            else if (strType.Equals(BluetoothCharacteristic.TypeStateWaitingSend.Clear.ToString()))
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.Clear;
            }
            else
            {
                output = BluetoothCharacteristic.TypeStateWaitingSend.None;
            }
            return output;
        }


        /// <summary>
        /// 指定したデータを送信する
        /// </summary>
        /// <param name="serviceUUID"></param>
        /// <param name="characteristicUUID"></param>
        abstract public void SendData(string serviceUUID, string characteristicUUID, string sendData);


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
        public async Task<bool> ConnectServer(CoreDispatcher dispatcher)
        {
            //Task<GattCharacteristic> task = null;
            try
            {
                logger.Info("Getting Server device:" + this.name);

                TaskCompletionSource<Windows.Devices.Bluetooth.BluetoothLEDevice> taskCompletionSource =
                    new TaskCompletionSource<Windows.Devices.Bluetooth.BluetoothLEDevice>();

                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
#warning このメソッドについてはUIスレッドで実行する必要があり
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
                {
                    taskCompletionSource.SetResult(await Windows.Devices.Bluetooth.BluetoothLEDevice.FromIdAsync(this.Id));
                    //this.bluetoothLeDevice = await Windows.Devices.Bluetooth.BluetoothLEDevice.FromIdAsync(this.Id);
                });

                bluetoothLeDevice = taskCompletionSource.Task.Result;

                if (bluetoothLeDevice == null)
                {
#if DEBUG
                    logger.Info("Can not get device of ConnectServer:" + this.name);
#endif
                    return false;
                }
                else
                {

#if DEBUG
                    logger.Info("Get device of ConnectServer!!:" + this.name);
#endif
                    bluetoothLeDevice.ConnectionStatusChanged += eventConnectionStatusChanged;
                    bluetoothLeDevice.GattServicesChanged += eventGattServicesChanged;
                    // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                    // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                    // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.

                    var tasksub = await Task.Run(async () =>
                    {
                        GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
                        if (result.Status == GattCommunicationStatus.Success)
                        {
#if DEBUG
                            logger.Info("Success geting GattServices:" + this.name);
#endif
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
                            var task = await this.getUserCustomService(this.services).GetRegisteredCharacteristic();
                            //this.registeredCharacteristic = task.Result;
                            //this.registeredCharacteristic.ValueChanged += this.registeredCharacteristicNotify;
                            if (task != null)
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
                            Debug.WriteLine("Device unreachable");
                            return false;
                        }
                    });
                    if (tasksub)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
            {
#if DEBUG
                Debug.WriteLine("Bluetooth radio is not on.");
                logger.Error("Bluetooth radio is not on.:" + this.Id.ToString());
#endif
                return false;
            }
            logger.Info("ConnectServer End:" + this.name);
        }

        /// <summary>
        /// Bleサーバー接続状況変化時イベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        private async void eventConnectionStatusChanged(Windows.Devices.Bluetooth.BluetoothLEDevice e, object sender)
        {
#if DEBUG
            logger.Info("ConnectionStatusChanged is "+ e.ConnectionStatus.ToString()+" : "+e.Name);
#endif
#warning この関数はBluetoothLEDeviceクラスへ移動すべき
            if (e.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {//初回Connect関数実行時にConnectできなければthis.services.Count=0のため再接続動作ができない
                //→Characteristicクラスのコンストラクタにてnotifyイベントを受けるようにする
                GattCommunicationStatus ret= GattCommunicationStatus.Unreachable;
                if (this.services != null && this.services.Count > 0)
                {//初回接続時のイベントハンドラによるリクエストについては処理をしないようにする

                    foreach (var service in services)
                    {
                        foreach (var characteristic in service.Characteristics)
                        {//TODO:Disconnectとなりここら辺が実行される途中で落ちる→この中で使うWriteClientCharacteristicConfigurationDescriptorAsyncが非同期であるため、これが有効になるまでに次の処理に進むためエラー？
                            //切断された後、再度接続した際はnotifyによるValueChanged イベントを再度受信できるようにする
                           
                           ret=await Task.Run(()=> characteristic.CanNotifyCharacteristic());
                           if (ret == GattCommunicationStatus.Success)
                           {//TODO:ここでnotifyを許可にするまで待つようにしても、結局はまたずにすすむ・・・
                             //この実装だとserviceが持っているcharacteristicのうちどれかがnotifyになれば接続されたとなるが、本当はserviceが接続したではなく
                             //Chracteristicsが接続したとするべき
                                this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connected);//serviceのなかにChracteristicsがひとつだけなら、この形のservice単位の実装でOKだが、本当は複数Chracteristicsがあると考えるべき
                                this.Status = TypeStatus.Coonected;

                                characteristic.Characteristic.ValueChanged += characteristic.registeredCharacteristicNotify;
                            }
                            else
                            {
                                ;
                            }
                        }
                    }
                }
                /*
                if (ret== GattCommunicationStatus.Success)
                {//TODO:ここでnotifyを許可にするまで待つようにしても、結局はまたずにすすむ・・・
                 //この実装だとserviceが持っているcharacteristicのうちどれかがnotifyになれば接続されたとなる
                    this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connected);
                    this.Status = TypeStatus.Coonected;
                }
                */
            }
            else
            {
                //TODO : Disconnectedになると落ちてしまうみたい・・・
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
