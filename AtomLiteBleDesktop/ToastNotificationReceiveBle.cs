using AtomLiteBleDesktop.Database;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop
{
    class ToastNotificationReceiveBle
    {
        private string[] srcArrayAudios =
        {
            "ms-appx:///Assets/Music/Rotary_Phone-Ringtone01-1.mp3",
            "ms-appx:///Assets/Music/Telephone-Ringtone01-1.mp3",
            "ms-appx:///Assets/Music/Telephone-Ringtone02-1.mp3",
            "ms-appx:///Assets/Music/Warning-Siren01-1.mp3",
            "ms-appx:///Assets/Music/Warning-Siren02-04(Fast-Short).mp3",
            "ms-appx:///Assets/Music/Warning-Siren03-01(High-Mid).mp3",
            "ms-appx:///Assets/Music/Warning-Siren04-01.mp3",
            "ms-appx:///Assets/Music/Warning-Siren05-01(Fast-Mid).mp3",
        };

        private int currentAudioSrcNum;
        public int CurrentAudioSrcNum
        {
            get { return this.currentAudioSrcNum; }
            set { this.currentAudioSrcNum = value; }
        }

        private string srcAudio;
        public string SrcAudio
        {
            get { return this.srcAudios[currentAudioSrcNum]; }
        }

        private List<string> srcAudios;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ToastNotificationReceiveBle()
        {
            srcAudios = new List<string>();
            foreach(var src in srcArrayAudios)
            {
                srcAudios.Add(src);
            }
        }


        public bool Show(string deviceName)
        {
            int audioSrcNum = 0;
            //TODO: メッセージをM5Stuckのほうには遅れるようにする
            if (0 <= audioSrcNum
                && audioSrcNum < srcAudios.Count)
            {
                var serverPost = BleContext.GetServerPosts(deviceName);
                var audio = new ToastAudio()
                {
                    Loop = false,
                    Silent = false,
                    Src = new Uri(srcAudios[serverPost.NumberSound])
                };

                new ToastContentBuilder()
                    .AddAudio(audio)
                    .SetToastScenario(ToastScenario.Reminder)
                    .AddText("Call Server")
                    .AddText(deviceName)
                    .AddToastInput(new ToastSelectionBox("snoozeTime")
                    {
                        DefaultSelectionBoxItemId = "15",
                        Items =
                        {//下記ではリストボックスのリストを生成する、第一引数のidにて指定された値で再通知時間(分単位)がきまる(id="1"なら1分)
                            new ToastSelectionBoxItem("1", "1 minutes"),
                            new ToastSelectionBoxItem("5", "5 minutes"),
                            new ToastSelectionBoxItem("15", "15 minutes"),
                            new ToastSelectionBoxItem("60", "1 hour"),
                            new ToastSelectionBoxItem("240", "4 hours")
                        }
                    })
                    .AddButton(new ToastButtonSnooze() { SelectionBoxId = "snoozeTime" })
                    .AddButton(new ToastButtonDismiss())
                    .Show();
            }
            else
            {
                return false;
            }

            return true;
        }

    }
}
