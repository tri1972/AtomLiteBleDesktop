using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace AtomLiteBleDesktop.Bluetooth
{
    //NotifyReceiveCharacteristicイベントで返されるデータ
    //ここではstring型のひとつのデータのみ返すものとする
    public class NotifyReceiveCharacteristicEventArgs : EventArgs
    {
        public string Message;
    }

    public class BluetoothConnector
    {
        /// <summary>
        /// 定義済みUUIDのリスト
        ///     This enum assists in finding a string representation of a BT SIG assigned value for Service UUIDS
        ///     Reference: https://developer.bluetooth.org/gatt/services/Pages/ServicesHome.aspx
        /// </summary>
        public enum GattNativeServiceUuid : ushort
        {
            None = 0,
            AlertNotification = 0x1811,
            Battery = 0x180F,
            BloodPressure = 0x1810,
            CurrentTimeService = 0x1805,
            CyclingSpeedandCadence = 0x1816,
            DeviceInformation = 0x180A,
            GenericAccess = 0x1800,
            GenericAttribute = 0x1801,
            Glucose = 0x1808,
            HealthThermometer = 0x1809,
            HeartRate = 0x180D,
            HumanInterfaceDevice = 0x1812,
            ImmediateAlert = 0x1802,
            LinkLoss = 0x1803,
            NextDSTChange = 0x1807,
            PhoneAlertStatus = 0x180E,
            ReferenceTimeUpdateService = 0x1806,
            RunningSpeedandCadence = 0x1814,
            ScanParameters = 0x1813,
            TxPower = 0x1804,
            SimpleKeyService = 0xFFE0
        }


        /// <summary>
        ///     This enum is nice for finding a string representation of a BT SIG assigned value for Characteristic UUIDs
        ///     Reference: https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicsHome.aspx
        /// </summary>
        public enum GattNativeCharacteristicUuid : ushort
        {
            None = 0,
            AlertCategoryID = 0x2A43,
            AlertCategoryIDBitMask = 0x2A42,
            AlertLevel = 0x2A06,
            AlertNotificationControlPoint = 0x2A44,
            AlertStatus = 0x2A3F,
            Appearance = 0x2A01,
            BatteryLevel = 0x2A19,
            BloodPressureFeature = 0x2A49,
            BloodPressureMeasurement = 0x2A35,
            BodySensorLocation = 0x2A38,
            BootKeyboardInputReport = 0x2A22,
            BootKeyboardOutputReport = 0x2A32,
            BootMouseInputReport = 0x2A33,
            CSCFeature = 0x2A5C,
            CSCMeasurement = 0x2A5B,
            CurrentTime = 0x2A2B,
            DateTime = 0x2A08,
            DayDateTime = 0x2A0A,
            DayofWeek = 0x2A09,
            DeviceName = 0x2A00,
            DSTOffset = 0x2A0D,
            ExactTime256 = 0x2A0C,
            FirmwareRevisionString = 0x2A26,
            GlucoseFeature = 0x2A51,
            GlucoseMeasurement = 0x2A18,
            GlucoseMeasurementContext = 0x2A34,
            HardwareRevisionString = 0x2A27,
            HeartRateControlPoint = 0x2A39,
            HeartRateMeasurement = 0x2A37,
            HIDControlPoint = 0x2A4C,
            HIDInformation = 0x2A4A,
            IEEE11073_20601RegulatoryCertificationDataList = 0x2A2A,
            IntermediateCuffPressure = 0x2A36,
            IntermediateTemperature = 0x2A1E,
            LocalTimeInformation = 0x2A0F,
            ManufacturerNameString = 0x2A29,
            MeasurementInterval = 0x2A21,
            ModelNumberString = 0x2A24,
            NewAlert = 0x2A46,
            PeripheralPreferredConnectionParameters = 0x2A04,
            PeripheralPrivacyFlag = 0x2A02,
            PnPID = 0x2A50,
            ProtocolMode = 0x2A4E,
            ReconnectionAddress = 0x2A03,
            RecordAccessControlPoint = 0x2A52,
            ReferenceTimeInformation = 0x2A14,
            Report = 0x2A4D,
            ReportMap = 0x2A4B,
            RingerControlPoint = 0x2A40,
            RingerSetting = 0x2A41,
            RSCFeature = 0x2A54,
            RSCMeasurement = 0x2A53,
            SCControlPoint = 0x2A55,
            ScanIntervalWindow = 0x2A4F,
            ScanRefresh = 0x2A31,
            SensorLocation = 0x2A5D,
            SerialNumberString = 0x2A25,
            ServiceChanged = 0x2A05,
            SoftwareRevisionString = 0x2A28,
            SupportedNewAlertCategory = 0x2A47,
            SupportedUnreadAlertCategory = 0x2A48,
            SystemID = 0x2A23,
            TemperatureMeasurement = 0x2A1C,
            TemperatureType = 0x2A1D,
            TimeAccuracy = 0x2A12,
            TimeSource = 0x2A13,
            TimeUpdateControlPoint = 0x2A16,
            TimeUpdateState = 0x2A17,
            TimewithDST = 0x2A11,
            TimeZone = 0x2A0E,
            TxPowerLevel = 0x2A07,
            UnreadAlertStatus = 0x2A45,
            AggregateInput = 0x2A5A,
            AnalogInput = 0x2A58,
            AnalogOutput = 0x2A59,
            CyclingPowerControlPoint = 0x2A66,
            CyclingPowerFeature = 0x2A65,
            CyclingPowerMeasurement = 0x2A63,
            CyclingPowerVector = 0x2A64,
            DigitalInput = 0x2A56,
            DigitalOutput = 0x2A57,
            ExactTime100 = 0x2A0B,
            LNControlPoint = 0x2A6B,
            LNFeature = 0x2A6A,
            LocationandSpeed = 0x2A67,
            Navigation = 0x2A68,
            NetworkAvailability = 0x2A3E,
            PositionQuality = 0x2A69,
            ScientificTemperatureinCelsius = 0x2A3C,
            SecondaryTimeZone = 0x2A10,
            String = 0x2A3D,
            TemperatureinCelsius = 0x2A1F,
            TemperatureinFahrenheit = 0x2A20,
            TimeBroadcast = 0x2A15,
            BatteryLevelState = 0x2A1B,
            BatteryPowerState = 0x2A1A,
            PulseOximetryContinuousMeasurement = 0x2A5F,
            PulseOximetryControlPoint = 0x2A62,
            PulseOximetryFeatures = 0x2A61,
            PulseOximetryPulsatileEvent = 0x2A60,
            SimpleKeyState = 0xFFE1
        }

        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        private Windows.Devices.Bluetooth.BluetoothLEDevice bluetoothLeDevice = null;
        private BluetoothLEDevice deviceInfoSerchedServer = null;

        private List<BluetoothService> services;
        /// <summary>
        /// servicesを取得します
        /// </summary>
        public List<BluetoothService> Services
        {
            get { return this.services; }
        }

        private IReadOnlyList<GattCharacteristic> characteristics = null;
        /// <summary>
        /// Characteristicsを取得します
        /// </summary>
        public IReadOnlyList<GattCharacteristic> Characteristics
        {
            get { return this.characteristics; }
        }

        /// <summary>
        /// Characteristicを取得します
        /// </summary>
        public GattCharacteristic RegisteredCharacteristic
        {
            get { return this.registeredCharacteristic; }
        }
        private GattCharacteristic registeredCharacteristic;

        private List<string> gattNativeServiceUuidName;

        /// <summary>
        /// Serviceに接続されているか否かを取得します
        /// </summary>
        public bool IsConnectService
        {
            get { return this.isConnectService; }
        }
        private bool isConnectService;

        
        /// <summary>
        /// Characteristicの名前を取得します
        /// </summary>
        public List<string> CharacteristicNames
        {
            get { return this.characteristicNames; }
        }
        private List<string> characteristicNames;

        public delegate void NotifyReceiveCharacteristicEventHandler(object sender, NotifyReceiveCharacteristicEventArgs e);
        public event NotifyReceiveCharacteristicEventHandler NotifyReceiveCharacteristic;
        protected virtual void OnNotifyReceiveCharacteristic(NotifyReceiveCharacteristicEventArgs e)
        {
            if (NotifyReceiveCharacteristic != null)
            {
                NotifyReceiveCharacteristic(this, e);
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BluetoothConnector(BluetoothLEDevice deviceInfoSerchedServer)
        {
            this.deviceInfoSerchedServer = deviceInfoSerchedServer;
            this.services = new List<BluetoothService>();
        }
        /// <summary>
        /// 非同期BLE接続処理を実行します
        /// </summary>
        /// <returns></returns>
        public async Task<bool>  Connect()
        {
            Task<GattCharacteristic> task=null;
            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                this.bluetoothLeDevice = await Windows.Devices.Bluetooth.BluetoothLEDevice.FromIdAsync(this.deviceInfoSerchedServer.Id);
                if (bluetoothLeDevice == null)
                {
                    Debug.WriteLine("Failed to connect to device.");
                }
            }
            catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
            {
                Debug.WriteLine("Bluetooth radio is not on.");
            }

            if (bluetoothLeDevice != null)
            {
                bluetoothLeDevice.ConnectionStatusChanged += eventConnectionStatusChanged;
                bluetoothLeDevice.GattServicesChanged += eventGattServicesChanged;
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.

                var tasksub = Task.Run(async () =>
                {

                    GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        var services = result.Services;
                        foreach (var service in services)
                        {
                            this.services.Add(new BluetoothService()
                            {
                                Service = service,
                                ServiceGattNativeServiceUuid = GetGattNativeServiceUuid(service),
                                ServiceGattNativeServiceUuidString = GetServiceName(service)
                            });
                        }
                        task = this.getRegisteredCharacteristic(this.getUserCustomService(this.services));
                        this.registeredCharacteristic = task.Result;
                        this.registeredCharacteristic.ValueChanged += this.registeredCharacteristicNotify;
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("Device unreachable");
                        return false;
                    }
                });
                if (tasksub.Result)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            //task= this.getRegisteredCharacteristic(this.getUserCustomService(this.services));
            //this.registeredCharacteristic = task.Result;
        }

        /// <summary>
        /// BleServerからのNotify受信イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        [Obsolete]
        private async void registeredCharacteristicNotify(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
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
        /// Bleサーバー接続状況変化時イベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        private async void eventConnectionStatusChanged(Windows.Devices.Bluetooth.BluetoothLEDevice e,  object sender)
        {
            if(e.ConnectionStatus== BluetoothConnectionStatus.Connected)
            {
                if (this.services != null && this.services.Count > 0)
                {//初回接続時のイベントハンドラによるリクエストについては処理をしないようにする
                    if (this.registeredCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {//切断された後、再度接続した際はnotifyによるValueChanged イベントを再度受信できるようにする
                        await this.registeredCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    }
                }
                this.isConnectService = true;
            }
            else
            {
                this.isConnectService = false;
            }
            //disconnectになった場合、ここで再度イベントハンドラの登録を行うようにしてみる
        }

        /// <summary>
        /// Bleサーバーサービス変更時イベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        private void eventGattServicesChanged(Windows.Devices.Bluetooth.BluetoothLEDevice e, object sender)
        {
            ;
        }
        /// <summary>
        /// 取得したserviceの中からUserCustomServiceを取得する
        /// </summary>
        /// <param name="services"></param>
        private BluetoothService getUserCustomService(List<BluetoothService> services)
        {
            foreach (var service in services)
            {
                if (this.gattNativeServiceUuidName == null)
                {
                    this.gattNativeServiceUuidName = new List<string>();
                }
                this.gattNativeServiceUuidName.Add( string.Copy(service.ServiceGattNativeServiceUuidString));
                if (service.ServiceGattNativeServiceUuid == GattNativeServiceUuid.None)
                {
                    return service;
                }
            }
            return null;
        }

        /// <summary>
        /// serviceの中から未定義のCharacteristicをRegistedCharacteristicとして取得する
        /// </summary>
        /// <param name="bleService"></param>
        private async Task<GattCharacteristic> getRegisteredCharacteristic(BluetoothService bleService)
        {
            var service = bleService.Service;

            try
            {
                // Ensure we have access to the device.
                var accessStatus = await service.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        characteristics = result.Characteristics;
                    }
                    else
                    {
                        // On error, act as if there are no characteristics.
                        characteristics = new List<GattCharacteristic>();
                    }
                }
                else
                {
                    // On error, act as if there are no characteristics.
                    characteristics = new List<GattCharacteristic>();

                }

            }
            catch (Exception ex)
            {
                // On error, act as if there are no characteristics.
                characteristics = new List<GattCharacteristic>();
            }
            foreach (GattCharacteristic c in characteristics)
            {
                if (this.characteristicNames == null)
                {
                    this.characteristicNames = new List<string>();
                }
                this.characteristicNames.Add (GetCharacteristicName(c));
                if (GetCharacteristicType(c) == GattNativeCharacteristicUuid.None)
                {
                    //定義済みuuidでないものをCharacteristicsとする
                    return c;
                }
            }
            return null;
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
            if (IsSigDefinedUuid(characteristic.Uuid))
            {
                GattNativeCharacteristicUuid characteristicName;
                if (Enum.TryParse(ConvertUuidToShortId(characteristic.Uuid).ToString(), out characteristicName))
                {
                    return characteristicName;
                }
            }
            return GattNativeCharacteristicUuid.None;
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

        /// <summary>
        /// 定義済みUUIDかどうか判別する
        ///     The SIG has a standard base value for Assigned UUIDs. In order to determine if a UUID is SIG defined,
        ///     zero out the unique section and compare the base sections.
        ///     
        /// </summary>
        /// <param name="uuid">The UUID to determine if SIG assigned</param>
        /// <returns></returns>
        private static bool IsSigDefinedUuid(Guid uuid)
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
    }
}
