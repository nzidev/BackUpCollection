using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Подключение всех таблиц для EF
    /// </summary>
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Delay> Delays { get; set; }
        public DbSet<Frequency> Frequencies { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<MainEvent> MainEvents { get; set; }
        public DbSet<MasterServer> MasterServers { get; set; }
        public DbSet<MediaServer> MediaServers { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<BackupLog> BackupLogs { get; set; }
        public DbSet<ADOConnectionString> ADOConnectionStrings { get; set; }
        public DbSet<MailSetting> MailSettings { get; set; }
        public DbSet<ServiceSetting> ServiceSettings { get; set; }


        /// <summary>
        /// В случае если база данных отсутствует создаются первые данные в таблицы
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Delay>().HasData(new Delay
            {
                DelayId = 1,
                Name = ""
            });

            modelBuilder.Entity<Frequency>().HasData(new Frequency
            {
                FrequencyId = 1,
                Name = ""
            });
        }




    }
}
