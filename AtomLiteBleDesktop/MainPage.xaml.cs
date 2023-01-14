using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Bluetooth.Advertisement;
using System.Threading;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using System.Text;
using SDKTemplate;
using AtomLitePIR.Bluetooth;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace AtomLitePIR
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        private const string PIRSERVER = "ESP32PIRTRI";

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


        static BluetoothLEAdvertisementWatcher watcher;
        private BluetoothLEDevice bluetoothLeDevice = null;
        private List<BluetoothService> services = new List<BluetoothService>();
        private GattPresentationFormat presentationFormat;
        private GattCharacteristic selectedCharacteristic;
        private GattCharacteristic registeredCharacteristic;

        private TextNotifyPropertyChanged _textData = new TextNotifyPropertyChanged();
        private IReadOnlyList<GattCharacteristic> characteristics = null;

        private BluetoothWatcher bluetoothWatcher;

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = new
            {
                textData = _textData,
            };
        }

        private void PageLoaded(FrameworkElement sender, object args)
        {
            this.bluetoothWatcher = new BluetoothWatcher(this.Dispatcher);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.bluetoothWatcher.PIRServer = PIRSERVER;
            this.bluetoothWatcher.StartBleDeviceWatcher();
            this._textData.Text = "Searching";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.bluetoothWatcher.StopBleDeviceWatcher();

            this._textData.Text = "Searched"+ this.bluetoothWatcher.DeviceInfoSerchedServer.Name+" : "+ this.bluetoothWatcher.DeviceInfoSerchedServer.Id;
        }

        private static void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Debug.WriteLine("---Received---");
            var bleServiceUUIDs = args.Advertisement.ServiceUuids;

            Debug.WriteLine("Found");
            Debug.WriteLine("MAC:" + args.BluetoothAddress.ToString());
            Debug.WriteLine("NAME:" + args.Advertisement.LocalName.ToString());
            Debug.WriteLine("ServiceUuid");
            foreach (var uuidone in bleServiceUUIDs)
            {
                Debug.WriteLine(uuidone.ToString());
            }
            Debug.WriteLine("---END---");
            Debug.WriteLine("");
        }


        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            String tmp = string.Copy(this._textData.Text);
            Task.Run(Connect).Wait();
                foreach (var service in services)
                {
                    _textData.Text += "\n" + string.Copy(service.ServiceGattNativeServiceUuidString);
                    if (service.ServiceGattNativeServiceUuid == GattNativeServiceUuid.None)
                    {
                        this.setCharacteristics(service);
                    }
                }
            
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

        private async Task<List<BluetoothService>> Connect()
        {
            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                this.bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(this.bluetoothWatcher.DeviceInfoSerchedServer.Id);
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
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    foreach (var service in services)
                    {
                        this.services.Add(new BluetoothService()
                        {
                            Service = service,
                            ServiceGattNativeServiceUuid= GetGattNativeServiceUuid(service),
                            ServiceGattNativeServiceUuidString= GetServiceName(service)
                        }) ;
                    }
                }
                else
                {
                    Debug.WriteLine("Device unreachable");
                }
            }
            return this.services;
        }

        private async void setCharacteristics(BluetoothService bleService)
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

                _textData.Text+="\n"+ GetCharacteristicName(c);
                if(GetCharacteristicType(c)== GattNativeCharacteristicUuid.None){
                    //定義済みuuidでないものをCharacteristicsとして取得する
                    this.registeredCharacteristic = c;
                }
            }
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
            public static string GetCharacteristicName(GattCharacteristic characteristic)
        {
            GattNativeCharacteristicUuid output;
            if ((output=GetCharacteristicType(characteristic))!= GattNativeCharacteristicUuid.None)
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

        private async void CharacteristicRead(GattCharacteristic selectedCharacteristic)
        {
            // BT_Code: Read the actual value from the device by using Uncached.
            GattReadResult result = await selectedCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success)
            {
                string formattedResult = FormatValueByPresentation(result.Value, presentationFormat);
            }
            else
            {
            }
        }
        private string FormatValueByPresentation(IBuffer buffer, GattPresentationFormat format)
        {
            // BT_Code: For the purpose of this sample, this function converts only UInt32 and
            // UTF-8 buffers to readable text. It can be extended to support other formats if your app needs them.
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            if (format != null)
            {
                if (format.FormatType == GattPresentationFormatTypes.UInt32 && data.Length >= 4)
                {
                    return BitConverter.ToInt32(data, 0).ToString();
                }
                else if (format.FormatType == GattPresentationFormatTypes.Utf8)
                {
                    try
                    {
                        return Encoding.UTF8.GetString(data);
                    }
                    catch (ArgumentException)
                    {
                        return "(error: Invalid UTF-8 string)";
                    }
                }
                else
                {
                    // Add support for other format types as needed.
                    return "Unsupported format: " + CryptographicBuffer.EncodeToHexString(buffer);
                }
            }
            else if (data != null)
            {
                
                // We don't know what format to use. Let's try some well-known profiles, or default back to UTF-8.
                if (this.registeredCharacteristic.Uuid.Equals(GattCharacteristicUuids.HeartRateMeasurement))
                {
                    try
                    {
                        return "Heart Rate: " + ParseHeartRateValue(data).ToString();
                    }
                    catch (ArgumentException)
                    {
                        return "Heart Rate: (unable to parse)";
                    }
                }
                else if (this.registeredCharacteristic.Uuid.Equals(GattCharacteristicUuids.BatteryLevel))
                {
                    try
                    {
                        // battery level is encoded as a percentage value in the first byte according to
                        // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.battery_level.xml
                        return "Battery Level: " + data[0].ToString() + "%";
                    }
                    catch (ArgumentException)
                    {
                        return "Battery Level: (unable to parse)";
                    }
                }
                // This is our custom calc service Result UUID. Format it like an Int
                else if (this.registeredCharacteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
                {
                    return BitConverter.ToInt32(data, 0).ToString();
                }
                /*
                // No guarantees on if a characteristic is registered for notifications.
                else if (registeredCharacteristic != null)
                {
                    // This is our custom calc service Result UUID. Format it like an Int
                    if (registeredCharacteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
                    {
                        return BitConverter.ToInt32(data, 0).ToString();
                    }
                }*/
                else
                {
                    try
                    {
                        return "Unknown format: " + Encoding.UTF8.GetString(data);
                    }
                    catch (ArgumentException)
                    {
                        return "Unknown format";
                    }
                }
            }
            else
            {
                return "Empty data received";
            }
            return "Unknown format";
        }
        /// <summary>
        /// Process the raw data received from the device into application usable data,
        /// according the the Bluetooth Heart Rate Profile.
        /// https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.heart_rate_measurement.xml&u=org.bluetooth.characteristic.heart_rate_measurement.xml
        /// This function throws an exception if the data cannot be parsed.
        /// </summary>
        /// <param name="data">Raw data received from the heart rate monitor.</param>
        /// <returns>The heart rate measurement value.</returns>
        private static ushort ParseHeartRateValue(byte[] data)
        {
            // Heart Rate profile defined flag values
            const byte heartRateValueFormat = 0x01;

            byte flags = data[0];
            bool isHeartRateValueSizeLong = ((flags & heartRateValueFormat) != 0);

            if (isHeartRateValueSizeLong)
            {
                return BitConverter.ToUInt16(data, 1);
            }
            else
            {
                return data[1];
            }
        }

        private async void readCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                // BT_Code: Read the actual value from the device by using Uncached.
                GattReadResult result = await this.registeredCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    presentationFormat = null;
                    string formattedResult = FormatValueByPresentation(result.Value, presentationFormat);
                    this._textData.Text += "\n"+ formattedResult;
                }
                else
                {
                }
            }catch(Exception err)
            {
                throw err;
            }
        }


        /*
        static async Task AsyncMain()
        {
            var deviceName = "ESP32PIRTRI";
            try
            {
                //アドバタイズメントを検索する
                var watcher = new BluetoothLEAdvertisementWatcher();
                ulong btaddr = 0;

                watcher.Received += (BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args) =>
                {
                    //検出時コールバック
                    Debug.WriteLine("+ " + args.Advertisement.LocalName);
                    if (args.Advertisement.LocalName == deviceName)
                    {
                        //デバイスを見つけた
                        Debug.WriteLine("Found: " + args.BluetoothAddress);
                        btaddr = args.BluetoothAddress;
                    }
                };
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                watcher.Start();

                Debug.WriteLine("Scan Start.");

                //5秒間スキャンする
                await Task.Delay(5000);
                watcher.Stop();

                Debug.WriteLine("Scan Stop.");

                if (btaddr == 0)
                {
                    Debug.WriteLine("Not found.");
                    return;
                }

                //デバイスに接続する
                Debug.WriteLine("Connect...");
                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(btaddr);


                //UUIDからサービスを取得する
                Debug.Write("Service: ");
                var services = await device.GetGattServicesForUuidAsync(new Guid("00000000-0000-4000-A000-000000000000"));
                Debug.WriteLine(services.Status);

                //UUIDからキャラクタリスティックを取得する
                Debug.Write("Characteristic: ");
                var characteristics = await services.Services[0].GetCharacteristicsForUuidAsync(new Guid("00000000-0000-4000-A000-000000000001"));
                Debug.WriteLine(characteristics.Status);

                var c = characteristics.Characteristics[0];

                //通知を受け取るコールバックを設定
                c.ValueChanged += (GattCharacteristic sender, GattValueChangedEventArgs args) =>
                {
                    Debug.Write("Notify: ");
                    var streamNotify = args.CharacteristicValue.AsStream();
                    PrintFromStream(streamNotify);
                };
                //通知購読登録
                await c.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                //単純な値読み込み
                Debug.Write("Read: ");
                var d = await c.ReadValueAsync();
                var streamRead = d.Value.AsStream();
                PrintFromStream(streamRead);

                //単純な値書き込み
                Debug.WriteLine("Write");
                await c.WriteValueAsync(new byte[1] { 0xAA }.AsBuffer());


                //60秒待つ(通知が飛んでくるはず)
                await Task.Delay(60 * 1000);

                //開放すると自動で切断される
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        */
        /*
        static void PrintFromStream(Stream s)
        {
            //1byteずつ読み込んで表示
            int d = 0;
            while ((d = s.ReadByte()) != -1)
            {
                Debug.Write(d.ToString("X"));
                Debug.Write(",");
            }
            Debug.WriteLine("");
        }
        */
    }
}
