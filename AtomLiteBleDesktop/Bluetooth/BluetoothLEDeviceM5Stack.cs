using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothLEDeviceM5Stack:BluetoothLEDevice
    {
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
        override public void SendData(string serviceUUID, string characteristicUUID, BluetoothCharacteristic.TypeStateWaitingSend sendData)
        {
            var beforeStatus = this.status;
            this.status = TypeStatus.Sending;
            foreach (var server in this.services)
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
