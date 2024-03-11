using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop.Database
{
    public class ConfigColumns: IHasTimestamps
    {
        public int Id { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        private string selectPortName;
        /// <summary>
        /// 選択したPort名
        /// </summary>
        public string SelectPortName
        {
            get { return this.selectPortName; }
            set { this.selectPortName = value; }
        }

        private bool isSendingKeepAlive;
        /// <summary>
        /// KeepAliveを送るか否か
        /// </summary>
        public bool IsSendingKeepAlive
        {
            get { return this.isSendingKeepAlive; }
            set
            {
                this.isSendingKeepAlive = value;
            }
        }
    }
}
