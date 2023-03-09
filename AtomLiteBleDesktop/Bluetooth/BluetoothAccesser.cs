using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Core;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothAccesser
    {
        /// <summary>
        /// Connect時の最大リトライ回数
        /// </summary>
        private const int MAX_RETRY_CONNECT = 5;

        /// <summary>
        /// BluetoothAccesserのeventハンドラの関数の引数と戻り値を設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void NotifyBluetoothAccesserEventHandler(object sender, NotifyBluetoothAccesserEventArgs e);

        /// <summary>
        /// BluetoothAccesserがもつeventの引数クラス
        /// </summary>
        public class NotifyBluetoothAccesserEventArgs : EventArgs
        {
            public enum Status
            {
                Connected,
                Connecting,
                Abort
            }
            private Status state;
            /// <summary>
            /// 接続状態
            /// </summary>
            public Status State
            {
                get { return this.state; }
                set { this.state = value; }
            }
            private string message;
            /// <summary>
            /// メッセージ
            /// </summary>
            public string Message
            {
                get { return this.message; }
                set { this.message = value; }
            }
        }

        /// <summary>
        /// BluetoothWatcherインスタンス
        /// </summary>
        private BluetoothWatcher bluetoothWatcher;
        /// <summary>
        /// BluetoothConnectorインスタンス
        /// </summary>
        private BluetoothConnector bluetoothConnector;

        private GattCharacteristic registeredCharacteristic;

        /// <summary>
        /// servicesを取得します
        /// </summary>
        public List<BluetoothService> Services
        {
            get { return this.bluetoothConnector.Services; }
        }

        /// <summary>
        /// Characteristicの名前を取得します
        /// </summary>
        public List<string> CharacteristicNames
        {
            get { return this.bluetoothConnector.CharacteristicNames; }
        }

        /// <summary>
        /// ServerConnectイベント
        /// </summary>
        public event NotifyBluetoothAccesserEventHandler NotifyConnectingServer;
        /// <summary>
        /// BleServerからのNotify受信イベント
        /// </summary>
        public event NotifyBluetoothAccesserEventHandler NotifyReceiveCharacteristic;

        /// <summary>
        /// ServerConnect時イベントキック用関数
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnNotifyConnectingServer(string message,　NotifyBluetoothAccesserEventArgs.Status state)
        {
            if (NotifyConnectingServer != null)
            {
                var e = new NotifyBluetoothAccesserEventArgs();
                e.Message = message;
                e.State = state;
                NotifyConnectingServer(this, e);
            }
        }
        protected virtual void OnNotifyReceiveCharacteristic(NotifyReceiveCharacteristicEventArgs e)
        {
            if (NotifyReceiveCharacteristic != null)
            {
                var data = new NotifyBluetoothAccesserEventArgs();
                data.Message = e.Message;
                NotifyReceiveCharacteristic(this, data);
            }
        }


        //UIスレッドにアクセスするためのDispatcher
        private static CoreDispatcher _mDispatcher;

        /// <summary>
        /// コンストラクタ:引数なしでないとXamlのResourceDictionnaryに登録できない
        /// </summary>
        public BluetoothAccesser()
        {
        }

        /// <summary>
        /// 指定したサーバ名を探し、存在すればそのサーバ名を返却：非同期
        /// </summary>
        /// <param name="PIRSERVER"></param>
        /// <returns></returns>
        public async Task<string> Search(string PIRSERVER)
        {
            this.bluetoothWatcher = BluetoothWatcher.GetInstance();

            var task = await Task.Run<string>(() =>
            {
                return this.bluetoothWatcher.SearchSync(PIRSERVER);
            });
            return task;
        }

        /// <summary>
        /// Bluetoothデバイスを1minスキャンします
        /// </summary>
        /// <returns></returns>
        public async Task<ObservableCollection<BluetoothLEDevice>> StartScanning()
        {
            this.bluetoothWatcher = BluetoothWatcher.GetInstance();

            var task = await Task.Run<ObservableCollection<BluetoothLEDevice>>(() =>
            {
                return this.bluetoothWatcher.StartScanServer();
            });
            return task;
        }

        /// <summary>
        /// Bluetoothデバイススキャンを中止します
        /// </summary>
        public void StopScanning()
        {
            this.bluetoothWatcher = BluetoothWatcher.GetInstance();
            this.bluetoothWatcher.StopScanServer();
        }

        public async void Connect()
        {
            int counter = 0;
            this.bluetoothConnector = new BluetoothConnector(this.bluetoothWatcher.DeviceInfoSerchedServer);

            this.bluetoothConnector.NotifyReceiveCharacteristic += this.registeredCharacteristicNotify;

            await Task.Run(async () =>
            {
                counter = MAX_RETRY_CONNECT;
                while (counter > 0)
                {
                    var task = await Task.Run(this.bluetoothConnector.Connect);
                    if (task)
                    {
                        this.OnNotifyConnectingServer("Connected Server!", NotifyBluetoothAccesserEventArgs.Status.Connected);

                        this.registeredCharacteristic = this.bluetoothConnector.RegisteredCharacteristic;

                            //Notify受信イベントハンドラの登録とデバイスから ValueChanged イベントを受信できるようにします。
                            if (this.registeredCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            await this.registeredCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        }
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
        /// Characteristic受信時イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void registeredCharacteristicNotify(object sender, NotifyReceiveCharacteristicEventArgs e)
        {
            OnNotifyReceiveCharacteristic(e);
        }
    }
}
