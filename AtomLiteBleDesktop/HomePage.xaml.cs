using AtomLiteBleDesktop.Bluetooth;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();
        public HomePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            var bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];

            bluetoothAccesser.NotifyReceiveCharacteristic += registeredCharacteristicNotify;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
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
                        stringAdd_TextDataDispatcher("\n" + "取得Service名：");
                        foreach (var service in (sender as BluetoothAccesser).Services)
                        {
                            stringAdd_TextDataDispatcher("\n" + string.Copy(service.ServiceGattNativeServiceUuidString));
                        }
                        stringAdd_TextDataDispatcher("\n" + "取得Characteristic名：");
                        foreach (var name in (sender as BluetoothAccesser).CharacteristicNames)
                        {
                            stringAdd_TextDataDispatcher("\n" + string.Copy(name));

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

        /// <summary>
        /// BleServerからのNotify受信イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        [Obsolete]
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
    }
}
