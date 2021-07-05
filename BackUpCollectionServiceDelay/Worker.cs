using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackUpCollectionDAL.DataBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackUpCollectionServiceDelay
{
    public class Worker : BackgroundService, IDisposable
    {
        private UpdateDB updateDB;
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory scopeFactory;
        private System.Diagnostics.EventLog eventLog;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("BackUpCollection"))
            {
                System.Diagnostics.EventLog.CreateEventSource("BackUpCollection", "BackUpCollection Log");
            }

            eventLog.Source = "BackUpCollection";
            eventLog.Log = "BackUpCollection Log";
            this.scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //читаем настройки из appsettings.json
                var configuration = new ConfigurationBuilder()
                  .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                  .AddJsonFile("appsettings.json", false)
                  .Build();
                var conStrJson = configuration.GetSection("ADOConnectionStrings").GetChildren().ToDictionary(x => x.Key, x => x.Value);
                var DelyMS = configuration.GetSection("DelayMs").Get<int>();
                var PolicyDays = configuration.GetSection("PolicyDays").Get<int>();
                updateDB = new UpdateDB();
                updateDB.Notify += EventLogWrite;
                try
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
                        updateDB.Start(dbContext, PolicyDays);
                        // GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    EventLogWrite(ex.ToString(), 299);
                    EventLogWrite(String.Format("BackUpCollectionServiceDelay отработал с ошибкой." + ex.ToString()), 205);
                }
                EventLogWrite(String.Format("BackUpCollectionServiceDelay отработал."), 204);
                
                await Task.Delay(DelyMS, stoppingToken);
            }
        }
        public override void Dispose()
        {
            updateDB.Dispose();
            base.Dispose();
        }


        public void EventLogWrite(string message, int eventId)
        {
            eventLog.WriteEntry(message, EventLogEntryType.Information, eventId++);
        }
    }
}
