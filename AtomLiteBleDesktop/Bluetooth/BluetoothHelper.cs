﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using static AtomLiteBleDesktop.Bluetooth.BluetoothUuidDefine;

namespace AtomLiteBleDesktop.Bluetooth
{
    public  class BluetoothHelper
    {
        /// <summary>
        ///     Converts from standard 128bit UUID to the assigned 32bit UUIDs. Makes it easy to compare services
        ///     that devices expose to the standard list.
        /// </summary>
        /// <param name="uuid">UUID to convert to 32 bit</param>
        /// <returns></returns>
        public static ushort ConvertUuidToShortId(Guid uuid)
        {
            // Get the short Uuid
            var bytes = uuid.ToByteArray();
            var shortUuid = (ushort)(bytes[0] | (bytes[1] << 8));
            return shortUuid;
        }

        public static GattNativeServiceUuid GetGattNativeServiceUuid(GattDeviceService service)
        {
            if (IsSigDefinedUuid(service.Uuid))
            {
                GattNativeServiceUuid serviceName;
                if (Enum.TryParse(ConvertUuidToShortId(service.Uuid).ToString(), out serviceName))
                {
                    return serviceName;
                }
            }
            return GattNativeServiceUuid.None;
        }


        public static string GetServiceName(GattDeviceService service)
        {
            if (IsSigDefinedUuid(service.Uuid))
            {
                GattNativeServiceUuid serviceName;
                if (Enum.TryParse(ConvertUuidToShortId(service.Uuid).ToString(), out serviceName))
                {
                    return serviceName.ToString();
                }
            }
            return "Custom Service: " + service.Uuid;
        }
        /// <summary>
        /// 定義済みUUIDかどうか判別する
        ///     The SIG has a standard base value for Assigned UUIDs. In order to determine if a UUID is SIG defined,
        ///     zero out the unique section and compare the base sections.
        ///     
        /// </summary>
        /// <param name="uuid">The UUID to determine if SIG assigned</param>
        /// <returns></returns>
        public static bool IsSigDefinedUuid(Guid uuid)
        {
            var bluetoothBaseUuid = new Guid("00000000-0000-1000-8000-00805F9B34FB");

            var bytes = uuid.ToByteArray();
            // Zero out the first and second bytes
            // Note how each byte gets flipped in a section - 1234 becomes 34 12
            // Example Guid: 35918bc9-1234-40ea-9779-889d79b753f0
            //                   ^^^^
            // bytes output = C9 8B 91 35 34 12 EA 40 97 79 88 9D 79 B7 53 F0
            //                ^^ ^^
            bytes[0] = 0;
            bytes[1] = 0;
            var baseUuid = new Guid(bytes);
            return baseUuid == bluetoothBaseUuid;
        }

        /// <summary>
        /// Windows プロパティ システムよりDeviceの接続状態を取得します
        /// </summary>
        public static bool IsConnected(DeviceInformation DeviceInformation)
        {
            try
            {
                if ((bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true)
                {//デバイスが現在システムに接続されているかどうかを判定
                   return  true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
            return false;
        }

        /// <summary>
        /// Windows プロパティ システムよりDeviceのアドバタイズ状態を取得します
        /// </summary>
        public static bool IsConnectable(DeviceInformation DeviceInformation)
        {
            try
            {
                if ((bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true)
                {//Bluetooth LE デバイスが現在、接続可能なアドバタイズをアドバタイズしているかどうかを判定
                    return true;

                }
                else
                {
                    return  false;
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
            return false;
        }
    }
}
