using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица цодов
    /// </summary>
    public class Location
    {
        public int LocationId { get; set; }
        public string Description { get; set; }
        public string ServerName { get; set; }
        public string Name { get; set; }
    }
}
