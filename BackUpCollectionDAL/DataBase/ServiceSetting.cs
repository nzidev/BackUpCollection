using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Таблица настроек сервиса - замена appsetting.json
    /// </summary>
    public class ServiceSetting
    {
        public int ServiceSettingId { get; set; }
        public string ServiceName { get; set; }
        public int ADOConnectionStringId { get; set; }
        public virtual ADOConnectionString ADOConnectionString { get; set; }
        public int DelayMs { get; set; }
        public bool UpdateDelayAndFrequency { get; set; }
        public string Mode { get; set; }
        public byte Type { get; set; }
        public byte HalfPolicies { get; set; }
        public int MailSettingId { get; set; }
        public virtual MailSetting MailSetting { get; set; }
        
    }
}
