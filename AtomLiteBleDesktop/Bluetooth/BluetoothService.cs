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
using static AtomLiteBleDesktop.Bluetooth.BluetoothUuidDefine;
using static AtomLiteBleDesktop.MainPage;

namespace AtomLiteBleDesktop
{
    public class BluetoothService
    {

        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //NotifyReceiveCharacteristicイベントで返されるデータ
        //ここではstring型のひとつのデータのみ返すものとする
        public class NotifyReceiveServerCharacteristicEventArgs : EventArgs
        {
            public BluetoothCharacteristic Characteristic;
            public string Message;
            public BluetoothCharacteristic.TypeStateReseive State;
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
        [Obsolete]
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
                    var result = await service.GetCharacteristicsAsync(BluetoothCacheMode.Cached);//この関数でDevice.ConnectionStatusがSuccessかUnSuccessかが決まる。よって実際に接続できたかはこの関数が実行されたのちにConnectionStausを調べなくてはいけない
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        /*
                        if (service.Device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                        {
                            foreach (var characteristic in result.Characteristics)
                            {
                                var bluetoothCharacteristic = new BluetoothCharacteristic(characteristic);
                                bluetoothCharacteristic.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
                                characteristics.Add(bluetoothCharacteristic);
                                logger.Info("set EventHandler:" + bluetoothCharacteristic.Name);
                            }
                        }
                        else
                        {
                            return null;
                        }
                        */
                        //起動時にデバイスがOFFされていても、Characteristicのコールアウト関数は登録し、電源ＯＮ時に再接続できるようにする
                        foreach (var characteristic in result.Characteristics)
                        {
                            var bluetoothCharacteristic = new BluetoothCharacteristic(characteristic);
                            bluetoothCharacteristic.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
                            characteristics.Add(bluetoothCharacteristic);
                            logger.Info("set EventHandler:" + bluetoothCharacteristic.Name);
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
                e.State = eventArgs.State;
#if DEBUG
                logger.Info("ReceiveCharacteristic Name :" + e.Characteristic.Name + " ReceiveData:"+e.Message);
#endif                
                this.OnNotifyReceiveCharacteristic(e);
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
                //throw err;
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
