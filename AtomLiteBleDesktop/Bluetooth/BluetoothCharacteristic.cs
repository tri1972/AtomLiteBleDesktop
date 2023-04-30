using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
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
        public BluetoothCharacteristic.TypeStateReseive State;
    }

    public class BluetoothCharacteristic
    {

        public enum TypeStateWaitingSend
        {
            /// <summary>
            /// できるだけ早く
            /// </summary>
            ASAP,
            /// <summary>
            /// ちょっと待って
            /// </summary>
            WAIT,
            /// <summary>
            /// 今都合悪い
            /// </summary>
            WRONG,
            /// <summary>
            /// キャンセル
            /// </summary>
            Cancel,
            /// <summary>
            /// 該当なし
            /// </summary>
            None,

        }

        public enum TypeStateReseive
        {
            Received,
            StartReceiving,
            RepeatReceiving,
            None
        }

        private const int NUMBER_COUNT_WAIT = 10;

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

        private volatile bool isCancelRepeatReceiving;
        /// <summary>
        /// 受信繰り返しを中断します
        /// </summary>
        public  bool IsCancelRepeatReading
        {
            get { return this.isCancelRepeatReceiving; }
        }

        private int numberCounteRx;
        /// <summary>
        /// 受信数を取得します
        /// (ただし、送信動作などで0価となる)
        /// </summary>
        public int NumberCounteRx
        {
            get { return this.numberCounteRx; }
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
            this.isCancelRepeatReceiving = false;
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

        public void WriteCharacterCharacteristic(TypeStateWaitingSend data)
        {
            this.isCancelRepeatReceiving = true;//送信時には受信繰り返しを中断する
            this.numberCounteRx = 0;//送信時に受信数をクリアします
            switch (data)
            {
                case TypeStateWaitingSend.ASAP:
                    BluetoothSender.WriteCharacteristic(this.characteristic, "a");
                    break;
                case TypeStateWaitingSend.WAIT:
                    BluetoothSender.WriteCharacteristic(this.characteristic, "b");
                    break;
                case TypeStateWaitingSend.WRONG:
                    BluetoothSender.WriteCharacteristic(this.characteristic, "c");
                    break;
                case TypeStateWaitingSend.Cancel:
                    BluetoothSender.WriteCharacteristic(this.characteristic, "d");
                    break;
                default:
                    break;

            }
            //this.isCancelRepeatReceiving = false;//送信が終われば受信繰り返しを再開

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
        private async void  registeredCharacteristicNotify(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            try
            {
                var countWait = NUMBER_COUNT_WAIT;


                var data = eventArgs.CharacteristicValue.ToArray();
                var str = Encoding.UTF8.GetString(data);
                NotifyReceiveCharacteristicEventArgs e = new NotifyReceiveCharacteristicEventArgs();
                e.Message = str;
                this.numberCounteRx++;
                this.isCancelRepeatReceiving = false;
                var task=await  Task.Run<bool>(() =>
                {
                    e.State = TypeStateReseive.StartReceiving;
                    this.OnNotifyReceiveCharacteristic(e);
                    while (countWait>0)
                    {
                        if (this.isCancelRepeatReceiving)
                        {
                            break;
                        }
                        Thread.Sleep(1000);
                        e.State = TypeStateReseive.RepeatReceiving;
                        this.OnNotifyReceiveCharacteristic(e);
                        countWait--;
                    }
                    return true;
                });
                if (task)
                {
                    e.State = TypeStateReseive.Received;
                    this.OnNotifyReceiveCharacteristic(e);
                }
                
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
