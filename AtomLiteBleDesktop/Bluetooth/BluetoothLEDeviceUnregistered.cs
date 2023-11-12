﻿using System;
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

        public override TypeStatus Status { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


        protected override void SendDataService(string serviceUUID, string characteristicUUID, BluetoothCharacteristic.TypeStateWaitingSend sendData, TypeStatus beforeStatus, List<BluetoothService> services)
        {
            throw new NotImplementedException();
        }
    }
}
