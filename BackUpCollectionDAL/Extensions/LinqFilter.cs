using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.Extensions
{
    /// <summary>
    /// Класс сбора параметров для фильтра запросов на WEB-странице в один объект
    /// </summary>
    public class LinqFilter
    {
        public DateTime dtStart { get; set; }
        public DateTime dtEnd { get; set; }
        public string locationId { get; set; }
        public string zone { get; set; }
        public string searchText { get; set; }
        public bool IsProgress { get; set; }
        public bool IsGood { get; set; }
        public bool IsProblem { get; set; }
        public bool IsError { get; set; }

    }
}
