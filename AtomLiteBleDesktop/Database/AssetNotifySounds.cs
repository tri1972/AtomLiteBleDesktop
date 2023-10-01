using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace AtomLiteBleDesktop.Database
{
    public class AssetNotifySounds
    {
        /// <summary>
        /// Assetsの中にある音源データ名とソースパス取得用コレクション
        /// 音源追加時にはAssetsフォルダに音源データを置き、このコレクションにパスと名前を追加する
        /// </summary>
        private static List<AssetNotifySoundsSource> srcArrayAudios = new List<AssetNotifySoundsSource>
        {
            new AssetNotifySoundsSource{ Name="Phone-Ringtone01-1.mp3",                 AssetsSource="ms-appx:///Assets/Music/Rotary_Phone-Ringtone01-1.mp3" },
            new AssetNotifySoundsSource{ Name="Telephone-Ringtone01-1.mp3",             AssetsSource="ms-appx:///Assets/Music/Telephone-Ringtone01-1.mp3"},
            new AssetNotifySoundsSource{ Name="Telephone-Ringtone02-1.mp3",             AssetsSource="ms-appx:///Assets/Music/Telephone-Ringtone02-1.mp3" },
            new AssetNotifySoundsSource{ Name="Warning-Siren01-1.mp3",                  AssetsSource="ms-appx:///Assets/Music/Warning-Siren01-1.mp3" },
            new AssetNotifySoundsSource{ Name="Warning-Siren02-04(Fast-Short).mp3",     AssetsSource="ms-appx:///Assets/Music/Warning-Siren02-04(Fast-Short).mp3" },
            new AssetNotifySoundsSource{ Name="Warning-Siren03-01(High-Mid).mp3",       AssetsSource="ms-appx:///Assets/Music/Warning-Siren03-01(High-Mid).mp3" },
            new AssetNotifySoundsSource{ Name="Warning-Siren04-01.mp3",                 AssetsSource="ms-appx:///Assets/Music/Warning-Siren04-01.mp3" },
            new AssetNotifySoundsSource{ Name="Warning-Siren05-01(Fast-Mid).mp3",       AssetsSource="ms-appx:///Assets/Music/Warning-Siren05-01(Fast-Mid).mp3" },
        };

        /// <summary>
        /// Assetsの中にある音源データ取得用コレクション
        /// </summary>
        public static List<AssetNotifySoundsSource> SrcArrayAudios
        {
            get { return srcArrayAudios; }
        }
        
        public static int findIdWithName(string name)
        {
            return srcArrayAudios.FindIndex((x) => x.Name.Equals( name));
        }

    }
    public class AssetNotifySoundsSource
    {
        public string Name { get; set; }

        public string AssetsSource { get; set; }
    }
}
