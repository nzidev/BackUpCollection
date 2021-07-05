using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class MailSettingRepository
    {
        CoreDbContext context;
        public MailSettingRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        ///  Получить объект MailSetting по имени
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public MailSetting GetByName(string Name)
        {
            var MailSettings = context.MailSettings.Where(s => s.Name == Name).FirstOrDefault<MailSetting>();
            if (MailSettings != null)
            {
                return MailSettings;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Добавить новый или обновить существующий
        /// </summary>
        /// <param name="mailSetting"></param>
        public void AddOrUpdate(MailSetting mailSetting)
        {
            
            var MailSettings = context.MailSettings.Where(s => s.Name == mailSetting.Name).FirstOrDefault<MailSetting>();
            if(MailSettings == null)
            { 
                context.Entry(mailSetting).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
            }
            else
            {
                MailSettings.FromAddress = mailSetting.FromAddress;
                MailSettings.Server = mailSetting.Server;
                MailSettings.ToAddress = mailSetting.ToAddress;
                context.SaveChanges();
            }
        }
    }
}
