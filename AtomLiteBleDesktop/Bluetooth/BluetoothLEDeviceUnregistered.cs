using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace AtomLiteBleDesktop.Bluetooth
{
    class BluetoothLEDeviceUnregistered : BluetoothLEDevice
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="deviceInfoIn"></param>
        public BluetoothLEDeviceUnregistered(DeviceInformation deviceInfoIn) : base(deviceInfoIn)
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        public BluetoothLEDeviceUnregistered(string name) : base(name)
        {

        }
        /// <summary>
        /// 指定したデータを送信する
        /// </summary>
        /// <param name="serviceUUID"></param>
        /// <param name="characteristicUUID"></param>
        override public void SendData(string serviceUUID, string characteristicUUID, BluetoothCharacteristic.TypeStateWaitingSend sendData)
        {
            ;
        }

    }
}
