using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomLiteBleDesktop.Database
{
    public class BleContext : DbContext
    {
        public DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=blogging.db");


        public static void DbInitRecord()
        {
            using (var db = new BleContext())
            {
                //db.Database.EnsureCreated();
                //var people = Enumerable.Range(1, 100).Select(x => new Post { Title = $"tanaka {x}" }).ToArray();
                db.Posts.Add(new Post
                {
                    ServerName = "ESP32PIRTRI",
                    ServiceUUID = "e72609f6-2bcb-4fb0-824a-5276ec9e355d",
                    CharacteristicUUID = "cca99442-dab6-4f69-8bc2-685e2412d178",
                    NumberSound=0
                });
                db.Posts.Add(new Post
                {
                    ServerName = "M5STACKTRI",
                    ServiceUUID = "ee007086-0dc9-4a48-b381-0f9e56d8c597",
                    CharacteristicUUID = "245c84dc-9422-41fb-bbf9-ddcd7da28120",
                    NumberSound = 1
                });
                db.SaveChanges();
            }
        }

        /// <summary>
        /// DBのレコードデータを修正します
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public static void DbSetRecord(int id,Post data)
        {
            using (var retdb = new BleContext())
            {
                var record = from b in retdb.Posts
                             where b.PostId.Equals(id)
                             select b;
                var fristFindData=record.First();
                fristFindData.ServerName = data.ServerName;
                fristFindData.ServiceUUID = data.ServiceUUID;
                fristFindData.CharacteristicUUID = data.CharacteristicUUID;
                fristFindData.NumberSound = data.NumberSound;
                retdb.SaveChanges();
            }
        }

        /// <summary>
        /// デバイス名より、最初に該当したレコードを返します
        /// </summary>
        /// <param name="DeviceName"></param>
        /// <returns></returns>
        public static Post GetServerPost(string DeviceName)
        {
            using (var retdb = new BleContext())
            {
                var record = from b in retdb.Posts
                             where b.ServerName.Equals(DeviceName)
                             select b;

                return record.First();
                //var ret = retdb.Posts.ToArray();
            }
        }

        public static List<Post> GetServerPosts()
        {
            using (var retdb = new BleContext())
            {
                var ret = retdb.Posts.ToArray();
                var output = new List<Post>();
                foreach (var retPost in ret)
                {
                    output.Add(retPost);
                }
                return output;
                //var ret = retdb.Posts.ToArray();
            }
        }

        public static List<string> GetServerNames()
        {
            using (var retdb = new BleContext())
            {
                var ret = retdb.Posts.ToArray();
                var output =new  List<string>();
                foreach(var retPost in ret)
                {
                    output.Add(retPost.ServerName);
                }
                return output;
            }
        }
    }
}
