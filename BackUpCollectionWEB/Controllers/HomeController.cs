using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BackUpCollectionWEB.Models;
using BackUpCollectionDAL.Repository;
using BackUpCollectionDAL;
using PagedList.Mvc;
using PagedList;
using BackUpCollectionDAL.DataBase;
using BackUpCollectionDAL.Extensions;

namespace BackUpCollectionWEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly MainEventRepository mainEventRepository;
        private readonly LocationRepository locationRepository;
        private readonly ClientRepository clientRepository;
        private readonly ProgressRepository progressRepository;
        private readonly BackupLogRepository backupLogRepository;

        public HomeController(MainEventRepository mainEventRepository, LocationRepository locationRepository, ClientRepository clientRepository, ProgressRepository progressRepository, BackupLogRepository backupLogRepository)
        {
            //            _logger = logger;
            this.mainEventRepository = mainEventRepository;
            this.locationRepository = locationRepository;
            this.clientRepository = clientRepository;
            this.progressRepository = progressRepository;
            this.backupLogRepository = backupLogRepository;
            //this.backupLogRepository = backupLogRepository;
        }

        [HttpGet]
        public IActionResult Index(string searchText, string dtStart, string dtEnd, string loc, string zone, bool IsGood = true, bool IsProblem = true, bool IsError = true, bool IsProgress = true, int? page = 1)
        {
            var browserInfo = Request.Headers["User-Agent"].ToString();
            backupLogRepository.WriteLog(searchText, User.Identity.Name, browserInfo, User.Identity.Name.Split('\\')[1]);

            ViewModel model = new ViewModel();
            model.Place = new Dictionary<string, string>();
            model.Place.Add("", "Все");
            model.Place.Add("", "");
            model.Place.Add("", "");
            model.Place.Add("", "");

            model.Zone = new Dictionary<string, string>();
            model.Zone.Add("", "Все");
            
            model.Zone.Add("idmz", "IDMZ");
            model.Zone.Add("inside", "Inside");


            model.page = (page ?? 1);

            model.IsError = IsError;
            model.IsGood = IsGood;
            model.IsProblem = IsProblem;
            model.IsProgress = IsProgress;

            model.SearchString = searchText;
            model.Locations = locationRepository.GetAll();
            model.dtStart = dtStart ?? DateTime.Now.ToString("dd.MM.yyyy");
            model.dtEnd = dtEnd ?? DateTime.Now.Date.ToString("dd.MM.yyyy");

            return View(model);
        }
        [HttpGet]
        public IActionResult PoliciesList(string searchText, string dtStart, string dtEnd, string loc, string zone, bool IsGood = true, bool IsProblem = true, bool IsError = true, bool IsProgress = true, int? page = 1)
        {

            LinqFilter linqFilter = new LinqFilter();
            ViewModel model = new ViewModel();
            int pageSize = 20; //количество объектов на странице

            model.Place = new Dictionary<string, string>();
            model.Place.Add("", "Все");
            model.Place.Add("", "");
            model.Place.Add("", "");
            model.Place.Add("", "");

            model.Zone = new Dictionary<string, string>();
            model.Zone.Add("", "Все");
            
            model.Zone.Add("idmz", "IDMZ");
            model.Zone.Add("inside", "Inside");

            model.page = (page ?? 1);
            //надо что сделать с этим дублированием данных
            linqFilter.IsError = model.IsError = IsError;
            linqFilter.IsGood = model.IsGood = IsGood;
            linqFilter.IsProblem = model.IsProblem = IsProblem;
            linqFilter.IsProgress = model.IsProgress = IsProgress;
            model.SearchString = searchText;
            model.Locations = locationRepository.GetAll();
            //model.Progresses = progressRepository.GetAll();


            // model.MainEvents = mainEventRepository.GetLastTen(searchText);


            linqFilter.searchText = searchText;







            model.dtStart = dtStart ?? DateTime.Now.ToString("dd.MM.yyyy");
            model.dtEnd = dtEnd ?? DateTime.Now.Date.ToString("dd.MM.yyyy");

            linqFilter.dtStart = DateTime.Parse(model.dtStart);
            linqFilter.dtEnd = DateTime.Parse(model.dtEnd).AddDays(1).AddSeconds(-1);

            if (loc != null)
            {
                linqFilter.locationId = loc;
            }
            if (zone != null)
            {
                linqFilter.zone = zone;
            }

            IEnumerable<MainEvent> MainEvents = mainEventRepository.GetLast(linqFilter);
            model.pageCount = (int)Math.Ceiling((double)MainEvents.Count() / pageSize);

            model.PagedMainEvents = MainEvents.ToPagedList(model.page, pageSize);



            return this.PartialView("PoliciesList", model);
        }

        public IActionResult ClientInfo(string clientName)
        {
            ViewData["Name"] = clientName;
            Client client = clientRepository.GetByName(clientName);

            ViewData["Resurs"] = client.Resurs;
            ViewData["Podsistema"] = client.Podsistema;
            ViewData["Kompleks"] = client.Kompleks;
            ViewData["Description"] = client.Description;
            return View();
        }


        [HttpGet]
        public IActionResult Statistic(string Month, string Year)
        {
            ViewData["Year"] = mainEventRepository.getYear();
            ViewData["Month"] = mainEventRepository.getMonth();
            DateTime dt = new DateTime();
            try { 
                dt = new DateTime(Convert.ToInt32(Year), Convert.ToInt32(Month),1);
            }
            catch
            {
                dt = DateTime.Now;
            }
            
            if(Year != "" && Year != null)
            {
                ViewData["SelectYear"] = Year;
            }
            else
            {
                ViewData["SelectYear"] = DateTime.Now.Year;
            }

            if (Month != "" && Month != null)
            {
                ViewData["SelectMonth"] = mainEventRepository.GetMonthName(Int32.Parse(Month));
            }
            else
            {
                ViewData["SelectMonth"] = mainEventRepository.GetMonthName(DateTime.Now.Month);
                
            }

            ViewData["Statistic"] = mainEventRepository.GetStatistic(dt);
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
