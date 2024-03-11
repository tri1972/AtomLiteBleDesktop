using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace AtomLiteBleDesktop.Database
{
    public class DBSQLiteBaseConfig
    {

        private int numberRelease;
        /// <summary>
        /// DBのバージョン
        /// </summary>
        public int NumberRelease
        {
            get { return this.numberRelease; }
            set { this.numberRelease = value; }
        }

        public DBSQLiteBaseConfig()
        {

        }

    }


    public  class DBSQLiteBase<T>
    {
        private const string FILE_NAME= "sqliteSample.db";

        private string fileName;
        public string FileName
        {
            get { return this.fileName; }
            set { this.fileName = value; }
        }

        private string dbpath;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="numRelease">リリース番号</param>
        public DBSQLiteBase(int numRelease)
        {
            this.dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, FILE_NAME);
            //var configDBContext = new DBSQLiteBase<DBSQLiteBaseConfig>(0);

        }

        public string getTableName()
        {
            var data= this.GetType().GetGenericArguments()[0];
            // クラス名のみ取得
            return data.Name;
        }

        public List<DBSQLiteColumnInfo> getColumnInfos()
        {
            List<DBSQLiteColumnInfo> output = new List<DBSQLiteColumnInfo>();
            var data = this.GetType().GetGenericArguments()[0];

            //プロパティリストを取得
            var lstProperty = data.GetProperties();
            foreach (var property in lstProperty)
            {
                output.Add(new DBSQLiteColumnInfo(property.PropertyType.Name, property.Name));
            }

            return output;
        }

        /// <summary>
        /// データベース初期化
        /// </summary>
        /// <param name="fileName"></param>
        public async void InitializeDatabase(string fileName=null)
        {
            string currentFilePath;

            if (fileName == null)
            {
                currentFilePath = this.dbpath;
            }
            else
            {
                currentFilePath= Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
            }

            await ApplicationData.Current.LocalFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.OpenIfExists);
            using (SqliteConnection db =
               new SqliteConnection($"Filename={currentFilePath}"))
            {
                db.Open();

                String tableCommand = "CREATE TABLE IF NOT " +
                    "EXISTS "+
                    this.getTableName() + 
                    " (Id INTEGER PRIMARY KEY, ";

                for(int i=0;i< this.getColumnInfos().Count;i++)
                {
                    if(i!= this.getColumnInfos().Count - 1)
                    {
                        tableCommand += this.getColumnInfos()[i].Name + " "+ this.getColumnInfos()[i].Type.ToString()+ " NULL,";
                    }
                    else
                    {
                        tableCommand += this.getColumnInfos()[i].Name + " " + this.getColumnInfos()[i].Type.ToString() + " NULL)";
                    }
                }

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        /// <summary>
        /// DBにレコードを追加する
        /// </summary>
        /// <param name="inputTexts"></param>
        public void Add(T inputTexts)
        {
            try
            {
                var input = new List<object>();
                foreach (var data in inputTexts.GetType().GetProperties())
                {
                    input.Add(data.GetValue(inputTexts));
                }

                using (SqliteConnection db =
                  new SqliteConnection($"Filename={this.dbpath}"))
                {
                    db.Open();

                    SqliteCommand insertCommand = new SqliteCommand();
                    insertCommand.Connection = db;
                    insertCommand.CommandText = "INSERT INTO " + this.getTableName() + " VALUES (NULL, ";
                    for (int i = 0; i < input.Count(); i++)
                    {
                        // Use parameterized query to prevent SQL injection attacks
                        if (i != input.Count() - 1)
                        {
                            insertCommand.CommandText += "@" + i.ToString() + ",";
                        }
                        else
                        {
                            insertCommand.CommandText += "@" + i.ToString() + ");";
                        }
                        insertCommand.Parameters.AddWithValue("@" + i.ToString(), input[i]);
                    }
                    insertCommand.ExecuteReader();
                }
            }catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 終端レコードのみを取得
        /// </summary>
        /// <returns></returns>
        public T GetLastData()
        {
            List<T> entries;
            List<string> propertyNames = new List<string>();
            string sqlStr = "SELECT ";
            var properties = typeof(T).GetProperties();

            for (int i = 0; i < properties.Count(); i++)
            {//クラスのプロパティの順番で、レコードのすべてのColumnのデータを取得するSQL文字列を生成
                propertyNames.Add(properties[i].Name);
                if (i != properties.Count() - 1)
                {
                    sqlStr += properties[i].Name + ",";

                }
                else
                {
                    sqlStr += properties[i].Name + " FROM " + this.getTableName() + " ORDER BY id DESC LIMIT 1";

                }
            }
            entries = getRecords(sqlStr);
            return entries[0];
        }

        /// <summary>
        /// すべてのレコードを取得
        /// </summary>
        /// <returns></returns>
        public List<T> GetAllData()
        {
            List<T> entries;
            List<string> propertyNames = new List<string>();
            string sqlStr = "SELECT ";
            var properties = typeof(T).GetProperties();

            for (int i = 0; i < properties.Count(); i++)
            {//クラスのプロパティの順番で、レコードのすべてのColumnのデータを取得するSQL文字列を生成
                propertyNames.Add(properties[i].Name);
                if (i != properties.Count() - 1)
                {
                    sqlStr += properties[i].Name + ",";

                }
                else
                {
                    sqlStr += properties[i].Name + " FROM " + this.getTableName();

                }
            }
            entries = getRecords(sqlStr);
            return entries;
        }

        private List<T> getRecords(string sqlStr)
        {
            List<T> output = new List<T>();
            var properties = typeof(T).GetProperties();

            using (SqliteConnection db =
               new SqliteConnection($"Filename={this.dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    (sqlStr, db);

                SqliteDataReader query = selectCommand.ExecuteReader();
                while (query.Read())
                {//すべてのレコードを取得し、元クラスに格納
                    var instance = (T)Activator.CreateInstance(typeof(T));
                    for (int i = 0; i < properties.Count(); i++)
                    {
                        if (properties[i].PropertyType.Name == "String")
                        {
                            properties[i].SetValue(instance, query.GetString(i));
                        }
                        else if (properties[i].PropertyType.Name == "Int32")
                        {
                            properties[i].SetValue(instance, query.GetInt32(i));
                        }
                        else
                        {

                        }
                    }
                    output.Add(instance);
                }
            }
            return output;
        }
    }
}
