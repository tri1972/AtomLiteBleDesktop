using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.ExtendedExecution;

namespace AtomLiteBleDesktop
{
    public class ExtendedExecutionHelper
    {
        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ExtendedExecutionSession session = null;

        private static ExtendedExecutionHelper instance = new ExtendedExecutionHelper();

        private ExtendedExecutionHelper()
        {

        }

        public static ExtendedExecutionHelper GetInstance()
        {
            return instance;
        }

        public async void BeginExtendedExecution()
        {
            // The previous Extended Execution must be closed before a new one can be requested.
            // This code is redundant here because the sample doesn't allow a new extended
            // execution to begin until the previous one ends, but we leave it here for illustration.
            ClearExtendedExecution();

            var newSession = new ExtendedExecutionSession();
            newSession.Reason = ExtendedExecutionReason.Unspecified;
            newSession.Description = "Raising periodic toasts";
            newSession.Revoked += SessionRevoked;
            ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

            switch (result)
            {
                case ExtendedExecutionResult.Allowed:
                    session = newSession;
                    break;

                default:
                case ExtendedExecutionResult.Denied:
#if DEBUG
                    logger.Error("Extended execution denied.");
#endif
                    newSession.Dispose();
                    break;
            }
        }


        void ClearExtendedExecution()
        {
            if (session != null)
            {
                session.Revoked -= SessionRevoked;
                session.Dispose();
                session = null;
            }
        }

        private async void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
                switch (args.Reason)
                {
                    case ExtendedExecutionRevokedReason.Resumed:
#if DEBUG
                        logger.Info("Extended execution revoked due to returning to foreground.");
#endif
                        break;

                    case ExtendedExecutionRevokedReason.SystemPolicy:
#if DEBUG
                        logger.Info("Extended execution revoked due to system policy.");
#endif
                        break;
                }

                EndExtendedExecution();
        }
        private void EndExtendedExecution()
        {
            ClearExtendedExecution();
        }
    }
}
