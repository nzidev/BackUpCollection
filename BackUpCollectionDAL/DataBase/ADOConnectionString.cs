using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица строки соединения с OPSCenter
    /// </summary>
    public class ADOConnectionString
    {
        public int ADOConnectionStringId { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        /// <summary>
        /// Было отправлено письмо или нет. По умолчанию false. Создано чтобы не спамить ошибками.
        /// </summary>
        public bool isMailSend { get; set; }
    }
}
