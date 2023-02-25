using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AtomLiteBleDesktop
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Application
    {
        public const string SampleBackgroundTaskEntryPoint = "AtomLiteBleBackground.BluetoothConnector";
        public const string SampleBackgroundTaskName = "BluetoothConnector";
        public static string SampleBackgroundTaskProgress = "";
        public static bool SampleBackgroundTaskRegistered = false;

        public const string TimeTriggeredTaskName = "TimeTriggeredTask";
        public const string ApplicationTriggerTaskName = "ApplicationTriggerTask";

        /// <summary>
        ///単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、論理的には main() または WinMain() と等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
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
                    //TODO: 以前中断したアプリケーションから状態を読み込みます
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

            /*
            var task = RegisterBackgroundTask(SampleBackgroundTaskEntryPoint,
                                                                   SampleBackgroundTaskName,
                                                                   new TimeTrigger(15, false),
                                                                   null);
            */
            
            var task = RegisterBackgroundTask(SampleBackgroundTaskEntryPoint,
                                                                   SampleBackgroundTaskName,
                                                                   new SystemTrigger(SystemTriggerType.TimeZoneChange, false),
                                                                   null);
            

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
            //TODO: アプリケーションの状態を保存してバックグラウンドの動作があれば停止します
            deferral.Complete();
        }

        /// <summary>
        ///BackGroundタスクの登録を行う 
        /// </summary>
        /// <param name="taskEntryPoint">名前空間.クラス名</param>
        /// <param name="name">タスクの名前</param>
        /// <param name="trigger"></param>
        /// <param name="condition"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public static BackgroundTaskRegistration RegisterBackgroundTask(String taskEntryPoint, String name, IBackgroundTrigger trigger, IBackgroundCondition condition, BackgroundTaskRegistrationGroup group = null)
        {
            BackgroundTaskRegistration task;
            try
            {
                if (TaskRequiresBackgroundAccess(name))
                {
                    // If the user denies access, the task will not run.
                    var requestTask = BackgroundExecutionManager.RequestAccessAsync();
                }

                var builder = new BackgroundTaskBuilder();

                builder.Name = name;
                builder.TaskEntryPoint = taskEntryPoint;
                builder.SetTrigger(trigger);

                if (condition != null)
                {
                    builder.AddCondition(condition);

                    //
                    // If the condition changes while the background task is executing then it will
                    // be canceled.
                    //
                    builder.CancelOnConditionLoss = true;
                }

                if (group != null)
                {
                    builder.TaskGroup = group;
                }

                task = builder.Register();

                //UpdateBackgroundTaskRegistrationStatus(name, true);

                //
                // Remove previous completion status.
                //
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values.Remove(name);
            }catch(Exception err)
            {
                throw err;
            }
            return task;
        }
        public static bool TaskRequiresBackgroundAccess(String name)
        {
            if ((name == TimeTriggeredTaskName) ||
                (name == ApplicationTriggerTaskName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
