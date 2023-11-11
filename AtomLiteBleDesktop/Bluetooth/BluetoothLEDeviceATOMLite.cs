using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothLEDeviceATOMLite : BluetoothLEDevice
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
        public BluetoothLEDeviceATOMLite(DeviceInformation deviceInfoIn):base( deviceInfoIn)
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        public BluetoothLEDeviceATOMLite(string name) : base(name)
        {

        }

        override protected void SendDataService(string serviceUUID, string characteristicUUID, BluetoothCharacteristic.TypeStateWaitingSend sendData, TypeStatus beforeStatus,List<BluetoothService> services)
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
