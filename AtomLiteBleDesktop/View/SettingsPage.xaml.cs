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
        private ObservableCollection<SettingPageContent> recordings = new ObservableCollection<SettingPageContent>();
        public ObservableCollection<SettingPageContent> Recordings 
        { 
            get 
            { 
                return this.recordings; 
            } 
        }

        public SettingsPage()
        {
            this.InitializeComponent();
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
    }
}
