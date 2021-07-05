using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BackUpCollectionDAL.DataBase;
using BackUpCollectionDAL.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace BackUpCollectionManager
{
    public class Worker : BackgroundService
    {
        
        private readonly IServiceScopeFactory scopeFactory;
        private Dictionary<string, int> servicesAndPids;
        private System.Diagnostics.EventLog eventLog;
        private MailSettingRepository mailSettingRepository;
        private ADOConnectionStringRepository ADOConnectionStringRepository;
        private ServiceSettingRepository _serviceSettingRepository;

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

        
        /// <summary>
        /// Get path to directory service from registry
        /// </summary>
        /// <param name="serviceName">Name of service</param>
        /// <returns></returns>
        private static string GetServiceInstallPath(string serviceName)
        {
            RegistryKey regkey;
            regkey = Registry.LocalMachine.OpenSubKey(string.Format(@"SYSTEM\CurrentControlSet\Services\{0}", serviceName));

            if (regkey.GetValue("ImagePath") == null)
                return "Not found";
            else
            {
                int pos = regkey.GetValue("ImagePath").ToString().LastIndexOf(@"\") +1 ;
                return regkey.GetValue("ImagePath").ToString().Substring(0, pos);
            }
                
        }

        private static List<string> GetServices()
        {
            return Registry.LocalMachine.OpenSubKey(string.Format(@"SYSTEM\CurrentControlSet\Services\")).GetSubKeyNames().ToList().Where(x => x.StartsWith("BackupCollection")).ToList();
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            EventLogWrite(String.Format("Старт менеджера"), 101);

            //services from appsetting file
            servicesAndPids = new Dictionary<string, int>();

            //читаем настройки из appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .Build();

            var ConnectionStrings = configuration.GetSection("ConnectionStrings").GetChildren().OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            

            var ServicesAppSetting = configuration.GetSection("Services").GetChildren().ToList()
               .Select(x => (x.GetValue<string>("Name"),
                          x.GetSection("ADOConnectionStrings").GetChildren().ToDictionary(x => x.Key, x => x.Value),
                          x.GetValue<int>("DelayMs"),
                          x.GetValue<bool>("UpdateDelayAndFrequency"),
                          x.GetValue<string>("Mode"),
                          x.GetValue<byte>("Type"),
                          x.GetValue<byte>("HalfPolicies")
                          )
               ).ToList<(string Name, Dictionary<string, string> ADOConnectionStrings, int DelayMs, bool UpdateDelayAndFrequency, string Mode, byte Type, byte HalfPolicies)>();

            int MaxMemorySizeMb = configuration.GetSection("MaxMemorySizeMb").Get<int>();
            int DelayMS = configuration.GetSection("DelayMs").Get<int>();



            MailSetting mailSetting = new MailSetting
            {
                Name = configuration.GetSection("Mail:MailName").Get<string>(),
                Server = configuration.GetSection("Mail:Server").Get<string>(),
                ToAddress = configuration.GetSection("Mail:ToAddress").Get<string>(),
                FromAddress = configuration.GetSection("Mail:FromAddress").Get<string>()
            };





            EventLogWrite(String.Format("Остановка всех запущенных служб"), 102);
            
            //stop every backup service
            foreach (var serviceName in ServicesAppSetting)
            {
                try
                {
                    ServiceController service = new ServiceController(serviceName.Name);
                    if (service.Status != ServiceControllerStatus.Stopped)
                        service.Stop();
                }
                catch
                {

                }
            }

            EventLogWrite(String.Format("Старт всех служб"), 103);
            //Запускаем Delay и добавляем в словарь его процесс
            DelayService(configuration);

            var delayProc = Process.GetProcessesByName("BackUpCollectionServiceDelay")[0];

            //if NOT contain value and key
            if (!servicesAndPids.ContainsValue(delayProc.Id) && !servicesAndPids.ContainsKey("BackUpCollectionDelay"))
            {
                servicesAndPids.Add("BackUpCollectionDelay", delayProc.Id);
            }

            foreach (var connectionString in ConnectionStrings)
            {

                try
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
                        mailSettingRepository = new MailSettingRepository(dbContext);
                        ADOConnectionStringRepository = new ADOConnectionStringRepository(dbContext);

                        _serviceSettingRepository = new ServiceSettingRepository(dbContext);
                        _serviceSettingRepository.Clear();

                        mailSettingRepository.AddOrUpdate(mailSetting);

                        foreach (var serviceAppSetting in ServicesAppSetting)
                        {
                            WriteToSQLnAppSettingFile(serviceAppSetting, mailSetting);


                            //start service
                            ServiceController service = new ServiceController(serviceAppSetting.Name);
                            if (service.Status == ServiceControllerStatus.Stopped)
                                service.Start();

                            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
                            var proccesses = Process.GetProcessesByName("BackUpCollectionService");
                            foreach (var proc in proccesses)
                            {
                                //if NOT contain value and key
                                if (!servicesAndPids.ContainsValue(proc.Id) && !servicesAndPids.ContainsKey(serviceAppSetting.Name))
                                {
                                    servicesAndPids.Add(serviceAppSetting.Name, proc.Id);
                                }
                            }

                        }


                        
                    }
                }
                catch (Exception ex)
                {
                    EventLogWrite(ex.ToString(), 190);
                }
            }


            EventLogWrite(String.Format("Режим слежения за процессами"), 104);
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var serviceAndPid in servicesAndPids.ToList())
                {
                    
                    ServiceController service = new ServiceController(serviceAndPid.Key);
                    if (service.Status == ServiceControllerStatus.Stopped)
                        continue;
                    else
                    {
                        Process process = Process.GetProcessById(serviceAndPid.Value);
                        process.Refresh();
                        if (process.PrivateMemorySize64 / 1024 / 1024 > MaxMemorySizeMb)
                        {
                            EventLogWrite(String.Format("Служба {0} потребляет много памяти, перезапускаем", serviceAndPid.Key), 171);
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromMinutes(3));
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
                           

                            var names = new[] { "BackUpCollectionService", "BackUpCollectionServiceDelay" };
                            var proccesses = names.SelectMany(name => Process.GetProcessesByName(name)).ToArray();


                            foreach (var proc in proccesses)
                            {
                                //if NOT contain value
                                if (!servicesAndPids.ContainsValue(proc.Id))
                                {
                                    servicesAndPids[serviceAndPid.Key] = proc.Id;
                                }
                            }
                        }
                       
                    }
                    
                }
                EventLogWrite(String.Format("Менеджер ждет"), 104);
                await Task.Delay(DelayMS, stoppingToken);

                
            }
        }

        private void WriteToSQLnAppSettingFile((string Name, Dictionary<string, string> ADOConnectionStrings, int DelayMs, bool UpdateDelayAndFrequency, string Mode, byte Type, byte HalfPolicies) serviceAppSetting, MailSetting mailSetting)
        {
            
            foreach (var ADOConStr in serviceAppSetting.ADOConnectionStrings)
            {
                ADOConnectionString aDOConnectionString = new ADOConnectionString
                {
                    Name = ADOConStr.Key,
                    ConnectionString = ADOConStr.Value,
                    isMailSend = false
                };
                ADOConnectionStringRepository.AddOrUpdate(aDOConnectionString);

                ServiceSetting serviceSetting = new ServiceSetting
                {
                    ServiceName = serviceAppSetting.Name,
                    DelayMs = serviceAppSetting.DelayMs,
                    HalfPolicies = serviceAppSetting.HalfPolicies,
                    Mode = serviceAppSetting.Mode,
                    Type = serviceAppSetting.Type,
                    UpdateDelayAndFrequency = serviceAppSetting.UpdateDelayAndFrequency,
                    MailSetting = mailSettingRepository.GetByName(mailSetting.Name),
                    ADOConnectionString = ADOConnectionStringRepository.GetByName(aDOConnectionString.Name)
                   
                };
                //serviceSetting.ADOConnectionStringId = serviceSetting.ADOConnectionString.ADOConnectionStringId;

                _serviceSettingRepository.Add(serviceSetting);

            }
        }

        
        private void DelayService(IConfigurationRoot configuration)
        {
            EventLogWrite(String.Format("Старт DelayService"), 103);
            DelayServiceSetting delayServiceSetting = new DelayServiceSetting();

            delayServiceSetting.ServiceName = "BackUpCollectionDelay";
            delayServiceSetting.DelayMs = configuration.GetSection(String.Format("{0}:DelayMs", delayServiceSetting.ServiceName)).Get<int>();
            delayServiceSetting.PolicyDays = configuration.GetSection(String.Format("{0}:PolicyDays", delayServiceSetting.ServiceName)).Get<int>();
            delayServiceSetting.ConnectionStrings = configuration.GetSection("ConnectionStrings").GetChildren().OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value); ;
            string settingPath = GetServiceInstallPath(delayServiceSetting.ServiceName);
            string json = JsonConvert.SerializeObject(delayServiceSetting, Formatting.Indented);
            System.IO.File.WriteAllText(settingPath + "appsettings.json", json);
            //start service
            ServiceController service = new ServiceController(delayServiceSetting.ServiceName);

            if(service.Status == ServiceControllerStatus.Running)
            { 
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(3));
            }
            if(service.Status == ServiceControllerStatus.Stopped)
                service.Start();            
        }

        //Делим сообщения журнала по типу
        public void EventLogWrite(string message, int eventId)
        {
            if (eventId >= 190)
                eventLog.WriteEntry(message, EventLogEntryType.Error, eventId);
            else if (eventId >= 170)
                eventLog.WriteEntry(message, EventLogEntryType.Warning, eventId);
            else
                eventLog.WriteEntry(message, EventLogEntryType.Information, eventId);
        }
        public override void Dispose()
        {
            EventLogWrite(String.Format("Остановка Менеджера"), 105);
            foreach (var serviceAndPid in servicesAndPids.ToList())
            {

                ServiceController service = new ServiceController(serviceAndPid.Key);
                if (service.Status == ServiceControllerStatus.Stopped)
                    continue;
                else
                {
                    service.Stop();
                }
            }
            
            base.Dispose();
        }
    }
}
