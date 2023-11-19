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
        /// 
        /// </summary>
        /// <param name="serviceUUID"></param>
        /// <param name="characteristicUUID"></param>
        /// <param name="sendData"></param>
        public override void SendData(string serviceUUID, string characteristicUUID, string sendData)
        {
            var beforeStatus = this.Status;
            this.Status = TypeStatus.Sending;

            var sendDataType = ToTypeStateWaitingSend(sendData);

            foreach (var server in this.Services)
            {
                if (server.Service.Uuid == new Guid(serviceUUID))
                {
                    foreach (var characteristic in server.Characteristics)
                    {
                        if (characteristic.Characteristic.Uuid == new Guid(characteristicUUID))
                        {
                            if (sendDataType != BluetoothCharacteristic.TypeStateWaitingSend.None)
                            {
                                characteristic.WriteCharacterCharacteristic(sendDataType);
                            }
                            else
                            {
                                characteristic.WriteCharacterCharacteristic(sendData);
                            }
                            this.status = beforeStatus;
                        }
                    }
                    break;
                }
            }
        }
    }
}
