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
using Windows.UI.Popups;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace AtomLitePIR
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private const string PIRSERVER = "ESP32PIRTRI";



        static BluetoothLEAdvertisementWatcher watcher;
        private GattPresentationFormat presentationFormat;
        private GattCharacteristic selectedCharacteristic;

        private TextNotifyPropertyChanged _textData = new TextNotifyPropertyChanged();

        private BluetoothWatcher bluetoothWatcher;
        private BluetoothConnector bluetoothConnector;

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.bluetoothWatcher.PIRServer = PIRSERVER;
            this.bluetoothWatcher.StartBleDeviceWatcher();
            var task =  await Task.Run<string>(() => {
                //1s待って。接続Server名が取得できなければnullを返す
                int counter = 100;
                while(this.bluetoothWatcher.PIRServerSearched==null )
                {
                    if (counter == 0)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                    counter--;
                }
                if(this.bluetoothWatcher.PIRServerSearched != null)
                {
                    return bluetoothWatcher.PIRServerSearched;
                }
                else
                {
                    return null;
                }
            });
            if (task != null)
            {
                this._textData.Text = "取得サーバー名:\n" + task;
            }
            else
            {
                var dialog = new MessageDialog("接続指定サーバをSearchしたが取得できませんでした", "エラー");
                _ = dialog.ShowAsync();
            }
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
            if (this.bluetoothWatcher.DeviceInfoSerchedServer != null)
            {
                var connector = new BluetoothConnector(this.bluetoothWatcher.DeviceInfoSerchedServer);
                var task = Task.Run(connector.Connect);
                var characteristic = task.Result;
                _textData.Text += "\n" + "取得Service名：";
                foreach (var service in connector.Services)
                {
                    _textData.Text += "\n" + string.Copy(service.ServiceGattNativeServiceUuidString);
                }
                _textData.Text += "\n" + "取得Characteristic名：";
                foreach (var name in connector.CharacteristicNames)
                {
                    _textData.Text += "\n" + string.Copy(name);

                }
            }
            else
            {
                var dialog = new MessageDialog("Scanが実行されていません", "エラー");
                _ = dialog.ShowAsync();
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
                if (bluetoothConnector.RegisteredCharacteristic.Uuid.Equals(GattCharacteristicUuids.HeartRateMeasurement))
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
                else if (this.bluetoothConnector.RegisteredCharacteristic.Uuid.Equals(GattCharacteristicUuids.BatteryLevel))
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
                else if (this.bluetoothConnector.RegisteredCharacteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
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
                GattReadResult result = await this.bluetoothConnector.RegisteredCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
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
    }
}
