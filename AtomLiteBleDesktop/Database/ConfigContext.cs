using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AtomLiteBleDesktop.Database
{
    public class ConfigContext : DbContext
    {

        
        public DbSet<ConfigColumns> Settings { get; set; }


        public ConfigContext()
        {
            //ChangeTracker.StateChanged += UpdateTimestamps;
            //ChangeTracker.Tracked += UpdateTimestamps;

        }

        public static void DbInitRecord()
        {
            using (var db = new ConfigContext())
            {
                db.Settings.Add(new ConfigColumns
                {
                    IsSendingKeepAlive=true,
                    SelectPortName="Com0"
                });
                db.SaveChanges();
            }
        }
        
        

        /// <summary>
        /// ChangeTracker.StateChanged イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UpdateTimestamps(object sender, EntityEntryEventArgs e)
        {
            if (e.Entry.Entity is IHasTimestamps entityWithTimestamps)
            {
                switch (e.Entry.State)
                {
                    case EntityState.Modified:
                        entityWithTimestamps.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Added:
                        entityWithTimestamps.UpdatedAt = DateTime.UtcNow;
                        entityWithTimestamps.CreatedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
        

    }
}
