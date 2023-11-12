using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothLEDeviceM5Stack : BluetoothLEDevice
    {

        private TypeStatus status;
        /// <summary>
        /// デバイスの現状の接続状態を取得します
        /// </summary>
        override public TypeStatus Status
        {
            set { this.status = value; }
            get { return this.status; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="deviceInfoIn"></param>
        public BluetoothLEDeviceM5Stack(DeviceInformation deviceInfoIn) : base(deviceInfoIn)
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        public BluetoothLEDeviceM5Stack(string name) : base(name)
        {

        }

        /// <summary>
        /// 指定したデータを送信する
        /// </summary>
        /// <param name="serviceUUID"></param>
        /// <param name="characteristicUUID"></param>
        /// <param name="sendData"></param>
        /// <param name="beforeStatus"></param>
        /// <param name="services"></param>
        override protected void SendDataService
            (string serviceUUID, 
            string characteristicUUID, 
            BluetoothCharacteristic.TypeStateWaitingSend sendData, 
            TypeStatus beforeStatus, 
            List<BluetoothService> services)
        {
            foreach (var server in services)
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
    }
}
