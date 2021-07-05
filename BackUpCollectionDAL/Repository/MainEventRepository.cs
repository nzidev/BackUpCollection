using BackUpCollectionDAL.DataBase;
using BackUpCollectionDAL.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackUpCollectionDAL.Repository
{
    public class MainEventRepository
    {
        private  CoreDbContext context;

        public MainEventRepository(CoreDbContext _context)
        { 
            context = _context; 
        }

        /// <summary>
        /// Получить список записей по фильтру
        /// </summary>
        /// <param name="linqFilter"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetLast(LinqFilter linqFilter)
        {
            context.Database.SetCommandTimeout(60);
            IEnumerable<MainEvent> result = context.MainEvents
                .Include("Client")
                .Include("Policy")
                .Include("MasterServer")
                .Include("MediaServer")
                .Include("Delay")
                .Include("Frequency")
                .Include("Location")
                .Where(x => x.isParent == true
                    && (linqFilter.searchText != null ? (x.Client.Name.Contains(linqFilter.searchText) || x.Policy.Name.Contains(linqFilter.searchText)) : true)
                    && (linqFilter.locationId != null ? x.Location.ServerName.Contains(linqFilter.locationId) : true)
                    && (linqFilter.zone != null ? x.MasterServer.Description.Contains(linqFilter.zone) : true)
                    && (x.StartTime >= linqFilter.dtStart)
                   // && (x.StartTime <= linqFilter.dtEnd)
                    && (
                    ((linqFilter.IsProgress && !linqFilter.IsGood && !linqFilter.IsError) ? x.EndTime == DateTime.Parse("1970-01-01 03:00:00.0000000") : false) ||
                    ((linqFilter.IsProblem && !linqFilter.IsGood && !linqFilter.IsError) ? x.Size == 0 && x.EndTime > DateTime.Parse("1990-01-01 03:00:00.0000000") : false) ||
                    (linqFilter.IsProgress ? true : x.EndTime != DateTime.Parse("1970-01-01 03:00:00.0000000"))
                    && (linqFilter.IsError ? true : x.Status == 0)
                    && (linqFilter.IsGood ? true : x.Status != 0 )
                    )
                    //&& ((linqFilter.IsProgress && !linqFilter.IsGood && !linqFilter.IsError) ? true : x.EndTime != DateTime.Parse("1970-01-01 03:00:00.0000000"))
                    //&& (linqFilter.IsProblem ? true : x.Size != 0)
                );


            //if (linqFilter.searchText != null)
            //{

            //    result = result.Where(x => (x.Client.Name.Contains(linqFilter.searchText) || x.Policy.Name.Contains(linqFilter.searchText)));
            //}



            
            result = result.ToList(); //после toList страница грузится быстрее, быстрее проходит метод Count().

            result = result.OrderByDescending(x => x.StartTime);
            return result;
        }

        /// <summary>
        /// Обновление частоты дней
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="locId"></param>
        /// <param name="delayDayId"></param>
        /// <param name="days"></param>
        public void UpdatePolicyDelayDay(Policy policy, int locId, int delayDayId, int days)
        {
            var ListOfMain = context.MainEvents.Where(x => x.PolicyId == policy.PolicyId && x.LocationId == locId && x.isParent == true && x.StartTime > DateTime.Now.AddDays(-1 * days)).ToList();
            foreach (var entry in ListOfMain)
            {
                entry.DelayId = delayDayId;
            }
            
            context.Database.SetCommandTimeout(90);
            context.SaveChanges();
            
        }

        /// <summary>
        /// Обновление  частоты времени
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="locId"></param>
        /// <param name="FrequencyId"></param>
        /// <param name="days"></param>
        public void UpdatePolicyFrequency(Policy policy, int locId, int FrequencyId, int days)
        {
            foreach (var entry in context.MainEvents.Where(x => x.PolicyId == policy.PolicyId && x.LocationId == locId && x.isParent == true && x.StartTime > DateTime.Now.AddDays(-1 * days)).ToList())
            {
                entry.FrequencyId = FrequencyId;
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Чистка без родителей
        /// </summary>
        /// <param name="days"></param>
        public void ClearOldWithoutParents(int days)
        {
            DateTime dateTime = DateTime.Now.AddDays(-1 * days);
            List<MainEvent> listToDelete = context.MainEvents.Where(x => x.isParent == false && x.ParrentId == -1 && x.StartTime < dateTime).OrderByDescending(x => x.MainEventId).ToList();
            foreach(var itemToDel in listToDelete)
            {
                context.Entry(itemToDel).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Получить все Location по ID политики
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        public IEnumerable<int> getAllLocationByPolicyId(int policyId)
        {
            return context.MainEvents.Where(x => x.PolicyId == policyId).Select(p => p.LocationId).Distinct().ToArray();
        }

        /// <summary>
        /// Последнее событие по имени политики
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locId"></param>
        /// <returns></returns>
        public MainEvent GetLastByName(Policy policy, int locId)
        {
            if (policy != null)
            {
                var listOfPol = context.Policies.Where(x => x == policy).ToArray();
                if (listOfPol != null)
                {
                    foreach (var pol in listOfPol)
                    {
                        var temp = context.MainEvents.Where(x => x.PolicyId == pol.PolicyId && x.LocationId == locId).OrderByDescending(o => o.MainEventId).FirstOrDefault<MainEvent>();
                        if (temp != null)
                            return temp;
                    }
                    return null;
                }
                else
                    return null;
            }
            else
                return null;
            
        }

        /// <summary>
        /// Добавляем событие 
        /// </summary>
        /// <param name="policyEvent"></param>
        public void Add(MainEvent policyEvent)
        {
            context.Entry(policyEvent).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            context.SaveChanges();
        }

        /// <summary>
        /// Проверяем есть уже такое в базе
        /// </summary>
        /// <param name="JobID"></param>
        /// <param name="locationServerId"></param>
        /// <returns></returns>
        public bool HaveThis(MainEvent mainEvent, int locationServerId)
        {
            
            return context.MainEvents.Any(x => x.JobID == mainEvent.JobID && x.LocationId == locationServerId && x.PolicyId == mainEvent.PolicyId && x.MasterServerId == mainEvent.MasterServerId);
        }
        
        /// <summary>
        /// Получаем первое которое в процесс
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public MainEvent GetFirstInProgress(string policyName, string serverName)
        {
            return context.MainEvents.Where(x => x.Policy.Name == policyName && x.Location.ServerName == serverName
                                    && x.isParent == true && x.EndTime == DateTime.Parse("1970-01-01 03:00:00.0000000")).OrderByDescending(s => s.MainEventId).FirstOrDefault<MainEvent>();
        }

        /// <summary>
        /// Получаем словарь месяцев
        /// </summary>
        /// <returns></returns>
        public Dictionary<byte, string> getMonth()
        {
            Dictionary<byte, string> month = new Dictionary<byte, string>();
            for(byte i=1;i<=12;i++)
            {
                month.Add(i, GetMonthName(i));
            }
            return month;
        }

        public string GetMonthName(int num)
        {
            switch (num)
            {
                case 1:
                    return "Январь";
                case 2:
                    return "Февраль";
                case 3:
                    return "Март";
                case 4:
                    return "Апрель";
                case 5:
                    return "Май";
                case 6:
                    return "Июнь";
                case 7:
                    return "Июль";
                case 8:
                    return "Август";
                case 9:
                    return "Сентябрь";
                case 10:
                    return "Октябрь";
                case 11:
                    return "Ноябрь";
                case 12:
                    return "Декабрь";
                default:
                    return "";

            }
        }

        /// <summary>
        /// Получаем список имеющихся годов в базе, начиная с 2019
        /// </summary>
        /// <returns></returns>
        public List<int> getYear()
        {
            return context.MainEvents.Where(x=> x.StartTime.Year >= 2019).Select(x => x.StartTime.Year).Distinct().ToList();
        }

        public void UpdateParrentSize(MainEvent mainEvent, int locId)
        {
            try { 
            var child = context.MainEvents.Single(x => x.JobID == mainEvent.JobID && x.LocationId == locId && x.PolicyId == mainEvent.PolicyId  && x.MasterServerId == mainEvent.MasterServerId);
                
            var parrent = context.MainEvents.SingleOrDefault(x => x.JobID == child.ParrentId && x.LocationId == locId && x.PolicyId == child.PolicyId && x.MasterServerId == mainEvent.MasterServerId);
            
            if (parrent != null && parrent.JobID != child.JobID)
            { 
                parrent.Size += child.Size;
                setSize(parrent);
                context.SaveChanges();
            }
            }
            catch (Exception e)
                {
                throw new Exception("Main event" + mainEvent.MainEventId, e);
            }
        }

        /// <summary>
        /// В процессе ли текущая политика в базе
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locationServerName"></param>
        /// <returns></returns>
        public bool isCurrentInProgress(string policyName, string locationServerName)
        {
            var lastinProgress = context.MainEvents.Where(x => x.Policy.Name == policyName && x.Location.ServerName == locationServerName
                                    && x.isParent == true && x.EndTime == DateTime.Parse("1970-01-01 03:00:00.0000000")).OrderByDescending(s => s.MainEventId).FirstOrDefault<MainEvent>();

            if (lastinProgress != null)
            {
                if (context.MainEvents.Any(x => x.Policy.Name == policyName && x.Location.ServerName == locationServerName
                                         && x.isParent == true && x.StartTime > lastinProgress.StartTime))
                { return false; }
                else
                    return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Устанавливаем указанному событию размер в читаемом виде, этот метод не сохраняет в базе
        /// </summary>
        /// <param name="mainEvent">Событие</param>
        public void setSize(MainEvent mainEvent)
        {
            mainEvent.SizeHumanRead = SizeToHumanRead(mainEvent.Size);
        }

        /// <summary>
        /// Байты к читаемому виду "{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB"
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private string SizeToHumanRead(long size)
        {
            double s = size;
            string[] format = new string[] { "{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB" };
            int i = 0;
            while (i < format.Length && s >= 1024)
            {
                s = (long)(100 * s / 1024) / 100.0; i++;
            }
            return string.Format(format[i], s);
        }

        /// <summary>
        /// Вычисляем время выполнения события, этот метод не сохраняет в базе
        /// </summary>
        /// <param name="main">Событие</param>
        public void SetTime(MainEvent mainEvent)
        {
            if(mainEvent.EndTime > mainEvent.StartTime)
            { 
                var timeDuratoin = mainEvent.EndTime - mainEvent.StartTime;
                mainEvent.Time = Math.Round(timeDuratoin.TotalHours).ToString() + "ч " + timeDuratoin.Minutes + "мин " + timeDuratoin.Seconds + "сек.";
            }
            else
            {
                mainEvent.Time = "";
            }

        }

        /// <summary>
        /// Список всех родителей
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locId"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetAllParent(string policyName, int locId)
        {
            return context.MainEvents
                .Include("Client")
                .Include("Policy")
                .Include("MasterServer")
                .Include("MediaServer")
                .Include("Delay")
                .Include("Frequency")
                .Include("Location")
                .Where(x => x.Policy.Name == policyName && x.LocationId == locId && x.isParent == true).OrderByDescending(o => o.MainEventId).ToArray();
        }
        /// <summary>
        /// Список всех родителей
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locationServerName"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetAllParent(string policyName, string locationServerName)
        {
            return context.MainEvents
                .Include("Client")
                .Include("Policy")
                .Include("MasterServer")
                .Include("MediaServer")
                .Include("Delay")
                .Include("Frequency")
                .Include("Location")
                .Where(x => x.Policy.Name == policyName && x.Location.ServerName == locationServerName && x.isParent == true).OrderByDescending(o => o.MainEventId).ToArray();
        }

        /// <summary>
        /// Список всех родителей которые в прогрессе
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locationServerName"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetAllProgressParentByLocalName(Policy policy, int locId)
        {
            return context.MainEvents.Where(x => x.Policy.PolicyId == policy.PolicyId && x.LocationId == locId && x.isParent == true 
            && x.EndTime == DateTime.Parse("1970-01-01 03:00:00.0000000")
            ).OrderByDescending(o => o.MainEventId).ToArray();
        }

       
        /// <summary>
        /// Обновляем событие. Время окончания, размер, скорость, статус, время выполнения
        /// </summary>
        /// <param name="lastParent"></param>
        /// <param name="newEvent"></param>
        public void UpdateEvent(MainEvent lastParent, MainEvent newEvent)
        {
            lastParent.EndTime = newEvent.EndTime;
            lastParent.Size = newEvent.Size;
            lastParent.Speed = newEvent.Speed;
            lastParent.Status = newEvent.Status;
            lastParent.Time = newEvent.Time;
           
            context.SaveChanges();
        }

      
        /// <summary>
        /// Получаем событие по JobId
        /// </summary>
        /// <param name="JobID"></param>
        /// <param name="locId"></param>
        /// <returns></returns>
        public MainEvent GetById(MainEvent mainEvent, int locId)
        {
            var aa = context.MainEvents.Single(x => x.JobID == mainEvent.JobID && x.PolicyId == mainEvent.PolicyId && x.LocationId == locId && x.MasterServerId == mainEvent.MasterServerId);
            return aa;
        }

        /// <summary>
        /// JobID родительского события 
        /// </summary>
        /// <param name="policyEvent"></param>
        /// <returns></returns>
        public int FindParent(MainEvent policyEvent)
        {

            //if (policyEvent.EndTime > DateTime.Parse("1999-01-01 03:00:00.0000000") && (policyEvent.EndTime > policyEvent.StartTime) 
            //    && context.MainEvents.Any(x => x.PolicyId == policyEvent.PolicyId && x.LocationId == policyEvent.LocationId
            //                            && (x.Type == 0 || x.Type == 1 || x.Type == 4) && x.isParent == true
            //                            && x.StartTime <= policyEvent.StartTime && x.EndTime >= policyEvent.EndTime)
            //    )
            if (policyEvent.EndTime > DateTime.Parse("1999-01-01 03:00:00.0000000") && (policyEvent.EndTime > policyEvent.StartTime))
            {

                try
                {
                    MainEvent singleEvent = context.MainEvents.SingleOrDefault(x => x.PolicyId == policyEvent.PolicyId && x.LocationId == policyEvent.LocationId && x.MasterServerId == policyEvent.MasterServerId
                                                                 && (x.Type == 0 || x.Type == 1 || x.Type == 4) && x.isParent == true
                                                                 && x.StartTime <= policyEvent.StartTime && x.EndTime >= policyEvent.EndTime);


                    if (singleEvent == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return singleEvent.JobID;
                    }

                }
                catch(Exception ex)
                {
                    if(ex.Message.Contains("Sequence contains more than one element"))
                    {
                        return -2;
                    }
                    else
                    //такое может быть если вычисляем по дате старта и конца, и в эти параметры попадает несколько родителей. Например политика идет больше недели, а по расписанию каждую неделю.
                    return -1;
                }
               
            }

            else if (policyEvent.Type == 2)
                return -1;
            else
               return policyEvent.JobID;
        }

        /// <summary>
        /// Это не последнее родительское событие, bool
        /// </summary>
        /// <param name="mainEvent"></param>
        /// <returns></returns>
        public bool isNotLastParent(MainEvent mainEvent)
        {
            return context.MainEvents.Any(x => x.PolicyId == mainEvent.PolicyId && x.LocationId == mainEvent.LocationId && x.ClientId == mainEvent.ClientId && x.MasterServerId == mainEvent.MasterServerId
            && x.isParent == true && 
            x.JobID > mainEvent.JobID
            ); ;
        }

        /// <summary>
        /// Список событий без родителя у данной политики
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locationServerName"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetWithoutParent(Policy policy, string locationServerName)
        {
            return context.MainEvents.Where(x => x.Policy == policy && x.Location.ServerName == locationServerName 
            && x.ParrentId == -1).ToArray();
        }

        /// <summary>
        /// Политики типа "2" могут быть сами себе родителями, например политики *Delta
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locationServerName"></param>
        public void TypeTwoIsParrent(Policy policy, string locationServerName)
        {
            var tempList = context.MainEvents.Where(x => x.Policy == policy && x.Location.ServerName == locationServerName
            && x.Type == 2 && x.JobID == x.ParrentId && x.isParent != true).ToList();

            foreach(var tempEvent in tempList)
            {
                if (!context.MainEvents.Any(x => x.PolicyId == tempEvent.PolicyId && x.LocationId == tempEvent.LocationId
                && x.Type != 2 && x.isParent == true && x.StartTime <= tempEvent.StartTime && x.EndTime == DateTime.Parse("1970-01-01 03:00:00.0000000")))
                { 
                    var parrentEvent = context.MainEvents.Where(x => x.PolicyId == tempEvent.PolicyId && x.LocationId == tempEvent.LocationId
                    && x.Type != 2 && x.isParent == true && x.StartTime <= tempEvent.StartTime
                    && (x.EndTime > tempEvent.EndTime)).FirstOrDefault();

                    if (parrentEvent != null && parrentEvent.EndTime >= tempEvent.EndTime)
                    {
                        tempEvent.ParrentId = parrentEvent.JobID;
                        context.SaveChanges();
                        UpdateParrentSize(tempEvent, tempEvent.LocationId);
                    }
                    else
                        tempEvent.isParent = true;


                    context.SaveChanges();
                }
                
            }
            
        }

        /// <summary>
        /// Метод возвращает родителей размером 0 байт, завершенных, и тех кто есть в родителях у кого-нибудь
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="locationServerName"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetWithParentAndWithoutSize(string policyName, int locId)
        {
            return context.MainEvents.Where(x => x.Policy.Name == policyName && x.LocationId == locId
            && x.Size == 0 && x.EndTime != DateTime.Parse("1970-01-01 03:00:00.0000000")
            && x.isParent == true && context.MainEvents.Any(p => p.ParrentId == x.JobID && p.isParent == false)
            ).OrderByDescending(o => o.MainEventId).ToArray();
        }
        /// <summary>
        /// Получаем список дочерних событий 
        /// </summary>
        /// <param name="pEvent"></param>
        /// <returns></returns>
        public IEnumerable<MainEvent> GetAllChildren(MainEvent pEvent)
        {
            return context.MainEvents.Include("Client")
                .Where(x => x.ParrentId == pEvent.JobID && x.LocationId == pEvent.LocationId && x.JobID != pEvent.JobID && x.isParent == false && x.MasterServerId == pEvent.MasterServerId).ToArray();
        }

        /// <summary>
        /// Подсчет статискити
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public List<StatiscticItem> GetStatistic(DateTime dateTime)
        {
            DateTime dateMonth = new DateTime(dateTime.Year, dateTime.Month,1);
            DateTime dateYear = new DateTime(dateTime.Year, 1, 1);
            
            List<StatiscticItem> resultMonth = new List<StatiscticItem>();
            List<StatiscticItem> resultYear = new List<StatiscticItem>();

            List<string> locations = context.Locations.Select(x => x.Name).Distinct().ToList();
            foreach(var loc in locations)
            {
                StatiscticItem statiscticItemMonth = new StatiscticItem();
                statiscticItemMonth.Name = loc;
                statiscticItemMonth.typeOfStatistic = TypeOfStatistic.Month;
                statiscticItemMonth.Count = context.MainEvents.Where(x => x.Location.Name == loc && x.StartTime >= dateMonth && x.StartTime < dateMonth.AddMonths(1)).Select(l => l.PolicyId).Distinct().Count();
                statiscticItemMonth.Size = context.MainEvents.Where(x => x.Location.Name == loc && x.isParent == true && x.StartTime >= dateMonth && x.StartTime < dateMonth.AddMonths(1)).Select(l => l.Size).Sum();
                statiscticItemMonth.SizeHumanRead = SizeToHumanRead(statiscticItemMonth.Size);
                resultMonth.Add(statiscticItemMonth);

                StatiscticItem statiscticItemYear = new StatiscticItem();
                statiscticItemYear.Name = loc;
                statiscticItemYear.typeOfStatistic = TypeOfStatistic.Year;
                statiscticItemYear.Count = context.MainEvents.Where(x => x.Location.Name == loc && x.StartTime >= dateYear && x.StartTime < dateYear.AddYears(1)).Select(l => l.PolicyId).Distinct().Count();
                statiscticItemYear.Size = context.MainEvents.Where(x => x.Location.Name == loc && x.isParent == true && x.StartTime >= dateYear && x.StartTime < dateYear.AddYears(1)).Select(l => l.Size).Sum();
                statiscticItemYear.SizeHumanRead = SizeToHumanRead(statiscticItemYear.Size);
                resultYear.Add(statiscticItemYear);

            }

            StatiscticItem statiscticItemSumMonth = new StatiscticItem();
            statiscticItemSumMonth.Name = "All";
            statiscticItemSumMonth.typeOfStatistic = TypeOfStatistic.Month;
            statiscticItemSumMonth.Count = resultMonth.Sum(x => x.Count);
            statiscticItemSumMonth.Size = resultMonth.Sum(x => x.Size);
            statiscticItemSumMonth.SizeHumanRead = SizeToHumanRead(statiscticItemSumMonth.Size);
            resultMonth.Add(statiscticItemSumMonth);

            StatiscticItem statiscticItemSumYear = new StatiscticItem();
            statiscticItemSumYear.Name = "All";
            statiscticItemSumYear.typeOfStatistic = TypeOfStatistic.Year;
            statiscticItemSumYear.Count = resultYear.Sum(x => x.Count);
            statiscticItemSumYear.Size = resultYear.Sum(x => x.Size);
            statiscticItemSumYear.SizeHumanRead = SizeToHumanRead(statiscticItemSumYear.Size);
            resultYear.Add(statiscticItemSumYear);

           return resultMonth.Concat(resultYear).ToList();
        }

        /// <summary>
        /// Меняем clientId у родительского события. Нужен для LSN
        /// </summary>
        /// <param name="lsnParent"></param>
        /// <param name="clientId"></param>
        public void ChangeClientId(MainEvent lsnParent, int clientId)
        {
            lsnParent.ClientId = clientId;
            context.SaveChanges();
        }

    }
}
