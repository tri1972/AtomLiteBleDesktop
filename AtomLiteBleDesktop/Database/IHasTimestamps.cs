using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop.Database
{
    public interface IHasTimestamps
    {
        DateTime? UpdatedAt { get; set; }
        DateTime? CreatedAt { get; set; }
    }
}
