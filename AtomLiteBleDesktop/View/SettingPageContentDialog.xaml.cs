using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public DBDevice ViewModel { get; set; }

        ObservableCollection<string> ViewModelComboBox = new ObservableCollection<string>();

        public string SelectedItem { get; set; }

        public SettingPageContentDialog()
        {
            this.InitializeComponent();
            this.ViewModel = new DBDevice();
            this.ViewModel.CharacteristicUUID = "aaaaaa";
            this.ViewModelComboBox.Add("test1");
        }


        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var selecet=SelectedItem;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
