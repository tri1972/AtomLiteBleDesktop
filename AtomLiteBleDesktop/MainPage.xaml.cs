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
using Windows.System;
using AtomLiteBleDesktop;
using Windows.ApplicationModel.Background;
using Windows.Storage;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

/// <summary>
/// 参考Url：https://blog.beachside.dev/entry/2018/06/22/210000
/// </summary>
namespace AtomLiteBleDesktop
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

        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();

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

        private async void PageLoaded(FrameworkElement sender, object args)
        {
            this.bluetoothWatcher = new BluetoothWatcher(this.Dispatcher);
            //画面を起動時に最小にする方法
            //参考：https://social.msdn.microsoft.com/Forums/vstudio/ja-JP/fe39e1b6-e891-43a8-8bb2-01e4550a4b64/uwpmain?forum=winstoreapp
            IList<Windows.System.AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
            await resourceInfos[0].StartSuspendAsync();

            var task = BackgroundTaskRegistration.AllTasks
                .First(x => x.Value.Name == "BluetoothConnector")
                .Value;
            task.Progress += this.Task_Progress;
            task.Completed += this.Task_Completed;
        }


        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // MainPage.xaml で書いた MenuItems の下にさらにコードで MenuItems を追加したり...
            NavView.MenuItems.Add(new NavigationViewItemSeparator());
            NavView.MenuItems.Add(new NavigationViewItem() { Content = "My content", Icon = new SymbolIcon(Symbol.Folder), Tag = "my-content" });

            // home を選択
            var item = GetNavigationViewItem("home");
            NavView.SelectedItem = item;
            // frame に home を表示
            NavView_Navigate("home");

            ContentFrame.Navigated += On_Navigated;
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                // メソッドの引数 arg の args.InvokedItem は、メニューがクリックされたら、Content の値が飛んでくる。へー。
                var tag = sender.MenuItems.OfType<NavigationViewItem>()
                    .First(x => x.Content.ToString() == args.InvokedItem.ToString())
                    .Tag.ToString();

                NavView_Navigate(tag);
            }
        }

        private NavigationViewItem GetNavigationViewItem(string tag)
            => NavView.MenuItems.OfType<NavigationViewItem>().First(i => i.Tag.ToString() == tag);

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            //NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem as NavigationViewItem;
            }
            else
            {
                var tag = _pageContents[ContentFrame.SourcePageType];
                NavView.SelectedItem = GetNavigationViewItem(tag);
            }
        }

        private void NavView_Navigate(string tag)
        {
            var targetType = _pageContents.First(c => c.Value.Equals(tag)).Key;
            ContentFrame.Navigate(targetType);
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            // 今回は使わないけどメソッドだけ用意してみた...
        }


        private static IReadOnlyDictionary<Type, string> _pageContents = new Dictionary<Type, string>()
        {
            {typeof(HomePage), "home"},
            {typeof(ControlPage), "Control"},
            {typeof(LogPage), "Log"},
           
        };


        private string backGroundText;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
/*
            var task = BackgroundTaskRegistration.AllTasks
                .First(x => x.Value.Name == "BluetoothConnector")
                .Value;
            task.Progress += this.Task_Progress;
            task.Completed += this.Task_Completed;
*/
        }

        private async void Task_Progress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.backGroundText = $"{args.Progress}%";
                Debug.WriteLine(this.backGroundText);
            });
        }

        private async void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.backGroundText = (string)ApplicationData.Current.LocalSettings.Values["BluetoothConnector"];
                Debug.WriteLine(this.backGroundText);
            });
        }
    }

}
