using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица политик
    /// </summary>
    public class Policy
    {
        public int PolicyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
