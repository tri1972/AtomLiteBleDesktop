using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop.Database
{
    public class BleDeviceColumns
    {
        public int PostId { get; set; }
        public string ServerType { get; set; }
        public string ServerName { get; set; }
        public string ServiceUUID { get; set; }
        public string CharacteristicUUID { get; set; }

        public int NumberSound { get; set; }

    }
}
