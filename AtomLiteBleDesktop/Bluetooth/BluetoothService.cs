using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static AtomLitePIR.Bluetooth.BluetoothConnector;
using static AtomLitePIR.MainPage;

namespace AtomLitePIR
{
    public class BluetoothService
    {

        private GattDeviceService service;
        /// <summary>
        /// BluetoothService
        /// </summary>
        public GattDeviceService Service
        {
            set { this.service = value; }
            get { return this.service; }
        }


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


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BluetoothService()
        {

        }
    }
}
