using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace AtomLiteBleDesktop.Bluetooth
{
    public class BluetoothAccesser
    {
        private static BluetoothAccesser instance = new BluetoothAccesser();

        private BluetoothWatcher bluetoothWatcher;
        public BluetoothWatcher BluetoothWatcher
        {
            get { return this.bluetoothWatcher; }
        }
        private static CoreDispatcher _mDispatcher;

        private BluetoothAccesser()
        {
        }

        public static BluetoothAccesser GetInstance(CoreDispatcher dispatcher)
        {
            _mDispatcher = dispatcher;
            return instance;
        }

        public string Watch2(string PIRSERVER)
        {
            this.bluetoothWatcher = new BluetoothWatcher(_mDispatcher);
            this.bluetoothWatcher.PIRServer = PIRSERVER;
            this.bluetoothWatcher.StartBleDeviceWatcher();
                //1s待って。接続Server名が取得できなければnullを返す
                int counter = 500;
                while (this.bluetoothWatcher.PIRServerSearched == null)
                {
                    if (counter == 0)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                    counter--;
                }
                if (this.bluetoothWatcher.PIRServerSearched != null)
                {
                    return bluetoothWatcher.PIRServerSearched;
                }
                else
                {
                    return null;
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PIRSERVER"></param>
        /// <returns></returns>
        public async Task<string> Watch(string PIRSERVER)
        {
            var task = await Task.Run<string>(() =>
              {
                  this.bluetoothWatcher = new BluetoothWatcher(_mDispatcher);
                  this.bluetoothWatcher.PIRServer = PIRSERVER;
                  this.bluetoothWatcher.StartBleDeviceWatcher();
                //1s待って。接続Server名が取得できなければnullを返す
                int counter = 500;
                  while (this.bluetoothWatcher.PIRServerSearched == null)
                  {
                      if (counter == 0)
                      {
                          break;
                      }
                      Thread.Sleep(10);
                      counter--;
                  }
                  if (this.bluetoothWatcher.PIRServerSearched != null)
                  {
                      return bluetoothWatcher.PIRServerSearched;
                  }
                  else
                  {
                      return null;
                  }
              });
            return task;
        }



    }
}
