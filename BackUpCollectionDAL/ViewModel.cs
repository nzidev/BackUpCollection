using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Text;
using PagedList;
using BackUpCollectionDAL.Extensions;

namespace BackUpCollectionDAL
{
    public class ViewModel
    {
        public List<MainEvent> MainEvents { get; set; }
        public IPagedList<MainEvent> PagedMainEvents { get; set; }
        public List<Location> Locations { get; set; }
        public List<Progress> Progresses { get; set; }
        public string SearchString { get; set; }
        public string dtStart { get; set; }
        public string dtEnd { get; set; }
        public bool IsGood { get; set; }
        public bool IsProblem { get; set; }
        public bool IsError { get; set; }
        public bool IsProgress { get; set; }
        public int page { get; set; }
        public int pageCount { get; set; }
        public Dictionary<string,string> Place { get; set; }
        public Dictionary<string, string> Zone { get; set; }
    }
}
