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
        
        private void Control_Setting_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
            {
                
                (sender as TextBox).Background = new SolidColorBrush(Color.FromArgb(234,234,234,234));
                (sender as TextBox).BorderBrush = new SolidColorBrush(Color.FromArgb(234, 234, 234, 234));
                
                //(sender as TextBox).IsReadOnly = false;
            }
        }
//TODO : ダブルクリックしたListviewのテキストボックスを編集できるようにする
        private async void Control_Setting_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if(sender is Grid)
            {
                var grid = (sender as Grid);
                if(grid.DataContext is DBDevice)
                {
                    var dbdeviceSender = grid.DataContext as DBDevice;

                    var dlg = new SettingPageContentDialog();
                    dlg.ViewModel.Id = dbdeviceSender.Id;
                    dlg.ViewModel.Name = dbdeviceSender.Name;
                    dlg.ViewModel.ServiceUUID = dbdeviceSender.ServiceUUID;
                    dlg.ViewModel.CharacteristicUUID = dbdeviceSender.CharacteristicUUID;
                    dlg.ViewModel.Sound = dbdeviceSender.Sound;
                    var result = await dlg.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        dbdeviceSender.Id = dlg.ViewModel.Id;
                        dbdeviceSender.Name= dlg.ViewModel.Name;
                        dbdeviceSender.ServiceUUID= dlg.ViewModel.ServiceUUID;
                        dbdeviceSender.CharacteristicUUID = dlg.ViewModel.CharacteristicUUID;
                        dbdeviceSender.Sound= dlg.ViewModel.Sound;
                        BleContext.DbSetRecord(dlg.ViewModel.Id, new Post
                        {
                            PostId = dlg.ViewModel.Id,
                            ServerName = dlg.ViewModel.Name,
                            CharacteristicUUID = dlg.ViewModel.CharacteristicUUID,
                            ServiceUUID = dlg.ViewModel.ServiceUUID,
                            NumberSound = int.Parse(dbdeviceSender.Sound)
                    });
                    }


                    if (sender is ListView)
                    {
                        int index = (sender as ListView).SelectedIndex;

                        ((sender as ListView).SelectedValue as TextBox).IsReadOnly = false;
                    }

                }
            }
        }
    }
    public class DBDevice : INotifyPropertyChanged
    {
        private int id;
        public int Id
        {
            get { return this.id; }
            set
            {
                this.id = value;
                NotifyPropertyChanged("Id");
            }
        }

        private string name { get; set; }
        public string Name 
        {
            get { return this.name; }
            set 
            { 
                this.name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private string serviceUUID;
        public string ServiceUUID 
        {
            get { return this.serviceUUID; }
            set 
            {
                this.serviceUUID=value;
                NotifyPropertyChanged("ServiceUUID");
            }
        
        }
        private string characteristicUUID;
        public string CharacteristicUUID 
        {
            get { return this.characteristicUUID; }
            set 
            { 
                this.characteristicUUID = value;
                NotifyPropertyChanged("CharacteristicUUID");
            }
        }

        private string sound;
        public string Sound{
            get { return this.sound; }
            set { 
                this.sound = value;
                NotifyPropertyChanged("Sound");
            }
        }
        public DBDevice()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
    public class DeviceDBViewModel
    {
        private ObservableCollection<DBDevice> recordings = new ObservableCollection<DBDevice>();
        public ObservableCollection<DBDevice> Recordings { get { return this.recordings; } }
        public DeviceDBViewModel()
        {
            foreach (var post in BleContext.GetServerPosts())
            {
                this.recordings.Add(new DBDevice()
                {
                    Id=post.PostId,
                    Name = post.ServerName,
                    ServiceUUID = post.ServiceUUID,
                    CharacteristicUUID = post.CharacteristicUUID,
                    Sound=post.NumberSound.ToString()
                });

            }
        }
    }
}
