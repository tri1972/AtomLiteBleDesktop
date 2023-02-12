using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// xaml上のTextプロパティを変更するBindingクラス
    /// Text="{Binding Path=textData.Text, UpdateSourceTrigger=PropertyChanged}" 
    /// </summary>
    public class HomePagePropertyChanged : INotifyPropertyChanged
    {
        private string _statusText = "初期値";
        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                NotifyPropertyChanged("StatusText");
            }
        }
        /// <summary>
        /// INotifyPropertyChangedの実態化event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Property変更イベントハンドラ
        /// </summary>
        /// <param name="info"></param>
        void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
