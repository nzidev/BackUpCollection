using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class ADOConnectionStringRepository
    {
        CoreDbContext context;
        public ADOConnectionStringRepository(CoreDbContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// Получить объект по имени
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ADOConnectionString GetByName(string Name)
        {
            var ADOConnectionString = context.ADOConnectionStrings.Where(s => s.Name == Name).FirstOrDefault<ADOConnectionString>();
            if (ADOConnectionString != null)
            {
                return ADOConnectionString;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Добавить новый или обновить существующий
        /// </summary>
        /// <param name="ADOConnectionString"></param>
        public void AddOrUpdate(ADOConnectionString ADOConnectionString)
        {
            var ADOConnectionStringTMP = context.ADOConnectionStrings.Where(s => s.Name == ADOConnectionString.Name).FirstOrDefault<ADOConnectionString>();
            if (ADOConnectionStringTMP == null)
            {

                context.Entry(ADOConnectionString).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
            }
            else
            {
                ADOConnectionStringTMP.ConnectionString = ADOConnectionString.ConnectionString;
                context.SaveChanges();
            }
        }
        

        /// <summary>
        /// Обновить параметр отправлять mail или не отправлять.
        /// </summary>
        /// <param name="serviceSetting"></param>
        public void UpdateMailSend(ServiceSetting serviceSetting)
        {
            var ADOConnectionStringTMP = context.ADOConnectionStrings.Where(s => s.ADOConnectionStringId == serviceSetting.ADOConnectionStringId).FirstOrDefault<ADOConnectionString>();
            if (ADOConnectionStringTMP != null)
            {
                ADOConnectionStringTMP.isMailSend = serviceSetting.ADOConnectionString.isMailSend;
                context.SaveChanges();
            }
        }
    }
}
