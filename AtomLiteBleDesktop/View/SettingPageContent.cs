using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop.View
{

    public class SettingPageContent : INotifyPropertyChanged
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
                this.serviceUUID = value;
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
        public string Sound
        {
            get { return this.sound; }
            set
            {
                this.sound = value;
                NotifyPropertyChanged("Sound");
            }
        }
        public SettingPageContent()
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
}
