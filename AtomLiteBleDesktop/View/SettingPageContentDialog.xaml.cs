using AtomLiteBleDesktop.Database;
using AtomLiteBleDesktop.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AtomLiteBleDesktop
{
    public sealed partial class SettingPageContentDialog : ContentDialog
    {
        private SettingPageContent viewModel;
        public SettingPageContent ViewModel
        {
            get { return this.viewModel; }
            set { this.viewModel = value; }
        }

        ObservableCollection<string> ViewModelComboBox = new ObservableCollection<string>();

        public string SelectedItem { get; set; }

        public SettingPageContentDialog(int id, string name, string serviceUUID, string characteristicUUID, string sound)
        {
            this.InitializeComponent();
            this.viewModel = new SettingPageContent();
            this.viewModel.Id = id;
            this.viewModel.Name = name;
            this.viewModel.ServiceUUID = serviceUUID;
            this.viewModel.CharacteristicUUID = characteristicUUID;
            this.viewModel.Sound = sound;
            foreach(var data in AssetNotifySounds.SrcArrayAudios)
            {
                this.ViewModelComboBox.Add(data.Name);
            }
        }


        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.viewModel.Sound = SelectedItem;//ここで選択されたItemをViewmodelに書き戻す
            BleContext.DbSetRecord(this.ViewModel.Id,
                new Post
                {
                    PostId = this.viewModel.Id,
                    ServerName = this.viewModel.Name,
                    CharacteristicUUID = this.viewModel.CharacteristicUUID,
                    ServiceUUID = this.viewModel.ServiceUUID,
                    NumberSound = AssetNotifySounds.findIdWithName(SelectedItem)
                });
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
