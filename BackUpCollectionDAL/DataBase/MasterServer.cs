using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблицы мастер серверов
    /// </summary>
    public class MasterServer
    {
        public int MasterServerId { get; set; }
        public int OpsId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ServerName { get; set; }
    }
}
