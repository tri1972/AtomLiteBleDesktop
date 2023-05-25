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
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static AtomLiteBleDesktop.Bluetooth.BluetoothAccesser;
using static AtomLiteBleDesktop.Bluetooth.BluetoothCharacteristic;
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
            DisConnected,
            NotFind,
            Find
        }

        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string SERVICE_UUID_CALL_UNDER_LEVEL= "e72609f6-2bcb-4fb0-824a-5276ec9e355d";
        private const string CHARACTERISTIC_UUID_CALL_UNDER_LEVEL = "cca99442-dab6-4f69-8bc2-685e2412d178";

        private Servers resourceGridServer;
        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();

        private volatile bool isCancelRepeatReceivingBlink;

        public HomePage()
        {
            this.InitializeComponent();
            batchUpdateBadgeGlyphClear();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ExtendedExecutionHelper.GetInstance().BeginExtendedExecution();
        
            var bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];
            var task = Task.Run(() =>
            {
                while (bluetoothAccesser.NumberDevice != bluetoothAccesser.Devices.Count) ;//取得要求Serverがすべて処理されるまで待ち
                foreach (var device in bluetoothAccesser.Devices)
                {
                    if (device.Status== TypeStatus.Finded)
                    //if (device.IsFindDevice)
                    {
                        try
                        {
                            listBoxAdd_TextDataDispatcher(device.Name, typeDeviceStatus.Find, null, null, null, null, BluetoothCharacteristic.TypeStateReseive.Received,0);
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine(err.Message);
                        }
                    }
                    if (device.Status == TypeStatus.Coonected)
                    {
                        ;
                    }
                    else
                    {
                        try
                        {
                            listBoxAdd_TextDataDispatcher(device.Name, typeDeviceStatus.NotFind, null, null, null, null, BluetoothCharacteristic.TypeStateReseive.Received,0);
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine(err.Message);
                        }
                    }
                    if (device != null)
                    {
                        if (device.Status== TypeStatus.Coonected)
                        {
                            AccesserStatusChange(NotifyBluetoothAccesserEventArgs.Status.Connected, null);
                        }
                        device.NotifyConnectingServer += NotifyConnectServerBluetoothEventHandler;
                        device.NotifyReceiveCharacteristic += NotifyBluetoothLEDeviceCharacteristicEvent;
                    }
                }
            });
            
            //bluetoothAccesser.NotifyConnectingServer += NotifyConnectServerBluetoothEventHandler;
            //bluetoothAccesser.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
            this.resourceGridServer = (Servers)this.HomeGrid.Resources["servers"];

            this.isCancelRepeatReceivingBlink = false;
        }


        private  BluetoothLEDevice getDevice(string deviceName)
        {
            var bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];

            foreach (var device in bluetoothAccesser.Devices)
            {
                if(device.Name == deviceName)
                {
                    return device;
                }
            }
            return null;
        }
        private async void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if(sender is Button)
            {
                var senderButton = sender as Button;
                if (senderButton.IsPressed)
                {
                    if (senderButton.DataContext is Server)
                    {
                        TypeStateWaitingSend sendData;
                        switch (senderButton.Name)
                        {
                            case "Button_ASAP":
                                sendData = TypeStateWaitingSend.ASAP;
                                break;
                            case "Button_Wait":
                                sendData = TypeStateWaitingSend.WAIT;
                                break;
                            case "Button_Wrong":
                                sendData = TypeStateWaitingSend.WRONG;
                                break;
                            case "Button_Cancel":
                                sendData = TypeStateWaitingSend.Cancel;
                                break;
                            default:
                                sendData = TypeStateWaitingSend.None;
                                break;
                        }
                        var serverListview = (sender as Button).DataContext as Server;
                        this.isCancelRepeatReceivingBlink = true;//送信ボタンが押されたら点滅はキャンセルされる
                        var mDevice = getDevice(serverListview.DeviceName);
                        if (mDevice.Status== TypeStatus.Coonected)
                        {
                            mDevice.SendData(SERVICE_UUID_CALL_UNDER_LEVEL, CHARACTERISTIC_UUID_CALL_UNDER_LEVEL, sendData);
                        }
                        else
                        {
                            MessageDialog md = new MessageDialog("デバイスに接続されていません");
                            await md.ShowAsync();
                        }
                        batchUpdateBadgeGlyphClear();
                    }
                }
            }
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
                        listBoxAdd_TextDataDispatcher(sender.Name, typeDeviceStatus.Connected, null, null, null, null,BluetoothCharacteristic.TypeStateReseive.Received,0);
                        foreach (var service in sender.Services)
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
                case NotifyBluetoothAccesserEventArgs.Status.Disconnected:
                    if (sender != null)
                    {
                        listBoxAdd_TextDataDispatcher(sender.Name, typeDeviceStatus.DisConnected, null, null, null, null, BluetoothCharacteristic.TypeStateReseive.Received,0);
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
                    listBoxAdd_TextDataDispatcher((sender as BluetoothLEDevice).Name, typeDeviceStatus.RxData, e.Service.Service.Uuid.ToString(), e.Characteristic.Characteristic.Uuid.ToString(), "Rx", e.Message, e.State,e.Characteristic.NumberCounteRx);
                }
            }
        }
        private static void NotificationToast(string source)
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
                    Text = source
                },
                new AdaptiveText
                {
                    Text = "受信しました"
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
        /// device名よりList中のItemを検索します
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns></returns>
        private Server getListItem(string deviceName)
        {
            foreach (var item in resourceGridServer)
            {
                if (item.DeviceName == deviceName)
                {
                    return item;
                }
            }
            return null;
        }


        static BluetoothCharacteristic.TypeStateReseive beforeRxdata = TypeStateReseive.None;
        static bool blink = true;

        private async void blinkListItem(Server item)
        {
            this.isCancelRepeatReceivingBlink = false;
            await Task.Run(async () =>
            {//ここで点滅プログラムをかきたい・・・
                int counter = 10;
                while (counter > 0)
                {
                    if (this.isCancelRepeatReceivingBlink)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            item.PanelListColor = new SolidColorBrush(Colors.PaleGreen);
                        });
                        break;
                    }
                    Thread.Sleep(1000);
                    if (blink)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            item.PanelListColor = new SolidColorBrush(Colors.OrangeRed);
                        });
                        blink = false;
                    }
                    else
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            item.PanelListColor = new SolidColorBrush(Colors.PaleGreen);
                        });
                        blink = true;
                    }
                    counter--;
                }
            });

        }

        /// <summary>
        /// TextDataにUIスレッド外で書き込みを行う
        /// </summary>
        /// <param name="text"></param>
        private async void listBoxAdd_TextDataDispatcher(string deviceName, typeDeviceStatus deviceStatus,string serverName,string characteristicName, string characteristicStatus, string CharacteristicData,BluetoothCharacteristic.TypeStateReseive rxStatus,int? numberRx)
        {
            try
            {
                var item = getListItem(deviceName);
                
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,  async () =>
                {
                    //bool hasItem = false;
                    if (item != null)
                    {
                        switch (deviceStatus)
                        {
                            case typeDeviceStatus.Connected:
                                item.RxStatus = "Connected";
                                item.PanelFrontColor = new SolidColorBrush(Colors.LimeGreen);
                                break;
                            case typeDeviceStatus.Find:
                                item.RxStatus = "Find!";
                                item.PanelFrontColor = new SolidColorBrush(Colors.AliceBlue);
                                break;
                            case typeDeviceStatus.NotFind:
                                item.RxStatus = "NotFind!";
                                item.PanelFrontColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));
                                break;
                            case typeDeviceStatus.DisConnected:
                                item.RxStatus = "Disconnected!";
                                item.PanelFrontColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));
                                break;
                            case typeDeviceStatus.RxData:
                                item.ServerName = serverName;
                                item.RxStatus = "Connected";
                                item.CharacteristicName = characteristicName;
                                item.CharacteristicStatus = characteristicStatus;
                                item.CharacteristicData = CharacteristicData;
                                item.PanelFrontColor = new SolidColorBrush(Colors.LimeGreen);
                                if (rxStatus == TypeStateReseive.StartReceiving)
                                {
                                    if (rxStatus != beforeRxdata)
                                    {
                                        NotificationToast(item.DeviceName);

                                        batchUpdateBadgeNum(numberRx ?? 0);
                                        //batchUpdateBadgeGlyphAlert();
                                        
                                        
                                        beforeRxdata = rxStatus;
                                    }
                                }
                                else if (rxStatus == TypeStateReseive.RepeatReceiving)
                                {
                                    if (rxStatus != beforeRxdata)
                                    {
                                        beforeRxdata = rxStatus;
                                        blinkListItem(item);
                                    }
                                }
                                else if (rxStatus == TypeStateReseive.Received)
                                {
                                    item.PanelListColor = new SolidColorBrush(Colors.PaleGreen);
                                }
                                else
                                {

                                }
                                break;
                        }
                    }
                    else
                    {
                        resourceGridServer.Add(new Server(deviceName, "NotFind!"));

                    }
                });
            }
            catch(Exception err)
            {
                Debug.WriteLine(err.Message);
            }

        }

        /// <summary>
        /// Batchをクリアします
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void batchUpdateBadgeGlyphClear()
        {
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
        }
        private void batchUpdateBadgeNum(int num)
        {
            batchUpdateBadgeGlyph(num.ToString());
        }

        /// <summary>
        /// AleartGlyphBatchを表示します
        /// </summary>
        private void batchUpdateBadgeGlyphAlert()
        {
            batchUpdateBadgeGlyph("alert");
        }

        /// <summary>
        /// GlyphBatchを表示します
        /// </summary>
        /// <param name="badgeGlyphValue"></param>
        private void batchUpdateBadgeGlyph(string badgeGlyphValue)
        {
            /*
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(GetContent(10));
            BadgeNotification badge = new BadgeNotification(xmlDoc);
            */
            
            // Get the blank badge XML payload for a badge glyph
            XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeGlyph);
            // Set the value of the badge in the XML to our glyph value
            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

            badgeElement.SetAttribute("value", "busy");//赤〇警告で表示する場合
           // badgeElement.SetAttribute("value", badgeGlyphValue );//着信数を表示する場合
            
            // Create the badge notification
            BadgeNotification badge = new BadgeNotification(badgeXml);


            // Create the badge updater for the application
            BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();

            // And update the badge
            badgeUpdater.Update(badge);
            
            /*
            string badgeContent = "10";
            string badgeTextColor = "#00b2f0";
            // バッジのXMLテンプレートを作成
            string badgeXmlString = $@"
        <badge value=""{badgeGlyphValue}"" 
               xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
               xmlns:badge=""http://schemas.microsoft.com/windows/notifications/1.0"">
            <badge:BadgeContent 
               BadgeForeground='#{badgeTextColor}'/>
        </badge>";
            // XML文字列をパースしてBadgeNotificationオブジェクトを作成
            XmlDocument badgeXml = new XmlDocument();
            badgeXml.LoadXml(badgeXmlString);
            BadgeNotification badge = new BadgeNotification(badgeXml);

            // Badgeを更新
            BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            badgeUpdater.Update(badge);
            */
        }


        /// <summary>
        /// Retrieves the notification Xml content as a WinRT Xml document.
        /// </summary>
        /// <returns>The notification Xml content as a WinRT Xml document.</returns>
        public XmlDocument GetXml(int number)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(GetContent(number));
            return xml;
        }

        public const int NOTIFICATION_CONTENT_VERSION = 1;
        public string GetContent(int m_Number)
        {
            return String.Format("<badge version='{0}' value='{1}' Style='{ ThemeResource AttentionIconInfoBadgeStyle}'/>", NOTIFICATION_CONTENT_VERSION, m_Number);
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
        public  Brush PanelFrontColor
        {
            get { return this.panelFrontColor; }
            set
            {
                this.panelFrontColor = value;
                NotifyPropertyChanged("PanelFrontColor");
            }
        }

        private Brush panelListColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));// #FFD19F9F;
        public Brush PanelListColor
        {
            get { return this.panelListColor; }
            set { 
                this.panelListColor = value;
                NotifyPropertyChanged("PanelListColor");
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
