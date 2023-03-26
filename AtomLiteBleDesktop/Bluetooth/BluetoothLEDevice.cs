using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// BluetoothLe(BLE)のデバイス情報を格納する
    /// </summary>
    public class BluetoothLEDevice //: INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ
        /// Deviceが見つかった場合
        /// </summary>
        /// <param name="deviceInfoIn"></param>
        public BluetoothLEDevice(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
            this.id = DeviceInformation.Id;
            this.name= DeviceInformation.Name;
            this.isFindDevice = true;
            this.isPaired = DeviceInformation.Pairing.IsPaired;
            try
            {
                if ((bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true)
                {
                    this.isConnected = true;
                }
                else
                {
                    this.isConnected = false;
                }
                if ((bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true)
                {
                    this.isConnectable = true;

                }
                else
                {
                    this.isConnectable = false;
                }
            }catch(Exception err)
            {
                Debug.WriteLine(err.Message);
            }

                this.properties = DeviceInformation.Properties;
        }

        /// <summary>
        /// コンストラクタ
        /// Deviceが見つからなかった場合
        /// </summary>
        public BluetoothLEDevice()
        {
            this.id = null;
            this.name = null;
            this.isFindDevice = false;
            this.isPaired = false;
            this.isConnected = false;
            this.isConnectable = false;
            this.properties = null;
        }

        public DeviceInformation DeviceInformation { get; private set; }

        private bool isFindDevice;
        public bool IsFindDevice
        {
            get { return this.isFindDevice; }
        }

        private string id;
        public string Id
        {
            get { return this.id; }
        }
        private string name;
        public string Name
        {
            get { return this.name; }
        }
        private bool isPaired;
        public bool IsPaired
        {
            get { return this.isPaired; }
        }
        private bool isConnected;
        public bool IsConnected
        {
            get { return this.isConnected; }
        }
        private bool isConnectable;
        public bool IsConnectable
        {
            get { return this.isConnectable; }
        }

        private IReadOnlyDictionary<string, object> properties;
        public IReadOnlyDictionary<string, object> Properties
        {
            get { return this.properties; }
        }

        public void Connect()
        {

        }

        //public event PropertyChangedEventHandler PropertyChanged=null;
    }
}
