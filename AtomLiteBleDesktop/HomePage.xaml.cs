using AtomLiteBleDesktop.Bluetooth;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static AtomLiteBleDesktop.Bluetooth.BluetoothAccesser;
using static AtomLiteBleDesktop.BluetoothLEDevice;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        /// <summary>
        /// Device状態
        /// </summary>
        enum typeDeviceStatus
        {
            RxData,
            TxData,
            Connected,
            NotFind,
            Find
        }

        private const string SERVICE_UUID_CALL_UNDER_LEVEL= "e72609f6-2bcb-4fb0-824a-5276ec9e355d";
        private const string CHARACTERISTIC_UUID_CALL_UNDER_LEVEL = "cca99442-dab6-4f69-8bc2-685e2412d178";

        private Servers resourceGridServer;
        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();



        public HomePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            var bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];
            var task = Task.Run(() =>
            {
                while (bluetoothAccesser.NumberDevice != bluetoothAccesser.Devices.Count) ;//取得要求Serverがすべて処理されるまで待ち
                foreach (var device in bluetoothAccesser.Devices)
                {
                    if (device.IsFindDevice)
                    {
                        try
                        {
                            listBoxAdd_TextDataDispatcher(device.Name, typeDeviceStatus.Find, null, null, null, null);
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine(err.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            listBoxAdd_TextDataDispatcher(device.Name, typeDeviceStatus.NotFind, null, null, null, null);
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine(err.Message);
                        }
                    }
                    if (device.BluetoothConnector != null)
                    {
                        if (device.BluetoothConnector.IsConnectService)
                        {
                            AccesserStatusChange(NotifyBluetoothAccesserEventArgs.Status.Connected, null);
                        }
                        device.NotifyConnectingServer += NotifyConnectServerBluetoothEventHandler;
                        device.NotifyReceiveCharacteristic += NotifyBluetoothLEDeviceCharacteristicEvent;
                    }
                }
            });
            
            //bluetoothAccesser.NotifyConnectingServer += NotifyConnectServerBluetoothEventHandler;
            bluetoothAccesser.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
            this.resourceGridServer = (Servers)this.HomeGrid.Resources["servers"];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AccesserStatusChange(BluetoothAccesser.NotifyBluetoothAccesserEventArgs.Status status, BluetoothLEDevice sender)
        {
            switch (status)
            {
                case NotifyBluetoothAccesserEventArgs.Status.Connected:
                    stringHogehogeData_TextDataDispatcher("Connect!!");
                    stringAdd_TextDataDispatcher("\n" + "取得Service名：");//接続した場合のUIへのBindをおこなう
                    if (sender != null)
                    {
                        listBoxAdd_TextDataDispatcher(sender.Name, typeDeviceStatus.Connected, null, null, null, null);
                        foreach (var service in sender.BluetoothConnector.Services)
                        {
                            stringAdd_TextDataDispatcher("\n" + string.Copy(service.ServiceGattNativeServiceUuidString));
                            if (service.CharacteristicNames != null)
                            {
                                foreach (var name in service.CharacteristicNames)
                                {
                                    stringAdd_TextDataDispatcher("\n" + string.Copy(name));

                                }
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

        /// <summary>
        /// SeverConnectイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyConnectServerBluetoothEventHandler(object sender, NotifyBluetoothAccesserEventArgs e)
        {
#warning BLEがDisConnectになった場合はこのイベントハンドラには来ないのを修正する
            if (sender is BluetoothLEDevice)
            {
                AccesserStatusChange(e.State,sender as BluetoothLEDevice);
            }
        }

        void NotifyBluetoothLEDeviceCharacteristicEvent(object sender, NotifyReceiveLEDeviceCharacteristicEventArgs e)
        {
            if (e.Service.Service.Uuid.ToString().Equals(SERVICE_UUID_CALL_UNDER_LEVEL))
            {
                if (e.Characteristic.Characteristic.Uuid.ToString().Equals(CHARACTERISTIC_UUID_CALL_UNDER_LEVEL))
                {
                    listBoxAdd_TextDataDispatcher((sender as BluetoothLEDevice).Name, typeDeviceStatus.RxData, e.Service.Service.Uuid.ToString(), e.Characteristic.Characteristic.Uuid.ToString(),"Rx", e.Message);
                }
            }
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
                });
                NotificationToast();
            }
            catch (Exception err)
            {
                throw err;
            }
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
        /// TextDataにUIスレッド外で書き込みを行う
        /// </summary>
        /// <param name="text"></param>
        private async void listBoxAdd_TextDataDispatcher(string deviceName, typeDeviceStatus deviceStatus,string serverName,string characteristicName, string characteristicStatus, string CharacteristicData)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                bool hasItem = false;
                foreach(var item in resourceGridServer)
                {
                    if (item.DeviceName == deviceName)
                    {
                        switch (deviceStatus)
                        {
                            case typeDeviceStatus.Connected:
                                item.RxStatus = "Connected";
                                item.PanelFrontColor = new SolidColorBrush( Colors.LimeGreen);
                                break;
                            case typeDeviceStatus.Find:
                                item.RxStatus = "Find!";
                                item.PanelFrontColor = new SolidColorBrush(Colors.AliceBlue);
                                break;
                            case typeDeviceStatus.NotFind:
                                item.RxStatus = "NotFind!";
                                item.PanelFrontColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));
                                break;
                            case typeDeviceStatus.RxData:
                                item.ServerName= serverName;
                                item.RxStatus = "Connected";
                                item.CharacteristicName= characteristicName;
                                item.CharacteristicStatus= characteristicStatus;
                                item.CharacteristicData= CharacteristicData;
                                item.PanelFrontColor = new SolidColorBrush(Colors.LimeGreen);
                                break;

                        }
                        hasItem = true;
                        break;
                    }
                }
                if (!hasItem)
                {
                    resourceGridServer.Add(new Server(deviceName, "NotFind!"));
                }
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

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_ASAP_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class Server : INotifyPropertyChanged
    {

        private Brush panelBackColor = new SolidColorBrush(Color.FromArgb(0xff, 0xa0, 0x90, 0x90));//#FFA09090
        public Brush PanelBackColor
        {
            get { return this.panelBackColor; }
            set { 
                this.panelBackColor = value;
                NotifyPropertyChanged("PanelBackColor");
            }
        }

        private Brush panelFrontColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));// #FFD19F9F;
        public Brush PanelFrontColor
        {
            get { return this.panelFrontColor; }
            set { 
                this.panelFrontColor = value;
                NotifyPropertyChanged("PanelFrontColor");
            }
        }

        private String deviceName;
        public String DeviceName 
        {
            get { return this.deviceName; }
            set 
            { 
                this.deviceName = value;
                NotifyPropertyChanged("DeviceName");
            }
        }

        private String serverName;
        public String ServerName
        {
            get { return this.serverName; }
            set
            {
                this.serverName = value;
                NotifyPropertyChanged("ServerName");
            }
        }

        private String rxStatus;
        public String RxStatus
        {
            get { return this.rxStatus; }
            set { 
                this.rxStatus = value;
                NotifyPropertyChanged("RxStatus");
            }
        }

        private String characteristicName;
        public String CharacteristicName
        {
            get { return this.characteristicName; }
            set
            {
                this.characteristicName = value;
                NotifyPropertyChanged("CharacteristicName");
            }
        }

        private String characteristicStatus;
        public String CharacteristicStatus
        {
            get { return this.characteristicStatus; }
            set
            {
                this.characteristicStatus = value;
                NotifyPropertyChanged("CharacteristicStatus");
            }
        }

        private String characteristicData;
        public String CharacteristicData
        {
            get { return this.characteristicData; }
            set
            {
                this.characteristicData = value;
                NotifyPropertyChanged("CharacteristicData");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public Server(String deviceName, String rxStatus)
        {
            this.DeviceName = deviceName;
            this.RxStatus = rxStatus;
        }

    }
    public class Servers : ObservableCollection<Server>
    {
        public Servers()
        {
            //以下の実装については静的にxamlに結合されるため、表示テストの為に残す（実際に使用する場合はコメントアウトすること
            /*
            Add(new Server("Michael", "Connected")
            {
                ServerName = "test",
            });
            ;
            Add(new Server("Chris", "NotFind"));
            Add(new Server("Seo-yun", "Find!!"));
            Add(new Server("Guido", "COnnecting"));
            */
        }

    }
}
