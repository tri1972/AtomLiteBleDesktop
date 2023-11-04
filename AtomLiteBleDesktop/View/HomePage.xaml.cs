using AtomLiteBleDesktop.Bluetooth;
using AtomLiteBleDesktop.Database;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System.Profile;
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
        public enum typeDeviceStatus
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

        private Servers resourceGridServer;
        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();

        private volatile bool isCancelRepeatReceivingBlink;
        private BluetoothAccesser bluetoothAccesser;

        private bool isRegistedListItem = false;

        private bool isInitialized = false;

        public HomePage()
        {
            this.InitializeComponent();

            batchUpdateBadgeGlyphClear();

            //initializePage();

        }

        private async void initializePage()
        {
            this.bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];
            this.resourceGridServer = (Servers)this.HomeGrid.Resources["servers"];

            var taskResult = await Task.Run<bool>(() =>
            {
                while (bluetoothAccesser.NumberDevice != bluetoothAccesser.Devices.Count) ;//取得要求Serverがすべて処理されるまで待ち
                foreach (var device in bluetoothAccesser.Devices)
                {
                    if (device != null)
                    {
                        if (device.Status == TypeStatus.Finded)
                        //if (device.IsFindDevice)
                        {
                            try
                            {
                                listBoxInitialize_TextDataDispatcher(device.Name, typeDeviceStatus.Find);
                                //listBoxAdd_TextDataDispatcher(device.Name, typeDeviceStatus.Find, null, null, null, null, BluetoothCharacteristic.TypeStateReseive.Received, 0);
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine(err.Message);
                            }
                        }
                        else if (device.Status == TypeStatus.Coonected)
                        {
                            AccesserStatusChange(NotifyBluetoothAccesserEventArgs.Status.Connected, null);
                        }
                        else
                        {
                            try
                            {
                                listBoxInitialize_TextDataDispatcher(device.Name, typeDeviceStatus.NotFind);
                                //listBoxAdd_TextDataDispatcher(device.Name, typeDeviceStatus.NotFind, null, null, null, null, BluetoothCharacteristic.TypeStateReseive.Received, 0);
                            }
                            catch (Exception err)
                            {
                                Debug.WriteLine(err.Message);
                            }
                        }
                        device.NotifyConnectingServer += NotifyConnectServerBluetoothEventHandler;
                        device.NotifyReceiveCharacteristic += NotifyBluetoothLEDeviceCharacteristicEvent;
                    }
                }
                return true;
            });
            if (taskResult)
            {
                isRegistedListItem = true;
            }

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ExtendedExecutionHelper.GetInstance().BeginExtendedExecution();

            if (!this.isInitialized)
            {
                initializePage();
                this.isInitialized = true;
            }

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

        private async void sendDataToServer(object sender, TypeStateWaitingSend sendData)
        {//TODO:データ送信用の関数として送信データをstringとするものをつくる？
            var serverListview = (sender as FrameworkElement).DataContext as Server;
            var mDevice = getDevice(serverListview.DeviceName);
            if (mDevice.Status == TypeStatus.Coonected)
            {
                this.isCancelRepeatReceivingBlink = true;//送信ボタンが押されたら点滅はキャンセルされる
                var server = BleContext.GetServerPost(serverListview.DeviceName);
                if (server != null)
                {
                    mDevice.SendData(server.ServiceUUID, server.CharacteristicUUID, sendData );
                }
                else
                {
                    MessageDialog md = new MessageDialog("該当のService、Characteristicが登録されていません");
                    await md.ShowAsync();

                }
                var listItem = getListItem(serverListview.DeviceName);
                listItem.NumberCall = "";
                listItem.IconCallNumber = "";
            }
            else
            {
                MessageDialog md = new MessageDialog("デバイスに接続されていません");
                await md.ShowAsync();
            }
        }


        private async void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                var senderButton = sender as Button;
                if (senderButton.IsPressed)
                {
                    if (senderButton.DataContext is Server)
                    {
                        TypeStateWaitingSend sendData;

                        var serverListview = (sender as Button).DataContext as Server;
                        var mDevice = getDevice(serverListview.DeviceName);
                        switch (senderButton.Name)
                        {//TODO:このボタンによる送信ランプ点灯＋状況送信をボタンなしでトグルとなっている箇所を押すことで切り替えられるようにする
                            case "Button_ASAP":
                                sendData = TypeStateWaitingSend.ASAP;
                                serverListview.StringGreenLamp = "●";
                                serverListview.StringYellowLamp = "〇";
                                serverListview.StringRedLamp = "〇";
                                serverListview.StringBlueLamp = "〇";
                                break;
                            case "Button_Wait":
                                sendData = TypeStateWaitingSend.WAIT;
                                serverListview.StringGreenLamp = "〇";
                                serverListview.StringYellowLamp = "●";
                                serverListview.StringRedLamp = "〇";
                                serverListview.StringBlueLamp = "〇";
                                break;
                            case "Button_Wrong":
                                sendData = TypeStateWaitingSend.WRONG;
                                serverListview.StringGreenLamp = "〇";
                                serverListview.StringYellowLamp = "〇";
                                serverListview.StringRedLamp = "●";
                                serverListview.StringBlueLamp = "〇";
                                break;
                            case "Button_Cancel":
                                sendData = TypeStateWaitingSend.Cancel;
                                serverListview.StringGreenLamp = "〇";
                                serverListview.StringYellowLamp = "〇";
                                serverListview.StringRedLamp = "〇";
                                serverListview.StringBlueLamp = "●";
                                break;
                            default:
                                sendData = TypeStateWaitingSend.None;
                                break;
                        }
                        sendDataToServer(sender, sendData);
                        batchUpdateBadgeGlyphClear();
                    }
                }
            }
        }

        /*
        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }
        */

        private void AccesserStatusChange(BluetoothAccesser.NotifyBluetoothAccesserEventArgs.Status status, BluetoothLEDevice sender)
        {
            switch (status)
            {
                case NotifyBluetoothAccesserEventArgs.Status.Connected:
                    stringHogehogeData_TextDataDispatcher("Connect!!");
                    stringAdd_TextDataDispatcher("\n" + "取得Service名：");//接続した場合のUIへのBindをおこなう
                    if (sender != null)
                    {
                        listBoxAdd_TextDataDispatcher(sender.Name, typeDeviceStatus.Connected, null, null, null, null,BluetoothCharacteristic.TypeStateReseive.Received,0);//ここでデバイス名のアイテムが登録されていなければ追加、登録されていればデバイスのステータスを変更
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
                AccesserStatusChange(e.State, sender as BluetoothLEDevice);//①ここでsenderのデバイスがnullなら新規アイテムとして登録される.nullでなければListBoxのステータスを変更
                /*
                typeDeviceStatus deviceStatus;
                switch ((sender as BluetoothLEDevice).Status)
                {
                    case TypeStatus.Coonected:
                    case TypeStatus.Connecting:
                        deviceStatus = typeDeviceStatus.Connected;
                        break;
                    case TypeStatus.Disconnect:
                        deviceStatus = typeDeviceStatus.DisConnected;
                        break;
                    case TypeStatus.Finded:
                        deviceStatus = typeDeviceStatus.Find;
                        break;
                    case TypeStatus.NoFinded:
                        deviceStatus = typeDeviceStatus.NotFind;
                        break;
                    default:
                        deviceStatus = typeDeviceStatus.RxData;
                        break;
                }
                var item = getListItem((sender as BluetoothLEDevice).Name);//①で新規アイテムが登録される前にここに到達する（非同期のためそういう場合もあり）と、COnnectedなのにアイテムが見つからない
                listBoxChange_TextDataDispatcher(item, deviceStatus, null, null, null, null, BluetoothCharacteristic.TypeStateReseive.Received, 0);//ここでもitemuのステータスを変更しているのは二度手間
                */
            }
        }
        
        void NotifyBluetoothLEDeviceCharacteristicEvent(object sender, NotifyReceiveLEDeviceCharacteristicEventArgs e)
        {
            listBoxAdd_TextDataDispatcher((sender as BluetoothLEDevice).Name, typeDeviceStatus.RxData, e.Service.Service.Uuid.ToString(), e.Characteristic.Characteristic.Uuid.ToString(), "Rx", e.Message, e.State, e.Characteristic.NumberCounteRx);
            /*
            if (e.Service.Service.Uuid.ToString().Equals(SERVICE_UUID_CALL_UNDER_LEVEL))
            {
                if (e.Characteristic.Characteristic.Uuid.ToString().Equals(CHARACTERISTIC_UUID_CALL_UNDER_LEVEL))
                {
                    listBoxAdd_TextDataDispatcher((sender as BluetoothLEDevice).Name, typeDeviceStatus.RxData, e.Service.Service.Uuid.ToString(), e.Characteristic.Characteristic.Uuid.ToString(), "Rx", e.Message, e.State,e.Characteristic.NumberCounteRx);
                }
            }
            */
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
            while(isRegistedListItem==false)
            {
                ;
            }
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
                            item.PanelListColor = (SolidColorBrush)this.Resources["ListNoticePanelBackground"];
                            //item.PanelListColor = new SolidColorBrush( Colors.PaleGreen);
                        });
                        break;
                    }
                    Thread.Sleep(1000);
                    if (blink)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            item.PanelListColor = (SolidColorBrush)this.Resources["ListWarningPanelBackground"];
                            //item.PanelListColor = new SolidColorBrush(Colors.OrangeRed);
                        });
                        blink = false;
                    }
                    else
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            item.PanelListColor = (SolidColorBrush)this.Resources["ListNoticePanelBackground"];
                            //item.PanelListColor = new SolidColorBrush(Colors.PaleGreen);
                        });
                        blink = true;
                    }
                    counter--;
                }
            });

        }


        private async void listBoxChange_TextDataDispatcher
            (
            Server item,
            typeDeviceStatus deviceStatus,
            string serverName,
            string characteristicName,
            string characteristicStatus,
            string CharacteristicData,
            BluetoothCharacteristic.TypeStateReseive rxStatus,
            int? numberRx)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {

                item.Status = deviceStatus;
                switch (deviceStatus)
                {
                    case typeDeviceStatus.Connected:
                        item.IconStatus = "\xE702";
                        item.IconSignalBars = "\xE870";
                        //item.PanelFrontColor = new SolidColorBrush(Colors.LimeGreen);
                        break;
                    case typeDeviceStatus.Find:
                        //item.PanelFrontColor = new SolidColorBrush(Colors.AliceBlue);
                        break;
                    case typeDeviceStatus.NotFind:
                        //item.PanelFrontColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));
                        break;
                    case typeDeviceStatus.DisConnected:
                        item.IconStatus = "\xE871";
                        item.IconSignalBars = "\xE870";
                        //item.PanelFrontColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));
                        break;
                    case typeDeviceStatus.RxData:
                        item.ServerName = serverName;
                        item.CharacteristicName = characteristicName;
                        item.CharacteristicStatus = characteristicStatus;
                        item.CharacteristicData = CharacteristicData;
                        //item.PanelFrontColor = new SolidColorBrush(Colors.LimeGreen);
                        if (rxStatus == TypeStateReseive.StartReceiving)
                        {
                            if (rxStatus != beforeRxdata)
                            {
                                //NotificationToast(item.DeviceName);
                                
                                new ToastNotificationReceiveBle().Show( item.DeviceName);
                                
                                item.IconCallNumber = "\xE717";
                                item.NumberCall = numberRx.ToString();
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
                            item.PanelListColor = (SolidColorBrush)this.Resources["ListNoticePanelBackground"];
                            //item.PanelListColor = new SolidColorBrush(Colors.PaleGreen);
                        }
                        else
                        {

                        }
                        break;
                }
            });
         }

        private async void listBoxInitialize_TextDataDispatcher
            (string deviceName,
            typeDeviceStatus deviceStatus)
        {
            /*
            var taskCompletionSource =
                new TaskCompletionSource<bool>();
            */
            bool isRet;
            isRet = await Task.Run<bool>(() => {
                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    resourceGridServer.Add(new Server(deviceName, deviceStatus, Resources));
                    //taskCompletionSource.SetResult(true);
                });
                return true;
            });
            if (isRet)
            {
                ;
            }
            /*
            var retTask =taskCompletionSource.Task;
            if (retTask.Result)
            {
                ;
            }
            */
        }

        /// <summary>
        /// TextDataにUIスレッド外で書き込みを行う
        /// </summary>
        /// <param name="deviceName">デバイス名</param>
        /// <param name="deviceStatus">ステータス</param>
        /// <param name="serverName">サーバー名</param>
        /// <param name="characteristicName">characteristic名</param>
        /// <param name="characteristicStatus">characteristicステータス</param>
        /// <param name="CharacteristicData">characteristic</param>
        /// <param name="rxStatus"></param>
        /// <param name="numberRx"></param>
        private async void listBoxAdd_TextDataDispatcher
            (string deviceName, 
            typeDeviceStatus deviceStatus,
            string serverName,
            string characteristicName, 
            string characteristicStatus, 
            string CharacteristicData,
            BluetoothCharacteristic.TypeStateReseive rxStatus,
            int? numberRx)
        {
            try
            {
                var item = getListItem(deviceName);
                //bool hasItem = false;
                if (item != null)
                {
                    listBoxChange_TextDataDispatcher(item, deviceStatus, serverName, characteristicName, CharacteristicData, characteristicStatus, rxStatus, numberRx);
                }
                else
                {
                    listBoxInitialize_TextDataDispatcher(deviceName, deviceStatus);

                }
                //addRegistedServer(deviceName);
            }
            catch(Exception err)
            {
                Debug.WriteLine(err.Message);
            }

        }
        private int tmpTestCounter = 0;
        private List<string> registedServer = new List<string>();
        private volatile int currentQueHash = -999999;
        private volatile int beforeQueHash ;
        private volatile string currentServerName;
        Queue<Action<string,int>> que;
        private async void addRegistedServer(string serverName)
        {
            if (que == null)
            {
                que = new Queue<Action<string,int>>();

            }
            Debug.WriteLine("StartAdd Server:" + serverName);
            /*
            if (que.Count>0)
            {
                Debug.WriteLine("currentQueHash2:" + currentQueHash + "  que.Peek().GetHashCode():" + que.Peek().GetHashCode());
            }
            */
            await Task.Run(() =>
            {
                que.Enqueue((str,currentHashNum) =>
                {//実行すべき関数をキューに保存
                    try
                    {
                        currentServerName = str;
                        Debug.WriteLine("Start Server:" + str + ":" + currentHashNum);
                        /*
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            //resourceGridServer.Add(new Server(deviceName, deviceStatus, Resources));
                        });
                        */
                        //時間がかかる処理
                        Thread.Sleep(1000);


                        tmpTestCounter++;
                        
                        
                        
                        registedServer.Add(str);
                        Debug.WriteLine("End Server:" + str + ":" + currentHashNum);
                    }
                    catch (System.InvalidOperationException err)
                    {
                        throw err;
                    }
                });
                //var tmpFunc = que.Peek(); //キューの先頭の要素を取得かつキューから削除
                //Debug.WriteLine("currentQueHash:" + currentQueHash + "  que.Peek().GetHashCode():" + que.Peek().GetHashCode());
                if (que.Count > 1) {
                    while (currentQueHash != beforeQueHash)//キューの先頭と現状実行中のハッシュが一致していると、先に進まない
                    {
                        Debug.WriteLine("Wait Server:" + currentServerName + ":" + currentQueHash);
                    }
                }
                beforeQueHash = que.Peek().GetHashCode();
                var tmpFunc =que.Peek();
                tmpFunc(serverName, currentQueHash);//取り出した要素を実行
                tmpFunc = que.Dequeue(); //キューの先頭の要素を取得かつキューから削除(削除しないと次のキューがWhileを抜けてしまうので、削除するのはマスト)
                //var tmpFunc = que.Peek(); //キューの先頭の要素を取得かつキューから削除
                currentQueHash = tmpFunc.GetHashCode();//取り出した要素を現状実行中のハッシュとして保存
                if (que.Count() > 0) {

                    var tmpFunc2 = que.Peek();//現状の要素が終了したら、キューから次に実行する要素をとりだし（削除はしない）
                    currentQueHash = tmpFunc2.GetHashCode();//次に実行する要素のハッシュを現状実行中のハッシュとして保存→whileを抜けてくるようになる
                }
                else
                {

                    if (tmpTestCounter == 2)
                    {
                        ;
                    }


                    currentQueHash = -999999;
                }
            });
            /*
            //以下、時間がかかる処理にはisListBoxWaitがfalseになるまで入れない
            if (registedServer.FindAll((str) => {
                 if (str == serverName)
                {
                     return true;
                }
                return false;
            }).Count > 0)
            {
                ;
            }
            else
            {
                var func= que.Peek();
                func(serverName);
                func = que.Dequeue();
                currentQueHash = func.GetHashCode();
            }*/

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

        private void Send_Data_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                var sendData=(sender as TextBox).Text;
            }
        }
    }
    public class Server : INotifyPropertyChanged
    {
        private ResourceDictionary resource;


        private Brush panelListColor;
        //private Brush panelListColor = new SolidColorBrush(Color.FromArgb(0xff, 0xd1, 0x9f, 0x9f));// #FFD19F9F;
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

        private String deviceType;
        public String DeviceType
        {
            get { return this.deviceType; }
            set
            {
                this.deviceType = value;
                //NotifyPropertyChanged("DeviceType");//現状特に通知すべきUIはない
            }
        }

        private String iconStatus;
        public String IconStatus
        {
            get { return this.iconStatus; }
            set
            {
                this.iconStatus = value;
                NotifyPropertyChanged("IconStatus");
            }
        }
        private String iconSignalBars;
        public String IconSignalBars
        {
            get { return this.iconSignalBars; }
            set
            {
                this.iconSignalBars = value;
                NotifyPropertyChanged("IconSignalBars");
            }
        }

        private String iconCallNumber;
        public String IconCallNumber
        {
            get { return this.iconCallNumber; }
            set
            {
                this.iconCallNumber = value;
                NotifyPropertyChanged("IconCallNumber");
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
            set
            {
                this.rxStatus = value;
                NotifyPropertyChanged("RxStatus");
            }
        }

        private HomePage.typeDeviceStatus status;
        public HomePage.typeDeviceStatus Status
        {
            get { return this.status; }
            set {
                switch (value)
                {
                    case HomePage.typeDeviceStatus.Connected:
                        this.RxStatus = "Connected";
                        break;
                    case HomePage.typeDeviceStatus.Find:
                        this.RxStatus = "Find!";
                        break;
                    case HomePage.typeDeviceStatus.NotFind:
                        this.RxStatus = "NotFind!";
                        break;
                    case HomePage.typeDeviceStatus.DisConnected:
                        this.RxStatus = "Disconnected!";
                        break;
                    case HomePage.typeDeviceStatus.RxData:
                        this.RxStatus = "Connected";
                        break;
                }
            }
        }

        private String numberCall;
        public String NumberCall
        {
            get { return this.numberCall; }
            set
            {
                this.numberCall = value;
                NotifyPropertyChanged("NumberCall");
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

        private String stringGreenLamp;
        public String StringGreenLamp
        {
            get { return this.stringGreenLamp; }
            set
            {
                this.stringGreenLamp = value;
                NotifyPropertyChanged("StringGreenLamp");
            }

        }

        private String stringYellowLamp;
        public String StringYellowLamp
        {
            get { return this.stringYellowLamp; }
            set
            {
                this.stringYellowLamp = value;
                NotifyPropertyChanged("StringYellowLamp");
            }

        }

        private String stringRedLamp;
        public String StringRedLamp
        {
            get { return this.stringRedLamp; }
            set
            {
                this.stringRedLamp = value;
                NotifyPropertyChanged("StringRedLamp");
            }

        }

        private String stringBlueLamp;
        public String StringBlueLamp
        {
            get { return this.stringBlueLamp; }
            set
            {
                this.stringBlueLamp = value;
                NotifyPropertyChanged("StringBlueLamp");
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

        public Server(String deviceName, HomePage.typeDeviceStatus status,ResourceDictionary resource)
        {
            this.DeviceName = deviceName;
            this.DeviceType = BleContext.GetServerPost(deviceName).ServerType;
            this.IconStatus = "\xE702";
            this.IconSignalBars = "\xE870";
            this.Status = status;
            this.IconCallNumber = "";
            this.resource = resource;
            this.stringGreenLamp = "〇";
            this.stringYellowLamp = "〇";
            this.stringRedLamp = "〇";
            this.stringBlueLamp = "〇";
            if (resource != null)
            {
                this.PanelListColor = (SolidColorBrush)resource["ListNoticePanelBackground"];
            }
        }

    }
    public class Servers : ObservableCollection<Server>
    {
        public Servers()
        {//TODO:このジェネリクスの要素が０では表示できずエラーとなるのでは？

            //以下の実装については静的にxamlに結合されるため、表示テストの為に残す（実際に使用する場合はコメントアウトすること
            /*
            Add(new Server("Michael", HomePage.typeDeviceStatus.Connected,null)
            {
                ServerName = "test",
            });
            ;
            Add(new Server("Chris", HomePage.typeDeviceStatus.NotFind, null));
            Add(new Server("Seo-yun", HomePage.typeDeviceStatus.Find, null));
            Add(new Server("Guido", HomePage.typeDeviceStatus.DisConnected, null));
            */
        }

    }
    public class ListViewTemplateSelector : DataTemplateSelector
    {//TODO:M5Stack用のListviewUIを作成する
        //TODO:xaml側に何らかの問題があり、デバッガを介さないで実行すると落ちてしまう
        public DataTemplate TemplateAtomLIte { get; set; }
        public DataTemplate TemplateM5Stuck { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if(item is Server)
            {
                if((item as Server).DeviceType.Equals(BleContext.ServerType.AtomLite.ToString()))
                {
                    return TemplateAtomLIte;

                }
                else if ((item as Server).DeviceType.Equals(BleContext.ServerType.M5Stack.ToString()))
                {
                    return TemplateM5Stuck;
                }
            }
            return null;
        }
    }
}
