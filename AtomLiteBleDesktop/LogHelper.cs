using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AtomLiteBleDesktop
{
    public static class LogHelper
    {
        private static ILog logger;

        private static readonly string defaultFileName="AtomLiteBleDesktop.log";

        public static void SetInstance(Type type)
        {
            GetInstanceLog4net(type);
        }

        public static ILog GetInstanceLog4net(Type type)
        {
            // Loggerの生成
            LogHelper.logger = LogManager.GetLogger(type);

            // RootのLoggerを取得
            var rootLogger = ((Hierarchy)logger.Logger.Repository).Root;
            
            
            // RootのAppenderを取得
            var appender = rootLogger.GetAppender("RollingLogFileAppender") as RollingFileAppender ;

            // ファイル名の取得
            var filepath = appender.File;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            
            if (!filepath.Equals(localFolder.Path+@"\"+ defaultFileName))
            {
                
                // ファイル名の設定
                appender.File = localFolder.Path + @"\" + defaultFileName;
                appender.ActivateOptions();
                
            }
            
            return logger;
        }
    }
}
