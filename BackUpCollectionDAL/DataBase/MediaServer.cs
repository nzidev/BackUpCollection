using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица Медиасерверов
    /// </summary>
    public class MediaServer
    {
        public int MediaServerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
