using AtomLiteBleDesktop.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


namespace AtomLiteBleDesktop
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        ///単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、論理的には main() または WinMain() と等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += new EventHandler<Object>(App_Resuming);
            
            
            using (var db = new BleContext())
            {
                db.Database.EnsureCreated();
            }
            //BleContext.DbInitRecord();//Dbに新規にデータを追加したい場合はこれを実行する
        }

        /// <summary>
        /// アプリケーションがエンド ユーザーによって正常に起動されたときに呼び出されます。他のエントリ ポイントは、
        /// アプリケーションが特定のファイルを開くために起動されたときなどに使用されます。
        /// </summary>
        /// <param name="e">起動の要求とプロセスの詳細を表示します。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // ウィンドウに既にコンテンツが表示されている場合は、アプリケーションの初期化を繰り返さずに、
            // ウィンドウがアクティブであることだけを確認してください
            if (rootFrame == null)
            {
                // ナビゲーション コンテキストとして動作するフレームを作成し、最初のページに移動します
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //以前中断したアプリケーションから状態を読み込みます
                }

                // フレームを現在のウィンドウに配置します
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // ナビゲーションの履歴スタックが復元されていない場合、最初のページに移動します。
                    // このとき、必要な情報をナビゲーション パラメーターとして渡して、新しいページを
                    // 作成します
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // 現在のウィンドウがアクティブであることを確認します
                Window.Current.Activate();
            }
#if DEBUG
            logger.Info("On launched");

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
#endif
            this._timer = ThreadPoolTimer.CreatePeriodicTimer(_timerEvent, TimeSpan.FromSeconds(1));

            //initSerialPort();

        }
        private int _count;
        private ThreadPoolTimer _timer;
        private SerialDevice device;

        private async void driveSerial()
        {

            string portName = "COM6";
            string aqs = SerialDevice.GetDeviceSelector(portName);
            DeviceInformationCollection serialDeviceInfos = await DeviceInformation.FindAllAsync(aqs);
            //DeviceInformationCollection serialDeviceInfos = await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector());

            foreach (DeviceInformation serialDeviceInfo in serialDeviceInfos)
            {
                try
                {
                    using (SerialDevice serialDevice = await SerialDevice.FromIdAsync(serialDeviceInfo.Id))
                    {

                        if (serialDevice != null)
                        {
                            if (serialDevice.IsRequestToSendEnabled)
                            {
                                serialDevice.BaudRate = 115200;
                                serialDevice.DataBits = 8;
                                serialDevice.StopBits = SerialStopBitCount.One;
                                serialDevice.Parity = SerialParity.None;
                                serialDevice.Handshake = SerialHandshake.None;
                                serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                                serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                            }
                            serialDevice.ErrorReceived += SerialDeviceError;
                            // Found a valid serial device.

                            // Reading a byte from the serial device.
                            //DataReader dr = new DataReader(serialDevice.InputStream);
                            //int readByte = dr.ReadByte();

                            // Writing a byte to the serial device.
                            DataWriter dw = new DataWriter(serialDevice.OutputStream);
                            //dw.WriteByte(0x0a);
                            dw.WriteString("\r");
                            var ret = dw.StoreAsync();
                        }
                    }
                }
                catch (Exception)
                {
                    // Couldn't instantiate the device
                }
            }

        }

        private async void _timerEvent(ThreadPoolTimer timer)
        {
            driveSerial();
            /*
            try
            {
                Debug.WriteLine("1secTick : " + this._count.ToString() + "Count");

                logger.Info("Send Serial data 1secTick");
                this._count++;
                if (device != null)
                {
                    using (DataWriter dataWriteeObject = new DataWriter(device.OutputStream))
                    {
                        dataWriteeObject.WriteString("\r");
                        var ret = dataWriteeObject.StoreAsync();
                        
                        if (ret!=null && ret.Status == AsyncStatus.Completed)
                        {
#if DEBUG
                            logger.Info("Send Serial data");
#endif
                        }
                        else
                        {
#if DEBUG
                            //logger.Info("Error Send Serial data : " + ret.Status.ToString());
#endif

                        }
                        await Task.Delay(100);
                    }


                }
            }catch(SystemException e)
            {
                throw e;
            }
            */
        }

        private void SerialDeviceError(SerialDevice sender, ErrorReceivedEventArgs e)
        {
            ;
        }

        private async void initSerialPort()
        {
            string portName = "COM6";
            string aqs = SerialDevice.GetDeviceSelector(portName);

            var myDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);
            if (myDevices.Count == 0)
            {
                return;
            }
            else
            {
                //TODO: 下記がnullになると落ちてしまうので対策が必要・・・
                device = await SerialDevice.FromIdAsync(myDevices[0].Id);
                if (device.IsRequestToSendEnabled)
                {
                    device.BaudRate = 115200;
                    device.DataBits = 8;
                    device.StopBits = SerialStopBitCount.One;
                    device.Parity = SerialParity.None;
                    device.Handshake = SerialHandshake.None;
                    device.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    device.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                }
                device.ErrorReceived += SerialDeviceError;

            }

        }

        /// <summary>
        /// 特定のページへの移動が失敗したときに呼び出されます
        /// </summary>
        /// <param name="sender">移動に失敗したフレーム</param>
        /// <param name="e">ナビゲーション エラーの詳細</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// アプリケーションの実行が中断されたときに呼び出されます。
        /// アプリケーションが終了されるか、メモリの内容がそのままで再開されるかに
        /// かかわらず、アプリケーションの状態が保存されます。
        /// </summary>
        /// <param name="sender">中断要求の送信元。</param>
        /// <param name="e">中断要求の詳細。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        async protected void App_Resuming(Object sender, Object e)
        {
            ;
        }
    }
}
