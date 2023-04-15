using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.UI.Core;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothAccesser
    {

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
                NotFound,
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

        private int numberDevice;
        /// <summary>
        /// BluetoothDevice数
        /// </summary>
        public int NumberDevice
        {
            get { return this.numberDevice; }
        }


        private List<BluetoothLEDevice> devices;
        /// <summary>
        /// Connctsで名前を指定したDevice
        /// </summary>
        public List< BluetoothLEDevice> Devices
        {
            get { return this.devices; }
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

        private event NotifyBluetoothAccesserEventHandler notifyReceiveCharacteristic;
        /// <summary>
        /// BleServerからのNotify受信イベント
        /// </summary>
        public event NotifyBluetoothAccesserEventHandler NotifyReceiveCharacteristic
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

        /// <summary>
        /// コンストラクタ:引数なしでないとXamlのResourceDictionnaryに登録できない
        /// </summary>
        public BluetoothAccesser()
        {
            this.devices = new List<BluetoothLEDevice>();
        }

        public async void SearchDevices(List<string> deviceNames)
        {
            try
            {
                this.numberDevice = deviceNames.Count;
                foreach (var deviceName in deviceNames)
                {
                    var task = await this.SearchDevice(deviceName);
                    if (task != null)
                    {
                        if (task.IsFindDevice)
                        {
                            Debug.WriteLine("取得サーバー名:" + task.Name);
                            task.Connect();
                            //bluetoothAccesser.Connect();
                        }
                        else
                        {
                            Debug.WriteLine(deviceName + "サーバーは見つかりませんでした:\n");

                        }
                    }
                    else
                    {
                        Debug.WriteLine(deviceName + "SearchDeviceでnullが返りました:\n");
                    }

                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);

            }
        }

        /// <summary>
        /// ペアリングができているデバイスを探す 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns></returns>
        public async Task<BluetoothLEDevice> SearchDevice(string deviceName)
        {
            var bluetoothWatcher = BluetoothWatcher.GetInstance();

            var task = await Task.Run<BluetoothLEDevice>(() =>
            {
                var device = bluetoothWatcher.FindServerDevice(deviceName);
                this.devices.Add(device);
                return device;
            });
            return task;
        }
    }
}
