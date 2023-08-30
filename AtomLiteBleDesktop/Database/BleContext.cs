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

        public static Post GetServerPosts(string DeviceName)
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

    public class Post
    {
        public int PostId { get; set; }
        public string ServerName { get; set; }
        public string ServiceUUID { get; set; }
        public string CharacteristicUUID { get; set; }
        
        public int NumberSound { get; set; }

    }
}
