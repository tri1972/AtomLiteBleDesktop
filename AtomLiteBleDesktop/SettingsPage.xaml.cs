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
using Windows.UI;
using Windows.System;
using static AtomLiteBleDesktop.Bluetooth.BluetoothAccesser;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        //TODO: ボタン類はすべて削除
        //TODO: デバイスにより着信時の音が変わるようにする
        //TODO: 現在DBに登録されているデバイス名を表示できるようにする
        //TODO: DBにデバイスを登録できるようにする

        private const string PIRSERVER = "ESP32PIRTRI";
        private const int MAX_RETRY_CONNECT = 5;

        //static BluetoothLEAdvertisementWatcher watcher;
        private GattCharacteristic registeredCharacteristic=null;

        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();

        private BluetoothAccesser bluetoothAccesser;
        private BluetoothWatcher bluetoothWatcher;
        //private BluetoothConnector bluetoothConnector;
        
        /// <summary>
        /// ページロードイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];
            this.bluetoothWatcher = BluetoothWatcher.GetInstance();
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = new
            {
                textData = _textData,
            };
        }

        private void Seach_button_Click(object sender, RoutedEventArgs e)
        {
            /*
            var task = await this.bluetoothAccesser.Search(PIRSERVER);
            if (task != null)
            {
                this._textData.Text = "取得サーバー名:\n" + task;
            }
            else
            {
                var dialog = new MessageDialog("接続指定サーバをSearchしたが取得できませんでした", "エラー");
                _ = dialog.ShowAsync();
            }

            NotificationToast();
            */
        }

        private static void NotificationToast()
        {
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

        private void ScanStop_Click(object sender, RoutedEventArgs e)
        {
            this.bluetoothWatcher.StopBleDeviceWatcher();

            this._textData.Text = "Searched" + this.bluetoothWatcher.DeviceInfoSerchedServer.Name + " : " + this.bluetoothWatcher.DeviceInfoSerchedServer.Id;
        }
        /*
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
        */
        /// <summary>
        /// TextDataにUIスレッド外で書き込みを行う
        /// </summary>
        /// <param name="text"></param>
        private async void stringAdd_TextDataDispatcher(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _textData.Text += text;
            });

        }
        /// <summary>
        /// HogehogeDataのTextプロパティにUIスレッド外から書き込みを行う
        /// </summary>
        /// <param name="text"></param>
        private async void stringHogehogeData_TextDataDispatcher(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var hogehogeData = Application.Current.Resources["HomePageDataInstance"] as HomePagePropertyChanged;
                hogehogeData.StatusText = text;
                hogehogeData.StatusTextBackground = new SolidColorBrush(Colors.Red);
            });
        }

        /// <summary>
        /// SeverConnectイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyConnectServerBluetoothEventHandler(object sender, NotifyBluetoothAccesserEventArgs e)
        {
            if (sender is BluetoothAccesser)
            {
                switch (e.State)
                {
                    case NotifyBluetoothAccesserEventArgs.Status.Connected:
                        foreach (var device in (sender as BluetoothAccesser).Devices)
                        {
                            foreach (var service in device.Services)
                            {
                                stringAdd_TextDataDispatcher("\n" + string.Copy(service.ServiceGattNativeServiceUuidString));

                                foreach (var name in service.CharacteristicNames)
                                {
                                    stringAdd_TextDataDispatcher("\n" + string.Copy(name));

                                }
                            }
                            stringAdd_TextDataDispatcher("\n" + "取得Characteristic名：");
                        }
                        break;
                    case NotifyBluetoothAccesserEventArgs.Status.Connecting:
                        stringAdd_TextDataDispatcher("・");
                        break;
                    case NotifyBluetoothAccesserEventArgs.Status.Abort:
                        stringAdd_TextDataDispatcher("\n" + "接続できなかった");
                        break;
                    case NotifyBluetoothAccesserEventArgs.Status.NotFound:
                        stringAdd_TextDataDispatcher("\n" + "接続先が未探索状態です");
                        break;
                }
            }
        }

        private void Connect_button_Click(object sender, RoutedEventArgs e)
        {
            this.bluetoothAccesser.NotifyConnectingServer += NotifyConnectServerBluetoothEventHandler;
            this.bluetoothAccesser.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
            //this.bluetoothAccesser.Connect();
        }

        /// <summary>
        /// BleServerからのNotify受信イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private async void registeredCharacteristicNotify(object sender, NotifyBluetoothAccesserEventArgs e)
        {
            try
            {
                //UIスレッド側のオブジェクトに対して書き込むため、UIスレッド側のメソッドを作り実行
                //参考：https://m-miya.blog.jp/archives/1063899401.html
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _textData.Text += "\n" + string.Copy(e.Message);
                    stringHogehogeData_TextDataDispatcher(e.Message);
                    var sh = this.scrollViewSettings.ScrollableHeight;
                    this.scrollViewSettings.ChangeView(null, sh, null);
                    //this.scrollViewSettings.ScrollToVerticalOffset(sh);
                });
                NotificationToast();
            }
            catch(Exception err)
            {
                throw err;
            }
        }

        private void readCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            if (this.registeredCharacteristic != null)
            {
                var receiver = new BluetoothReceiver(this.registeredCharacteristic);

                var task = Task.Run(receiver.ReadCharacteristic);

                this._textData.Text += "\n" + task.Result;

                stringHogehogeData_TextDataDispatcher(task.Result);
            }
            else
            {
                var dialog = new MessageDialog("Connectされていません(Characteristicが取得できませんでした)", "エラー");
                _ = dialog.ShowAsync();

            }
        }

        private void ScanStart_button_Click(object sender, RoutedEventArgs e)
        {
            /*
            var task = await this.bluetoothAccesser.StartScanning();
            if (task != null)
            {
                foreach(var data in task)
                {
                    this._textData.Text = "取得サーバー名:\n" + data.Name;

                }
            }
            else
            {
                var dialog = new MessageDialog("接続指定サーバをSearchしたが取得できませんでした", "エラー");
                _ = dialog.ShowAsync();
            }
            */
        }

        /// <summary>
        /// ScanStopボタンイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScanStop_button_Click(object sender, RoutedEventArgs e)
        {
            /*
            this.bluetoothAccesser.StopScanning();
            */
        }
    }

}
