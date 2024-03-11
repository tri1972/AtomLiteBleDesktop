using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop.Database
{
    public class DBSQLiteColumnInfo
    {
        public enum Types
        {
            NULL,
            INTEGER,
            REAL,
            TEXT,
            BLOB,
        }

        /// <summary>
        /// 
        /// </summary>
        public Types Type
        {
            get;set;
        }

        public string Name { get; set; }

        public DBSQLiteColumnInfo(string type,string name)
        {
            if (type.Equals("Int32"))
            {
                this.Type = Types.INTEGER;
            }
            else if (type.Equals("String"))
            {
                this.Type = Types.TEXT;
            }
            else
            {
                this.Type = Types.NULL;
            }
            this.Name = name;

        }
    }
}
