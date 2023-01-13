using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace AtomLitePIR
{
    public class BluetoothLEDeviceDisplay : INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="deviceInfoIn"></param>
        public BluetoothLEDeviceDisplay(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
            UpdateGlyphBitmapImage();
        }

        public DeviceInformation DeviceInformation { get; private set; }

        public string Id => DeviceInformation.Id;
        public string Name => DeviceInformation.Name;
        public bool IsPaired => DeviceInformation.Pairing.IsPaired;
        public bool IsConnected => (bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true;
        public bool IsConnectable => (bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;

        public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;

        public BitmapImage GlyphBitmapImage { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private async void UpdateGlyphBitmapImage()
        {
            DeviceThumbnail deviceThumbnail = await DeviceInformation.GetGlyphThumbnailAsync();
            var glyphBitmapImage = new BitmapImage();
            await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
            GlyphBitmapImage = glyphBitmapImage;
            OnPropertyChanged("GlyphBitmapImage");
        }

        private void OnPropertyChanged(string v)
        {
            Debug.WriteLine("NotImplementedException:GlyphBitmapImage");
            //throw new NotImplementedException();
        }
    }
}
