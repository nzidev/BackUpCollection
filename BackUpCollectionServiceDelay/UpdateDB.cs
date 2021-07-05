using BackUpCollectionDAL.DataBase;
using BackUpCollectionDAL.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionServiceDelay
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
        private DelayRepository delayRepository;
        private FrequencyRepository frequencyRepository;
       // private IEnumerable<MainEvent> PolicyWithoutFrequencyId;

        public void Start(CoreDbContext contextDb, int PolicyDays)
        {
            //подключаем репозитории для работы с объектами
            mainEventRepository = new MainEventRepository(contextDb);
            locationRepository = new LocationRepository(contextDb);
            clientRepository = new ClientRepository(contextDb);
            masterServerRepository = new MasterServerRepository(contextDb);
            mediaServerRepository = new MediaServerRepository(contextDb);
            policyRepository = new PolicyRepository(contextDb);
            delayRepository = new DelayRepository(contextDb);
            frequencyRepository = new FrequencyRepository(contextDb);

            Notify?.Invoke("start BackUpCollectionServiceDelay", 201);

            int days = PolicyDays;

            ClearOldWithoutParents(days);

            

            var policies = policyRepository.GetAll().ToList();
            Random random = new Random();
            var policiesRandom = policies.OrderBy(item => random.Next()).ToList();
            
            int aaa = policies.Count();

            int tenProcent = (int)(policiesRandom.Count * 0.1);


                foreach (Policy policy in policiesRandom.Take(tenProcent))
                //for(int i =0; i<= 1; i++)
                {
                //var policy = policiesRandom[i];
                    WorkWithPolicy(policy, days);
                }
            
            

        }

        /// <summary>
        /// Чистим старые политики без родителей. Они уже не обновятся, т.к. в базе их уже нет, а в матаматике учавствуют
        /// </summary>
        /// <param name="days">Количество дней для отчистки</param>
        private void ClearOldWithoutParents(int days)
        {
            Notify?.Invoke("Почистим старые политики без родителей", 203);
            mainEventRepository.ClearOldWithoutParents(days);
            //Notify?.Invoke("Чисто!", 204);
        }

        private void WorkWithPolicy(Policy policy, int days)
        {
            try
            {

                // string tmp = "Fs_n5001-ppa-kpe_WAL";
                //  Notify?.Invoke("start " + policy.Name, 22);
                //Policy policy2 = policy;
                //policy2 = policyRepository.GetByName(tmp);

                //Получаем список LocationID потому что политика с одним именем может быть в разных сегментах и площадках
                foreach (int locId in mainEventRepository.getAllLocationByPolicyId(policy.PolicyId))
                {

                    string delayHourHumanReadString = AnalizeDelayHour(policy, locId, days);

                    string delayDayHumanReadString = AnalizeDelayDay(policy, locId, days);

                    if (delayHourHumanReadString != "")
                        mainEventRepository.UpdatePolicyFrequency(policy, locId, frequencyRepository.GetByName(delayHourHumanReadString).FrequencyId, days);
                    if (delayDayHumanReadString != "")
                        mainEventRepository.UpdatePolicyDelayDay(policy, locId, delayRepository.GetByName(delayDayHumanReadString).DelayId, days);
                }
            }
            catch (Exception ex)
            {
                Notify?.Invoke("BackUpCollectionServiceDelay отработал с ошибкой. " + policy.Name, 205);
            }
        }

        private string AnalizeDelayHour(Policy policy, int locId, int days)
        {
            string delayHourHumanReadString = "";

            var PolicyWithoutFrequencyId = mainEventRepository.GetAllParent(policy.Name, locId).Where(x => x.Status == 0 && x.StartTime > DateTime.Now.AddDays(-1 * days) && x.FrequencyId == 1);
            
            if (PolicyWithoutFrequencyId.Any())
            {

                var groupTimes = mainEventRepository.GetAllParent(policy.Name, locId).Where(x => x.Status == 0 && x.StartTime > DateTime.Now.AddDays(-1 * days)).GroupBy(x => x.StartTime.TimeOfDay)

               .Select(g => new { StartTime = g.Key, Count = g.Count() })
               .OrderByDescending(t => t.Count)
               .ToList();

                if (groupTimes.Count > 0)
                {
                    List<string> delayHour = new List<string> { };
                    List<string> delayMin = new List<string> { };
                    Dictionary<int, string> delayHourHumanRead = new Dictionary<int, string>();
                    Dictionary<int, string> delayMinHumanRead = new Dictionary<int, string>();

                    if ((groupTimes[0].Count * 100) / groupTimes.Sum(x => x.Count) > 50) //раз в день
                    {
                        delayHourHumanRead.Add(1, "В " + groupTimes[0].StartTime.ToString(@"hh\:mm"));
                    }
                    else
                    {
                        //несколько раз в день, поэтому количество дней уменьшаем в 4 раза для скорости
                        groupTimes.Clear();
                        var groupTimes2 = mainEventRepository.GetAllParent(policy.Name, locId).Where(x => x.Status == 0 && x.StartTime > DateTime.Now.AddDays(-1 * (days / 4))).OrderBy(t => t.StartTime)
                            .Select(y => y.StartTime)
                            .ToList();

                        if (groupTimes2.Count > 1)
                        {
                            for (int i = 0; i < groupTimes2.Count - 1; i++)
                            {
                                var delayMinTmp = groupTimes2[i + 1] - groupTimes2[i];
                                TimeSpan t1 = new TimeSpan(0, 0, delayMinTmp.Seconds);

                                delayMinTmp = delayMinTmp.Subtract(t1);
                                delayMin.Add(delayMinTmp.ToString(@"hh\:mm"));
                            }

                            var groupMin = delayMin.GroupBy(i => i).Select(g => new { Key = g.Key, Count = g.Count() })
                            .OrderByDescending(t => t.Count)
                            .ToList();

                            if ((groupMin[0].Count * 100) / groupMin.Sum(x => x.Count) > 50)
                            {
                                delayHourHumanRead.Add(1, "Каждые " + groupMin[0].Key.ToString());
                            }

                            groupTimes2 = null;
                        }
                    }

                    foreach (var humanTime in delayHourHumanRead.Values)
                    {
                        delayHourHumanReadString += humanTime;
                    }


                }

                PolicyWithoutFrequencyId=null;
                return delayHourHumanReadString;
            }
            else
                return "";
        }
        private string AnalizeDelayDay(Policy policy, int locId, int days)
        {
            string delayDayHumanReadString = "";

            var PolicyWithoutDelaiId = mainEventRepository.GetAllParent(policy.Name, locId).Where(x => x.Status == 0 && x.StartTime > DateTime.Now.AddDays(-1 * days) && x.DelayId == 1);

            if (PolicyWithoutDelaiId.Any())
            {
                //Когда запускается - ежедневно, каждый Вт или раз в месяц
                var groupDay = mainEventRepository.GetAllParent(policy.Name, locId).Where(x => x.Status == 0 && x.StartTime > DateTime.Now.AddDays(-1 * days)).GroupBy(x => x.StartTime.DayOfWeek)
                    .Select(g => new { DayOfWeek = (DayOfWeek)g.Key, Count = g.Count() })
                    .OrderByDescending(t => t.Count)
                    .ToList();



                if (groupDay.Count > 0)
                {
                    List<string> delayDay = new List<string> { };
                    Dictionary<int, string> delayDayHumanRead = new Dictionary<int, string>();

                    if ((groupDay[0].Count * 100) / groupDay.Sum(x => x.Count) > 50)
                    {
                        delayDay.Add(groupDay[0].DayOfWeek.ToString());
                    }
                    else
                    {
                        int summ = groupDay.Sum(x => x.Count);
                        foreach (var day in groupDay)
                        {
                            if (Math.Round((double)(day.Count * 100) / groupDay.Sum(x => x.Count), 1) > Math.Round((100 / ((double)summ + 3)), 1))
                            {
                                delayDay.Add(day.DayOfWeek.ToString());
                            }
                        }
                    }

                    if (delayDay.Count == 7)
                    {
                        delayDayHumanRead.Add(1, "Ежедневно");
                    }
                    else if (delayDay.Count > 0)
                    {

                        foreach (var day in delayDay)
                        {
                            switch (day)
                            {
                                case "Monday":
                                    delayDayHumanRead.Add(1, "Пн");
                                    break;
                                case "Tuesday":
                                    delayDayHumanRead.Add(2, "Вт");
                                    break;
                                case "Wednesday":
                                    delayDayHumanRead.Add(3, "Ср");
                                    break;
                                case "Thursday":
                                    delayDayHumanRead.Add(4, "Чт");
                                    break;
                                case "Friday":
                                    delayDayHumanRead.Add(5, "Пт");
                                    break;
                                case "Saturday":
                                    delayDayHumanRead.Add(6, "Сб");
                                    break;
                                case "Sunday":
                                    delayDayHumanRead.Add(7, "Вс");
                                    break;
                            }
                        }
                        delayDayHumanRead = delayDayHumanRead.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);
                    }


                    foreach (var humanDay in delayDayHumanRead.Values)
                    {
                        delayDayHumanReadString += humanDay;
                    }
                    groupDay = null;
                }


               

                return delayDayHumanReadString;
            }
            else
                return "";
        }
        public void Dispose()
        {
            Notify?.Invoke("Кто-то стопит BackUpCollectionServiceDelay", 202);
         //   mainEventRepository = null;
        }
    }
}
