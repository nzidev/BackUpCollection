using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица клиента
    /// </summary>
    public class Client
    {
        public int ClientId { get; set; }
        public string Name { get; set; }
        public string Resurs { get; set; }
        public string Kompleks { get; set; }
        public string Podsistema { get; set; }
        /// <summary>
        /// Состояние Standby or Primary
        /// </summary>
        public string State { get; set; }
        public string Description { get; set; }
    }
}
