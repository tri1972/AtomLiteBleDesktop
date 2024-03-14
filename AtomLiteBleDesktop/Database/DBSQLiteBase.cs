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
        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string FILE_NAME= "sqliteSample.db";

        private string fileName;
        public string FileName
        {
            get { return this.fileName; }
            set { this.fileName = value; }
        }

        private string dbpath;
        private DBSQLiteBase<DBSQLiteBaseConfig> configDBContext;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DBSQLiteBase()
        {
            this.dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, FILE_NAME);

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
        public async void InitializeDatabase(int releaseNumber, string fileName=null)
        {
            try
            {
                string currentFilePath;

                if (fileName == null)
                {
                    currentFilePath = this.dbpath;
                    fileName = FILE_NAME;
                }
                else
                {
                    currentFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
                }

                //ファイルが存在しなければ新規作成。存在していればそのファイルをオープン
                await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);

                if (this.configDBContext == null)
                {
                    this.configDBContext = new DBSQLiteBase<DBSQLiteBaseConfig>();
                    if (!this.configDBContext.HasTable())
                    {
                        this.configDBContext.CreateNewTable(currentFilePath);
                    }
                    if (this.configDBContext.Count() > 0)
                    {
                        var lastData = this.configDBContext.GetLastData();
                        if (lastData != null)
                        {
                            if (lastData.NumberRelease < releaseNumber)
                            {
                                DeleteTable(this.getTableName(), currentFilePath);
                                this.CreateNewTable(currentFilePath);
                                this.configDBContext.Add(new DBSQLiteBaseConfig() { NumberRelease = releaseNumber });
                            }
                        }
                        else
                        {
                            this.configDBContext.Add(new DBSQLiteBaseConfig() { NumberRelease = releaseNumber });
                        }
                    }
                    else
                    {
                        this.configDBContext.Add(new DBSQLiteBaseConfig() { NumberRelease = releaseNumber });
                    }
                }

                this.CreateNewTable(currentFilePath);
            }
            catch(SqliteException err)
            {
                logger.Error("Sqllite Error :" + err.Message);
                throw err;
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// テーブルが存在するかを確認
        /// </summary>
        /// <param name="tableName">テーブル名（指定しなければクラス名で）</param>
        /// <returns></returns>
        public bool HasTable(string tableName=null )
        {
            bool output = false;
            if (tableName == null)
            {
                tableName = this.getTableName();
            }

            using (SqliteConnection db =
               new SqliteConnection($"Filename={this.dbpath}"))
            {
                int isExist = 0;
                db.Open();

                String tableCommand = "SELECT COUNT(*) FROM sqlite_master WHERE TYPE='table' AND name='"+ tableName + "'";
                SqliteCommand count = new SqliteCommand(tableCommand, db);
                SqliteDataReader query = count.ExecuteReader();
                while (query.Read())
                {
                    isExist = query.GetInt32(0);
                }
                if (isExist == 0)
                {
                    output = false;
                }
                else
                {
                    output= true;
                }

            }
            return output;
        }

        /// <summary>
        /// レコード数を数える
        /// </summary>
        /// <param name="tableName">テーブル名（指定しなければクラス名で）</param>
        /// <returns></returns>
        public int Count(string tableName = null)
        {
            int output = 0;
            if (tableName == null)
            {
                tableName = this.getTableName();
            }
            using (SqliteConnection db =
               new SqliteConnection($"Filename={this.dbpath}"))
            {
                db.Open();

                String tableCommand = "select count(*) from " + tableName;
                SqliteCommand count = new SqliteCommand(tableCommand, db); 
                SqliteDataReader query = count.ExecuteReader();
                while (query.Read())
                {
                    output= query.GetInt32(0);
                }

            }
            return output;
        }

        /// <summary>
        /// テーブルを削除する
        /// </summary>
        /// <param name="tableName">削除するテーブル名</param>
        /// <param name="currentFilePath">テーブルの入ったファイルパス</param>
        public void DeleteTable(string tableName, string currentFilePath)
        {
            using (SqliteConnection db =
               new SqliteConnection($"Filename={currentFilePath}"))
            {
                db.Open();

                String tableCommand = "DROP TABLE " + tableName;
                SqliteCommand deleteTable = new SqliteCommand(tableCommand, db);

                deleteTable.ExecuteReader();

            }
        }

        /// <summary>
        /// 作成したファイルに対してtableを構築する
        /// </summary>
        /// <param name="currentFilePath">作成したファイル名</param>
        public void CreateNewTable(string currentFilePath)
        {
            using (SqliteConnection db =
               new SqliteConnection($"Filename={currentFilePath}"))
            {
                db.Open();

                String tableCommand = "CREATE TABLE IF NOT " +
                    "EXISTS " +
                    this.getTableName() +
                    " (Id INTEGER PRIMARY KEY, ";

                for (int i = 0; i < this.getColumnInfos().Count; i++)
                {
                    if (i != this.getColumnInfos().Count - 1)
                    {
                        tableCommand += this.getColumnInfos()[i].Name + " " + this.getColumnInfos()[i].Type.ToString() + " NULL,";
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
            }catch(SqliteException e)
            {
                logger.Error("Sqllite Error :"+ e.Message);
            }
            catch (Exception e)
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
            if (entries.Count() > 0)
            {
                return entries[0];
            }
            else
            {
                return default(T);
            }
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
