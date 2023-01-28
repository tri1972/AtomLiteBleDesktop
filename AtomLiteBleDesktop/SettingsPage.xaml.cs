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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLitePIR
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private const string PIRSERVER = "ESP32PIRTRI";

        static BluetoothLEAdvertisementWatcher watcher;
        private GattCharacteristic registeredCharacteristic;

        private TextNotifyPropertyChanged _textData = new TextNotifyPropertyChanged();

        private BluetoothWatcher bluetoothWatcher;
        private BluetoothConnector bluetoothConnector;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.bluetoothWatcher = new BluetoothWatcher(this.Dispatcher);
        }
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = new
            {
                textData = _textData,
            };
        }




        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.bluetoothWatcher.PIRServer = PIRSERVER;
            this.bluetoothWatcher.StartBleDeviceWatcher();
            var task = await Task.Run<string>(() => {
                //1s待って。接続Server名が取得できなければnullを返す
                int counter = 100;
                while (this.bluetoothWatcher.PIRServerSearched == null)
                {
                    if (counter == 0)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                    counter--;
                }
                if (this.bluetoothWatcher.PIRServerSearched != null)
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

            this._textData.Text = "Searched" + this.bluetoothWatcher.DeviceInfoSerchedServer.Name + " : " + this.bluetoothWatcher.DeviceInfoSerchedServer.Id;
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
                this.bluetoothConnector = new BluetoothConnector(this.bluetoothWatcher.DeviceInfoSerchedServer);
                var task = Task.Run(this.bluetoothConnector.Connect);
                this.registeredCharacteristic = task.Result;
                
                //Notify受信イベントハンドラの登録とデバイスから ValueChanged イベントを受信できるようにします。
                if (this.registeredCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    this.registeredCharacteristic.ValueChanged += this.registeredCharacteristicNotify;
                    await this.registeredCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                }
                


                 _textData.Text += "\n" + "取得Service名：";
                foreach (var service in this.bluetoothConnector.Services)
                {
                    _textData.Text += "\n" + string.Copy(service.ServiceGattNativeServiceUuidString);
                }
                _textData.Text += "\n" + "取得Characteristic名：";
                foreach (var name in this.bluetoothConnector.CharacteristicNames)
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
                //UIスレッド側のオブジェクトに対して書き込むため、UIスレッド側のメソッドを作り実行
                //参考：https://m-miya.blog.jp/archives/1063899401.html
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _textData.Text += "\n" + string.Copy(str);
                    var sh = this.scrollViewSettings.ScrollableHeight;
                    this.scrollViewSettings.ScrollToVerticalOffset(sh);
                });
            }catch(Exception err)
            {
                throw err;
            }
        }

        private async void readCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            if (this.registeredCharacteristic != null)
            {
                var receiver = new BluetoothReceiver(this.registeredCharacteristic);

                var task = Task.Run(receiver.ReadCharacteristic);

                this._textData.Text += "\n" + task.Result;
            }
            else
            {
                var dialog = new MessageDialog("Connectされていません(Characteristicが取得できませんでした)", "エラー");
                _ = dialog.ShowAsync();

            }

                /*
                try
                {

                    // BT_Code: Read the actual value from the device by using Uncached.
                    GattReadResult result = await this.bluetoothConnector.RegisteredCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        presentationFormat = null;
                        string formattedResult = FormatValueByPresentation(result.Value, presentationFormat);
                        this._textData.Text += "\n" + formattedResult;
                    }
                    else
                    {
                    }
                }
                catch (Exception err)
                {
                    throw err;
                }
                */
            }
    }

}
