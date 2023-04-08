using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static AtomLiteBleDesktop.Bluetooth.BluetoothConnector;

namespace AtomLiteBleDesktop.Bluetooth
{
    //NotifyReceiveCharacteristicイベントで返されるデータ
    //ここではstring型のひとつのデータのみ返すものとする
    public class NotifyReceiveCharacteristicEventArgs : EventArgs
    {
        public string Message;
    }

    public class BluetoothCharacteristic
    {

        private GattCharacteristic characteristic = null;
        /// <summary>
        /// Characteristicsを取得します
        /// </summary>
        public GattCharacteristic Characteristic
        {
            get { return this.characteristic; }
        }

        private string name;
        /// <summary>
        /// 名前を取得します
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        private GattNativeCharacteristicUuid type;
        public GattNativeCharacteristicUuid Type
        {
            get { return this.type; }
        }

        private event NotifyReceiveCharacteristicEventHandler notifyReceiveCharacteristic;
        /// <summary>
        /// Characteristic受信イベント
        /// </summary>
        public event NotifyReceiveCharacteristicEventHandler NotifyReceiveCharacteristic
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
        /// <param name="characteristic"></param>
        public BluetoothCharacteristic(GattCharacteristic characteristic)
        {
            this.characteristic = characteristic;
            CanNotifyCharacteristic();
            this.characteristic.ValueChanged += this.registeredCharacteristicNotify;
            this.name = GetCharacteristicName(characteristic);
            this.type=GetCharacteristicType(characteristic);
            
        }

        /// <summary>
        ///コンストラクタ 
        ///Characteristicはnull
        /// </summary>
        public BluetoothCharacteristic()
        {
            this.characteristic = null;
        }

        /// <summary>
        /// CharacteristicのNotifyを有効とする
        /// </summary>
        public async void CanNotifyCharacteristic()
        {
            if (this.characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {//Notifyをここで有効とする
                await this.characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }
        }

        public static string GetCharacteristicName(GattCharacteristic characteristic)
        {
            GattNativeCharacteristicUuid output;
            if ((output = GetCharacteristicType(characteristic)) != GattNativeCharacteristicUuid.None)
            {
                return output.ToString();
            }

            if (!string.IsNullOrEmpty(characteristic.UserDescription))
            {
                return characteristic.UserDescription;
            }

            else
            {
                return "Custom Characteristic: " + characteristic.Uuid;
            }
        }


        public static GattNativeCharacteristicUuid GetCharacteristicType(GattCharacteristic characteristic)
        {
            if (BluetoothHelper.IsSigDefinedUuid(characteristic.Uuid))
            {
                GattNativeCharacteristicUuid characteristicName;
                if (Enum.TryParse(BluetoothHelper.ConvertUuidToShortId(characteristic.Uuid).ToString(), out characteristicName))
                {
                    return characteristicName;
                }
            }
            return GattNativeCharacteristicUuid.None;
        }
        /// <summary>
        /// BleServerからのNotify受信イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void registeredCharacteristicNotify(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            try
            {
                var data = eventArgs.CharacteristicValue.ToArray();
                var str = Encoding.UTF8.GetString(data);
                NotifyReceiveCharacteristicEventArgs e = new NotifyReceiveCharacteristicEventArgs();
                e.Message = str;
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
        protected virtual void OnNotifyReceiveCharacteristic(NotifyReceiveCharacteristicEventArgs e)
        {
            if (notifyReceiveCharacteristic != null)
            {
                notifyReceiveCharacteristic(this, e);
            }
        }

    }
}
