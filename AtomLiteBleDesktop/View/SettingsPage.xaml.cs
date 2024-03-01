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
using System.ComponentModel;
using AtomLiteBleDesktop.View;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// 検索対象となるポート数
        /// </summary>
        private const int NUMBER_SEARCHING_PORT = 16;

        private ObservableCollection<SettingPageContent> recordings = new ObservableCollection<SettingPageContent>();
        public ObservableCollection<SettingPageContent> Recordings 
        { 
            get 
            { 
                return this.recordings; 
            } 
        }

        ObservableCollection<string> Ports = new ObservableCollection<string>();



        public SettingPageBind ResourceSettingPage { get; set; }

        public SettingsPage()
        {
            this.InitializeComponent();
            this.ResourceSettingPage = new SettingPageBind();

            foreach (var post in BleContext.GetServerPosts())
            {
                this.recordings.Add(new SettingPageContent()
                {
                    Id = post.PostId,
                    Name = post.ServerName,
                    ServiceUUID = post.ServiceUUID,
                    CharacteristicUUID = post.CharacteristicUUID,
                    Sound = AssetNotifySounds.SrcArrayAudios[post.NumberSound].Name
                });

            }
            /*
            Ports.Add("COM0");
            Ports.Add("COM1");
            Ports.Add("COM2");
            Ports.Add("COM3");
            Ports.Add("COM4");
            Ports.Add("COM5");
            Ports.Add("COM6");
            Ports.Add("COM7");
            Ports.Add("COM8");
            */
        }


        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {

            for(int i=0;i< NUMBER_SEARCHING_PORT; i++)
            {
                var namePort = "COM" + i.ToString();
                ret = await SerialCommunication.SerialCommunicationTx.IsFindSerialPort(namePort);
                if (ret)
                {
                    Ports.Add(namePort);
                }
            }
        }


        static bool ret=false;
        private async void PortsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ret= await SerialCommunication.SerialCommunicationTx.IsFindSerialPort((sender as ComboBox).SelectedValue.ToString());
            if (ret)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.ResourceSettingPage.IsEnebledSerialPort = "true";
                });

            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.ResourceSettingPage.IsEnebledSerialPort = "false";

                });
            }
        }

        private void Control_Setting_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
            {
                
                (sender as TextBox).Background = new SolidColorBrush(Color.FromArgb(234,234,234,234));
                (sender as TextBox).BorderBrush = new SolidColorBrush(Color.FromArgb(234, 234, 234, 234));
                
                //(sender as TextBox).IsReadOnly = false;
            }
        }

        private async void Control_Setting_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if(sender is Grid)
            {
                var grid = (sender as Grid);
                if(grid.DataContext is SettingPageContent)
                {
                    var dbdeviceSender = grid.DataContext as SettingPageContent;

                    var dlg = new SettingPageContentDialog
                        (dbdeviceSender.Id, 
                        dbdeviceSender.Name, 
                        dbdeviceSender.ServiceUUID, 
                        dbdeviceSender.CharacteristicUUID, 
                        dbdeviceSender.Sound);
                    var result = await dlg.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {//dialogで入力されたデータを書き戻し
                        dbdeviceSender.Id = dlg.ViewModel.Id;
                        dbdeviceSender.Name= dlg.ViewModel.Name;
                        dbdeviceSender.ServiceUUID= dlg.ViewModel.ServiceUUID;
                        dbdeviceSender.CharacteristicUUID = dlg.ViewModel.CharacteristicUUID;
                        dbdeviceSender.Sound = dlg.ViewModel.Sound;
                    }
                    if (sender is ListView)
                    {
                        int index = (sender as ListView).SelectedIndex;

                        ((sender as ListView).SelectedValue as TextBox).IsReadOnly = false;
                    }

                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }
    }

    /// <summary>
    /// SendingPage.xamlとのBindingクラス
    /// </summary>
    public class SettingPageBind: BindableBase
    {

        private string isEnebledSerialPort;
        /// <summary>
        /// SerialPortが有効か否か
        /// </summary>
        public string IsEnebledSerialPort
        {
            get
            {
                return this.isEnebledSerialPort;
            }
            set
            {
                this.SetProperty(ref this.isEnebledSerialPort, value);
                this.isEnebledSerialPort = value;
            }
        }

        private bool isSendingKeepAlive;
        /// <summary>
        /// KeepAliveを送るか否か
        /// </summary>
        public  bool IsSendingKeepAlive
        {
            get { return this.isSendingKeepAlive; }
            set {
                this.SetProperty(ref this.isSendingKeepAlive, value);
                this.isSendingKeepAlive = value; 
            }
        }
    }
}
