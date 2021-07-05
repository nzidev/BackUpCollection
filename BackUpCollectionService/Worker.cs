using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackUpCollectionDAL.DataBase;
using BackUpCollectionDAL.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace BackUpCollectionService
{
    public class Worker : BackgroundService, IDisposable
    {
        private UpdateDB updateDB;       
        private readonly IServiceScopeFactory scopeFactory;
        private System.Diagnostics.EventLog eventLog;
        

        public Worker(IServiceScopeFactory scopeFactory)
        {
            
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
                
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", false)
                    .Build();
                
                string serviceName = configuration.GetSection("ServiceName").Get<string>();                              
                bool FromFile = configuration.GetSection("FromFile").Get<bool>();
                bool FromSQL = configuration.GetSection("FromSQL").Get<bool>();


                if (FromFile && FromSQL)
                    FromFile = false;

                updateDB = new UpdateDB();
                updateDB.Notify += EventLogWrite;

                int DelayMs = 60000;


                try
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

                        ServiceSettingRepository serviceSettingRepository = new ServiceSettingRepository(dbContext);
                        //���� ��������� �� �����
                        if (FromFile)
                        {
                            ServiceSetting serviceSetting = new ServiceSetting();
                            serviceSetting = GetDataFromAppSetting(configuration);
                            var conStrJson = configuration.GetSection("ADOConnectionStrings").GetChildren().OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                            Random rand = new Random();
                            conStrJson = conStrJson.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);

                            ADOConnectionString aDOConnectionString = new ADOConnectionString();
                            foreach (var connectionString in conStrJson)
                            {
                                aDOConnectionString.ConnectionString = connectionString.Value;
                                aDOConnectionString.Name = connectionString.Key;
                                serviceSetting.ADOConnectionString = aDOConnectionString;
                                updateDB.Start(dbContext, serviceSetting);
                                DelayMs = serviceSetting.DelayMs;
                                EventLogWrite(String.Format("������ {0} ���������.", connectionString.Key), 4);
                            }

                        }
                        //���� ��������� �� ���� ������
                        if (FromSQL)
                        {
                            List<ServiceSetting> serviceSettings = serviceSettingRepository.GetByName(serviceName);
                            foreach(var serviceSetting in serviceSettings)
                            {
                               
                                    DelayMs = serviceSetting.DelayMs;
                                    updateDB.Start(dbContext, serviceSetting);
                                    
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventLogWrite(ex.ToString(), 99);
                }
                    
                    EventLogWrite(String.Format("������ ����������. ���� {0} ms", DelayMs), 5);
                    await Task.Delay(DelayMs, stoppingToken);
            }
        }

        private ServiceSetting GetDataFromAppSetting(IConfigurationRoot configuration)
        {
            ServiceSetting serviceSetting = new ServiceSetting();
            serviceSetting.ServiceName = configuration.GetSection("ServiceName").Get<string>();
            MailSetting mailSetting = new MailSetting();
            mailSetting.Server = configuration.GetSection("Mail:Server").Get<string>();
            mailSetting.ToAddress = configuration.GetSection("Mail:ToAddress").Get<string>();
            mailSetting.FromAddress = configuration.GetSection("Mail:FromAddress").Get<string>();

            serviceSetting.MailSetting = mailSetting;
            serviceSetting.UpdateDelayAndFrequency = configuration.GetSection("UpdateDelayAndFrequency").Get<bool>(); //��������\��������� ���������� ������� � �������������
            serviceSetting.Mode = configuration.GetSection("Mode").Get<string>(); // ������ ������: 0 - ��� ��������, 1 - ������ ���������, (2,3,4 - ������ ������������ ����������), 5 - LSN, 6 - ��� ����� ���������� � �������
            serviceSetting.Type = configuration.GetSection("Type").Get<byte>(); // ������: () 0 - ��� ��������, 1 - ��� ����� ������ � ��������, 2 - ������ �����, 4 - ������ �������, 5 -����� ������, 6 ���� + ������ 
            //��������� �� ������� - ���� �� �������� � ������������:
            //��� ���� ���������   ����
            //0    0     0          0    - ��� ��������
            //0    0     1          1    - ��� ����� ������ � ��������
            //0    1     0          2    - ������ �����
            //1    0     0          4    - ������ �������
            //1    0     1          5    - ����� ������
            //1    1     0          6    - ����� � �������
            serviceSetting.HalfPolicies = configuration.GetSection("HalfPolicies").Get<byte>(); // ����� ��������: 0 - ��� ��������, 1 - ������ ��������, 2 - ������ ��������
            serviceSetting.DelayMs = configuration.GetSection("DelayMs").Get<int>();
            return serviceSetting;
        }

        public override void Dispose()
        {
            updateDB.Dispose();
            base.Dispose();
        }
        

        public void EventLogWrite(string message, int eventId)
        {
            if(eventId >= 90)
                eventLog.WriteEntry(message, EventLogEntryType.Error, eventId);
            else if (eventId >= 70)
                eventLog.WriteEntry(message, EventLogEntryType.Warning, eventId);
            else
                eventLog.WriteEntry(message, EventLogEntryType.Information, eventId);
        }

    }
}
