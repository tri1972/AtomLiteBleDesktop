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
using AtomLiteBleDesktop.Database;
using Windows.Media.Import;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    //TODO: デバイスにより着信時の音が変わるようにする
    //TODO: 現在DBに登録されているデバイス名を表示できるようにする
    //TODO: DBにデバイスを登録できるようにする
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.ViewModel = new DeviceDBViewModel();
        }
        public DeviceDBViewModel ViewModel { get; set; }
    }
    public class DBDevice
    {
        public string Name { get; set; }
        public string ServiceUUID { get; set; }
        public string CharacteristicUUID { get; set; }
        public string Sound{ get; set; }
        public DBDevice()
        {
        }

    }
    public class DeviceDBViewModel
    {
        private ObservableCollection<DBDevice> recordings = new ObservableCollection<DBDevice>();
        public ObservableCollection<DBDevice> Recordings { get { return this.recordings; } }
        public DeviceDBViewModel()
        {
            foreach(var post in BleContext.GetServerPosts())
            {
                this.recordings.Add(new DBDevice()
                {
                    Name = post.ServerName,
                    ServiceUUID = post.ServiceUUID,
                    CharacteristicUUID = post.CharacteristicUUID,
                    Sound=post.NumberSound.ToString()
                });

            }
        }
    }


}
