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
        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string PIRSERVER = "ESP32PIRTRI";
        private const string dummySERVER1 = "dummy1";
        private const string dummySERVER2 = "dummy2";



        //static BluetoothLEAdvertisementWatcher watcher;
        //private GattPresentationFormat presentationFormat;
        //private GattCharacteristic selectedCharacteristic;

        private SettingsPagePropertyChanged _textData = new SettingsPagePropertyChanged();

        //private BluetoothConnector bluetoothConnector;

        List<string> servers;

        public MainPage()
        {
            this.InitializeComponent();
            advertist();
            DataContext = new
            {
                textData = _textData,
            };
        }

        private async void PageLoaded(FrameworkElement sender, object args)
        {

            logger.Info("Page Loaded :" + "MainPage");
            servers = new List<string>();
            servers.Add(PIRSERVER);
            servers.Add(dummySERVER1);
            servers.Add(dummySERVER2);
            

            var bluetoothAccesser = (BluetoothAccesser)Application.Current.Resources["appBluetoothAccesserInstance"];
            
            var result= await bluetoothAccesser.SearchDevices(servers,Dispatcher);
            if (result)
            {
                //ここで画面が小さくなってしまうと、UIスレッドが止まってしまうため、Connectがじっこうされない・・・

                //画面を起動時に最小にする方法
                //参考：https://social.msdn.microsoft.com/Forums/vstudio/ja-JP/fe39e1b6-e891-43a8-8bb2-01e4550a4b64/uwpmain?forum=winstoreapp
                IList<Windows.System.AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
                IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
                await resourceInfos[0].StartSuspendAsync();
            }

            // Attach a handler to process the received advertisement. 
            // The watcher cannot be started without a Received handler attached
            watcher.Received += OnAdvertisementReceived;
            // Attach a handler to process watcher stopping due to various conditions,
            // such as the Bluetooth radio turning off or the Stop method was called
            watcher.Stopped += OnAdvertisementWatcherStopped;

            watcher.Start();

        }

        /// <summary>
        /// Invoked as an event handler when an advertisement is received.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about the advertisement event.</param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {   
            // We can obtain various information about the advertisement we just received by accessing 
            // the properties of the EventArgs class

            // The timestamp of the event
            DateTimeOffset timestamp = eventArgs.Timestamp;

            // The type of advertisement
            BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;

            // The received signal strength indicator (RSSI)
            Int16 rssi = eventArgs.RawSignalStrengthInDBm;

            // The local name of the advertising device contained within the payload, if any
            string localName = eventArgs.Advertisement.LocalName;

            // Check if there are any manufacturer-specific sections.
            // If there is, print the raw data of the first manufacturer section (if there are multiple).
            string manufacturerDataString = "";
            var manufacturerSections = eventArgs.Advertisement.ManufacturerData;
            if (manufacturerSections.Count > 0)
            {
                // Only print the first one of the list
                var manufacturerData = manufacturerSections[0];
                var data = new byte[manufacturerData.Data.Length];
                using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                {
                    reader.ReadBytes(data);
                }
                // Print the company ID + the raw data in hex format
                manufacturerDataString = string.Format("0x{0}: {1}",
                    manufacturerData.CompanyId.ToString("X"),
                    BitConverter.ToString(data));
            }
            Debug.WriteLine(string.Format("[{0}]: type={1}, rssi={2}, name={3}, manufacturerData=[{4}]",
                    timestamp.ToString("hh\\:mm\\:ss\\.fff"),
                    advertisementType.ToString(),
                    rssi.ToString(),
                    localName,
                    manufacturerDataString));
        }
        
        /// <summary>
        /// Invoked as an event handler when the watcher is stopped or aborted.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about why the watcher stopped or aborted.</param>
        private async void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            // Notify the user that the watcher was stopped
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
            });
        }

        // The Bluetooth LE advertisement watcher class is used to control and customize Bluetooth LE scanning.
        private BluetoothLEAdvertisementWatcher watcher;
        /// <summary>
        /// Advertisement受信処理初期化
        /// </summary>
        private void advertist()
        {

            // Create and initialize a new watcher instance.
            watcher = new BluetoothLEAdvertisementWatcher();

            // Begin of watcher configuration. Configure the advertisement filter to look for the data advertised by the publisher 
            // in Scenario 2 or 4. You need to run Scenario 2 on another Windows platform within proximity of this one for Scenario 1 to 
            // take effect. The APIs shown in this Scenario are designed to operate only if the App is in the foreground. For background
            // watcher operation, please refer to Scenario 3.

            // Please comment out this following section (watcher configuration) if you want to remove all filters. By not specifying
            // any filters, all advertisements received will be notified to the App through the event handler. You should comment out the following
            // section if you do not have another Windows platform to run Scenario 2 alongside Scenario 1 or if you want to scan for 
            // all LE advertisements around you.

            // For determining the filter restrictions programatically across APIs, use the following properties:
            //      MinSamplingInterval, MaxSamplingInterval, MinOutOfRangeTimeout, MaxOutOfRangeTimeout

            // Part 1A: Configuring the advertisement filter to watch for a particular advertisement payload

            /*
                // First, let create a manufacturer data section we wanted to match for. These are the same as the one 
                // created in Scenario 2 and 4.
                var manufacturerData = new BluetoothLEManufacturerData();

                // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
                manufacturerData.CompanyId = 0xFFFE;

                // Finally set the data payload within the manufacturer-specific section
                // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
                var writer = new DataWriter();
                writer.WriteUInt16(0x1234);

                // Make sure that the buffer length can fit within an advertisement payload. Otherwise you will get an exception.
                manufacturerData.Data = writer.DetachBuffer();

                // Add the manufacturer data to the advertisement filter on the watcher:
                watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            */
            // Part 1B: Configuring the signal strength filter for proximity scenarios

            // Configure the signal strength filter to only propagate events when in-range
            // Please adjust these values if you cannot receive any advertisement 
            // Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
            // will start to be considered "in-range".
            watcher.SignalStrengthFilter.InRangeThresholdInDBm = -100;

            // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
            // to determine when an advertisement is no longer considered "in-range"
            watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -105;

            // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
            // to determine when an advertisement is no longer considered "in-range"
            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);

            // By default, the sampling interval is set to zero, which means there is no sampling and all
            // the advertisement received is returned in the Received event

            // End of watcher configuration. There is no need to comment out any code beyond this point.

        }


        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // MainPage.xaml で書いた MenuItems の下にさらにコードで MenuItems を追加したり...
            //NavView.MenuItems.Add(new NavigationViewItemSeparator());
            //NavView.MenuItems.Add(new NavigationViewItem() { Content = "My content", Icon = new SymbolIcon(Symbol.Folder), Tag = "my-content" });

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
    }

}
