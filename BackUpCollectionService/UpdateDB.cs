using BackUpCollectionDAL.DataBase;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Data.Odbc;
using System.Linq;
using BackUpCollectionDAL.Repository;

using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Net.Mail;
using BackUpCollectionDAL.Extensions;

namespace BackUpCollectionService
//***************************************************
//Должен быть установлен SAP SQL Anywhere 16 драйвер
//https://archive.sap.com/documents/docs/DOC-35857
//***************************************************
{
    public class UpdateDB
    {
        public delegate void DbHandler(string message, int eventId);
        public event DbHandler Notify;

        private MainEventRepository mainEventRepository;
        private LocationRepository locationRepository;
        private ClientRepository clientRepository;
        private MasterServerRepository masterServerRepository;
        private MediaServerRepository mediaServerRepository;
        private PolicyRepository policyRepository;
        private ProgressRepository progressRepository;
        private ServiceSettingRepository serviceSettingRepository;
        private ADOConnectionStringRepository aDOConnectionStringRepository;
        
        private Regex regex;
        
        public List<string> CatalogHotBackup;
        public void Start(CoreDbContext contextDb, ServiceSetting serviceSetting)
        {
            
            mainEventRepository = new MainEventRepository(contextDb);
            locationRepository = new LocationRepository(contextDb);
            clientRepository = new ClientRepository(contextDb);
            masterServerRepository = new MasterServerRepository(contextDb);
            mediaServerRepository = new MediaServerRepository(contextDb);
            policyRepository = new PolicyRepository(contextDb);
            progressRepository = new ProgressRepository(contextDb);
            serviceSettingRepository = new ServiceSettingRepository(contextDb);
            aDOConnectionStringRepository = new ADOConnectionStringRepository(contextDb);

            regex = new Regex(".*Host=(.*);Server.*");


            Notify?.Invoke("start " + serviceSetting.ADOConnectionString.Name, 1);
            string serverName = regex.Match(serviceSetting.ADOConnectionString.ConnectionString).Groups[1].Value;
            
            //Добавляем пароль к строке соединения

            string connectionString = serviceSetting.ADOConnectionString.ConnectionString;


            string protectPassword = connectionString.Split(";").Where(x => x.StartsWith("Pwd=")).FirstOrDefault().ToString().Substring(4);
          string unprotectPassword =  SecurityStringManager.Unprotect(protectPassword);
        
             connectionString = connectionString.Replace(protectPassword, unprotectPassword);

            //пример строки  --  "ADOConnection": "Driver=SQL Anywhere 16;Host=;Server=;Database=;Uid=;Pwd=;Port="
           

            if (string.IsNullOrEmpty(connectionString))
            {
                Notify?.Invoke("Строка подключения к OpsCenter пустая", 98);
                throw new ArgumentException("No connection string in config.json");
            }

            //Обновляем список мастер-серверов
            string selectMasterservers = "SELECT id,networkName FROM domain_MasterServer";
            IEnumerable<DataRow> masterservers = ConnectToOpscenter(connectionString, selectMasterservers);

            //Если не подключился пробуем отремонтировать 
            if (masterservers == null)
            {
               
                Notify?.Invoke("Пробуем отремонтировать - " + regex.Match(connectionString).Groups[1].Value, 97);
                try { 
                    List<string> dsnlist = new List<string>();
                    dsnlist.AddRange(EnumDsn(Registry.CurrentUser, regex.Match(connectionString).Groups[1].Value));

                    foreach (var dsn in dsnlist)
                    {
                        Notify?.Invoke("Открываем - " + dsn, 97);
                        OdbcConnection connection = new OdbcConnection("DSN=" + dsn);
                        connection.Open();
                        connection.Close();
                    }
                    
                    masterservers = ConnectToOpscenter(connectionString, selectMasterservers);
                }
                catch
                {
                    if (serviceSetting.ADOConnectionString.isMailSend == false)
                    {
                        if (CreateMessage(serviceSetting, "Не удалось отремонтировать OdbcConnection", "Не удалось отремонтировать OdbcConnection " + serviceSetting.ADOConnectionString.Name + " " + serviceSetting.ADOConnectionString.ConnectionString))
                        {
                            serviceSetting.ADOConnectionString.isMailSend = true;
                            aDOConnectionStringRepository.UpdateMailSend(serviceSetting);
                        }
                    }
                    throw new ArgumentException("Не удалось отремонтировать OdbcConnection");
                }
            }
            else //если список серверов есть, добавляем их в таблицу. Добавляются только новые
            {
                //Отправляем сообщение об успехе если этого еще не было
                if (serviceSetting.ADOConnectionString.isMailSend == true)
                {
                    if (CreateMessage(serviceSetting, "Отремонтировали OdbcConnection", "Успешно удалось отремонтировать OdbcConnection " + serviceSetting.ADOConnectionString.Name))
                    {
                        serviceSetting.ADOConnectionString.isMailSend = false;
                        aDOConnectionStringRepository.UpdateMailSend(serviceSetting);
                    }
                }
                foreach (var mServer in masterservers)
                {
                    int serverId = Convert.ToInt32(mServer.ItemArray[0]);
                    string Name = mServer.ItemArray[1].ToString();
                    masterServerRepository.SetServerName(serverId, Name, serverName);

                }
            }

            

                //выбираем все имеющиеся политики
                string selectSQL = "SELECT policyName FROM domain_Job "; 
            if (serviceSetting.Type == 1)
                selectSQL += "where policyName NOT LIKE '%Tran%' AND policyName NOT LIKE '%tran%' AND policyName NOT LIKE '%Archlog%' AND policyName NOT LIKE '%archlog%'";
            else if(serviceSetting.Type == 2)
                selectSQL += "where policyName LIKE '%Tran%' OR policyName LIKE '%tran%'";
            else if (serviceSetting.Type == 4)
                selectSQL += "where policyName LIKE '%Archlog%' OR policyName LIKE '%archlog%'";
            else if (serviceSetting.Type == 5)
                selectSQL += "where policyName NOT LIKE '%Tran%' AND policyName NOT LIKE '%tran%'";
            else if (serviceSetting.Type == 6)
                selectSQL += "where policyName LIKE '%Tran%' OR policyName LIKE '%tran%' OR policyName  LIKE '%Archlog%' OR policyName  LIKE '%archlog%'";

            selectSQL = selectSQL + " GROUP BY policyName ORDER BY policyName";
            var policies = ConnectToOpscenter(connectionString, selectSQL);

            //Если в настройках обработка только половины
            if (serviceSetting.HalfPolicies == 1)
                policies = policies.Take(policies.Count()/2);
            else if (serviceSetting.HalfPolicies == 2)
                policies = policies.Skip(policies.Count() / 2);

            if (policies==null)
            {
                Notify?.Invoke("Нет коннекта к OpsCenter - " + regex.Match(connectionString).Groups[1].Value, 96);
                //throw new ArgumentException("No connection to OpsCenter");
            }
            else
            {
                int currentnum = 1;
                foreach (var policy in policies)
                {
                    //Статус прогресса записывается в базу и отображается на сайте
                    progressRepository.SetProgress(serviceSetting.ServiceName, currentnum, policies.Count());
                    string policyName = policy.ItemArray[0].ToString();
                    //если имя не пустое (обычно первая запись пустая) и не содержит SLP или $
                    if (policyName != "" && !policyName.Contains("SLP") && !policyName.Contains("$"))
                    {

                        string progressMessage = currentnum + " из " + policies.Count();
                        // policyName = "";

                        WorkWithPolicy(connectionString, policyName, serviceSetting.ServiceName, progressMessage, serviceSetting.UpdateDelayAndFrequency, serviceSetting.Mode);
                    }
                    currentnum++;
                }
            }
        }
        
        /// <summary>
        /// Метод для получение заранее добавбленных серверов ODBC с именем "ops_"
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        private IEnumerable<string> EnumDsn(RegistryKey rootKey, string serverName)
        {
            RegistryKey regKey = rootKey.OpenSubKey(@"Software\ODBC\ODBC.INI\ODBC Data Sources");
            if (regKey != null)
            {
                foreach (string name in regKey.GetValueNames())
                {
                    if(name.Contains(serverName))
                    { 
                        string value = regKey.GetValue(name, "").ToString();
                        yield return name;
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатываем конкретную политику по имени
        /// </summary>
        /// <param name="connectionString">Строка подключения</param>
        /// <param name="policyName">Имя политики</param>
        /// <param name="progressMessage">Сообщение прогреса для журнала</param>
        /// <param name="UpdateDelayAndFrequencyFlag">Флаг обновлять частоту и периодичность сразу</param>
        /// <param name="ProgramMode">Режим работы</param>
        private void WorkWithPolicy(string connectionString, string policyName, string serviceName, string progressMessage = null, bool UpdateDelayAndFrequencyFlag = false, string ProgramMode = "0")
        {
            string serverName = regex.Match(connectionString).Groups[1].Value;     //serverName нужен для отображения в событиях для удобного чтения
            int locId = locationRepository.GetByServerName(serverName).LocationId; //locId нужен для скорости работы, чтобы каждый раз не преобразовывать имя сервера в id
                       
            string selectSQL;
                        
            Notify?.Invoke(String.Format("Стартую работу по потилике {0}. На площадке {1}. {2}. Служба {3}", policyName, serverName, progressMessage, serviceName), 2);
            var LastEvent = mainEventRepository.GetLastByName(policyRepository.GetByName(policyName), locId);
            if(LastEvent == null)
            {
                //такой политики еще нет в базе, добавляем все ее события
                 selectSQL = String.Format("SELECT  id as 'Job_ID', clientName as 'Client', mediaServerName as 'MediaServer',MasterServerID as 'MasterServerID',scheduletype as 'Type', " +
                "UTCBigIntToNomTime(startTime) as 'StartTime', UTCBigIntToNomTime(endTime) as 'EndTime', statusCode as 'Status' ,policyName as 'PolicyName', " +
                "throughput as 'speed', bytesWritten as 'size', parentJobId as 'parentJobId' " +
                "FROM domain_Job " +
                "WHERE policyName='{0}' and StartTime > '2000/01/01 00:00:00' " +
                "ORDER BY id", policyName);
            }
            
            else
            {
                //такая политика уже есть в базе, надо проверить в нашей базе оно в процессе или завершилось
                if (mainEventRepository.isCurrentInProgress(policyName, serverName))
                {
                    string dateTime = DateTime.Now.AddDays(-1 * 60).ToString("yyyy/MM/dd/ HH:mm:ss"); // больше 60 дней в OpsCentr нет информации
                    MainEvent mainEventFirstInProg = mainEventRepository.GetFirstInProgress(policyRepository.GetById(LastEvent.PolicyId).Name, serverName);
                    selectSQL = String.Format(
                                       "SELECT id as 'Job_ID', clientName as 'Client', mediaServerName as 'MediaServer',MasterServerID as 'MasterServerID',scheduletype as 'Type', " +
                                   "UTCBigIntToNomTime(startTime) as 'StartTime', UTCBigIntToNomTime(endTime) as 'EndTime', statusCode as 'Status' ,policyName as 'PolicyName', " +
                                   "throughput as 'speed', bytesWritten as 'size', parentJobId as 'parentJobId'" +
                                   "FROM domain_Job " +
                                   "WHERE policyName='{0}' and Job_ID >= '{1}' and StartTime > '{2}' " +
                                   "ORDER BY id", policyName, mainEventFirstInProg.JobID, dateTime);

                }
                else 
                { 
                    //в базе событий есть такая политика, добавляем не добавленные новые события
                    selectSQL = String.Format(
                        "SELECT  id as 'Job_ID', clientName as 'Client', mediaServerName as 'MediaServer',MasterServerID as 'MasterServerID',scheduletype as 'Type', " +
                    "UTCBigIntToNomTime(startTime) as 'StartTime', UTCBigIntToNomTime(endTime) as 'EndTime', statusCode as 'Status' ,policyName as 'PolicyName', " +
                    "throughput as 'speed', bytesWritten as 'size', parentJobId as 'parentJobId'" +
                    "FROM domain_Job " +
                    "WHERE policyName='{0}' and StartTime > '{1}'" +
                    "ORDER BY id", policyRepository.GetById(LastEvent.PolicyId).Name, LastEvent.StartTime.ToString("yyyy'/'MM'/'dd HH:mm:ss"));
                }
            }

            if(selectSQL != null)
            {
                Policy policyTmp = policyRepository.GetByName(policyName);
                if (ProgramMode == "0" || ProgramMode == "1" || ProgramMode == "12")
                    AddAndUpdatesPolicies(connectionString, selectSQL, policyTmp, serverName, locId);
                if (ProgramMode == "0" || ProgramMode == "2" || ProgramMode == "12")
                    //проверим не зависла ли политика
                    CheckFreezePolicy(policyTmp, serverName, connectionString, locId);

                if (ProgramMode == "0" || ProgramMode == "3" || ProgramMode == "345")
                    //проверим всех у кого не обновился родитель
                    CheckUpdateParents(policyTmp, serverName, locId);
                
                if (ProgramMode == "0" || ProgramMode == "4" || ProgramMode == "345")
                    //проверим всех у кого не обновился размер
                    CheckSizePolicy(policyTmp, serverName, locId);

                //Отдельная проверка по LSN 
                //в данных политиках образуется дерево:
                //Политика запускает подителя - обычно медиа сервер
                //Который запускает подродителя - обычно *lsn* сервер
                //Который бэкапит клиента - вот этого клиента необходимо вычислить и подставить в ClientId политики.
                if (policyName.Contains("lsn") && (ProgramMode == "0" || ProgramMode == "5" || ProgramMode == "345"))
                {
                    CheckLSNPolicy(policyName, serverName);
                }
            }
            Notify?.Invoke(String.Format("По политике {0} готово. На площадке {1}", policyName, serverName), 3);
        }

        private void AddAndUpdatesPolicies(string connectionString, string selectSQL, Policy policy, string serverName, int locId)
        {
            IEnumerable<DataRow> policyEvents;
            policyEvents = ConnectToOpscenter(connectionString, selectSQL);
            if (policyEvents != null && policyEvents.Count() > 0)
            {

                Notify?.Invoke(String.Format("Добавляю новые и обновляю старые события по потилике {0}. На площадке {1}", policy.Name, serverName), 11);
                foreach (var pEvent in policyEvents)
                {
                    MainEvent mEvent = DatarowToEvent(connectionString, pEvent);

                    //Notify?.Invoke(String.Format("Проверка {1} - time: {2} - status: {3}. На площадке {0}", serverName, mEvent.JobID, mEvent.StartTime, mEvent.Status), 6);
                    if (mainEventRepository.HaveThis(mEvent, locId))
                    {
                        UpdateEvent(mEvent, locId);
                    }
                    else
                    {
                        AddNewEvent(mEvent);
                    }

                }

                Notify?.Invoke(String.Format("Обновление размера добавленных событий {0}. На площадке {1}", policy.Name, serverName), 12);
                foreach (var pEvent in policyEvents.OrderByDescending(x => Convert.ToInt32(x.ItemArray[0])))
                {
                    
                    mainEventRepository.UpdateParrentSize(DatarowToEvent(connectionString, pEvent), locId);
                    //if(UpdateDelayAndFrequencyFlag)   ///TODO: переработать метод
                    //    mainEventRepository.UpdateDelayAndFrequency(Convert.ToInt32(pEvent.ItemArray[0]), serverName, LastEvent);
                }
            }
        }

        private void CheckUpdateParents(Policy policy, string serverName, int locId)
        {
            //проверим всех у кого не обновился родитель
            Notify?.Invoke(String.Format("Проверим всех у кого не обновился родитель {0}. На площадке {1}", policy.Name, serverName), 31);
           
            foreach (var pEvent in mainEventRepository.GetWithoutParent(policy, serverName).OrderBy(x => x.MainEventId))
            {

                pEvent.ParrentId = mainEventRepository.FindParent(pEvent);
                if (pEvent.ParrentId == -2)
                {
                    Notify?.Invoke(String.Format("У события {0} два родителя. У политики {1}. На площадке {2}", pEvent.StartTime, policy.Name, serverName), 71); //warning
                }
                if (pEvent.ParrentId != -1)
                { 
                    UpdateEvent(pEvent, locId);
                }
            }
            mainEventRepository.TypeTwoIsParrent(policy, serverName);
            
        }

        private void CheckFreezePolicy(Policy policy, string serverName, string connectionString, int locId)
        {
            //проверим не зависла ли политика
            Notify?.Invoke(String.Format("Проверка не зависла ли политики {0}. На площадке {1}", policy.Name, serverName), 21);
            foreach (var parent in mainEventRepository.GetAllProgressParentByLocalName(policy, locId))
            {
                string selectSQL = String.Format(
                                       "SELECT  id as 'Job_ID', clientName as 'Client', mediaServerName as 'MediaServer',MasterServerID as 'MasterServerID',scheduletype as 'Type', " +
                                   "UTCBigIntToNomTime(startTime) as 'StartTime', UTCBigIntToNomTime(endTime) as 'EndTime', statusCode as 'Status' ,policyName as 'PolicyName', " +
                                   "throughput as 'speed', bytesWritten as 'size', parentJobId as 'parentJobId'" +
                                   "FROM domain_Job " +
                                   "WHERE policyName='{0}' and Job_ID = '{1}' and MasterServerID = {2} " +
                                   "ORDER BY id", policy.Name, parent.JobID, parent.MasterServerId);
                IEnumerable<DataRow> policyEvents = ConnectToOpscenter(connectionString, selectSQL);
                if (policyEvents != null)
                {
                    foreach (var pEvent in policyEvents)
                    {
                        MainEvent mainEvent = DatarowToEvent(connectionString, pEvent);
                        //Проверяем зависла ли эта политика
                        if (mainEvent.EndTime == DateTime.Parse("1970-01-01 03:00:00.0000000") && mainEventRepository.isNotLastParent(mainEvent))
                        {
                            mainEvent.EndTime = DateTime.Parse("1986-10-09 03:00:00.0000000");
                        }
                        UpdateEvent(mainEvent, locId);
                    }
                }

            }

        }

        private void CheckSizePolicy(Policy policy, string serverName, int locId)
        {
            //проверим всех у кого не обновился размер
            Notify?.Invoke(String.Format("Проверим всех у кого не обновился размер {0}. На площадке {1}", policy.Name, serverName), 41);
            foreach (var pEvent in mainEventRepository.GetWithParentAndWithoutSize(policy.Name, locId))
            {
                foreach (var pEventChild in mainEventRepository.GetAllChildren(pEvent).Where(x => x.Size > 0))
                {
                    
                        mainEventRepository.UpdateParrentSize(pEventChild, locId);
                    
                }
            }
        }

        private void CheckLSNPolicy(string policyName, string serverName)
        {
            //Отдельная проверка по LSN 
            //в данных политиках образуется дерево:
            //Политика запускает подителя - обычно медиа сервер
            //Который запускает подродителя - обычно *lsn* сервер
            //Который бэкапит клиента - вот этого клиента необходимо вычислить и подставить в ClientId политики.
            Notify?.Invoke(String.Format("Отдельная проверка по политикам LSN {0}. На площадке {1}", policyName, serverName), 51);

            // список всех родителей, чье имя клиента это медиасервер
            var lsnParents = mainEventRepository.GetAllParent(policyName, serverName).Where(x => mediaServerRepository.isContain(x.Client.Name)).ToArray();
            foreach (var lsnParent in lsnParents)
            {
                //дети родителя являются подродителями
                var lsnParentsLevelTwo = mainEventRepository.GetAllChildren(lsnParent);
                List<string> lsnChildren = new List<string>();
                foreach (var lsnParentLevelTwo in lsnParentsLevelTwo)
                {
                    lsnChildren.AddRange(mainEventRepository.GetAllChildren(lsnParentLevelTwo).Select(x => x.Client.Name).ToList());
                }

                if (lsnChildren.Distinct().Count() == 1)
                {
                    mainEventRepository.ChangeClientId(lsnParent, clientRepository.GetByName(lsnChildren[0]).ClientId);
                }
            }
        }

        /// <summary>
        /// Метод для работы с OpsCenter получаем массив 
        /// </summary>
        /// <param name="connectionString">Строка подключения</param>
        /// <param name="SQLCommand">SQL запрос</param>
        /// <returns></returns>
        private IEnumerable<DataRow> ConnectToOpscenter(string connectionString, string SQLCommand)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                OdbcCommand command = new OdbcCommand(SQLCommand, connection);
                try
                {
                    connection.Open();
                    OdbcDataAdapter odbcAdapter = new OdbcDataAdapter();
                    odbcAdapter.SelectCommand = command;
                    //данные из OpsCenter сохранятся в DataTable
                    DataTable dt = new DataTable();
                    odbcAdapter.Fill(dt);

                    //список политик в массив
                    IEnumerable<DataRow> policies = dt.AsEnumerable().ToArray();
                    dt = null;
                    return policies;

                }
                catch
                {
                    return null;
                    // return error message
                }
            }
        }

       private MainEvent DatarowToEvent(string connectionString, DataRow pEvent)
        {
            MainEvent policyEvent = new MainEvent();
            policyEvent.Location = locationRepository.GetByServerName(regex.Match(connectionString).Groups[1].Value);
            policyEvent.LocationId = policyEvent.Location.LocationId;
            policyEvent.JobID = Convert.ToInt32(pEvent.ItemArray[0]);
            policyEvent.Client = clientRepository.GetByName(Convert.ToString(pEvent.ItemArray[1]));
            policyEvent.ClientId = policyEvent.Client.ClientId;
            policyEvent.MediaServer = mediaServerRepository.GetByName(Convert.ToString(pEvent.ItemArray[2]));
            policyEvent.MediaServerId = policyEvent.MediaServer.MediaServerId;
            policyEvent.MasterServer = masterServerRepository.GetById(Convert.ToInt32(pEvent.ItemArray[3]), regex.Match(connectionString).Groups[1].Value);
            policyEvent.MasterServerId = policyEvent.MasterServer.MasterServerId;
            policyEvent.Type = Convert.ToInt32(pEvent.ItemArray[4]);
            policyEvent.StartTime = Convert.ToDateTime(pEvent.ItemArray[5]);
            policyEvent.EndTime = Convert.ToDateTime(pEvent.ItemArray[6]);
            policyEvent.Status = Convert.ToInt64(pEvent.ItemArray[7]);
            policyEvent.Policy = policyRepository.GetByName(Convert.ToString(pEvent.ItemArray[8]));
            policyEvent.PolicyId = policyEvent.Policy.PolicyId;
            policyEvent.Speed = Convert.ToInt64(pEvent.ItemArray[9]);
            policyEvent.Size = Convert.ToInt64(pEvent.ItemArray[10]);
            policyEvent.DelayId = 1;
            policyEvent.FrequencyId = 1;
            policyEvent.ParrentId = Convert.ToInt32(pEvent.ItemArray[11]);
            if (policyEvent.JobID == policyEvent.ParrentId && policyEvent.Type != 2)
            {
                policyEvent.isParent = true;
            }
            if(policyEvent.Type == 2 && policyEvent.EndTime > DateTime.Parse("1999-01-01 03:00:00.0000000"))
            {
                try { 
                    policyEvent.ParrentId = mainEventRepository.FindParent(policyEvent);
                    if (policyEvent.ParrentId == -1 && policyEvent.Type == 2)
                    {
                        policyEvent.ParrentId = Convert.ToInt32(pEvent.ItemArray[11]);
                    }
                    if (policyEvent.ParrentId == -2)
                    {
                        Notify?.Invoke(String.Format("У события {0} два родителя. У политики {1}. На площадке {2}", policyEvent.StartTime, pEvent.ItemArray[8], regex.Match(connectionString).Groups[1].Value), 71);
                    }
                }
                catch
                {

                }
            }
            else if  (policyEvent.Type == 2)
            {
                policyEvent.ParrentId = -1;
            }
            return policyEvent;
        }

        private bool CreateMessage(ServiceSetting serviceSetting, string Subject, string Body)
        {
            
            string to = serviceSetting.MailSetting.ToAddress;
            string from = serviceSetting.MailSetting.FromAddress;

            MailMessage message = new MailMessage(from, to);
            message.Subject = Subject;
            message.Body = Body;
            SmtpClient client = new SmtpClient(serviceSetting.MailSetting.Server);
            client.UseDefaultCredentials = true;

            try
            {
                if (to != "")
                {
                    client.Send(message);
                    
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Не удалось отправить e-mail");
            }
        }

        private void AddNewEvent(MainEvent policyEvent)
        {
            
            mainEventRepository.setSize(policyEvent);
            mainEventRepository.SetTime(policyEvent);
            mainEventRepository.Add(policyEvent);
            
        }

        private void UpdateEvent(MainEvent policyEvent, int locId)
        {
            mainEventRepository.setSize(policyEvent);
            mainEventRepository.SetTime(policyEvent);
            mainEventRepository.UpdateEvent(mainEventRepository.GetById(policyEvent, locId),policyEvent);
        }

        public void Dispose()
        {
            Notify?.Invoke("Кто-то BackUpCollectionService стопит", 70);
            mainEventRepository = null;
        }
    }
}
