using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица прогресса обработки для наглядности
    /// </summary>
    public class Progress
    {
        public int ProgressId { get; set; }
        public string serviceName { get; set; }
        public int CurrentPos { get; set; }
        public int Total { get; set; }
    }
}
