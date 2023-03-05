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
        private BluetoothWatcher bluetoothWatcher;

        //UIスレッドにアクセスするためのDispatcher
        private static CoreDispatcher _mDispatcher;

        /// <summary>
        /// コンストラクタ:引数なしでないとXamlのResourceDictionnaryに登録できない
        /// </summary>
        public BluetoothAccesser()
        {
        }

        /// <summary>
        /// 指定したサーバ名を探し、存在すればそのサーバ名を返却：非同期
        /// </summary>
        /// <param name="PIRSERVER"></param>
        /// <returns></returns>
        public async Task<string> Watch(string PIRSERVER)
        {
            this.bluetoothWatcher = BluetoothWatcher.GetInstance();

            var task = await Task.Run<string>(() =>
            {
                return this.bluetoothWatcher.WatchSync(PIRSERVER);
            });
            return task;
        }


    }
}
