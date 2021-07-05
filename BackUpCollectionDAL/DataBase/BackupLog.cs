using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица логирования посещения web-страницы
    /// </summary>
    public class BackupLog
    {
        public int BackupLogId { get; set; }
        public string Account { get; set; }
        public string SearchText { get; set; }
        public string Username { get; set; }
        public string BrowserInfo { get; set; }
        public DateTime DateTime { get; set; }
    }
}
