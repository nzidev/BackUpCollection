using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица настроек почты
    /// </summary>
    public class MailSetting
    {
        public int MailSettingId { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
        public string ToAddress { get; set; }
        public string FromAddress { get; set; }
    }
}
