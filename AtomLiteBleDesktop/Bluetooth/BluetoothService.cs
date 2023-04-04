using AtomLiteBleDesktop.Bluetooth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using static AtomLiteBleDesktop.Bluetooth.BluetoothConnector;
using static AtomLiteBleDesktop.MainPage;

namespace AtomLiteBleDesktop
{
    public class BluetoothService
    {
        //NotifyReceiveCharacteristicイベントで返されるデータ
        //ここではstring型のひとつのデータのみ返すものとする
        public class NotifyReceiveServerCharacteristicEventArgs : EventArgs
        {
            public BluetoothCharacteristic Characteristic;
            public string Message;
        }

        private GattDeviceService service;
        /// <summary>
        /// BluetoothService
        /// </summary>
        public GattDeviceService Service
        {
            set { this.service = value; }
            get { return this.service; }
        }

        private List<string> characteristicNames;
        /// <summary>
        /// Characteristicの名前を取得します
        /// </summary>
        public List<string> CharacteristicNames
        {
            get { return this.characteristicNames; }
        }

        private List<BluetoothCharacteristic> characteristics = null;
        /// <summary>
        /// Characteristicsを取得します
        /// </summary>
        public List<BluetoothCharacteristic> Characteristics
        {
            get { return this.characteristics; }
        }

        /*
        private IReadOnlyList<GattCharacteristic> characteristics = null;
        /// <summary>
        /// Characteristicsを取得します
        /// </summary>
        public IReadOnlyList<GattCharacteristic> Characteristics
        {
            get { return this.characteristics; }
        }
        */

        private  string serviceListGattNativeServiceUuidString;
        /// <summary>
        /// BluetoothService規定のUUID名(文字列)
        /// </summary>
        public string ServiceGattNativeServiceUuidString
        {
            set { this.serviceListGattNativeServiceUuidString = value; }
            get { return this.serviceListGattNativeServiceUuidString; }
        }

        private GattNativeServiceUuid serviceGattNativeServiceUuid;
        /// <summary>
        /// BluetoothService規定のUUID名
        /// </summary>
        public GattNativeServiceUuid ServiceGattNativeServiceUuid
        {
            set { this.serviceGattNativeServiceUuid = value; }
            get { return this.serviceGattNativeServiceUuid; }
        }

        public delegate void NotifyReceiveServerCharacteristicEventHandler(object sender, NotifyReceiveServerCharacteristicEventArgs e);
        private event NotifyReceiveServerCharacteristicEventHandler notifyReceiveCharacteristic;
        public event NotifyReceiveServerCharacteristicEventHandler NotifyReceiveCharacteristic
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
        /// コンストラクタ
        /// </summary>
        public BluetoothService()
        {
            this.characteristics = new List<BluetoothCharacteristic>();

        }

        /// <summary>
        /// serviceの中から未定義のCharacteristicをRegistedCharacteristicとして取得する
        /// </summary>
        /// <param name="bleService"></param>
        public async Task<GattCharacteristic> GetRegisteredCharacteristic()
        {
            var service = this.Service;

            try
            {
                // Ensure we have access to the device.
                var accessStatus = await service.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        foreach(var characteristic in result.Characteristics)
                        {
                            var bluetoothCharacteristic = new BluetoothCharacteristic(characteristic);
                            bluetoothCharacteristic.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
                            characteristics.Add(bluetoothCharacteristic);
                        }
                    }
                    else
                    {
                        foreach (var characteristic in result.Characteristics)
                        {
                            // On error, act as if there are no characteristics.
                            characteristics.Add(new BluetoothCharacteristic());
                        }
                    }
                }
                else
                {
                    // On error, act as if there are no characteristics.
                    characteristics.Add(new BluetoothCharacteristic());

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // On error, act as if there are no characteristics.
                characteristics.Add(new BluetoothCharacteristic());
            }

            foreach (var characteristic in this.characteristics)
            {
                if (characteristic.Type== GattNativeCharacteristicUuid.None)
                {
                    return characteristic.Characteristic;
                }
            }
            return null;
        }

        /// <summary>
        /// BleServerからのNotify受信イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void registeredCharacteristicNotify(object sender, NotifyReceiveCharacteristicEventArgs eventArgs)
        {
            try
            {
                NotifyReceiveServerCharacteristicEventArgs e = new NotifyReceiveServerCharacteristicEventArgs();
                e.Characteristic = sender as BluetoothCharacteristic;
                e.Message = eventArgs.Message;
                this.OnNotifyReceiveCharacteristic(e);
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        /// <summary>
        /// イベント発生時実行関数
        /// registeredCharacteristicNotifyイベント引継ぎ、このクラスのイベントとする
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnNotifyReceiveCharacteristic(NotifyReceiveServerCharacteristicEventArgs e)
        {
            if (notifyReceiveCharacteristic != null)
            {
                notifyReceiveCharacteristic(this, e);
            }
        }
    }
}
