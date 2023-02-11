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
using AtomLiteBleDesktop.Bluetooth;
using Windows.UI.Popups;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private const string PIRSERVER = "ESP32PIRTRI";
        private const int MAX_RETRY_CONNECT = 5;

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

            var hogehogeData = Application.Current.Resources["HogeDataInstance"] as TextNotifyPropertyChanged;
            hogehogeData.Text = "hogehoge";




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

            //通知の中身を作成
            var content = new ToastContent
            {
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children = {
                new AdaptiveText
                {
                    Text = "通知"
                },
                new AdaptiveText
                {
                    Text = "これは通知です"
                }
            }
                    }
                }
            };
            //通知を作成
            var notification = new ToastNotification(content.GetXml());
            //通知を送信
            ToastNotificationManager.CreateToastNotifier().Show(notification);
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

        /// <summary>
        /// UIスレッド外で_TextDataに書き込みを行う場合、この関数を使用
        /// </summary>
        /// <param name="text"></param>
        private async void stringAdd_TextDataDispatcher(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _textData.Text += text;
            });

        }


        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            String tmp = string.Copy(this._textData.Text);
            int counter = 0;
            if (this.bluetoothWatcher.DeviceInfoSerchedServer != null)
            {
                this.bluetoothConnector = new BluetoothConnector(this.bluetoothWatcher.DeviceInfoSerchedServer);

                this.bluetoothConnector.NotifyReceiveCharacteristic += this.registeredCharacteristicNotify;
                _ = Task.Run(async () =>
                  {
                      counter = MAX_RETRY_CONNECT;
                      while(counter>0)
                      {
                          var task = await Task.Run(this.bluetoothConnector.Connect);
                          string tmpStr;
                          if (task)
                          {
                              this.registeredCharacteristic = this.bluetoothConnector.RegisteredCharacteristic;

                              //Notify受信イベントハンドラの登録とデバイスから ValueChanged イベントを受信できるようにします。
                              if (this.registeredCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                              {
                                  await this.registeredCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                              }

                              stringAdd_TextDataDispatcher("\n" + "取得Service名：");
                              //_textData.Text += "\n" + "取得Service名：";
                              foreach (var service in this.bluetoothConnector.Services)
                              {
                                  stringAdd_TextDataDispatcher("\n" + string.Copy(service.ServiceGattNativeServiceUuidString));
                                  //_textData.Text += "\n" + string.Copy(service.ServiceGattNativeServiceUuidString);
                              }
                              stringAdd_TextDataDispatcher("\n" + "取得Characteristic名：");
                              //_textData.Text += "\n" + "取得Characteristic名：";
                              foreach (var name in this.bluetoothConnector.CharacteristicNames)
                              {
                                  stringAdd_TextDataDispatcher("\n" + string.Copy(name));
                                  //_textData.Text += "\n" + string.Copy(name);

                              }
                              break;
                          }
                          else
                          {
                              stringAdd_TextDataDispatcher("・");
                          }
                          counter--;
                      }
                      if (counter == 0)
                      {
                          stringAdd_TextDataDispatcher("\n" + "接続できなかった");
                      }

                  });

                /*
                var task = Task.Run(this.bluetoothConnector.Connect);
                if (task.Result)
                {
                    this.registeredCharacteristic = this.bluetoothConnector.RegisteredCharacteristic;

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
                    _textData.Text += "\n" + "接続できない";
                }
                */
                _textData.Text += "\n" + "処理待ち";
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
        private async void registeredCharacteristicNotify(object sender, NotifyReceiveCharacteristicEventArgs e)
        {
            try
            {
                //UIスレッド側のオブジェクトに対して書き込むため、UIスレッド側のメソッドを作り実行
                //参考：https://m-miya.blog.jp/archives/1063899401.html
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _textData.Text += "\n" + string.Copy(e.Message);
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
        }
    }

}
